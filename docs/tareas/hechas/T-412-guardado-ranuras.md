# T-412 Guardar y cargar en 3 ranuras, autoguardado y "Continuar"
Dueño: Tony | Rama: tony/T-412-guardado-ranuras | Modelo: Opus 5.5, medium (lógica de guardado) | MODO RÁPIDO (docs/ronda3.md)
Depende de: T-408 (kit de UI). Va **antes** de T-410 (la pausa usa estas ranuras).

## Objetivo
La partida se guarda en 3 ranuras que muestran fecha, zona y dinero, más un AUTOGUARDADO que se escribe solo al dormir y al conquistar una
zona. "Continuar" carga la más reciente. Cargar te deja donde estabas, con tu dinero, tus armas, tu respeto, tus aliados y tu paso de la historia.

## Contexto
- Guardado actual: `Progresion/Guardado/ServicioGuardado.cs` (un solo `partida.json`, `RutaForzada` y `FijarRuta` para pruebas, piezas
  pendientes) y `ArchivoPartida.cs` (versión 1, fecha y piezas). `Core/Guardado/IGuardable.cs` tiene la lista de claves en uso.
  `Progresion/Escenas/CambioEscena.cs` marca la transición para que la escena nueva cargue (y tiene `Interceptar` para pruebas).
- Ya guardan: `cartera`, `inventario`, `territorio`, `aliados`, `historia_pandillas`, `reloj` (Alan), `perfil` y otras. **No se guarda
  dónde está el jugador**: hoy cargar no lo mueve.
- Dormir ya guarda (`Progresion/Prologo/CamaJugador.cs`); la conquista se avisa con `ITerritorio.Conquistada`.
- Con `DEMO_FINAL` el director borra `GuardadoDePrueba` (F5/F9) y la demo empieza siempre nueva (D-tony-231).
- Desde el paso 2: el dinero se escribe con `Moneda.Texto` (Ƀ, T-414); `perfil` ya trae el nombre y el color de tu pandilla (T-415); el arma en la
  mano puede ser FUSIL o SNIPER (T-416); la historia tiene los pasos Doc, Inventor y NeonAlto (T-418, se guardan por nombre).
- Decisiones: D-tony-244 y D-tony-259. Guía de estilo: docs/diseno/ui-cyberfix.md.
- **Cambio de Core autorizado por Tony**: solo el comentario de claves de `Core/Guardado/IGuardable.cs` (agrega `jugador`).

## Carpetas permitidas
Progresion/Guardado/, Progresion/Partida/ (nuevo), Progresion/Escenas/, Progresion/Pruebas/, Progresion/Prologo/ (Pandillas/, CamaJugador.cs,
Editor/CreadorCiudadPandillas.cs), Personaje/Jugador/ (guardar la posición), UI/ (Guardado/ nuevo, Kit/, Editor/, Datos/), Core/Guardado/IGuardable.cs
(solo el comentario), Escenas/Ciudad_Prologo.unity (solo con código), Tests/EditMode/Progresion/, Tests/PlayMode/Progresion/, docs de Tony.
No tocar: Radio/, Mundo/.

## Qué hacer
1. **Ranuras** en `ServicioGuardado`: `Guardar(int ranura)`, `Cargar(int ranura)`, `ResumenDe(int ranura)` y `RanuraMasReciente()`, con
   las ranuras 1, 2 y 3 y `RanuraAuto` (0). Archivos `partida_auto.json` y `partida_1.json` a `partida_3.json` en `persistentDataPath`.
   `RutaArchivo` pasa a ser la ranura AUTO; con `RutaForzada` o `FijarRuta`, AUTO es exactamente esa ruta y las ranuras salen a su lado
   (`<ruta>_1.json`…), así las pruebas no tocan las partidas reales. `Guardar()` y `Cargar()` sin número siguen igual que hoy sobre
   `RutaArchivo`, para que las pruebas viejas de guardado y de CambioEscena no cambien. El `partida.json` viejo se ignora.
2. **Resumen** dentro del archivo (campo opcional; `ArchivoPartida` sigue en la versión 1 y lee los archivos sin resumen): fecha local,
   zona donde está el jugador (`ITerritorio`), dinero (`ICartera`) y texto del objetivo actual (`IObjetivos`). Leerlo no carga la partida.
   Un archivo roto o de otra versión sale como "VACÍA" y nunca lanza excepciones.
3. **Posición del jugador**: un `IGuardable` con la clave `jugador` en Personaje/Jugador (posición, giro y arma en la mano). Si guardas
   dentro de un carro, guarda el punto de bajada junto al carro. Al cargar te coloca ahí sin atravesar el piso.
4. **`Progresion/Partida/FlujoPartida.cs`** (estático): `IrANuevaPartida()`, `IrACargar(int ranura)` e `IrAlTitulo()` vuelven a cargar
   Ciudad_Prologo y dejan pendiente qué hacer. El `ServicioGuardado` de la escena nueva carga la ranura pendiente al arrancar.
   `IrAlTitulo()` por ahora es igual a una partida nueva (T-413 le pone el título). Con `Interceptar` para pruebas, como CambioEscena.
   Con `DEMO_FINAL` la partida empieza nueva **salvo** que haya una carga pendiente.
5. **Autoguardado** `Progresion/Partida/Autoguardado.cs`: escribe la ranura AUTO al dormir (CamaJugador) y al conquistar una zona. Al
   guardar sale abajo a la derecha "GUARDADO" en turquesa durante 2 s, con el sonido UI_GUARDADO.
6. **`UI/Guardado/PantallaRanuras.cs`** (prefab hecho con el kit; canvas de orden 70): AUTOGUARDADO arriba y las ranuras 1 a 3, cada una con
   "RANURA 2 · 29 OCT 21:14 · EL TIANGUIS · Ƀ340" en una línea y el objetivo abajo en turquesa, o "VACÍA". En modo GUARDAR: las 3 manuales
   (sobrescribir pide "¿SOBRESCRIBIR?" SÍ/NO en magenta) y AUTO desactivada. En modo CARGAR: las 4, y las vacías desactivadas. Esc o
   VOLVER la cierran. Mientras está abierta, el jugador no se mueve ni dispara. Hasta que llegue la pausa (T-410), F5 la abre en modo
   GUARDAR y F9 en modo CARGAR, solo fuera de `DEMO_FINAL` (reemplaza `GuardadoDePrueba` en la escena).

## Criterios de aceptación
- [ ] Guardar en la ranura 2, cambiar dinero, respeto y posición y cargar la 2 deja todo como estaba: dinero, armas, respeto, aliados,
      paso de la historia y la posición a menos de 0.5 m.
- [ ] El resumen de cada ranura dice la fecha, la zona y el dinero correctos sin cargar la partida.
- [ ] Dormir y conquistar EL TIANGUIS escriben la ranura AUTO y sale "GUARDADO"; `RanuraMasReciente()` es la última escrita.
- [ ] Un archivo de ranura roto sale como VACÍA, sin excepciones y con un solo warning.
- [ ] Las pruebas viejas de `ServicioGuardadoPruebas`, `GuardadoClavesPendientesPruebas` y `CambioEscenaPruebas` (PlayMode/Progresion)
      siguen en verde (corre solo esas tres clases).
- [ ] Captura revisada a ojo y corregida si se ve mal: docs/capturas/T-412-1.png (pantalla de ranuras en modo CARGAR con dos llenas) en
      1920x1080 y 2560x1440.

## Pruebas (solo estas 2 nuevas)
- EditMode `RanurasPruebas.TresRanurasYAuto` (con ruta temporal): escribe las ranuras 1 y 3 y AUTO con fechas distintas; los resúmenes
  son los correctos, la 2 está vacía, `RanuraMasReciente()` es la más nueva y una ranura con texto basura sale vacía.
- PlayMode `GuardadoRanurasPruebas.RecorridoGuardarYCargar` (InputTestFixture, `RutaForzada` temporal): Ciudad_Prologo con Ƀ340 y
  respeto 30 en el Tianguis; camina con W; F5 abre las ranuras, ↓ hasta la RANURA 2 y Enter guarda; cambia el dinero y camina más;
  F9, ↓ hasta la RANURA 2 y Enter carga (la escena se recarga por FlujoPartida); comprueba dinero, respeto y posición. Toma la captura.

## Al terminar
- Builds: `ConstruirDemo.ConstruirJuego` → Builds/Juego/Cyberfix.exe y `ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/Cyberfix.exe.
- docs/guia-para-jugar.md, sección de pandillas: 3 líneas de qué probar.
- Bitácora de una línea, fila de docs/estado-tony.md en "hecha", merge a tony/fase-g y a main con push (autorizado en docs/ronda3.md).
  Nunca force push; `git restore` a lo que Unity cambia solo.

## Sensación (Tony)
- ¿Confías en que no pierdes nada? ¿El AUTOGUARDADO aparte se entiende? ¿Cargar te deja donde lo esperabas?

## Fuera de alcance
- Guardar a los pandilleros vivos, los carros robados o el calor; ranuras en la nube; borrar ranuras desde el menú.
