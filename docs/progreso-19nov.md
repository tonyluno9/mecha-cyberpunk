# Progreso 19-nov (noche): ciudad idéntica a las referencias

Dirige Opus. Las tareas las hacen subagentes Sonnet, una rama `tony/T-xxxx-…` por tarea. Se integran en `integ/ciudad-19nov`
(carpeta principal, que tiene la única Library) y de ahí van a main con merge --no-ff y push.
**Si se corta la sesión:** lee este archivo, `git log --oneline integ/ciudad-19nov -15` y `git branch | grep T-13[89]` y sigue con lo
que diga "Qué sigue".

## Reglas de esta noche
- Un solo Unity, en modo batch y en la carpeta principal. Nada de PlayMode completo: pruebas por clase. Blender sigue abierto.
- No correr CreadorTodo. Cada zona es un prefab en `Assets/Arte/Mundo/Zonas/`; la escena debe quedar en menos de 100 MB.
- Mandan las referencias de `imageassets/referencias/look/`: K11–K18, A11–A18 y M12.

## Paso 0 (02:00–02:20) ✔
- A las 02:00 Unity y Unity Hub ya estaban cerrados y no había lockfile. Blender está abierto (con blender-mcp).
- `git fetch`: origin/main (cbd8ad5) ya está contenida en integ/bugs-mapa.
- Lo de la sesión s2 (T-1370 a T-1377, paso 1 del Cerro y el Puerto) estaba sin commit desde las 14:29 del 18-nov:
  se hizo commit en su rama (a1796f7).
- Rama nueva `integ/ciudad-19nov` = integ/bugs-mapa + tony/T-1370-cerro-puerto-paso1 (merge bd4fc97; se conservaron
  las dos versiones de docs/assets-faltan.md).

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1380 | Blender: ruedas de carros, puertas, cofre, cristal; rotores de DR01–DR04, E13, NV01, NV03 | pendiente |
| T-1381 | Blender: rig humanoide E15, E16, E18, E19, E20 y NP07–NP14; cuadrúpedo E17 | pendiente |
| T-1382 | Importar nuevos-18nov (variantes _a y _b), texturas T07–T09, SK01 como skyline, W10 de Nova | pendiente |
| T-1383 | Look: sodio, neones con luz, niebla de color, piso mojado con charcos, quitar el piso de escarcha | pendiente |
| T-1384 | Malecón como K11, K12 y A11 | pendiente |
| T-1385 | Cerro como K13 y A13 | pendiente |
| T-1386 | Autopista como K18 y A18 | pendiente |
| T-1387 | Puerto como K15 y A15 | pendiente |
| T-1388 | Uniones con los 4 barrios y detalle (cables, letreros, carros, NPCs, basura) | pendiente |
| T-1389 | Tráfico, peatones, enemigos y drones nuevos | pendiente |
| T-1390 | Rendimiento: 50 fps o más por zona | pendiente |
| T-1391 | Builds (Demo y Juego/CyberCity.exe) y reporte-19nov | pendiente |

## Bitácora
- 02:20 Plan escrito. Primera tanda: T-1380, T-1381, T-1382 y T-1383, en paralelo.

## Qué sigue
- Esperar la primera tanda; integrar T-1382 y T-1383 en Unity (importar y aplicar el look); después las zonas (T-1384 a T-1388).
