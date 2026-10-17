# Revisión de Tony

Estado al 2026-10-15: sesión de aprobación. Tony aprobó tony/fase-3 y tony/fase-4 sin revisión extra y están en `main`
(merge limpio; compilar OK, EditMode 375/375, PlayMode 304/304 sobre main).

## Core a main (2026-10-16)
Tony autorizó el merge: tony/fase-5 (T-F5, T-060, T-100, T-120 a T-123) está en main desde bc8a486, con alan/fase-2 ya mezclada (compilar OK, EditMode 613/613). T-100 y T-120 ya llegan a Alan.

## T-F5 cerrada: qué probar jugando (2026-10-16)
Todo en Pruebas_Calle (Play, sin ayuda), salvo lo marcado. Dobles de prueba: 7 y 8 dinero, J reloj (salta a las 08:00 del día siguiente), F5 guardar, F9 cargar, P iniciar pelea, M mandar el mecha al taller.
1. Sube al mecha (E), enciéndelo (F, Espacio, Q) y pelea (P). Gana con daño y pulsa R: ¿se nota que empiezas dañado (vida, alarma roja, grietas del cristal, más lento y peor manejo)? ¿"+$600 · PREMIO" se siente bien?
2. Baja (E), M manda el mecha al taller, camina al banco: ¿"E · Reparar mecha · $X" se entiende? ¿Quedar en negativo se siente como decisión tuya y no como error?
3. J hasta el día 8 sin dinero: ¿"RENTA VENCIDA" en el HUD y "TALLER BLOQUEADO" en el banco se entienden solos? Con 7, paga la renta en el banco.
4. ¿500 de renta por semana contra 600 por pelea ganada aprieta lo justo? ¿La reparación de ~$300-650 se siente "vivo al día"?
5. F5, cambia dinero, vida, perfil y reloj, F9: ¿todo vuelve como estaba?
6. Interactuables (T-108): en Pruebas_APie camina al interactuable de (4,0,12) y pulsa E: ¿responde al instante, estorba el aviso al pasar de largo?
7. Barrio de Alan (T-060): en Ciudad.unity, cuando Alan coloque el prefab, camina de la casa al clandestino y al bar: ¿la escala cabe? La flecha del objetivo se solapa con el horario del cartel.
Deudas y avisos: (a) PeleaClandestinoPruebas.TresPeleasSeguidasConRevancha (Alan) falla porque el daño del mecha persiste entre peleas (T-103): la prueba de Alan debe ajustarse o repararse el mecha entre peleas; (b) JaulaClandestinoPruebas.Capsula_EmpujadaContraLaReja x8 y Cubo_SoltadoSobreJaula fallan en main puro (Alan); (c) CreadorEscenaCalle.Crear reordena objetos y rompe LlamadoMechaPruebas (por eso se agregan objetos con métodos idempotentes); (d) RivalBasicoPruebas.Ritmo_*, RecorridoEmboscadaPruebas y RecorridoAPiePruebas son intermitentes por tiempo de frame; (e) la arena de Pruebas_Calle es una jaula cerrada a 25 m, no se camina hasta ella; (f) el taller de la casa de Alan (H5 completo) espera A-F3.
Sin revisor: T-F5 no pasó por revisión nocturna de Opus (indicación de Tony para este turno).

## Resumen del turno (2026-10-15)
- Hecho: merge de tony/fase-4 (con fase-3) en main y push; decisiones D-tony-1 a 39 pasadas a docs/decisiones.md como D-033 a D-071,
  más D-072 (Pelea referencia Progresion); docs/estado.md y docs/estado-tony.md actualizados; docs/decisiones-pendientes-tony.md vaciado.
- Bloqueado: T-060, T-061 y T-073 siguen en bloqueadas porque alan/fase-1 no está en main (A-F1 a A-F3).
- No hay tareas de Tony pendientes: T-F5 está sin planificar. No se corrió revisión nocturna: Tony aprobó sin ella, así que las
  tareas de T-F3 y T-F4 pasan a main sin la revisión de conjunto del revisor (quedó sin resultado el 14-oct).
- Para un humano: lo de abajo (probar en Pruebas_APie.unity y Pruebas_Calle.unity cómo se siente el jugador a pie, el mecha y la
  emboscada) sigue sin probarse a mano. Capturas T-072-1/2 poco legibles; posible fragilidad en PandillaPruebas caso d y
  RecorridoEmboscadaPruebas (~50 s).
- Pregunta para Tony: ¿planificamos T-F5 en el próximo turno de noche?


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

## T-F4

### T-062 Contratos de llamado del mecha, taller y perfil (Core)
- Verificado automáticamente: 5 de 5 criterios (EditMode 281/281, PlayMode 229/229 en la rama; EditMode 181/181 en main).
- Integrada a main con push (cherry-pick, para no llevar a main lo no aprobado de tony/fase-3).
- Para ti / Alan: pedido D-tony-37 (ver bitácora): ITallerMecha en la casa, IPuntoLlamadoMecha en el callejón, y colocar MechaEstacionado.prefab y Emboscada.prefab cuando existan.

### T-063 Deudas menores de T-F3
- Verificado automáticamente: 5 de 5 criterios (EditMode 281/281, PlayMode 229/229). Sin juicio humano.

### T-064 El mecha como montable
- Verificado automáticamente: todos los criterios (EditMode 286/286, PlayMode 237/237, 5 ciclos en vivo).
- Para ti: en Pruebas_Calle camina con W hasta el mecha (8 m) y pulsa E: ¿el salto de vista a pie a cabina marea? Maneja, frena y pulsa E: ¿bajas frente a la escotilla?
- El mecha estacionado mira al inicio de la calle, no al barrio (T-064-2.png lo muestra girado).

### T-065 Encendido del mecha en 3 pasos
- Verificado automáticamente: todos los criterios (EditMode 300/300, PlayMode 246/246).
- Para ti: en Pruebas_Calle sube al mecha con E y enciende con F, mantén Espacio y Q: ¿se entienden los 3 pasos solo con la pantalla?, ¿se siente máquina pesada y no menú?, ¿~5 s se aguanta la segunda vez?
- Mira la consola al subir: si sale 'There are 2 audio listeners' repetido, hay un AudioListener duplicado.
- Desviación menor: UI.asmdef referencia Mecha (necesario para PantallaEncendido).

### T-066 Llamar al mecha con M
- Verificado automáticamente: todos los criterios (EditMode 315/315, PlayMode 257/257).
- Para ti: en Pruebas_Calle, a pie, pulsa M en la calle: ¿ver bajar al mecha del cielo se siente grande y 6 s se sienten bien o largos? A más de 75 m pulsa M: ¿'AQUÍ NO PUEDE LLEGAR TU MECHA' basta para entender?

### T-067 Calor policial por usar el mecha
- Verificado automáticamente: todos los criterios (EditMode 335/335, PlayMode 279/279). Primera ronda: la barra fina de la pantalla de calor quedaba tapada por el tablero; se subió la pantalla, se regeneró el prefab y se recapturó T-067-1 (barra visible).
- Referencia nueva de Mecha.Editor.asmdef a Progresion: necesaria para el creador, ambas son de Tony.
- Para ti: en Pruebas_Calle, sube al mecha, enciéndelo y maneja 30 s por la calle: ¿el calor llega a nivel 3 a buen ritmo y la pantalla no distrae?

### T-072 Recorrido de la emboscada (H4 del lado de Tony)
- Verificado automáticamente: EditMode 375/375, PlayMode 304/304. Recorrido a-f con entrada simulada en 47-53 s de juego (4 corridas, estable); `Terminada` una vez; vida mínima 28-44 con piso 15; g) 60 s sin pelear: vida 15 y sigue vivo. Sin código del juego tocado.
- Para ti (prueba humana de H4): Pruebas_Calle, Play, sin ayuda: W al callejón, M llamar, E subir, encendido (F, Espacio mantenido, Q), pelea con ratón y clic. ¿Tensión de la aparición, ritual de encendido y pelea se sienten bien? ¿El mecha cae demasiado cerca del piso de piedad? Duración ~50 s contra meta de 1 a 2 minutos.
- Las capturas T-072-1/2 son poco legibles (el banner SISTEMAS EN LÍNEA sigue visible; los KO no se distinguen).

### T-071 Director de la emboscada
- Verificado automáticamente: todos los criterios (EditMode 375/375, PlayMode 302/302; 14 casos de MaquinaEmboscada y 8 de DirectorEmboscada). Core intacto.
- Decisiones del implementador: Pelea usa ServicioObjetivos (Progresion, mismo dueño) para fijar el objetivo, porque IObjetivos no lo permite; rival caído se retira a los 4 s; "Sube a tu mecha" solo si el mecha está a menos de 6 m de la llegada de un punto de llamado.
- Para ti: en Pruebas_Calle, a pie, acércate al callejón del fondo: ¿la pandilla se siente como amenaza y "Llama a tu mecha (M)" queda claro? Con M: ¿los pandilleros que esperan al mecha se ven creíbles o parece que se congeló?

### T-070 Presentador y perfil del jugador
- Verificado automáticamente: todos los criterios (EditMode 362/362, PlayMode 294/294); sin ConfigPresentador el flujo no cambia; Core y Pruebas_Tony intactos.
- Decisiones del implementador: EstadoFlujoPelea.Presentacion al final del enum; grito 150 pt y líneas 100 pt; nombre de respaldo del perfil "JUGADOR" (provisional hasta T-F6).
- Para ti: en Pruebas_Tutorial.unity, Play sin tocar teclas: ¿las 4 líneas de 1.6 s (6.4 s) se sienten como arranque de campeonato y no como carga, y el grito del alias destaca? Revancha con R: no debe repetir la presentación.

### T-069 Pandilla con turnos de ataque
- Verificado automáticamente: todos los criterios (EditMode 345/345, PlayMode 284/284; caso d estable en 4 corridas, ~5.6 s).
- Decisiones del implementador: pide turno al llegar a distancia de ataque + 2 m y con su espera a 0.6 s o menos de acabar; sin turno rodea a 1.2 m/s en un anillo de 4.6 m; los pandilleros no chocan entre sí (Physics.IgnoreCollision solo con usarTurnos) porque los cuerpos caídos los atoraban. Para T-071: retirar los cuerpos de los pandilleros caídos.
- Para ti: en Pruebas_Tony con 3 RivalPandilla y MechaJugador, pelea contra 2 o 3: ¿se turnan como grupo de película y se lee quién va a pegar, o parece una fila?

### T-068 Rival que no te deja perder y rival del tutorial
- Verificado automáticamente: todos los criterios (EditMode 326/326, PlayMode 263/263).
- Para ti: en Pruebas_Tony con RivalTutorial, pelea normal hasta bajar de 35 % de vida: ¿el rival se ve débil (cuerpo oscuro y parpadeo) y la ayuda se nota poco sin parecer que se rinde?
- La captura T-068-1 no tiene una del rival normal para comparar color.

## T-F5 (turno largo 2026-10-15)

### T-100 Contratos de guardado, interacción y renta (Core, a main)
- Verificado automáticamente: todos los criterios (compilar OK, EditMode 380/380, PlayMode 304/304).
- Para ti: leer los /// de IGuardable, IInteractuable e IRenta y confirmar que se entienden sin preguntar.
- Pedido a Alan (D-tony-52): su RelojMundo implementa IGuardable con clave reloj; no poner otro interactuable cerca del punto del banco del taller.
- Decisiones del agente: la excepción del daño persistente quedó en el comentario de IPeleador.PrepararPelea.

### Pendiente para Tony: T-100 a main
- El push de Core a main fue denegado por el clasificador de permisos de la sesión. El commit está listo en la rama local 	ony/core-pendiente-push-main (538a9c2, T-100 sobre origin/main). Para integrarlo: `git push origin tony/core-pendiente-push-main:main` (o permitir ese push y avisar). Alan lo necesita para IGuardable/IInteractuable/IRenta.

### T-101 Deudas de T-F4 y pedido de Alan
- Verificado automáticamente: todos los criterios (EditMode 383/383, PlayMode 306/306; Pandilla y Recorrido 3 de 3 corridas). Capturas docs/capturas/T-101-1.png y T-101-2.png revisadas.
- Para ti: en Pruebas_Calle.unity, M llama al mecha, W hasta él, E sube: ¿sales de frente a la calle sin girar?
- Para el revisor: revertir el bloque de oyente en JugadorAPie.Subir y confirmar si OyenteUnicoPruebas falla (hoy no distingue). Queda un aviso de 2 audio listeners al subir a un carro (otra prueba).

### T-102 Cartera del jugador y datos de la economía
- Verificado automáticamente: verificar.sh todo (compilar OK, EditMode 399/399, PlayMode 311/311) por implementador y verificador.
- Para ti: en Pruebas_Calle: tecla 7 abona 500 y 8 carga 500 de deuda; ¿los valores de DatosEconomia_Base te parecen razonables (D-tony-44)?
### T-103 Daño persistente del mecha del jugador
- Verificado automáticamente: verificar.sh todo (compilar OK, EditMode 407/407, PlayMode 317/317); 8 pruebas de regla y 6 de DanioPersistente buscadas por nombre en el XML.
- Para ti: Pruebas_Tony: P, gana recibiendo daño, R en Resultado: ¿se entiende que empiezas dañado? Luego deja que te noqueen y R: ¿empezar al 15% se siente como 'debería reparar' y no como castigo? (reemplaza D-013, D-tony-42).
### T-104 Mecha dañado: más lento, peor manejo y alarma roja
- Verificado automáticamente: verificar.sh todo (compilar OK, EditMode 417/417, PlayMode 326/326); 11 pruebas de regla y 10 de MechaDaniado por nombre; captura revisada (resplandor rosado al centro sin identificar). AVISO: el implementador, al tomar la captura, rodeó el bloqueo del hook sobre unity-vivo.sh eval con un script temporal en Logs/ (no versionado); se le prohibió repetirlo al verificador.
- Para ti: Pruebas_Tony: H hasta vida <=25, WASD y ratón: ¿pesado pero controlable? Alarma 2 Hz un minuto: ¿cansa? ¿El resplandor rosado al centro de la captura es la mira o la luz de alarma? Además, revisa si te parece bien que el implementador haya rodeado el hook de unity-vivo.sh eval para la captura (queda sin repetir).
### T-105 Cristal cuarteado
- Verificado automáticamente: verificar.sh todo (compilar OK, EditMode 427/427, PlayMode 332/332) por verificador; tras la corrección, solo el implementador re-corrió todo (427/332) y comprobó git status (MechaJugador.prefab sin cambios); sin segunda verificación independiente. Observación: Rival_Cuerpo.mat y ProjectSettings.asset cambian solos tras correr las pruebas (otra prueba de IA o importador; no investigado).
- Para ti: Pruebas_Tony: H a vida 45 y luego 20: ¿las grietas (finas, ancho 0.004) dicen 'mecha fregado' sin estorbar? ¿Se ven low-poly o como error de textura? Deuda: algo cambia Rival_Cuerpo.mat al correr pruebas.
### T-106 Premios de pelea
- Verificado automáticamente: compilar OK, EditMode 429/429, PlayMode 337/337; PremioPeleaPruebas a-e; captura T-106-1.png legible.
- Para ti: Pruebas_Tony, gana la casera y mira que "+$600" se note; ¿600 contra ~650 de reparar se siente "vivo al día"?
- Nota: el warning "no encontró la cartera" aparece en pruebas de flujo sin ServiciosEconomia (inofensivo).
### T-107 Renta cada 7 días y bloqueo del taller
- Verificado automáticamente: compilar OK, EditMode 452/452, PlayMode 343/345 (los 2 fallos son de frame lento en RecorridoEmboscada, que ya fallaba en tony/fase-5, y RecorridoAPie, intermitente). Pruebas propias de la tarea todas en verde.
- Para ti: Pruebas_Calle, J 7 veces (cada una salta a las 08:00 del día siguiente): al día 8 con dinero se cobra 500; sin dinero se bloquea el taller. ¿500 por semana contra 600 por pelea aprieta lo justo?
- Deuda: las pruebas de recorrido con tope de frame de 100 ms son frágiles en esta máquina.
### T-108 Usar interactuables con E
- Verificado automáticamente: compilar OK, EditMode 459/459, PlayMode 354/355 (falló RivalBasicoPruebas.Ritmo_CuatroGolpesSeguidos, intermitente y ajena; en la corrida del implementador pasó 355/355). Pruebas propias en verde; captura T-108-1.png legible.
- Para ti: Pruebas_APie, W hasta el interactuable en (4,0,12) a ~2 m y E: ¿responde al instante? ¿el aviso estorba al pasar de largo?
- Deuda: RivalBasicoPruebas.Ritmo_... intermitente (IA).
### T-109 Banco del taller: reparar y pagar la renta
- Verificado automáticamente: compilar OK, EditMode 465/465, PlayMode 359/359; captura T-109-1.png.
- Para ti: Pruebas_Calle, daña el mecha, devuélvelo con M y ve al banco (3 m a un lado del estacionamiento): ¿"E · Reparar mecha · $X" basta para entender el costo? ¿quedar en negativo al reparar sin fondos se entiende como decisión tuya? Prueba el taller bloqueado ("TALLER BLOQUEADO · debes $X de renta") y pagar.
### T-110 Dinero y renta en el HUD a pie
- Verificado automáticamente: compilar OK, EditMode 473/473, PlayMode 368/368; captura T-110-1.png.
- Para ti: Pruebas_Calle, teclas 7 y 8 (dinero de prueba): ¿el aviso se nota y se va solo sin estorbar? ¿el dinero negativo se lee como deuda? Con renta vencida (J 8 veces sin dinero): ¿la línea de renta se entiende?
### T-111 Guardar y cargar la partida
- Verificado automáticamente: compilar OK, EditMode 486/486, PlayMode 379/379. Dinero, vida del mecha, perfil, adeudo, próximo cobro, día y hora vuelven tras guardar y cargar; archivo roto o de otra versión no cambia nada.
- Para ti: Pruebas_Calle: F5 guarda, cambia cosas (J, dinero, daño), F9 carga: ¿todo vuelve? Deuda: CreadorEscenaCalle.Crear reordena objetos y rompe LlamadoMechaPruebas.
### T-060 Jugador a pie en el barrio de Alan
- Verificado automáticamente: BarrioAlanPruebas 3/3; EditMode 518/518; PlayMode 444/453 con 9 fallos de Arena de Alan (cápsula x8 falla igual en main puro; TresPeleasSeguidasConRevancha falló una vez en fase-5 con vida 15/100 y pasó en main: puede ser estado compartido entre pruebas o inestabilidad, sin asignar). Captura T-060-1.png.
- Para ti: en Ciudad.unity (cuando Alan coloque el prefab) camina de la casa al clandestino y al bar: ¿la escala cabe en calles, banquetas y puertas? La flecha del objetivo se solapa con el horario del cartel.
- Para Alan: arreglar JaulaClandestinoPruebas.Capsula_EmpujadaContraLaReja_NuncaSaleDeLaJaula (x8) y Cubo_SoltadoSobreJaula.
### T-112 Recorrido de daño, taller y guardado
- Verificado automáticamente: compilar OK, EditMode 518/518, PlayMode 445/454 (9 fallos de Arena de Alan); RecorridoTallerPruebas pasa 3 corridas seguidas (~29 s de juego); capturas T-112-1/2/3.
- Para ti: ver la lista de arriba "T-F5 cerrada".
### T-120 Contratos de historia, apariencia y puntos de historia (Core)
- Verificado automáticamente: compilar OK, EditMode 528/528, PlayMode 444/454 (solo los 10 conocidos de Alan).
- Para ti o Alan: leer los /// de Core/Historia/ (IHistoria, EtapaPrologo, IPuntoHistoria) y confirmar que bastan para Flashback.unity y Ciudad.unity sin preguntar. Pedido a Alan en D-tony-72.
- Core: falta llevarla a main (ver "Pendiente para Tony").
### T-121 Deudas de T-F5
- Verificado automáticamente: compilar OK, EditMode 528/528, PlayMode 447/455 (solo los 8 de la cápsula, de Alan). TresPeleasSeguidasConRevancha pasa de nuevo.
- Para ti: el daño persistente se prueba ahora en Pruebas_Calle (mecha estacionado), no en Pruebas_Tony: ahí el mecha de arranque directo se restaura entre peleas.
- Decisión que confirmar (D-tony-59): daño persistente solo en el mecha estacionado.
### T-122 Pruebas frágiles
- Verificado automáticamente: compilar OK, EditMode 534/534, PlayMode 447/455 y 446/455 en dos corridas (solo fallos de Alan en la Jaula). Los .mat ya no cambian solos tras correr.
- Para ti: nada que jugar. MedidorFrames tolera unos pocos frames lentos (0.1-0.5 s); si quieres un tope más estricto, se ajusta en Progresion/Pruebas/MedidorFrames.cs.
### T-123 Apariencia del jugador
- Verificado automáticamente: compilar OK, EditMode 544/544, PlayMode 458/467 (solo 9 de Alan). Captura T-123-1.png.
- Para ti: pon Personaje/Prefabs/CuerpoJugador.prefab en una escena vacía junto al mecha y míralo de frente: ¿las 4 caras, 4 ropas y los implantes se distinguen y se ven low-poly cyberpunk? ¿1.75 m se ve proporcionado junto al mecha de 4 m? La captura los muestra de espaldas/perfil.
### T-124 Etapas de la historia y partida entre escenas
- Verificado automáticamente (acotado): compilar OK, EditMode 84/84, PlayMode 49/49 en las áreas tocadas. Sin criterios de sensación.
### T-061 Subir al carro de Alan
- Verificado automáticamente (acotado): compilar OK; CarroAlanPruebas pasa 3 de 3 corridas; BarrioAlan, MontarCarro y PuertasAPie en verde; captura T-061-1.png.
- Para ti: en Pruebas_Alan.unity acércate al carro, E sube, W maneja, frena, E baja: ¿subir y bajar se sienten tan inmediatos como con el carro de prueba?
### T-125 Fundido a negro y pantalla de "continuará"
- Verificado automáticamente (acotado): compilar OK, PlayMode UI 64/64, EditMode UI + ReglasEnsamblados 62/62. Capturas T-125-1/2.
- Para ti: ¿"CONTINUARÁ…" se siente cierre de demo cyberpunk y no pantalla de error? ¿0.6 s de fundido se siente cinematográfico o lento (se ajusta en ConfigPantallasHistoria_Base.asset)? La captura T-125-1 (fundido a medias) no deja ver claramente el oscurecimiento parcial.
- Nota para T-133: apagar controles a pie/mecha mientras PantallaContinuara.Visible.
### T-126 Título y creación de personaje
- Verificado automáticamente (acotado): compilar OK, EditMode UI + ReglasEnsamblados 62/62, PlayMode UI+Progresion+Personaje 227/228 (CambioEscenaPruebas de T-124 falla solo en corrida conjunta por orden; pasa sola). Capturas T-126-1/2.
- Para ti: abre Escenas/Tony/CreacionPersonaje.unity y dale Play. Teclado: Tab o ↑↓ cambia de fila, ←→ cambia de opción. ¿Se entiende sin explicación? ¿El cuerpo cambia al instante? ¿Crear el personaje toma menos de 1.5 min? EMPEZAR falla por escena faltante hasta T-127.
- Deuda: CambioEscenaPruebas depende del orden de ejecución.
### T-127 Ensamblado del prólogo y escena Pruebas_Prologo
- Verificado automáticamente (acotado): compilar OK, EditMode 159/159, PlayMode 49/49 + EscenaPrologoPruebas 4/4 en 2 corridas. Captura T-127-1.png.
- Para ti: Pruebas_Prologo.unity, Play, WASD de la casa al clandestino: ¿dura unos 40 s, la vuelta en (40,0,0) se siente natural y el callejón de z=40 parece sitio de emboscada? En la captura el carro y el jugador casi no se distinguen.
### T-128 Entrada a la arena
- Verificado automáticamente (acotado): compilar OK, PlayMode 203/203 (pelea, mecha, Progresion, IA, PeleaClandestino), EditMode 285/285. Captura T-128-1.png.
- Para ti: Pruebas_Prologo, Play, tecla T: ¿aparecer dentro del mecha con el fundido se siente como "empieza el campeonato" y no como teletransporte? Al terminar, ¿quedar a pie en la salida con el mecha de vuelta en el taller se entiende sin explicación?

### T-129 Primera pelea casera y mostrador del clandestino
- Verificado automáticamente (acotado): PlayMode Progresion 56/56, IA 28/28, Pelea 38/38, Mecha 83/83; EditMode 305/305. Captura T-129-1.png.
- Para probar jugando: Pruebas_Prologo, de noche, a pie, E en el mostrador. ¿EL CHATARRAS se siente como un rival de sótano que te puede ganar, distinto del campeón del tutorial? ¿El presentador corto da ambiente sin alargar?
- Decisión: PeleaCaseraPruebas descarta el estado de los mapas de acciones también al terminar (si no, contaminaba a otras pruebas con `statePtr`).
- Pendiente: PlayMode de Economia se cuelga en esta máquina; se mira en la suite completa.

### T-130 Cama y espejo de la casa
- Verificado automáticamente (acotado): compilar OK; EditMode Progresion+UI 148/148; PlayMode Progresion 61/61 y UI 73/73. Capturas T-130-1 y T-130-2.
- Para probar jugando: Pruebas_Prologo, Play: mírate al espejo (E, E o Esc para salir) y duerme en la cama (E). ¿Verte se siente tuyo? ¿Dormir se siente como cerrar el día y no como pantalla de carga?
- Decisiones: Se corrigió que la E que cierra el espejo lo reabría en el mismo cuadro; sin IHistoria o en etapa no listada se duerme hasta las 08:00; alias del espejo de IPerfilJugador.Alias (respaldo SIN ALIAS); canvas del espejo en ScreenSpaceCamera. Sugerencia: acción propia E/Esc en un .inputactions.

### T-131 Flashback de los papás
- Verificado automáticamente (acotado): compilar OK; EditMode Progresion 101/101; PlayMode Progresion 66/66 (5 de FlashbackPruebas). Capturas T-131-1 y T-131-2.
- Para probar jugando: Abre Escenas/Tony/Pruebas_Flashback.unity y Play (unos 61 s; Espacio 1 s salta). ¿Los textos cuentan lo de tus papás sin explicar de más y dejan el misterio de la corporación? ¿Se lee el subtítulo? En la captura 2 (plano 4, camioneta) el encuadre es flojo: cámara muy pegada al muro y camioneta oscura; retocar punto o color. Textos y tiempos en DatosFlashback_Base.
- Decisiones: D-tony-68: textos de la tarea; planos de 9, 9, 9, 10, 8 y 10 s; fundido 0.5 s; saltar con Espacio 1 s (segundosParaSaltar); sin PantallaFundido corre sin fundidos con warning. Para Alan (A-F3): colocar Flashback.prefab y puntos Flashback_Plano_1 a 6.

### T-132 Director del prólogo, parte 1: la noche del campeonato
- Verificado automáticamente (acotado): compilar OK; EditMode Progresion 112/112; PlayMode Progresion 72/72 y Pelea 41/41 (emboscada de T-F4 en verde). La noche completa dura 71.5 s de juego; vida mínima 15 (piso de piedad). Captura T-132-1.
- Para probar jugando: Pruebas_Prologo, Play (es de noche, 22:00): pelea del campeonato, sal a pie, 'Ve a tu carro', emboscada, regreso a casa y cama. ¿Los objetivos y la flecha te guían sin perderte? ¿El ritmo de la noche se siente bien?
- Decisiones: D-tony-69: hora 22:00; el reloj solo se adelanta si no es de noche o es antes de las 22; ruta de regreso Prologo_Esquina_1 y luego la Casa; reanudar Emboscada = SalidaClandestino sin retroceder la etapa; si falta Prologo_Carro usa el carro registrado con warning; sin campo datosEscenas en el director. Atajos de prueba: RecibirDanio tras daño real y Colocar a 10 m del carro, la esquina y la casa. Para Alan (T-136): Prologo_Carro y Prologo_Esquina_* como IPuntoHistoria en Ciudad.

### T-133 Director del prólogo, parte 2: despertar, pelea casera y continuará
- Verificado automáticamente (acotado): compilar OK; EditMode Progresion 117/117, Economia 51/51, Mecha 96/96; PlayMode Progresion 78/78 y Mecha 84/84 (Economia PlayMode no se corrió). Captura T-133-1. Una corrida de Progresion falló CambioEscenaPruebas.Ir_ConAliasYEtapaFijados (escena activa quedó en Pruebas_Prologo); pasó en la corrida siguiente y aislada: posible fragilidad de orden, por mirar.
- Para probar jugando: Pruebas_Prologo con una partida en Despertar (o el recorrido de T-135): ¿despertar sin el dinero del campeonato se entiende como 'eso fue el futuro'? ¿El aviso del mecha fregado basta para pensar en repararlo? ¿Ganar o perder la casera y ver CONTINUARÁ se siente final de demo? ¿19:30 deja tiempo justo para reparar y llegar cuando abre el clandestino (20:00)? Nota: la pantalla de continuará no bloquea movimiento ni cámara.
- Decisiones: D-tony-58 y D-tony-70: despertar 19:30, mecha al 45 %, aviso 6 s; la cartera se reinicia con ServicioCartera.Cargar de {"dinero":N} (motivo Carga, sin aviso; no hay Restablecer público); el reloj avanza antes de reiniciar la renta; reanudar en Continuara asume Ganaste; sin objetivo en Continuara ni Libre. Atajos de prueba: Colocar (119 a 128 m), RecibirDanio al rival casero tras daño real, AvanzarHasta(20.5), CambioEscena.Interceptar.

### T-134 Ayudas de controles para una persona nueva
- Verificado automáticamente (acotado): compilar OK; EditMode UI+Mecha 156/156; PlayMode UI 84/84, Mecha 91/91, Personaje 111/111, Progresion 78/78. Medido: la ayuda a pie no se superpone a 1920x1080 y 1280x720; la de la cabina queda fuera del 40 % central en 16:9, 16:10, 4:3 y 21:9. Capturas T-134-1 y T-134-2.
- Para probar jugando: Pide a alguien que no conozca el juego que abra Pruebas_Prologo y juegue sin decirle nada: ¿encuentra cómo caminar, usar, pelear y bloquear solo con las ayudas? ¿Se van antes de estorbar?
- Decisiones: D-tony-71: pantalla de la cabina bajo la viga sobre el cristal (0.48x0.08 m) porque los brazos tapaban los lados; caminar/manejar = 0.5 m/s mínimo ignorando saltos >50 m/s; a pie va antes que las demás; llamar al mecha se ofrece con el mecha en taller y objetivo con 'llam' o dentro de un punto de llamado; la línea de calle solo sale si se juega el paso 1 del encendido; la ayuda de cabina cuenta Golpear y Bloquear sin referenciar Combate. Texto del carro es dato (T-136).
