# Reporte 23-nov: voces de personajes y videos de las cinemáticas

Ramas: `tony/T-voces-23nov` y `tony/T-cinematicas-23nov`, las dos con merge --no-ff a `main` y push (autorizado por Tony).
Builds (los dos con "Build Finished, Result: Success"):
- `Builds/Demo/CyberCity.exe`, hecho con `ConstruirDemo.ConstruirDemoPandillas`.
- `Builds/Juego/CyberCity.exe`, hecho con `ConstruirDemo.ConstruirJuego`.

## Qué quedó

### Voces (T-voces-personajes-23nov)
- **Archivos:** 239 .wav en `Assets/Arte/Audio/19_Voces/`, todas las carpetas sin `_sin_efecto` y con `Policia/_megafono`.
  - Importados en mono.
  - Jefe_SKULLS, Jefe_Nova y Doc en Vorbis, comprimidos en memoria.
  - Quedan 90 IDs en el CatalogoSonidos.
  - Para repetir la importación: `MechaCyberpunk.Progresion.Editor.AplicarVoces23nov.Aplicar`.
- **Reglas:**
  - Las tomas se eligen al azar sin repetir la última.
  - Cada facción tiene un solo canal de voz, así que no hablan dos a la vez, y hay silencio extra entre plática y burlas.
  - Las del jugador y el Doc suenan en 2D; el resto en 3D.
- **Ojo:** las voces y los subtítulos de los SKULLS vuelven a estar prendidos. `VocesPandilla.VocesSkullsActivas` estaba en false desde T-1655; para callarlos, regrésalo a false.
- **Jugador:**
  - esfuerzo al golpear o empujar;
  - salto;
  - dolor leve o fuerte según el daño, que sustituye a JUG_DANIO;
  - quemado al prenderte una explosión, y tos al apagarse el fuego;
  - muerte, que sustituye a JUG_CAE_DERROTADO;
  - respiración de cansancio en bucle con fundido después de correr 6 s.
- **SKULLS, Matón y Jefe de los SKULLS:** alerta, combate, dolor y muerte.
  - El Matón pega con el brazo (ATAQUE) y se ríe.
  - El jefe usa ENFRENTAMIENTO, DOLOR y DERROTA.
- **Policía a pie:**
  - ALTO cuando empieza a disparar;
  - RINDETE al pegarte;
  - REFUERZOS al cubrirse;
  - DOLOR y MUERTE;
  - OFICIAL CAÍDO cuando cae un compañero a menos de 30 m.
- **Patrulla en persecución:** se turnan el altavoz y el megáfono ("alto" y "ríndete"). Al escaparte suena LO PERDIMOS por el radio. POL_RADIO_DESPACHO ya se había cambiado por POL_RADIO_AVISO en T-1531.
- **Nova:** alerta, orden, "cúbranse" al activar el escudo, lo perdimos, dolor y muerte.
- **Jefe final de Nova:** frase al empezar y al romperle el escudo, ataque en el puñetazo, muerte.
- **Doc:**
  - saludo y despedida al entrar y salir de la clínica;
  - EXPLICAR al instalar un implante;
  - SIÉNTATE y SUERO en la silla;
  - REVIVIR al despertar tras morir.

### Cinemáticas (T-cinematicas-videos-23nov)
- **Archivos:** 17 videos en `Assets/Cinematicas/C01`, `C02` y `C03`, por Git LFS.
  - Cada `DatosCinematica` acepta ahora una lista de clips en orden.
  - Para volver a asignarlos (toma los .mp4 de cada carpeta por nombre): `MechaCyberpunk.Progresion.Recorrido.Editor.VideosCinematicas23nov.Aplicar`.
- **Cómo se reproducen:**
  - Dos VideoPlayer alternados: mientras uno suena, el otro ya prepara el siguiente clip.
  - El último cuadro de un clip se queda en pantalla hasta que el nuevo pinta, para que no haya negro entre clips.
  - Fundido de 0.5 s al entrar y al salir.
  - El audio va por el grupo Efectos.
  - Se saltan con ENTER, como antes.
- **Qué lleva cada una:**

  | Cinemática | Contenido | Duración |
  |---|---|---|
  | C01 EL RECUERDO | T01 + T02 | 9.1 s |
  | C02 EL MALETÍN | C02 + C03 + F08–F11 + F12–F15 (12 clips) | 38.4 s |
  | C03 LA DECISIÓN | G01–G03 y luego 6 s de pantalla pendiente con el texto de C07 | 15 s |
  | C04 EL SUERO | placeholder; C07 y C08 aún no tienen video | — |

- **Intro:** el logo C00 ya era el video de la intro (es el mismo archivo).
- No se tocó el arranque de la historia: eso es VR24.

## EL MALETÍN (paso 4) en Play
Prueba PlayMode `VideosCinematicas23novPruebas`, 2 de 2 pasaron:
- Duración total: **39.5 s**. Son 38.4 s de video más los fundidos.
- **0 cuadros sin imagen** durante el contenido.
- Cada clip duró lo que dura su archivo:
  - el clip 1 dura 2.4 s y el 2 dura 6.0 s;
  - del 3 al 11, 3.0 s cada uno;
  - el 12 marcó 3.5 s porque incluye el fundido de salida.
- Ningún clip se cortó ni se trabó.

Solo probé la cinemática del paso 4, no el paso 4 completo jugado.

Las pruebas EditMode de voces y catálogo pasaron 13 de 13.

## Qué no quedó
- SKU_JEFE_MALETIN, SKU_JEFE_GUARDIAS y SKU_JEFE_CASTIGO están en el catálogo, pero no suenan: no existe la escena del maletín con el jefe.
- NOV_JEFE_REVELACION está en el catálogo, pero no suena: no existe la escena final.
- POL_MEGAFONO_LO_PERDIMOS está cargado, pero no se usa porque, cuando escapas, la patrulla ya está lejos. En su lugar suena LO PERDIMOS por el radio.
- La policía a pie nunca deja de estar en alerta, así que su "lo perdimos" no ocurre.
- C07 y C08 siguen con placeholder porque no tienen videos. Cuando haya clips, se copian a `Assets/Cinematicas/C03` (con `_C07_` en el nombre) o a `Assets/Cinematicas/C04`, y se corre el método de arriba.
- **Si las voces se oyen bajas:** la ganancia de Voces sigue en 0.85 contra 1.0 de Efectos; se ajusta en `CatalogoSonidos.MezclaVoces`.
- **VR:** la rama `vr/quest3` tiene su propia `CinematicaPendiente` (con la pantalla grande de VR15). Al juntarla con main hay que pasar los dos VideoPlayer a esa pantalla.

## Qué hacer jugando para oír cada grupo
1. **Jugador:**
   - golpea y salta;
   - corre más de 6 s con Shift y para;
   - deja que te peguen y que te disparen;
   - acércate a un carro que explota, y oirás quemado y luego tos;
   - muere.
2. **SKULLS:** acércate sin que te vean (plática), deja que te provoquen, pelea, mata a uno y huye.
3. **Matón** (el SKULL grandote del brazo robótico): deja que te provoque y te pegue, y mátalo.
4. **Jefe de los SKULLS:** entra a su zona, pelea y véncelo.
5. **Policía:**
   - sube las estrellas y deja que se bajen de la patrulla (alto, refuerzos, ríndete);
   - mata a un policía junto a otro (oficial caído);
   - oye el altavoz en la persecución;
   - escápate (lo perdimos por el radio).
6. **Nova:** provócalos (alerta, orden), oye "cúbranse" cuando sacan el escudo, y pégales hasta matarlos.
7. **Jefe final de Nova:** empieza la pelea, rómpele el escudo, deja que te pegue y véncelo.
8. **Doc:** entra y sal de la clínica, instala un implante, siéntate en la silla y muere en la calle para despertar con él.
9. **Cinemáticas:** empieza una partida nueva y fíjate en que C01 tiene video, en que C02 EL MALETÍN sale completa sin negro y en que C03 tiene video y luego la parte pendiente. Sáltalas con ENTER.
