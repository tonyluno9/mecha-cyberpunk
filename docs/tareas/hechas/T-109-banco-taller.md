# T-109 Banco del taller: reparar y pagar la renta
Dueño: Tony | Rama: tony/T-109-banco-taller | Modelo: Sonnet 5.5, medium
Depende de: T-102, T-103, T-107, T-108

## Objetivo
En el banco del taller (el `PuntoInteraccion` del `ITallerMecha`), con E, el jugador repara el mecha completo pagando según el
daño, o paga la renta vencida si el taller está bloqueado. El aviso dice siempre qué pasa: cuánto cuesta reparar, que el mecha
no está, que no tiene daño o que el taller está bloqueado y cuánto debe.

## Contexto
- Diseño: mecha-taller.md ("La reparación completa es en el taller, con costo según el daño"), economia-misiones.md (bloqueo por
  renta: no reparar ni mejorar, sí pelear dañado; quedar en negativo por reparar es intencional).
- Decisiones: D-039 (`CargarDeuda` puede dejar el dinero en negativo), D-060 y D-062 (arranque Estacionado, estado del llamado).
  Propuestas: D-tony-46 (reparación completa con deuda, solo con el mecha en el taller), D-tony-47, D-tony-49 (el banco es un
  `IInteractuable` que vive en el mecha; Mecha referencia Economia).
- Contratos: Core/Mecha/ITallerMecha.cs (`PuntoInteraccion`), Core/Jugador/IInteractuable.cs, Core/Economia/IRenta.cs e
  ICartera.cs (`MotivoDinero.Reparacion`).
- Código: Mecha/Estado/MechaPersistente.cs (`Danio`, `Reparar`, T-103), Mecha/Montaje/MontajeMecha.cs (`Estacionado`, `Ocupado`),
  Mecha/Llamado/LlamadoMecha.cs (`Estado`: `EnTaller` = descansa en su taller; subir en el taller lo pasa a `EnCalle`),
  Economia/Reparacion/ReglaReparacion.cs, Economia/FormatoDinero.cs, `DatosEconomia` (T-102), Personaje/Interaccion/ (T-108).
- En Pruebas_Calle el `TallerDePrueba` pone el punto de interacción 3 m a un lado del estacionamiento.

## Qué hacer
1. `Mecha/Taller/EstadoBanco.cs` (enum) y `Mecha/Taller/ReglaBancoTaller.cs` (pura): `Decidir(int adeudo, int dinero, bool
   mechaEnTaller, float danio)` en este orden:
   1) adeudo > 0 y dinero >= adeudo → `PagarRenta` (se puede usar);
   2) adeudo > 0 → `Bloqueado` (no);
   3) mecha fuera del taller → `MechaFuera` (no);
   4) daño < 0.5 → `SinDanio` (no);
   5) si no → `Reparar` (se puede usar, aunque el dinero no alcance: queda en negativo).
2. `Mecha/Datos/ConfigBancoTaller.cs` + asset `_Base` con los textos: "Pagar renta · {0}", "TALLER BLOQUEADO · debes {0} de renta",
   "Tu mecha no está en el taller", "Mecha sin daños", "Reparar mecha · {0}" ({0} con `FormatoDinero`).
3. `Mecha/Taller/BancoTaller.cs : MonoBehaviour, IInteractuable` en `MechaJugador.prefab` (`[RequireComponent]` de
   `MechaPersistente`): se agrega a `Registro<IInteractuable>` solo si el mecha arranca `Estacionado` y hay un `ITallerMecha`
   registrado (lo busca en `Start` y por `Agregado`; se quita si el taller se quita). `Posicion` = `PuntoInteraccion` del taller.
   "Mecha en el taller" = `LlamadoMecha.Estado == EnTaller` y nadie dentro (sin `LlamadoMecha`, basta con que nadie esté dentro y
   esté a menos de 3 m del estacionamiento). Costo con `ReglaReparacion` y los precios de `ServicioCartera.Datos` o de un
   `DatosEconomia` asignado. `Usar`: `PagarRenta` → `IRenta.PagarAdeudo()`; `Reparar` → `ICartera.CargarDeuda(costo,
   MotivoDinero.Reparacion)` y luego `MechaPersistente.Reparar()`. Sin `IRenta` registrada se salta el paso de la renta; sin
   `ICartera`, no se registra como interactuable y avisa un warning (una vez). `MechaCyberpunk.Mecha` referencia
   `MechaCyberpunk.Economia` (mismo dueño, sin ciclos).
4. Creador `Mecha/Editor/CreadorBancoTaller.cs` (idempotente): agrega `BancoTaller` con su config a `MechaJugador.prefab`.
5. Pruebas:
   - EditMode `Tests/EditMode/Mecha/ReglaBancoTallerPruebas.cs`: los 5 estados y el orden (con adeudo y mecha fuera gana la renta;
     reparar con dinero 0 sí se puede).
   - PlayMode `Tests/PlayMode/Mecha/BancoTallerPruebas.cs` (Pruebas_Calle, entrada simulada para caminar y pulsar E):
     a) mecha en 35 de vida, dinero 500: frente al banco el HUD dice "E · Reparar mecha · $650"; E → dinero -150 (motivo
        `Reparacion`), vida al máximo; E otra vez → "Mecha sin daños" y nada cambia;
     b) con el mecha llamado a la calle: "Tu mecha no está en el taller" y E no cobra;
     c) con adeudo 500 y dinero 0 (por `Cargar` de la renta o el reloj de prueba): "TALLER BLOQUEADO · debes $500 de renta", E no
        repara; tras abonar 600: "E · Pagar renta · $500", E → dinero 100 y taller desbloqueado; luego ya ofrece reparar;
     d) en Pruebas_Tony (`MechaJugador` en arranque Directo, sin taller) no hay ningún `IInteractuable` registrado.
6. Captura docs/capturas/T-109-1.png: frente al banco de Pruebas_Calle con "E · Reparar mecha · $…" y el mecha dañado al fondo.

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Taller/ nueva, Datos/, Editor/, Prefabs/ y el asmdef de Mecha), Assets/_Proyecto/Tests/EditMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/Mecha/, docs/capturas/
No tocar: Core/, Economia/ (salvo que falte exponer los precios en `ServicioCartera`: solo esa propiedad), Personaje/ y UI/ (ya
muestran interactuables desde T-108), Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] ReglaBancoTallerPruebas y BancoTallerPruebas (a-d) pasan
- [ ] Reparar descuenta exactamente el costo de `ReglaReparacion` y deja la vida al máximo (prueba a)
- [ ] Con el taller bloqueado no se puede reparar; pagando la renta sí (prueba c)
- [ ] Ningún asmdef de Tony referencia un área de Alan; Economia no referencia Mecha (sin ciclos)
- [ ] Captura docs/capturas/T-109-1.png revisada
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- En Pruebas_Calle, daña el mecha (o pelea la emboscada), regrésalo con M y ve al banco: ¿el aviso basta para entender cuánto
  cuesta y qué pasa si no te alcanza? ¿Quedar en negativo se entiende como decisión tuya y no como error?

## Pruebas automáticas
- EditMode: ReglaBancoTallerPruebas. PlayMode: BancoTallerPruebas.

## Fuera de alcance
- Reparar a medias, kits de reparación y mejoras de piezas (T-F7); animación o tiempo de reparación; el taller de Alan en la casa
  (integración cuando A-F3 esté en main)
