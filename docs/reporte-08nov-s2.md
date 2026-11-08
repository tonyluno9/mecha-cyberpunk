# Reporte sesión 2, noche del 7-nov (docs/playtest-07nov.md, SESIÓN 2)

Trabajé de 16:30 a 19:30 en la carpeta principal. Cada tarea va en su rama `tony/T-88x-*`, con sus pruebas, merge de `origin/main`,
merge `--no-ff` a main y push (HEAD separado sobre origin/main, porque main está abierto en mecha-s1). Plan: docs/plan-08nov-s2.md.
Al final, sobre main: **compila y EditMode da 2072 de 2072.** Unity no quedó abierto en esta carpeta.

Antes de empezar subí a main los paquetes que importaste (DOTween y Particle Pack, con LFS para .tif, .exr, .FBX, .dll; el define
DOTWEEN de ProjectSettings y el Readme de TutorialInfo que trae el Particle Pack), docs/playtest-07nov.md, docs/pedido-arte-08nov-b.md
y las imágenes de imageassets/ (sin zips, .blend ni Assets/_Recovery, Font/ o Logs).

## Lo que quedó (todo en main)

| Tarea | Qué | Capturas |
| --- | --- | --- |
| T-880 | **La luz de K01 en toda la ciudad.** Exposición 2.5 → 1.7, luna 1.1 → 0.32, ambiente y niebla azul de K01 (niebla 0.02 → 0.036), cielo azul marino liso (sin el domo de nubes, pocas estrellas, y los telones pintados del fondo, que tenían nubes y un atardecer, quedan cubiertos por la bruma de noche), postes con alcance de 9 m (antes 20 a 26), focos de pared chicos y el asfalto ya no es metal (por eso se veía negro). **La causa de la luz dispareja:** el volumen de sondas de luz (APV de AMB_Noche) se horneó con la ciudad de antes; las casas nuevas caían en sondas que quedaron dentro de los edificios viejos (negras) o junto a sus luces (quemadas). Se apagó: toda la ciudad usa el mismo ambiente. La calle de prueba se cierra al fondo con casas del kit (antes llegaba al muro del borde y se veía el vacío), con más neones y charcos; el carro estacionado va a lo largo de la banqueta. | s2o-luz-juego-antes, s2o-luz-k01-final, s2o-luz-k01-sin-apv, s2o-luz-zonas-despues |
| T-881 | **Postes: solo brilla el cristal.** El C07 y el N09 son una sola malla con un material: el cristal no es pieza aparte. Para no esperar, el script lee la malla (la cara de abajo de cada cabeza de lámpara) y la textura (el panel naranja del C07, la barra blanca del N09) y pinta una máscara de emisión: brilla solo eso. Se quitó la "Mica" de primitivo que se veía como un hueco. | s2o-poste-*-antes/despues |
| T-882 | **Charcos más nítidos:** el reflejo sale a resolución completa (antes a la mitad), cada charco tiene un borde mojado que oscurece el asfalto alrededor, las ondas cambian a lo largo del charco (ya no es espejo plano) y el reflejo es más fuerte. | s2o-charcos-* |
| T-883 | **Taller El Canepa a escala real:** 16 × 8 m y 5.4 m de alto (antes 9 × 8 × 4.6), el carro del elevador al centro y el resto contra el fondo. El letrero es el **L15** que hiciste. **Canepa** medía 1.53 m animado (medido en Play con BakeMesh): ahora mide 1.80. | s2o-taller-* |
| T-884 | **Casino:** puerta de cristal que se desliza de lado sola al acercarte (como de tienda) y adentro tus modelos: arco dorado I16 en la entrada, 4 mesas de blackjack I11 con 3 sillas I12 y un candil I13 cada una, barra I14 con 3 bancos I15 y su luz. Se quitaron las mesas de cartas de cubos. Prueba de PlayMode en la ciudad del juego: el jugador llega, el cristal se abre solo y se cierra cuando se va. | s2o-casino-* |
| T-885 | **Puente sin sentido:** eran los dos N02 de la Av. Neón, que entraban de un edificio al de enfrente sin escaleras. Como no hay modelo de pasarela como K07, se quitaron y la pasarela va al pedido de arte. En la calle de Nova no había puente. | s2o-puente-* |
| T-886 | **Cuarto como R10:** de 3 × 4 a 5 × 4 m (la pared izquierda se recorrió 2 m dentro de la casa, que ahí estaba vacía). Con tus muebles: cama y buró con lámpara a la izquierda, pósters, ventana con barrotes y neón al fondo, escritorio con la CRT, ropero junto a la puerta y el foco pelón al centro con sus cables (estaban rotos, en x = 1000). El espejo no se movió. | s2o-cuarto-antes/despues |
| T-887 | **Menú de Tino con el estilo de H10:** acento turquesa de las líneas del HUD en lugar del naranja, paneles de línea fina casi transparentes y la letra Rajdhani del HUD. Reacomodo: la tabla ANTES → DESPUÉS pasa al centro, debajo del holograma (antes se apretaba bajo las piezas); a la derecha quedan las tres piezas con su precio y el botón de mejorar. | s2n-menu-armas-1..6 (nuevas) y s2o-menu-tino-antes-* |
| T-888 | **Callejones de Nova:** eran rendijas de 1.5 m entre las torres. Del lado norte cruzaban a la calle de atrás por un pasillo de un metro y del lado sur terminaban en bolsas sin salida (las vi en el mapa de paso). Ahora las torres van juntas. | s2o-mapa-paso, s2o-nova-* |
| T-889 | **Neón Alto parejo:** entre las torres quedaba con la mitad del brillo de los demás barrios (16 contra 35 a 49 de promedio en las fotos); su volumen sube la exposición y queda en 40. | s2o-luz-zona-calle-despues |

## Decisiones que tomé sin preguntar
- **El APV quedó apagado (T-880).** Era la causa de las zonas negras y quemadas. Para volver a usarlo hay que hornearlo de nuevo con la
  ciudad de hoy (Mecha Cyberpunk/Ambiente/Hornear APV) y prender `AMB_Noche/APV` y su `ProbeVolumePerSceneData`.
- **Cielo liso:** de noche ya no se ven las nubes (domo y capa) y las estrellas quedan tenues. Ajusté las pruebas que pedían lo de antes
  (estrellas fuertes, cielo del color de la niebla, niebla 0.02, saturación negativa).
- **Puerta del casino:** es la hoja de siempre vuelta cristal transparente y corrediza (no hay modelo de puerta de cristal; va en el pedido).
- **Puentes N02:** apagados, no borrados, por si llega la pasarela PS01 y se quieren volver a usar como referencia.
- **P11 y P12 no se pusieron:** llegaron los modelos (los importé con L15 y P10), pero traen la reja o la puerta cerrada en la misma malla.
  Encima de la puerta que sí abre se vería una reja cerrada. Hay que separar la reja de P11 y la puerta de P12 en Blender.
- **Meseras R01:** el zip trae el modelo sin esqueleto; no van hasta que pase por Mixamo.
- **Mochila del cuarto:** quedó en la pared izquierda (una prueba vieja lo pide) y el ropero 40 cm más al fondo para no tapar la entrada.

## Para Tony (Blender / Tripo)
- **Cristal de los postes** C07 y N09 como pieza o material aparte (hoy brilla por una máscara que sale de la malla y la textura).
- **P11:** separar la reja de la puerta (para que abra) o hacerla sin puerta, para el hueco `S2t_Casa/Hueco_Entrada_P11`.
- **P12:** igual con la puerta metálica, para la fachada del consultorio del Doc.
- **R01:** rig de Mixamo para las meseras del casino.
- Pedido nuevo al chat de arte: docs/pedido-arte-08nov-c.md (PS01 pasarela de Nova como K07, PC01 puerta de cristal de dos hojas).

## Pruebas
- **EditMode completo sobre main: 2072 de 2072.**
- **PlayMode de Mundo:** 183 de 194 después de T-880; las 11 que fallan ya fallaban (cámara libre, horarios del mercado negro,
  estacionamiento de la casa). Después de cada tarea corrí las de lo que tocó: taller y puertas, casino (incluida la nueva de la puerta
  automática), casa, cuarto y espejo (incluido el recorrido del prólogo hasta la cama) y el menú de Tino. Ninguna falla nueva.
- **PlayMode de UI: 178 de 202, 15 fallan.** Son del HUD, del carro, del teléfono y de los avisos de puerta; ninguna toca el menú de Tino.
  Seis ya están en el reporte de la sesión 1 (minimapa, título de zona, teléfono y balas del HUD, que dependen del orden). No corrí la
  base de main para las demás: son de lo que hace la sesión 1.

## Para la próxima vez
- `herramientas/unity-vivo.sh iniciar` se niega a arrancar si hay cualquier Unity abierto, aunque sea el de mecha-s1 (otro proyecto).
  Lo arranqué con los mismos pasos sin esa revisión. Convendría que `editor_corriendo` mire el proyecto (tarea de Core).
- Las pruebas de PlayMode de `EscenaLugares` cargan `Ciudad.unity`, no `Ciudad_Prologo` (la del juego): lo nuevo del mundo se prueba
  cargando `Ciudad_Prologo` directo.
- Una corrida de EditMode se cayó una vez por falta de memoria (con el Unity de la sesión 1 abierto); la repetí y pasó.
