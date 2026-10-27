# T-149 Reparar a medias en el banco del taller
Dueño: Tony | Rama: tony/T-149-reparar-a-medias | Modelo: Sonnet 5.5, medium
Depende de: T-143

## Objetivo
En el banco del taller, E ya no repara todo de golpe: abre una pantalla con tres opciones (reparar todo, reparar la mitad del daño, reparar
con lo que tienes) y su costo, para que el jugador decida cuánto se endeuda. Sigue siendo posible quedar en negativo reparando todo
(intencional en el diseño), pero ya no es la única salida.

## Contexto
- Diseño: mecha-taller.md ("La reparación completa es en el taller, con costo según el daño"), economia-misiones.md ("Ganas una pelea pero el
  costo de reparar el mecha supera el premio: quedas en negativo. Es intencional"; taller bloqueado por renta: no reparas).
- Decisiones: D-039, D-tony-46 (reparación completa fiada con `CargarDeuda`; "reparar a medias: T-F7"), D-tony-47 (bloqueo por renta),
  D-tony-49 (`BancoTaller` vive en el mecha; orden de estados). Propuestas nuevas: D-tony-84 y D-tony-85 (deudas).
- `MechaCyberpunk.UI` referencia `MechaCyberpunk.Mecha`, no al revés: el banco (Mecha) no puede abrir una pantalla de UI directamente.
- Ejemplos: `Mecha/Taller/BancoTaller.cs`, `ReglaBancoTaller.cs`, `Economia/Reparacion/ReglaReparacion.cs`, `Mecha/Estado/MechaPersistente.cs`,
  la pantalla de apuesta de T-145 o la de tienda de T-146 (navegación y congelar al jugador). Usa el helper de mapas de acciones de T-140.

## Qué hacer
1. `ReglaReparacion` (Economia): `PuntosPorDinero(dinero, costoPorPunto, costoMinimo)` (cuántos puntos se reparan con ese dinero; 0 si no
   llega al mínimo) y el costo de reparar N puntos (el de hoy con N). Pruebas de EditMode.
2. `MechaPersistente.RepararPuntos(float puntos)`: sube la vida esos puntos sin pasar de la máxima, quita el KO si sube de 0 y avisa `Reparado`
   solo si quedó completa.
3. `BancoTaller`: con el estado `Reparar`, `Usar` calcula las opciones (`OpcionReparacion`: texto, puntos, costo, si se puede elegir) y avisa
   `OpcionesPedidas`; si nadie escucha (no hay pantalla), repara todo como hoy. `Elegir(opcion)` cobra (todo y mitad con `CargarDeuda`; "con lo
   que tienes" con `IntentarPagar`) y repara. Opciones (D-tony-84): "REPARAR TODO · $X"; "REPARAR LA MITAD · $Y" (la mitad del daño, redondeada
   hacia arriba; solo si el daño es al menos el doble del mínimo cobrable); "CON LO QUE TIENES · $Z (+N)" (solo si 0 < dinero < costo total y
   alcanza el mínimo). Los demás estados (pagar renta, bloqueado, mecha fuera, sin daño) no cambian.
4. `UI/Taller/PantallaBancoTaller.cs` + prefab (creador en UI/Editor/): escucha al banco registrado, muestra vida actual y opciones con ↑ ↓,
   Enter elige, Esc sale sin cobrar; congela al jugador; dinero arriba (rojo si es negativo). Va donde ya está el HUD a pie del jugador o como
   prefab propio colocado por el creador de las escenas de Tony que tienen taller (Pruebas_Calle con su método idempotente, Pruebas_Economia,
   Pruebas_Prologo): decide y anota por qué en la bitácora; Alan no tiene que colocar nada nuevo.
5. PlayMode `Tests/PlayMode/Mecha/ReparacionParcialPruebas.cs` en Pruebas_Economia con entrada simulada; actualizar `BancoTallerPruebas`,
   `RecorridoTallerPruebas` y `RecorridoPrologoPruebas` si ahora necesitan elegir una opción (Enter sobre "REPARAR TODO" deja el resultado de antes).

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Taller/, Estado/, Editor/, Prefabs/ solo con creador), Assets/_Proyecto/Economia/Reparacion/, Assets/_Proyecto/UI/
(Taller/, Datos/, Editor/, Prefabs/), creadores y escenas de Tony con taller (Pruebas_Calle, Pruebas_Prologo, Pruebas_Economia) solo con sus
creadores, Assets/_Proyecto/Tests/EditMode/Economia/, Tests/EditMode/Mecha/, Tests/PlayMode/Mecha/, Tests/PlayMode/Progresion/, docs/capturas/
No tocar: Core/, `JugadorAPie.prefab` salvo con su creador y si la bitácora lo justifica, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] EditMode: `PuntosPorDinero(120, 10, 50)` = 12, `PuntosPorDinero(30, 10, 50)` = 0; `RepararPuntos` no pasa de la máxima y quita el KO
- [ ] ReparacionParcialPruebas a): mecha en su taller con 40/100 y $1,000: E muestra "REPARAR TODO · $600", "REPARAR LA MITAD · $300" y no
      muestra "CON LO QUE TIENES"; ↓ y Enter: vida 70 y dinero $700
- [ ] b) con 70/100 y $120: aparece "CON LO QUE TIENES · $120 (+12)"; elegirla deja vida 82 y dinero $0
- [ ] c) con 70/100 y −$50: "CON LO QUE TIENES" no aparece (todo y mitad siguen, fiados); elegir "REPARAR TODO" deja vida 100 y dinero −$350 (sigue siendo fiado)
- [ ] d) Esc no cobra ni repara y el jugador vuelve a moverse; con el taller bloqueado por renta, E paga la renta como antes y no abre la pantalla
- [ ] BancoTallerPruebas, RecorridoTallerPruebas y RecorridoPrologoPruebas en verde (reparación de $550 del prólogo intacta)
- [ ] Captura docs/capturas/T-149-1.png: la pantalla del banco con las tres opciones

## Criterios de sensación (para un humano)
- Pruebas_Economia: deja el mecha al 30 % con poco dinero y ve al banco. ¿Elegir cuánto reparar se siente como administrar la pobreza y no
  como un menú más? ¿Quedar en negativo reparando todo sigue sintiéndose como decisión tuya?

## Pruebas automáticas
- EditMode: ReglaReparacion y RepararPuntos. PlayMode: ReparacionParcialPruebas.

## Fuera de alcance
- Mejoras y piezas en el taller (bloque de piezas). Intereses o cobradores de deudas (D-tony-85). El taller de la casa de Alan (sigue el contrato
  `ITallerMecha`).
