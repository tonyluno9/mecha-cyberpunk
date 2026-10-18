# T-168 Comprar y cambiar piezas en el taller (lógica y banco, sin pantalla)
Dueño: Tony | Rama: tony/T-168-taller-piezas | Modelo: Sonnet 5.5, medium
Depende de: T-167

## Objetivo
En el banco del taller, con el mecha estacionado, se pueden comprar piezas (se ponen al instante) y poner las que ya tienes en el
inventario; la pieza que quitas va al inventario para venderla o volver a ponerla. Solo se compran piezas de nivel igual o menor que tu
liga, y con el taller bloqueado por renta no se mejora nada. Todo por código y con la E del banco; la pantalla es T-169.

## Contexto
- Diseño: mecha-taller.md ("Compras las mejoras en el taller"; "Una pieza de nivel 3 no está disponible hasta llegar a la liga 3"),
  economia-misiones.md ("Vendes piezas viejas del mecha ... en el mercado negro"; "Taller bloqueado por renta: no puedes reparar ni
  mejorar, pero sí pelear con el mecha dañado").
- Contratos: `Core/Ligas/IProgresoLigas.cs` (`LigaActual`; sin servicio registrado, liga 1), `ICartera` (`IntentarPagar`, `Abonar`,
  `MotivoDinero.Compra` y `Venta`), `IRenta` (`Adeudo`).
- Decisiones: D-039 y D-tony-85 (con dinero negativo no se compra), D-tony-49 y D-tony-90 (el banco vive en el mecha; su pantalla pide
  opciones por evento), D-tony-80 (pieza que no cabe en el inventario: te pagan su valor de venta). Propuestas nuevas: D-tony-117 (nivel ≤
  liga) y D-tony-119 (reglas del taller de piezas).
- Código: `Mecha/Taller/BancoTaller.cs`, `ReglaBancoTaller.cs`, `EstadoBanco.cs`, `OpcionReparacion.cs`; `UI/Taller/PantallaBancoTaller.cs`
  (escucha `OpcionesPedidas`); `Economia/Inventario/ServicioInventario.cs` (`Registro<ServicioInventario>`); `EquipoMecha` (T-167).

## Qué hacer
1. `Mecha/Taller/ReglaTallerPiezas.cs` (pura): para una ranura, con lo puesto, lo que hay en el inventario, la liga, el dinero y si el taller
   está bloqueado, devuelve las opciones en orden: piezas del inventario de esa ranura ("PONER", gratis), piezas de la tienda de nivel ≤ liga
   ("COMPRAR $precio"), y las del nivel siguiente bloqueadas ("SE ABRE EN LA LIGA n", no se eligen). Cada opción dice si se puede elegir y
   por qué no: ya puesta, no te alcanza (o dinero negativo), taller bloqueado, nivel bloqueado. La pieza que ya llevas no aparece. Cada
   opción trae la diferencia de vida, daño y velocidad contra lo puesto (con `ReglaEstadisticasPiezas`).
2. `Mecha/Taller/TallerPiezas.cs` (en el mecha, junto al `BancoTaller`, con su creador): `Opciones(ranura)` y `Elegir(opcion)`:
   - comprar: `IntentarPagar(precio, Compra)`; si no alcanza no cambia nada; si sí, `EquipoMecha.Equipar`;
   - poner del inventario: `Quitar` del inventario y luego `Equipar`; si quitar falla no cambia nada;
   - la que se quitó entra al inventario; si no cabe (máximo), se paga su valor de venta con `Abonar(..., Venta)` y se avisa en el resultado;
   - con el taller bloqueado, el mecha fuera del taller o montado, no hace nada.
   Liga: `Registro<IProgresoLigas>.Actual?.LigaActual ?? 1`.
3. Banco: una sola E para todo el taller. `EstadoBanco` agrega al final `Piezas` (sin daño que reparar, sin renta vencida, mecha en el
   taller y con `TallerPiezas`): el aviso dice "E · Taller · piezas" (texto en `ConfigBancoTaller`) y se puede usar. En `Reparar`, las
   opciones del banco agregan al final "PIEZAS DEL MECHA". Bloqueado sigue diciendo lo de hoy y no deja reparar ni mejorar. Elegir "PIEZAS
   DEL MECHA" avisa un evento nuevo `PiezasPedidas` (lo escuchará la pantalla de T-169); sin nadie que lo escuche, no hace nada. Sin
   `TallerPiezas` en el mecha, el banco es exactamente el de hoy.
4. Revisa que la pantalla del banco (T-149) muestre la opción nueva sin romper sus pruebas; si hace falta, ajústala en UI/Taller/ (mínimo).
5. EditMode `Tests/EditMode/Mecha/ReglaTallerPiezasPruebas.cs` y PlayMode `Tests/PlayMode/Mecha/TallerPiezasPruebas.cs` (taller de prueba,
   cartera, inventario y un doble de `IProgresoLigas`; las elecciones por código).

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Taller/, Piezas/, Datos/, Editor/, Prefabs/ solo con su creador), Assets/_Proyecto/UI/Taller/ (solo si la
pantalla del banco necesita mostrar la opción nueva), Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/UI/
No tocar: Core/, Economia/ (solo se usa), Progresion/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ReglaTallerPiezasPruebas: en liga 1 se compran nivel I y el nivel II sale "SE ABRE EN LA LIGA 2" sin poder elegirse; en liga 3 se
      compran I a III; con $400 el nivel I ($500) dice "no te alcanza"; con dinero negativo no se compra nada pero sí se pone del inventario;
      con el taller bloqueado nada se elige; la pieza puesta no aparece; la diferencia de vida de torso cuerpo a cuerpo I contra el de serie es +30
- [ ] TallerPiezasPruebas: con $1,000 en liga 1, comprar piernas de cuerpo a cuerpo I deja $500, las piernas puestas y las de serie en el
      inventario; poner otra vez las de serie desde el inventario deja las de cuerpo a cuerpo en el inventario y el dinero igual; con 2 piernas
      de serie ya en el inventario, quitar otras paga $100 de venta; sin `IProgresoLigas` registrado actúa como liga 1
- [ ] Banco: sin daño y con `TallerPiezas`, el HUD dice "E · Taller · piezas"; con daño, las opciones terminan en "PIEZAS DEL MECHA";
      con renta vencida sin dinero dice lo de hoy; las pruebas del banco de T-109 y T-149 siguen en verde
- [ ] El creador corrido dos veces no cambia los prefabs (md5 igual)

## Criterios de sensación (para un humano)
- Ninguno todavía (la pantalla llega en T-169).

## Pruebas automáticas
- EditMode: ReglaTallerPiezasPruebas. PlayMode: TallerPiezasPruebas y las del banco.

## Fuera de alcance
- La pantalla de piezas (T-169). Comprar piezas en el mercado negro (solo se venden ahí). Precios que cambian o descuentos (favores, bloque 6).
