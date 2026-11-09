# T-903: tienda sobre la guía H10

- La tienda se viste por código al abrir (el prefab no se toca): cabecera con el dinero, pestañas COMPRAR y VENDER con recuadro (elegida en turquesa), lista a la izquierda con recuadro por fila (hover, línea que se enciende y clic para elegir) y detalle a la derecha.
- Detalle con datos reales del objeto: nombre, tipo, descripción de la oferta, precio, lo que llevas, balas por paquete (munición) y cuánto te quedaría; en VENDER, lo que te pagan, lo que tienes y cuánto tendrías. Precio en amarillo, coral si no alcanza.
- Se conservan compra, venta, descuento por favor (precio rebajado en la fila y en el detalle, línea del favor) y munición a la reserva. `TextoFila(i)` devuelve el mismo texto de antes; la fila dibuja nombre, lo que llevas y el precio por separado.
- Con muchas filas el paso entre ellas se achica para que quepan en el panel.

Pruebas: `TiendaH10Pruebas` (nueva), `MostradorTiendaPruebas`, `VenderMercadoPruebas`, `TiendaCicT747Pruebas`, `MenusCyberfixPruebas`, `MercadoArmasT720Pruebas`, `ArmasMercadoPruebas`, `FavoresPruebas`. Captura: docs/capturas/ui-tienda.png.
