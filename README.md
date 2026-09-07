# PhotinBro

A [Photino](https://www.tryphotino.io/) desktop app: a native window hosting an Angular UI,
with the .NET host in `backend/` (project `GimxBackend`) and the Angular workspace in `FrontEnd/`.

```
PhotinBro.slnx            solution (XML slnx format)
backend/                  GimxBackend - .NET 10 Photino host
  Program.cs                window setup + #if DEBUG mode switch + web-message bridge
  wwwroot/                  Angular build output (generated, git-ignored, never shipped loose)
FrontEnd/                 Angular 21 workspace (project `frontend`)
```

## Prerequisites

- .NET SDK 10.0
- Node.js >= 24 (Angular 21 requires `^20.19 || ^22.12 || >=24`)
- **WebView2 Runtime** on Windows (preinstalled on Windows 11)

## How the two modes work

`backend/Program.cs` flips on the `DEBUG` conditional-compilation symbol:

| | Debug | Release |
|---|---|---|
| Window URL | `http://localhost:4200` (`ng serve`) | `http://localhost:8000+` (Photino static file server) |
| Static file server | not started | `PhotinoServer.CreateStaticFileServer` |
| UI files | served by `ng serve` | **embedded in `GimxBackend.dll`** |
| DevTools + context menu | enabled | disabled |
| Console window | shown (`Exe`) | hidden (`WinExe`) |

Debug points the window straight at the Angular dev server, so Angular's live reload works
inside the native window.

## Development

Two terminals:

```bash
cd FrontEnd && npm start
```

```bash
dotnet run --project backend/GimxBackend.csproj
```

The window opens on the dev server. Editing anything under `FrontEnd/src` reloads it in place.

## Release build

```bash
dotnet publish backend/GimxBackend.csproj -c Release -o dist
```

The `EmbedAngularBuild` MSBuild target runs `npm ci` (only if `FrontEnd/node_modules` is
missing) and `npm run build`, then embeds everything the Angular build wrote into `wwwroot`
as resources inside `GimxBackend.dll`. `dist/` therefore contains no UI files at all:

```
dist/
  GimxBackend.exe        <- run this
  GimxBackend.dll        <- Angular bundle lives in here
  Photino.NET*.dll
  runtimes/              <- Photino native libraries
```

Skip the npm steps on a .NET-only rebuild (whatever is already in `wwwroot` still gets
embedded; the build errors out if `wwwroot` is empty):

```bash
dotnet publish backend/GimxBackend.csproj -c Release -o dist -p:SkipAngularBuild=true
```

### How the embedding works

`PhotinoServer` serves through a `CompositeFileProvider` of a physical `wwwroot` provider and
a `ManifestEmbeddedFileProvider` rooted at `Resources/<webRootFolder>`. The `EmbedAngularBuild`
target adds each file under `wwwroot` as an `EmbeddedResource` with
`Link="Resources\wwwroot\..."`, which is what places it at that path in the generated
manifest. `GenerateEmbeddedFilesManifest` is on for Release only - Debug never serves static
files, and enabling it with no embedded resources emits a build warning.

Two ordering details the target depends on, both easy to break:

- The `wwwroot` glob happens *inside* the target, not in a static `ItemGroup`. MSBuild
  evaluates project-level globs before any target runs, so files the `npm run build` step just
  wrote would otherwise be invisible to the same build.
- `BeforeTargets` lists `_CalculateEmbeddedFilesManifestInputs`. That target is prepended to
  `PrepareResourceNamesDependsOn`, so it runs *before* `AssignTargetPaths` and would hash an
  `EmbeddedResource` set that does not yet include the Angular output.

## Angular -> .NET bridge

There is no HTTP API; the UI talks to the host over Photino's web-message channel.

- Angular: `FrontEnd/src/app/photino.service.ts` wraps `window.external.sendMessage` /
  `receiveMessage`, exposes a `messages` signal, and reports `available === false` when the
  page is open in a plain browser instead of the Photino window.
- .NET: `Program.OnWebMessageReceived` handles inbound messages and replies with
  `window.SendWebMessage(...)`.

The home page has a "Ping the .NET host" button that exercises the round trip.

## Notes

- The Angular build writes straight into `backend/wwwroot` via
  `outputPath: { base: "../backend/wwwroot", browser: "" }` in `angular.json`. The builder
  **deletes that directory** on every build, so keep nothing else there.
- Routing uses `withHashLocation()`. `PhotinoServer` serves static files with no SPA
  fallback, so a reload on a path-routed deep link would 404 in Release.
- `Photino.NET` 4.0.16 targets `net9.0`; referencing it from `net10.0` is normal and
  warning-free.
- The published app creates an empty `wwwroot/` folder next to the executable on first run.
  That is ASP.NET Core materialising `WebApplicationOptions.WebRootPath`; nothing is served
  from it (the request log shows `Physical path: 'N/A'` for every file). Dropping real files
  in there would override the embedded copies, which is occasionally useful for debugging.

## Pinned versions

| Package | Version |
|---|---|
| .NET | `net10.0` (SDK 10.0.400) |
| `Photino.NET` | 4.0.16 |
| `Photino.NET.Server` | 4.0.12 |
| `Microsoft.Extensions.FileProviders.Embedded` | 10.0.11 |
| `@angular/cli` / `@angular/build` | 21.2.23 (v21 LTS) |
| `@angular/core` | 21.2.22 |
