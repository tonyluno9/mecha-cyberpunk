# T-058 Subir y bajar del carro (lado del jugador) con un carro de prueba
Dueño: Tony | Rama: tony/T-058-subir-bajar-carro | Modelo: Sonnet 5.5, medium
Depende de: T-050 en main (`IMontable`), T-054 (selector de interacción), T-057 (avisos del HUD)

## Objetivo
El jugador se sube con E a cualquier `IMontable` y se baja con E: cambia de modo (APie a Carro y de vuelta), de cámara, deja de
caminar y va dentro del carro hasta bajarse. Se prueba con un carro de prueba propio de Tony que cumple el contrato como lo hará
el carro de Alan (A-F2) y, en T-F4, el mecha. Es el lado del jugador del hito H3.

## Contexto
- Diseño: docs/diseno/ciudad.md (solo usas tus carros; hay que manejar para llegar), docs/diseno/personaje-progresion.md
  (tercera persona opcional al manejar: es cosa de la cámara del carro).
- Contratos: Core/Jugador/IMontable.cs (T-050: quién prende cada cámara, quién lee la E, el jugador sigue al `Asiento` con su
  `CharacterController` apagado, `IntentarBajar` da la pose de salida), IJugador.cs (`Modo`, `Camara`, `ModoCambio`, `Colocar`).
- Propuestas D-tony-14 y D-tony-22 en docs/decisiones-pendientes-tony.md.
- La E es la acción Interactuar de ControlesAPie (T-051): debe seguir habilitada en modo Carro aunque el resto del control a pie
  esté apagado.

## Qué hacer
1. `InteraccionAPie`: suma como candidatos los `IMontable` de `Registro<IMontable>` con `PuedeSubir`, usando su `PuntoSubida`
   y el mismo `SelectorInteraccion`. Expone el tipo de candidato (Puerta o Montable) y el montable candidato.
2. Lógica pura `Personaje/Jugador/ReglaMontar.cs`: ¿qué hace la E? Con el modo actual, si está en KO, el candidato y lo que dice el
   montable (`PuedeSubir`, `PuedeBajar`): Nada, AbrirPuerta, Subir, Bajar o NoPuedeBajar.
3. `JugadorAPie`: con Subir llama `IntentarSubir(this)`; si sube: apaga `ControladorAPie`, `ArmasAPie` e `InteraccionAPie` (salvo
   la E), su `CharacterController`, su cámara y su `AudioListener`; esconde `Manos`; `Modo` = el del montable; `Camara` = la del
   montable; avisa `ModoCambio` una vez. En `LateUpdate` sigue al `Asiento`. Con Bajar llama `IntentarBajar(out pose)`; si baja,
   `Colocar(pose)`, prende todo lo que apagó, `Modo` = APie, `Camara` = la suya y avisa `ModoCambio`. Con NoPuedeBajar avisa un
   evento para el HUD.
4. Doble `Personaje/Pruebas/CarroDePrueba.cs : MonoBehaviour, IMontable`: caja greybox de 4.2 × 1.5 × 2 m con collider y un
   `Rigidbody` cinemático; hijos `Asiento`, `Camara` (atrás y arriba, tercera persona, con `AudioListener`; las dos apagadas al
   empezar) y `PuntoSubida` a 1 m del lado izquierdo. Mientras está ocupado lee su propia `InputAction` de manejo (flechas, para no
   chocar con WASD en las pruebas) y avanza y gira de forma simple (12 m/s máximo, valores en un `ConfigCarroDePrueba`).
   `PuedeBajar` solo con velocidad menor a 0.5 m/s; `IntentarBajar` devuelve una pose en el piso junto a `PuntoSubida`, mirando
   como el carro. Se registra en `Registro<IMontable>`. Prefab `Personaje/Pruebas/CarroDePrueba.prefab`.
5. `TextoInteraccion` y `HudAPie` (UI/): "E · Subir <Nombre>" con un montable candidato y "Frena para bajar" con NoPuedeBajar.
6. Creador de la escena: un `CarroDePrueba` en Pruebas_APie, en una calle despejada de 60 m. Regenera.

## Carpetas permitidas
Assets/_Proyecto/Personaje/, Assets/_Proyecto/UI/HudAPie/, Assets/_Proyecto/UI/Datos/,
Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity, Assets/_Proyecto/Tests/EditMode/Personaje/,
Assets/_Proyecto/Tests/PlayMode/Personaje/, Assets/_Proyecto/Tests/EditMode/UI/ y Assets/_Proyecto/Tests/PlayMode/UI/
No tocar: Core/, Vehiculos/ y todo lo de Alan, Mecha/ (el mecha como montable es de T-F4).

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode ReglaMontar: todos los casos (a pie con montable candidato que puede subir: Subir; con puerta: AbrirPuerta; en
      carro y puede bajar: Bajar; en carro sin poder bajar: NoPuedeBajar; en KO: Nada; sin candidato: Nada)
- [ ] PlayMode con entrada simulada: a 1.5 m del `PuntoSubida` mirando al carro, E sube: `Modo` es Carro, `ModoCambio(Carro)`
      avisa una vez, `IJugador.Camara` es la cámara del carro y está prendida, la del jugador apagada, y en toda la escena hay
      exactamente un `AudioListener` habilitado
- [ ] PlayMode con entrada simulada: en el carro, la flecha arriba 3 s lo mueve más de 15 m y la `Raiz` del jugador queda a menos
      de 0.3 m del `Asiento`; W y clic izquierdo no mueven ni atacan a la persona; un trigger de prueba que el carro cruza no ve
      ningún collider con `IJugador` en sus padres (el `CharacterController` del jugador está apagado, como dice el contrato)
- [ ] PlayMode con entrada simulada: E con el carro a más de 0.5 m/s no baja y el HUD dice "Frena para bajar"; ya detenido, E baja:
      `Modo` APie, `ModoCambio(APie)` una vez, el jugador queda en el piso junto a la puerta (fuera del collider del carro) con su
      cámara prendida, y con W 1 s camina otra vez
- [ ] PlayMode: el HUD dice "E · Subir Carro de prueba" frente al carro; en el carro muestra el objetivo y la canción y oculta vida,
      mira y arma
- [ ] PlayMode: subir y bajar 10 veces seguidas no deja cámaras ni `AudioListener` prendidos de más ni errores en la consola
- [ ] Capturas: frente al carro con el aviso (docs/capturas/T-058-1.png) y manejando con el objetivo en el HUD (T-058-2.png)

## Criterios de sensación (para un humano)
- Subir y bajar se siente inmediato y claro; el cambio de cámara no marea.
- Al bajar, el jugador aparece donde se espera (junto a la puerta), mirando hacia donde iba el carro.

## Pruebas automáticas
- EditMode (Tests/EditMode/Personaje/): ReglaMontarPruebas; (Tests/EditMode/UI/) los casos nuevos de TextoInteraccionPruebas.
- PlayMode (Tests/PlayMode/Personaje/): MontarCarroPruebas con InputTestFixture.

## Fuera de alcance
- El carro de verdad, su manejo y su cámara (Alan, A-F2); probar con el carro de Alan (T-061, espera A-F2)
- Subir al mecha (T-F4); bajar a la fuerza; animación de abrir la puerta del carro
