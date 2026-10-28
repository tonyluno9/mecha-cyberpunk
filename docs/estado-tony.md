# Estado de Tony

Rama de integración: tony/fase-7. Bloques 1 a 4 de T-F7 hechos (economía, ligas, piezas, habilidades; T-140 a T-180).

## Bloque 5 cerrado (misiones y reputación, T-181 a T-189): en main (a9b8ffb). Build provisional y guía: ff255ee (ese commit dice "T-190" por error; no es la tarea T-190).

## Bloque actual: 6, implantes (T-190 a T-197; D-tony-163 a D-tony-174)
| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-190 Contratos de los implantes (Core; autorizada por Tony en el /goal) | nada | hecha |
| T-191 Implantes como datos: tabla, servicio, guardado y balance | T-190 | hecha |
| T-192 Brazos y piernas a pie; los implantes se ven en el cuerpo | T-190, T-191 | hecha |
| T-193 La clínica y Pruebas_Implantes | T-191, T-192 | hecha |
| T-194 Ojos y sistema nervioso en el mecha | T-191, T-193 | a medias (WIP) |
| T-195 Pandilleros a pie | T-192 a T-194 | pendiente |
| T-196 Caer en la calle: el doctor | T-193, T-195 | pendiente |
| T-197 Recorrido de los implantes (cierre del bloque) | T-190 a T-196 | pendiente |
| T-136 El prólogo en Ciudad.unity y Flashback.unity de Alan | Alan (T-093, ya en main) | lista para hacer (con Escenas/Ciudad_Prologo.unity) |
| T-137 Escenas de la demo en Build Settings y build de Windows (Core) | T-136 | bloqueada |

Build provisional con escenas de Tony: docs/guia-para-jugar.md.

## Pendiente (en orden)
1. Bloque 7: radio (desde T-198)
2. Bloque 8: adaptación del rival

## Parada manual (2026-10-27, 15:10)
Tony paró el turno de noche. Bloque 5 en main; bloque 6: T-190 a T-193 hechas en tony/fase-7 (no en main).
T-194 A MEDIAS en tony/T-194-ojos-sinapsis (WIP 39f1fd0): código, creadores, pruebas y captura T-194-1 escritos. PlayMode IA y Combate
pasaron; Mecha, Pelea, UI y Progresion(.Implantes) se cortaron. Falta: compilar, correr esas áreas (OjosSinapsisPruebas 3 veces),
revisar T-194-1.png, bitácora, mover a hechas y merge en tony/fase-7. Después: T-195 a T-197, cierre del bloque 6 (verificador, main,
build), y bloques 7 (radio, desde T-198) y 8.
Conocido T-193: el horario de la clínica no sigue al reloj (pasarlo a T-196).
Temp/UnityLockfile quedó suelto por la corrida en batch cortada; no hay ningún Unity abierto. Assets/Logs/ (sin seguimiento) es basura de una corrida: no subir.
Alan aún no coloca DirectorPrologo/Flashback (T-136/T-137 bloqueadas). (Ya no: ver la unificación de abajo).

## Unificación con Alan en main (2026-10-27)
- main: **7b4b27a** (merge --no-ff de tony/unificacion = origin/main + tony/fase-7 + alan/fase-5 + alan/T-311-fachadas-neon +
  alan/T-303-interior-casino, sin conflictos). Después, main en tony/fase-7: 076a064. Fuera, por estar a medias: tony/T-194-ojos-sinapsis,
  alan/T-312-asfalto-mojado, alan/T-320-interior-mercado-negro.
- Pruebas (`verificar.sh todo`): compilar OK; EditMode 1094/1094; PlayMode 976/994, 9 omitidas, **9 fallos, todos de Alan (Arena)**:
  `MechaCyberpunk.Tests.Arena.JaulaClandestinoPruebas.Capsula_EmpujadaContraLaReja_NuncaSaleDeLaJaula` (las 8 direcciones) y
  `MechaCyberpunk.Tests.Arena.JaulaClandestinoPruebas.Cubo_SoltadoSobreJaula_QuedaSobreElPiso`. Corrida sola de JaulaClandestinoPruebas: 7/15
  (fallan las 8 de la cápsula). No es nuevo ni mío: fallaba igual en Logs/s-playmode-Arena.xml y resultados-playmode-Arena.xml (9/17).
  tony/fase-7 no toca Arena, ProjectSettings, Packages ni inicializaciones globales, y el prefab de la jaula tiene los mismos objetos y
  colliders que antes. Alan lo describe como dependiente del cuadro (`Time.deltaTime`) en docs/revision-alan.md; no se tocó.
- T-093 (Alan) ya estaba en docs/tareas/hechas/: la movió Alan (240fdec) y entró con alan/fase-5.
- **T-136 ya se puede hacer** con `Assets/_Proyecto/Escenas/Ciudad_Prologo.unity` (Alan la hizo con JugadorAPie, MechaEstacionado,
  Emboscada, DirectorPrologo y MostradorPeleas; apuntar `DatosEscenas_Demo` a Ciudad_Prologo, no a Ciudad.unity). Su archivo sigue en
  docs/tareas/bloqueadas/ hasta que se empiece.
