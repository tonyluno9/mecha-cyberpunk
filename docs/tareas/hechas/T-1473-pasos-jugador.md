# T-1473 Pasos por tipo de piso y sonidos del jugador

Rama: tony/T-1473-pasos-jugador. Sin compilar ni correr pruebas (el worktree no tiene Library): lo compila el orquestador.

## Piso -> ID (ReglaSuperficiePaso, Personaje/Movimiento)
El rayo corto hacia abajo junta material físico, material del renderer, collider y objeto raíz, y busca palabras clave (en este orden).

| Orden | Piso | ID | Palabras |
| --- | --- | --- | --- |
| 1 | Interior (casa, taller, bar, clandestino, casino, mercado negro, clínica) | PAS_MADERA | por el lugar |
| 2 | Metal | PAS_METAL | carro (IMontable), metal, rejilla, reja, acero, lamina, fierro, hierro, contenedor, pu08, techobodega... |
| 3 | Mojado | PAS_MOJADO | mojad, charco, agua, wet, lluvia... |
| 4 | Madera | PAS_MADERA | madera, muelle, tablon, tabla, wood, plank, parquet, duela |
| 5 | Grava | PAS_GRAVA | grava, obra, escombro, balasto |
| 6 | Tierra | PAS_TIERRA | tierra, baldio, lote, deshuesadero, lodo, terreno |
| 7 | Concreto (por defecto) | PAS_CONCRETO | asfalto, banqueta, azotea; con lluvia (parámetro llueve) PAS_MOJADO |

Las variantes (PAS_TIERRA_01.._NN) las elige el catálogo. Tiempo mínimo entre pasos: 0.25 s (LimitadorPasos). Los pasos ya son por zancada, correr cae más seguido.

## Evento -> ID
| Evento | ID | Dónde |
| --- | --- | --- |
| Aterrizar tras caer más de 1.2 m | JUG_ATERRIZAJE (3D, como el JUG_CAER anterior) | ControladorAPie |
| Rodar agachado, o rodar tras caída alta | JUG_RODADA (antes JUG_SALTO) | CubiertaYRodada, CaidaJugador |
| Agarrar un borde o enganchar una escalera/tubo | JUG_TREPAR | TreparAPie |
| Golpe conecta | MEL_GOLPE_CARNE (3D en el punto, antes PUN_GOLPE_CARNE) | GolpeAPie |
| Golpe al aire | MEL_GOLPE_AIRE (antes PUN_GOLPE_AIRE) | GolpeAPie |

## Falta
- Pisos sin sonido: arena, pasto, loseta, alfombra, vidrio (hoy caen en concreto).
- Agachado más bajo y correr más fuerte: ISonidos no tiene volumen por llamada; va en la política de mezcla de T-1471 si se quiere.
- Lluvia: la regla acepta `llueve` pero ControladorAPie aún no lee el clima (solo suena mojado por nombre).
- MEL_GOLPE (sin sufijo) no se pide: el golpe usa CARNE/AIRE.
- El PlayMode de pasos usa ahora PAS_*; el catálogo de producción necesita PAS_* encendidos (T-1471).
