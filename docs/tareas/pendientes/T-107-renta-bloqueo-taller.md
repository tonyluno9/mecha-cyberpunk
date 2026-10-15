# T-107 Renta cada 7 días y bloqueo del taller
Dueño: Tony | Rama: tony/T-107-renta-bloqueo-taller | Modelo: Sonnet 5.5, medium
Depende de: T-100, T-102

## Objetivo
La casa cobra renta cada 7 días de juego. Si al vencer hay dinero, se cobra sola; si no, la renta queda como adeudo y el taller
se bloquea hasta pagarla. `ServicioRenta` registra `IRenta` (el banco del taller y el HUD la leen; Alan puede mostrar el bloqueo)
y se guarda como `IGuardable`. Como el reloj del mundo es de Alan y todavía no está en main, hay un reloj de prueba propio que
cumple `IRelojMundo` y también se guarda.

## Contexto
- Diseño: economia-misiones.md ("Pagas renta de tu casa. Si no pagas, el casero te bloquea el taller hasta que pagues";
  "Renta cada 7 días de juego"; "Taller bloqueado por renta: no puedes reparar ni mejorar, pero sí pelear con el mecha dañado",
  propuesta; criterio: "Al pasar 7 días sin pagar, el taller queda bloqueado").
- Decisiones: D-037 (el reloj y los horarios son de Alan), D-038, D-039, D-034. Propuestas: D-tony-44 (renta 500, primer cobro al
  empezar el día 8), D-tony-47 (cobro automático y adeudo), D-tony-51 (reloj de prueba).
- Contratos: Core/Economia/IRenta.cs y `MotivoDinero.Renta` (T-100), Core/Mundo/IRelojMundo.cs (`Dia`, `DiaCambio`,
  `AvanzarHasta` y sus reglas de avisos), Core/Guardado/IGuardable.cs, Core/Economia/ICartera.cs.
- Código: Economia/Cartera/ServicioCartera.cs y `DatosEconomia_Base` (T-102: renta 500, cada 7 días),
  Economia/Editor/CreadorServiciosEconomia.cs (T-102), Mecha/Editor/CreadorEscenaCalle.cs.
- Alan tiene su `RelojMundo` en alan/fase-1 (D-alan-4: el día dura 20 minutos, el juego empieza a las 21:00). El doble de Tony
  no lo copia: solo cumple el contrato.

## Qué hacer
1. `Economia/Renta/ModeloRenta.cs` (pura): constructor (monto, días entre cobros, primer día de cobro); `Monto`, `DiaProximoCobro`,
   `Adeudo`, `Bloqueado` (`Adeudo` > 0), `event Action Cambio`.
   - `AlEmpezarDia(int dia, ICartera cartera)`: mientras `dia` >= `DiaProximoCobro`: si no hay adeudo y la cartera puede pagar,
     `IntentarPagar(Monto, MotivoDinero.Renta)`; si no, `Adeudo += Monto`. Luego `DiaProximoCobro += días entre cobros`. Con una
     cartera null, todo va a adeudo. Avisa `Cambio` si algo cambió.
   - `PagarAdeudo(ICartera)`: si hay adeudo y la cartera puede pagarlo completo, lo cobra (`Renta`), deja el adeudo en 0 y avisa;
     si no, false y nada cambia. `DiaProximoCobro` no se mueve al pagar.
   - `Restablecer(int diaProximoCobro, int adeudo)` (para cargar).
2. `Economia/Renta/ServicioRenta.cs : MonoBehaviour, IRenta, IGuardable` (clave `renta`), en `ServiciosEconomia.prefab`: usa
   `DatosEconomia` (primer cobro = 1 + días entre rentas = día 8); busca el `IRelojMundo` en `Registro` en `Start` y se suscribe a
   su `DiaCambio` (también si el reloj aparece después, por `Agregado`, y se suelta si se quita); la cartera la busca en
   `Registro<ICartera>` al cobrar. Sin reloj no cobra nunca y no da error. Se agrega a `Registro<IRenta>` y `Registro<IGuardable>`.
   `Guardar`/`Cargar` → JSON con `DiaProximoCobro` y `Adeudo`; cargar no cobra nada.
3. `Progresion/Pruebas/RelojDePrueba.cs : MonoBehaviour, IRelojMundo, IGuardable` (clave `reloj`): hora y día propios (empieza el
   día 1 a las 21:00, amanecer 07:00, anochecer 19:00, un día = 20 minutos reales por defecto, todo en el inspector); avisos
   `HoraEnteraCambio`, `FranjaCambio`, `DiaCambio` y `AvanzarHasta` como dice el contrato; `Pausar`. Tecla J (prueba) =
   `AvanzarHasta(8)`: salta a las 08:00 del día siguiente. `Cargar` fija día y hora sin avisar `DiaCambio` ni `HoraEnteraCambio`
   (sí `FranjaCambio` si cambió la franja). Pon la lógica de horas en una clase pura (`ModeloRelojDePrueba`) para probarla en
   EditMode. Es solo para escenas y pruebas de Tony; no reemplaza al reloj de Alan.
4. Creadores: `CreadorServiciosEconomia` agrega `ServicioRenta` a `ServiciosEconomia.prefab` (idempotente);
   `CreadorEscenaCalle` pone un `RelojDePrueba` en Pruebas_Calle. Regenera con sus creadores.
5. Pruebas:
   - EditMode `Tests/EditMode/Economia/ModeloRentaPruebas.cs`: el día 7 no cobra; el día 8 con 500 cobra 500 (motivo `Renta`) y el
     próximo es el 15; el día 8 con 300 deja adeudo 500, bloqueado y el dinero en 300; saltar del día 1 al 16 sin dinero deja
     adeudo 1000 y próximo 22; con adeudo, la renta nueva se suma al adeudo aunque haya dinero; `PagarAdeudo` con poco dinero →
     false; con suficiente → desbloquea y el próximo no cambia; `Restablecer` no avisa cobro.
   - EditMode `Tests/EditMode/Progresion/RelojDePruebaPruebas.cs`: avisos de hora entera en orden, `DiaCambio` al pasar las 00:00,
     `AvanzarHasta` (misma hora no hace nada; una hora pasada salta al día siguiente; cruzar 2 medianoches avisa 2 `DiaCambio`; un
     solo `HoraEnteraCambio`), franja correcta en 06:59 y 07:00.
   - PlayMode `Tests/PlayMode/Economia/ServicioRentaPruebas.cs`: con `JugadorAPie.prefab` y un `RelojDePrueba` en una escena vacía hay
     una `IRenta` registrada con próximo cobro 8; con 500 de dinero, J 7 veces (entrada simulada) cobra 500 el día 8; con 0, queda
     bloqueado con adeudo 500; abonar 600 y `PagarAdeudo` desbloquea y deja 100; si el reloj aparece después del jugador, igual cobra;
     `Guardar`/`Cargar` de la renta y del reloj devuelven día, hora, próximo cobro y adeudo, y cargar no cobra.

## Carpetas permitidas
Assets/_Proyecto/Economia/ (Renta/ nueva, Editor/, Prefabs/), Assets/_Proyecto/Progresion/Pruebas/,
Assets/_Proyecto/Mecha/Editor/CreadorEscenaCalle.cs, Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity (con su creador),
Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab (solo si su creador lo regenera), Assets/_Proyecto/Tests/EditMode/Economia/,
Assets/_Proyecto/Tests/EditMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Economia/
No tocar: Core/, Mundo/ y el reloj de Alan, Pruebas_Tony.unity, Pruebas_APie.unity (sus puertas tienen horario propio), las carpetas y
escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] ModeloRentaPruebas, RelojDePruebaPruebas y ServicioRentaPruebas pasan
- [ ] Con `JugadorAPie.prefab` solo en una escena vacía hay exactamente una `IRenta` registrada (prueba automática)
- [ ] Sin dinero, al empezar el día 8 el taller queda bloqueado con adeudo 500; pagando se desbloquea (prueba automática)
- [ ] Cargar una partida no cobra renta (prueba automática)
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- ¿500 cada 7 días (unas 2 h 20 min de juego) contra 600 por pelea ganada aprieta lo justo, o hay que moverlo?

## Pruebas automáticas
- EditMode: ModeloRentaPruebas, RelojDePruebaPruebas. PlayMode: ServicioRentaPruebas.

## Fuera de alcance
- Mostrar la renta en el HUD (T-110) y pagarla en el banco (T-109); el casero como personaje; dormir (T-F6)
