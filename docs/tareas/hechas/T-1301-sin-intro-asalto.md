# T-1301 · Quitar la intro del asalto (playtest 17-nov, punto 1)

- `AsaltoDinero.ConCinematica` (estático, false de fábrica): sin cámara, barras ni HUD oculto; el asalto queda como si se hubiera saltado con Enter
  (robo hecho, objetivo "RÓBATE UN CARRO" activo, el ladrón sube al carro y huye). Para volver a prenderla: `ConCinematica = true`.
- Espera máxima del carro del ladrón: 30 s (sin la cinemática no hay margen previo).
- Pruebas: `AsaltoDineroT728Pruebas` (7). Las pruebas viejas prenden la cinemática en Setup; la nueva
  `Asalto_SinCinematica_NoHayCamaraNiHudOcultoYLaMisionQuedaActiva` la apaga. Corrida de la clase: 5/7; las 2 del maletín fallan por el orden
  (memoria "asalto-pruebas-orden") y solas pasan 2/2. El ladrón sube al carro a los 15.6 s.
