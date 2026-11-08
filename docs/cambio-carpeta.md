# Cambio de carpeta: de mecha-cyberpunk a CyberCity

Ruta nueva: `C:\Users\Angel Perez\Desktop\videojuego\CyberCity`. **Todavía no se cambió ninguna ruta**: esto es la lista.
Búsqueda del 8-nov-2026 de `mecha-cyberpunk` y `mecha-s1` en docs/, herramientas/, .claude/ y los scripts (`.ps1`, `.sh`, `.py`, `.cs`, `.json`). En herramientas/ y en .claude/ no hay ninguna.

## 1. Sí cambiar: dejan de funcionar con la carpeta nueva

Scripts de imágenes, línea `Set-Location` con la ruta absoluta; cambiarla por `C:\Users\Angel Perez\Desktop\videojuego\CyberCity`:

| Archivo | Línea |
| --- | --- |
| `imageassets/generar_06nov.ps1` | 5 |
| `imageassets/generar_06nov_b.ps1` | 4 |
| `imageassets/generar_07nov.ps1` | 4 |
| `imageassets/generar_07nov_b.ps1` | 5 |
| `imageassets/generar_08nov.ps1` | 4 |
| `imageassets/generar_08nov_c.ps1` | 4 |
| `imageassets/generar_09nov.ps1` | 4 |
| `imageassets/generar_09nov_b.ps1` | 4 |
| `imageassets/generar_logo.ps1` | 4 |
| `imageassets/generar_logo2.ps1` | 4 |
| `imageassets/generar_piezas.ps1` | 4 |

`generar_08nov_c.ps1` y `generar_09nov_b.ps1` no están en git: solo existen en disco.

Código que busca las imágenes en una carpeta hermana (solo servía desde el worktree mecha-s1). Con la sesión 2 en `.sesiones/s1`, la carpeta principal queda en `../../`, así que la ruta nueva sería `../../imageassets/...`:

| Archivo | Línea | Hoy |
| --- | --- | --- |
| `Assets/Arte/Editor/ImportarS1n.cs` | 25 (comentarios en 17 y 24) | `../mecha-cyberpunk/imageassets/` |
| `Assets/_Proyecto/UI/Editor/ImportadorGraficosH10.cs` | 49 | `../mecha-cyberpunk/imageassets/ui/hud` |

Documento vigente con la carpeta del proyecto:

| Archivo | Línea |
| --- | --- |
| `docs/TRASPASO.md` | 37 y 161 |

## 2. Solo si se vuelven a usar: pedidos y planes con la ruta completa

Son registros de trabajo ya hecho; no afectan a nada si se dejan.

- `docs/pedido-arte-06nov.md`, `pedido-arte-06nov-b.md`, `pedido-arte-07nov.md`, `pedido-arte-07nov-b.md`, `pedido-arte-07nov-c.md`, `pedido-arte-08nov.md`
- `docs/plan-noche.md` (58), `docs/plan-noche-06nov-s2.md`

## 3. No cambiar

- **URL del repo en GitHub** (`github.com/tonyluno9/mecha-cyberpunk`): `docs/ARRANQUE.md` 68-69 y `docs/TRASPASO.md` 36, 148, 149 y 197. El repo no cambia de nombre.
- **Historia** (qué pasó en cada sesión): `docs/reporte-*`, `docs/playtest-*`, `docs/plan-*-s2*`, `docs/sesion-A.md`, `docs/sesion-B.md`, `docs/meter-assets-06nov.md`, `docs/decisiones-pendientes-tony.md`, `docs/estado-alan.md`, `docs/revision-alan.md`, `docs/tareas/hechas/T-678-importador-s1.md`.
- `imageassets/lote_escenas.json` y `lote_ui.json`: la mención es un texto descriptivo. `Assets/Arte/Fuentes~/bar/bar2.py`: es una ruta temporal de la máquina de Alan.

## 4. Fuera del repo, después de renombrar

- **Unity Hub**: la entrada del proyecto apunta a la carpeta vieja; hay que quitarla y agregar `CyberCity`.
- **Claude Code**: guarda su memoria por ruta de carpeta, así que en `CyberCity` empieza sin la memoria de `mecha-cyberpunk`.
