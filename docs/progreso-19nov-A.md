# Progreso 19-nov, sesión A: look de las zonas nuevas igual a las referencias

Dirige Opus en la carpeta principal, rama `integ/look-19nov-A` (sale de origin/main ce7c4bd). Subagentes Sonnet, una rama
`tony/T-144x-…` por tarea (numeración 1441+ para no chocar con la sesión B). Brief común: docs/brief-19nov-A.md.
**Si se corta la sesión:** lee este archivo, `git log --oneline integ/look-19nov-A -15`, `git branch | grep T-144` y sigue con "Qué sigue".

## Reglas
- Un solo Unity en modo batch en esta carpeta. Sin PlayMode completo; pruebas por clase. No CreadorTodo. Escena < 100 MB.
- No tocar T1355_Cerro ni T1357_Puerto (sesión B). Míos: escena, luz, postproceso, materiales compartidos, Malecón, Autopista, cielo y fondo.
- Fotos: `Vista19b.Fotos` (9 tomas) -> docs/capturas/_vista19b/; copia por ronda en Logs/vista19b-<ronda>/.

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1441 | Look de zonas nuevas: piso mojado, charcos, sondas, postes con halo, neón con luz, contraste, niebla | pendiente |
| T-1442 | Aéreas: quitar lo que flota, fondo lejano bien desde cualquier altura, niebla más baja en altura | pendiente |
| T-1443 | Malecón como K11 y A11, con vida | pendiente |
| T-1444 | Autopista como K18 y A18, con vida | pendiente |
| — | Final: git pull (B), 9 fotos, builds Demo y Juego, reporte-19nov-A | pendiente |

## Bitácora
- 08:28 Paso 0: Unity y Hub cerrados, sin lockfile; Blender abierto (no se toca). Rama integ/look-19nov-A desde origin/main.
- 08:40 Vista19b.cs (fotos de las 9 tomas con marcas `Vista19b_<TOMA>`). Corrida r0 (foto base) en marcha.

## Qué sigue
- Ver la r0, lanzar T-1441..T-1444.
