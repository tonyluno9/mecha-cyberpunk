# Revisión nocturna de Alan

Rama para probar: `alan/fase-5` (T-093 incluida). Actualizado: 2026-10-27. A-F1 a A-F4 ya están en main (T-084 y T-085 no se hicieron); las afirmaciones de abajo de que A-F3 y A-F4 "esperan a main" son del pasado.

## Qué probar primero
1. Escena Clandestino.unity, Play (la cámara es la de la cabina del jugador).
2. Juzga: neón cyberpunk con el centro legible, escala de mecha (reja de 6 m), y si la pantalla de 8×4.5 se ve (hoy solo asoma arriba; subir o inclinar la cámara si molesta).

## Por tarea

### T-020 Arena greybox del clandestino
- Verificado automáticamente: todos los criterios (compilar, EditMode 181/181, PlayMode 113/113; capturas docs/capturas/T-020-1.png y -2.png, idénticas).
- Para ti: neón, niebla, escala y visibilidad de la pantalla (ver arriba).
- Decisiones del agente: ninguna relevante.

### T-040 Barrio greybox y cámara libre
- Verificado automáticamente: todos los criterios (EditMode 188/188, PlayMode 135/135; capturas docs/capturas/T-040-1..3.png).
- Para ti: Pruebas_Alan.unity, Play, WASD+ratón (Shift corre, F vuela) de la casa al clandestino: vueltas, calles no vacías, finales creíbles, escala de persona y de mecha.
- Decisiones del agente: casa en Manzana_4; muros en vez de calles perimetrales; jaula a 400 m de la cámara.

### T-041 Reloj y día y noche
- Verificado automáticamente: todos los criterios (EditMode 206/206, PlayMode 144/144; capturas T-041-1/2). La prueba x30 corre 45 s reales (>1 día de juego), no los 3 min literales.
- Para ti: Pruebas_Alan.unity, Play: N=22:00, M=12:00, T=x30; que amanecer y anochecer no sean saltos bruscos, que la noche se sienta fría con niebla y farolas (en la captura el cielo es casi negro).
- Decisiones del agente: LuzNocturna con MaterialPropertyBlock; CicloDiaNoche pinta el fondo de la cámara como la niebla; CreadorReloj ya no pisa los configs (T-045).

### T-042 Locales, puertas y horarios
- Verificado automáticamente: todos los criterios (EditMode 213/213, PlayMode 160/160; capturas T-042-1..3; consola x30 3 min sin errores).
- Para ti: Ciudad.unity, E y luego W frente al bar: que la puerta abra sin estorbar; de noche, que se vea de un vistazo qué está abierto; el estado del letrero del casino se lee pequeño a ~13 m.
- Decisiones del agente: ninguna relevante.

### T-043 Bar interior
- Verificado automáticamente: todos los criterios (EditMode 213/213, PlayMode 165; capturas T-043-1..3; 3 min con 18 ciclos entrar/salir sin errores).
- Para ti: Pruebas_Alan, 22:00, puerta del bar, E y W: sin tirón al cruzar; el interior sale algo oscuro (muros y techo); tamaño entre mesas.
- Props de Blender (barra, mesa, DJ, letrero) en Assets/Arte/Mundo/Bar/, aún no colocados en el bar.

### T-044 Pelea en Clandestino (cierre de H1)
- Verificado automáticamente: todos los criterios (EditMode 213/213, PlayMode 166/166; capturas T-044-1/2).
- Para ti: Clandestino.unity, Play, pelea tres veces con R: que la luz de la jaula deje leer al rival y su aviso naranja, que la pantalla se lea desde la cabina y que se sienta igual que en Pruebas_Tony.
- Decisiones del agente: colector propio de errores/excepciones en vez de LogAssert (el "[Combate] KO" de Tony es un Debug.Log y no cuenta como fallo).

### T-045 Correcciones de la revisión de la fase 1
- Creadores: nuevo `CreadorTodo.Crear` (Mundo/Editor) corre CreadorArena, CreadorBarrio, CreadorReloj, CreadorLugares, CreadorBar y CreadorPeleaClandestino en orden; los demás avisan en su encabezado. Correr alguno suelto borra lo posterior.
- CreadorReloj y CreadorLugares ya no pisan assets existentes. RelojMundo publica con cola (reentrada segura). ArenaPelea.Centro está en la superficie del piso (y = 0).
- Menores: PuertaLocal no cierra con alguien en el umbral; LugarMundo avisa si el reloj aparece tarde (LetreroLugar se actualiza); prueba de Canvas PantallaPelea a menos de 0.1 m. Sin prueba propia: el aviso tardío del reloj en LugarMundo.

### T-074 Carro: física y manejo
- Verificado automáticamente: 12 criterios (EditMode 234/234, PlayMode 182/182; captura T-074-1).
- Para ti: abre CarroViejo.prefab sobre un piso, TomarControl(true), Play: W S A D Espacio; acelera sin prisa, frena firme, no derrapa solo; escalón de 0.2 m a 5 m/s da un brinquito.

### T-076 Arte de Blender a Unity
- Verificado automáticamente: 13 FBX, 31 materiales, 13 prefabs en Mundo/Prefabs/Kit (EditMode 241/241, PlayMode 182/182; capturas T-076-1/2). La prueba de orientación usa vértices Neon_* porque la original (promedio de z de Concreto_Manchas) estaba mal definida.
- Para ti: CreadorArte.Vitrina() en una escena sin guardar: colores iguales a los renders de Blender, neón que brille (el bloom llega con T-079), nada negro, rosa ni volteado.

### T-075 Subir y bajar del carro, cámaras
- Verificado automáticamente: 9 criterios (EditMode 255/255, PlayMode 201/201; capturas T-075-1/2; 10 subidas y bajadas sin errores).
- Para ti: Pruebas_Alan con cámara libre: a <2.5 m de la puerta del conductor, E sube y E baja (inmediato, sin saltos); dentro, WASD; C alterna primera y tercera persona sin marear ni temblor.

### T-077 Finales del barrio y retenes
- Verificado automáticamente: todos los criterios medibles (EditMode 262/262, PlayMode 210/210; capturas T-077-1/2; carro a 40 m con W 8 s en los 6 extremos sin salir).
- Para ti: de noche, en el extremo norte y el este: que el retén se entienda como cierre y no como muro de videojuego; la torreta llama la atención sin cegar; el jersey y la reja salen oscuros (un poco más de luz sería un extra).

### T-078 El carro en el barrio (cierre de H3, lado de Alan)
- Verificado automáticamente: EditMode 262/262, PlayMode 214/214; carro único junto a la cochera en Ciudad y Pruebas_Alan; recorrido casa a clandestino en 14.1 s con giro de 130°; 165 s de manejo en vivo sin errores; capturas T-078-1/2/3.
- No cumple: `CreadorTodo.Crear` dos veces deja la misma lista de archivos pero no los mismos bytes (p. ej. `_Color` de Barrio_Reja.mat cambia entre corridas); es el ruido de creadores viejos, pendiente de arreglar.
- Para ti: Pruebas_Alan, Play: de la puerta de la casa al carro, E, W y A/D hasta el clandestino, C cambia cámara, bajar con E. Que la vuelta y el manejo se sientan naturales. La captura 3 no enmarca la puerta del clandestino.
- **Decisión tuya:** el barrio se cruza en unos 20 s con el carro a 16 m/s; ciudad.md pide "minuto y medio". Opciones: agrandar el barrio o bajar la velocidad máxima.
- T-061 (subir al carro, de Tony) se desbloquea cuando alan/fase-2 llegue a main; teclas del carro y regla `PuedeBajar` en el encabezado de `CarroViejo.cs`.

## Pedido a Tony (T-079, propuesta D-alan-19)
- Prender `renderPostProcessing` (UniversalAdditionalCameraData) en la cámara de MechaJugador para que el jugador vea el bloom y el color de noche.

### T-079 Noche cyberpunk: posproceso, neón con bloom, asfalto mojado
- Verificado automáticamente: EditMode 269/269 y PlayMode 220/220 en dos corridas seguidas; luminancia V1 22:00 = 0.362 (+79 % sobre la base 0.203), 0.00 % saturado; capturas T-079-1..5; 2 min a x30 sin errores. Ronda 1: se perdía el keyword _EMISSION tras EditMode; arreglado con CreadorAmbiente.AsegurarEmision.
- Para ti: Pruebas_Alan, Play, `VistasReferencia.Ir(1, 22)`: halo del neón sin cegar, asfalto que se sienta mojado, fachadas legibles. `Ir(1, 13)`: el día no se ve gris. Flojos: la vista aérea V4 (0.12) sigue algo plana; el día (0.25) es más oscuro que la noche (0.36); los charcos son manchas suaves, no reflejos nítidos.
- Pedido a Tony: prender `renderPostProcessing` en la cámara de MechaJugador y de JugadorAPie. Clandestino.unity no recibe este posproceso.

## Bloqueos y preguntas
- Nada todavía.

## A-F2 (rama alan/fase-2)

### T-080 La calle vestida con el kit urbano
- Verificado automáticamente: todos los criterios (EditMode 463/463, PlayMode 433/433 tras mezclar main; capturas T-080-1..4).
- Para ti: Ciudad.unity, `VistasReferencia.Ir(1,22)` y `Ir(1,13)`: densidad de cables, puestos y basureros; caminar por la banqueta y pasar en carro sin rozar postes.
- Decisiones del agente: colliders de transformador, puesto y basurero achicados al cuerpo.

### T-081 Fachadas con detalle
- Verificado automáticamente: todos los criterios (EditMode 467/467, PlayMode 437/437; capturas T-081-1..5). Triángulos del barrio 201 692 → 792 334.
- Para ti: Ciudad.unity, `VistasReferencia.Ir(1,22)` y `Ir(1,13)`: barrio denso y habitado, zona pobre (Manzanas 1 y 4) distinta del centro; muros lisos donde el vecino es más bajo (conocido).
- Decisiones del agente: Fachada_Modular se achica en Z para no pasar de 0.45 m; emisión RealtimeEmissive 1.
- Pendiente de confirmar: los EXR de Mundo/Reflejos no cambiaron en el commit; comprobar que las sondas reflejan las fachadas (rehornear con CreadorAmbiente.HornearReflejos() en el editor).

### T-082 Letreros de neón
- Verificado automáticamente: todos los criterios (EditMode 476/476, PlayMode 444/444; capturas T-082-1..4). Reflejos rehorneados: los 4 .exr cambiaron (solo los letreros están marcados ReflectionProbeStatic; el resto del barrio sigue fuera del horneado).
- Para ti: Ciudad.unity a las 22:00: ¿"río de neón"? En la captura 1 los letreros se ven pequeños y escasos; cantina del bar y CASINO no tienen captura, míralos de lejos; parpadeo de 1 de cada 6 (neón viejo).
- Decisiones del agente: ParpadeoNeon propio en lugar de LuzNocturna; banderas a 1.5×; marcar todo el barrio para reflejos cambiaría los charcos (siguiente paso posible).

### T-083 El bar con los props de Blender y mejor luz
- Verificado automáticamente: EditMode 476/476, PlayMode 447/447; capturas T-083-1..3. Luminancia +57 % y 10 luces: solo de la bitácora, no remedido. No confirmado: 3 minutos entrando y saliendo con la consola abierta.
- Para ti: Ciudad.unity o Pruebas_Alan.unity a las 22:00, entra al bar y camina entre mesas, barra y DJ; ¿dan ganas de quedarse?, ¿algo fuera de escala? (techo algo oscuro, letreros pequeños). Deja 3 minutos entrando y saliendo y mira la consola.

### Cierre de A-F2 e integración a main (2026-10-16)
- `bash herramientas/verificar.sh todo` en alan/fase-2: compilar OK, EditMode 476/476, PlayMode 447/447 (22 min). Alan autorizó integrar sus fases verificadas: alan/fase-2 se mezcló a main (6be7940) y se subió, sin force push.
- **T-084 (tráfico de fachada) y T-085 (modelo del carro viejo) siguen PENDIENTES.** El modo Auto negó lanzar sus implementadores; no se hicieron ni verificaron por otra vía. Están en docs/tareas/pendientes/. Así que A-F2 entra a main sin ellas: el carro usa su modelo actual y no hay tráfico.
- Las pruebas regeneran capturas de T-074/T-075 y cambian `SENTIS_ANALYTICS_ENABLED` en ProjectSettings; se descartaron con `git restore`.
- A-F3 planificada: T-086 a T-090 (pendientes, independientes) y T-092, T-093 (bloqueadas por `IInteractuable`/`IGuardable` y T-F4/T-F5 de Tony). Rama alan/planificacion-fase-3.

### T-086 Casa por dentro con el taller
- Verificado automáticamente: compilar OK, EditMode 476/476, PlayMode 452/452 (CasaInteriorPruebas: 1 solo `ITallerMecha`, cilindro de 2 m × 5 m libre, Estacionamiento mira a la salida, 36 rayos sin huecos a 1 y 3 m, de la banqueta al centro sin cargas).
- Capturas tomadas y revisadas: docs/capturas/T-086-1.png (taller, vacío porque el mecha lo coloca Tony) y T-086-2.png (sala con cama), con CapturaPruebas. Luz fría del taller bajada a 4.5 tras ver la primera. Tras repetir: PlayMode 453/453.
- NO hecho: la salida ancha de 6 m (la casa mide 14 × 12 y el vestíbulo es de 3 m; el mecha llega del cielo, el Estacionamiento mira hacia la puerta). Interior de 13 × 7.5 × 5.6 m.
- Para ti: Ciudad.unity, entra a la casa de noche: ¿el taller (izquierda) y la sala con cama (derecha) se ven bien y caben? Reabre luces: 5 luces bajo ZonaInterior.

### T-087 Cama y espejo (solo lo visual)
- Verificado automáticamente: compilar OK, EditMode 476/476, PlayMode 455/455 (props con collider, PuntoUso dentro de la casa, mirando al prop y libre; captura docs/capturas/T-087-1.png revisada).
- Límite: los 3 props (Prop_Cama, Prop_Espejo, Prop_Mesita en Mundo/Prefabs/Kit/) son cubos con materiales, no modelos de Blender (el MCP de Blender no se usó); el espejo es un cristal reflectante barato, sin reflejo real.
- Para ti: Ciudad.unity, entra a la casa: ¿la cama, la mesita con lámpara y el espejo se ven bien? ¿quieres modelos de Blender más detallados?

### T-088 Callejón de la emboscada y punto de llamado
- Verificado automáticamente: compilar OK, EditMode 476/476, PlayMode 460/460 (CallejonPruebas: Llegada con cilindro 3 m × 6 m libre y cielo abierto, radio que cubre la boca y deja fuera los interiores, camino a pie con SphereCast, rivales de la emboscada en piso libre; capturas T-088-1 y T-088-2 revisadas).
- Cambios: Manzana_4 deja libre un callejón de 30 × 10 m abierto a la calle del este (CreadorBarrio, `CallejonX0/Z0/Z1`); `PuntoLlamadoMundo` (radio 14 m, llegada x=28); punto de aparición `Callejon_Entrada` (BarrioPruebas ahora espera 4 puntos); luces y neón del callejón con LuzNocturna.
- Decisión: NO coloqué `Emboscada.prefab` en el barrio, porque se activa sola al acercarse cualquier IJugador (incluida la cámara libre) y cambiaría la escena de Alan (D-029). El callejón publica `Emboscada_Anclaje` (+Z hacia el fondo) y la prueba instancia el prefab ahí. T-073 de Tony debe instanciarlo en ese anclaje.
- Límites: el radio de llamado solo cubre el callejón y su boca, no la calle frente a la casa; las poses de los rivales (14 m, ±1.5 m) están copiadas de DatosEmboscada_Base en la prueba; el callejón usa cubos, no modelos de Blender.
- Para ti: Ciudad.unity de noche, `Callejon_Entrada`: ¿el callejón se siente apto para una pelea y para que baje el mecha?

### T-089 Clandestino por dentro
- Verificado automáticamente: compilar OK, EditMode 476/476, PlayMode 464/464 (ClandestinoInteriorPruebas: de la banqueta a la puerta de la fosa en < 20 s sin cargas, 36 rayos sin huecos a 1 y 3 m, 8 luces apagadas con la cámara lejos y prendidas dentro; capturas T-089-1 y T-089-2 revisadas).
- LÍMITE IMPORTANTE: la jaula real (24 × 24 m, Arena/JaulaClandestino.prefab) NO está dentro del edificio de la ciudad: el edificio mide 24.67 × 24.67 m y no cabe con gradas. Lo que hay es una fosa de peleas visual (plataforma 10.4 m con rejas y puerta, gradas con 69 figurantes de cápsula, bar chico) y la pelea sigue en Clandestino.unity. Los puntos de la arena no se tocaron. Unir las dos (agrandar el edificio, o decidir cómo se pasa a la arena) queda como decisión para Alan y Tony; el criterio "llegar al borde de la jaula" se cumplió con la puerta de la fosa visual (`BordeFosa`).
- Para ti: Ciudad.unity de noche, entra al clandestino: ¿la fosa y las gradas se ven bien? ¿quieres agrandar el edificio para meter la jaula real?

### T-090 Flashback.unity
- Verificado automáticamente: compilar OK, EditMode 476/476, PlayMode 468/468 (FlashbackPruebas: carga sin errores ni cámara, punto `Flashback_Inicio` a pie con piso y espacio libre, rayos cerrados salvo la puerta; captura docs/capturas/T-090-1.png revisada).
- Qué es: cuarto de 10 × 8 m de tarde (luz cálida, posproceso PerfilDia), mesa con dos sillas de adulto y una de niño, maletas junto a la puerta y el mecha cubierto con una lona (cubo gris, no es el mecha de Tony). Del diseño solo salía "de niño tus papás se fueron y te dejaron el mecha"; el lugar y la hora son mi interpretación.
- Límites: la escena no lleva cámara (la pone el jugador de Tony); no hay secuencia, diálogos ni cambio de escena (T-F6); el porche no tiene calle ni cielo detallado.
- Para ti: abre Flashback.unity y dime si el lugar (cuarto de la casa, de tarde) coincide con lo que imaginas.

### T-094 Exteriores cerrados del casino y el mercado negro
- Verificado automáticamente: compilar OK, EditMode 476/476, PlayMode 471/471 (CerradosPruebas: siguen cerrados a las 12:00 y a las 22:00, nada sobre la Entrada ni más de ~2 m hacia la calle; capturas T-094-1..4 de día y de noche revisadas). Una primera versión con colliders rompió 3 pruebas (puertas y franja de caminar de KitCalle); por eso todo lo nuevo es decorativo, sin collider. Las capturas del mercado salieron en niebla pura con la cámara a 6 m (quedaba dentro de un edificio) y se repitieron a 3.2 m.
- Qué hay: casino con marquesina, columnas doradas, alfombra, postes con cadena y tubos de neón tenues; mercado con cortina de 8 tablillas, guías, candados, cajas y manta "VOLVEMOS PRONTO". El cartel "CERRADO" ya lo daba el letrero de horario de T-042, no hice otro.
- Límites: la cortina y la manta son cubos y texto de Canvas; de noche el mercado se ve muy oscuro y azul (T-099 lo ajusta con la luz final).
- Para ti: Ciudad.unity, `Lugar_Casino` y `Lugar_MercadoNegro` de día y de noche: ¿se leen como cerrados y con carácter?

### T-095 Grafitis
- Verificado automáticamente: compilar OK, EditMode 476/476, PlayMode 474/474 (GrafitisPruebas: 33 decales —entre 30 y 60—, ninguno con collider, cada uno mira hacia afuera con muro o fachada detrás y nada pegado delante; capturas T-095-1 y T-095-2 revisadas). Triángulos: no los medí; son 33 cuads de 2 triángulos (66 en total).
- Qué hay: 8 texturas procedurales con alfa (estrella, calavera, firma en zigzag, espiral, ojo, rayo, flecha, cruz con aro) con materiales `Grafiti_N` (URP/Lit con recorte alfa y un poco de emisión); 25 en muros que dan a la calle de las Manzanas 1 y 4 y 8 en las paredes del callejón de T-088.
- Límite: las fachadas de T-081 sobresalen hasta 0.45 m, así que los decales flotan a 0.5 m del muro (con 3 cm quedaban escondidos detrás de las fachadas): se leen como paneles pintados delante de las cortinas, no pintura sobre el muro. La captura 1 salió muy clara/lavada (no sé por qué esa toma no se ve de noche; el reloj lo fijó la prueba a las 22:00). Las texturas son simples (formas, no letras reales).
- Para ti: Ciudad.unity de noche en la zona pobre y en el callejón: ¿los grafitis aportan o estorban? ¿quieres otros diseños?

### T-096 Lluvia y niebla
- Verificado automáticamente: compilar OK, EditMode 481/481 (CalculoLluviaPruebas, 5 nuevas), PlayMode 480/480 (LluviaPruebas: de noche afuera cae; a las 12:00 no; con `LluviaSigueJugador.Activa = false` se apaga y vuelve; dentro del bar se apaga y afuera vuelve; ningún frame de más de 100 ms con la lluvia corriendo y hay gotas vivas; capturas T-096-1 y T-096-2 revisadas, la 2 muestra la lluvia y la calle). La última corrida completa (todo) fue antes de forzar `AlwaysSimulate`; después corrí PlayMode y EditMode por separado (compilar entra en ambos).
- Qué hay: `LluviaSigueJugador` (Mundo/Ambiente) con dos ParticleSystem —gotas estiradas y nubes de niebla baja— que siguen a `Registro<IJugador>.Actual` o a la cámara principal, con números en `ConfigLluvia_Mundo` y la regla pura `CalculoLluvia`. El objeto `Lluvia` va en la raíz de Barrio.prefab. Interruptor para el director del prólogo: `LluviaSigueJugador.Activa` (estático, se reinicia al entrar a Play).
- Un fallo real que encontré: con el culling automático los sistemas se pausaban y nunca emitían; se arregló con `cullingMode = AlwaysSimulate`.
- Límites: la niebla de noche por hora ya existía (ConfigCicloDia); no cambié su densidad. La niebla baja es muy tenue y casi no se ve en las capturas; las gotas son líneas finas sin salpicaduras ni sonido. Se ve la lluvia en cámara de pruebas; con el jugador de Tony (IJugador.Raiz) no se probó.
- Para ti: Ciudad.unity de noche, ¿la lluvia se ve bien, muy fuerte o muy leve? Los números están en ConfigLluvia_Mundo.

### T-097 Audio ambiental de la ciudad
- Verificado automáticamente: compilar OK, EditMode 486/486 (CalculoAmbienteSonoroPruebas, 5 nuevas), PlayMode 483/483 (AmbienteSonoroPruebas: de noche afuera suenan lluvia y neón y no la música; dentro del bar sube la música, baja la lluvia y se va el neón; al salir vuelve todo; a las 12:00 solo tráfico; con `LluviaSigueJugador.Activa = false` no suena lluvia). Los 4 WAV existen, se importan y el componente los usa (4 `AudioSource` con su clip y bucle cuando hay un listener).
- Problema encontrado y resuelto: EscenasMundoPruebas (Ciudad y Pruebas_Alan) fallaba con "There are no audio listeners in the scene". Lo aislé corriendo solo esa clase (falla igual) y desactivando el componente (pasa): el componente creaba las fuentes al ver un `AudioListener` que Unity no cuenta (no activo o con el componente deshabilitado). Ahora solo arranca con uno `isActiveAndEnabled` y detiene y destruye las fuentes si lo pierde. Un primer intento (crear las fuentes tarde, sin comprobar `isActiveAndEnabled`) no bastó.
- Limpieza del commit provisional 3dbd763: quité del índice la escena temporal `InitTestScene…` que Unity crea al correr pruebas (nuevo commit, sin reescribir la rama). La reescritura grande de Barrio.prefab, escenas y JaulaClandestino.prefab es normal: CreadorTodo los regenera completos (pasó igual en T-094 a T-096); lo que cambió de verdad es el objeto `AmbienteSonoro` en la raíz del barrio.
- Límites: lo audible no lo oí (los clips son provisionales: ruido filtrado, zumbido de 100 Hz, ruido marrón y un bombo con bajo, todos generados por código en Assets/Arte/Audio/ y archivados por LFS); no hay sonido 3D por local ni música distinta por local, solo una capa de música al estar dentro de cualquiera; con el jugador de Tony (otro AudioListener) no se probó.
- Para ti: Ciudad.unity de noche con audio: ¿se oye bien la lluvia, el zumbido y la música dentro del bar, y se pueden cambiar los volúmenes en ConfigAudioCiudad_Mundo?

### T-098 Pulido de calles y finales (retén, obra, autopista)
- Verificado automáticamente: compilar OK, EditMode 486/486, PlayMode 486/486 (PulidoFinalesPruebas: el pulido no tiene ningún collider, así que FinalesCarroPruebas y CamaraLibreCarroPruebas siguen pasando; ≥100 franjas de paso de cebra, alcantarillas y grietas; la losa de la autopista queda fuera del barrio y sus carros avanzan). Después de esa corrida solo cambié la posición de la cámara de la captura 2 y repetí esa clase sola (3/3); capturas T-098-1..3 revisadas.
- Qué hay (`VestirFinales`, grupo `PulidoFinales` en Barrio.prefab): 8 pasos de cebra en las bocas de los dos cruces, 14 tapas de alcantarilla, 40 grietas, una autopista elevada al norte (losa de 340 × 16 m a 10 m de alto, pilares cada 17 m, barandales, postes y 12 carros falsos con faros y calaveras que recorren la losa con `TraficoAutopista`), y una grúa torre y un andamio detrás de la obra del oeste. Los retenes y las obras con máquinas ya existían (T-077) y no los toqué.
- Límites: todo es de cubos y sin collider (la autopista y la grúa quedan fuera del barrio, el carro no puede llegar); la autopista se ve en una toma elevada pero desde la calle queda tapada por el muro del norte; en la captura 2 la composición es confusa por los muros del final; no hay obra nueva en el este ni el sur; los carros de la autopista no proyectan luz sobre la losa. Los pasos de cebra son muy brillantes de noche.
- Para ti: Ciudad.unity, `VistasReferencia`/cámara libre en los cruces y en los finales norte y oeste: ¿la ciudad ya no se siente cortada con muros lisos? ¿falta algo que se vea desde la calle (la autopista casi no se ve)?

### T-099 Iluminación final y comparación con la referencia
- Verificado automáticamente: compilar OK, EditMode 486/486, PlayMode 487/487. `IluminacionFinalPruebas` captura las 4 vistas de referencia (VistasReferencia) a las 22:00 y a las 12:00 (docs/capturas/T-099-V*_22/12.png) y comprueba que ninguna salga lavada (< 25 % de píxeles saturados), aplastada (< 60 % casi negros) ni fuera de 0.05 a 0.75 de luminancia media. Medidas finales: media 0.15 a 0.63, saturados ≤ 0.8 %, negros ≤ 13 %; no hubo vista fuera de rango.
- Un solo ajuste: los pasos de cebra de T-098 salían casi blancos y brillantes de noche; bajé su color de 0.85 a 0.55 (VestirFinales). La vista 1 de noche pasó de media 0.414 a 0.403 y de 0.7 % a 1.2 % de negros; antes y después quedan en docs/capturas/T-099-V1_22_antes.png y T-099-V1_22.png. No toqué luna, ambiente, niebla ni posproceso: sus números caían en rango y no tengo la imagen de referencia para decidir otros cambios.
- Comparación honesta con la referencia (docs/diseno/estetica-hud.md: "niebla, reflejos en el asfalto mojado y neón azul, magenta, naranja y turquesa"): hay neón en esos colores, lluvia (T-096) y asfalto oscuro con charcos; faltan reflejos nítidos del neón en el asfalto, la niebla se ve poco (cielo azul plano sin brillo cerca de los edificios) y los edificios son planos de color; las tomas de día se ven correctas pero sin carácter. Eso es juicio mío a partir de capturas, no una medición contra la imagen.
- Para ti: mira las ocho capturas (o las vistas 1 a 4 en el editor) con la imagen de referencia al lado y dime qué falta; lo más probable es subir la niebla y los reflejos.

### T-092 Lo que el prólogo de Tony espera del barrio (reloj guardable, puntos y prefabs)
- Alcance real: al llegar origin/main (IInteractuable, IGuardable, T-F5 y los prefabs del prólogo de Tony, D-tony-67 y D-tony-72) esta tarea cambió: la cama y el espejo ya no los programo yo, Tony los hizo como prefabs que Alan coloca. Hice la parte de Alan: el reloj guardable, los puntos de historia, las apariciones y colocar esos prefabs.
- Verificado automáticamente: mezcla de origin/main en alan/fase-3 sin conflictos (compilar OK, EditMode 674/674, PlayMode 671/673 con 2 omitidas, 0 fallos); después de T-092: compilar OK, EditMode 678/678, PlayMode 676/678 (las mismas 2 omitidas de Tony), 0 fallos. Pruebas nuevas: CalculoRelojPruebas (+4), PrologoMundoPruebas (5): `RelojMundo` se registra como `IGuardable` con clave `reloj`, guardar y cargar conserva día y hora sin avisar `DiaCambio` ni `HoraEnteraCambio` (solo la franja), un texto inválido avisa y no cambia nada; `Prologo_Carro` y `Prologo_Esquina_1` (IPuntoHistoria) están libres y sobre piso; apariciones `Casa_Cama` (dentro de la casa) y `Clandestino_Salida` (en la Entrada); `CamaJugador` y `EspejoJugador` de Tony colocados en los puntos de uso de la cama y el espejo de Alan (su geometría greybox apagada para que se vean los props de Alan), registrados como IInteractuable y sin errores en consola.
- Cambios: `CalculoReloj.Restaurar`, `RelojMundo : IGuardable`, `PuntoHistoriaMundo`, `CreadorPrologoMundo`; BarrioPruebas ahora espera 6 puntos de aparición.
- Descubierto y dejado fuera: `MostradorPeleas.prefab` trae un `FlujoPeleaCasera` (DirectorPelea de Tony) que avisa con un warning cuando no hay un mecha del jugador al arrancar; puesto en Barrio.prefab rompió 5 pruebas con logs. Va junto con el mecha estacionado, el jugador y el director en Ciudad.unity (T-093). No hay captura de T-092: lo visible no cambió (los props son los de T-087).
- Límites: no probé dormir ni mirarse al espejo con el jugador de Tony (CamaJugador pide su fundido, historia y guardado); `Prologo_Carro` es una elección mía (4 m dentro de la calle y 7 m al lado de la Entrada del clandestino) y la ruta solo tiene una esquina; no hice los puntos `Flashback_Plano_N`.
- Para ti: ¿el carro queda donde lo esperas al salir del clandestino? Y a Tony le sirve saber que su cama y su espejo ya están en la casa de Alan.

### T-093 Recorrido del prólogo en la ciudad de Alan (H4/H5/H6, lado de Alan) — 2026-10-27
- Verificado automáticamente: compilar OK, EditMode 914/914, PlayMode 889/898 con 0 fallos (9 omitidas: capturas de Tony que se activan con variable de entorno). `RecorridoPrologoCiudadPruebas` (3) y `FlashbackPruebas` (+1, y su comprobación de cámara cambió a "una sola cámara, la del prefab de Tony") pasan. Capturas docs/capturas/T-093-flashback-plano-1, -4 y -6 revisadas.
- Qué hay (para Tony, T-136):
  - **`Assets/_Proyecto/Escenas/Ciudad_Prologo.unity`**: copia de Ciudad.unity (que sigue sin jugador para la cámara libre de Alan) con `JugadorAPie` (aparece en `Casa_Puerta`), `MechaEstacionado` (en el `Estacionamiento` del taller), `Emboscada` (en `Emboscada_Anclaje`), `DirectorPrologo` y `MostradorPeleas` (con su flujo de pelea y la jaula a 400 m, como en Pruebas_Alan). La escena la hace `CreadorPrologoCiudad.Crear` (o `CrearSoloPrologo`, que además rehace los puntos y Flashback sin tocar el resto del barrio). **Tony debe apuntar `DatosEscenas_Demo` a `Ciudad_Prologo.unity`**, no a Ciudad.unity (decisión mía para no meter un jugador en las pruebas de cámara libre de Alan: si prefieren, se puede renombrar).
  - El carro viejo queda en `Prologo_Carro` (junto al clandestino, mirando a la calle del regreso). Antes quedaba frente a la casa y nadie lo movía, aunque el objetivo apuntaba a `Prologo_Carro`.
  - Puntos de historia: `Prologo_Carro`, `Prologo_Esquina_1` (cruce) y **`Prologo_Esquina_2`** (calle del oeste frente a la puerta de la casa). Para usar las dos esquinas, `DatosPrologo_Base.pasosRuta` debe ser `Prologo_Esquina_1, Prologo_Esquina_2` (hoy solo lleva la 1; es dato de Tony). Apariciones `Casa_Puerta`, `Casa_Cama`, `Clandestino_Salida`; reloj guardable (`reloj`), carro `IMontable`.
  - `Flashback.unity`: `Flashback_Plano_1` a `_6` (cámara a 1.1 m, como un niño: mesa, mecha con lona, puerta desde la silla del niño, porche, maletas, mecha solo) y una instancia del prefab `Flashback` de Tony (director, cámara, subtítulos). El orden de los planos sigue los textos de `DatosFlashback_Base`.
- Pruebas del recorrido (entrada simulada W, A, D, S, E, Espacio; la prueba orienta la raíz del jugador porque no simula el ratón): (1) la escena publica todo lo que busca Tony y no da errores; (2) a pie: casa → calle → fondo del callejón y regreso → cruce → clandestino → carro, sin atravesar muros (llega a cada punto dentro de 60 s y su velocidad media no delata saltos); (3) subir al carro con E, manejar por las esquinas 1 y 2 hasta frente a la casa, frenar, bajar con E, abrir la puerta con E, entrar y llegar a los pies de la cama. Tiempos por tramo en `Logs/*.log` con la etiqueta `[Mundo] T-093`: unos 55 s de juego en total a pie y en carro, sin contar dormir ni los combates.
- Hallazgos de las pruebas: el carro no puede girar 90° en la esquina a más de unos 5 m/s (el conductor de prueba frena en las vueltas); la puerta de la casa mide 3 m y el jugador casi roza el marco si llega en diagonal; la mesa de la sala con sus bancos queda en el camino recto de la puerta a la cama, por eso la prueba rodea por el pasillo entre los bancos y los pies de la cama.
- Límites (honestos): **no probé el prólogo completo con el director de Tony** (dormir, la emboscada, el llamado del mecha y el cambio de escenas son T-136, de Tony); la prueba solo recorre lo físico. No probé el llamado ni la emboscada en marcha, ni entrar al clandestino por dentro con el mostrador. El plano 4 del flashback mira por la puerta a un cielo azul plano y un piso (el porche no tiene calle ni camioneta). No se probó el jugador de Tony subiendo al mecha dentro de la casa. Las capturas de otras tareas que regeneran las pruebas no se subieron.
- Limpieza: los cambios sin commit de una generación antigua (Barrio.prefab, escenas, 20 materiales) quedaron guardados en `git stash` ("alan-generacion-antigua-20261027") y en `../_respaldo-alan/cambios-sin-commit-20261027.patch`; no eran necesarios (CreadorTodo regenera todo igual).
- Para ti: Ciudad_Prologo.unity, Play: ¿el carro junto al clandestino y la ruta de regreso (dos esquinas) te parecen bien? ¿Los seis planos del flashback cuentan lo que quieres?

### T-300 Lógica pura de la tragamonedas (A-F5, casino) — 2026-10-27
- Verificado automáticamente: `CalculoTragamonedasPruebas` 8/8 (retorno esperado exacto entre 0.90 y 0.97, un millón de giros con semilla fija a ±0.02 del esperado, misma semilla igual resultado, pago por símbolo, dos cerezas pagan y dos de otro símbolo no, apuesta 0 o negativa no paga, datos inválidos lanzan). Corrida filtrada (compilar y esas pruebas); la verificación completa se hará al cerrar la siguiente tarea, no se repite aquí.
- Qué hay: `Casino/Tragamonedas/CalculoTragamonedas` (C# puro, azar inyectado), `Casino/Datos/DatosTragamonedas` y `DatosTragamonedas_Base.asset` (CEREZA 40, LIMON 25, CAMPANA 18, SIETE 10, NEON 7; tres iguales ×6, ×8, ×15, ×40, ×100; dos cerezas ×1; apuesta 50), `Casino/Editor/CreadorDatosTragamonedas` (y su asmdef). Retorno esperado = 0.9588 (calculado a mano y por la prueba).
- Límites: los números son de trabajo, no están balanceados contra la economía de Tony (los premios de pelea); la apuesta de 50 es provisional.

### T-330 Lógica de rutas de peatones y T-340 Lógica de persecución (A-F5) — 2026-10-27
- Verificado automáticamente: `MechaCyberpunk.Tests.Trafico` EditMode 15/15 (CalculoPeatonPruebas 8: llega a tiempo, espera en esquinas y sigue, da la vuelta en los finales, ida y vuelta, misma semilla repite, un paso grande iguala a muchos pasos chicos, datos inválidos lanzan, ConfigPeatones por defecto con más peatones de noche que de día y dentro de 12 a 30; CalculoPersecucionPruebas 7: 0/1/2/2 drones y 0/0/0/1 patrullas por nivel, nivel fuera de rango se recorta, el paso no rebasa la distancia de seguimiento, captura, datos inválidos). Corrida filtrada; la verificación completa del conjunto va en el cierre de alan/fase-5.
- Qué hay: `Trafico/Peatones/CalculoPeaton` (C# puro con azar inyectado; trabaja con distancias por tramo), `Trafico/Persecucion/CalculoPersecucion` (C# puro), `Trafico/Datos/ConfigPeatones` y `ConfigPersecucion` (ScriptableObjects con los números; todavía sin assets `_Base` porque nadie los lee hasta T-331 y T-341).
- Límites: nada de esto se ve en el juego todavía; los números (1.4 m/s, 12 y 24 peatones, drones a 9 m/s, seguimiento a 6 m) son de trabajo. Un detalle del peatón: en un final da la vuelta sin esperar y la espera de las esquinas usa el azar de la ruta.

### Cierre de alan/fase-5 (2026-10-27)
- `bash herramientas/verificar.sh todo` sobre alan/fase-5 (T-093, T-300, T-330, T-340 y la T-310 que otra sesión integró; Ciudad_Prologo regenerada con el cielo nuevo): compilar OK, EditMode 942/942, PlayMode 889/898 (0 fallos; 9 omitidas, capturas de Tony). Esto cubre el "verificar todo" que las tres tareas de lógica habían dejado como corrida filtrada.
- Fallo de Arena que anotó Tony (`JaulaClandestinoPruebas.Capsula_EmpujadaContraLaReja_NuncaSaleDeLaJaula`, 8/15): **no se reproduce** (15/15 en la corrida completa y en la anterior). La prueba mueve la cápsula con `Time.deltaTime`, así que si un cuadro dura mucho (máquina cargada) un paso grande puede atravesar la reja; no es un fallo de la jaula. Si reaparece, la corrección sería mover con un paso fijo; no se cambió porque no hubo fallo real.
- Ciudad_Prologo.unity es una copia de Ciudad.unity: cada vez que cambie Ciudad hay que rehacerla con `CreadorPrologoCiudad.Crear` (conserva su GUID). Main: **no se mezcló a main.** No encontré en el repo una autorización de Alan para este paso (solo la del 2026-10-16 para alan/fase-2); alan/fase-5 queda lista (sin conflictos con origin/main, que no avanzó) para que Alan la apruebe.
- Pendiente de la fase: T-301 (tragamonedas jugable), T-302, T-303, T-320, T-321 (espera a Tony), T-331 y T-341 están escritas en docs/tareas/pendientes/. T-310 y T-311 las llevan otras sesiones (worktrees mecha-cyberpunk-alan-visual y -fachadas); T-084 y T-085 no se hacen.
