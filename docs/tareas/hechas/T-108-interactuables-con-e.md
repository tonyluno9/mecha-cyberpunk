# T-108 Usar interactuables con E
Dueño: Tony | Rama: tony/T-108-interactuables-con-e | Modelo: Sonnet 5.5, medium
Depende de: T-100

## Objetivo
El jugador a pie puede usar con E cualquier `IInteractuable` (el banco del taller en T-109; la cama, el espejo y la cajera de
Alan después), igual que hoy abre puertas y sube a montables: el más cercano y de frente gana, el HUD dice qué hace la E y, si no
se puede, dice por qué.

## Contexto
- Decisiones: D-054 (el jugador interactúa por registro: 2.5 m y 60°, sin colliders), D-052 (HUD a pie; en carro y mecha no hay
  avisos de interacción), D-055 (dobles en Personaje/Pruebas/ y Pruebas_APie generada por su creador). Propuesta: D-tony-49.
- Contrato: Core/Jugador/IInteractuable.cs (T-100): `Posicion`, `Texto`, `PuedeUsar`, `Usar()`.
- Código: Personaje/Interaccion/InteraccionAPie.cs (puertas y montables compiten en una lista de puntos con
  `SelectorInteraccion.Elegir`), Personaje/Interaccion/TipoCandidatoAPie.cs, Personaje/Jugador/ReglaMontar.cs y AccionMontar.cs,
  UI/HudAPie/HudAPie.cs (`ActualizarInteraccion`), UI/HudAPie/TextoInteraccion.cs y TipoCandidatoInteraccion.cs,
  UI/Datos/ConfigHudAPie.cs, Personaje/Editor/CreadorJugadorAPie.cs (genera Pruebas_APie).

## Qué hacer
1. `TipoCandidatoAPie` y `TipoCandidatoInteraccion`: agrega `Interactuable` al final.
2. `InteraccionAPie`: los `IInteractuable` de `Registro<IInteractuable>` entran a la misma lista que puertas y montables (todos,
   también los que no se pueden usar, para que el HUD diga por qué). Propiedad `InteractuableCandidato`. Con E a pie y sin KO:
   si `PuedeUsar`, llama `Usar()` una vez y avisa `event Action<IInteractuable> InteractuableUsado`; si no, avisa
   `InteractuableNegado` y no llama nada. En carro o mecha no se ofrece ni se usa. Extiende `ReglaMontar`/`AccionMontar` si así queda
   más claro (acción `Usar`).
3. HUD a pie: con un interactuable de candidato muestra "E · " + `Texto` (prefijo en `ConfigHudAPie`) si `PuedeUsar`, o solo
   `Texto` con el color de aviso si no. El texto se relee cada frame (puede cambiar mientras lo miras). En carro y mecha, nada.
   Agrega a `TextoInteraccion` una función pura para este texto.
4. Doble `Personaje/Pruebas/InteractuableDePrueba.cs : MonoBehaviour, IInteractuable`: `Texto`, `PuedeUsar` y la posición de su
   transform; cuenta `Usos`. `CreadorJugadorAPie` pone uno en Pruebas_APie lejos de puertas y del carro (por ejemplo en (4, 0, 12)
   con texto "Probar interactuable") y regenera la escena con su creador.
5. Pruebas:
   - EditMode `Tests/EditMode/UI/TextoInteraccionPruebas.cs` (o la existente): "E · Dormir" con `PuedeUsar`; "TALLER BLOQUEADO"
     sin; vacío si el texto es vacío.
   - EditMode de `ReglaMontar` si la cambias: a pie con interactuable → Usar; en KO → nada; en carro → Bajar como hoy.
   - PlayMode `Tests/PlayMode/Personaje/InteractuablesAPiePruebas.cs` (Pruebas_APie, entrada simulada): caminar con W hasta 2 m
     del interactuable mirándolo → candidato correcto; E → `Usos` = 1 y un solo `InteractuableUsado`; con `PuedeUsar` false, E no lo
     usa y avisa `InteractuableNegado`; de espaldas (más de 60°) o a 3 m no es candidato; con una puerta cerrada más cerca, gana la
     puerta; subido al carro no se ofrece.
   - PlayMode `Tests/PlayMode/UI/HudInteractuablePruebas.cs`: el HUD muestra "E · Probar interactuable", cambia a solo el texto al
     poner `PuedeUsar` en false, y se oculta al alejarse.
6. Captura docs/capturas/T-108-1.png: HUD a pie frente al interactuable de prueba.

## Carpetas permitidas
Assets/_Proyecto/Personaje/ (Interaccion/, Jugador/, Pruebas/, Editor/ y lo que su creador regenere), Assets/_Proyecto/UI/ (HudAPie/,
Datos/, Editor/, Prefabs/HudAPie.prefab con su creador), Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity (con su creador),
Assets/_Proyecto/Tests/EditMode/UI/, Assets/_Proyecto/Tests/EditMode/Personaje/, Assets/_Proyecto/Tests/PlayMode/Personaje/,
Assets/_Proyecto/Tests/PlayMode/UI/, docs/capturas/
No tocar: Core/, Mecha/ (el banco del taller es T-109), Pruebas_Tony.unity, Pruebas_Calle.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] InteractuablesAPiePruebas y HudInteractuablePruebas pasan; PuertasAPiePruebas, MontarCarroPruebas y HudInteraccionPruebas
      siguen pasando
- [ ] E usa el interactuable una sola vez por pulsación y nunca si `PuedeUsar` es false (prueba automática)
- [ ] Captura docs/capturas/T-108-1.png revisada: "E · Probar interactuable" legible
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- En Pruebas_APie, acércate al interactuable de prueba y pulsa E: ¿responde al instante y el aviso no estorba cuando pasas de largo?

## Pruebas automáticas
- EditMode: TextoInteraccionPruebas (y ReglaMontar si cambia). PlayMode: InteractuablesAPiePruebas, HudInteractuablePruebas.

## Fuera de alcance
- El banco del taller (T-109); la cama, el espejo y la cajera (Alan, A-F3 y A-F5); mantener E pulsada o menús de varias opciones
