# Lista de comprobación de release (v1)

Objetivo: pasar `dev` → `main`, etiquetar `v1.0.0` y publicar el instalador.
Android queda para v2.

## 1. Verificación en verde (rama `dev`)

- [ ] `build.bat release` → **0 errores / 0 avisos**, migraciones al día.
- [ ] `dotnet test` → **todo verde** (79/79 a fecha de este documento).
- [ ] Arranque de la app: sin `[WRN]`/`[ERR]` en `logs/nexus-<fecha>.log`;
      la copia diaria automática se crea; el scheduler arranca.
- [ ] Recorrido manual rápido: crear proyecto → tarea → subtarea → acción rápida →
      seguimiento → adjunto → informe PDF → copia de seguridad → restaurar (reinicio).
- [ ] `docs/ROADMAP.md` y `docs/DATA-MODEL.md` reflejan el estado real.

## 2. Instalador

- [ ] `installer\pack.bat` → `installer\releases\NexusWorkspace-win-Setup.exe`.
- [ ] Instalar en una máquina/for perfil limpio: arranca, crea `%APPDATA%\NexusWorkspace`.
- [ ] Con datos existentes: reinstalar **no** borra `nexus.db` ni `backups/`.
- [ ] Desinstalar deja la carpeta de datos intacta.

## 3. CI (si aplica)

- [ ] Mover `docs/ci-build.yml` a `.github/workflows/build.yml`
      (`gh auth refresh -s workflow` y `git mv`, o pegarlo desde la web de GitHub).
- [ ] El workflow pasa en `dev`.

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
