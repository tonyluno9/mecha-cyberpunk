# Pedido al chat de arte — 6-nov 23:45 (CYBERFIX): HUD final, esquina, Nova y mapa corregido

Mismo estilo de HUD_A / K01 (usa `imageassets/referencias/look/K01_Barrio_HUDnuevo.png` como imagen de entrada en todas).
Mismo flujo: prompts + script de PowerShell para WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt), en
`C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\referencias\look\`. Sin lluvia en ninguna.

## H10_HUD.png — HUD final sobre la calle de K01
Tony quiere el ACOMODO de un HUD de juego cyberpunk comercial que le gustó, pero con diseño PROPIO (no copiar gráficos,
fuentes ni íconos de otro juego). Misma calle de K01, personaje igual, solo cambia el HUD:
- Arriba izquierda: barra de ESCUDO turquesa delgada y corta; debajo la barra de VIDA rojo-rosa larga y delgada con número
  "143 / 204"; debajo las estrellas de búsqueda turquesa (3 de 5) y el dinero "$ 12,450".
- Arriba derecha: minimapa CUADRADO de líneas finas con calles y la ruta punteada, y debajo el objetivo "Ve con el Doc · 120 m".
- Abajo izquierda: 3–4 casillas chicas de objetos (lata de bebida, papitas, venda) con su tecla [1][2][3] y cantidad.
- Abajo derecha: holograma cian del arma de lado, balas grandes "12 | 48", un muñequito chico del cuerpo al lado, y arriba dos
  ayudas de botón pequeñas: "[R] Recargar" y "[F] Golpe".
- Líneas finas, sin paneles de fondo, tipografía delgada, textos en español.

## K05_EsquinaBarrio.png — un cruce del barrio (sin HUD)
Cruce de dos calles del barrio de K01, de noche, visto desde una esquina: las 4 esquinas con casas de 2 pisos de colores y
negocios en planta baja (tienda con cortina, puesto de tacos, farmacia), postes con lámpara naranja y cables cruzando en
diagonal, semáforo, un letrero de nombre de calle en el poste (sin texto legible), cruces peatonales pintados, charcos que reflejan.
Debe verse que la siguiente cuadra empieza a ~70 m (cuadras cortas).

## K06_CalleNova.png — una calle del barrio Nova (con HUD H10)
Calle más ancha y limpia del barrio de la corporación Nova: edificios de oficinas de 6–10 pisos con cristal oscuro y franjas de luz
blanca fría y azul, banquetas anchas, bolardos, cámaras de seguridad, la gran torre de Nova al fondo con su luz en la punta,
pocos neones, niebla azul. Mismo estilo ilustrado. Objetivo: "Llega a la torre Nova".

## M11_MapaCiudad.png — mapa corregido
Mismo estilo ilustrado visto desde arriba que el mapa anterior, PERO:
- Nada de 4 cuadrados marcados. Una sola ciudad continua de cuadras cortas (~60 × 80 m) en todo el mapa, calles que doblan,
  callejones cortos, 2–3 plazas, una glorieta, autopista elevada en un borde.
- Los barrios se distinguen solo por el COLOR de su luz y el tipo de edificio, mezclándose en los bordes: Suburbios naranja (casas
  bajas), El Mercado amarillo (tianguis en una calle, sin focos colgando), Neón Alto magenta (casino y edificios altos), Nova azul
  frío (torres y la torre Nova; refinería chica, no la mitad del barrio).
- Íconos con nombre: Casa, Taller El Canepa (enfrente de la casa), I+D+I del Doc y CIC de Tino (las dos a 1–2 cuadras de la casa, en
  Suburbios), Tianguis, Mercado de armas, Casino VivaCoin, Torre Nova. Sin nombres grandes de barrio encima.
