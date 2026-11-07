# Pedido al chat de arte — 6-nov noche (CYBERFIX): imagen objetivo del juego

Tony eligió **HUD_A_minimalista.png** (imageassets/ui/hud-opciones/) y además decidió que **esa imagen es cómo debe verse todo el
juego**: la calle, la luz, el piso mojado, las casas, el estilo ilustrado. Todo lo de abajo debe salir **en el mismo estilo exacto**:
usa HUD_A_minimalista.png como imagen de entrada/referencia (edit) en cada prompt.
Mismo flujo: prompts + script de PowerShell para WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt), guardando en
`C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\`.

## HUD nuevo (para todas las imágenes de abajo que lleven HUD)
- Arriba izquierda: minimapa circular de líneas finas (como el de HUD_A) y debajo el objetivo en texto delgado.
- Arriba derecha: dinero "$ 12,450" y estrellas de búsqueda turquesa (como en HUD_A).
- Abajo izquierda: barras de VIDA y ESCUDO.
- Abajo derecha: holograma cian del arma y balas "12 / 48".
- Sin paneles ni marcos, tipografía delgada.
- Personaje del jugador en tercera persona como GTA: centrado, abajo, ~1/4 de la altura de la pantalla, de espaldas.

## 1. Imágenes de zona (cómo se verá cada barrio) → imageassets/referencias/look/
Mismo estilo, misma luz de noche azul con niebla, charcos que reflejan, cables entre postes, cuadras cortas (se ve la siguiente
esquina, nunca un pasillo infinito), HUD nuevo encima.
| Archivo | Qué es |
| --- | --- |
| K01_Barrio_HUDnuevo.png | La MISMA escena de HUD_A, sin cambiar nada de la calle, solo con el HUD en el acomodo nuevo (minimapa arriba izquierda, barras abajo izquierda). |
| K02_Tianguis.png | Barrio El Mercado: calle cerrada con puestos de tianguis con lonas de colores y focos, gente, plaza al fondo, un local de armas con reja. Objetivo: "Compra en el mercado · 60 m". |
| K03_NeonAlto.png | Barrio Neón Alto: cruce con el casino VivaCoin (letrero grande dorado y magenta), edificios de 4–6 pisos con pantallas, más neón magenta/turquesa, taxis rosas. Objetivo: "Entra al casino". |
| K04_Barrio_Dia.png | La calle de HUD_A de día: sol de tarde, sombras largas, cielo con nubes, sin neón encendido, piso seco con un charco. |

## 2. Barras de vida y escudo (abajo izquierda) → imageassets/ui/barras/
Solo la esquina inferior izquierda del HUD sobre la calle de HUD_A (recorte de 16:9 enfocando abajo izquierda), mismo estilo
minimalista. VIDA roja-rosa y ESCUDO turquesa. 4 variantes:
| Archivo | Estilo |
| --- | --- |
| B01_Lineas.png | Dos líneas finas largas con texto pequeño encima (como HUD_A). |
| B02_Segmentos.png | Barras en segmentos cortos (cada uno = 10 de vida), número al lado. |
| B03_Arco.png | Dos arcos finos alrededor de un ícono circular chico del personaje. |
| B04_Diagonal.png | Barras inclinadas con corte en diagonal en las puntas, número grande delgado. |

## 3. Mockup del mapa de la ciudad → imageassets/referencias/look/
| Archivo | Qué es |
| --- | --- |
| M10_MapaCiudad.png | Mapa visto desde arriba, ilustrado en el mismo estilo (como el mapa de un juego tipo GTA), de una ciudad de cuadras cortas (~60 × 80 m), con 4 barrios de color distinto: SUBURBIOS (abajo izquierda, casas bajas, naranja), EL MERCADO (abajo derecha, tianguis y plaza, amarillo), NEÓN ALTO (arriba derecha, casino y edificios altos, magenta) y NOVA (arriba izquierda, torres y refinería, azul frío). Calles que doblan, plazas, una glorieta, callejones cortos, autopista elevada en el borde. Íconos con nombre: Casa, I+D+I (Doc), CIC (Tino), Taller El Canepa, Tianguis, Mercado de armas, Casino VivaCoin, Torre Nova. Sin otros textos. |
