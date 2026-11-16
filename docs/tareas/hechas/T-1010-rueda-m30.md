# T-1010: rueda de armas y objetos como el mockup M30 (playtest 16-nov, punto 15)

- Anillo de sectores con contorno turquesa y brillo, el resaltado en amarillo, y octágono al centro con el nombre y las balas
  ("30 / 120") de lo resaltado, como `imageassets/referencias/menus/M30_RuedaArmas.png`. Sprites hechos en código (`DibujoRueda`).
- La rueda muestra solo lo que llevas: armas de fuego (pistola arriba, en el sentido del reloj), puños y objetos con piezas;
  con menos de 8 casillas rellena con sectores vacíos. El número de armas sale del enum `ArmaAPie`: las armas nuevas entran solas.
- Con la rueda abierta se esconde todo el HUD, también el minimapa y lo que comparte canvas con la rueda (pendiente del 15-nov).
- Las pruebas eligen con el ángulo que da la rueda abierta (`RuedaArmasObjetos.Actual.AnguloDe(id)`).
- También se ajustaron TerceraPersonaPruebas y ApuntadoAsistidoPruebas a la cámara de T-1001 (apuntar mueve la cámara al hombro).
- Pruebas: ReglaRuedaArmasPruebas 7/7, RuedaArmasT991Pruebas 9/9; clases que usan la rueda 60/65, y los 4 fallos ya estaban en main
  (ArmasPruebas.ComprarYUsarEscopeta, CuerposBotinT746, HudH10 Estrellas) más TerceraPersona, que ya se corrigió.
- Capturas revisadas: `docs/capturas/t1010/`.
- Queda: el icono de los puños es el sprite verde de antes, no un dibujo de línea como en el mockup.
