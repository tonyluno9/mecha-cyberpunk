# T-065 Encendido del mecha en 3 pasos
Dueño: Tony | Rama: tony/T-065-encendido-mecha | Modelo: Sonnet 5.5, medium
Depende de: T-064

## Objetivo
Cada vez que el jugador se sube al mecha, la cabina está apagada y hay que encenderla en 3 pasos con teclas: confirmar entrada,
levantar el mecha y desbloquear el cristal. Con entrada inmediata toma unos 5 segundos; mientras tanto el mecha no se mueve, no
golpea y no deja bajar, y se le puede pegar. Es lo que la emboscada enseña ("aprendes el encendido", demo.md).

## Contexto
- Diseño: mecha-taller.md ("El encendido tiene 3 pasos (confirmar entrada, levantar el mecha y desbloquear el cristal) y ocurre
  cada vez que te subes"; "dura unos 5 segundos"; "en plena emboscada sigue siendo de 5 s y eres vulnerable"); combate.md
  ("al soltarlo usas palancas y botones de la cabina. En PC esas acciones van a teclas"); D-011 (en el mecha todo es diegético:
  las indicaciones van en una pantalla de la cabina, nada flota).
- Propuesta: D-tony-29 en docs/decisiones-pendientes-tony.md (teclas F, mantener Espacio y Q; tiempos).
- Lo que ya hay: `MontajeMecha` y el permiso de piloto (T-064), `PeleadorMecha.PeleaPreparada` (Combate), la cabina greybox con
  sus piezas de neón, el cristal y la pantalla de vida (CreadorCabina, CreadorVidaMecha), `ControlesMecha.inputactions`.
- Entrada: `ControlesMecha.inputactions` lo escribió CreadorEscenaMecha con `ToJson`. Si lo regeneras, las acciones cambian de id
  y se rompen las `InputActionReference` del prefab. Agrega las acciones nuevas con un método que cargue el asset, agregue solo
  las que falten y conserve los id de las existentes; déjalo reutilizable (T-066 agrega otra acción con él).
- Una pelea de arena no pide encendido: si `PeleadorMecha.PrepararPelea` corre (evento `PeleaPreparada`), el encendido queda
  completo. En arranque `Directo` el mecha empieza encendido.

## Qué hacer
1. Lógica pura `Mecha/Encendido/SecuenciaEncendido.cs`: estados Apagado, Paso1, Paso2, Paso3 (cada uno esperando su tecla o
   animándose) y Encendido. Recibe en `Avanzar(dt, confirmar, mantenerLevantar, cristal)` lo que se pulsó. Paso 1: F, luego
   anima `segundosPaso1`. Paso 2: hay que mantener Espacio `segundosMantenerPaso2` seguidos (si se suelta antes, el avance vuelve a
   0 de ese paso). Paso 3: Q, luego anima `segundosPaso3`. Entre pasos, `pausaEntrePasos`. Teclas de otro paso no hacen nada.
   Expone paso actual, progreso del paso (0 a 1) y eventos `PasoCambio` y `Completado`; `Reiniciar()` y `Completar()`.
2. `ConfigEncendido` (Mecha/Datos/): 1.0 s, 1.5 s, 1.2 s y 0.3 s, altura agachado −1.2 m, textos de cada paso.
3. `Mecha/Encendido/EncendidoMecha.cs` en el mecha: al subir (OcupadoCambio true) reinicia y empieza; mientras no está encendido,
   permiso de piloto false y `MontajeMecha.PuedeBajar` false; al completar, permiso de piloto true. Lee las acciones nuevas solo
   mientras enciende. Presentación greybox: neón de la cabina y pantalla de vida apagados hasta el paso 1; la cámara empieza
   agachada y sube a su altura durante el paso 2; una persiana oscura tapa el cristal y sube durante el paso 3.
   `PeleaPreparada` y el arranque `Directo` lo dejan encendido al instante.
4. Pantalla diegética `UI/Cabina/PantallaEncendido.cs` con `ConfigPantallaEncendido` (UI/Datos/): un Canvas World Space en la
   cabina, cerca de la pantalla de vida y legible desde la cámara, que muestra "PASO 1/3 · [F] CONFIRMAR ENTRADA",
   "PASO 2/3 · MANTÉN [ESPACIO] · LEVANTAR" con barra de progreso, "PASO 3/3 · [Q] DESBLOQUEAR CRISTAL" y "SISTEMAS EN LÍNEA"
   (1 s) al terminar; con una tecla equivocada, un parpadeo rojo de la línea. Se apaga cuando el mecha está encendido.
5. Creador idempotente en Mecha/Editor/ que agregue las acciones Confirmar (F), Levantar (Espacio) y Cristal (Q) al mapa del
   mecha, EncendidoMecha, la persiana y la pantalla al prefab (y por herencia a MechaEstacionado.prefab).
6. Pruebas:
   - EditMode `Tests/EditMode/Mecha/SecuenciaEncendidoPruebas.cs`: orden de pasos; teclas fuera de orden no avanzan; soltar
     Espacio antes de tiempo reinicia el paso 2; con teclas inmediatas la duración total es la suma de config (±1 frame);
     `Completar()` y `Reiniciar()`.
   - PlayMode `Tests/PlayMode/Mecha/EncendidoMechaPruebas.cs` (Pruebas_Calle, entrada simulada): subir con E; durante el
     encendido W no mueve y clic no golpea y E no baja; pulsando cada tecla 0.1 s después de que se pide, de subir a tener control
     pasan entre 4.5 y 5.5 s; después W mueve y clic golpea; bajar y volver a subir pide el encendido otra vez; un golpe de
     `DaniadorDePrueba` durante el encendido sí baja la vida; en Pruebas_Tony (Directo) el mecha se mueve desde el primer frame.

## Carpetas permitidas
Assets/_Proyecto/Mecha/, Assets/_Proyecto/UI/Cabina/, Assets/_Proyecto/UI/Datos/, Assets/_Proyecto/UI/Editor/,
Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity (solo regenerándola con su creador), Assets/_Proyecto/Tests/EditMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/Mecha/
No tocar: Core/, Combate/ (solo se escucha PeleaPreparada), Personaje/, Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, incluidas las pruebas de pelea en Pruebas_Tony
- [ ] SecuenciaEncendidoPruebas y EncendidoMechaPruebas pasan
- [ ] Las `InputActionReference` existentes del prefab siguen apuntando a acciones válidas (prueba que lo comprueba) y
      `ControlesMecha.inputactions` conserva los id de Mover, Mirar, Golpear, Bloquear y LiberarCursor (`git diff` del asset)
- [ ] Capturas desde la cabina: apagada con el paso 1 en pantalla (docs/capturas/T-065-1.png), paso 2 a medio levantar
      (T-065-2.png) y encendida con el cristal libre (T-065-3.png); el texto de la pantalla se lee en las tres
- [ ] Consola sin errores durante EncendidoMechaPruebas

## Criterios de sensación (para un humano)
- En Pruebas_Calle, subir y encender: ¿los 3 pasos se entienden a la primera solo con la pantalla de la cabina? ¿Se siente como
  encender una máquina pesada (luces, levantarse, abrir el cristal) y no como un menú?
- ¿Unos 5 segundos se sienten bien, o largos al subir por segunda vez?

## Pruebas automáticas
- EditMode: SecuenciaEncendidoPruebas. PlayMode: EncendidoMechaPruebas.

## Fuera de alcance
- Sonido del encendido; animación del cuerpo del mecha fuera de la cámara; saltarse el encendido con una tecla
- Indicaciones de tutorial de golpe y bloqueo (T-F6, director del prólogo)
