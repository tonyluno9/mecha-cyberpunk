# Pedido al chat de arte — 6-nov-2026 (CYBERFIX)

Del chat principal. Necesitamos estas imágenes para la noche del 6-nov (corte 12-nov). Haz los prompts y el script de PowerShell
para WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt) como en los lotes anteriores, guardando cada archivo con su nombre en
la carpeta indicada, dentro de `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\`.
Estilo de siempre: cyberpunk de megaciudad mexicana, desgastado, acentos turquesa y naranja, sin texto ni logos.

## 1. Armas para Tripo → imageassets/armas/
Para convertir a 3D en Tripo. Vista 3/4 de lado, arma completa, fondo blanco liso, sin manos, iluminación pareja.
Deben combinar con la pistola W01 que ya está en el juego (adjúntala como referencia si el flujo lo permite:
imageassets/modelos3d/armas/W01_Pistola/).
| Archivo | Qué es |
| --- | --- |
| W02_Escopeta.png | Escopeta de bomba |
| W03_Fusil.png | Fusil de asalto compacto, mira de punto rojo |
| W04_Francotirador.png | Rifle de francotirador de cerrojo, mira grande, bípode plegado |

## 2. Hologramas de armas para la UI → imageassets/ui/hologramas/
Para el menú de mejora de armas del taller de Tino y el ícono del arma en el HUD. Cada arma **vista exactamente de lado**,
como proyección holográfica: cian/turquesa translúcido, contornos que brillan, líneas de escaneo horizontales, un poco de glitch,
**fondo negro puro** (se usa con mezcla aditiva). Misma forma que el arma real: usar como imagen de entrada (edit) la de cada arma.
| Archivo | Entrada |
| --- | --- |
| H01_Pistola.png | imagen de la pistola W01 (la del lote de la pistola) |
| H02_Escopeta.png | W02_Escopeta.png |
| H03_Fusil.png | W03_Fusil.png |
| H04_Francotirador.png | W04_Francotirador.png |
(Los hologramas dependen de las armas: generar el punto 1 primero.)

## 3. Efectos → imageassets/vfx/
| Archivo | Qué es | Fondo |
| --- | --- | --- |
| F01_Destello.png | Destello de disparo de pistola, chico, compacto y breve, núcleo blanco-amarillo, sin humo. El actual del juego se ve horrible. | negro puro (aditivo) |
| D01_HuecoConcreto.png | Hueco de bala en concreto visto de frente (ortográfico), cráter oscuro con astillado, se desvanece a los bordes. Reemplaza el cuadrado que sale hoy al disparar a paredes. | blanco puro |
| D02_HuecoMetal.png | Hueco de bala en lámina pintada de carro, metal hundido, anillo quemado, pintura saltada. | blanco puro |
No hace falta modelo de bala: las balas no se ven volar; el trazo (si hay) lo hace Unity.

## 4. Calle → imageassets/calle/
Vista cenital ortográfica, solo la pintura, **fondo negro puro** donde no hay pintura (Unity la usa como máscara sobre el asfalto
actual, que a Tony le gusta).
| Archivo | Qué es |
| --- | --- |
| M01_LineasAmarillas.png | Doble línea amarilla continua al centro, vertical, desgastada; que repita arriba/abajo (seamless) |
| M02_CrucePeatonal.png | Cruce peatonal tipo cebra, franjas blancas gruesas desgastadas |

## 5. Para Tripo → los .zip de Tripo a imageassets/modelos3d/
Imagen de entrada para Tripo, fondo blanco liso, iluminación pareja. Caras objetivo en Tripo: ~15–20k carros y personaje.
| Archivo | Qué es |
| --- | --- |
| C01_Policia.png | Policía de la ciudad, adulto, **T-pose estricta, de frente, cuerpo completo**. Uniforme táctico azul marino, chaleco ligero, franjas reflejantes turquesa, cinturón, botas, gorra con visera. Manos vacías, sin arma. Va a Mixamo como los demás personajes. |
| V09_Taxi.png | Sedán compacto viejo de taxi, rosa y blanco, abollado, letrero de techo sin texto. Vista 3/4 frontal, carro completo. |
| V10_Pickup.png | Pickup vieja, turquesa deslavado con óxido, caja con huacales. Vista 3/4 frontal. |
| V11_Van.png | Van de reparto cuadrada, crema y naranja, abollada, puerta corrediza. Vista 3/4 frontal. |
Carros con las ruedas bien separadas visualmente de la carrocería (en el juego las ruedas son piezas aparte).

## Orden
1 → 2 (dependen de 1) → 3 → 4 → 5.
