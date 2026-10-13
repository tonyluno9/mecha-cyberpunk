# T-075 Subir y bajar del carro, y sus cámaras
Dueño: Alan | Rama: alan/T-075-carro-subir-bajar-camaras | Modelo: Sonnet 5.5, medium
Depende de: T-074. Contrato `IMontable` (Core/Jugador/IMontable.cs, T-050, ya en main).

## Objetivo
El carro viejo cumple `IMontable`: cualquier jugador (el de Tony o la cámara libre de Alan) se sube, maneja con la cámara del
carro en primera persona o, con C, en tercera, y se baja cuando el carro va casi detenido. La cámara libre de Alan aprende a
subirse con E, para que Alan pruebe el carro en su barrio sin el jugador de Tony. Es el lado del carro del hito H3.

## Contexto
- Contrato: Core/Jugador/IMontable.cs (lee el comentario completo: quién prende cada cámara, quién lee la E, el jugador sigue al
  `Asiento` en `LateUpdate` con su `CharacterController` apagado, `IntentarBajar` entrega una pose libre en el piso) e IJugador.cs.
- Cómo lo usa Tony: docs/tareas/pendientes/T-058-subir-bajar-carro.md (su doble `CarroDePrueba`: `PuedeBajar` solo a menos de
  0.5 m/s, pose de salida junto al `PuntoSubida`) y docs/tareas/bloqueadas/T-061-subir-carro-alan.md (su prueba se pone a 1.5 m
  del `PuntoSubida` mirándolo, sube con E, maneja "con las teclas de Alan", se detiene y baja con E; con el carro en marcha E no
  baja). Propuesta D-tony-22: el jugador busca montables en `Registro<IMontable>` a 2.5 m o menos del `PuntoSubida` y a 60° o menos
  de hacia dónde mira.
- Diseño: personaje-progresion.md (tercera persona opcional al manejar). Propuestas D-alan-11 a D-alan-14.
- La cámara libre (Mundo/Pruebas/CamaraLibre.cs) es `IJugador` y hoy siempre es APie.

## Qué hacer
1. `CarroViejo` implementa `IMontable` (`Modo` Carro, `Nombre` "Carro viejo"). Hijos nuevos en el prefab: `Asiento` (asiento del
   conductor, lado izquierdo), `PuntoSubida` (en el piso, 1 m afuera de la puerta del conductor), `CamaraCarro` (Camera +
   `AudioListener`, los dos apagados al empezar). Se registra en `Registro<IMontable>` en `OnEnable` y se quita en `OnDisable`.
   - `PuedeSubir`: no ocupado. `IntentarSubir`: prende cámara, listener y entrada (`TomarControl(true)`), `Ocupado` y avisa
     `OcupadoCambio(true)` una vez.
   - `PuedeBajar`: ocupado, rapidez menor a 0.5 m/s y hay una salida libre. La salida es junto al `PuntoSubida`; si una cápsula de
     persona (radio 0.4 m, alto 1.8 m) choca ahí, prueba del lado del copiloto; si las dos chocan, no puede bajar.
     `IntentarBajar`: apaga cámara, listener y entrada, deja de estar ocupado, avisa `OcupadoCambio(false)` y da la pose en el
     piso mirando como el carro.
2. Cámara del carro, `Vehiculos/Carro/CamaraCarro.cs` y valores en `ConfigCamaraCarro` (+ asset):
   - Primera persona (por defecto): ojos del conductor a unos 1.15 m sobre el asiento; el ratón mira hasta ±110° a los lados y
     ±60° arriba y abajo, y vuelve sola al frente tras 1.5 s sin mover el ratón.
   - C alterna con tercera persona: detrás y arriba (unos 6 m y 2.5 m), sigue con suavidad y se acerca si un muro queda entre
     la cámara y el carro (rayo, sin atravesar edificios). La elección se recuerda entre una subida y otra.
3. Cámara libre como jugador de prueba (Mundo/Pruebas/CamaraLibre.cs): con E, si no hay puerta al frente, busca en
   `Registro<IMontable>` el que `PuedeSubir` más cercano a 2.5 m o menos del `PuntoSubida` y a 60° o menos de su mirada, y se
   sube cumpliendo el contrato: apaga su cámara, su `AudioListener` y su `CharacterController`, ignora Mover, Mirar y vuelo,
   cambia `Modo`, avisa `ModoCambio` y `Camara` pasa a ser la del carro; en `LateUpdate` sigue al `Asiento`. Con E dentro llama
   `IntentarBajar`; si baja, `Colocar(pose)`, prende lo suyo, `Modo` APie y avisa `ModoCambio`. Si no puede bajar, escribe una vez
   "[Mundo] Frena para bajar" en la consola. La cámara libre no conoce la clase del carro (solo `IMontable`).
4. El creador del carro (T-074) agrega los hijos y la cámara. Documenta al inicio de `CarroViejo.cs` las teclas y la regla de
   `PuedeBajar`, que Tony lee para T-061.

## Carpetas permitidas
Assets/_Proyecto/Vehiculos/, Assets/_Proyecto/Mundo/Pruebas/, Assets/_Proyecto/Mundo/Datos/ (solo ConfigCamaraLibre si hace
falta la distancia de subida), Assets/_Proyecto/Mundo/Prefabs/CamaraLibre.prefab, Assets/_Proyecto/Tests/EditMode/Vehiculos/,
Assets/_Proyecto/Tests/PlayMode/Vehiculos/ y Assets/_Proyecto/Tests/PlayMode/Mundo/
No tocar: Core/ (si el contrato no alcanza, se anota en docs/revision-alan.md y se sigue), las escenas, las carpetas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (incluidas CamaraLibrePruebas y las pruebas de T-074 sin cambios de criterio)
- [ ] PlayMode con un doble de jugador definido en la prueba (implementa `IJugador`, sin la cámara libre): `IntentarSubir`
      devuelve true una vez y false si ya está ocupado; `OcupadoCambio(true)` llega una vez; la cámara del carro y su listener
      están prendidos solo mientras está ocupado
- [ ] PlayMode: sin ocupar, W no mueve el carro; ocupado, W 3 s lo mueve más de 15 m
- [ ] PlayMode: a más de 0.5 m/s `PuedeBajar` es false e `IntentarBajar` devuelve false sin avisar; detenido, baja y la pose
      queda en el piso (rayo hacia abajo a menos de 0.1 m), fuera de los colliders del carro, y una cápsula de 0.4 × 1.8 m ahí no
      choca con nada
- [ ] PlayMode: con un muro pegado a la puerta del conductor, la salida es del lado del copiloto; con muros en los dos lados,
      `PuedeBajar` es false
- [ ] PlayMode con entrada simulada y la cámara libre: a 1.5 m del `PuntoSubida` mirándolo, E sube (`Modo` Carro,
      `ModoCambio(Carro)` una vez, `IJugador.Camara` es la del carro); manejando, su `Raiz` queda a menos de 0.3 m del `Asiento`;
      detenido, E baja (`Modo` APie, `ModoCambio(APie)` una vez) y con W 1 s camina otra vez
- [ ] PlayMode: subir y bajar 10 veces seguidas deja en toda la escena exactamente un `AudioListener` y una cámara prendidos
      después de cada cambio, y la consola sin errores
- [ ] PlayMode: C cambia a tercera persona y otra C regresa; en tercera, con un muro entre el carro y la posición ideal, la cámara
      queda del lado del carro (rayo cámara-carro sin choques)
- [ ] Capturas en primera persona (docs/capturas/T-075-1.png) y en tercera (T-075-2.png), manejando en el piso de prueba

## Criterios de sensación (para un humano)
- Subir y bajar es inmediato; el cambio de cámara no marea.
- La primera persona deja ver el camino; la tercera persona sigue al carro sin tirones ni temblor.

## Pruebas automáticas
- EditMode (Tests/EditMode/Vehiculos/): la lógica pura de la salida libre y de la cámara (límites de mirada, regreso al frente).
- PlayMode (Tests/PlayMode/Vehiculos/): MontableCarroPruebas (con el doble); (Tests/PlayMode/Mundo/): CamaraLibreCarroPruebas.

## Fuera de alcance
- El jugador de Tony subiendo al carro (T-061, de Tony); el carro en el barrio (T-078)
- Animación de abrir la puerta, tablero con velocímetro, HUD (el HUD es de Tony)
