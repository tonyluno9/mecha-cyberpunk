# Guía de estilo de los menús de CyberCity

La base es el HUD H10 (`imageassets/referencias/look/H10_HUD.png`), llevado a pantalla completa. El código común está en
`Assets/_Proyecto/UI/Kit/TrazoH10.cs` (colores, marcos, reglas, texto) y `AnimH10.cs` (animaciones con DOTween).

Esta guía es un diseño propio. Sin papel picado, sin rótulos pintados a mano en los marcos y sin imitar la composición, los marcos ni los
colores de otros juegos.

## Color

| Uso | Nombre en código | Valor |
| --- | --- | --- |
| Color principal: líneas, títulos, elegido, holograma | `Turquesa` | #5CE3D6 |
| Líneas secundarias y marcas de regla | `TurquesaTenue` | #5CE3D6 al 42 % |
| Fondo del elemento elegido | `TurquesaFondo` | #5CE3D6 al 8 % |
| Acento, poco y con intención: lo que cuesta, lo que mejora, la acción principal | `Amarillo` | #F6F326 (el del logo LG24) |
| Solo alertas y costos que no alcanzan | `Coral` | #F56E63 |
| Texto | `Texto` | #E6F5F3 |
| Texto secundario | `TextoTenue` | #8EB0B0 |
| Panel, casi transparente | `FondoPanel` | #03090D al 62 % |
| Velo de pantalla completa | `FondoPantalla` | #020609 al 82 % |

Los colores de rareza se ven solo como un filo fino en la casilla: nunca rellenan.

## Letra

Rajdhani en todo, la misma del HUD (`TrazoH10.Fuente`). Tamaños a 1920x1080:

| Rol | px |
| --- | --- |
| Título de pantalla | 52 |
| Cifra grande | 44 |
| Sección | 34 |
| Fila | 28 |
| Dato | 24 |
| Micro (mínimo permitido) | 20 |

Mayúsculas en títulos, secciones y botones. Oraciones normales en descripciones.

## Marcos y líneas

- Línea de 1.5 px. No hay bordes gruesos ni brillos de neón alrededor de los paneles.
- Un solo corte, en la esquina superior derecha, de 16 px. Las demás esquinas son rectas.
- Paneles casi transparentes: la ciudad o el holograma se alcanzan a ver detrás.
- Marcas de regla en los bordes de los módulos, como en un instrumento.
- Módulo de 12 px para separar y margen de 48 px a la orilla de la pantalla.

## Composición

- El holograma es el protagonista: ocupa el centro y los paneles lo rodean sin taparlo.
- Densa pero ordenada: toda cifra visible es un dato real del juego; no hay texto de relleno.
- Una barra de título arriba (nombre del lugar a la izquierda, dinero a la derecha) y una barra de teclas abajo en todas las pantallas.

## Movimiento (DOTween, tiempo sin escala)

| Evento | Animación | Duración |
| --- | --- | --- |
| Abrir pantalla | Paneles escalonados que llegan deslizándose con un parpadeo | 0.32 s, escalón de 0.05 s |
| Línea de título | Crece desde su pivote | 0.32 s |
| Hover o foco | La línea se enciende y el fondo se tiñe | 0.12 s |
| Selección | Destello de la casilla y entrada del detalle | 0.12 a 0.45 s |
| Cambio de valor | El número corre hasta el valor nuevo | 0.45 s |

Todo se puede usar con teclado, ratón y control; el hover y el foco se ven igual.

## Lo que no cambia

La lógica, los datos, las compras y las pruebas de cada pantalla. Solo cambia cómo se ve.
