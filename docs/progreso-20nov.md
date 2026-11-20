# Progreso 20-nov: modelos sin integrar y pendientes del playtest

Pedido de Tony (ver docs/playtest-19nov.md). Merge --no-ff a main y push autorizados. Una sola instancia de Unity; Blender trabajando a la vez:
nada de PlayMode completo (pruebas por clase), no correr CreadorTodo, nunca force push. Solo los 4 barrios (nada de las zonas quitadas).
"Le gustó" no se toca. Nada inventado con primitivos. Al final: builds Demo y Juego en la carpeta principal, uno por uno, docs/reporte-20nov.md, cerrar Unity.

## Dónde se trabaja
- Integración y Unity: `.sesiones/s2` (tiene Library), rama `integ/20nov` (sale de main f35707b).
- Subagentes Sonnet en worktrees propios (no compilan): una rama `tony/T-xxx` por tarea. Se integran en `integ/20nov`, se compilan,
  se corren sus métodos de editor y sus pruebas por clase en s2.
- La carpeta principal está en `integ/look-19nov-A` (ya en main) con cambios de otra sesión: no se toca; los builds se copian ahí al final.

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| Inventario | Qué modelos faltan de importar o de usar | hecho ($CLAUDE_JOB_DIR/tmp/inventario-modelos.md) |
| T-1490 | Cámara casi sin temblor; combo de 2 golpes; enemigos con más vida contra golpes | integrada y probada |
| T-1491 | Armas: alcance y daño de todas, trazos, mira/arco de granada, sonido cámara lenta en la rueda | integrada y probada |
| T-1492 | HUD: daño de drones, vida baja 40 %, curarse en persecución, prótesis clic/doble clic, ranuras E/C | integrada y probada |
| T-1493 | Animaciones que bloquean el movimiento | integrada y probada |
| T-1494 | Nova y robots, perro robot, drones sin atravesar muros | integrada y probada |
| T-1495 | SKULLS con tope; civiles; meseras; hacker sentado | integrada y probada |
| T-1496 | Menú con poca niebla; juego sin cuarto | integrada; pendiente HistoriaDemo (8 SNAKES antes de Libre) |
| T-1497..T-1499 | Nova a escala y W10; drones y naves (T-1498); maletín, parque, V11, NPCs | T-1498 y T-1499 integradas con métodos corridos; T-1497 métodos corriendo |
| T-1501..T-1503 | Barrido (50 arreglos, 205 a revisar); 52 de 95 edificios de las afueras con modelos; armería y charcos | integradas, fotos en docs/capturas/_20nov |
| T-1505..T-1507 / Blender | Locomoción de Blender (T-1505), animaciones de NPC/Nova/PJ01 (T-1506, en curso), carros revisados (T-1507) | métodos corriendo |
| Final | Builds, reporte, cerrar Unity | pendiente |

## Bitácora
- 20-nov: paso 0 (Unity cerrado, sin lockfile, main f35707b, 11 GB RAM libres, 153 GB disco; Blender abierto).
- 20-nov: T-1490..T-1496 integradas en integ/20nov (cf6ecc0), métodos de editor corridos, pruebas por clase. Segunda tanda lanzada (T-1497..T-1503, T-1505).
- 20-nov madrugada: T-1498, T-1499, T-1501, T-1502, T-1503 integradas (b8edbcb). T-1497, T-1505, T-1507 mezcladas; métodos en curso. T-1506 (animaciones de Blender B y C) en curso. No se corre ConfigurarEscaleras de T-1505: los clips de Blender son de escalones y AnimacionEscalera es la escalera de mano.
