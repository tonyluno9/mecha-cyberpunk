# Pedido al chat de arte — 8-nov (CYBERCITY): lo que le falta a la calle de K01

Del chat principal (sesión 2, noche del 7-nov). La calle de prueba de K01 ya está hecha en Unity con el kit C (capturas
`docs/capturas/s2c-calle-prueba-*.png`). Esto es lo que se armó en Unity a falta de modelo y conviene pedir para que se vea igual que la imagen.
Mismo flujo: prompts + script de PowerShell para WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt), guardando en
`C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\modelos-nuevos\`. Estilo: el de
`imageassets/referencias/look/K01_Barrio_HUDnuevo.png` (usar K01 como imagen de entrada). Para Tripo: fondo blanco liso, vista 3/4,
objeto completo, iluminación pareja, sin texto salvo donde se dice.

## 1. Piezas de la calle (las que hoy son una caja con textura o una copia de otra cosa)
| Archivo | Qué es | Hoy en Unity |
| --- | --- | --- |
| TK01_BaseTinaco.png | Base de ladrillo rojo desgastado para tinaco, 1.5 × 1.9 m y 0.7 m de alto, con un tubo que baja. Como las de K01 arriba de las casas. | Caja con ladrillo dibujado en código. |
| NE01_NeonBandera.png | Letrero de neón chico de bandera (sale de la pared): caja negra de 0.6 × 0.8 m con un tubo de neón que hace un dibujo sin letras (onda, rayo). Dos versiones: magenta y turquesa. | Caja C15b con una cara de neón dibujada en código. |
| NE02_NeonPared.png | Letrero de neón plano de pared, rectangular de 0.9 × 0.5 m, tubo turquesa con un ícono sin letras (taza, foco, ojo). | Igual que NE01, pegado a la pared. |
| AL01_Alcantarilla.png | Rejilla de alcantarilla rectangular de banqueta junto a la guarnición, de fierro, vista de arriba en 3/4. | Copia de la tapa redonda que ya había. |
| BS01_Basura.png | Juego de basura chica del piso: papeles arrugados, una lata aplastada, un vaso, hojas. Piezas sueltas en una sola imagen. | Bolsas de basura copiadas del callejón. |
| MT01_MuroTrasero.png | Muro de atrás de casa para callejón: aplanado sucio, ventanita con barrotes, tubos de desagüe, medidor, un foco. 4 m de ancho × 6 m de alto. | Muro liso pintado. |
| TL01_Toldo.png | Toldo de lámina acanalada amarilla con soportes, para ponerlo sobre puertas de vivienda (no solo en el local C01). 2.5 m de ancho. | Solo el que trae C01. |
| ES01_Semaforo.png | Semáforo de esquina mexicano: poste gris con brazo y caja de 3 luces, un letrero de nombre de calle en blanco (sin texto). Para las esquinas de K05. | No hay (lo necesita el kit de esquinas). |
| FA01_Farmacia.png | Letrero de neón de FARMACIA con cruz verde (este SÍ dice FARMACIA), para la esquina de K05. | Se busca el letrero_farmacia del kit viejo. |

## 2. Para Nova (K06)
| Archivo | Qué es |
| --- | --- |
| NV01_FachadaOficinas.png | Módulo de fachada de edificio de oficinas: 6 m de ancho × 3.5 m de un piso, cristal oscuro con franja de luz blanca fría, marco gris. Se apila. |
| NV02_PlantaBajaNova.png | Planta baja de oficina de Nova: puerta de cristal, marco con luz turquesa, logo "NOVA" chico (este SÍ dice NOVA). |
| BL01_Bolardo.png | Bolardo de acero con franja reflejante, 0.9 m. |
| BT01_BoteBasura.png | Bote de basura metálico redondo de banqueta corporativa. |

## Notas
- Las casas, el piso mojado, los charcos que reflejan, los halos de luz, los cables y el posproceso ya están en Unity: no hace falta pedirlos.
- Cuando lleguen TK01, NE01/NE02, AL01, BS01 o MT01 se cambian en `KitS2c` (Assets/Arte/Editor/KitS2c.cs) por lo hecho en código y se vuelve a correr `ColocarS2cCalle.Colocar`.
