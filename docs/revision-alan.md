# Revisión nocturna de Alan

Rama para probar: `alan/fase-1`. Actualizado: 2026-10-13.

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
