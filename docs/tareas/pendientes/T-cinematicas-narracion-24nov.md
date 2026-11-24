# T-cinematicas-narracion-24nov: narración del protagonista sobre las cinemáticas

## Qué es
El usuario grabó su narración (voz en off, en primera persona) para las cinemáticas C02–C07. Hay que reproducir cada frase sobre su video y **no apresurar nada**: si la frase dura más que el video, el último cuadro se queda congelado hasta que termine la frase, después **1 s de pausa** y entonces pasa al siguiente video.
Todo lo necesario está en `docs/cinematicas-narracion/` (este flujo, el guion y la tabla de tiempos). Complementa la tarea ya hecha `T-cinematicas-videos-23nov.md` (que reproduce los videos sin voz).

## Paso 0 (obligatorio)
Cerrar Unity y Unity Hub, confirmar que no queda ningún Unity.exe, borrar `Temp/UnityLockfile` y repetir si el candado vuelve a aparecer.

## Dónde están las cosas
- **Videos (nueva numeración 01–27):** `C:\Users\Angel Perez\Desktop\videojuego\trailer-cybercity-referencias\Videos_Cinematicas\<CXX_nombre>\` (una carpeta por cinemática). Ya hay video de C07, C08 y C09: quitar sus placeholders.
- **Narración:** `3d sonidos/19_Voces/Narracion/NAR_*.wav` → copiar a `Assets/Arte/Audio/19_Voces/Narracion/` (2D, grupo de mezcla **Voces**). No copiar `_sin_efecto`.
- **Guion:** `docs/guion-narracion-cinematicas.md`.

## Regla de tiempo (lo más importante)
Para cada video de la tabla, en orden:
1. Empieza el video y, al mismo tiempo, su frase de narración.
2. Si el video termina antes que la frase: **congelar el último cuadro** (pausar el VideoPlayer en el último frame, sin negro) hasta que termine la frase.
3. Si la frase termina antes que el video: dejar que el video termine normal.
4. Al terminar los dos: **1 s de pausa** en el último cuadro y luego el siguiente video, sin cortes negros (precargar con los dos VideoPlayer alternados que ya existen).
5. Nunca acelerar el video ni cortar la voz. Mientras suena la narración, la música (MUS_PIANO_INTRO, ver `T-musica-24nov.md`) baja unos 10 dB y los efectos del video unos 6 dB.
6. Saltar (ENTER / B en VR) salta toda la cinemática y corta la voz con un fade de 0.2 s.

Implementarlo como dato: en cada `DatosCinematica`, junto a cada VideoClip de la lista, un AudioClip opcional de narración y la pausa (1 s por defecto). Sin narración, el clip se comporta como hoy.

## Tabla de tiempos (C02–C07)
| # | Video | Dura video | Narración | Dura voz | ¿Congelar? | Tramo total (+1 s) | Frase |
|---|---|---|---|---|---|---|---|
| 04 | `C02_Maletin_A_Llegan/04_C02_Maletin_llegan.mp4` | 2.4 s | `NAR_C02.wav` | 4.3 s | congelar 1.9 s | 5.3 s | Los Skulls llegaron como siempre… como si la calle fuera suya. |
| 05 | `C03_Maletin_B_Lo_roba/05_C03_Lo_roba.mp4` | 6.1 s | `NAR_C03.wav` | 9.0 s | congelar 2.9 s | 10.0 s | Dejaron un maletín de Nova a un lado, para ir a asaltar al de los tacos. Nadie lo estaba viendo… nadie, más que yo. |
| 06 | `C04_Huida/06_C04_F08_Se_escabulle.mp4` | 3.0 s | `NAR_C04_F08.wav` | 2.8 s | no | 4.0 s | No lo pensé. Lo tomé y corrí. |
| 07 | `C04_Huida/07_C04_F09_Camara_huida.mp4` | 3.0 s | `NAR_C04_F09.wav` | 2.4 s | no | 4.0 s | Creí que nadie me había visto… |
| 08 | `C04_Huida/08_C04_F10_Barril_vacio.mp4` | 3.0 s | `NAR_C04_F10.wav` | 2.5 s | no | 4.0 s | Cuando voltearon, ya no estaba. |
| 09 | `C04_Huida/09_C04_F11_Lo_buscan.mp4` | 3.0 s | `NAR_C04_F11.wav` | 2.4 s | no | 4.0 s | Y se volvieron locos buscándolo. |
| 10 | `C05_Cuarto_Contenido/10_C05_F12_Cuarto_llega.mp4` | 3.0 s | `NAR_C05_F12.wav` | 1.4 s | no | 4.0 s | Me encerré en mi cuarto. |
| 11 | `C05_Cuarto_Contenido/11_C05_F12B_Maletin_en_mesa.mp4` | 3.0 s | `NAR_C05_F12B.wav` | 2.9 s | no | 4.0 s | Era de Nova. No debía tenerlo. |
| 12 | `C05_Cuarto_Contenido/12_C05_F12C_Pulgares_huella.mp4` | 3.0 s | `NAR_C05_F12C.wav` | 2.5 s | no | 4.0 s | Pero se abrió con mis huellas. |
| 13 | `C05_Cuarto_Contenido/13_C05_F13_Abre_maletin.mp4` | 3.0 s | `NAR_C05_F13.wav` | 3.0 s | no | 4.0 s | Adentro había algo que lo cambiaba todo. |
| 14 | `C05_Cuarto_Contenido/14_C05_F14_Contenido.mp4` | 3.0 s | `NAR_C05_F14.wav` | 4.3 s | congelar 1.3 s | 5.3 s | Veinte mil biits, una llave… y un suero. |
| 15 | `C05_Cuarto_Contenido/15_C05_F15_Ventana_Skulls.mp4` | 3.0 s | `NAR_C05_F15.wav` | 7.6 s | congelar 4.6 s | 8.6 s | Y por la ventana los oí: esa noche nadie iba a cuidar la ranura del muro. Era mi oportunidad de salir. |
| 16 | `C06_Ranura_A_El_grito/16_C06_G01_Guardias_se_van.mp4` | 3.0 s | `NAR_C06_G01.wav` | 2.3 s | no | 4.0 s | Los guardias se fueron a buscarme. |
| 17 | `C06_Ranura_A_El_grito/17_C06_G02_Sale_de_casa.mp4` | 3.0 s | `NAR_C06_G02.wav` | 2.7 s | no | 4.0 s | Agarré el maletín y salí. |
| 18 | `C06_Ranura_A_El_grito/18_C06_G03_En_la_ranura_voltea.mp4` | 3.0 s | `NAR_C06_G03.wav` | 4.3 s | congelar 1.3 s | 5.3 s | Estaba a un paso de salir… y entonces los oí. |
| 19 | `C07_Ranura_B_Regresa/19_C07_G04_Lo_que_ve.mp4` | 3.0 s | `NAR_C07_G04.wav` | 4.1 s | congelar 1.1 s | 5.1 s | Estaban golpeando a mi gente… por algo que hice yo. |
| 20 | `C07_Ranura_B_Regresa/20_C07_G05_Duda.mp4` | 3.0 s | `NAR_C07_G05.wav` | 3.4 s | congelar 0.4 s | 4.4 s | Podía irme. Nadie me lo iba a reprochar. |
| 21 | `C07_Ranura_B_Regresa/21_C07_G06_Regresa.mp4` | 3.0 s | `NAR_C07_G06.wav` | 5.1 s | congelar 2.1 s | 6.1 s | Pero no así. Si me iba, me iba con ellos. |

Total aproximado de C02–C07 con la regla: **90 s** (antes, solo videos: 56 s).

## Cinemáticas sin narración
| Cinemática | Videos | Voz |
|---|---|---|
| C00 Logo | 01 | ninguna |
| C01 Recuerdo del campo | 02–03 | la que ya trae el video |
| C08 Suero | 22–24 | las dos frases del Doc ("Con esto tu cuerpo aguanta cualquier implante." / "Sin esto, te mata."), si ya vienen en el video. Si el video no las trae, dejar el hueco y anotarlo en la revisión |
| C09 Tino / mejora de la pistola | 25–27 | ninguna |

Entre estos clips no hay pausa: se unen sin negro, como ya funciona hoy.

## Qué cinemática del juego usa qué videos
- C02 EL MALETÍN: videos 04–15.
- C03 LA DECISIÓN: videos 16–21 (ya hay C07: quitar placeholder).
- C04 EL SUERO: videos 22–24 (ya hay video: quitar placeholder).
- Tino (mejora de la pistola): videos 25–27, al poner la primera mejora.
- Intro: 01 (logo) y 02–03 (recuerdo).

## Cómo comprobarlo
Solo las pruebas necesarias (que cada DatosCinematica tenga su par video + narración). Lo importante es que el usuario lo vea jugando: partida nueva → C02 EL MALETÍN completo → C03 LA DECISIÓN. Debe oírse cada frase completa sobre su video, con el cuadro congelado donde haga falta, 1 s de respiro entre frases y sin negros. Anotar en la revisión cuánto duró cada cinemática.
