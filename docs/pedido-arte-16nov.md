# Pedido al chat de arte — 16-nov noche (CYBERCITY)

Mismo flujo: prompts + script de PowerShell para WaveSpeed (Nano Banana 2.1, 1K, una imagen por prompt). Estilo del juego: usa como
referencia `imageassets/referencias/look/K01_Barrio_HUDnuevo.png` y `H10_HUD.png` (ilustrado, contornos finos, noche azul con neón
turquesa/magenta y sodio naranja; HUD de líneas finas turquesa con acento amarillo, textos en español). Nada copiado de otros juegos.

## 1. Modelos para Tripo (fondo blanco liso, vista 3/4, objeto completo, iluminación pareja) → imageassets/modelos-nuevos/
| Archivo | Qué es |
| --- | --- |
| MA01_Maletin.png | Maletín metálico de dinero de los asaltantes: aluminio rayado, cerraduras, una calcomanía de calavera de los SKULLS, cerrado. |
| TR01_Tragamonedas.png | Máquina tragamonedas del casino VivaCoin: gabinete dorado y magenta, pantalla con símbolos abstractos, palanca, luces en el borde. |
| TR02_Tragamonedas.png | Segunda variante: gabinete negro y turquesa, pantalla curva, botones grandes. |

## 2. Pantalla de carga tipo GTA → imageassets/ui/carga/
Ilustraciones grandes del juego que se van a animar (zoom lento y paneo) durante la carga. 16:9, sin HUD, sin texto, mismo estilo de K01,
con el protagonista PJ01 (`imageassets/modelos-nuevos/PJ01_Protagonista_v2.png`) como personaje:
| Archivo | Escena |
| --- | --- |
| L01_Carga_Protagonista.png | El protagonista de cerca con la pistola, en la calle de noche, con luz de neón en la cara. |
| L02_Carga_Persecucion.png | Persecución: el carro del jugador tras la camioneta de los SKULLS, chispas y patrulla atrás. |
| L03_Carga_Doc.png | El Doc en su clínica I+D+I con el brazo robótico sobre la silla. |
| L04_Carga_Casino.png | La entrada del casino VivaCoin con meseras robot y luces. |
| L05_Carga_Nova.png | La torre de Nova de noche con guardias y drones. |
| L06_Carga_Techos.png | El protagonista en una azotea viendo la ciudad, tinacos y cables. |

## 3. Mockups de pantallas (16:9, estilo HUD H10) → imageassets/referencias/menus/
| Archivo | Qué es |
| --- | --- |
| M33_Arrestado.png | Pantalla ARRESTADO: el jugador esposado contra una patrulla con luces roja/azul, la imagen desaturada, texto grande ARRESTADO, multa "-25 %  $ 3,112" y "Vuelves a casa". |
| M34_HasMuerto.png | Pantalla HAS MUERTO: el jugador caído en la calle, viñeta roja, glitch, texto grande HAS MUERTO y "Despiertas en la clínica del Doc". |

## 4. Íconos → imageassets/ui/menus/
| Archivo | Qué es |
| --- | --- |
| H42_IconosProtesisAtaque.png | Íconos de línea fina turquesa (como H41) del brazo láser y de la hombrera de misiles, para la barra de prótesis. Fondo negro. |

## 5. Referencias de calles (para rehacer las calles que se ven raras) → imageassets/referencias/look/
Mismo estilo de K01/K05, sin HUD, de noche:
| Archivo | Qué es |
| --- | --- |
| K08_CalleSuburbios_Cuadra.png | Una cuadra completa de Suburbios vista desde la banqueta: casas de 2 pisos pegadas, tiendita, taller, lote baldío con barda, postes. |
| K09_Plaza_Barrio.png | Una plaza chica de barrio entre cuadras: kiosco o fuente, bancas, puestos, árboles, casas alrededor. |
| K10_CalleMercado.png | Calle comercial entre El Mercado y Neón Alto: locales con cortinas, letreros, más edificios de 3-4 pisos. |

## 6. Opcional: letreros para edificios → imageassets/modelos-nuevos/
LT01–LT04: letreros de neón grandes para fachadas (taquería, farmacia 24 h, hotel, anuncio de VivaCoin), de frente, fondo blanco.
(Este SÍ lleva texto; que diga exactamente lo pedido.)
