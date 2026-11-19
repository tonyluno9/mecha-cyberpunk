# Pedido al chat de arte — 8-nov 13:00 (CYBERCITY): logo pintado e íconos que faltan

Mismo flujo: prompts + script de PowerShell para WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt).

## 1. Logo → imageassets/referencias-privadas/logo/
A Tony le encanta LG20 (CYBER agresivo + CITY con estilo pintado). Rehacerlo con el **estilo pintado más marcado** y con **letras
propias** (que no repitan las letras del logo de ningún juego): trazos de pincel/aerosol, salpicaduras, textura de pintura sobre
muro, inclinado. Usa LG20 como referencia de composición. Fondo negro puro.
| Archivo | Variante |
| --- | --- |
| LG24_Pintado_Amarillo.png | Amarillo eléctrico, CITY con goteo de pintura. |
| LG25_Pintado_Turquesa.png | Turquesa con borde coral. |
| LG26_Pintado_Blanco.png | Blanco con sombra turquesa desplazada y salpicaduras. |

## 2. Íconos de implante nivel 2 → imageassets/ui/menus/
Los de H30 de nivel 2 no se parecen a nuestros modelos. Rehacer solo esos 3, en el mismo estilo de H30, usando como entrada la
imagen de nuestro modelo de nivel 2:
| Archivo | Entrada |
| --- | --- |
| H33_Implantes_Nivel2.png (3 íconos en fila) | Exo de piernas II = `modelos-nuevos/IMP01_ExoPiernas_v2.png` (exoesqueleto completo: cinturón, muslos, rodillas, espinillas, bandejas de pies); Exo de brazos II = `IMP02_ExoBrazos_v2.png` (peto, hombreras, bisagra de codo, guantelete); Visor II = `IMP03_VisorCabeza_v2.png` (visor tipo goggles). |

## 3. Íconos de piezas de nuestras armas → imageassets/ui/menus/
| Archivo | Qué es |
| --- | --- |
| H34_PiezasArma.png | 6 íconos en el estilo de H30 (dentro de su casilla), de las piezas REALES de nuestra pistola W01 (entrada: `imageassets/armas/W01_Pistola.png`): cargador, corredera, cañón, mira, empuñadura, armazón. Mismo diseño, colores y desgaste que la W01. |
