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
- Para ti: Escenas/Tony/Pruebas_APie.unity, Play, WASD, ratón, Shift para correr, Espacio para saltar. ¿Responde al instante sin patinar ni flotar? ¿El salto y la carrera se sienten ligeros, distintos del mecha? ¿La altura de ojos (1.65 m) no se siente enana ni gigante junto a los cubos y la rampa de 30°?
- Decisiones del agente: clic con cursor libre lo vuelve a bloquear; Manos es hijo de Camara; valores de aceleración, frenado, gravedad y control en el aire en ConfigControlesAPie.
