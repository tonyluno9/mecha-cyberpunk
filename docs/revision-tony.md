# Revisión de Tony

Rama para probar: `tony/fase-2`. Turno de día en curso (2026-10-12).

## Por tarea

### T-030 Carpetas y ensamblados por dueño
- Verificado automáticamente: 6 de 7 criterios (EditMode 59/59, PlayMode 64/64); el séptimo es de juicio humano.
- Para ti: que las referencias de Mundo, Vehiculos, Casino y Trafico le alcancen a Alan para A-F1 y A-F2 (¿TextMeshPro?).
- Decisiones del agente: la regla "Core no referencia nada" solo aplica a MechaCyberpunk.Core.
- Se quitó del commit ProjectSettings/SceneTemplateSettings.json (lo generó el editor).
- Integrada a main porque Alan la necesita.

### T-031 Contratos de arena y jugador
- Verificado automáticamente: todos los criterios (EditMode 68/68, PlayMode 64/64).
- Para ti (o Alan): abrir Assets/_Proyecto/Core/Arena/IArenaPelea.cs y confirmar que los comentarios bastan para armar la jaula y orientar la pantalla (+Z) sin preguntar.
- Decisiones del agente: Registros.Apuntar público pero documentado como interno; Quitar(null) no hace nada.
- Sugerencia: prueba de PlayMode de Registro<T> con un MonoBehaviour real, en una tarea posterior.
- Integrada a main porque Alan la necesita.

### T-032 Contratos de reloj, lugares y puertas
- Verificado automáticamente: 6 de 6 criterios (EditMode 94/94, PlayMode 64/64).
- Para ti (o Alan): leer los /// de IRelojMundo, ILugar e IPuerta en Core/Mundo/ y confirmar que bastan para T-041 y T-042.
- Decisiones del agente: HorarioLugar normaliza abre/cierra (24 = 0); nombres PublicarEntro / PublicarSalio / PublicarEstadoLugar.
- Integrada a main porque Alan la necesita.

### T-021 Rival básico
- Verificado automáticamente: todos los criterios (EditMode 105/105, PlayMode 80/80; captura docs/capturas/T-021-1.png). La tecla C real no se probó en vivo (editor sin ventana); la cubre una prueba con teclado simulado.
- Para ti: en Pruebas_Tony, tecla C. ¿El aviso naranja da tiempo de subir la guardia (clic derecho) y bloquear en la raya se siente justo? ¿El rival se siente pesado y la caída en KO se entiende? Clic izquierdo para pegarle.
- Observación: el AtacanteDePrueba de la escena mata al jugador en unos 30 s y estorba; conviene apagarlo al probar.
- Decisiones del agente: límite de DistanciaMinima por distancia; espera entre golpes corre solo en Listo; RivalBasico.Start quita el control (T-022 lo activa un frame después).

### T-022 Flujo de pelea
- Verificado automáticamente: todos los criterios (EditMode 118/118, PlayMode 92/92; 3 peleas seguidas con revancha, ganadores Jugador, Rival, Jugador; capturas docs/capturas/T-022-1.png y -2.png).
- Para ti: Pruebas_Tony, Play, P para empezar, ganar o perder, R para revancha, 3 veces. ¿El ritmo del conteo y la pausa tras el KO se sienten bien? El "3" y "R: revancha" se ven pequeños y el "3" se solapa con la cabeza del rival; si molesta, se ajusta ConfigFlujoPelea_Base o el tamaño de la pantalla.
- Decisiones del agente: Pelea/ no referencia IA/; el director avisa el KO a la pantalla porque Core no tiene evento de KO; PrepararPelea no se llama al rival recién creado.
- Para H1 falta T-020 (jaula de Alan) y T-044: el flujo se prueba aquí con ArenaDePrueba.

### T-033 Contratos de cartera y ofertas
- Verificado automáticamente: todos los criterios (EditMode 137/137, PlayMode 92/92).
- Para ti (o Alan): leer ITienda y DatosOferta en Core/Economia/ y confirmar que los /// bastan para hacer el mostrador del mercado negro.
- Decisiones del agente: PuedePagar false con cantidad <= 0; Comprar con cartera null devuelve NoDisponible; id de oferta = nombre del archivo.
- Integrada a main porque Alan la necesita.

### T-034 Contratos de calor y objetivos
- Verificado automáticamente: 4 de 4 criterios (EditMode 161/161, PlayMode 92/92).
- Para ti (o Alan): leer los /// de ICalor e IObjetivos en Core/ y confirmar que queda claro cuándo persiguen los drones y cómo dibujar el objetivo.
- Decisiones del agente: Avanzar enfría solo lo que pasa del retraso; Enfriar y Borrar no reinician el retraso.
- Integrada a main porque Alan la necesita.

## Revisión de conjunto de T-F2 (revisor, Opus) y correcciones
- Veredicto: código aprobado; ninguna área referencia a un área del otro dueño. Corrida: EditMode 164/164, PlayMode 98/98 tras la corrección.
- Corregido en tony/T-F2-correccion (Core, integrada a main): orden de decisión de IPuerta/ILugar alineado con T-042; nombres de avisos de T-042 y T-043
  (PublicarEntro, PublicarSalio, PublicarEstadoLugar); MostrarKO no se borraba en un KO temprano; contrato y respaldo de la pantalla de la arena;
  aviso en HorarioLugar (0 a 24 queda cerrado: usa SiempreAbierto); Fijar(Ninguno) = Quitar; precio 0 comprable; DirectorPelea escucha Quitado y se re-suscribe;
  RivalBasico ya no se congela según el orden de Start; /// de ITienda; nota de "nadie lo registra todavía" en ICalor, ICartera, IObjetivos, ITienda e IJugador.
- Pendiente anotado (no es bug): ningún componente registra todavía ICalor, ICartera, ITienda, IObjetivos ni IJugador (llegan en T-F3 a T-F5);
  RivalBasico pasa siempre tieneControl=true a CerebroRival; el "3" del conteo queda pegado a la cabeza del rival en T-022-1.png (el contrato de la pantalla ya recomienda ponerla alta).

### T-050 Contrato para subir y bajar de un vehículo
- Verificado automáticamente: 6 de 6 criterios (EditMode 176/176, PlayMode 98/98).
- Para ti (o Alan): leer solo los /// de Core/Jugador/IMontable.cs y confirmar que responden sin dudas quién prende la cámara y el AudioListener al subir, quién lee la E y dónde queda el jugador al bajar.
- Decisiones del agente: Resolver deja posicion en cero al devolver false; IntentarBajar en false deja `salida` sin uso.
- Integrada a main porque Alan la necesita para el carro (A-F2).

### T-051 Jugador a pie
- Verificado automáticamente: todos los criterios (EditMode 192/192, PlayMode 118/118; captura docs/capturas/T-051-1.png; consola 0 errores en Play).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play, WASD, ratón, Shift para correr, Espacio para saltar. ¿Responde al instante sin patinar ni flotar? ¿El salto y la carrera se sienten ligeros, distintos del mecha? ¿La altura de ojos (1.65 m) no se siente enana ni gigante junto a los edificios y las banquetas del barrio en L?
- Decisiones del agente: clic con cursor libre lo vuelve a bloquear; Manos es hijo de Camara; valores de aceleración, frenado, gravedad y control en el aire en ConfigControlesAPie.

### T-055 Servicio de objetivos
- Verificado automáticamente: todos los criterios (EditMode 206/206, PlayMode 131/131). Sin criterios de sensación.
- Decisiones del agente: Llego se mantiene hasta que cambia el objetivo; PublicarEntro del lugar del objetivo cuenta como llegada aunque el jugador esté lejos; tecla O de prueba cambia de objetivo.
- Cubre el pendiente de la revisión de conjunto de T-F2 (IObjetivos ya se registra).

### T-054 Puertas desde el lado del jugador
- Verificado automáticamente: todos los criterios (EditMode 220/220, PlayMode 155/155; recorrido en vivo con consola sin errores; captura docs/capturas/T-054-1.png).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. Párate a unos 2 m de cada puerta (Bar x=8, Clandestino x=17, Casino x=26, todas en z=17) y pulsa E sin centrar la puerta; camina con W para entrar y gira para salir. ¿E responde al instante? ¿Hay tirones al cruzar el umbral? (El Clandestino debe decir cerrado por horario y el Casino bloqueado.)
- Observación para Alan: si se teletransporta al jugador desactivando su CharacterController dentro de una zona, el trigger no avisa la salida (JugadorDentro queda en true).
- Decisiones del agente: "no funciona en KO" usa ControladorAPie.ControlActivo (el jugador a pie aún no tiene vida).

### T-052 Vida y golpes a pie
- Verificado automáticamente: todos los criterios (EditMode 221/221, PlayMode 168/168 dos corridas; captura docs/capturas/T-052-1.png; consola 0 errores).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. Clic izquierdo frente a un muñeco: ¿el puño avanza y el muñeco parpadea justo al conectar, sin sentirse tardío? H varias veces hasta el KO: ¿la vista cae a 0.4 m y se ladea sin verse como error? K revive.
- Decisiones del agente: el jugador a pie NO es IPeleador (D-tony-15); el creador conserva ControlesAPie.inputactions si ya existe; tiempos y geometría del KO en DatosPersonaje.

### T-053 Pistola
- Verificado automáticamente: todos los criterios (EditMode 228/228, PlayMode 180/180 dos corridas; en vivo 32 disparos y 3 recargas, consola 0 errores; captura docs/capturas/T-053-1.png sin destello visible, la luz dura 0.05 s).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. Tecla 2, apunta a uno de los muñecos (16, 0, 4) y (16, 0, 7) y haz clic repetido: ¿se ven el destello, el retroceso corto y la marca en el muro, y se siente seco y claro? Dispara 8 veces y un clic más: ¿se entiende que el clic vacío recarga? Prueba R y 1 / 2 para cambiar de arma.
- Decisiones del agente: ArmasAPie conserva el arma y la copia a GolpeAPie; los avisos de HUD salen por el evento Cambio de ArmasAPie. Las pruebas usan blancos de 8 m de alto por el calentamiento de los primeros frames; en el juego la cámara está a 1.70 m.

### T-056 HUD a pie: vida, arma y canción
- Verificado automáticamente: todos los criterios menos la captura a 1920×1080 (EditMode 245/245, PlayMode 189/189; captura docs/capturas/T-056-1.png a 1280×720, hecha con el Canvas en ScreenSpaceCamera solo en memoria porque la herramienta no dibuja el Overlay).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play, vista Game a 1920×1080 y a 1280×720. ¿Se leen vida, arma y balas, canción y mira sin taparse? (El texto se ve pequeño y de contraste justo sobre el cielo; si molesta, se sube el tamaño en ConfigHudAPie_Base.) Cambia con 1/2, dispara, R, y H para recibir daño: ¿el HUD se siente mínimo y neón?
- Decisiones del agente: UI ahora referencia Personaje (mismo dueño); el HUD repite la lectura de vida un frame después de registrarse el jugador.

### T-057 HUD del objetivo y avisos de interacción
- Verificado automáticamente: todos los criterios (EditMode 264/264, PlayMode 206/206 dos corridas; recorrido en vivo con consola 0 errores; capturas docs/capturas/T-057-1.png, -2.png y -3.png).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. Camina con W siguiendo solo la flecha hacia el Bar: ¿se llega sin pensar? Camina hacia atrás unos 40 m: ¿el "REGRESA" magenta se nota sin molestar? Acércate a la puerta del Clandestino (x=17, z=17, mirando +Z) y pulsa E: ¿"CERRADO · abre 20:00" se entiende a la primera y no se queda pegado? Igual con el Casino.
- Observaciones: la flecha apunta en línea recta (se topa con el muro trasero del Bar, por diseño); la prueba de Carro sin aviso de interacción pasa casi por construcción (JugadorFalso sin InteraccionAPie).
- Pendiente (corrección T-F3): la captura docs/capturas/T-059-2.png muestra el aviso "E · Abrir Clandestino" y no el de cerrado por horario que pide el criterio; hay que recapturarla con el reloj fuera de 20:00-06:00 o verificar la hora de la demo.
- Decisiones del agente: flecha dibujada por código; UI y Personaje/Editor referencian Progresion (mismo dueño).

### T-058 Subir y bajar del carro (lado del jugador)
- Verificado automáticamente: todos los criterios (EditMode 276/276, PlayMode 218/218 dos corridas; en vivo el carro se movió ~16 m y 11 ciclos subir/bajar con consola 0 errores; capturas docs/capturas/T-058-1.png, -2.png y T-058-vivo-1/2.png).
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play. WASD hasta el carro rosa (-2, 0, 9), E para subir, flechas para manejar, E con el carro detenido para bajar. ¿Subir y bajar es inmediato y claro, y la cámara no marea? Al bajar debes quedar a la izquierda, fuera del collider y mirando hacia adelante.
- Decisiones del agente: la E la lee InteraccionAPie (siempre prendida); al subir se apagan ControladorAPie y ArmasAPie y se congela el control; el carro usa un .inputactions propio con las flechas.
- Para Alan (A-F2): el carro de Alan solo necesita implementar IMontable (contrato en Core/Jugador/IMontable.cs); CarroDePrueba es el ejemplo.

### T-059 Prefab completo del jugador a pie y recorrido
- Verificado automáticamente: todos los criterios (EditMode 276/276, PlayMode 229/229; capturas docs/capturas/T-059-1 a -3.png).
- Para ti: Pruebas_APie, Play, WASD y ratón siguiendo solo la flecha hasta la esquina, el Bar y el carro. ¿Nada se atora, la flecha basta, el HUD es legible?
- Pedido para Alan (D-tony-25): colocar Personaje/Prefabs/JugadorAPie.prefab en Ciudad.unity y Pruebas_Alan.unity con idAparicion "Casa_Puerta" y apagar su cámara libre.
- Observación: GolpeBloqueoPruebas.Golpe_ClicsSeguidos... es flaky (preexistente, de Combate).
