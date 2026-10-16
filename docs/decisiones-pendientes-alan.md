# Decisiones pendientes de Alan (se pasan a docs/decisiones.md al aprobar)

## Planeación de A-F1 (2026-10-11, planificador Opus 5.5, a pedido de Tony)

### D-alan-1 Pruebas_Alan.unity (T-020, T-040)
Propuesta. Pruebas_Alan.unity reemplaza a Pruebas_Compa.unity. Tiene el barrio, la cámara libre y las herramientas de prueba de
Alan, con la jaula fuera del barrio. Ciudad.unity tiene el mismo barrio sin herramientas de prueba.

### D-alan-2 Cámara libre propia (T-040)
Propuesta. Alan prueba con `Mundo/Pruebas/CamaraLibre`: CharacterController con ojos a 1.75 m, modo vuelo con F y sus propias
teclas. Cuando T-031 llega a main implementa `IJugador` (modo a pie), así las puertas y zonas de Alan se prueban sin el jugador
de Tony y funcionan igual con él.

### D-alan-3 Barrio como prefab y medidas (T-040)
Propuesta. El barrio es `Mundo/Prefabs/Barrio.prefab`, hecho por un creador de editor y colocado en Ciudad.unity y
Pruebas_Alan.unity. 3 × 2 manzanas de unos 80 m, calles de 14 m y banquetas de 3 m (caben una persona y un mecha de 4 m). Se
ajusta en A-F2 midiendo con el carro el "minuto y medio cruzando" de ciudad.md.

### D-alan-4 Reloj (T-041)
Propuesta. Amanecer a las 07:00 y anochecer a las 19:00: las 12 horas de día pasan en 8 minutos reales y las 12 de noche en 12,
así el día dura 20 minutos como dice ciudad.md. El juego empieza a las 21:00. Valores en `ConfigRelojMundo`.

### D-alan-5 Horarios de los locales (T-042)
Propuesta. Casa siempre abierta; bar de 19:00 a 04:00; clandestino de 20:00 a 06:00; casino de 20:00 a 06:00 y mercado negro de
09:00 a 18:00, pero los dos "cerrados en la demo" (demo.md): su puerta da `Bloqueada` y el letrero dice "CERRADO · PRÓXIMAMENTE".

### D-alan-6 La puerta deja salir (T-042)
Propuesta. Si el jugador está dentro de un lugar que cierra, la puerta le abre para que pueda salir.

### D-alan-7 Integración de la pelea en una tarea aparte (T-044)
Propuesta. Colocar MechaJugador y FlujoPelea en Clandestino.unity es T-044, separada de T-020, porque depende de que T-022 de
Tony esté en main y así T-020 no queda a medias esperando.

### D-alan-8 Interacción de prueba con E (T-042)
Propuesta. La cámara libre abre puertas con E (rayo de 3 m). La interacción real del jugador con puertas es de Tony (T-F3) y usa
el mismo `IPuerta`.

## Planeación de A-F2 (2026-10-13, planificador Opus 5.5, a pedido de Alan)

### D-alan-9 IDs de A-F2: T-074 a T-085
Propuesta. Tony ya usó hasta T-073 en tony/fase-3 y tony/fase-4, así que A-F2 toma T-074 a T-085. La siguiente planeación de
cualquiera de los dos empieza en T-086 (revisar antes las ramas de los dos: `git ls-tree -r --name-only <rama> docs/tareas`).

### D-alan-10 Física del carro (T-074)
Propuesta. `Rigidbody` dinámico con suspensión por rayos (sin `WheelCollider`): más fácil de controlar y de probar, y sube
banquetas de 20 cm. Valores iniciales en `ConfigCarro_Viejo`: 1100 kg, máxima 16 m/s (58 km/h, es un carro viejo de barrio),
reversa 5 m/s, de 0 a 14 m/s en unos 6 s, dirección 32° parado y 10° a la máxima.

### D-alan-11 Teclas del carro (T-074, T-075)
Propuesta. W o flecha arriba acelera, S o flecha abajo frena y da reversa (ya detenido), A/D o flechas giran, Espacio es freno de
mano, C cambia de cámara y el ratón mira. La E es del jugador (contrato `IMontable`): el carro nunca la lee. El carro solo lee su
entrada mientras está ocupado.

### D-alan-12 Cámara del carro (T-075)
Propuesta. Primera persona por defecto (personaje-progresion.md: primera persona; tercera opcional al manejar). C alterna con
tercera persona; la elección se recuerda entre subidas. En primera, el ratón mira ±110° a los lados y vuelve sola al frente.

### D-alan-13 Cuándo se puede bajar (T-075)
Propuesta. `PuedeBajar` con el carro a menos de 0.5 m/s (igual que el `CarroDePrueba` de Tony en T-058) y con una salida libre:
del lado del conductor y, si está tapado, del copiloto. Si los dos lados están tapados, no se baja.

### D-alan-14 La cámara libre se sube a montables (T-075)
Propuesta. Para probar el carro sin el jugador de Tony, la cámara libre sube con E al `IMontable` más cercano a 2.5 m del
`PuntoSubida` y a 60° de su mirada (la misma regla que D-tony-22) y cumple el contrato como lo haría el jugador.

### D-alan-15 Finales de las calles (T-077)
Propuesta. Los 6 extremos de calle se cierran con algo visible y sólido: retén corporativo con patrulla y torretas en el este y
en los dos del norte (bajo la autopista), obra en el oeste y en los dos del sur. Nada de colliders invisibles.

### D-alan-16 Tráfico mínimo (T-084)
Propuesta. Solo en la calle principal (una vuelta cerrada con vuelta en U frente a los cierres, 4 carros) y en la autopista
elevada (decorado, 8 carros que reaparecen lejos, entre la niebla). Las calles norte-sur solo tienen carros estacionados, para no
tener bloqueos en los cruces. Los carros de tráfico no son `IMontable` (no se roban carros, ciudad.md).

### D-alan-17 El "minuto y medio cruzando" (T-078)
Propuesta. ciudad.md dice unas 6 manzanas y alrededor de 1 minuto y medio cruzándolas en carro; con 16 m/s la calle principal de
268 m se cruza en unos 20 s. En A-F2 se mide (casa al clandestino y punta a punta) y no se cambian las medidas del barrio;
agrandar el barrio o hacer rutas más largas se decide con esas cifras en la revisión (A-F4 si hace falta).

### D-alan-18 Arte de Blender (T-076, T-082, T-085)
Propuesta. Las fuentes de Blender (scripts .py, .glb y renders) viven en `Assets/Arte/Fuentes~/` (Unity ignora las carpetas con
`~`). Como el proyecto no tiene glTFast, todo entra a Unity como FBX exportado por Blender headless, en `Assets/Arte/<Área>/`.
Los materiales URP viven en `Assets/Arte/Materiales/` y se crean desde `materiales.json`, que escribe el script de Blender; un
`AssetPostprocessor` de Mundo/Editor remapea por nombre. Los .glb salen de `Assets/Arte/Mundo/Bar/`. Las fuentes las copió el
planificador desde el scratchpad de la sesión que las hizo, para que no se pierdan.

### D-alan-19 Posproceso en las escenas de Alan (T-079)
Propuesta. Ciudad.unity y Pruebas_Alan.unity llevan Volumes globales (PerfilNoche y PerfilDia en Mundo/Datos/) que
`CicloDiaNoche` mezcla por hora. Las cámaras de Alan (cámara libre, carro) tienen el posproceso prendido. Pedido a Tony: prender
`renderPostProcessing` en la cámara de `JugadorAPie` y en la de la cabina del mecha para que el neón brille también para el jugador.

### D-alan-20 Letreros con palabras genéricas (T-082)
Propuesta. Mientras los nombres de los locales sigan abiertos (estetica-hud.md), los letreros dicen palabras genéricas: CANTINA,
TACOS, FARMACIA, HOTEL, BIRRIA 24H, CIBER, REFACCIONES, LAVANDERÍA, PELEAS y CASINO.

### D-alan-21 Orden de A-F2 y de CreadorTodo
Propuesta. Las tareas se hacen una tras otra (casi todas regeneran Barrio.prefab y las escenas, así que en paralelo chocarían):
T-074, T-075, T-076, T-077, T-078 (cierre de H3), T-079, T-080, T-081, T-082, T-083, T-084, T-085.
Orden final de `CreadorTodo.Crear`: CreadorArte, CreadorArena, CreadorBarrio (que llama a CreadorFinales, VestirCalle,
VestirFachadas y VestirLetreros), CreadorReloj, CreadorLugares, CreadorBar, CreadorCarro, CreadorTrafico, CreadorAmbiente y
CreadorPeleaClandestino. Correrlo dos veces no cambia ningún archivo (los reflejos solo se hornean si faltan).

### D-alan-22 T-060 y T-061 de Tony siguen bloqueadas
Revisado el 2026-10-13. T-060 necesita en main el barrio, el reloj, las puertas y el bar de A-F1 (main todavía no tiene
`Mundo/Lugares/`: alan/fase-1 espera aprobación) y T-059 de Tony (tampoco en main). T-061 necesita el carro de Alan con
`IMontable` en main (`Vehiculos/` en main solo tiene su asmdef): llega con T-075 y T-078 cuando alan/fase-2 se apruebe. Las dos
se quedan en docs/tareas/bloqueadas/. Pendiente aparte, sin tarea todavía: colocar `JugadorAPie.prefab` en Ciudad.unity y
Pruebas_Alan.unity (D-tony-25), cuando T-059 esté en main.
