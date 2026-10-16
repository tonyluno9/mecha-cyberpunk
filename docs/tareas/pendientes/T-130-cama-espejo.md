# T-130 Cama (dormir y guardar) y espejo de la casa
Dueño: Tony | Rama: tony/T-130-cama-espejo | Modelo: Sonnet 5.5, medium
Depende de: T-123, T-124, T-125, T-127

## Objetivo
En la casa hay una cama y un espejo que se usan con E. Dormir hace fundido, adelanta el reloj a las 08:00, guarda la partida solo y
regresa; en la noche del prólogo (etapa `Dormir`) dormir lleva al flashback. El espejo muestra tu apariencia y tu alias. Son prefabs de
Tony que Alan coloca en la casa de A-F3 sin programar nada.

## Contexto
- Diseño: demo.md (paso 4: "regresas a casa en carro y duermes"; paso 5: flashback), personaje-progresion.md ("tu apariencia se ve en el
  espejo de tu casa"), ciudad.md (día de 20 minutos reales: 12 de noche y 8 de día).
- Contratos: `IInteractuable` (D-054: el más cercano a 2.5 m y 60°), `IRelojMundo.AvanzarHasta` ("sirve para dormir"), `IHistoria` (T-120).
  Piezas: `ServicioHistoria`, `CambioEscena`, `DatosEscenas`, `ServicioGuardado` (T-124), `PantallaFundido` (T-125),
  `VistaPreviaApariencia.prefab` (T-123), `InteraccionAPie` (Personaje/).
- Decisiones: D-tony-50 ("cuándo se guarda solo (al dormir): T-F6"), D-tony-52 (decía que la cama y el espejo podían ser `IInteractuable`
  de Alan; D-tony-67 lo cambia: son prefabs de Tony que Alan coloca). Propuesta nueva: D-tony-67.

## Qué hacer
1. `Progresion/Prologo/CamaJugador.cs` + `Progresion/Prologo/Prefabs/CamaJugador.prefab` (con creador; una caja de cama greybox y el punto
   de uso al lado). `IInteractuable`, solo a pie:
   - etapa `Dormir`: "Dormir" → fundido a negro, `Avanzar(Flashback)` y `CambioEscena.Ir(RutaPara(Flashback))` (que guarda);
   - etapas de la noche del prólogo antes de llegar a casa (`PeleaTutorial` a `RegresoACasa`) y `PeleaCasera`: aviso sin uso
     ("Ahora no puedes dormir" / "Primero tu pelea casera");
   - `Libre`, sin `IHistoria` o cualquier otra: "Dormir · hasta las 08:00" → fundido, `AvanzarHasta(8)`, guardar, fundido de vuelta. Sin
     reloj registrado solo guarda.
   Usa `DatosEscenas_Demo` por defecto (serializado, para que Pruebas_Prologo use `DatosEscenas_Pruebas`).
2. `UI/Historia/PantallaEspejo.cs` (Canvas Overlay: la `RawImage` de `VistaPreviaApariencia`, el alias grande y "E · salir") y
   `Progresion/Prologo/EspejoJugador.cs` + `Progresion/Prologo/Prefabs/EspejoJugador.prefab` (con creador; un marco greybox con
   superficie brillante y el punto de uso enfrente). "Mirarte al espejo": abre la pantalla, prende la vista previa con la apariencia del
   perfil y quita el control al jugador (no camina ni mira); E o Esc cierran y devuelven el control; la vista previa queda apagada.
3. Pruebas_Prologo (en `CreadorEscenaPrologo.Crear`): `CamaJugador.prefab` en "Sitio_Cama" y `EspejoJugador.prefab` en "Sitio_Espejo", a
   más de 2.5 m entre sus puntos de uso; la cama usa `DatosEscenas_Pruebas`.
4. PlayMode `Tests/PlayMode/Progresion/CamaEspejoPruebas.cs` (Pruebas_Prologo, entrada simulada para caminar y E; `RutaForzada` temporal;
   `CambioEscena.Interceptar` donde se cambie de escena).

## Carpetas permitidas
Assets/_Proyecto/Progresion/Prologo/ (con Prefabs/ y Editor/), Assets/_Proyecto/UI/Historia/, Assets/_Proyecto/UI/Editor/ (si la pantalla del
espejo se arma con creador), Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity (con su creador), Assets/_Proyecto/Tests/PlayMode/Progresion/,
docs/capturas/
No tocar: Core/, todo lo de Alan, `JugadorAPie.prefab`, otras escenas.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] CamaEspejoPruebas:
      a) sin etapa de historia, día 1 21:00: E en la cama → día 2 08:00, el archivo guardado trae `reloj` con ese día y hora, y el jugador
         sigue junto a la cama con control;
      b) etapa `Dormir`: E en la cama → etapa `Flashback` guardada y la escena pedida es la del flashback de `DatosEscenas_Pruebas`;
      c) etapa `PeleaCasera`: el HUD muestra "Primero tu pelea casera" sin "E · " y E no cambia la hora;
      d) espejo: E abre la pantalla con el alias del perfil y la cámara de la vista previa activa; W simulado no mueve al jugador; E cierra,
         W vuelve a mover y no queda cámara de vista previa activa
- [ ] Un solo `AudioListener` activo en todo momento; consola sin errores
- [ ] Capturas docs/capturas/T-130-1.png (pantalla del espejo con el cuerpo y el alias) y T-130-2.png (aviso "E · Dormir · hasta las 08:00")

## Criterios de sensación (para un humano)
- Pruebas_Prologo, Play: entra a la casa, mírate al espejo y duerme. ¿Verte en el espejo se siente tuyo? ¿Dormir con el fundido se siente
  como cerrar el día y no como pantalla de carga?

## Pruebas automáticas
- PlayMode: CamaEspejoPruebas.

## Fuera de alcance
- El flashback (T-131); quién pone la etapa `Dormir` (T-132); que el calor baje al llegar a casa; morir en la calle y la clínica (T-F7);
  un espejo con reflejo real
