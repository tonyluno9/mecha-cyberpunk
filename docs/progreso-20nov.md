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
| Inventario | Qué modelos faltan de importar o de usar | pendiente |
| T-1490 | Cámara casi sin temblor; combo de 2 golpes; enemigos con más vida contra golpes | pendiente |
| T-1491 | Armas: alcance y daño de todas, trazos, mira/arco de granada, sonido cámara lenta en la rueda | pendiente |
| T-1492 | HUD: daño de drones y de dónde disparan, vida baja antes, curarse en persecución, prótesis clic/doble clic, 2 ranuras E/C | pendiente |
| T-1493 | Animaciones que bloquean el movimiento (bajar de moto y similares) | pendiente |
| T-1494 | Nova y robots: detectar de lejos, buscar, usar arma, cubrirse agachados, más vida; perro robot; drones sin atravesar muros | pendiente |
| T-1495 | SKULLS con tope por lugar; NPCs reaccionan a disparos y mueren con animación; meseras; hacker sentado | pendiente |
| T-1496 | Menú con poca niebla; juego sin cuarto (tutorial y despertar intactos) | pendiente |
| T-1497.. | Modelos: Nova a escala y W10, drones y naves, V11, NPCs, tragamonedas, maletín, parque | pendiente |
| T-15xx | Ciudad: flotando/hundido, texturas de calles, primitivos, armería, charcos | pendiente |
| Blender | Revisar BITACORA y nuevos-19nov e integrar lo que llegue | pendiente |
| Final | Builds, reporte, cerrar Unity | pendiente |

## Bitácora
- 20-nov: paso 0 (Unity cerrado, sin lockfile, main f35707b, 11 GB RAM libres, 153 GB disco; Blender abierto).
