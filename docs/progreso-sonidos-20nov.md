# Progreso sonidos 20-nov (integrar "3d sonidos" completo)

Pedido: el /goal de sonidos del 20-nov (Tony). Base: integración del 19-nov (docs/reporte-sonidos-19nov.md, 138 WAV).
Fuente: `3d sonidos/` en la carpeta principal (ignorada por git; 229 sonidos útiles + 00_*). Leer LEEME_SONIDOS.md (decisiones del 20-nov).

## Dónde está todo
- Carpeta principal: en main; **el chat de Blender trabaja ahí** (cambia FBX de PJ01 en Assets): no hacer checkout/restore ahí.
- Integración y Unity: `.sesiones/s2`, rama `integ/sonidos-20nov` (desde main 8f97866). Una sola instancia de Unity.
- Subagentes: worktrees dispersos en `Desktop/videojuego/wt21/` (sin imageassets ni binarios LFS); leen `3d sonidos/` de la carpeta principal.
- Al final: merge --no-ff a main, push, builds Demo y Juego en la carpeta principal, reporte docs/reporte-sonidos-20nov.md.

## Plan
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1530 | Comparar carpeta vs Assets/Arte/Audio, copiar/recortar/convertir, carpetas nuevas 11..17 y 00_*, importador, ids-20nov.csv, sonidos-faltan.md | pendiente |
| T-1531 | Catálogo + MapaSonidos + ReglaMezclaSonidos para todos los IDs nuevos (con pruebas) | después de T-1530 |
| T-1532 | Armas, impactos, explosiones, plasma W07, rueda con cámara lenta real | después de T-1531 |
| T-1533 | Nova, robots, drones, NPCs, policía | después de T-1531 |
| T-1534 | Vehículos y pasos por evento de animación según BITACORA | después de T-1531 |
| T-1535 | Ambiente por barrio con transición, clima, interiores, objetos, casino, misiones/HUD, EXO/implantes, radio | después de T-1531 |
| Final | Pruebas por clase, merge, builds, reporte | — |

## Bitácora
- 20-nov tarde: paso 0 (sin Unity abierto, sin lockfile; main 8f97866). integ/sonidos-20nov creada en s2.
