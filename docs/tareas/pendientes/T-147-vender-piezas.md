# T-147 Vender piezas y chatarra en el mercado negro
Dueño: Tony | Rama: tony/T-147-vender-piezas | Modelo: Sonnet 5.5, medium
Depende de: T-146

## Objetivo
En la pestaña VENDER del mostrador del mercado negro el jugador ve las piezas y la chatarra que lleva, con lo que le pagan por cada una, y
las vende con Enter. Cumple el criterio de economia-misiones.md "Puedo vender una pieza vieja en el mercado negro" con las piezas ganadas por
apuesta (las piezas viejas del mecha usarán lo mismo cuando exista el bloque de piezas).

## Contexto
- Diseño: economia-misiones.md ("Vendes piezas viejas del mecha, piezas ganadas en peleas por apuesta y chatarra de robos en el mercado negro").
- Decisiones: D-039, D-tony-55 (el HUD a pie anuncia cada movimiento de dinero: "+$300 · VENTA"). Propuestas nuevas: D-tony-81 y D-tony-82.
- Código de T-142 (inventario, `DatosObjeto.valorVenta`) y T-146 (`MostradorTienda`, `PantallaTienda`).

## Qué hacer
1. `MostradorTienda`: campo `compra` (lista de `TipoObjeto` que ese mostrador compra). En `MostradorMercadoNegro.prefab` (con el creador):
   `Pieza` y `Chatarra`. Con la lista vacía, la pestaña VENDER no aparece.
2. `PantallaTienda`, pestaña VENDER: lista con ↑ ↓ los objetos del inventario cuyo tipo compra el mostrador y con `valorVenta` > 0, con nombre,
   "$300 c/u" y "TIENES n"; Enter vende uno: `Abonar(valorVenta, MotivoDinero.Venta)` y quita uno del inventario (si quitar falla, no paga).
   Sin nada vendible: "NO TIENES NADA QUE LE INTERESE". El dinero de arriba se actualiza al instante.
3. Regla pura `Economia/Inventario/ReglaVenta.cs` (qué se puede vender en un mostrador y cuánto paga), con su prueba de EditMode.
4. PlayMode `Tests/PlayMode/UI/VenderMercadoPruebas.cs` en Pruebas_Economia con entrada simulada (E, flechas, Enter, Esc).

## Carpetas permitidas
Assets/_Proyecto/UI/ (Tiendas/, Datos/, Editor/, Prefabs/), Assets/_Proyecto/Economia/Inventario/, Assets/_Proyecto/Tests/EditMode/Economia/,
Assets/_Proyecto/Tests/PlayMode/UI/, Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity (solo con su creador), docs/capturas/
No tocar: Core/, Mecha/, Pelea/, otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ReglaVentaPruebas: un kit (valor 0) no se vende; una pieza en un mostrador que no compra piezas no se vende; el pago es `valorVenta`
- [ ] VenderMercadoPruebas a): con 2 Brazos de chatarra, 1 kit y $100, E en el mostrador, → a VENDER: aparece solo el brazo ("$300 c/u",
      "TIENES 2"); Enter: dinero $400 y 1 brazo; el HUD a pie mostró "+$300 · VENTA" al cerrar la pantalla (o encima, si se ve)
- [ ] b) Enter otra vez: dinero $700, 0 brazos y la pestaña dice "NO TIENES NADA QUE LE INTERESE"; el kit sigue en el inventario
- [ ] c) con el mercado cerrado no se abre la pantalla (el aviso de T-146 sigue); Esc cierra y el jugador vuelve a moverse
- [ ] Captura docs/capturas/T-147-1.png: la pestaña VENDER con un brazo

## Criterios de sensación (para un humano)
- Pruebas_Economia: gana una apuesta con pieza (T-145) y véndela. ¿Vender se siente como deshacerte de chatarra por lana rápida?
- ¿$300 por un brazo ganado se siente buen botín sin valer más que pelear?

## Pruebas automáticas
- EditMode: ReglaVentaPruebas. PlayMode: VenderMercadoPruebas.

## Fuera de alcance
- Vender piezas equipadas o viejas del mecha (bloque de piezas). Regatear, precios que cambian. Chatarra de robos (misiones).
