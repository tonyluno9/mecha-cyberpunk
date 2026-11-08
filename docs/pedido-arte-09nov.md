# Pedido al chat de arte — 8-nov madrugada (CYBERCITY): menús y logo con el look nuevo

Tony quiere que los menús de mejora (armas e implantes) y el menú de inicio tengan el LOOK de las referencias que pasó
(`imageassets/referencias/menus/REF_*.png`, capturas de un juego cyberpunk comercial): paneles oscuros con líneas rojas finas,
esquinas cortadas en diagonal, acentos cian, casillas con borde de color por rareza, tipografía delgada condensada en mayúsculas,
mucho aire, un holograma al centro. **Diseño propio:** se toma el estilo, NO se copian logos, nombres, íconos ni textos de ese juego.
Paleta: rojo coral (#FF5A4E aprox.) para líneas y títulos, cian (#3FE3E0) para lo activo, fondo casi negro rojizo, y los colores de
rareza (verde, azul, morado, naranja). Textos en español.
Mismo flujo: prompts + script de PowerShell para WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt). Usar las REF como
imagen de referencia de estilo; para el personaje, PJ01 (`imageassets/modelos-nuevos/PJ01_Protagonista_v2.png`); para las armas,
H01–H04 (`imageassets/ui/hologramas/`).

## 1. Mockups de menús (16:9) → imageassets/referencias/menus/
| Archivo | Qué es |
| --- | --- |
| M20_MenuInicio.png | Menú de inicio: panel vertical rojo translúcido a la izquierda con el logo CYBERCITY arriba y las opciones CONTINUAR, NUEVA PARTIDA, CARGAR, OPCIONES, CRÉDITOS, SALIR; de fondo una calle de la ciudad del juego de noche (estilo K01) desenfocada; número de versión abajo. |
| M21_MejoraArmas.png | Banco de Tino: arriba pestañas (MEJORAS / ARMAS), a la izquierda filas de íconos de categoría y una cuadrícula de armas en casillas con borde de color; al centro la pistola W01 como holograma cian grande con una pieza separada brillando (el cargador); a la derecha nombre del arma, rareza, DAÑO, CADENCIA, CARGADOR con antes → después, y abajo las piezas a mejorar en casillas (CARGADOR, CAÑÓN, MIRA, EMPUÑADURA) con costo en $ y un botón MEJORAR. |
| M22_ClinicaImplantes.png | Clínica del Doc: al centro el cuerpo de PJ01 como holograma (rojo/naranja con venas y líneas de luz), con líneas que salen hacia casillas a los lados: CABEZA / OJOS a la izquierda arriba, BRAZOS a la izquierda, PIERNAS a la derecha, PIEL/ESCUDO a la derecha; cada grupo con 2–3 casillas de implante; un recuadro de detalle del implante seleccionado (EXO DE PIERNAS II, descripción, costo); arriba a la derecha "I+D+I · CLÍNICA DEL DOC" y el dinero. |

## 2. Logo con el look nuevo → imageassets/logo/
CYBERCITY en letras de trazo agresivo inclinado tipo pincel/marcador digital (original, no imitar el logo de ningún juego), con una
línea o subrayado y un detalle propio (un poste de luz o la silueta de la torre). Fondo negro puro. 4 variantes:
| Archivo | Color |
| --- | --- |
| LG20_Amarillo.png | Amarillo eléctrico con borde cian fino. |
| LG21_Rojo.png | Rojo coral con brillo. |
| LG22_Cian.png | Cian con glitch rojo leve. |
| LG23_Blanco.png | Blanco con sombra roja desplazada. |

## 3. Íconos para las casillas (hojas de íconos, fondo negro puro, cada ícono dentro de su casilla) → imageassets/ui/menus/
| Archivo | Qué es |
| --- | --- |
| H30_IconosImplantes.png | 6 íconos ilustrados (no de línea), estilo de las REF: exo de piernas I, exo de piernas II, exo de brazos I, exo de brazos II, ojo biónico, visor goggles. Basados en IMP01–03 (`imageassets/modelos-nuevos/IMP0*_v2.png`). |
| H31_IconosPiezasArma.png | 6 íconos ilustrados de piezas de arma: cargador, cañón, mira, empuñadura, silenciador, chip de daño. |
| H32_IconosCategorias.png | Fila de 8 íconos de categoría en línea fina roja: pistolas, escopetas, rifles, francotiradores, cuchillo, consumibles, implantes, piezas. |
