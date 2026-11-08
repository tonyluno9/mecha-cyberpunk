# Pendiente tras la limpieza del 8-nov-2026

Se dejó una sola carpeta. Esto es lo que quedó a medias y dónde está.

## T-749: clips de Blender en PJ01 (sesión de mecha-s1)

- **Rama:** `wip/T-749-pj01-clips` (subida). Sale de 9d48597; un commit encima.
- **Trae:** `ImportadorClipsPJ01.cs`, `PJ01/Clips` y `PJ01/ClipsReferencia` (FBX por LFS, con sus .meta), `Override_Jugador.overrideController`, el mapa `ClipsDeJugadorPJ01` en `ConstructorControladorBase.cs` y cuatro archivos de PlayMode en `Tests/PlayMode/Personaje/` (`ApuntarClipsPJ01T749Pruebas`, `RetargetPJ01T749Pruebas`, `MedidaPoseClipsPJ01`, `DiagPiesT749`).
- **Estado:** a medias, sin resultados de pruebas conocidos. No se corrió nada de esto en la limpieza. `DiagPiesT749` y `MedidaPoseClipsPJ01` parecen diagnósticos: decidir si se quedan.
- **Ojo:** el código cita `docs/pedido-blender-10nov.md`, que no existe; el que hay es `docs/pedido-blender-09nov.md`.
- Los FBX de origen siguen en disco, sin git: `imageassets/blender/pj01_clips/`.
- Descartado por ser ruido: seis `.fbx.meta` que solo cambiaban en redondeo (máximo 1.4e-7), la línea de SENTIS de ProjectSettings y `SceneTemplateSettings.json`.

## B7 (sesión de esta carpeta)

No se encontró rastro de B7: ni archivos de `Assets/` sin commit, ni ramas, ni commits, ni la palabra "B7" en docs/. Si había algo, no llegó a guardarse. Lo único sin commit en archivos con seguimiento fue lo de abajo.

- **Rama:** `wip/limpieza-docs-arte-08nov` (subida). Las secciones B5 y B6 en `docs/tareas-piezas-08nov.md` (no están en main), más `imageassets/PROMPTS_ASSETS.md` y `imageassets/generadas/registro.csv`.

## Archivos sin git que siguen en disco

No son ruido de pruebas y no se tocaron; viajan con la carpeta al renombrarla. No se subieron porque son de arte y fuentes, y `Audio-20261031T070509Z-1-001.zip` (142 MB) pasa del límite de GitHub.

- Raíz: `.agents/`, `.codex/`, `AGENTS.md`, `Audio-20261031T070509Z-1-001.zip`, `Font/`, `Logs_v121.txt`, `assetss/`
- `docs/pedido-arte-09nov-b.md`
- `imageassets/`: `animaciones/`, `blender/pj01_clips/`, `blender/pistola/*`, `generar_08nov_c.ps1`, `generar_09nov_b.ps1`, `lote_08nov_c.json`, `lote_09nov_b.json`, `_hoja_imp.png`, `__pycache__/`, los zip de `modelos3d/` y `paquetes/`, `modelos3d/armas/W01_Pistola/*.blend`, `modelos-nuevos/PC01*` y `PS01*`, `referencias/menus/`, `ui/menus/H33*` y `H34*`

## Stashes borrados

Se borraron los 11, como se pidió; ninguno era de las sesiones detenidas (el más nuevo es del 7-nov). Se pueden recuperar con `git stash store <hash>` hasta que git limpie los objetos sueltos:

| Stash | Hash |
| --- | --- |
| t729-wip | c5041edfe8a70ee03a43fa4eac64b5c2709fea9c |
| restos de corridas batch 07-nov | 3ecbeb154c123d1bc56613bc2c1327f4af1f8ce5 |
| WIP en T-706-aliado-carro | 15be689adb2886be2a308a664366aeb9fd29acde |
| ruido build demo 6-nov | 64534995a95472cbbf05e96663445aaa4665fe7e |
| WIP en T-684-escudo | f29c5f8e52e337381f745da5331354ca06296ef4 |
| ruido tras T-812b | baefd0abb51a879aafc610839ac27d4232a62c0d |
| ruido Unity s2n tras T-804 | 8fe00598840aafd910ee59dc01fe751d38abf1c6 |
| churn-editor-anim-noche | 919a28a8c8b48da414bac6484807cd1222526508 |
| t562-efectos-pruebas | 3c541505af50749cd5cf064285edfd638f56b979 |
| t561-efectos-pruebas | 59ccaa64acbed65f9ef7ed6aa06ed72e3ed4cebb |
| t550-efectos-unity | 33dc3bbda4abb71a3f027d04ae0f339d75837401 |

## Ramas locales que quedan (no fusionadas a main)

- `tony/core-pendiente-push-main`: su commit 538a9c2 (T-100, contratos `IGuardable`, `IInteractuable`, `IRenta` y `MotivoDinero.Carga`) **solo existe en esta máquina**. Decidir si se sube o se tira.
- Ya están en GitHub: `tony/T-199` a `T-203`, `tony/fase-6b-alan`, `tony/fase-7`.

## Sobra y no se pidió borrar

`.claude/worktrees/tony-turno-largo/` (2.9 GB, ignorada por git): restos de un worktree que git ya no registra, sin `.git`.

## Build Settings y builds

Los builds de Juego y Demo tenían la lista de escenas fija, sin leer Build Settings. Se agregó `Menu_Calles` a esa lista en `ConstruirDemo.cs`; sin ella el inicio se quedaba en las fotos. Build Settings queda con Inicio, Menu_Calles, CreacionPersonaje, Ciudad_Prologo y Flashback, pero los builds llevan solo Inicio, Menu_Calles y Ciudad_Prologo, como ya pasaba con las otras dos.
