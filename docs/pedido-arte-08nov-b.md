# Pedido al chat de arte — 7-nov tarde (CYBERCITY)

Va junto con `docs/pedido-arte-08nov.md` (piezas de calle de la sesión 2). Mismo flujo: prompts + script de PowerShell para
WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt). Estilo: K01 (`imageassets/referencias/look/K01_Barrio_HUDnuevo.png`).

## 1. Gráficos del HUD → imageassets/ui/hud/
Tony quiere que el HUD use gráficos hechos por el chat de arte, no dibujados por código. Acomodo: H10_HUD.png. Las barras: como en la
captura de referencia que le gustó (un juego cyberpunk comercial): **sin las palabras VIDA ni ESCUDO**; arriba una barra delgada y corta
de escudo, debajo la de vida larga y delgada con el número al lado, con un pequeño emblema/ícono a la izquierda. Diseño propio, no copiar.
Cada pieza sola, sobre fondo negro puro, de frente, plana (para recortar y usar en Unity):
| Archivo | Qué es |
| --- | --- |
| H20_BarrasVida.png | Bloque de vida y escudo completo (llenas), rojo-rosa y turquesa, con emblema y número "143". |
| H21_BarrasVacias.png | El mismo bloque con las barras vacías (solo el marco/fondo), para rellenar en Unity. |
| H22_MarcoMinimapa.png | Marco cuadrado del minimapa en líneas finas turquesa con esquinas marcadas, centro vacío negro. |
| H23_IconosObjetos.png | Hoja con 6 íconos de línea fina turquesa en cuadrícula: lata de bebida energética, bolsa de papitas, venda, taza de café, batería, granada. |
| H24_Munequito.png | Silueta del cuerpo de frente en línea fina cian (cabeza, torso, brazos, piernas separables), para marcar daño e implantes. |
| H25_Estrella.png | Estrella de búsqueda turquesa, llena y vacía (dos en la imagen). |
| H26_CasillaObjeto.png | Casilla de objeto con su esquina para la tecla, vacía. |

## 2. Referencias de lugares → imageassets/referencias/look/
| Archivo | Qué es |
| --- | --- |
| R10_Cuarto.png | Cuarto del jugador rediseñado, más amplio (unos 5 × 4 m): cama, escritorio con computadora vieja, ropero, ventana con barrotes y luz de neón, espejo de cuerpo completo, pósters, ropa, mochila, puerta. Misma luz y estilo de K01, vista desde la puerta a la altura de los ojos. |
| K07_PuenteNova.png | Calle de Nova (estilo K06) con una pasarela peatonal que cruza la calle y se sube por escaleras desde las dos banquetas, con barandal y luz fría. Que tenga sentido: conecta las dos aceras. |

## 3. Personajes para Tripo (T-pose estricta, de frente, cuerpo completo, fondo blanco liso) → imageassets/modelos-nuevos/
| Archivo | Qué es |
| --- | --- |
| PJ01_Protagonista.png | Chico joven, cabello rizado medio corto con taper fade, dos líneas rapadas del lado derecho de la cabeza, camiseta sin mangas, brazos un poco marcados, pantalón cargo, tenis. Estilo del juego. |
| AL10_Aliado1.png | Aliado del barrio: chico con sudadera con capucha y chaleco, gorra. |
| AL11_Aliado2.png | Aliada del barrio: chica con chamarra corta, trenzas, botas. |
| AL12_Aliado3.png | Aliado mayor y grande: playera, brazo robótico sencillo, delantal de taller. |
