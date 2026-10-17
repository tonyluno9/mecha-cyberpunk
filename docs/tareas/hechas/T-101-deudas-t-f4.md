# T-101 Deudas de T-F4 y pedido de Alan
Dueño: Tony | Rama: tony/T-101-deudas-t-f4 | Modelo: Sonnet 5.5, medium
Depende de: nada

## Objetivo
Antes de cargar el mecha con daño, alarma y grietas, se quitan los pendientes menores que dejó T-F4 (docs/estado-tony.md y
docs/revision-tony.md) y se atiende el pedido de Alan de prender el posproceso en las cámaras del jugador, para que el jugador
vea el bloom y el color de noche de su barrio.

## Contexto
- Decisiones: D-070 (las deudas se arreglan midiendo bien, no aflojando pruebas), D-055 y D-068 (Pruebas_APie y Pruebas_Calle
  las generan sus creadores; nunca se editan a mano). Propuesta: D-tony-53.
- Pendientes de T-F4: el aviso "There are 2 audio listeners" al subir al mecha por código (T-065); el mecha estacionado de
  Pruebas_Calle mira al inicio de la calle y no hacia la salida del taller (T-064, se ve en docs/capturas/T-064-2.png); posible
  fragilidad de `PandillaPruebas` (caso d) y `RecorridoEmboscadaPruebas` (~50 s); capturas T-072-1/2 poco legibles (el banner
  SISTEMAS EN LÍNEA sigue visible y los KO no se distinguen).
- Pedido de Alan (D-alan-19, en su rama alan/fase-2): `UniversalAdditionalCameraData.renderPostProcessing = true` en la cámara de
  `MechaJugador.prefab` y en la de `JugadorAPie.prefab`. En escenas sin Volume (las de Tony) no cambia nada visible.
- Código: Mecha/Montaje/MontajeMecha.cs (prende y apaga cámara y `AudioListener`), Personaje/Jugador/JugadorAPie.cs (apaga los
  suyos al subir), Mecha/Editor/CreadorEscenaCalle.cs (pose de `TallerDePrueba`), Mecha/Editor/ y Personaje/Editor/ (creadores
  de los prefabs).

## Qué hacer
1. Posproceso: en el creador de cada prefab, pon `renderPostProcessing = true` en el `UniversalAdditionalCameraData` de la cámara
   de `MechaJugador.prefab` (la variante `MechaEstacionado.prefab` lo hereda) y de `JugadorAPie.prefab`. Si el asmdef de editor no
   tiene la referencia a `Unity.RenderPipelines.Universal.Runtime`, agrégala (y al asmdef de pruebas que lo compruebe).
2. Dos AudioListener: reproduce el aviso en Pruebas_Calle (subir al mecha con E, encender, bajar con E). Busca en qué frame hay
   dos `AudioListener` prendidos a la vez y corrige el orden en `MontajeMecha` / `JugadorAPie` para que en todo momento haya
   exactamente uno prendido. No borres el `AudioListener` de ninguno de los dos.
3. Orientación: en `CreadorEscenaCalle`, gira `TallerDePrueba` para que su +Z (la salida del taller) apunte hacia la calle por
   donde se sale. Regenera Pruebas_Calle con su creador. Comprueba que las pruebas de llamado y emboscada siguen pasando.
4. Pruebas frágiles: corre `PandillaPruebas` y `RecorridoEmboscadaPruebas` 3 veces seguidas. Si alguna falla, arregla la
   medición (esperas por condición y no por tiempo fijo, tolerancias justificadas en un comentario), nunca quitando lo que mide.
5. Capturas: vuelve a capturar la emboscada como docs/capturas/T-101-1.png (pelea contra los dos pandilleros, sin el banner de
   encendido) y T-101-2.png (los dos pandilleros caídos y distinguibles en el piso).
6. Pruebas:
   - EditMode `Tests/EditMode/Mecha/CamarasPosprocesoPruebas.cs`: las cámaras de `MechaJugador.prefab`, `MechaEstacionado.prefab`
     y `JugadorAPie.prefab` tienen `renderPostProcessing` en true.
   - PlayMode `Tests/PlayMode/Mecha/OyenteUnicoPruebas.cs` (Pruebas_Calle, entrada simulada): en cada frame de subir con E,
     encender (F, Espacio mantenido, Q) y bajar con E hay exactamente un `AudioListener` prendido, y en la consola no aparece
     ningún mensaje que contenga "audio listeners".
   - En la misma prueba o en `LlamadoMechaPruebas`: al arrancar Pruebas_Calle, el ángulo horizontal entre el +Z del mecha
     estacionado y la dirección hacia la calle es menor de 15°.

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Montaje/, Editor/, Prefabs/), Assets/_Proyecto/Personaje/ (Jugador/, Editor/, Prefabs/JugadorAPie.prefab
regenerado por su creador), Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity (con su creador),
Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/IA/,
Assets/_Proyecto/Tests/PlayMode/Pelea/, docs/capturas/
No tocar: Core/, Pruebas_Tony.unity, Pruebas_APie.unity (salvo que su creador la regenere sin cambios de contenido), las carpetas y
escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] CamarasPosprocesoPruebas y OyenteUnicoPruebas pasan
- [ ] `PandillaPruebas` y `RecorridoEmboscadaPruebas` pasan en 3 corridas seguidas (el verificador lo repite)
- [ ] El ángulo del mecha estacionado con la calle es menor de 15° (prueba automática)
- [ ] Capturas docs/capturas/T-101-1.png y T-101-2.png revisadas: sin banner de encendido y con los pandilleros caídos visibles
- [ ] Consola sin errores ni el aviso de dos AudioListener durante las pruebas de montaje

## Criterios de sensación (para un humano)
- En Pruebas_Calle, al salir del taller con el mecha, ¿se sale de frente hacia la calle sin tener que girar?

## Pruebas automáticas
- EditMode: CamarasPosprocesoPruebas. PlayMode: OyenteUnicoPruebas y las de IA y Pelea repetidas.

## Fuera de alcance
- Agregar Volumes o posproceso a las escenas de Tony; cambiar valores de sensación del mecha o de la pandilla
