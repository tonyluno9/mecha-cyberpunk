# Progreso: animaciones revisadas en Blender (20 y 21-nov) y transiciones

Pedido: /goal de Tony (animaciones 21-nov). Regla: nada hecho sin verlo en Play, con captura o video en docs/capturas/_anim21nov/.
No tocar el contenido de los FBX de Blender: solo importación (.meta), controladores, overrides y código.

## Dónde está todo
- Integración y Unity: `.sesiones/s2`, rama `integ/anim21nov` (desde main 7362735). Una sola instancia de Unity.
- Subagentes: worktrees dispersos en `Desktop/videojuego/wt24/` (sin imageassets ni LFS); leen imageassets en la carpeta principal.
- Fuentes: `imageassets/blender/_PENDIENTE_BLENDER_2/_REVISION/BITACORA.md` (20-nov desde la línea ~763; 21-nov desde ~844), docs/tareas-unity-*-20nov.md.

## Paso 0 (hecho)
- Unity cerrado, sin lockfile. Entregas del chat de Blender en `tony/blender-anim-21nov` → main 7362735 (fuera por tamaño: Audio-*.zip y 4 .blend >100 MB de _REVISION_TONY; siguen en disco).

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| Música | Tony: el tema empalma solo; se quita el cruce y queda loop normal | hecho en código, falta build |
| T-1560 | Parte 1: importación clip por clip (avatar, escala, rango, raíz, loop, compresión), overrides de Tony, estados de clips sin estado (no acciones del jugador) | por lanzar |
| T-1561 | Parte 2: transiciones con lógica (capas, modos, armas, granada, daño/muerte, saltos, carro, interacciones, NPC) y estados de acciones del jugador | por lanzar |
| Final | Pruebas en Play con capturas, builds, reporte-anim-21nov.md | pendiente |

## Bitácora
