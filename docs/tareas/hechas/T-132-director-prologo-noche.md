# T-132 Director del prólogo, parte 1: la noche del campeonato
Dueño: Tony | Rama: tony/T-132-director-prologo-noche | Modelo: Sonnet 5.5, medium
Depende de: T-124, T-127, T-128, T-130

## Objetivo
Un director encadena la noche del prólogo sin que el jugador se pierda: aparece en su mecha en la arena para la pelea de campeonato
(imposible de perder), sale a pie hacia su carro, lo embosca la pandilla en el callejón, regresa a casa y se le pide dormir. Cada paso es
una etapa de `IHistoria` con su objetivo en el HUD. Es la parte de demo.md pasos 2 a 4; la parte del presente (despertar, pelea casera,
"continuará") es T-133.

## Contexto
- Diseño: demo.md (orden; "el jugador intenta ir a otro lado durante el prólogo: un objetivo lo guía de vuelta"; no se puede perder ni la
  pelea del prólogo ni la emboscada), ciudad.md (de noche abren el clandestino y aparecen las pandillas).
- Decisiones: D-053 (guía de vuelta: flecha y "REGRESA"; el director pone objetivos intermedios en las esquinas), D-064 (piedad del rival
  del tutorial y de la pandilla), D-066 (presentador con el alias), D-067 (`Emboscada.prefab`: se activa a 12 m, una vez, fija sus propios
  objetivos y avisa `Terminada`), D-tony-65 (`EntradaArena`). Propuesta nueva: D-tony-69.
- Piezas: `ServicioHistoria`, `ServicioObjetivos.Fijar`, `CambioEscena`, `DatosEscenas` (T-124), `EntradaArena` y `FlujoPeleaTutorial`
  sin revancha (T-128), `CamaJugador` (T-130: en etapa `Dormir` lleva al flashback), `DirectorEmboscada` (Pelea/Emboscada/),
  `EventosLugares` (Core: entrar y salir de lugares), `Pruebas_Prologo.unity` y su creador (T-127).

## Qué hacer
1. `Progresion/Prologo/Datos/DatosPrologo.cs` + `DatosPrologo_Base.asset` (con creador): hora de la noche del prólogo (22:00), textos de los
   objetivos ("Ve a tu carro", "Regresa a casa en tu carro", "Duerme en tu cama"), id del punto del carro ("Prologo_Carro") y pasos de la
   ruta de regreso (ids de `IPuntoHistoria`, hoy solo "Prologo_Esquina_1", y al final el lugar Casa). Los textos y horas de T-133 se agregan
   en T-133.
2. `Progresion/Prologo/MaquinaPrologo.cs` (pura): con la etapa actual y avisos (tutorial terminado, emboscada empezó, emboscada terminada,
   llegó a un paso de la ruta, entró a la casa) da la etapa siguiente y el objetivo a mostrar (texto y punto o lugar). Sin Unity.
3. `DirectorEmboscada`: propiedad `Habilitada` (true por defecto: Alan y las pruebas de T-F4 no cambian); en false, acercarse no la activa.
4. `Progresion/Prologo/DirectorPrologo.cs` + `Progresion/Prologo/Prefabs/DirectorPrologo.prefab` (con creador; anida `FlujoPeleaTutorial.prefab`,
   una `EntradaArena` y `PantallasHistoria.prefab`; usa `DatosEscenas_Demo` por defecto). Un frame después de arrancar (para que la partida
   se cargue y todo se registre) toma la etapa de `IHistoria`; si es `Ninguna` usa `etapaSinPartida` (serializada; `Libre` en el prefab,
   así el director no hace nada si Alan da Play en su Ciudad). Por etapa:
   - `PeleaTutorial`: adelanta el reloj a la hora de la noche si no es esa noche y `EntradaArena.Empezar(tutorial)`; al terminar →
     `SalidaClandestino`.
   - `SalidaClandestino`: objetivo "Ve a tu carro" en el punto del carro; emboscada `Habilitada`; cuando la emboscada se activa → `Emboscada`.
   - `Emboscada`: los objetivos los pone la emboscada; al `Terminada` → `RegresoACasa`.
   - `RegresoACasa`: objetivo al primer paso de la ruta; al llegar (`Llego` del servicio de objetivos) el siguiente; al entrar a la casa →
     `Dormir`.
   - `Dormir`: objetivo "Duerme en tu cama" en el punto de uso de la `CamaJugador` de la escena (la cama hace el resto).
   - En cualquier otra etapa la emboscada queda deshabilitada.
   Reanudar (partida cargada a media noche): `SalidaClandestino` o `Emboscada` → jugador a pie en "Clandestino_Salida" y se comporta como
   `SalidaClandestino` (la emboscada se repite; la etapa guardada no retrocede); `RegresoACasa` → en "Clandestino_Salida" con su objetivo;
   `Dormir` → en "Casa_Cama". Todo cambio de etapa pasa por `ServicioHistoria.Avanzar`.
5. Pruebas_Prologo (en `CreadorEscenaPrologo.Crear`): reemplaza el `FlujoPeleaTutorial` y la `EntradaArena` sueltos de T-128 por
   `DirectorPrologo.prefab` con `etapaSinPartida` = `PeleaTutorial` y `DatosEscenas_Pruebas` (la tecla T de prueba puede quedarse si sigue
   funcionando). Ajusta `EntradaArenaPruebas` si dependía de los objetos sueltos.
6. Pruebas: EditMode `MaquinaPrologoPruebas` y PlayMode `Tests/PlayMode/Progresion/PrologoNochePruebas.cs` (Pruebas_Prologo, entrada simulada
   para pelear, caminar, M, E, encendido; se permite rematar rivales con `RecibirDanio` tras daño real al jugador y acortar caminatas de más de
   40 m con `IJugador.Colocar` hasta 10 m antes del objetivo, anotando cada atajo; `CambioEscena.Interceptar` y `RutaForzada` temporal).

## Carpetas permitidas
Assets/_Proyecto/Progresion/Prologo/ (Datos/, Prefabs/, Editor/, Pruebas/), Assets/_Proyecto/Pelea/Emboscada/DirectorEmboscada.cs,
Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity (con su creador), Assets/_Proyecto/Tests/EditMode/Progresion/,
Assets/_Proyecto/Tests/PlayMode/Progresion/, Tests/PlayMode/Pelea/ (solo casos de `Habilitada`), docs/capturas/
No tocar: Core/, todo lo de Alan, `Emboscada.prefab` (su valor por defecto no cambia), otras escenas.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (las pruebas de la emboscada de T-F4 siguen en verde)
- [ ] MaquinaPrologoPruebas: la secuencia de etapas y objetivos de la noche, avisos fuera de orden ignorados, y la regla de reanudar
- [ ] PrologoNochePruebas, desde Play en Pruebas_Prologo: es de noche (22:00) y el jugador está en su mecha en la arena; la presentación
      dice el alias del perfil; gana (+$1,500); queda a pie en "Clandestino_Salida" con el objetivo "Ve a tu carro"; caminando hacia el
      carro se activa la emboscada (etapa `Emboscada`); llama al mecha, sube, enciende y gana; etapa `RegresoACasa` con el objetivo en la
      esquina y luego en la casa; al entrar a la casa, etapa `Dormir` con el objetivo en la cama; E en la cama pide la escena del flashback y
      la etapa guardada es `Flashback`
- [ ] En toda la noche la vida del mecha nunca llega a 0 y no hay errores en consola; dura menos de 200 s de juego (con los atajos anotados)
- [ ] Con `etapaSinPartida` en `Libre` (valor del prefab) el director no fija objetivos, no mueve al jugador y la emboscada sigue
      `Habilitada` (prueba en Pruebas_Calle o en una escena mínima)
- [ ] Reanudar: con una partida en `RegresoACasa` y una transición pendiente, el jugador aparece en "Clandestino_Salida" con el objetivo
      de regreso
- [ ] Captura docs/capturas/T-132-1.png: HUD a pie con "Ve a tu carro" y la flecha al salir del clandestino

## Criterios de sensación (para un humano)
- Pruebas_Prologo, Play, sin ayuda: pelea, sal, camina, sobrevive a la emboscada, regresa a casa y duerme. ¿Los objetivos bastan para no
  perderse? ¿Si te alejas, "REGRESA" te trae de vuelta sin regañar? ¿La noche dura unos 6 a 8 minutos?

## Pruebas automáticas
- EditMode: MaquinaPrologoPruebas. PlayMode: PrologoNochePruebas y casos de `Habilitada` en DirectorEmboscadaPruebas.

## Fuera de alcance
- Despertar, estado inicial, pelea casera y "continuará" (T-133); ayudas de controles (T-134); el recorrido completo desde la creación
  (T-135); Ciudad.unity de Alan (T-136)
