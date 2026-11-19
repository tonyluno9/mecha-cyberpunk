# Pedido al chat de arte — 13-nov noche (CYBERCITY): rueda de armas, habilidades, tienda, armas, prótesis y enemigos nuevos

Mismo flujo de siempre: prompts + script de PowerShell para WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt).
Estilo: el del juego. Para pantallas, usa como referencia `imageassets/referencias/look/H10_HUD.png` y `K01_Barrio_HUDnuevo.png`
(HUD de líneas finas, esquinas cortadas, turquesa con acento amarillo, letra delgada, textos en español). Para modelos, el estilo de
las armas W01-W04 y de los personajes que ya existen. Nada copiado de otros juegos.

## 1. Mockups de interfaz (16:9, sobre la calle de K01) → imageassets/referencias/menus/
| Archivo | Qué es |
| --- | --- |
| M30_RuedaArmas.png | Rueda radial al centro de la pantalla (se abre manteniendo Tab, el fondo se oscurece y se ve en cámara lenta): 8 casillas en círculo con las armas (pistola, escopeta, fusil, francotirador, puños) y objetos (bebida, papitas, venda), la elegida resaltada en amarillo con su nombre y balas al centro. |
| M31_HabilidadesProtesis.png | El HUD de H10 con una barra nueva abajo al centro: 3 casillas de prótesis con su tecla [1] PIERNAS, [2] BRAZOS, [3] CABEZA, cada una con ícono, barra de energía/recarga y una activa brillando. |
| M32_TiendaArmas.png | Tienda de armas del Mercado: lista de armas a la izquierda con precio; a la derecha la imagen GRANDE del arma elegida (holograma), sus estadísticas (daño, cadencia, cargador, alcance) y botón COMPRAR. |

## 2. Armas nuevas para Tripo (vista 3/4 de lado, fondo blanco, arma completa, sin manos) → imageassets/armas/
| Archivo | Qué es |
| --- | --- |
| W05_Subfusil.png | Subfusil compacto de ráfaga, cargador largo, culata plegable. |
| W06_Revolver.png | Revólver pesado de gran calibre, cañón largo, tambor con luz turquesa. |
| W07_RiflePlasma.png | Rifle de energía/plasma: bobinas en el cañón, celda de energía que brilla. |
| W08_Lanzagranadas.png | Lanzagranadas de tambor rotatorio, robusto. |

## 3. Prótesis de ataque para Tripo (solo la pieza, de frente y 3/4, fondo blanco) → imageassets/modelos-nuevos/
| Archivo | Qué es |
| --- | --- |
| IMP04_BrazoLaser.png | Antebrazo cibernético que se abre y deja salir un cañón láser; para el brazo derecho. |
| IMP05_HombreraMisiles.png | Hombrera con un lanzador de 4 mini misiles que sale por encima del hombro. |

## 4. Enemigos nuevos (más poderosos) para Tripo (T-pose estricta, de frente, cuerpo completo, fondo blanco) → imageassets/modelos-nuevos/
| Archivo | Qué es |
| --- | --- |
| E10_PandilleroSnakes.png | Pandillero de los SNAKES (pandilla rival de los SKULLS): chamarra con escamas, tatuajes de serpiente, máscara de tela, verde y negro. |
| E11_JefeSnakes.png | Jefe de los SNAKES: grande, brazo cibernético con cuchilla, abrigo largo. Mini jefe. |
| E12_GuardiaPesadoNova.png | Guardia pesado de Nova con armadura completa blanca y azul, casco cerrado, escudo antidisturbios. |
| E13_DronAsalto.png | Dron de asalto de Nova (no T-pose; vista 3/4): 4 rotores, ametralladora abajo y luz roja. Hélices separadas visualmente. |
| E14_JefeFinalNova.png | Jefe final: el director de seguridad de Nova en un exotraje pesado (3 m), brazos con cañón y escudo de energía en el pecho. |

## 5. Después (cuando estén las armas)
Hologramas de lado H05–H08 de las armas nuevas, como H01–H04.
