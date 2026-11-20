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

# Segunda vuelta 20-nov (goal nuevo: nuevos-19nov y lo que faltó)

Rama de integración: `integ/20nov-B` en `.sesiones/s2`, sale de main e785e2a. Lo de la primera vuelta (T-1490..T-1507) ya está en main.
Lo de Blender 19N (pj01_clips, nova_clips, npc_clips, carros en metros) ya entró en T-1505..T-1507: no se repite.
Fuentes de modelos: `imageassets/modelos-nuevos/nuevos-19nov/` de la carpeta principal (no están en los worktrees).

| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1510 | PA01/PA02 patrullas nuevas (puertas, llantas, cristal), V22 nuevo (mejor variante), rines de policía V05/V06/V11, ninguna patrulla hundida | pendiente |
| T-1511 | CB01-CB04 cobertura: importar, poner en la ciudad, que Nova y SKULLS se agachen detrás | pendiente |
| T-1512 | Identificar SIN_NOMBRE_01-15 (ED03-ED12, MU01-MU03) y renombrar; ED01-ED12 en vez de edificios de primitivas por barrio | pendiente |
| T-1513 | Muro de la ciudad con MU01-MU03 (mismo trazo del muro que le gustó) | espera T-1512 (identificación) |
| T-1514 | Pruebas en rojo de la primera vuelta (maletín 45 cm, dron al borde del muro, SNAKES al inicio), cama/espejo sin cuarto | pendiente |
| T-1515 | PJ01: cobertura (quieto/asomarse/regresar), daño en 4 direcciones, manejar moto, escalones; velocidades del Animator = tabla | pendiente |
| T-1516 | NPCs: platicar, sentado en bancas, agacharse; perro E17 con su modelo de escala real y sus clips | pendiente |
| Final | Builds Demo y Juego (CyberCity.exe) en la carpeta principal, reporte, cerrar Unity | pendiente |

## Bitácora (segunda vuelta)
- 20-nov: paso 0. Unity cerrado, sin lockfile, 10 GB RAM, 119 GB disco. Blender abierto por otra sesión (no se toca; los subagentes usan Blender 5.2 en -b).
