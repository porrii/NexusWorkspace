# Lista de comprobación de release (v1)

Objetivo: pasar `dev` → `main`, etiquetar `v1.0.0` y publicar el instalador.
Android queda para v2.

## 1. Verificación en verde (rama `dev`)

- [x] Build 0/0, 79/79 tests, en cada bloque desde A hasta H (parcial) — verificado en este PC.
- [x] `docs/ROADMAP.md` y `docs/DATA-MODEL.md` reflejan el estado real (A–H, `docs/UX-REVIEW.md`).
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

```bat
git checkout main
git merge --no-ff dev -m "Release v1.0.0"
git tag -a v1.0.0 -m "NexusWorkspace v1.0.0"
git push origin main --tags
```

## 5. GitHub Release

- [ ] Crear la release del tag `v1.0.0`.
- [ ] Adjuntar `NexusWorkspace-win-Setup.exe`, los `*.nupkg` y `RELEASES`.
- [ ] Notas: resumen de fases 0–7 + 6b (ver `docs/ROADMAP.md`), requisitos
      (Windows 10+ x64, sin prerequisitos), y que los datos son locales y offline.

## 6. Post-release

- [ ] Subir `<Version>` en `Directory.Build.props` a la siguiente (p. ej. `1.1.0-dev`).
- [ ] Abrir seguimiento para v2: **Fase 8 · Android** y los stubs
      (Sync, IA local, OCR, integraciones).
