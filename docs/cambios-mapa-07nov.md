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
