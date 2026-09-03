# NexusWorkspace

Espacio de trabajo técnico personal, **local-first** y multiplataforma
(Windows ahora, Android más adelante). Centraliza proyectos, tareas, incidencias,
seguimientos, reuniones, comunicaciones, personas, empresas y documentación,
**conservando todo el histórico de forma inmutable**.

> Principio fundamental: **nunca perder información.** Una tarea, incidencia o
> proyecto finalizado no desaparece — se archiva y permanece localizable durante años.

## Estado

En desarrollo. Fase 0 (cimientos) + Fase 1 (Proyectos · Tareas · Historial) en curso.
Ver `docs/ROADMAP.md`.

## Tecnología

C# · .NET 9 · Avalonia 11 (MVVM) · SQLite + EF Core (FTS5) · CommunityToolkit.Mvvm ·
FluentAvalonia · Material.Icons · Serilog · LiveCharts2 · xUnit.

100 % offline. Sin cuenta. Sin servicios en la nube obligatorios.

## Documentación

| Documento | Contenido |
|---|---|
| [`docs/BUILD.md`](docs/BUILD.md) | Qué instalar en el PC de compilación y cómo compilar / ejecutar / publicar |
| [`docs/FIRST-COMPILE.md`](docs/FIRST-COMPILE.md) | Guía para la primera compilación: secuencia, orden de errores, puntos frágiles |
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Arquitectura, capas, almacenamiento, historial, backups, estrategia multiplataforma |
| [`docs/DATA-MODEL.md`](docs/DATA-MODEL.md) | Entidades, relaciones, enums de estado, búsqueda FTS5 |
| [`docs/ROADMAP.md`](docs/ROADMAP.md) | Fases de implementación |

Documento visual de arquitectura y wireframes:
<https://claude.ai/code/artifact/b9e28e8b-0851-4844-916b-385f9f3b03a9>

## Compilar

Vía rápida (comprueba/instala .NET 9, restaura, migra, compila, testea y deja logs):

```bat
build.bat
```

`build.bat release` · `build.bat run` · `build.bat clean` · `build.bat notest`.
Los logs quedan en `build-logs\<fecha_hora>\` con un `SUMMARY.txt`.

Manual:

```powershell
dotnet restore
dotnet run --project src/NexusWorkspace.Desktop
dotnet test
```

Requisitos y detalles: [`docs/BUILD.md`](docs/BUILD.md) · [`docs/FIRST-COMPILE.md`](docs/FIRST-COMPILE.md).
