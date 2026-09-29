# Lista de comprobación de release (v1)

Objetivo: pasar `dev` → `main`, etiquetar `v1.0.0` y publicar el instalador.
Android queda para v2.

> **v1.0.0 publicada el 2026-09-29** — repo público, `main` como rama por defecto,
> release con instalador y portable. Quedan pendientes las pruebas manuales de las
> secciones 1 y 2 (el usuario está probando la compilación de esa misma fecha).

## 1. Verificación en verde (rama `dev`)

- [x] Build 0/0, 82/82 tests (incluye recordatorios recurrentes) — verificado en este PC y en CI.
- [x] `docs/ROADMAP.md` y `docs/DATA-MODEL.md` reflejan el estado real (A–H, `docs/UX-REVIEW.md`,
      y los cambios post-pulido: Seguimientos ocultos, subtareas desplegables, calendario).
- [ ] **Recorrido manual en el PC de build**, tocando lo añadido tras A/B1: vincular
      persona/empresa↔proyecto/tarea (C), estado→seguimiento automático (D), "Registrar
      evento" + comunicaciones en la tarea (E), Actividad con filtros (F), página
      **Tareas** global (G), lista de subtareas fusionada (B2).
- [ ] Arranque de la app: sin `[WRN]`/`[ERR]` en `logs/nexus-<fecha>.log`;
      la copia diaria automática se crea; el scheduler arranca.

## 2. Instalador

- [ ] `build.bat installer` (o `installer\pack.bat`) → `installer\releases\NexusWorkspace-win-Setup.exe`,
      ya con el icono de la app.
- [ ] Instalar en un perfil limpio: arranca, crea `%APPDATA%\NexusWorkspace`, icono correcto
      en escritorio/menú Inicio.
- [ ] Con datos existentes: reinstalar **no** borra `nexus.db` ni `backups/`.
- [ ] Desinstalar deja la carpeta de datos intacta.

## 3. CI

- [x] `.github/workflows/build.yml` activo (restore + build + test en cada push/PR a `dev`/`main`).
- [x] El workflow pasa en `dev` (verificado dos veces, incl. tras limpiar avisos CA1873).

## 4. Merge y etiqueta

- [x] Hecho el 2026-09-29. `main` no existía aún, así que en vez de un merge se creó
      directamente desde la punta de `dev` (`5c7b38c`) — no había historia divergente que
      fusionar:

```bat
git checkout -b main
git push -u origin main
git tag -a v1.0.0 -m "NexusWorkspace v1.0.0"
git push origin v1.0.0
```

  `main` se puso además como rama por defecto del repo, y el repositorio se hizo público.

## 5. GitHub Release

- [x] Creada la release del tag `v1.0.0`: <https://github.com/porrii/NexusWorkspace/releases/tag/v1.0.0>
- [x] Adjuntos `NexusWorkspace-win-Setup.exe` y `NexusWorkspace-win-Portable.zip`. (No hay
      `*.nupkg`/`RELEASES` de Velopack generados en este PC — quedan pendientes si en el futuro
      se quiere activar el auto-update de Velopack; no bloquean la instalación normal.)
- [x] Notas con el resumen de funciones, requisitos (Windows 10+ x64, sin prerequisitos) y
      aviso de que los datos son locales y offline.

## 6. Post-release

- [x] Subido `<Version>` en `Directory.Build.props` a `1.1.0-dev`.
- [ ] Abrir seguimiento para v2: **Fase 8 · Android** y los stubs
      (Sync, IA local, OCR, integraciones).
