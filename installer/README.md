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

El script (reutilizable, todo en `D:\Programs`, nada en `C:`):

1. instala la herramienta `vpk` si falta (`dotnet tool`, en `D:\Programs\dotnet-tools`);
2. `dotnet publish -c Release -r win-x64 --self-contained` → `installer\publish\`;
3. `vpk pack` → `installer\releases\`.

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

Añadir el paquete `Velopack` al head Desktop y, al arrancar,
`new UpdateManager("<url de releases>").CheckForUpdatesAsync()`. La infraestructura
de delta ya la genera este script.
