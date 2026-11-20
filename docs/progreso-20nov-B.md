# Progreso 20-nov B (pendientes de docs/playtest-20nov.md sin Blender ni chat de arte)

Pedido completo: el /goal del 20-nov (Tony). Regla: nada se marca hecho sin verlo en el juego desde el jugador, con captura en
`docs/capturas/_20nov-B/`. Si no se pudo comprobar, se dice.

## Dónde está todo
- Carpeta principal: en `main`. **El chat de Blender trabaja ahí** (imageassets/blender): no hacer checkout ni restore ahí sin revisar.
- Integración y Unity: `.sesiones/s2` en la rama `integ/20nov-C` (tiene su Library). Una sola instancia de Unity.
- Subagentes Sonnet: worktrees dispersos (sin imageassets ni binarios LFS) en `Desktop/videojuego/wt20/T15xx-*`, sin Library: no corren Unity, dejan `Aplicar()` para -executeMethod.
- Al final: merge --no-ff de integ/20nov-C a main, push, builds Demo y Juego en la carpeta principal (uno por uno).

## Paso 0 (hecho)
- Cambios sin guardar de la carpeta principal en `tony/blender-20nov` (5 commits por tamaño: GitHub no acepta un push de más de 2 GB)
  y merge a main 601dd62 (subido). Fuera del commit: `Audio-20261031T070509Z-1-001.zip` (142 MB, GitHub no lo acepta),
  `Logs_v121.txt`, `__pycache__`, respaldos `.blend1` y `imageassets/blender/_REVISION_TONY/revision_personajes.blend`
  (140 MB y el chat de Blender lo sigue editando). Siguen en disco.
- Unity y Hub cerrados, sin lockfile.

## Tareas
| Tarea | Punto | Rama | Estado | Comprobado en juego |
| --- | --- | --- | --- | --- |
| T-1520 | 1 Inicio sin cuarto | tony/T-1520-sin-cuarto | pendiente | — |
| T-1521 | 2 Escalas de humanos, civil acostada | tony/T-1521-escalas | pendiente | — |
| T-1522 | 3 Armería mostradores y armero | tony/T-1522-armeria | pendiente | — |
| T-1523 | 4 Formas básicas → ED, nada flotando | tony/T-1523-sin-primitivas | pendiente | — |
| T-1524 | 4 Muro cerrado, puertas sólidas | tony/T-1524-muro | pendiente | — |
| T-1525 | 4 Costales CB02, agua sin salpicadura | tony/T-1525-costales-agua | pendiente | — |
| T-1526 | 5 Carros dentro del muro, patrulla, sonido | tony/T-1526-carros | pendiente | — |
| T-1527 | 6 Armas | tony/T-1527-armas | pendiente | — |
| T-1528 | 7 Enemigos | tony/T-1528-enemigos | pendiente | — |
| T-1529 | 8 Tienda e implantes | tony/T-1529-tienda | pendiente | — |
| Final | Builds, reporte-20nov-B, cerrar Unity | — | pendiente | — |

## Bitácora
- 20-nov: paso 0 hecho. integ/20nov-C creada en s2 desde main 601dd62.
- 20-nov: 10 subagentes lanzados (T-1520..T-1529) en wt20/. Compilación base de main en s2 corriendo.
