# T-056 HUD a pie: vida, mira, arma y canción
Dueño: Tony | Rama: tony/T-056-hud-a-pie-vida-arma | Modelo: Sonnet 5.5, medium
Depende de: T-052 (vida a pie), T-053 (armas y balas)

## Objetivo
A pie se ve un HUD mínimo y flotante: la vida, una mira, el arma en la mano con sus balas y la canción que suena. En el mecha
no se ve nada. Es la primera mitad del HUD de estetica-hud.md; el objetivo y los avisos de interacción llegan en T-057.

## Contexto
- Diseño: docs/diseno/estetica-hud.md ("a pie hay un HUD mínimo flotante con vida, objetivo y la canción que suena"; "en el mecha
  todo es diegético"; neón azul, magenta, naranja y turquesa).
- Decisiones: D-010 (uGUI `Text`, sin TextMeshPro), D-011 (en el mecha nada flota). Propuestas: D-tony-17 (UI referencia
  Personaje y Progresion), D-tony-20 (HUD por modo y canción de espera) en docs/decisiones-pendientes-tony.md.
- Patrón (solo leer): UI/Cabina/PantallaVidaCabina.cs (barra con `Image` rellena y texto, actualiza con `Cambio`) y su config.
- El HUD encuentra al jugador por `Registro<IJugador>` (en `Start` o con `Agregado`), y en su `Raiz` busca una vez `IVida` y
  `ArmasAPie`. Para leer `ArmasAPie`, `MechaCyberpunk.UI` pasa a referenciar `MechaCyberpunk.Personaje` (mismo dueño, esta
  tarea lo pide). Personaje NO referencia UI.
- Radio es de T-F7: la canción se muestra con un método público y, mientras nadie lo llame, un texto de espera.

## Qué hacer
1. `UI/Datos/ConfigHudAPie.cs`: colores (vida en turquesa, vida baja en magenta, texto principal, aviso en naranja), umbral de
   vida baja (30 %), texto de espera de la canción ("RADIO · sin señal"), tamaños y márgenes. Asset `ConfigHudAPie_Base.asset`.
2. Lógica pura `UI/HudAPie/FormatoHud.cs`: texto de vida ("VIDA 75"), fracción de la barra, si es vida baja, y texto del arma
   ("PUÑOS", "PISTOLA 7/8", "PISTOLA · RECARGANDO", "PISTOLA 0/8 · R").
3. `UI/HudAPie/HudAPie.cs` en un Canvas Screen Space Overlay:
   - Abajo a la izquierda: barra de vida y texto; se actualiza con `IVida.Cambio`, no en cada frame.
   - Centro: mira pequeña (un punto con cruz delgada).
   - Abajo a la derecha: arma y balas; se actualiza con el evento de `ArmasAPie`.
   - Arriba a la izquierda: canción, con `MostrarCancion(string)` público y el texto de espera al empezar.
   - Por modo, escuchando `IJugador.ModoCambio`: APie muestra todo; Carro muestra solo la canción (T-057 suma el objetivo);
     Mecha no muestra nada. Sin jugador registrado no muestra nada.
4. `UI/Editor/MechaCyberpunk.UI.Editor.asmdef` (solo Editor) y `UI/Editor/CreadorHudAPie.cs` que genera
   `UI/Prefabs/HudAPie.prefab`. En Pruebas_APie, el creador de Personaje (Personaje/Editor) agrega una instancia del HUD;
   el anidado dentro del prefab del jugador es de T-059.

## Carpetas permitidas
Assets/_Proyecto/UI/ (incluido su asmdef), Assets/_Proyecto/Personaje/Editor/ (solo el creador de la escena),
Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity, Assets/_Proyecto/Tests/EditMode/UI/ y Assets/_Proyecto/Tests/PlayMode/UI/
No tocar: Core/, el resto de Personaje/, Mecha/ y la pantalla de la cabina, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (la prueba ReglasEnsamblados acepta UI → Personaje porque son del mismo dueño)
- [ ] EditMode FormatoHud: textos de vida y de arma de los casos del paso 2, fracción entre 0 y 1, vida baja en el umbral
- [ ] PlayMode: con `JugadorAPie.prefab` y `HudAPie.prefab` instanciados, el texto dice la vida completa; tras `RecibirDanio` de
      25, en el mismo frame o el siguiente dice 75 y la barra está al 75 % (±1 %); por debajo del 30 % la barra cambia al color de
      vida baja
- [ ] PlayMode con entrada simulada: con 2 el arma dice "PISTOLA 8/8"; tras un disparo, "PISTOLA 7/8"; con R, "RECARGANDO"
      y luego 8/8; con 1, "PUÑOS"
- [ ] PlayMode: `MostrarCancion("Cumbia del Chip")` cambia el texto de la canción; sin llamarlo se ve el texto de espera
- [ ] PlayMode con un `JugadorFalso : IJugador` (definido en la prueba) que avisa `ModoCambio`: en Mecha ningún elemento del HUD
      está activo; en Carro solo la canción; de vuelta en APie, todo
- [ ] PlayMode: si el jugador se registra después que el HUD (orden de `Start` invertido), el HUD lo encuentra por `Agregado`
- [ ] Captura del HUD a pie en Pruebas_APie con la pistola y vida a medias: docs/capturas/T-056-1.png; los textos se leen a
      1920 × 1080 y a 1280 × 720 sin taparse
- [ ] Consola sin errores en Pruebas_APie disparando y recibiendo daño con H

## Criterios de sensación (para un humano)
- El HUD se siente mínimo: no estorba la vista y se lee de un vistazo.
- Los colores se sienten del mundo (neón cyberpunk), no de un menú genérico.

## Pruebas automáticas
- EditMode (Tests/EditMode/UI/): FormatoHudPruebas.
- PlayMode (Tests/PlayMode/UI/): HudAPiePruebas, con InputTestFixture para las armas y `Registros.LimpiarTodos()` en SetUp y TearDown.

## Fuera de alcance
- Objetivo, flecha, aviso de "regresa" e interacción (T-057)
- Radio de verdad y cambio de estación (T-F7); mapa o minimapa; números de daño
- Pantallas de la cabina del mecha (siguen como están)
