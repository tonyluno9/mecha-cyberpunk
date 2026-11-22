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
| T-1560 | Parte 1: importación clip por clip (avatar, escala, rango, raíz, loop, compresión), overrides de Tony, estados de clips sin estado (no acciones del jugador) | lanzada (wt24) |
| T-1561 | Parte 2: transiciones con lógica (capas, modos, armas, granada, daño/muerte, saltos, carro, interacciones, NPC) y estados de acciones del jugador | lanzada (wt24) |
| Final | Pruebas en Play con capturas, builds, reporte-anim-21nov.md | pendiente |

## Bitácora
- Lanzadas T-1560 (importación/overrides/estados NPC-robots) y T-1561 (transiciones/estados de acciones del jugador) en wt24/.
- T-1561 entregada (0912fce) e integrada; espera a T-1560 para aplicar (importación primero).
- T-1560 y T-1561 aplicadas (92e5530). Causa principal de la deformación: los 6 clips reemplazados el 21-nov (raíz PJ01_REV) no importaban (el .meta viejo traía otro esqueleto) y ~70 clips nuevos sin .meta tomaban un cuadro cualquiera como pose. Diagnósticos T-1560/T-1551/T-1550 en 0.
- EditMode de animación: 496/497 (falta solo el avatar E18 a -20°: Enforce T-Pose a mano). PlayMode: TransicionesT1561 11/11, ControladorBaseVisual 15/15, ClipsNpcNovaT1506 10/10, SaltoLocomocionT1550 5/5, RobotsNovaT1553 6/6, EnemigosAnim2 5/6 (Guardia_Muerte, ya fallaba antes). RobarCarroSacarConductorT1335 0/1: la prueba no ve el clip del conductor (por revisar en Play).
- Capturas: pedida al subagente de T-1561 una clase PlayMode que guarda tiras de cuadros por escenario en docs/capturas/_anim21nov/.
- Builds Demo y Juego hechos; música con loop normal comprobada en el build. Reporte en docs/reporte-anim-21nov.md.
