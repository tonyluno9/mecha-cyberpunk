# T-103 Daño persistente del mecha del jugador
Dueño: Tony | Rama: tony/T-103-danio-persistente | Modelo: Sonnet 5.5, medium
Depende de: T-100

## Objetivo
El daño del mecha del jugador se queda: la siguiente pelea (también la revancha) empieza con la vida con la que terminó la
anterior, y el mecha que vuelve al taller llega con su daño. Si quedó en KO o casi, la siguiente pelea lo levanta solo a un piso
de emergencia (15 %), así perder no deja el juego atorado pero sí obliga a reparar. El rival sigue empezando completo. El estado
del mecha se guarda como `IGuardable` (clave `mecha`) y se puede reparar por código (lo usará el banco del taller en T-109).

## Contexto
- Diseño: combate.md ("El daño es persistente hasta que reparas"; "Si salgo de una pelea dañado, la siguiente pelea empieza con
  ese mismo daño"), personaje-progresion.md ("Perder una pelea no es game over: ... reparas el mecha y puedes volver a retar"),
  demo.md ("Despiertas con el mecha fregado").
- Decisiones: D-013 (la revancha restauraba la vida en la fase 1; queda reemplazada por D-tony-42), D-041 (el rival reusa
  Combate), D-060 (MechaJugador Directo y la variante MechaEstacionado). Propuesta: D-tony-42.
- Contrato: el comentario nuevo de `IPeleador.PrepararPelea` (T-100) y `Core/Guardado/IGuardable.cs`.
- Código: Combate/Peleador/PeleadorMecha.cs (`PrepararPelea` llama `vida.Restaurar()` y avisa `PeleaPreparada`),
  Combate/Vida/VidaMecha.cs (envuelve `ModeloVida`; `ModeloVida` es de Core y NO se cambia), Pelea/Flujo/DirectorPelea.cs
  (prepara a los dos en cada pelea y revancha), Mecha/Editor/ (creadores de `MechaJugador.prefab`).
- Hay un mecha persistente por escena: `MechaJugador` en Pruebas_Tony y `MechaEstacionado` en Pruebas_Calle. Si alguna vez hay
  dos, el segundo avisa y no se registra para el guardado.

## Qué hacer
1. `PeleadorMecha`: propiedad `bool ConservarVidaAlPreparar` (false por defecto). En `PrepararPelea`, si es true no llama
   `vida.Restaurar()`; todo lo demás igual (pose, control apagado y `PeleaPreparada` al final).
2. `VidaMecha.FijarVida(float valor)`: deja la vida en `valor` limitado a (0, Maxima]; quita el KO si lo había; avisa `Cambio`;
   NO publica `EventosPelea.ImpactoRecibido` ni `KO`. Con `valor` <= 0 no hace nada y avisa un warning. Se puede hacer con
   `Restaurar()` + `Aplicar(Maxima - valor)` del modelo sin tocar Core.
3. `Mecha/Datos/ConfigMechaDaniado.cs` (ScriptableObject) y `ConfigMechaDaniado_Base.asset` con `pisoVidaPersistente` = 0.15
   (fracción de la vida máxima). T-104 y T-105 le agregarán campos.
4. `Mecha/Estado/ReglaVidaPersistente.cs` (estática, pura):
   - `float AlPreparar(float actual, float maxima, float fraccionPiso)` = el mayor entre `actual` y `maxima × fraccionPiso`.
   - `float AlCargar(float vidaGuardada, float maximaGuardada, float maximaActual, float fraccionPiso)`: escala por proporción a la
     vida máxima actual, la limita a [`maximaActual × fraccionPiso`, `maximaActual`]; con datos inválidos (máxima guardada <= 0)
     devuelve `maximaActual`.
5. `Mecha/Estado/MechaPersistente.cs : MonoBehaviour, IGuardable` (clave `mecha`), con `[RequireComponent]` de `VidaMecha` y
   `PeleadorMecha` y referencia a `ConfigMechaDaniado`:
   - En `Awake` pone `ConservarVidaAlPreparar = true` en su `PeleadorMecha`.
   - Al `PeleaPreparada`, si la vida quedó por debajo del piso (o en KO), `FijarVida(ReglaVidaPersistente.AlPreparar(...))`.
   - `float Danio` (máxima − actual), `void Reparar()` (vida completa con `Restaurar`) y `event Action Reparado`.
   - `Guardar` → JSON con vida y vida máxima; `Cargar` → `FijarVida(ReglaVidaPersistente.AlCargar(...))`; texto que no se entiende
     → warning y nada cambia.
   - En `OnEnable` se agrega a `Registro<IGuardable>` solo si no hay otro registrado con clave `mecha`; si lo hay, warning y no se
     registra (sigue conservando su daño entre peleas). Se quita en `OnDisable`.
6. Creador `Mecha/Editor/CreadorMechaPersistente.cs` (`MenuItem`, idempotente): crea `ConfigMechaDaniado_Base` y agrega
   `MechaPersistente` a `MechaJugador.prefab` (la variante `MechaEstacionado.prefab` lo hereda). Los rivales NO lo llevan.
7. `Tests/PlayMode/Pelea/FlujoPeleaPruebas.cs`: la prueba `Revancha_REnElResultado_RestauraTodoYVuelveElConteo` pasa a comprobar la
   regla nueva (el rival vuelve completo; el jugador conserva su vida, o el piso si quedó en KO). Es el único cambio permitido a
   pruebas viejas; si otra falla por la regla nueva, se ajusta igual y se explica en el resumen.
8. Pruebas:
   - EditMode `Tests/EditMode/Mecha/ReglaVidaPersistentePruebas.cs`: `AlPreparar` con 70/100 → 70, con 5/100 → 15, con 0/100 → 15;
     `AlCargar` con 40/100 a máxima 100 → 40, a máxima 200 → 80, con 0 → 15, con máxima guardada 0 → máxima actual.
   - PlayMode `Tests/PlayMode/Mecha/DanioPersistentePruebas.cs`:
     a) Pruebas_Tony, P para empezar: el jugador recibe 30 de daño y el rival cae; con R, al empezar el conteo el jugador tiene 70
        (±0.01) y el rival su vida máxima.
     b) El jugador cae en KO; con R empieza con 15 (±0.01), y al decir PELEA tiene control y la pelea no termina sola.
     c) Un `RivalBasico.prefab` suelto: `PrepararPelea` lo deja con su vida máxima (sin `MechaPersistente`).
     d) En Pruebas_Tony hay exactamente un `IGuardable` con clave `mecha`; `Guardar` con vida 40, `Reparar`, `Cargar` → 40; `Cargar`
        con vida 0 → 15; `Cargar("basura")` no cambia nada y avisa un warning; `Reparar` deja la vida máxima y avisa `Reparado`.
     e) Pruebas_Calle, entrada simulada: con el mecha estacionado en 50, subir (E), encender (F, Espacio, Q), bajar (E), M lo
        manda al taller; al terminar el regreso sigue en 50.
     f) Dos mechas con `MechaPersistente` en una escena vacía: solo uno queda registrado y sale un warning.

## Carpetas permitidas
Assets/_Proyecto/Combate/Peleador/, Assets/_Proyecto/Combate/Vida/, Assets/_Proyecto/Mecha/ (Estado/ nueva, Datos/, Editor/,
Prefabs/), Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/Pelea/FlujoPeleaPruebas.cs (solo la prueba de revancha)
No tocar: Core/ (ni `ModeloVida`), IA/ (los rivales no cambian), Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] ReglaVidaPersistentePruebas y DanioPersistentePruebas (a-f) pasan
- [ ] La revancha conserva el daño del jugador y restaura al rival (medido en la prueba a)
- [ ] Tras perder, la revancha empieza al 15 % y la pelea se juega (prueba b)
- [ ] `MechaJugador.prefab` y `MechaEstacionado.prefab` llevan `MechaPersistente`; `RivalBasico.prefab`, `RivalTutorial.prefab` y
      `RivalPandilla.prefab` no
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- En Pruebas_Tony, pelea, gana con daño y pulsa R: ¿se entiende que empiezas dañado (la pantalla de vida lo dice) y se siente
  justo, no como un error?
- Pierde y pulsa R: ¿empezar al 15 % se siente como "deberías reparar" y no como castigo absurdo?

## Pruebas automáticas
- EditMode: ReglaVidaPersistentePruebas. PlayMode: DanioPersistentePruebas y FlujoPeleaPruebas (revancha ajustada).

## Fuera de alcance
- Efectos de vida baja (T-104, T-105), reparar en el taller (T-109), el archivo de guardado (T-111)
- Daño por piezas (brazo destruido) y mecha que vuelve "muy dañado" tras morir en la calle (T-F6/T-F7)
