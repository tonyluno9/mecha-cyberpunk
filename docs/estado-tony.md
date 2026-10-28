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
| T-136 El prólogo en Ciudad.unity y Flashback.unity de Alan | Alan (DirectorPrologo en Ciudad.unity) | bloqueada |
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
Alan aún no coloca DirectorPrologo/Flashback (T-136/T-137 bloqueadas).

## T-250 assets 3D: A MEDIAS (parada manual, 2026-10-28)
Rama: tony/T-250-assets (sale de origin/main 7b4b27a). Commit WIP con todo lo hecho; no está mezclada en main.
- Hecho: los 38 modelos del lote (origen assetss/ = zips de Descargas, idénticos; sin E, X ni P) importados en Assets/Arte/<Zona>/ con
  postprocesador, material URP Lit, prefab y Escenas/Tony/Pruebas_Assets.unity. Colocados con ColocarT250.Colocar en Barrio.prefab
  (422 fachadas, 54 farolas, 22 basureros, 3 puestos, 17 carros, 10 tinacos, 45 aires, callejón, casa y taller), JaulaClandestino
  (gradas, sótano, lámpara) y CarroViejo. Capturas antes/después revisadas (docs/capturas/T-250-*). Tabla en docs/assets-importados.md.
- Pruebas: compilar OK, EditMode 1094/1094. PlayMode SE CORTÓ a medias (parada manual): falta el resultado.
- Falta, en orden: 1) bash herramientas/verificar.sh todo; ojo con las pruebas de 100 ms por cuadro (LluviaPruebas, BarInteriorPruebas):
  el barrio suma ~6 M de triángulos (carros 90 mil c/u, gradas 160 mil); si fallan, bajar polígonos o quitar carros de fondo.
  Si una prueba falla por collider o posición, se ajusta el modelo. 2) C04, C05, C08, H01a y L03 sin lugar en el juego.
  3) Commits por zona no se hicieron (va en uno WIP). 4) Merge a main con push (autorizado por Tony) y regenerar Builds/Provisional
  (más un Provisional_Assets que arranque en Ciudad_Prologo, porque las escenas del build actual no cargan Barrio).
- Para rehacer la colocación: git restore de los 3 prefabs y correr ColocarT250.Colocar (no deshace una colocación anterior).
- Temp/UnityLockfile puede quedar suelto por la corrida cortada; no hay ningún Unity abierto. No subir assetss/ ni Assets/InitTestScene*.
