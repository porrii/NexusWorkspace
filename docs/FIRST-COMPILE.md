# Primera compilación — guía rápida

El código de las Fases 0–2 está escrito pero **nunca se ha compilado**. Esta guía
es para la primera sesión de build. Objetivo: llegar a `dotnet build` + `dotnet test`
en verde lo antes posible.

## 0. Requisitos

Ver [`BUILD.md`](BUILD.md). Resumen: **.NET 9 SDK**. Nada más para escritorio.

## 1. La vía rápida: `build.bat`

En la raíz del repo hay un script reutilizable que hace **todo**: comprueba/instala
.NET 9 SDK en `D:\Archivos de programa\dotnet` (portable, sin admin), instala
`dotnet-ef`, restaura, crea la migración `Initial` si no existe, compila, ejecuta
los tests y deja los logs en `build-logs\<fecha_hora>\` con un `SUMMARY.txt`.

```bat
git pull
git switch dev
build.bat
```

Otras formas: `build.bat release` · `build.bat run` (lanza la app) ·
`build.bat clean` · `build.bat notest` · se combinan (`build.bat release run`).

Cuando termine, **envíame la carpeta `build-logs\<fecha_hora>\`** (o al menos
`SUMMARY.txt` + los `.log` marcados como FALLO).

## 2. La vía manual (equivalente)

```powershell
cd <repo>
git pull ; git switch dev

dotnet restore
dotnet ef migrations add Initial `
  --project src/NexusWorkspace.Infrastructure `
  --startup-project src/NexusWorkspace.Desktop `
  --output-dir Persistence/Migrations
dotnet build -c Debug
dotnet test
dotnet run --project src/NexusWorkspace.Desktop
```

> Si no tienes `dotnet-ef`: `dotnet tool install --global dotnet-ef --version 9.*`.
> La app arranca aunque no crees la migración (cae a `EnsureCreated`), pero es
> mejor tenerla.

## 3. Orden para resolver errores de compilación

Compila proyecto a proyecto, de abajo a arriba. Así los errores no se solapan:

```powershell
dotnet build src/NexusWorkspace.Domain
dotnet build src/NexusWorkspace.Application
dotnet build src/NexusWorkspace.Infrastructure
dotnet build src/NexusWorkspace.UI
dotnet build src/NexusWorkspace.Desktop
dotnet build tests/NexusWorkspace.Tests
```

`Domain` y `Application` son los de menor riesgo (sin dependencias raras). Si esos
dos compilan limpios, los patrones base (entidades, `Result<T>`, servicios,
sobrecargas de `IUnitOfWorkRunner`, generadores de CommunityToolkit) son correctos.

## 4. Puntos frágiles conocidos (y cómo salir del paso)

| Archivo / zona | Posible problema | Salida rápida |
|---|---|---|
| `src/NexusWorkspace.Desktop/Platform/WindowsGlobalHotkeyService.cs` | El espacio de nombres de `Win32Properties` / `CustomWndProcHookCallback` puede variar entre versiones de Avalonia. | Ajustar el `using` (`Avalonia.Controls` ↔ `Avalonia.Win32`). Si sigue fallando: borra este archivo **y** la línea `services.AddSingleton<IGlobalHotkeyService, WindowsGlobalHotkeyService>();` de `Program.cs`. El atajo `Ctrl+Shift+Espacio` seguirá funcionando con la app enfocada (hay un `KeyBinding` en la ventana principal). |
| `src/NexusWorkspace.UI/App.axaml` | `<materialIcons:MaterialIconStyles />` puede no existir en la versión instalada de Material.Icons.Avalonia. | Sustituir por `<StyleInclude Source="avares://Material.Icons.Avalonia/App.axaml" />`, o quitar la línea (el control `MaterialIcon` suele traer estilo por defecto). |
| `Directory.Packages.props` | Alguna versión NuGet fijada puede no existir tal cual (p. ej. `LiveChartsCore…rc5.4`, `Avalonia 11.2.3`, `FluentAvaloniaUI 2.2.0`). | Ajustar a la última disponible (`dotnet list package --outdated` ayuda) y `dotnet restore`. LiveCharts2 aún no se usa en Fase 0–2; si molesta, puedes bajar su `PackageVersion` a cualquiera que resuelva. |
| XAML con *bindings* por reflexión | No hay `x:DataType`, así que los errores de binding salen **en ejecución** como avisos en consola, no al compilar. | Arrancar la app, mirar la consola/log, corregir el `{Binding …}` señalado. No bloquean el build. |
| Migración EF | `dotnet ef` podría quejarse de la conversión global de `Guid`/enum o de la tabla FTS. La tabla `SearchIndex` **no** está en el modelo EF (se crea con SQL en `DatabaseInitializer`), así que **no** debe aparecer en la migración. | Si aparece algo raro de `SearchIndex` en la migración generada, bórralo del archivo de migración. |

## 5. Qué debería verse al arrancar

1. Ventana **NexusWorkspace** con barra lateral (Dashboard · Inbox · Proyectos · … · Configuración) y botón de tema.
2. `%APPDATA%\NexusWorkspace\` creado con `nexus.db`, `logs\`, `settings.json`.
3. Dashboard vacío con saludo y widgets. **Configuración → Cargar datos de demostración** llena un proyecto de ejemplo con tareas, subtareas, checklist, comentarios e historial.
4. `Ctrl+K` abre el Command Palette · `Ctrl+F` la búsqueda · `Ctrl+Shift+Espacio` la captura rápida.

## 6. Cuando esté en verde

Comparte el resultado — lo más cómodo es la carpeta `build-logs\<fecha_hora>\` que
genera `build.bat`. Con eso ajusto lo que haga falta y sigo con la **Fase 3**
(Seguimientos + Recordatorios + Notificaciones + Calendario) sobre una base ya validada.
