# T-055 Servicio de objetivos: registrar ModeloObjetivos y seguir al jugador
Dueño: Tony | Rama: tony/T-055-servicio-objetivos | Modelo: Sonnet 5.5, medium
Depende de: T-050 en main (`UbicacionObjetivo`)

## Objetivo
El objetivo de la historia existe de verdad en la escena: un componente registra el `IObjetivos` en `Registro<IObjetivos>`,
deja fijar y quitar objetivos, y sigue al jugador para saber a qué distancia está, si se está alejando y cuándo llegó. Es lo
que el HUD (T-057) dibuja y lo que el director del prólogo (T-F6) usará para guiar de vuelta al jugador. Cierra el pendiente de
la revisión de T-F2: "nadie registra todavía IObjetivos".

## Contexto
- Diseño: docs/diseno/demo.md (caso límite: "el jugador intenta ir a otro lado durante el prólogo: un objetivo lo guía de
  vuelta"), docs/diseno/estetica-hud.md (a pie se ve el objetivo).
- Ya existe: Progresion/Objetivos/ModeloObjetivos.cs (lógica pura de `IObjetivos`, con pruebas). No lo reescribas: envuélvelo.
- Contratos: Core/Objetivos/IObjetivos.cs, Objetivo.cs, UbicacionObjetivo.cs (T-050); Core/Mundo/ILugar.cs y EventosLugares.cs;
  Core/Jugador/IJugador.cs; Core/Servicios/Registro.cs (quien busca lo hace en `Start` o se suscribe a `Agregado`).
- Revisión de T-F2 (docs/revision-tony.md): sugerencia de una prueba de PlayMode de `Registro<T>` con un MonoBehaviour real.
  Esta tarea la cubre.
- Propuesta D-tony-21 (servicio y guía de vuelta) en docs/decisiones-pendientes-tony.md.

## Qué hacer
1. `Progresion/Datos/ConfigSeguimientoObjetivo.cs`: radio de llegada (4 m), margen para avisar que se aleja (25 m más que la mejor
   distancia alcanzada), margen para dejar de avisar (10 m), tiempo mínimo alejándose antes de avisar (1.5 s). Asset
   `ConfigSeguimientoObjetivo_Base.asset` (créalo desde Progresion/Editor/CreadorDatosProgresion.cs).
2. Lógica pura `Progresion/Objetivos/SeguimientoObjetivo.cs`: con el objetivo y su posición resuelta, y cada `Avanzar(dt,
   posicionJugador)`: distancia horizontal actual, mejor distancia desde que se fijó, `SeAleja` (con los dos márgenes, para que
   no parpadee) y `Llego` (dentro del radio, o porque se avisó `MarcarLlegada`); avisa `SeAlejaCambio(bool)` y `Llegada` una sola
   vez por objetivo. Al cambiar el objetivo se reinicia todo.
3. `Progresion/Objetivos/ServicioObjetivos.cs : MonoBehaviour, IObjetivos`: tiene un `ModeloObjetivos` y le pasa `HayObjetivo`,
   `Actual` y `Cambio`; `Fijar(Objetivo)` y `Quitar()` públicos para los sistemas de Tony. Se agrega a `Registro<IObjetivos>` en
   `OnEnable` y se quita en `OnDisable`. Cada frame resuelve la posición con `UbicacionObjetivo.Resolver(Actual,
   Registro<ILugar>.Todos, ...)` y avanza el seguimiento con `Registro<IJugador>.Actual.Raiz.position` (si no hay jugador o no se
   resuelve la posición, no avanza). Escucha `EventosLugares.JugadorEntro`: si entra al lugar del objetivo, cuenta como llegada.
   Expone el seguimiento (distancia, `SeAleja`, `Llego`, si la posición se resolvió y la posición).
4. `Progresion/Pruebas/FijadorObjetivoDePrueba.cs`: lista de objetivos en el inspector (texto y, o un `IdLugar`, o un Transform
   cuya posición se usa); fija el primero en `Start`; con su `InputAction` (tecla O) pasa al siguiente; con la opción
   "avanzar al llegar" pasa al siguiente con `Llegada`. Si necesitas Unity.InputSystem en Progresion, agrégalo a su asmdef.
5. Prefab `Progresion/Prefabs/ServiciosProgresion.prefab` (generado por el creador) con `ServicioObjetivos`. Más adelante llevará
   los servicios de calor y cartera. Todavía no se pone en ninguna escena (lo hace T-059).

## Carpetas permitidas
Assets/_Proyecto/Progresion/ (incluido su asmdef), Assets/_Proyecto/Tests/EditMode/Progresion/ y
Assets/_Proyecto/Tests/PlayMode/Progresion/
No tocar: Core/, Personaje/, UI/, las carpetas y escenas de Alan, las escenas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode SeguimientoObjetivo: la mejor distancia solo baja; `SeAleja` se prende al pasar el margen durante el tiempo mínimo y
      se apaga al volver dentro del margen menor (sin parpadeo en el borde); `Llegada` avisa una sola vez dentro del radio; al
      cambiar de objetivo se reinicia; sin posición resuelta no avisa nada
- [ ] PlayMode (MonoBehaviour real): con `ServiciosProgresion.prefab` instanciado, `Registro<IObjetivos>.Actual` es su servicio;
      al desactivarlo sale del registro y `Quitado` avisa una vez; al reactivarlo vuelve y `Agregado` avisa una vez
- [ ] PlayMode: `Fijar` con un objetivo de lugar avisa `Cambio` con ese objetivo; con un `LugarFalso : ILugar` registrado (definido
      en la prueba) la posición resuelta es su `PosicionEntrada`; `Fijar(Objetivo.Ninguno)` equivale a `Quitar`
- [ ] PlayMode: con un `JugadorFalso : IJugador` registrado (definido en la prueba) que se mueve por código lejos del objetivo,
      `SeAleja` se prende; al regresar se apaga; al llegar al radio, `Llegada` avisa una vez
- [ ] PlayMode: `EventosLugares.PublicarEntro(Bar)` con el objetivo en el Bar avisa `Llegada` aunque el jugador esté lejos de la
      entrada; `PublicarEntro(Casino)` no
- [ ] PlayMode: `FijadorObjetivoDePrueba` con dos objetivos y "avanzar al llegar" pasa del primero al segundo al llegar
- [ ] Consola sin errores en todas estas pruebas

## Criterios de sensación (para un humano)
- Ninguno en esta tarea (se juzga con el HUD en T-057 y en el recorrido de T-059).

## Pruebas automáticas
- EditMode (Tests/EditMode/Progresion/): SeguimientoObjetivoPruebas.
- PlayMode (Tests/PlayMode/Progresion/): ServicioObjetivosPruebas, con `Registros.LimpiarTodos()` y
  `EventosLugares.LimpiarSuscriptores()` en SetUp y TearDown.

## Fuera de alcance
- Dibujar el objetivo (T-057); secuencia de objetivos del prólogo (T-F6); misiones (T-F7)
- Registrar `ICalor`, `ICartera` o `ITienda` (T-F4 y T-F5)
- Marcadores del objetivo en el mundo (si Alan los quiere, usa `IObjetivos` y `UbicacionObjetivo`)
