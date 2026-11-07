# Estado de Tony

Rama de integración actual: tony/fase-g (fase G, abajo). Antes: tony/fase-7, bloques 1 a 4 de T-F7 hechos (economía, ligas, piezas, habilidades; T-140 a T-180).

## Noche B 3→4 nov: animaciones (docs/noche-B-tony.md, T-560 a T-562) — HECHA, en main
- Paso 0: planes en main (fdc2616). T-550 ya estaba mezclada.
- Paso 1, T-560: 46 clips de Mixamo en Assets/Arte/Personajes/Animaciones/<Grupo>/. Los importa ImportadorMixamo: Humanoid, en el sitio y con bucle según el nombre.
- Paso 2, T-561: ControladorBase con override por papel (Jugador, Aliados, NPC, SKULLS, SNAKES, Guardias, Robots, Profes; por ahora iguales).
  Tiene locomoción con blend 2D, giros, frenar, salto en 3 fases y caída; capa de torso por arma (puños, pistola, arma larga); cobertura y agachado;
  daño y muerte según la dirección; IK de pies y mirada. ManosPorIK quedó apagado. PandilleroAPie y AliadoAPie ya animan apuntar, disparar, daño y caer.
- Paso 3, T-562: sin patinar (clips medidos: caminar 1.28 m/s, correr 2.79 m/s) y cobertura sin aplanar (EscalaAgachado 1).
  Variación de idle por NPC; el jugador muere, despierta en I+D+I y se levanta con Dano_Levantarse.
  Moto: PoseMoto y PuntosMoto (pose sentado inclinado, IK de manos y pies, esconde el caballete). **No hay moto en el repo**: la Sesión A debe llamar
  PoseMoto.Montar(PuntosMoto) y Bajar() desde su manejo y poner PuntosMoto en el prefab de la moto. Rodar sigue con el giro de siempre.
- Paso 4: docs/animaciones-faltan.md (lista para bajar de Mixamo; el sufijo _Bucle hace bucle). Capturas en docs/capturas/anim-*.png
  (locomocion, giros-salto, pistola, rifle, cobertura-punos, dano-muerte, muerte-levantarse). La moto no tiene captura; la cubre PoseMotoPruebas.
- Pruebas: EditMode PersonajeVisual 102/102, AnimacionesMixamoPruebas 2/2; PlayMode PoseMoto 2/2, MuerteJugadorAnimada 1/1,
  ControladorBase y Cubierta 20/20, CaerEnLaCalle 5/5, RecorridoDemoPruebas 2/2 (solo).
- Ojo:
  - RecorridoDemoPruebas falló 2 veces corrida junto con las pruebas del jugador (contaminación de orden); sola pasa.
  - VencerlosPagaYTraeDrones (IA) falla por una ArgumentException en Trafico/Persecucion/GrafoCalles.cs, que es de Alan.
  - Correr a 2.79 m/s es menos que lo típico de Mixamo; si se ve lento, volver a medir con MedicionClips.
- Efectos de las pruebas guardados en git stash ("t561-efectos-pruebas", "t562-efectos-pruebas"). Assets/Settings y ProjectSettings siguen sin subir.

## T-550 interiores a tamaño real (3-nov-2026, cerrado a las 21:15 por la hora límite): solo la habitación
- Hecho y en main: sistema de cuartos (CuartoInterior, PuertaCuarto con fundido, ColocarT550) y la habitación a escala real (3 x 4 x 2.7 m),
  luz cálida, X01, I01-I05; captura revisada en docs/capturas/cuarto-habitacion-comparacion.png. Importados I01-I21 y X01-X05.
- Pruebas: CuartosPruebas 4/4, PrologoCiudadPruebas.Regreso (cuarto y cama) pasa. RecorridoPrologoCiudadPruebas tramo 7 (clandestino al carro)
  rozaba la esquina del CarroViejo: ahora rodea por un punto intermedio (7a). Corrida final (21:15): RecorridoPrologoCiudadPruebas 2/2 y RecorridoDemoPruebas 3/3 pasan (Logs/res-t550b.xml).
- PrologoCiudadPruebas.Prologo_DeLaCreacionAContinuara falla ("Pruebas_Prologo trae el director del prólogo"): ya fallaba antes de T-550
  (Logs/base-414.xml del 30-oct); no es de esta tarea.
- Falta (no hubo tiempo): consultorio del Doc con el brazo I06, entrada de armas de El Mercado, taller de Tino y casino como cuartos aparte.
  Cómo seguir: docs/interiores-faltan.md, sección "Pendiente de T-550".
- En main (merge a2a4e3b, con push). Builds/Demo/Cyberfix.exe regenerado a las 21:21 (Logs/build-demo-t550.log: Build Finished, Result: Success).
- Basura sin subir: Assets/_Recovery y Assets/Logs (de cortar Unity); se pueden borrar a mano. Cambios que Unity hizo solo al correr/construir
  (materiales T20-T22, T546_PapelPicado, Jaula_Pantalla, T-474-bug-2.png) quedaron en `git stash` "t550-efectos-unity"; los de Assets/Settings y
  ProjectSettings (m_BuildTargetBatching) del build quedaron sin subir.

## Fase G: juego de pandillas (T-400 a T-406; D-tony-202 a D-tony-224) — ACTUAL
Concepto nuevo (2026-10-29): sin mechas, GTA de un jugador en tercera persona en Ciudad_Prologo (docs/diseno/pandillas.md).
Rama de integración: tony/fase-g. MODO RÁPIDO: sin revisor ni verificador, 1 o 2 pruebas nuevas por tarea y al final de cada una
Builds/Juego/MechaCyberpunk.exe (arranca en Ciudad_Prologo) y 3 líneas en docs/guia-para-jugar.md. Van en serie: todas tocan la escena.
| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-400 El jugador en tercera persona en la ciudad (cámara de hombro, muñeco animado, pistola, mecha fuera, ConstruirJuego) | nada | hecha (modo rápido) |
| T-401 Carros: cámara en tercera persona, subir y bajar con E, robar los estacionados | T-400 | hecha (modo rápido) |
| T-402 Pandilleros armados en su territorio: dinero, calor, drones que disparan, clínica (Core: IdPandilla) | T-400 | hecha (modo rápido) |
| T-403 Cuatro armas del mercado negro con 3 niveles (daño, cargador, recarga) | T-400 (mejor tras T-402) | hecha (modo rápido) |
| T-404 Tres zonas con dueño: respeto, jefe, conquista y mapa con M (Core: ITerritorio) | T-402 | hecha (modo rápido) |
| T-405 Tu pandilla: hasta 3 aliados que siguen, pelean y suben de nivel (Core: IPandillaJugador) | T-402, T-404 | hecha (modo rápido) |
| T-406 Historia corta y demo de 15 min hasta CONTINUARÁ; build final en Builds/Demo | T-400 a T-405 | hecha (modo rápido) |

Orden: T-400 → T-401 → T-402 → T-403 → T-404 → T-405 → T-406. El bloque 8 (T-199 a T-205) queda en pausa (D-tony-202).

## Rondas 2 y 3 de CYBERFIX (T-407 a T-413; D-tony-232 a D-tony-247) — SIGUE
Rama tony/fase-g, modo rápido de docs/ronda3.md: merge a main y push al cerrar cada tarea; Builds/Juego/Cyberfix.exe y Builds/Demo/Cyberfix.exe
y 3 líneas en docs/guia-para-jugar.md en cada una. Guía de estilo de todas las pantallas: docs/diseno/ui-cyberfix.md.
| Tarea | Modelo | Depende de | Estado |
| --- | --- | --- | --- |
| T-407 Ronda 2: nombre CYBERFIX, GestorSonidos con provisionales, bugs del mercado negro, el Tianguis y la escopeta (Core: Sonido, productName) | Sonnet | T-406 | hecha |
| T-408 Kit de UI y marcador de objetivo con distancia y flecha en el borde; adiós OnGUI | Opus | T-407, T-418 | hecha |
| T-409 Minimapa redondo que gira y mapa grande con zoom, leyenda y punto marcado (Core: IMarcaMapa) | Opus | T-408 | hecha |
| T-410 Pausa real con Esc: continuar, guardar, cargar, opciones y salir al título (Core: Pausa) | Sonnet | T-408, T-412 | hecha |
| T-411 Opciones: sensibilidad, invertir Y, volúmenes, calidad, resolución y pantalla completa (Core: IOpciones) | Sonnet | T-407, T-408, T-410 | hecha |
| T-412 Guardar y cargar en 3 ranuras más AUTOGUARDADO; "Continuar" carga la más reciente | Opus | T-408 | hecha |
| T-413 Título CYBERFIX con la ciudad de noche y creación sin mechas: 2 personajes, escoger nombre y color de tu pandilla (los datos son de T-415) | Opus | T-407 a T-412, T-415 | hecha |

Orden: T-407 → (paso 2: T-414 a T-418) → T-408 → T-409 → T-412 → T-410 → T-411 → T-413 (en serie: todas tocan Ciudad_Prologo y ConstruirDemo). La ciudad grande no entra.

## Paso 2 del plan de la noche: nombres y decisiones del equipo (T-414 a T-418; D-tony-248 a D-tony-259) — VA ANTES DE T-408
Plan: docs/plan-noche.md. Rama tony/fase-g, modo rápido; T-414 a T-417 solo se mezclan a tony/fase-g y T-418 cierra el paso (main con push, builds y guía).
| Tarea | Modelo | Depende de | Estado |
| --- | --- | --- | --- |
| T-414 Nombres nuevos (Suburbios, SKULLS, SNAKES, El Mercado, I+D+I, El Fox, VivaCoin, Megalopolis) y moneda Biits Ƀ; CapturaUI (Core: Moneda) | Sonnet | T-407 | hecha |
| T-415 Tu pandilla con nombre y color en HUD, mapa, aliados y avisos (Core: IPerfilJugador, ColoresPandilla) | Sonnet | T-414 | hecha |
| T-416 Cinco armas en El Mercado: fusil de asalto y sniper con zoom, 3 niveles cada una | Sonnet | T-414 | hecha |
| T-417 Profes como NPC de prueba (Inventor en el CIC, Doc en I+D+I, Hacker, Mecánico, Director) y peatones al mínimo | Sonnet | T-414, T-416 | hecha |
| T-418 Historia de la demo con El Doc, El Inventor, el jefe de los SKULLS y Nova; cierre del paso 2 | Opus | T-414 a T-417 | hecha |

Orden: T-414 → T-415 → T-416 → T-417 → T-418 (en serie: todas tocan Ciudad_Prologo y CreadorCiudadPandillas). T-408 a T-413 ya usan los nombres nuevos (D-tony-259).

## Bloque 5 cerrado (misiones y reputación, T-181 a T-189): en main (a9b8ffb). Build provisional y guía: ff255ee (ese commit dice "T-190" por error; no es la tarea T-190).

## Bloque 8 (EN PAUSA por la fase G), adaptación del rival (T-198 a T-205; D-tony-190 a D-tony-201)
El bloque 7 (radio) lo lleva Alan en Radio/; este bloque no lo toca. Planeado el 2026-10-28 (planificador Opus 5.5).
| Tarea | Depende de | Estado |
| --- | --- | --- |
| T-198 Contratos del bloque 8: la clave `adaptacion` y la llave del rival (Core, solo comentarios) | nada | hecha (sin revisar): compilar OK, EditMode 1132/1132, solo comentarios; en main |
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

## Detenido por Tony (2026-10-29, noche)
- Fase G (T-400 a T-406) hecha y en main (f482d9f). La demo de pandillas está en Builds/Demo/MechaCyberpunk.exe y el juego con atajos en Builds/Juego.
- Rondas 2 y 3 planeadas en tony/fase-g (ea66199):
  - Guía de estilo: docs/diseno/ui-cyberfix.md.
  - Tareas T-407 a T-413 y decisiones D-tony-232 a 247.
  - Lo de la ronda 3 está en docs/ronda3.md.
- A medias: T-407 (ronda 2) en tony/T-407-cyberfix-sonido-bugs, commit WIP 6103fee. Solo se movió la tarea a en-curso; no hay código, porque se detuvo durante el diagnóstico.
- Sigue:
  1. Terminar T-407: diagnóstico con entrada simulada (mercado negro, Tianguis, escopeta) y arreglos, productName CYBERFIX, GestorSonidos y builds Cyberfix.exe.
  2. Luego T-408 a T-413 en el orden de ronda3.md: objetivo, minimapa y mapa, pausa, opciones, guardado y título. La pausa usa el guardado actual hasta T-412.
  3. Merge a main con push al cerrar cada punto.
- Sin rastrear y sin tocar (son de Tony): docs/imagenes-cyberfix/, docs/mejoras-pendientes.md, docs/prompts-imagenes-cyberfix.md.

## Detenido por Tony (2026-10-30, tarde)
- En main (f888f78, con push): paso 1, paso 2 y paso 3 completo del plan de la noche (T-408 a T-413: marcador, minimapa y mapa, pausa, opciones, 3 ranuras con autoguardado, título CYBERFIX y creación de personaje). Builds/Juego y Builds/Demo regenerados con T-413.
- A medias: paso 4 en tony/T-419-assets-noche, commit WIP c06136b. Los 39 modelos están importados (Assets/Arte/Calle, Clandestino, TallerCasa, Exteriores) y colocados con Arte/Editor/ColocarNoche, menos el kit de pisos. Hay capturas "antes" y de revisión en docs/capturas/noche-*. Decisiones D-tony-268 a 272. Los FBX descomprimidos están en videojuego/assets_noche/, fuera del repo.
- Falta del paso 4: terminar la medición de FPS de noche con Arte/Editor/MedicionNoche (se cortó a la mitad del build; 60 FPS es obligatorio), correr ArteNochePruebas y las PlayMode de la historia, El Mercado y El Tianguis, apilar el kit de pisos (C20a/b/d, C21, C22, C23) sobre las fachadas, sacar las capturas de después, builds, guía, y merge a tony/fase-g y a main.
- Sigue: el paso 5 de docs/plan-noche.md (mejoras 1 a 5, luego 6 a 11 y 12 a 18, sin la 13). No se ha empezado.
- Probar primero mañana: el .exe de Builds/Demo; título, NUEVA PARTIDA, creación, Esc (pausa), OPCIONES, GUARDAR en una ranura, SALIR AL TÍTULO y CONTINUAR.

## Paso 5 del plan de la noche: mejoras (T-420 en adelante)
| Tarea | Estado |
| --- | --- |
| T-420 Sensación de golpe y disparo | Hecha (rama tony/T-420-sensacion-disparo, integrada en tony/fase-g) |
| T-421 Cubrirse y rodar | Hecha (rama tony/T-421-cubrirse-rodar, integrada en tony/fase-g) |
| T-422 Pruebas rojas de PlayMode | Hecha (rama tony/T-422-pruebas-rojas, integrada en tony/fase-g) |
| T-423 IA de pandilleros más lista | Hecha (rama tony/T-423-ia-pandilleros, integrada en tony/fase-g) |
| T-424 Apuntado asistido suave | Hecha (rama tony/T-424-apuntado-asistido, integrada en tony/fase-g) |
| T-425 Rendimiento a 60 FPS | Hecha (rama tony/T-425-rendimiento, integrada en tony/fase-g) |
| T-426 Misiones secundarias repetibles | Hecha (rama tony/T-426-misiones-secundarias, integrada en tony/fase-g; cuota, defender y robar; faltan escoltar y carreras) |
| T-427 Ataques rivales a tus zonas | Hecha (rama tony/T-427-ataques-zonas, integrada en tony/fase-g) |
| T-428 Neón Alto + cuarta zona (LA REFINERÍA) | Hecha (rama tony/T-428-neon-cuarta-zona, integrada en tony/fase-g) |
| T-429 Negocios en tus zonas | Hecha (rama tony/T-429-negocios, integrada en tony/fase-g) |

## Detenido por Tony (2026-10-31, 00:50)
- En main (f790942, con push): paso 4 completo (T-419: kit de pisos, 122 FPS de noche en la calle) y paso 5 mejoras 1 a 9 (T-420 a T-429) más T-422 (15 pruebas viejas al día). Builds/Juego y Builds/Demo regenerados con T-429.
- A medias: T-430 (mejora 10, implantes a pie) en tony/T-430-implantes-a-pie, WIP 4c3e4ca: cambios en Core/Implantes, Personaje (pistola, cubierta/rodada, apuntado), El Doc y HudImplantes.cs, sin compilar ni .meta ni archivo de tarea. Falta: compilar, tienda de El Doc, pruebas, captura, docs, builds y merge.
- Sigue: terminar T-430; mejora 11 (tutorial); 12 a 18 sin la 13; al final suite completa y FPS con MedicionNoche (T-428 agregó props sin medir).
- Conocido: MostradorTiendaPruebas 2 fallan (esperan 6 artículos, hay 20); en T-426 faltan escoltar aliado y carreras; en la captura de T-429 el jugador sale en el cielo (revisar).
- Probar primero mañana: Builds/Demo/Cyberfix.exe: disparar (patada y X), Q cubrirse y Ctrl rodar, tablero de encargos junto a tu casa, negocios con E en los postes y la lista en el mapa (M).

## Sesión A (31-oct-2026, desde las 02:00)
- T-430 implantes a pie: hecha y en main (be4b6a29). Compila; EditMode Personaje+Progresion+UI 452/452; PlayMode Progresion+Implantes+Personaje 282/302. Builds/Juego/Cyberfix.exe regenerado (Result: Success); guía en docs/guia-para-jugar.md.
- Las 20 pruebas rojas de PlayMode son viejas, no de T-430 (T-430 no toca GolpeAPie): ImplantesAPiePruebas (6, esperan 15 de daño y los puños hacen 20 desde T-403), PandillaAPiePruebas (6), RecorridoImplantesPruebas, RecorridoAPiePruebas, BarrioAlanPruebas, EncargosPruebas.RobarCarro, Prologo* (3), ServicioGuardadoPruebas. ImplantesDocPruebas y EfectosAPiePruebas (las de T-430) pasan. Se atiende en T-440.
- Sigue: tutorial ligero (prioridad 2), botón CRÉDITOS del título (docs/bugs.md), T-440 pruebas viejas.

### Sesión A, resumen al cierre de T-449 (31-oct-2026, ~06:00)
Todo en main con push, una rama por tarea (`tony/T-4xx-...`), Builds/Juego regenerado y 3 líneas en docs/guia-para-jugar.md por tarea (Builds/Juego se regeneró en T-441 a T-449; T-440 se incluyó en el de T-441).
| Tarea | Qué | Prioridad de docs/sesion-A.md |
| --- | --- | --- |
| T-430 | Implantes a pie (mejora 10) | 1 |
| T-440 | Botón CRÉDITOS en el título (bug de la Sesión B) | 3 |
| T-441 | Tutorial ligero (mejora 11), opción TUTORIAL | 2 |
| T-442 | Ganchos de sonido de armas y jugador, ARM_FUS, ambiente 80 %, tabla de ensamblados | 3 y 4 |
| T-443 | Peatones que huyen al oír disparos (`EventosRuido`) | 5 (mitad) |
| T-444 | Carros que se dañan: humo, fuego, explotan | 6 (mitad) |
| T-445 | Disparar desde el carro (pistola y fusil) | 6 (mitad) |
| T-446 | Pandilleros que patrullan su zona | 7 |
| T-447 | Día y noche conectados (peatones 14/6, pandillas 1/3 de día) | 8 |
| T-448 | Guarida que sube de nivel: armero, garaje, cama | 9 |
| T-449 | Teléfono: pestaña MENSAJES de aliados y profes | 10 |
Pendiente de la lista: **tráfico que frena y pita** (no hay tráfico de calle: solo carros decorativos en la autopista; `EventosRuido` ya está listo para que frenen), ganchos de sonido que faltan de docs/sonidos-faltan-enganchar.md (hechos: ARM_*, JUG_*, PUN_BLOQUEO, VEH_CHOQUE_*; faltan CAL_*, VEH_* restantes, PAN_*, ZON_*, UI_CONTINUARA, ECO_*, MUN_*, AMB_CALLEJON), llamar a tu pandilla desde el teléfono, y conectar los visuales 3D de Mixamo (T-473 de la Sesión B; aún no hay FBX).
Pruebas rojas viejas que NO son de estas tareas (fallan igual antes): PlayMode Progresion 5 (EncargosPruebas.RobarCarroYEntregarlo, PrologoCiudadPruebas, PrologoNochePruebas, RecorridoPrologoPruebas, ServicioGuardadoPruebas.JugadorAPie_SoloEnEscenaVacia), Progresion.Implantes 7 (PandillaAPiePruebas x6 y RecorridoImplantesPruebas), Personaje 2 (BarrioAlanPruebas, RecorridoAPiePruebas), Vehiculos 2 (Camara_PrimeraPersona de MontableCarroPruebas y RobarCarroPruebas en la corrida completa; solas pasan). ArmasPruebas.ComprarYUsarEscopeta es azarosa (perdigones en cono). EditMode completo en verde (1279/1279).
Archivos de la Sesión B que NO se tocaron: docs/bugs.md (los tres bugs de la Sesión A están resueltos: CRÉDITOS en T-440, ARM_FUS en T-442, ambiente al 80 % en T-442; queda el de T-473 y poner `pisoOpcionAmbiente` en 0 de su lado), docs/sonidos-faltan-enganchar.md, Assets/Arte/Audio, GestorSonidos.

### Sesión A, cierre (31-oct-2026, ~07:15)
- T-450 (más ganchos de sonido) y T-451 (tráfico de calle que frena y pita: carros civiles por el grafo de calles, frenan a 14 m del jugador u otro carro, pitan bloqueados y al oír disparos, 6 de día y 3 de noche) también están en main con push, Builds/Juego regenerado y 3 líneas en la guía cada una. Con T-451 la prioridad 5 de docs/sesion-A.md queda completa.
- Pendiente de las 10 prioridades: solo la parte de sonido que sigue sin gancho (ver T-450), llamar a tu pandilla desde el teléfono y conectar los visuales 3D de Mixamo (T-473, no hay FBX todavía).
- Pruebas rojas viejas, sin cambios: Progresion 5, Progresion.Implantes 7, Personaje 2, Vehiculos 2-3 (solas pasan), y ArmasPruebas.ComprarYUsarEscopeta azarosa. EditMode completo 1286/1286.
| T-500 | Paso 0 importar: N01-N08, man/woman/woman2 y piezas de UI en Assets/Arte | hecha, EditMode 2/2, captura T-500-1 |
| T-501 | Paso A1 Suburbios: cables, lámparas N01, neón, charcos, niebla azul, tinacos, 48 torres de horizonte | hecha, EditMode 9/9, PlayMode 3/3, Suburbios 463 fps (Medio) / 329 (Ultra), capturas ref-suburbios-noche, ref-cruce-dia-lluvia, ref-azotea |
| T-502 | Paso A2 Callejón: tuberías N04, ventilas N05 con vapor, contenedor, llantas, tambos, escalera, neón magenta, niebla rosa | hecha, EditMode 7/7, PlayMode 2/2, callejón 443 fps (Medio, antes 436), capturas ref-callejon-noche |
| T-503 | Paso A3 El Tianguis: puestos N03a/N03b, focos en zigzag, vapor, expendedoras CY06, proyectores CY01 y hologramas de comida; arreglo del PerfilSuburbios | hecha, EditMode 8/8, PlayMode 3/3, tianguis 213 fps (Medio, antes 214), capturas ref-tianguis-noche |
| T-504 | Paso A4 Entrada de El Mercado: luz naranja del portón, papel picado, barreras C18 con franja turquesa, cámaras, servidor, implantes, plataforma de dron | hecha, EditMode 8/8, PlayMode 3/3, mercado_puerta 301 fps (Medio, antes 301), capturas ref-entrada-mercado-noche |
| T-505 | Paso A5 Neón Alto por fuera: autopista elevada N02, vallas N07, anuncios N08 y CY02, pantallas E03, entrada del casino con foquitos y tubos, torres de fondo, niebla verde y posproceso verde de noche; cierre de la Parte A (tabla de FPS abajo) | hecha, EditMode 10/10, PlayMode 4/4, neon_calle 281 fps (Medio, antes 360), capturas ref-neon-alto-noche |
| T-506 | Pulido 1, sin greybox: shader MuroMundo y materiales con textura para todo cubo liso de Ciudad_Prologo, puestos de cubos del Tianguis cambiados por mesa con huacales y tambo, mantas con calavera (colocador ColocarT506, SinGreyboxT506Pruebas 6/6, capturas T-506-*) | Hecha | tony/T-506-sin-greybox |
| T-507 | Pulido 2, luz y color: ambiente y luna de noche más bajos, farolas como focos naranja con sombra en las 14 cercanas a la ruta, fachadas terracota/turquesa/crema por edificio, grading por zona (Suburbios cálido, callejón y Tianguis magenta, Neón Alto verde), charcos suaves, pistola y barriles | hecha, EditMode Mundo 159/159, PlayMode LuzColorT507 5/5, tianguis 157 fps (Medio), RecorridoDemo 1/2 (KO intermitente del Tianguis), capturas pulido-*-comparacion |
| T-508 | Pulido 3, calles llenas: toldos de lona o lámina, letreros salientes con texto en español, balcones con barandal, macetas, tambos y huacales y gente parada en banquetas (sin colliders); charcos de T-501 a T-507 como reflejos sutiles | hecha, EditMode CallesLlenasT508 5, PlayMode CallesLlenasT508Play 2, capturas pulido3-* |
| T-520 | Paso B1 personajes: woman/man/woman2 Humanoid a 1.8 m en jugador, aliado, SKULLS y jefes, peatones, vecina, vendedor y apoyos; apuntar por IK; animaciones que faltan en docs/animaciones-faltan.md | hecha, EditMode 16 nuevas + verdes, PlayMode JugadorVisualPruebas 4/4, calle 210 fps / Tianguis 244 / tiroteo 313 / Suburbios 394 (Medio), capturas T-520-1 a T-520-13 |
| T-521 | Paso B2 (mitad 1): piezas de UI sin fondo y recortadas, fuentes Teko y Rajdhani, HUD a pie como el mockup (vida turquesa a rojo, calor, Biits con símbolo, panel de arma de 5 casillas, aliados junto al minimapa, zona con respeto) | hecha, PlayMode HudCyberfixPruebas 3/3 + HUD existentes verdes, capturas T-521-1, -1-1440 y -comparacion |
| T-522 | Paso B2 (mitad 2): menús (título con UI13/UI14, pausa, ranuras, opciones, creación, tienda El Mercado y CIC, mapa, créditos, aviso de zona) con el marco y los botones de las piezas; pulido del HUD (zona legible, minimapa oscuro con calles, contorno en texto, capturas sin posproceso); TuPandilla y TituloCreacion actualizadas a T-520 | hecho | PlayMode MenusCyberfixPruebas 3/3 + 67 de HUD y menús verdes, RecorridoDemoPruebas 2/2 (FPS de noche 42 a 274), capturas T-522-* |
| T-523 | Pulido 4: HUD limpio (cola de avisos del centro, tutorial abajo, sin lista de controles ni RADIO, zona solo en su panel, minimapa con calles claras, marcador discreto) | hecha | [T-523](tareas/hechas/T-523-hud-limpio.md) |
| T-524 | Pulido 5: título con degradado suave (bug del sprite en el prefab), barra de calor siempre visible, panel de zona con nombre grande y RESPETO | hecha | [T-524](tareas/hechas/T-524-menu-degradado.md) |
| T-510 | Personajes nuevos: SkullHombre (jefe SKULLS), Doctor, Inventor, Hacker, RobotHombre (peatones y gente), GuardiaNova (3 guardias en Neón Alto); man con su textura (antes negro) y en colores por pandilla; gente de banqueta con vida (camina, platica, respira) | hecha | [T-510](tareas/hechas/T-510-personajes-nuevos.md) |
| T-511 | Carros reales y todos robables: V02-V08 importados, 16 carros estacionados manejables y robables de los 8 modelos (sin carros de adorno), tráfico con modelos reales que se roba detenido (el conductor huye), ruedas de greybox ocultas y cámara por modelo | hecha | [T-511](tareas/hechas/T-511-carros-reales.md) |
| T-509 | Cierre del pulido: puestos de tacos del pasillo de El Mercado con el modelo C12 y sin caja colgada, ningún modelo con material liso, pistola tirada que gira y brilla ("E · Recoger la pistola"), Neón Alto verde azulado como la referencia | hecha | [T-509](tareas/hechas/T-509-cierre-pulido.md) |
| T-512 | Iluminación por zona: callejón y pasillo de El Mercado con lámparas N01 y neón magenta, portón más naranja, 18 puestos nuevos en hileras en El Tianguis, luz de luna sobre las azoteas y lluvia ligera de día | hecha | [T-512](tareas/hechas/T-512-luz-por-zona.md) |
| T-513 | Fondo de la ciudad: pinturas de Suburbios (sur y oeste), Neón Alto (este) y Nova (norte) en un cilindro de 560 m sin luces ni niebla, fundido con el cielo; 48 torres lejanas apagadas | hecha | [T-513](tareas/hechas/T-513-fondo-ciudad.md) |
| T-514 | Bot del recorrido: RecorridoDemoPruebas 2/2 tres veces seguidas sin rescate (no le tira al jefe antes de tiempo, flanquea, ventaja de curación solo en la prueba) | hecha | [T-514](tareas/hechas/T-514-bot-recorrido.md) |
| T-530 | Paso 1 (prueba 2-nov): escala y cámara más cerca, cámara que no entra a la pared, ciudad que sigue sin muros ni ALTO, barrido del Tianguis y de flotantes, puentes de Neón Alto, robots de Nova a 2.2 m | hecha | [T-530 a T-537](tareas/hechas/T-530-paso1-prueba-tony.md) |
| T-531 | Paso 1: habitación sin estación del mecha ni LA GÜERA; locales nuevos CIC de Tino (junto al MECÁNICO) e I+D+I del Doc | hecha | [T-530 a T-537](tareas/hechas/T-530-paso1-prueba-tony.md) |
| T-532 | Paso 1: tráfico que frena, cede cruces y entra/sale lejos; freno firme; aviso de incendio; nombre por modelo; motos manejables; patrulla V05 | hecha | [T-530 a T-537](tareas/hechas/T-530-paso1-prueba-tony.md) |
| T-533 | Paso 1: patrullas desde nivel 1 con sirena y radio, guardias de Nova que vigilan y reaccionan, SKULLS con SkullHombre | hecha | [T-530 a T-537](tareas/hechas/T-530-paso1-prueba-tony.md) |
| T-534 | Paso 1: NPC que te voltean a ver; peatones que no atraviesan muros | hecha | [T-530 a T-537](tareas/hechas/T-530-paso1-prueba-tony.md) |
| T-535 | Paso 1: asfalto mojado con detalle, lluvia que llega al piso, luces que se apagan poco a poco, farolas con luz en el foco, luz por puesto | hecha | [T-530 a T-537](tareas/hechas/T-530-paso1-prueba-tony.md) |
| T-536 | Paso 1: puertas a escala que se abren solas, sin pared negra, entrada del Casino abierta | hecha | [T-530 a T-537](tareas/hechas/T-530-paso1-prueba-tony.md) |
| T-537 | Paso 1: hora en el HUD, panel de armas como el mockup, título con la ciudad en movimiento, CARGANDO al recargar | hecha | [T-530 a T-537](tareas/hechas/T-530-paso1-prueba-tony.md) |
| T-540 | Paso 2: importar T20-T23, N09, L10-L14 y V07 (flujo de T-250) | hecha | [T-540 a T-544](tareas/hechas/T-540-paso2-assets-nuevos.md) |
| T-541 | Paso 2: 13 torres del kit en Neón Alto (6 a 20 pisos) y postes N09 con luz fría | hecha | [T-540 a T-544](tareas/hechas/T-540-paso2-assets-nuevos.md) |
| T-542 | Paso 2: letreros 3D VivaCoin, Hotel, El Mercado, I+D+I y CIC que brillan de noche | hecha | [T-540 a T-544](tareas/hechas/T-540-paso2-assets-nuevos.md) |
| T-543 | Paso 2: moto de reparto V07 manejable y robable en Suburbios y El Tianguis | hecha | [T-540 a T-544](tareas/hechas/T-540-paso2-assets-nuevos.md) |
| T-544 | Paso 2: 52 pantallas LED con anuncios que cambian y parpadean | hecha | [T-540 a T-544](tareas/hechas/T-540-paso2-assets-nuevos.md) |
| T-545 | Paso 3: pistola W01 en la mano del jugador y de los SKULLS y en el piso; corredera y cargador animados con las curvas de los .blend | hecha | [T-545](tareas/hechas/T-545-pistola-w01.md) |
| T-546 | Paso 4: cinco interiores con assets existentes (habitación, consultorio del Doc, taller de Tino, casino, entrada de armas del Mercado) y emisión de los letreros L10-L14 que se perdía al reimportar | hecha | [T-546](tareas/hechas/T-546-interiores.md) |

## Cierre de la Parte A (ciudad como las referencias): FPS de noche por zona
Todos medidos en el mismo build final (MedicionNoche con T-501 a T-505, 1920x1080, calidad Medio, hora 23, Logs/med505-despues/medicion.txt). Meta: 60.
| Zona | Punto de medición | FPS promedio | FPS 1% bajo | Comparación lado a lado |
| --- | --- | --- | --- | --- |
| Suburbios | suburbios | 375.2 | 264.6 | [ref-suburbios-noche-comparacion](capturas/ref-suburbios-noche-comparacion.png), [ref-cruce-dia-lluvia-comparacion](capturas/ref-cruce-dia-lluvia-comparacion.png), [ref-azotea-comparacion](capturas/ref-azotea-comparacion.png) |
| Callejón | callejon | 407.8 | 297.9 | [ref-callejon-noche-comparacion](capturas/ref-callejon-noche-comparacion.png) |
| El Tianguis | tianguis | 206.0 | 149.0 | [ref-tianguis-noche-comparacion](capturas/ref-tianguis-noche-comparacion.png) |
| Entrada de El Mercado | mercado_puerta | 359.3 | 257.9 | [ref-entrada-mercado-noche-comparacion](capturas/ref-entrada-mercado-noche-comparacion.png) |
| Neón Alto | neon_calle | 281.3 | 172.0 | [ref-neon-alto-noche-comparacion](capturas/ref-neon-alto-noche-comparacion.png) |
| Calle principal | calle | 206.9 | 115.4 | (sin referencia propia) |
La zona más lenta es la calle principal con 206.9 fps; las seis pasan 60 por mucho. En cada imagen la referencia va a la izquierda y el juego a la derecha (en Neón Alto: referencia, antes, después).

### Detenido por Tony (2026-11-01, noche)
- A medias: **T-509** (cierre del pulido) en `tony/T-509-cierre-pulido`, commit WIP 425edce con push. Hecho: Neón Alto más verde (perfil, niebla, charcos, pantallas), sin capturas ni pruebas. Falta: reemplazar el greybox del puesto de tacos del pasillo de El Mercado (trompos de bloques naranjas, garrafón blanco, mostrador gris, bancos rosas, caja gris colgante; ver docs/capturas/T-506-mercado-despues.png), terminar ColocarT509.cs (a medio escribir), ampliar SinGreyboxT506 para materiales sin textura, capturas, FPS, RecorridoDemoPruebas, Builds/Demo y merge.
- Sin importar: los modelos nuevos de imageassets/modelos3d del 1-nov (V02, V03, V04, V05, V06, V08, SkullHombre, RobotHombre, Hacker, Doctor, Inventor, Mecanico, GuardiaNova; hay copias duplicadas "- copia"). Siguiente: importarlos con el flujo de T-500/T-520 y cambiar los NPC (jefe SKULLS, DOC, INVENTOR, HACKER) y los carros.
- Conocido: RecorridoDemoPruebas da 1/2 casi siempre (el bot cae KO en la pelea con pistola del Tianguis; tramos 01-09 pasan). Hay archivos .whl de Python sueltos en la raíz del repo (los dejó una tarea de interfaz al instalar rembg): no se suben; se pueden borrar.

### Plan de personajes y carros: reporte (2026-11-02)
Los 6 pasos de docs/plan-personajes-carros.md están en main con push (un merge por paso): T-510 personajes (2de0df6), T-511 carros (c8b24da), T-509 cierre del pulido (02d0904), T-512 luz por zona (fb4f460), T-513 fondo (2a8515a) y T-514 bot del recorrido. Builds/Demo y Builds/Juego regenerados en cada paso (Result: Success); 3 líneas por tarea en docs/guia-para-jugar.md.
- Qué quedó: 16 carros estacionados robables de 8 modelos y tráfico robable; puestos de tacos C12, pistola que brilla, Neón Alto verde azulado; callejón y El Mercado con lámparas y neón, El Tianguis con 18 puestos más, luz de luna en azoteas, lluvia ligera de día; fondo pintado alrededor de la ciudad; RecorridoDemoPruebas 2/2 sin ayuda.
- Capturas: docs/capturas/T-511-*, T-509-*-comparacion, T-512-<zona>-comparacion, T-513-<vista>-comparacion.
- FPS de noche (build de medición, 1920x1080, Medio): la zona más lenta tras el paso 5 es "carros" con 155.2 (1 % bajo 88.2); calle 162.3, tianguis 162.8, suburbios 347.7. Todo muy por encima de 60.
- Qué falta o es raro: V05 y V06 son la misma patrulla y V08 una moto, y salen como estacionados de civiles (D-tony-338); el techo de lámina del frente de la azotea sigue oscuro; costura visible del fondo en el noreste (Nova y Neón Alto); PlayMode Mundo tiene 11 rojas ajenas a estos pasos (horario del mercado de 24 h, cámara libre de Alan), no comprobadas en main; MontableCarroPruebas.Camara_PrimeraPersona sigue roja desde antes; no hay clips de Mixamo todavía (docs/animaciones-faltan.md).

## Reporte del plan de la noche 3-nov (docs/plan-noche-03nov.md), pasos 1 a 4: terminado

- Paso 1 (T-530 a T-537), arreglos de la prueba del 2-nov: en main (0b7c759).
- Paso 2 (T-540 a T-544), assets nuevos: en main (37afd9f). FPS de noche arriba de 60 en todos los puntos (lo más bajo, la calle: 161, 1 % bajo 88).
- Paso 3 (T-545), pistola W01 animada: en main (f1ec404).
- Paso 4 (T-546), interiores: en main con este commit. Cinco interiores armados con lo que había y comparados con su referencia
  (docs/capturas/interior-*-comparacion.png); lo que falta, en docs/interiores-faltan.md. FPS de noche dentro: de 229 a 489.
- Hallazgo: los materiales URP/Lit hechos por código con GI None pierden la emisión al reimportarse. Los letreros L10-L14 ya
  están arreglados; otros creadores viejos (Mundo, Arena, Casino) también usan None (ver docs/interiores-faltan.md, nota técnica).
- Builds/Demo y Builds/Juego regenerados al final. Guía: docs/guia-para-jugar.md, pasos 1 a 4 del 3-nov.
- Siguen en rojo, como antes del plan: MercadoNegro 24 h/Lugares, RecorridoAPie (entrada del Bar), MontableCarro.Camara_PrimeraPersona.
- Para que lo veas tú: la habitación todavía es la caja grande del greybox. Para parecerse a la referencia hay que achicar las paredes de la casa
  (decídelo tú: es la escena del prólogo).


## Sesión A: falta para Sesión B/Tony (sonido, bloque 5 del 4-nov)

Audios que faltan o están flojos en Assets/Arte/Audio (todo lo demás ya suena con lo que hay):
- Voces por género: no hay JUG_DANIO_F / _M, JUG_CAE_DERROTADO_F / _M, JUG_CORRER_RESPIRA_F / _M, JUG_SALTO_F / _M (esfuerzo), PAN_RIVAL_DANIO_F / _M, PAN_RIVAL_CAE_F / _M, PAN_RIVAL_ALERTA_F / _M, PAN_ALIADO_RECLUTA_F / _M, PAN_ALIADO_CAE_F / _M. Hoy suena la voz única con tono +20 % (mujer) o -5 % (hombre). Cuando llegue un archivo con ese nombre (por ejemplo JUG_DANIO_F_01.wav, y correr CreadorCatalogoSonidos.Crear) se usa solo.
- Esfuerzo de golpe o patada (JUG_ESFUERZO) y gemido de pandillero/aliado herido distinto del KO: no existen.
- Casi mudos (pico menor a 0.1): ARM_SNI_ZOOM_01 y _02, ARM_BALA_IMPACTO_METAL_01 y _02, JUG_REGENERA_01, UI_MOVER_01 (el gestor no normaliza; conviene renormalizar el WAV). JUG_CORRER_RESPIRA_01 sale bajito (pico 0.1).
- ZON_ENTRAR_PROPIA_01 y ZON_ENTRAR_RIVAL_01 (piano) quedaron sin uso; si se quieren, un tic corto en vez del piano.
- UI_TEXTO es provisional (generado por código); falta uno final y sutil.
- Motor: no hay manera de mezclar VEH_VIEJO_MOTOR_ACELERA con el ralentí ni de arrancar el bucle en un punto al azar sin cambiar Core/ISonidos (volumen por bucle y desfase); VEH_MOTO_MOTOR y VEH_PRO_MOTOR_* siguen sin clase que los toque.
- Pasos: mojado solo por nombre de material ("mojad", "charco", "wet"); falta que Mundo nombre así la calle mojada o un contrato de clima en Core.
- Pisadas por evento de animación: no hay eventos en los clips; los pasos van por distancia (1.4 m caminando, 2 m corriendo en ControladorAPie). Si la Sesión B agrega eventos "Paso", se llama al mismo punto y se quita la zancada.

## Sesión del 4 al 5-nov (Opus orquesta, Sonnet implementa): detenida por Tony
- Bloque 4 (carros y motos) en main: T-590 luces con L, T-591 explosión con efectos (estruendo provisional), T-592 manejo de moto e inclinación, caballete oculto al manejar.
- Bloque 5 (sonido) en main: T-593 armas sin encimarse, T-594 sin piano al cruzar zona y tic de texto sutil, T-595 claxon y motor con variación y distancia, T-596 pasos por zancada y piso, T-597 voces por género (con tono mientras faltan los audios _F/_M, lista arriba). RecorridoDemoPruebas 2/2 tras cada bloque.
- A medias: **T-598** (rama tony/T-598-patrulla-sin-calles, commit WIP con push): PandillaArmadaPruebas.VencerlosPagaYTraeDrones fallaba porque GestorPersecucion armaba calles sin manzanas (viene de antes de T-587). Arreglo hecho y sin probar: correr PandillaArmadaPruebas, Persecucion y PatrullaParedT581Pruebas; si pasan, mezclar a fase-g y main.
- Rojas ya conocidas: MontableCarroPruebas.Camara_PrimeraPersona, AmbientePruebas.Asfalto_EsOscuroYLiso, SinGreyboxT506Pruebas x2, PersonajesT520Pruebas.ListaDeAnimacionesQueFaltan_Existe (docs/animaciones-faltan.md lo reescribió la Sesión B), RecorridoAPie; inestables: JugadorVisualPruebas.Caminar_SinPatinar y ArmasPruebas.ComprarYUsarEscopeta.
- Sigue: terminar T-598; bloques 6 (NPC y aliados), 7 (mundo) y 8 (interfaz) de docs/noche-A-alan.md; luego los [C] sin marcar de docs/pendientes-pruebas.md; al final regenerar Builds/Demo y Builds/Juego (no se regeneraron en esta sesión).
- Cambios del editor de tony/anim-noche guardados en el stash "churn-editor-anim-noche-20261104".

## Noche 5-nov (docs/plan-noche-05nov.md): detenida por Tony en el Paso 1
- Paso 0 hecho: plan y docs/sonidos-sugerencias.md en commit, origin/main mezclado.
- Paso 1, **T-620** (sonido mínimo por interruptores en CatalogoSonidos.asset) y **T-598** (patrulla sin manzanas) están mezcladas en tony/fase-g con push, **sin pasar a main**: EditMode de sonido 50/50; el PlayMode dirigido (SonidoMinimoT620Pruebas con el minuto simulado, ExplosionCarroPruebas, PandillaArmadaPruebas, PersecucionRealistaT587Pruebas, PatrullaParedT581Pruebas, Sonido, RecorridoDemoPruebas) quedó corriendo al detener; su resultado sale en Logs/r-t620-play.xml.
- La corrida completa de PlayMode por área de T-620 la mató el sistema por falta de memoria: correr por filtro o área por área.
- Sigue: revisar Logs/r-t620-play.xml; si pasa, merge de tony/fase-g a main (cierra el Paso 1). Luego Paso 2 (animaciones de imageassets/animaciones: Carros e interaccion.zip y Enemigos y jefe.zip, IDs T-621+) y Paso 3 (bloques 6 a 8 de docs/noche-A-alan.md y [C] sin marcar). Builds/Demo y Builds/Juego sin regenerar.

## Noche 5-nov (docs/plan-noche-05nov.md), pasos 0 a 4: terminado

Opus orquestó y Sonnet implementó. Cada paso se mezcló a main con push.

- **Paso 0**: Unity cerrado y sin candado; tony/fase-g al día con origin/main.
- **Paso 1** (794e21b): T-620 sonido mínimo y T-598 patrulla sin calles.
  - Interruptores en CatalogoSonidos: 24 de 130 IDs encendidos.
  - Límites: 1 voz por ID, 12 en total y 0,1 s entre repeticiones.
  - En un minuto simulado: 12 voces como máximo y 0 repeticiones.
- **Paso 2** (c1a0366): T-621 a T-625, animaciones de carros (subir, bajar, manejar, claxon), gestos, teléfono de NPC, SKULLS, jefe y Nova.
  - Las animaciones que faltan están en docs/animaciones-faltan.md.
- **Paso 3** (f90990e):
  - T-630: aliados que te siguen, suben al vehículo y reaparecen si se quedan atrás.
  - T-631: NPC que no se atraviesan y se destraban.
  - T-632: minimapa con ruta GPS.
  - T-633: teléfono con animación y hora.
  - T-634: prueba de la lluvia que llega al piso.
  - T-635: cielo con nubes y estrellas, y luz de día cálida. Se arreglaron las pruebas Asfalto y SinGreyboxT506.
  - En docs/pendientes-pruebas.md se marcaron 38 [x] (T-530 a T-537 y T-630 a T-635).
- **Paso 4**: Builds/Demo (MechaCyberpunk.exe) y Builds/Juego regenerados por última vez a las 14:45 UTC con T-636 a T-639, ambos con "Result: Success" y Unity cerrado.

**Qué sigue:**
- Segunda tanda del paso 3, a pedido del hook de meta, porque el corte de las 13:15 UTC era mío y no del plan:
  - T-636 (acc2319): los puños I a III del CIC ya existían. Solo se agregó una prueba (2/2).
  - T-637 (3435c4f): puestos de tacos a escala 0,72, unos 2,5 m. En Ciudad y Ciudad_Prologo se separaron 2 puestos que se traslapaban. Pruebas 3/3 más 44 de mundo.
  - T-638 (a36e285): KitUI.VestirPantalla pone marco UI01 y fuentes Teko/Rajdhani en tienda, teléfono, misiones, clínica, banco, taller, apuesta y cartelera. Captura: T-638-telefono.png.
  - RecorridoDemoPruebas 2/2.
- T-639 (a07c7f1): Hacker SERGIO sentado a una mesa con laptop, en pose procedural (PoseSentado) hasta que exista NPC_SentadoEscribiendo_Bucle.fbx.
  - HackerSentadoPruebas 3/3; captura revisada.
  - La mesa y la silla no tienen colisión.
  - RecorridoDemoPruebas con T-639: la corrida final de las 14:35 UTC no dio resultado. Unity se cayó (crash handler) y se cortó a los 600 s.
    - Antes del ajuste de orientación pasó 2/2. Las builds de las 14:45 UTC salieron con "Result: Success".
    - Repetir RecorridoDemoPruebas es lo primero de la próxima sesión.
- Calles con curvas: no se hicieron (razón en decisiones-pendientes-tony.md). El pendiente es [?] porque la frase quedó cortada.
- Abiertos:
  - Agacharse de los NPC.
- Faltan audios y animaciones, listados en docs/sonidos-sugerencias.md y docs/animaciones-faltan.md.
- La ruta GPS usa el grafo de las 6 manzanas de GeometriaBarrio, no la ciudad completa.
- Pruebas que siguen rojas, conocidas y sin perseguir:
  - MontableCarroPruebas.Camara_PrimeraPersona.
  - RecorridoAPie.
  - Inestables: JugadorVisualPruebas.Caminar_SinPatinar y ArmasPruebas.ComprarYUsarEscopeta.
- Las capturas T-635 de cielo miran a una pared y apenas muestran el cielo; hay que repetirlas.

## Sesión 2 del playtest 5-nov (mundo y arte), rama tony/s2-mundo

Todo por código de editor e idempotente: `Assets/Arte/Editor/ColocarS2*.cs` (menú Mecha Cyberpunk/S2). No se regeneró Barrio.prefab (solo se le quitó
el objeto Lluvia) ni se corrió CreadorTodo. Capturas lado a lado en `docs/capturas/s2-*.png`. Pruebas nuevas: `MundoS2Pruebas` (EditMode, 9) y
`TallerMecanicoS2Pruebas` (PlayMode, 3). RecorridoDemoPruebas 2/2 tras las zonas 3 y 4-5 (una corrida intermedia falló en la pelea con pistola del tianguis:
se quedó en 299 s de 300; al repetir pasó. Esa pelea va justa desde que entraron T-650 a T-652 de la sesión 1).

- **Zona 1** (`ColocarS2.Limpieza`): sin lluvia (objeto Lluvia fuera de Barrio.prefab, salpicaduras apagadas, `LluviaSigueJugador.Activa` empieza en false;
  se quitó LluviaPruebas). Fuera los letreros de primitivos de T-508/T-501/T-502, el arco y las series de focos del tianguis, los textos flotantes
  (letrero de PersonajeApoyo apagado con `mostrarLetrero`, EL FOX, VENDEDOR, ARMAS, I+D+I, EL MERCADO, ENCARGOS) y los halos/luces de neones planos.
  La luz de cada farola va bajo la cabeza de la lámpara y no ilumina su poste (capa de render 5).
- **Zona 2** (`ColocarS2.Escala`): los personajes de Tripo miden 1.80 en reposo pero mucho menos animados (medido con BakeMesh en Play: jugador 1.40,
  Doc 1.17, Tino 1.18, woman2 1.12). Se escaló Giro/Modelo de cada Visual_* para igualar al jugador (robots de Nova 1.25x). **El jugador también se ve de
  1.40 m contra una vara de 1.8 m**: no se tocó (prefab de la sesión 1); si se escala, volver a correr `ColocarS2.Escala` con la nueva `AlturaJugador`.
- **Zona 3** (`ColocarS2Interiores`): consultorio (I+D+I) y taller de Tino (CIC) en su local, con forro del kit X, frente con vano y muebles I06-I21;
  el Doc y Tino no se movieron. El taller del mecánico es un cuarto aparte (1020, 0, 1000) con fundido, puerta en la fachada de enfrente de la casa
  (Edificio_45) y `DatosLugar_Taller`; Canepa sigue afuera.
- **Zona 4** (`ColocarS2Casa`): fachada de la casa con el kit (cubos viejos solo como colisión), tablero de encargos como kiosco CY10, cuarto según
  habitacion.png (ropero a la derecha, foco pelón con cables, mochila de 50 cm, más pósters, caja eléctrica, puerta L02), aparición movida para no caer
  en el ropero. Las puertas de cuarto tienen su fundido propio (0.2 + 0.08 + 0.3 s) en lugar del de las pantallas de historia (0.6 + 0.15 + 0.6).
- **Zona 5** (`ColocarS2Callejon`): puertas con lámpara a los dos lados, medidores, cajas, tambos, llantas, basura y cables, pegados a los muros.
- **Zona 6** (`ColocarS2Calles`): los 102 cubos de las 6 calles de salida de T-530 vestidos con el kit (4761 piezas sin collider ni sombra, LODGroup
  kit/cubo viejo), azoteas con tinacos, aires y antenas, cables cruzando. FPS sin medir en build (en batch no hay estadísticas de render).

Pendiente: medir FPS de las salidas en el build de medición; tono del consultorio más verde que la referencia; la falla previa
ReglasEnsambladosPruebas (UI referencia a Trafico) no es de esta sesión.
- T-670 (06-nov noche): jugador y NPC a 1.80 m; ColocarS2.Escala con alturas medidas por BakeMesh; CubiertaYRodada conserva la escala del Modelo; pasos escalan con el modelo. Rama tony/T-670-escala-jugador.
- T-671 (06-nov noche): arma equipada sin apuntar = caminar/correr normal y arma abajo; poses de arma solo al apuntar; capa Piernas (T-651) quitada. Rama tony/T-671-arma-sin-apuntar.
- T-672 (06-nov noche): al apuntar cámara centrada (lateral 0), altura 2.0 y distancia 2.0, FOV 45; mira arriba de la cabeza. Rama tony/T-672-camara-apuntar.
- T-673 (06-nov noche): CTRL agacha/levanta (alterna), lento, SHIFT agachado más rápido, agachado+ESPACIO rueda y sin agacharse no rueda; enemigos pierden vista tras cobertura baja (pecho según cápsula). Rama tony/T-673-agacharse.
- T-674 (S1 noche): agachado con pose procedural (PoseAgachado): cadera baja, rodillas dobladas, torso recto; quieto, caminando y apuntando.
- T-675 (S1 noche): sin spam de golpes (espera la animacion), reaccion al dano corta y cortable con golpe o esquive.
- T-676 (S1 noche): destello chico, disparo siempre igual sin sonidos de impacto, trazo finisimo, sin linea de vista no hay disparo (jugador, SKULLS, dron).
- T-677 (S1 noche): headshot x3 (hueso de cabeza o 15 % de arriba del collider) a enemigos, aliados y civiles; ImpactoBala.Cabeza.
- T-678 (S1 noche): ImportarS1n mete W02-W04, C01 y F01 solo cuando aparecen (idempotente); H02-H04 ya estaban.
- T-679 (S1 noche): escopeta, fusil y francotirador con su modelo en la mano, en el piso (prefabs _Piso) y registrados; destello F01 más grande.
- T-680 (S1 noche): C01 trae esqueleto: Visual_Policia y prefab PoliciaAPie (pistola W01, datos propios); reemplazo man azul marino listo.
- T-681: el cañón de las 4 armas apunta al punto de mira al apuntar y disparar (torso + IK de la mano). Pruebas: CanonAlPuntoDeMiraT681Pruebas, AgarresArmasT681Pruebas.
- T-682: al apuntar la cámara conserva altura y distancia de caminar, centrada, con zoom (FOV 40). Pruebas: CamaraApuntarT682Pruebas.
- T-683: agachado con animación real (AgachadoQuieto, AgachadoCaminar, capa Brazos), pies más rápidos con Shift, rodada lenta de 0.8 s que no se hunde y termina agachado. Faltan en Mixamo: Crouch Idle, Crouched Walking, Forward Roll (docs/animaciones-faltan.md). Pruebas: AgachadoAnimadoT683Pruebas.

- T-684 escudo de energia: IEscudo (Core), EscudoJugador, barra en el HUD con destello; mejorable por IEscudo. 12 EditMode + 6 PlayMode.
- T-685 expendedoras: 5 consumibles (bebida, papitas, venda, cafe, bateria), Tab/F/6-0, expendedora con E en casa, tianguis y Nova colocadas en ejecucion. 5 EditMode + 9 PlayMode.
- T-686 botin de enemigos: los pandilleros sueltan dinero (se recoge al pasar) y arma (ArmaEnPiso); el jefe una escopeta. EventosBotin en Core. 2 EditMode + 3 PlayMode.
- T-687 disparar desde el carro y la moto: clic derecho apunta libre y la pistola sale (moto con una mano); 3 PlayMode.
- T-688 pruebas rojas: HudAPie (vida 150), Mercado/Nombres (pantalla de expendedoras T-685), Doc 1.10 en escala, escudo en Historia/Apoyo, intermitentes endurecidas; quedan rojas las de S2 (Doc, ambiente, CarrosS2n) y previas (Bar, Vehiculos). Ver docs/tareas/hechas/T-688-pruebas-rojas.md
- T-689 pantalla HAS MUERTO (PantallaAviso: Teko, neón, glitch) tras la caída y antes del fundido a la clínica; PlayMode HasMuertoT689Pruebas. Ver docs/tareas/hechas/T-689-has-muerto.md
- T-690 arresto con consecuencia: pantalla ARRESTADO, multa 25 % del dinero, apareces en casa (ConsecuenciaArresto en Progresion/Prologo/Policia, mensaje desde Trafico); EditMode ReglaMultaArresto y PlayMode ArrestoT690Pruebas. Ver docs/tareas/hechas/T-690-arresto.md
- T-691 quemarse: ReglaQuemadura/QuemaduraJugador (4 s, ticks, se apaga al rodar o al salir del área, tope de daño); medido: 0 daño junto al robot de Nova (ver tarea). EditMode ReglaQuemaduraPruebas, PlayMode FuegoT691 y MedicionDanioNovaT691. Ver docs/tareas/hechas/T-691-fuego.md
- T-692 explosión en área: causa de los carros voladores (impulso 1400 x masa) arreglada, choques imposibles no cuentan, patrullas con DanioCarro, alarma de fuego; EditMode ChoqueRealT692 y PlayMode ExplosionAreaT692 (Vehiculos 72/79, 3 rojas previas). Ver docs/tareas/hechas/T-692-explosion-area.md
