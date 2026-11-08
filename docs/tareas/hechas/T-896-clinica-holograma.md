# T-896: clínica del Doc (C3)

Sobre el menú de T-815, en turquesa y con su acomodo propio (casillas en una columna a la izquierda, cuerpo al centro, detalle a la derecha).

- El holograma ahora es PJ01 horneado desde `PJ01_Implantes.fbx`, con las 6 mallas de implante en el mismo espacio (`ColocarS2mClinica.Hornear`).
- Al elegir (o pasar el ratón por) una casilla, el implante aparece con un barrido de abajo hacia arriba; los instalados se ven en el cuerpo.
- Al instalar: se cobra de verdad, la pieza llega y se ensambla, destella y luego sigue la operación de siempre (`MostradorClinica`) y la silla, sin cambios en esa lógica.
- Íconos H30 (nivel 1) y H33 (nivel 2). Una línea de luz une la casilla elegida con su lugar en el cuerpo.
- `KitUI.CrearPanelH10`: panel turquesa común para los menús.
- `ImplantesDocPruebas` estaba desactualizada desde T-864 (el Doc regala PIERNAS I): se ajustó y ahora compra BRAZOS.
- No se calcó M22: reproduce la de la pantalla de otro juego. Ver docs/reporte-09nov-s2b.md.

Pruebas: EditMode 2237/2237; PlayMode `ImplantesDocPruebas`, `SillaClinicaPruebas`, `PersonajesApoyoPruebas` en verde. Capturas: docs/capturas/s2m-c3-*.png.
