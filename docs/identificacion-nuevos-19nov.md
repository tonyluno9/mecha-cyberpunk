# Identificación de los zips nuevos-19nov (T-1512 parte A)

Carpeta de origen: `imageassets/modelos-nuevos/nuevos-19nov/`. Cada SIN_NOMBRE se renderizó con Blender (vista 3/4, con su textura) y se
comparó a ojo contra los PNG de referencia ED03..ED12 y MU01..MU03 (_a y _b). Renders en `docs/capturas/_20nov/identificar/`.
Cada zip se copió con su nombre correcto en la misma carpeta (los SIN_NOMBRE se dejaron). ED01_a y ED02_a ya traían nombre.

| Zip original | Código y variante | Confianza | Justificación |
| --- | --- | --- | --- |
| ED01_CasaSuburbios_a | ED01_CasaSuburbios_a | con nombre | |
| ED02_TienditaEsquina_a | ED02_TienditaEsquina_a | con nombre | |
| SIN_NOMBRE_01 | ED07_HotelNeon_a | alta | Torre angosta de 5 pisos, gris azulado, letrero neón magenta vertical y marquesina crema en la base. La b es verde oscuro con marquesina rosa. |
| SIN_NOMBRE_02 | ED12_Estacionamiento_a | alta | Rampas y barandales turquesa y naranja, torrecillas en las esquinas. La b es beige con rampa grande. |
| SIN_NOMBRE_03 | MU01_MuroPerimetral_b | media-alta | Muro con barreras de concreto tipo jersey en la base, tuberías y alambre arriba. La a es un muro liso con paneles y franjas amarillas. |
| SIN_NOMBRE_04 | ED10_Bodega_a | alta | Bodega de techo a dos aguas de lámina oxidada, cortina a la izquierda y puerta a la derecha. La b tiene una ampliación pegada. |
| SIN_NOMBRE_05 | MU02_TorreControl_a | media-alta | Fuste liso con escalera a un lado, base ancha y plataforma con barandal. La b tiene la base inclinada con cajas laterales. |
| SIN_NOMBRE_06 | ED08_OficinaNova_a | alta | Caja blanca de oficinas con ventanas oscuras, sin retranqueos. |
| SIN_NOMBRE_07 | ED03_EdificioDeptos_b | alta | Verde con filos naranjas, escalera exterior, balcones y cuarto de azotea retranqueado con tinaco. |
| SIN_NOMBRE_08 | ED08_OficinaNova_b | alta | Oficina blanca escalonada, piso superior retranqueado. |
| SIN_NOMBRE_09 | ED04_LocalesMercado_b | media | Tres locales con toldos rosa, amarillo y turquesa, cartel cian a la izquierda y cables en la azotea (la b); la a no tiene carteles. |
| SIN_NOMBRE_10 | ED11_EdificioAbandonado_a | alta | Tres pisos con ventanas entabladas y grafitis a ambos lados de la planta baja, caja plana. La b tiene un volumen saliente y tanque grande. |
| SIN_NOMBRE_11 | MU01_MuroPerimetral_a | alta | Muro liso con rollos de alambre, paneles y franjas amarillas en el pie. |
| SIN_NOMBRE_12 | ED05_EdificioMercado_a | alta | Ladrillo naranja, toldo turquesa, antenas en la azotea y letrero rosa a la derecha. |
| SIN_NOMBRE_13 | ED09_PlantaBajaNova_b | alta | Blanco con ventanales altos y esquina achaflanada. La a es más cúbica con marquesina. |
| SIN_NOMBRE_14 | MU03_PuertaMuro_a | media-alta | Dos pilares casi simétricos con hoja de puerta de refuerzos en X. La b tiene pilares escalonados. |
| SIN_NOMBRE_15 | ED06_EdificioNeon_a | alta | Turquesa con ventanas naranjas, balcones y letreros magenta y cian. La b es marrón. |

Sin modelo todavía: ED01_b, ED02_b, ED03_a, ED04_a, ED05_b, ED06_b, ED07_b, ED09_a, ED10_b, ED11_b, ED12_b, MU02_b, MU03_b.

Importación: `Assets/Arte/Zonas/ED/<Codigo>_<v>/` y `Assets/Arte/Zonas/MU/<Codigo>_<v>/` con `<Codigo>_<v>.fbx`, `_basecolor.jpg`
y, solo en ED01 y ED02, `_normal.jpg` y `_metalsuave.png` (los demás zips traían solo el color). Los materiales y prefabs los crea
`MechaCyberpunk.Arte.Editor.ImportarEdificiosT1512.Aplicar` con la receta de T-1382.
