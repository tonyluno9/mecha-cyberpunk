# T-051 Jugador a pie: movimiento en primera persona, IJugador y escena de pruebas
Dueño: Tony | Rama: tony/T-051-jugador-a-pie | Modelo: Sonnet 5.5, medium
Depende de: nada nuevo (usa `IJugador` y `Registro<T>`, ya en main). Conviene hacerla después de T-050 para leer su comentario de `IJugador`.

## Objetivo
Existe el jugador a pie: una persona en primera persona que camina, corre, salta y mira con el ratón, con su prefab
`JugadorAPie.prefab`, y que se registra como el `IJugador` del juego para que las puertas, zonas y objetivos lo encuentren.
También la escena de pruebas a pie de Tony, donde las tareas siguientes agregan lo suyo. Es la base de todo T-F3 y del hito H2.

## Contexto
- Diseño: docs/diseno/personaje-progresion.md (primera persona a pie; implante de piernas: corres y saltas más, por eso la
  velocidad y el salto son datos), docs/diseno/ciudad.md (el barrio se recorre a pie y en carro), docs/diseno/estetica-hud.md.
- Decisiones: D-008 (solo Input System), D-009 (sin capas ni tags propios), D-014 (1 unidad = 1 m), D-026, D-029 (escenas en
  Escenas/Tony/). Propuestas: D-tony-18 (controles a pie), D-tony-23 (escena y prefab) en docs/decisiones-pendientes-tony.md.
- Contrato: lee los `///` de Core/Jugador/IJugador.cs. El componente que implementa `IJugador` va en la raíz, que tiene el
  `CharacterController`, para que los triggers del mundo lo encuentren con `GetComponentInParent<IJugador>()`.
- Patrón a seguir (solo leer): Mecha/Movimiento/ControladorMecha.cs (entrada con `InputActionReference`, cursor, lógica pura
  aparte en MovimientoPesado), Mecha/Editor/CreadorEscenaMecha.cs (escena y prefab con código de editor),
  Tests/PlayMode/Mecha/ControladorMechaPruebas.cs (InputTestFixture, piso creado en la prueba, datos propios de la prueba).
- Alan usa una cámara libre que también implementa `IJugador`, con ojos a 1.75 m, caminar a 4.5 m/s y correr a 8 m/s: el
  jugador a pie debe caber por los mismos huecos (puertas de unos 1 m de ancho y 2.2 m de alto).
- Personaje/ hoy solo referencia Core y Unity.InputSystem; no hace falta más en esta tarea.

## Qué hacer
1. `Personaje/Entrada/ControlesAPie.inputactions`, mapa "APie", con TODAS las acciones a pie (las tareas siguientes solo las
   conectan): Mover (WASD), Mirar (delta del ratón), Correr (Shift izquierdo), Saltar (Espacio), Interactuar (E),
   Atacar (clic izquierdo), ArmaPunos (1), ArmaPistola (2), Recargar (R), LiberarCursor (la misma tecla que en ControlesMecha).
2. `Personaje/Datos/DatosPersonaje.cs` (ScriptableObject, estadísticas que cambiarán los implantes): vida máxima (100),
   velocidad al caminar (4.5 m/s), al correr (7.5 m/s), altura de salto (1.1 m). Asset `Personaje/Datos/DatosPersonaje_Base.asset`.
3. `Personaje/Datos/ConfigControlesAPie.cs` (sensación): sensibilidad del ratón (°/unidad de delta), límite de inclinación
   (85°), aceleración y frenado en el piso (m/s²), control en el aire (0 a 1), gravedad (m/s²), altura de los ojos (1.65 m),
   cabeceo al caminar (amplitud y frecuencia). Asset `ConfigControlesAPie_Base.asset`.
4. Lógica pura en `Personaje/Movimiento/`: `MovimientoAPie` (velocidad horizontal con aceleración y frenado, caminar o correr,
   velocidad vertical con gravedad y salto solo en el piso) y `MiradaAPie` (giro e inclinación con límite). Reciben `dt`.
5. `Personaje/Movimiento/ControladorAPie.cs` (MonoBehaviour, requiere `CharacterController`): lee Mover, Mirar, Correr, Saltar
   y LiberarCursor; bloquea el cursor al habilitarse; `stepOffset` 0.35 m y `slopeLimit` 45°. Expone de solo lectura la
   velocidad, si está en el piso, la inclinación y la cámara. `ActivarControl(bool)` lo congela (lo usarán el KO y el carro).
6. `Personaje/Jugador/JugadorAPie.cs : MonoBehaviour, IJugador` en la raíz: `Raiz` su transform, `Modo` APie (el cambio de modo
   llega en T-058), `Camara` la cámara hija, `ModoCambio` (todavía no se dispara), `Colocar(pose)` apagando y prendiendo el
   `CharacterController`. Se agrega a `Registro<IJugador>` en `OnEnable` y se quita en `OnDisable`. Si al habilitarse ya hay
   otro `IJugador` registrado, escribe un `Debug.LogWarning("[Personaje] ...")` una vez (el contrato dice que hay uno solo).
7. `Personaje/Editor/MechaCyberpunk.Personaje.Editor.asmdef` (solo Editor; referencia Core, Personaje y Unity.InputSystem)
   y `Personaje/Editor/CreadorJugadorAPie.cs` con un menú que genera:
   - `Personaje/Prefabs/JugadorAPie.prefab`: raíz con `CharacterController` (alto 1.8 m, radio 0.35 m, centro a 0.9 m),
     `JugadorAPie` y `ControladorAPie`; hijo `Camara` a 1.65 m con `Camera` (tag MainCamera) y `AudioListener`; hijo `Manos`
     delante de la cámara (vacío; lo usan T-052 y T-053); hijo `Cuerpo` con una cápsula greybox en modo "solo sombras".
   - `Escenas/Tony/Pruebas_APie.unity`: piso de 120 × 120 m, luz direccional, un muro de 1.2 m de alto, un escalón de 0.3 m,
     una rampa de 30° y una de 50°, unos cubos de 3 a 10 m como "edificios", y el `JugadorAPie` en el centro. El creador es la
     única forma de cambiar esta escena: las tareas siguientes agregan su parte al creador y la regeneran.
   - Materiales greybox en `Personaje/Materiales/`.

## Carpetas permitidas
Assets/_Proyecto/Personaje/, Assets/_Proyecto/Escenas/Tony/, Assets/_Proyecto/Tests/EditMode/Personaje/ y
Assets/_Proyecto/Tests/PlayMode/Personaje/
No tocar: Core/, Mecha/ (solo leerlo), las carpetas y escenas de Alan, Pruebas_Tony.unity, ProjectSettings/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa y `verificar.sh ensamblados` lista MechaCyberpunk.Tests.PlayMode.Personaje
- [ ] EditMode MovimientoAPie y MiradaAPie: acelera hasta la velocidad de caminar o correr sin pasarse; frena a 0 al soltar;
      salta solo en el piso y la altura máxima de un salto calculado es la de los datos (±5 %); la inclinación nunca pasa del límite
- [ ] PlayMode con entrada simulada (prefab real, piso creado en la prueba): con W 2 s avanza entre 7.5 y 9.5 m; con Shift y W
      2 s, entre 12.5 y 15.5 m; al soltar se detiene en menos de 0.5 s
- [ ] PlayMode con entrada simulada: Espacio en el piso sube los pies entre 0.9 y 1.3 m; Espacio en el aire no hace nada
- [ ] PlayMode con entrada simulada: sube el escalón de 0.3 m y la rampa de 30° caminando; no sube el muro de 1.2 m ni la rampa
      de 50° (queda abajo después de 4 s de W)
- [ ] PlayMode con entrada simulada: un delta del ratón en X gira el cuerpo, en Y inclina la cámara, y un delta enorme en Y deja
      la inclinación en el límite de 85° y no más
- [ ] PlayMode: con el prefab en escena, `Registro<IJugador>.Actual` es su `JugadorAPie`, `Raiz` es la raíz, `Modo` es APie y
      `Camara` es la cámara hija; `Colocar` lo mueve a la pose aunque esté detrás de un muro; al desactivarlo sale del registro
- [ ] PlayMode: un trigger creado en la prueba recibe `OnTriggerEnter` cuando el jugador camina dentro con W, y
      `GetComponentInParent<IJugador>()` desde el collider que entra devuelve el `JugadorAPie` (lo que harán las zonas de Alan)
- [ ] Captura desde el jugador en Pruebas_APie mirando la rampa y los cubos: docs/capturas/T-051-1.png
- [ ] Consola sin errores ni avisos al dar Play en Pruebas_APie.unity

## Criterios de sensación (para un humano)
- Caminar y mirar se siente como un shooter en primera persona normal: responde al instante, sin patinar ni flotar.
- El salto y la carrera se sienten humanos, claramente distintos del mecha pesado.
- La altura de los ojos se siente de persona (ni enano ni gigante) junto a los cubos y la rampa.

## Pruebas automáticas
- EditMode (Tests/EditMode/Personaje/): MovimientoAPiePruebas, MiradaAPiePruebas.
- PlayMode (Tests/PlayMode/Personaje/): ControladorAPiePruebas y JugadorAPiePruebas con InputTestFixture (teclado y ratón
  simulados) y datos propios de la prueba (no tocar los assets del proyecto).

## Fuera de alcance
- Vida, golpes y pistola (T-052, T-053); puertas (T-054); HUD (T-056, T-057); carro (T-058)
- Aparecer en un `IPuntoAparicion` (T-059); agacharse, nadar, escaleras de mano; cuerpo visible o animado
