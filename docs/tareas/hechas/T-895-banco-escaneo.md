# T-895: banco de Tino (C2)

Sobre el menú de T-814/T-887, en turquesa y con su acomodo propio (lista a la izquierda, holograma al centro, tabla debajo y piezas a la derecha).

- Al mejorar, una franja recorre el arma, la pieza regresa a su lugar, destella y los números suben animados (`VistaHologramaArma.Escanear`, DOTween).
- El resalte del holograma pasó de naranja a turquesa claro (`HologramaArma.mat`).
- Las piezas llevan su ícono H34 (cargador, cañón y mira), cortado con `CortarIconosMenusS2m`.
- No se calcó la composición de M21: reproduce la de la pantalla de otro juego. Ver docs/reporte-09nov-s2b.md.

Pruebas: EditMode 2237/2237; PlayMode `MenuArmasPruebas` en verde con el escaneo. Capturas: docs/capturas/s2m-c2-*.png.
