# T-110 Dinero y renta en el HUD a pie
Dueño: Tony | Rama: tony/T-110-hud-dinero-renta | Modelo: Sonnet 5.5, medium
Depende de: T-102, T-107

## Objetivo
A pie, el jugador ve cuánto dinero tiene, un aviso breve cada vez que cambia ("+$600 · PREMIO", "-$650 · REPARACIÓN") y, si la
renta está vencida, un recordatorio de que el taller está bloqueado. Así entiende la economía sin abrir menús.

## Contexto
- Diseño: estetica-hud.md (a pie, HUD mínimo flotante; en el mecha nada flota), economia-misiones.md.
- Decisiones: D-052 (HUD a pie por modo: a pie todo; en carro solo objetivo y canción; en mecha nada), D-049 (UI referencia áreas
  de Tony sin ciclos). Propuesta: D-tony-55.
- Contratos: Core/Economia/ICartera.cs (`Cambio(diferencia, motivo)`; `MotivoDinero.Carga` no se anuncia, T-100),
  Core/Economia/IRenta.cs (`TallerBloqueado`, `Adeudo`, `Cambio`).
- Código: UI/HudAPie/HudAPie.cs (grupos por modo; repite la lectura un frame después de registrarse el jugador),
  UI/HudAPie/FormatoHud.cs, UI/Datos/ConfigHudAPie.cs, UI/Editor/CreadorHudAPie.cs, Economia/FormatoDinero.cs (T-102),
  Progresion/Pruebas/RelojDePrueba.cs (T-107).

## Qué hacer
1. `HudAPie`: grupo de dinero (esquina, junto a la vida) con `FormatoDinero.Texto`; en negativo, con el color de deuda de la
   config. Busca la `ICartera` en `Registro` (y si aparece después, por `Agregado`). Sin cartera, el grupo se oculta.
2. Aviso de movimiento: con cada `Cambio` cuyo motivo no sea `Carga`, muestra "+$600 · PREMIO" o "-$650 · REPARACIÓN" durante
   `segundosAvisoDinero` (2.5 s) en `ConfigHudAPie`; los nombres de cada `MotivoDinero` van en la config. Un movimiento nuevo
   reemplaza al anterior y reinicia el tiempo. La parte pura (texto del aviso) va en `FormatoHud` o en una clase pura nueva.
3. Renta: con `IRenta.TallerBloqueado`, una línea fija "RENTA VENCIDA · $500" (color de aviso); se quita al pagar.
4. Por modo (D-052): a pie se ve todo esto; en carro y en mecha no se ve nada de esto.
5. `CreadorHudAPie` agrega los textos al prefab (idempotente) y `UI.asmdef` referencia `MechaCyberpunk.Economia` (por
   `FormatoDinero`; mismo dueño, sin ciclos).
6. Pruebas:
   - EditMode `Tests/EditMode/UI/FormatoHudPruebas.cs` (o nueva): texto del aviso para +600 `Premio`, -650 `Reparacion`, -500
     `Renta` y +500 `Otro`; `Carga` → vacío.
   - PlayMode `Tests/PlayMode/UI/HudDineroPruebas.cs` (escena vacía con `JugadorAPie.prefab`): muestra "$500"; abonar 600 con
     `Premio` → "$1,100" y el aviso "+$600 · PREMIO" visible, que se va a los 2.5 s (±0.2); cargar deuda hasta -150 → "-$150" con
     el color de deuda; `Cargar` de la cartera → cambia la cifra sin aviso; con un `RelojDePrueba` y 0 de dinero, saltar al día 8 →
     aparece "RENTA VENCIDA · $500" y al pagar se quita; subido a un `CarroDePrueba` (Personaje/Pruebas/) el dinero no se ve.
7. Captura docs/capturas/T-110-1.png: HUD a pie con dinero y el aviso de premio, a 1280×720.

## Carpetas permitidas
Assets/_Proyecto/UI/ (HudAPie/, Datos/, Editor/, Prefabs/HudAPie.prefab con su creador y el asmdef de UI),
Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab (solo si su creador lo regenera), Assets/_Proyecto/Tests/EditMode/UI/,
Assets/_Proyecto/Tests/PlayMode/UI/, docs/capturas/
No tocar: Core/, Economia/ (solo se lee), Mecha/, Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] FormatoHudPruebas y HudDineroPruebas pasan; HudAPiePruebas, HudCarroPruebas y HudCalorPruebas siguen pasando
- [ ] El aviso dura 2.5 s (±0.2) y cargar una partida no lo dispara (prueba automática)
- [ ] En carro y en mecha no se ve nada de dinero ni renta (prueba automática)
- [ ] Captura docs/capturas/T-110-1.png revisada: dinero y aviso legibles sin tapar la mira ni el objetivo
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- En Pruebas_Calle, pulsa 7 y 8 (dinero de prueba): ¿el aviso se nota y se va sin estorbar? ¿El dinero negativo se lee como deuda?

## Pruebas automáticas
- EditMode: FormatoHudPruebas. PlayMode: HudDineroPruebas.

## Fuera de alcance
- Dinero en la cabina del mecha (el premio ya se ve en la pantalla de la arena, T-106); días para la próxima renta; sonido de caja
