# Estado de Tony

Rama de integración: tony/fase-7. Bloques 1 a 4 de T-F7 hechos (economía, ligas, piezas, habilidades; T-140 a T-180).

## Bloque actual: 5, misiones y reputación (T-181 a T-188; D-tony-143 a D-tony-152)
| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-181 Contratos de reputación, misiones y puntos de misión (Core; autorizada por Tony en el /goal) | nada | hecha |
| T-182 Reputación de barrio y corporaciones, y favores | T-181 | hecha |
| T-183 Las misiones como datos: tabla, reglas, máquina de pasos y balance | T-181, T-182 | hecha |
| T-184 Servicio de misiones en escena y Pruebas_Misiones | T-182, T-183 | hecha |
| T-185 Pelear y escapar: peleas callejeras, calor, robos con chatarra | T-184 | a medias (WIP) |
| T-186 Teléfono de día, contacto del bar de noche, encargos en la calle; reputación a la vista | T-184 (paralela a T-185) | pendiente |
| T-187 Usar los favores: descuento, escondite e información | T-182, T-184, T-185, T-186 | pendiente |
| T-188 Recorrido de las misiones y la reputación (cierre del bloque) | T-181 a T-187 | pendiente |
| T-136 El prólogo en Ciudad.unity y Flashback.unity de Alan | Alan (DirectorPrologo en Ciudad.unity) | bloqueada |
| T-137 Escenas de la demo en Build Settings y build de Windows (Core) | T-136 | bloqueada |

Build provisional con escenas de Tony: docs/guia-para-jugar.md.

## Pendiente (en orden)
1. Bloque 6: implantes (desde T-189)
2. Bloque 7: radio
3. Bloque 8: adaptación del rival

## Parada manual (2026-10-26)
Tony paró el /goal. T-185 (pelear y escapar, bloque 5) quedó A MEDIAS en tony/T-185-pelear-escapar: código, datos y pruebas
escritos pero sin verificar (la corrida de pruebas se cortó). Falta: compilar, correr EditMode y PlayMode Progresion.Misiones/Pelea,
revisar capturas T-185-1/2, cerrar, merge a tony/fase-7. Después: T-186 a T-188, y los bloques 6 a 8 (implantes, radio, adaptación del rival).
Bloques 4 (habilidades) y T-181 a T-184 ya en tony/fase-7; el bloque 4 ya está en main.

## Segunda parada manual (2026-10-26)
Se retomó T-185 y Tony la volvió a parar antes de terminar: sin cambios nuevos, sigue en el WIP 3c09bf1 de tony/T-185-pelear-escapar.
Sigue igual: compilar, EditMode/PlayMode de Progresion.Misiones y Pelea, capturas T-185-1/2, merge a tony/fase-7, y mover T-061 a hechas.
Temp/UnityLockfile quedó suelto de una corrida en batch cortada; no hay ningún Unity abierto.
