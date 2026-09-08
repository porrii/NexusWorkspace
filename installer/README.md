# Instalador de Windows

`NexusWorkspace` se distribuye con **[Velopack](https://velopack.io)**: un `Setup.exe`
autónomo (sin requisitos previos — incluye el runtime de .NET 9) con actualizaciones
delta opcionales.

## Generar el instalador

```bat
cd installer
pack.bat            :: usa la <Version> de Directory.Build.props (1.0.0)
pack.bat 1.0.1      :: o una versión concreta
```

El script es portable (funciona en cualquier equipo):

0. resuelve un .NET 9 SDK con `scripts\dotnet-bootstrap.bat` — usa el que ya
   tengas o instala una copia privada en `<repo>\.dotnet` sin admin;
1. instala la herramienta `vpk` si falta (`dotnet tool`, en la carpeta
   `%DOTNET_TOOLS%` que fija el bootstrap: `D:\Programs\dotnet-tools` en el PC
   de desarrollo, `<repo>\.dotnet-tools` en cualquier otro);
2. `dotnet publish -c Release -r win-x64 --self-contained` → `installer\publish\`;
3. `vpk pack` → `installer\releases\` (se vacía en cada ejecución).

Requiere que `NexusWorkspace.Desktop` referencie el paquete `Velopack` y llame a
`VelopackApp.Build().Run()` como primera línea de `Program.Main` (ya está hecho);
sin eso `vpk pack` aborta con *"Unable to verify VelopackApp is called"*.

## Qué produce (`installer/releases/`)

| Archivo | Para qué |
|---|---|
| `NexusWorkspace-win-Setup.exe` | **el instalador** que se entrega a los usuarios |
| `NexusWorkspace-<ver>-full.nupkg` | paquete completo de esa versión |
| `NexusWorkspace-<ver>-delta.nupkg` | parche desde la versión anterior (si existe) |
| `RELEASES` | índice que usa el actualizador |

Para una **release en GitHub**: sube `Setup.exe` + los `.nupkg` + `RELEASES`
como *assets* del tag `vX.Y.Z`.

## Datos del usuario

La app corre en modo normal (no portable): el workspace vive en
`%APPDATA%\NexusWorkspace\` (`nexus.db`, `files/`, `backups/`, `logs/`,
`settings.json`). **Reinstalar o actualizar no toca esa carpeta** — los datos
y las copias de seguridad se conservan.

> Modo portable: dejar un archivo `nexus.portable` junto al `.exe` hace que los
> datos vayan a `.\data\`. El instalador no lo incluye.

## Icono

`src/NexusWorkspace.Desktop/NexusWorkspace.Desktop.csproj` tiene `<ApplicationIcon>`
vacío. Para un icono propio: añade `app.ico` a ese proyecto y pon
`<ApplicationIcon>app.ico</ApplicationIcon>` + `--icon app.ico` en `vpk pack`.

## Actualizaciones automáticas (opcional, post-v1)

El paquete `Velopack` y `VelopackApp.Build().Run()` ya están integrados. Falta solo
comprobar actualizaciones al arrancar:
`new UpdateManager("<url de releases>").CheckForUpdatesAsync()` (p. ej. la URL de
*releases* de GitHub). La infraestructura de delta ya la genera este script.
