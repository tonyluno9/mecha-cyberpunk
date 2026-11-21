# Progreso 21-nov B: animaciones y decisiones de la revisión de Blender

Pedido: /goal de Tony (21-nov B). Fuentes: docs/tareas-unity-{locomocion,alturas,robots-nova,vehiculos}-20nov.md (sección "Falta (Unity)"),
imageassets/blender/_REVISION_TONY/ANIMACIONES_FALTAN.md, REVISION.md y la BITÁCORA.
Regla: nada hecho sin verlo en Play, captura en docs/capturas/_21nov-b/. No editar clips ni FBX de Blender; no tocar PROMPTS_ASSETS.md ni registro.csv.

## Dónde está todo
- Integración y Unity: `.sesiones/s2`, rama `integ/21nov-b` (desde main 82f0bac). Una sola instancia de Unity.
- Subagentes: worktrees dispersos en `Desktop/videojuego/wt23/` (sin imageassets ni LFS); leen imageassets en la carpeta principal.
- Base: T-1540 (21-nov) ya conectó salto corriendo/rodada, multiplicadores RapidezSaltoInicio/Aterrizaje, 6 borradores PJ01, daños PJ01 en 7 papeles no jugador, Skull_Burla, E15/E16_Muerte.

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1550 | Locomoción y salto PJ01 (CorrerRapido, velocidades, mezcla, retraso 3 frames, aterrizajes, pisadas) | lanzada |
| T-1551 | Combate y eventos: clips aprobados sin conectar (daños del jugador, recargas, granada, interacciones, implantes, NPC, pandilleros, Nova caída) | lanzada |
| T-1552 | Alturas: tabla de factores, EscalasNova/EscalasHumanos, Tino, cápsulas y NavMesh E14/E16 | espera worktree |
| T-1553 | Robots Nova: avatar E18, clips E18, jetpack y puños, E16 escudo y cañón, E15 W10, mover clips a su carpeta | espera worktree |
| T-1554 | Vehículos: bisagra cofre PA01/PA02/V22, V16–V19, hélices, carros viejos → Carro_Vxx, quitar V10/V11 | espera worktree |
| Final | Capturas en Play, pruebas por clase, builds, reporte-21nov-b.md | pendiente |

## Bitácora
- Paso 0: Unity cerrado, sin lockfile, main 82f0bac (lo de Blender ya estaba en main; solo cambiaron docs).
