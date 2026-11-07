# T-688 Pruebas en rojo en main
Dueño: Tony | Rama: tony/T-688-pruebas-rojas | Modelo: Sonnet 5.5, medium

## Objetivo
Encontrar la causa real de cada prueba en rojo en main (06-nov noche), arreglar las que rompió una tarea de la noche (T-670 a T-687) o son intermitentes, y anotar las de la sesión 2.

## Lista (causa y estado)
| Prueba | Causa | Estado |
| --- | --- | --- |
| HudAPiePruebas.Vida_TrasDanioDe25_Dice75YBarraAl75 y Jugador_SeRegistraDespuesDelHud | NO era el escudo de T-684 (las pruebas ya lo ponen en 0). La vida máxima es 150 desde T-585 (4-nov) y la prueba esperaba "VIDA 75". | Arregladas: usan vida.Maxima. Verde (HudAPie + Escudo 15/15). |
| ArmasMercadoPruebas.RecorridoFusilYSniper | T-685 (expendedoras) puso una segunda PantallaTienda ("Expendedoras") y FindFirstObjectByType devolvía esa, no la de El Mercado. Bisect: verde en d980c79 (T-684), roja en 4cc5903 (T-685). | Arreglada (la prueba quita la raíz Expendedoras). Verde. |
| RecorridoNombresPruebas (UI) | Misma causa (T-685). | Arreglada: elige la pantalla que no es de las expendedoras. Verde. |
| PersonajesApoyoPruebas.RecorridoProfes, RecorridoRonda2Pruebas.MercadoTianguisEscopeta | Misma causa de pantalla (preventivo) / RecorridoProfes además usaba el daño sin quitar el escudo (T-684). | Pantalla y escudo arreglados. Siguen rojas por S2: ver abajo. |
| HistoriaDemoPruebas.RecorridoDocInventorHacker | Dos causas: (1) el escudo de T-684 absorbía parte del daño de "vida 50" (arreglada: Escudo.Iniciar(0)); (2) el Doc dice ahora "Toma, mijo, va por la casa: te puse un exo de piernas..." (S2: silla/implantes del Doc), la prueba espera "Aguanta, mijo". | (1) arreglada; (2) ROJA, es del Doc (sesión 2), no se toca. |
| PersonajesApoyoPruebas.RecorridoProfes | Idem (2): el Doc dice "Toma, mijo, va por la casa..." y no "Siéntate en la silla, mijo". | ROJA, Doc (sesión 2). |
| RecorridoRonda2Pruebas.MercadoTianguisEscopeta | "El ambiente arranca apagado": el ambiente de la ciudad arranca en 0.8 (Mundo, S2). | ROJA, Mundo (sesión 2). |
| EscalaJugadorT670Pruebas.Personajes_NPCyJugador_QuedanParejosContraLos180 | La sesión 2 agrandó al Doc a propósito (T-812): 1.98 m. | Arreglada: excepción documentada, Visual_Doctor relativa 1.10. Verde. |
| RobarCarroYEntregarlo, RecorridoHastaContinuara | Intermitentes: una sola E antes de que el carro esté al alcance, y una espera real de 5 s al salir de la casa con la máquina cargada. | Hechas deterministas (E repetida hasta que sube; espera 20 s). Verdes en 2 corridas. |
| RecorridoAPiePruebas.Recorrido_DeLaEsquinaAlBarYAlCarro_SiguiendoSoloLaFlecha | Roja ya en aef6081 (inicio de la noche, antes de T-670): no llega a menos de 0.4 m de la entrada del Bar. Es de Mundo (Bar); no la rompió la noche. | ROJA, anotada. |
| Vehiculos: MontableCarroPruebas.Bajar_ConMurosEnLosDosLados_NoPuedeBajar, Camara_PrimeraPersona_MiraHastaElLimiteYVuelveAlFrenteTrasLaEspera, RecorridoCarroPruebas.Recorrido_DeLaCasaAlClandestino_... | Rojas ya en aef6081 y en main sin T-687 (las dos primeras desde antes de T-670); no las rompe T-687. No investigadas a fondo. | ROJAS, anotadas (previas a la noche). |
| CarrosS2nPruebas.Importador_SinZip_NoHaceNadaYAnotaQueFalta | La prueba dice "V09: sin zip no se crea su carpeta" pero la carpeta de V09 ya existe en el repo (T-850 metió V09/V10 en el tráfico). Sesión 2. | ROJA, sesión 2, no se toca. |

## Pruebas
Corridas por grupos con -testFilter: Vehiculos 71 (64 ok, 3 rojas previas), Personaje (T-687, armas, carro) 22/22, HudAPie+Escudo 15/15, RecorridoNombres 1/1, Escala+ArmasMercado ok, Encargos.Robar+Continuara 2/2.
