# Cambios del mapa — 7-nov (sesión 2, para la sesión 1)

La sesión 2 está cambiando la ciudad para que se vea como K01 (docs/plan-look-ciudad.md). Aquí va lo que cambia de lugar o de forma para
que tráfico, policía, peatones, misiones y el mapa lo sepan. Coordenadas del mundo en metros (x al oriente, z al norte).

## Lo que NO cambia
- **La red de calles de los carros** (3 × 2 manzanas de 80 m, paso de 94 m, calles de 14 m): `RutaCallesBarrio`, `GrafoCalles`, las patrullas y
  los carros del tráfico siguen sirviendo tal cual.
- **Las rutas de los peatones** (contorno de cada manzana a 2.4 m de la orilla, `CreadorPeatones`): siguen sobre las banquetas de siempre.
- **La casa, el taller El Canepa, el I+D+I del Doc y el CIC de Tino** siguen donde estaban: ya están cerca de la casa, como en M11
  (Canepa enfrente, el Doc a ~90 m y Tino a ~95 m). Sus interiores, puntos de misión e íconos no se movieron.

## T-870: calle nueva en la manzana de la casa (calle de prueba de K01)
- Calle chica **sin tráfico** que cruza la Manzana_4 de oriente a poniente: de x = -54 (sale a la calle de la casa, a 34 m al norte de la
  puerta de la casa) a x = -134 (sale a la calle del poniente), centrada en z = -43.
  - Arroyo de 7.5 m (z de -46.75 a -39.25), a 0.22 m de alto; banquetas de 2 m a 0.37 m; fachadas en z = -48.75 (sur) y z = -37.25 (norte).
  - Raíz de la escena: `S2c_CalleDorada`.
- Se apagaron los edificios greybox de la fila de en medio de la Manzana_4 (`Barrio/Manzana_4/Edificios/Edificio_34, 43, 40 y 37`) y lo
  que tenían encima. En su lugar hay 32 casas de 2 pisos con colliders.
- El **callejón de los SKULLS** (z de -64 a -54) no se movió. Su pared norte ahora es la parte de atrás de las casas nuevas (z = -54).
  Las lámparas y medidores que colgaban de esa pared se pegaron al muro nuevo (se movieron menos de 1 m).
- Si un peatón, una patrulla o un encargo necesita pasar por ahí: es una calle de a pie, con un carro estacionado (adorno, sin script) junto a la
  banqueta sur en x = -71.5.
- **Mapa / minimapa**: conviene dibujar esta calle (rectángulo x -134..-54, z -46.75..-39.25) como calle chica.
- **Reflejo de los charcos**: `S2c_Look` tiene `ReflejoCharcos` (Mundo/Ambiente), que pinta una cámara extra cada cuadro desde el ojo de la
  cámara principal reflejado en el piso. Los charcos van en la capa integrada Water (4), que esa cámara no ve. Si una prueba cuenta cámaras
  prendidas, ahí hay una más (`CamaraReflejoCharcos`, oculta).

## T-872: Suburbios en cuadras cortas
- **Calle nueva de norte a sur sin tráfico** en la Manzana_4: eje x = -110.5, de z = -87 (calle del sur) a z = -7 (avenida). Arroyo de 7.5 m
  (x de -114.25 a -106.75) a 0.22 m. Cruza la calle de prueba en (-110.5, -43): ahí están las cuatro esquinas de K05 (semáforos con micas que
  no cambian de color, cruces pintados; la esquina noreste es la farmacia con letrero FARMACIA). También cruza el andador que sale del
  callejón de los SKULLS al poniente (z de -64 a -59), que sigue abierto.
- **La calle de K01 sigue al oriente** cruzando la calle de la casa: de x = -40 a x = 40 por la fila de en medio de la Manzana_5 (z = -43,
  mismas medidas). Llega a la calle de x = 47. En las dos calles con tráfico hay cruce peatonal pintado (x = -47 y x = 47, z de -44.5 a -41.5).
  Los carros de esas calles no cambian; si hace falta que se detengan ahí, es cosa del tráfico.
- **Edificios greybox apagados**: Manzana_4 → 35, 36, 38, 39, 41 y 44; Manzana_5 → 46, 49 y 51 (con sus piezas de fachada de noche y lo que
  tenían encima). En su lugar hay ~80 casas de 2 pisos del kit con colliders, en todas las calles de esas cuadras.
- **Se quedan igual**: la casa (Lugar_Casa, cuarto y baño), el I+D+I del Doc y su letrero, el callejón de los SKULLS con sus muros (edificios 33 y 42),
  el taller El Canepa (edificio 45), el bar (Lugar_Bar) y el CIC de Tino (Manzana_1).
- **Peatones**: las rutas de banqueta del contorno de cada manzana no cambian. Las calles nuevas no tienen peatones; si la sesión 1 quiere
  ponerles, los contornos nuevos son las cuadras de 14.75 × 20 m, 17.75 × 20 m, etc. (ver `ColocarS2cSuburbios.cs`).
- **Mapa / minimapa**: conviene dibujar las dos calles nuevas (rectángulos x -114.25..-106.75, z -87..-7 y x -134..40, z -46.75..-39.25).

## T-873: El Mercado (tianguis de la avenida, K02)
- Los edificios greybox que daban al tianguis se cambiaron por casas de 2 pisos con locales del kit: Manzana_2 → 10 y 15; Manzana_5 → 47 y 52.
  Las casas tienen su frente en z = 10 (norte) y z = -10 (sur), con esquinas a la calle de la casa (x = -37) y a la de x = 47 (x = 37).
- Se quedan igual: el clandestino (Lugar_Clandestino), el bar (Lugar_Bar), el callejón del mercado de armas (PuestosCallejon) y todos los
  puestos del tianguis. Sin focos colgando. Hay cables que cruzan la avenida de casa a casa (a ~6 m de alto) y charcos en el arroyo.
- La avenida, sus banquetas y el tráfico no cambian.

## T-874: Neón Alto (K03)
- La calle del casino (x = 47, de z = 10 a 86) lleva unos 30 letreros de neón chicos (magenta y turquesa, con luz que se prende de noche) en
  las fachadas de los dos lados, cables de fachada a fachada a 7–11 m y charcos que reflejan. No se movió ni se apagó ningún edificio.
- Las luces verdes del barrio (T-507) pasaron a magenta y turquesa (M11: Neón Alto es magenta). El volumen de zona de Neón Alto ahora tiene
  menos exposición, más saturación y un tinte hacia el magenta. No cambian ni el tráfico, ni el casino, ni sus puertas.

## T-875: Nova (K06), lo mínimo para la demo
- La Manzana_6 (sureste, sin lugares de la historia) tiene una calle ancha de Nova **sin tráfico** en el eje de la calle de K01: z = -43, de
  x = 54 a 134 (arroyo de 10 m, banquetas de 4 m, fachadas en z = -52 y z = -34). Torres T20–T23 a los dos lados, bolardos, cámaras, botes y
  luz fría. Cruce pintado en la calle de x = 141.
- Edificios greybox apagados: 54, 57, 60 (fila de en medio) y 55, 58, 61 (fila del norte, que tapaban las torres).
- La calle de K01, la de Suburbios y la de Nova van en el mismo eje (z = -43) de x = -134 a 134, cortadas solo por las tres calles con tráfico
  (x = -47, 47 y 141), donde hay cruces pintados.

## Colliders de las casas nuevas (para peatones y policía)
- Las piezas de fachada del kit ya no chocan. Lo que choca es el cuerpo de cada casa, que empieza en su línea de fachada. Así las rutas de
  banqueta de los peatones (a 2.4 m de la orilla de la manzana) no pegan con toldos, marcos ni tubos (PeatonesBarrioPruebas en verde).
