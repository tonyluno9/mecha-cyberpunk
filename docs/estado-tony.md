# Estado de Tony

Rama de integración: tony/fase-7. Bloques 1 a 4 de T-F7 hechos (economía, ligas, piezas, habilidades; T-140 a T-180).

## Bloque 5 cerrado (misiones y reputación, T-181 a T-189): en main (a9b8ffb). Build provisional y guía: ff255ee (ese commit dice "T-190" por error; no es la tarea T-190).

## Bloque actual: 8, adaptación del rival (T-198 a T-205; D-tony-190 a D-tony-201)
El bloque 7 (radio) lo lleva Alan en Radio/; este bloque no lo toca. Planeado el 2026-10-28 (planificador Opus 5.5).
| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-198 Contratos del bloque 8: la clave `adaptacion` y la llave del rival (Core, solo comentarios) | nada | pendiente |
| T-199 La adaptación como datos y reglas: hábitos, memoria y niveles | nada | pendiente |
| T-200 El rival observa y recuerda: estilo por pelea, memoria y guardado | T-199 (T-198 no bloquea) | pendiente |
| T-201 GUARDIA: el rival que te estudió bloquea tus puños | T-200 | pendiente |
| T-202 FINTAS y RETIRADA | T-200, T-201 | pendiente |
| T-203 La cabina te dice qué te leyó el rival | T-200 | pendiente |
| T-204 Pruebas_Adaptacion: pelear una y otra vez contra el que elijas | T-200 a T-203 | pendiente |
| T-205 Recorrido de la adaptación del rival (cierre del bloque) | T-198 a T-204 | pendiente |

Sigue: T-198 y T-199 (a la par), luego T-200; después T-201 → T-202 con T-203 en paralelo; luego T-204 y T-205. Las preguntas para Tony
están en D-tony-201; ninguna bloquea.

## Bloque 6: implantes (T-190 a T-197; D-tony-163 a D-tony-174), cerrado
| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-190 Contratos de los implantes (Core; autorizada por Tony en el /goal) | nada | hecha |
| T-191 Implantes como datos: tabla, servicio, guardado y balance | T-190 | hecha |
| T-192 Brazos y piernas a pie; los implantes se ven en el cuerpo | T-190, T-191 | hecha |
| T-193 La clínica y Pruebas_Implantes | T-191, T-192 | hecha |
| T-194 Ojos y sistema nervioso en el mecha | T-191, T-193 | hecha (sin revisar): EditMode Mecha 167/0; PlayMode Combate 61, IA 44, Mecha 155/158, Pelea 66, Implantes 18, UI 125/131, sin fallos; OjosSinapsis y ReglaReaccion x3 verdes; md5 de prefabs igual |
| T-195 Pandilleros a pie | T-192 a T-194 | hecha (sin revisar): EditMode IA 67/0 x3 (ReglaPandilleroAPie 12); PandillaAPiePruebas 10/10 x3; PlayMode IA 44, Personaje 129, Implantes 28, UI 125/131 (6 omitidas), sin fallos; tumbar: 4 golpes / 3 con BRAZOS I / 2 con BRAZOS II / 3 tiros; $120 por la pandilla; salir del radio 2.53 s sin implante y 1.55 s con PIERNAS I; md5 de prefabs y escena igual; captura T-195-1 revisada |
| T-196 Caer en la calle: el doctor | T-193, T-195 | hecha (sin revisar): EditMode ReglaDoctorPruebas 12/0; CaerEnLaCallePruebas (5) + CaerEnLaCalleMisionPruebas (1) 6/6 x3; regresión (prólogo, emboscada, misiones, montaje, implantes) 49/0; despierta en la camilla a las 05:00 con dinero -$200 y calor 0; mecha 80 % -> 20 % ($800 en el banco), 10 % -> piso 15 %; misión del callejón falla una vez (barrio -3); pelea del flujo e historia fuera de Libre no llaman al doctor; md5 de escenas, prefab y asset igual en dos corridas; captura T-196-1 revisada; D-tony-180 |
| T-197 Recorrido de los implantes (cierre del bloque) | T-190 a T-196 | hecha (sin revisar): RecorridoImplantesPruebas 1/1 x3 en verde (pasos a a j; a 9.6 s, f 29 s, h 11 s); recta 30 m tramo 10-30 m 2.667 s sin implante y 2.222 s con PIERNAS I (igual a lo calculado); salto 1.1/1.43/1.76 m; pandilla 3 golpes con BRAZOS I; OJOS I avisa a 0.4 s; golpe SINAPSIS I 0.213 s; caida -$200 a las 05:00; F5/F9 devuelven todo; capturas T-197-1 a 3 revisadas; D-tony-181 (cajas de salto vs stepOffset) |
| T-136 El prólogo en Ciudad_Prologo.unity y Flashback.unity de Alan | Alan (T-093, ya en main) | hecha (sin revisar): EditMode Progresion+UI 337/337; PlayMode Progresion 115/115; PrologoCiudadPruebas 5/6 con la caminata que se desatasca (1 falla por un cuadro de 671 ms, no reproducida); D-tony-187/188 resueltas |
| T-137 Escenas de la demo en Build Settings y build de Windows (Core) | T-136 | hecha (sin revisar): Build Settings con CreacionPersonaje, Ciudad_Prologo y Flashback; Builds/Demo; EditMode 1132/1132; PlayMode por área sin fallos salvo los 9 de Arena de Alan; guía en docs/guia-demo.md |

Build provisional con escenas de Tony: docs/guia-para-jugar.md.

## Pendiente (en orden)
1. Bloque 8: adaptación del rival (T-198 a T-205, arriba). El bloque 7 (radio) es de Alan.
2. Hackeo si sobra tiempo (desde T-206)

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

## T-250 assets 3D: hecha (2026-10-28)
Rama tony/T-250-assets, mezclada en main. Los 38 modelos del lote importados y colocados (tabla en docs/assets-importados.md).
Pruebas: EditMode 1094/1094; PlayMode 984/994 (1 intermitente de Misiones, no de T-250, 3/3 sola). Builds/Provisional y
Builds/Provisional_Assets regenerados con ConstruirT250.Construir. El arte vuelve a ser de Alan desde esta noche.
Rama tony/T-197-cajas-salto: cajas de salto de Pruebas_Implantes con el stepOffset (0.725/1.615/1.945 m), pruebas ajustadas; D-tony-181 corregida.

## Bloque 6 cerrado (2026-10-28)
T-194, T-195, T-196, T-197 y la corrección de las cajas de salto, verificadas en tony/fase-7: EditMode 1129/1129; PlayMode 999/1017 (8 de JaulaClandestinoPruebas de Alan, conocidos; OjosII_ContraElJefe inestable, 3/3 sola); playmode-por-area sin LIMITE. Mezclado a main. Builds/Provisional y Builds/Provisional_Implantes (385 MB). Criterios de sensación para Tony en docs/revision-tony.md. Doc arreglado: el commit de T-197 había vaciado decisiones-pendientes y la guía (D-tony-183).

## Detenido por Tony (2026-10-28, mañana)
- A medias: T-136 en tony/T-136-prologo-en-ciudad-alan (commit WIP). Hecho: CreadorDatosProgresion.CrearT124 ya recibe ciudad y flashback por asset y _Demo apunta a Ciudad_Prologo.unity y Flashback.unity (sin compilar ni correr el creador). Falta: compilar, regenerar DatosEscenas_Demo con el creador, revisar qué tienen Ciudad_Prologo/Flashback de Alan (prefabs y puntos de D-tony-72), ids de ruta en DatosPrologo_Base, texto del carro en ConfigAyudas_Base, PrologoCiudadPruebas, capturas T-136-1..4.
- Sigue: terminar T-136; luego T-137 (Build Settings, Builds/Demo, verificador y suite, merge a main, guía de la demo de 15 min); luego el paso 4 (planificador, bloque 8 desde T-198).
- Bloque 6 (T-194 a T-197) cerrado y en main (b392387); T-250 en main (0ea424c).

## Detenido por Tony (2026-10-28, tarde)
- T-136 casi lista en tony/T-136-prologo-en-ciudad-alan (sin merge a tony/fase-7). En e6213c5: DatosEscenas_Demo regenerado (CreacionPersonaje, Ciudad_Prologo, Flashback), texto del carro, PrologoCiudadPruebas (hereda de T-135), capturas T-136-1..4 revisadas, bitácora y D-tony-184 a 189. Pruebas en e6213c5: compilar OK, EditMode 1129/1129, PlayMode Progresion 115/115, PrologoCiudadPruebas 7 de 8 corridas (1 falla sin reproducir en la emboscada). Recorrido 193 s; estimado para una persona 11 a 15 min.
- A medias, WIP 8fd6349: corrección para H6 de D-tony-187 (con el jugador en el carro, el objetivo apunta a la emboscada: textoIrALaEmboscada y el cambio en DirectorPrologo.Update; falta que FijarObjetivo use la emboscada) y D-tony-188 (horaDespertar 20:00; pruebas que fijaban 19:30 ajustadas; se quitó el atajo del reloj en PrologoCiudadPruebas). Sin compilar ni correr.
- Sigue: terminar FijarObjetivo, compilar, EditMode Progresion/UI, PlayMode Progresion, PrologoCiudadPruebas 3 veces (guardar xml y log si falla la emboscada), regenerar capturas si cambian, marcar D-tony-187/188 resueltas, mover T-136 a hechas y merge a tony/fase-7. Luego T-137 (Build Settings y Builds/Demo; ojo: la tarea dice Ciudad.unity, pero ahora es Ciudad_Prologo.unity) y el bloque 8 (planificador, desde T-198). El bloque 7 (radio) es de Alan.
