# T-100 Contratos de guardado, interacción y renta
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-100-contratos-partida | Modelo: Sonnet 5.5, medium
Depende de: nada (T-033, T-050 y T-062 ya están en main)

## Objetivo
En Core/ están los tres contratos nuevos que T-F5 necesita entre Tony y Alan: qué se conserva en la partida guardada
(`IGuardable`; Alan lo implementa en su reloj), qué puede usar el jugador a pie con E además de puertas y montables
(`IInteractuable`; Tony lo usa en el banco del taller y Alan lo usará en A-F3 para la cama y el espejo) y la renta de la casa
(`IRenta`; Alan puede leerla para mostrar el taller bloqueado). Los comentarios de los contratos viejos que cambian de
significado en T-F5 quedan al día.

## Contexto
- Decisiones: D-026, D-027 (quien posee el dato lo implementa), D-032, D-034 (Registro<T>), D-039 (dinero en enteros y deuda),
  D-054 (el jugador elige por registro, 2.5 m y 60°). Propuestas nuevas: D-tony-41, D-tony-42, D-tony-47 y D-tony-50 en
  docs/decisiones-pendientes-tony.md.
- Diseño: economia-misiones.md (renta cada 7 días; si no pagas, el casero bloquea el taller), combate.md (el daño es persistente
  hasta que reparas), mecha-taller.md (la reparación completa es en el taller).
- Lee primero, como ejemplo de estilo: Core/Mecha/ITallerMecha.cs, Core/Jugador/IMontable.cs, Core/Economia/ICartera.cs,
  Core/Servicios/Registro.cs. Los comentarios `///` SON el contrato; el revisor los lee con la mirada de Alan.
- Es una tarea de Core: el orquestador la integra a main en cuanto el verificador la apruebe (Alan necesita `IGuardable` para su
  reloj e `IInteractuable` para la casa de A-F3).

## Qué hacer
1. `Core/Guardado/IGuardable.cs` (carpeta nueva):
   - `string ClaveGuardado { get; }`: única y estable entre versiones del juego; solo minúsculas, números y `_`. Claves en uso:
     `cartera`, `renta`, `mecha`, `perfil` (Tony) y `reloj` (Alan). Si dos registrados comparten clave, el guardado usa el
     primero que se registró y avisa con un warning.
   - `string Guardar()`: el estado actual como texto (recomendado: `JsonUtility.ToJson` de una clase `[Serializable]` propia).
     Nunca null. No cambia nada.
   - `void Cargar(string datos)`: restablece el estado desde un texto que dio `Guardar` (de esta versión). No simula el tiempo:
     no dispara eventos de avance ni de cobro (el reloj NO avisa `DiaCambio` ni `HoraEnteraCambio` al cargar), pero sí los de
     "cambió" para que pantallas y luces se pongan al día (`Cambio`, `FranjaCambio`). Si el texto no se entiende, no cambia nada,
     avisa con `Debug.LogWarning` y nunca lanza excepciones.
   - Comentario de la interfaz: la implementa quien posee el dato (D-027) y se agrega a `Registro<IGuardable>` en `OnEnable` (se
     quita en `OnDisable`). El guardado de Tony (`ServicioGuardado`, Progresion/, T-111) junta el texto de todos al guardar y, al
     cargar, le devuelve a cada uno el suyo por clave, también a los que se registren después de cargar. Una clave que no está en
     el archivo no se toca.
2. `Core/Jugador/IInteractuable.cs`:
   - `Vector3 Posicion { get; }`: punto en el piso (m, en el mundo) frente a lo que se usa. El jugador a pie (Tony) elige, entre
     puertas, montables e interactuables, el más cercano a 2.5 m o menos y a 60° o menos de hacia donde mira (D-054); no hace falta
     collider ni apuntar con precisión.
   - `string Texto { get; }`: lo que hace, corto y en español ("Reparar mecha · $650", "Dormir"). Puede cambiar en cada frame.
   - `bool PuedeUsar { get; }`: true = el HUD muestra "E · " + `Texto` y la E llama a `Usar`; false = el HUD muestra solo
     `Texto` como aviso ("TALLER BLOQUEADO · debes $500 de renta") y la E no hace nada.
   - `void Usar()`: lo llama el jugador (Tony) al pulsar E con `PuedeUsar` en true; hace la acción en el acto o la empieza. No lo
     llames desde otro lado.
   - Comentario: se agrega a `Registro<IInteractuable>` en `OnEnable` (se quita en `OnDisable`). Ejemplos: el banco del taller
     (Tony, T-109) y en A-F3 la cama y el espejo (Alan). Solo se usa a pie: en carro o mecha no se ofrece.
3. `Core/Economia/IRenta.cs`:
   - `int Monto` ($ por periodo), `int DiaProximoCobro` (el `IRelojMundo.Dia` en cuyo inicio, al pasar las 00:00, se cobra la
     siguiente renta), `int Adeudo` ($ de renta vencida sin pagar, 0 si nada), `bool TallerBloqueado` (true si `Adeudo` > 0),
     `event Action Cambio` (tras cualquier cambio de los anteriores) y `bool PagarAdeudo()` (cobra todo el adeudo de la cartera
     con `MotivoDinero.Renta` solo si alcanza y desbloquea; false si no alcanza o no hay adeudo).
   - Comentario: la implementa Tony (`ServicioRenta`, Economia/, dentro de `ServiciosEconomia.prefab` anidado en
     `JugadorAPie.prefab`, T-107) y se busca en `Registro<IRenta>`. Con el taller bloqueado no se repara ni se mejora el mecha,
     pero sí se puede sacar y pelear dañado. Alan puede leerla para mostrar el bloqueo en el mundo (opcional); no la implementes.
4. `Core/Economia/MotivoDinero.cs`: agrega `Carga` AL FINAL del enum (no cambies el orden de los demás): "el dinero cambió porque
   se cargó una partida; no es un movimiento real y el HUD no lo anuncia".
5. Solo comentarios (sin cambiar código):
   - `ICartera`: cambia "Por ahora nadie la registra ... no la leas de Registro<ICartera> todavía" por: la registra
     `ServicioCartera` (Economia/, T-102) dentro de `ServiciosEconomia.prefab` anidado en `JugadorAPie.prefab`; mientras no esté en
     main usa un doble. Es también `IGuardable` con clave `cartera`.
   - `IPeleador.PrepararPelea`: "Restaura la vida" pasa a "Restaura la vida, salvo en el mecha del jugador con daño persistente
     (T-F5), que conserva su daño y solo sube a un piso de emergencia si quedó muy bajo o en KO".
   - `ITallerMecha.PuntoInteraccion`: quita "Lo usa T-F5; en T-F4 solo se lee Estacionamiento" y pon: Tony registra ahí el banco
     del taller como `IInteractuable` (reparar y pagar la renta); no pongas otro interactuable a menos de 2.5 m de ese punto.
   - `IRelojMundo`: en el comentario de la interfaz agrega que el reloj de Alan implementa también `IGuardable` con clave `reloj`
     (guarda `Dia` y `Hora`; al cargar no avisa `DiaCambio` ni `HoraEnteraCambio`, sí `FranjaCambio` si cambió la franja). En
     `AvanzarHasta` cambia "Sirve para dormir (T-F5)" por "(dormir llega en T-F6)".
6. Pruebas en `Tests/EditMode/Core/ContratosPartidaPruebas.cs`, con dobles definidos en la prueba (`GuardableFalso`,
   `InteractuableFalso`, `RentaFalsa`): entran y salen de su `Registro<T>`, se pueden implementar sin Unity vivo y
   `Registros.LimpiarTodos()` deja los tres registros vacíos; `MotivoDinero.Carga` es el último valor y `Premio` sigue valiendo 0.
   `Registros.LimpiarTodos()` en SetUp y TearDown.

Nota para el orquestador (no es trabajo del implementador): al integrar a main, anota en docs/revision-tony.md el pedido a Alan
de D-tony-52: su `RelojMundo` implementa `IGuardable` con clave `reloj`; nada de otro interactuable en el `PuntoInteraccion` del
taller; `IInteractuable` está listo para la cama y el espejo de A-F3.

## Carpetas permitidas
Assets/_Proyecto/Core/Guardado/ (nueva), Assets/_Proyecto/Core/Jugador/IInteractuable.cs, Assets/_Proyecto/Core/Economia/IRenta.cs,
Assets/_Proyecto/Core/Economia/MotivoDinero.cs, y solo comentarios en Core/Economia/ICartera.cs, Core/Peleadores/IPeleador.cs,
Core/Mecha/ITallerMecha.cs y Core/Mundo/IRelojMundo.cs; Assets/_Proyecto/Tests/EditMode/Core/
No tocar: el resto de Core/, las áreas de Tony y de Alan, escenas, prefabs, ProjectSettings/, Packages/ ni herramientas/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa: compilar OK, EditMode y PlayMode sin fallas, con ContratosPartidaPruebas contada
- [ ] ContratosPartidaPruebas pasa
- [ ] `git diff main --stat` solo muestra archivos de las carpetas permitidas; en ICartera.cs, IPeleador.cs, ITallerMecha.cs e
      IRelojMundo.cs solo cambian líneas de comentario (`git diff -U0 main -- <archivo>` sin líneas de código cambiadas)
- [ ] En MotivoDinero.cs los valores viejos conservan su orden y `Carga` es el último
- [ ] Cada miembro público de las tres interfaces nuevas tiene `///` con unidades (m, $, días) donde aplique
- [ ] Ningún archivo de Core/ referencia un ensamblado de área (ReglasEnsamblados sigue pasando)

## Criterios de sensación (para un humano)
- Alan (o Tony con su mirada) lee solo los `///` de IGuardable, IInteractuable e IRenta y sabe, sin preguntar, cómo hacer que
  su reloj se guarde, cómo hacer una cama que se use con E y cómo saber si el taller está bloqueado.

## Pruebas automáticas
- EditMode (Tests/EditMode/Core/): ContratosPartidaPruebas.

## Fuera de alcance
- Implementar los contratos (cartera T-102, mecha T-103, renta T-107, interactuables T-108, banco T-109, guardado T-111; los de
  Alan son suyos)
- Dormir, el menú de cargar partida y el guardado automático (T-F6); morir en la calle y la clínica (T-F6/T-F7)
