# Compilar NexusWorkspace en el PC de desarrollo

Este documento describe **qué necesita el equipo donde compilas** y **cómo
compilar, ejecutar y publicar** la aplicación.

> **Atajo:** `build.bat` (en la raíz) se encarga de todo — resuelve un .NET 9 SDK
> compatible (usa el del equipo o instala una copia privada en `.dotnet\` sin admin),
> restaura, crea la 1ª migración si falta, compila, testea y deja logs en
> `build-logs\`. `build.bat installer` genera además el instalador. El resto de este
> documento es la vía manual y los requisitos.

---

## 1. Requisitos para compilar la aplicación de escritorio (Windows)

| Componente | Versión | Cómo instalar |
|---|---|---|
| **.NET SDK** | **9.0.x** (cualquier feature band ≥ 9.0.100) | `winget install Microsoft.DotNet.SDK.9` · o el instalador de <https://dotnet.microsoft.com/download/dotnet/9.0> |
| **Git** | cualquiera reciente | `winget install Git.Git` |
| IDE (opcional pero recomendado) | — | Visual Studio 2022 **17.12+** (carga de trabajo *.NET desktop*), **o** JetBrains Rider 2024.3+, **o** VS Code + extensión *C# Dev Kit* |

Nada más. Avalonia de escritorio **no necesita workloads** de `dotnet workload`.

### Comprobación rápida

```powershell
dotnet --info          # debe listar un SDK 9.0.x y el runtime 9.0.x
```

Si `dotnet --list-sdks` no muestra ningún 9.0.x, el `global.json` de la raíz hará
que `dotnet` falle con un mensaje claro pidiendo esa versión.

---

## 2. Requisitos **adicionales** para la app Android (solo a partir de la Fase 8)

No hacen falta todavía. Cuando toque:

| Componente | Versión | Notas |
|---|---|---|
| Workload de Android | — | `dotnet workload install android` |
| **JDK** | **17** (Temurin o Microsoft OpenJDK) | Ya presente en tu equipo actual: `D:\Programs\Java\jdk-17.0.20+8` |
| **Android SDK** | Platform API **34/35**, build-tools, platform-tools | Ya presente: `D:\Programs\Android\Sdk` |
| Variables de entorno | `ANDROID_HOME`, `JAVA_HOME` | Apuntar a las rutas anteriores |

```powershell
dotnet workload install android
# Aceptar licencias del SDK de Android si se solicita:
& "$env:ANDROID_HOME\cmdline-tools\latest\bin\sdkmanager.bat" --licenses
```

---

## 3. Compilar y ejecutar

Desde la raíz del repositorio (`NexusWorkspace/`):

```powershell
dotnet restore                                  # descarga los paquetes NuGet (versiones fijadas en Directory.Packages.props)
dotnet build -c Debug                           # compila toda la solución
dotnet run --project src/NexusWorkspace.Desktop # arranca la app de escritorio
```

Compilación de release:

```powershell
dotnet build -c Release
```

---

## 4. Ejecutar los tests

```powershell
dotnet test
```

Con cobertura:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

---

## 5. Base de datos y migraciones EF Core

La app **aplica automáticamente las migraciones pendientes al arrancar**, creando
antes un backup (ver `docs/ARCHITECTURE.md` §3). Para trabajar con migraciones a mano:

```powershell
# Una sola vez por PC:
dotnet tool install --global dotnet-ef --version 9.*

# Crear una migración nueva (proyecto de persistencia = Infrastructure, host = Desktop):
dotnet ef migrations add NombreDeLaMigracion `
  --project src/NexusWorkspace.Infrastructure `
  --startup-project src/NexusWorkspace.Desktop `
  --output-dir Persistence/Migrations

# Aplicarla manualmente (normalmente no hace falta, lo hace la app):
dotnet ef database update `
  --project src/NexusWorkspace.Infrastructure `
  --startup-project src/NexusWorkspace.Desktop
```

> El proyecto `Infrastructure` incluye un `NexusDbContextFactory` (design-time)
> para que estos comandos funcionen sin arrancar la UI.

---

## 6. Publicar un ejecutable distribuible (Windows)

```powershell
dotnet publish src/NexusWorkspace.Desktop -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true

# Salida: src/NexusWorkspace.Desktop/bin/Release/net9.0/win-x64/publish/
```

- `--self-contained true` → no requiere .NET instalado en el equipo destino.
- Para un paquete más pequeño que sí exige .NET 9 en destino: `--self-contained false`.
- El instalador profesional (Inno Setup / Velopack) llega en la fase Post-MVP; ver `docs/ROADMAP.md`.

---

## 7. Dónde guarda los datos la aplicación

| Modo | Ubicación |
|---|---|
| Normal | `%APPDATA%\NexusWorkspace\` (`nexus.db`, `files\`, `backups\`, `logs\`, `settings.json`) |
| Portable | `.\data\` junto al ejecutable, si existe un archivo vacío llamado `nexus.portable` al lado del `.exe` |

Para empezar de cero: cerrar la app y borrar la carpeta `%APPDATA%\NexusWorkspace\`
(o `.\data\`). La app la vuelve a crear vacía en el siguiente arranque.

---

## 8. Problemas frecuentes

| Síntoma | Causa / solución |
|---|---|
| `The current .NET SDK does not support targeting .NET 9.0` | Falta el SDK 9. Instálalo (sección 1). |
| `NU1101: Unable to find package …` | Falta la fuente NuGet oficial. `dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org` |
| Error de versión en un paquete al restaurar | Ajusta la versión en `Directory.Packages.props` a una disponible y vuelve a `dotnet restore`. |
| `LiveChartsCore … rc` no resuelve | Es un paquete en release candidate; fija la última `2.0.0-rc*` publicada en `Directory.Packages.props`. |
| La app no arranca por SkiaSharp en Linux | Instala `libfontconfig1` / `libice6` / `libsm6` (solo relevante si pruebas en Linux). |
| Avalonia previewer no carga en el IDE | Es solo el previsualizador; `dotnet run` funciona igual. |

---

## 9. Estructura de la solución

```
NexusWorkspace/
├─ NexusWorkspace.sln
├─ global.json                 · fija .NET SDK 9
├─ Directory.Build.props       · nullable, analyzers, langversion (común a todos)
├─ Directory.Packages.props    · versiones NuGet centralizadas
├─ src/
│  ├─ NexusWorkspace.Domain          · entidades y reglas puras (sin dependencias)
│  ├─ NexusWorkspace.Application     · casos de uso + puertos (interfaces)
│  ├─ NexusWorkspace.Infrastructure  · EF Core/SQLite, archivos, backups, plataforma
│  ├─ NexusWorkspace.UI              · Avalonia (vistas + ViewModels), compartido
│  ├─ NexusWorkspace.Desktop         · head Windows (arranque + servicios de plataforma)
│  └─ NexusWorkspace.Android         · head Android (se añade en la Fase 8)
└─ tests/
   └─ NexusWorkspace.Tests           · xUnit (unit + integración SQLite)
```
