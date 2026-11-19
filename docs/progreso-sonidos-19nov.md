# Progreso sonidos 19-nov: integrar "3d sonidos"

Dirige Opus en `.sesiones/s2`, rama de integración `integ/sonidos-19nov`. Tareas hechas por subagentes Sonnet (brief: docs/brief-sonidos-19nov.md).
**Si se corta la sesión:** lee este archivo, `git branch -a | grep T-147` y sigue con "Qué sigue".

## Reglas
Una sola instancia de Unity (en s2, batch). Nada de PlayMode completo: pruebas por clase. No correr CreadorTodo. Nunca force push.
Merge --no-ff a main y push (autorizado por Tony). "3d sonidos" no va a git (.gitignore).

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1470 | Copiar y recortar WAV a Assets/Arte/Audio/<cat>/, importador, .gitignore | en curso |
| T-1471 | Catálogo: carpetas nuevas, IDs viejos -> archivos nuevos, encendidos, política de mezcla | en curso |
| T-1472 | Armas (disparo, recarga, sacar), impactos por material, explosiones | en curso |
| T-1473 | Pasos por piso, jugador (aterrizar, rodar, trepar, golpes) | en curso |
| T-1474 | UI, HUD, tienda, interacción, consumibles | en curso |
| T-1475 | Vehículos (motor con pitch, acelerón, frenos, choques, puertas, alarma, motos, camiones) | en curso |
| T-1476 | Policía (sirenas, radio, aviso de Nova) | en curso |
| Final | Integrar, compilar, pruebas por clase, builds Demo y Juego, reporte | pendiente |

## Bitácora
- Paso 0: no había Unity abierto; s2 pasada a main (d119ebf, ya con la sesión A) y lockfile borrado. Rama integ/sonidos-19nov.
  Se quitaron los 4 worktrees viejos de T-1440..T-1443 (sus ramas siguen). integ/zonas-19nov-B (zonas, sesión B) sigue sin merge, pendiente de su verificación.

## Qué sigue
1. Esperar a los 7 subagentes; integrar rama por rama en integ/sonidos-19nov; correr el importador de T-1470 y CreadorCatalogoSonidos.Crear.
