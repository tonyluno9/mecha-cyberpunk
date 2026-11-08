# T-735 PJ01: capa de daño aditiva (A5)
Dueño: Tony | Rama: tony/T-735-pj01-dano | Modelo: Sonnet 5.5

## Objetivo
Que el jugador se estremezca al recibir daño con Pistola_Dano_Frente, Espalda y Fuerte (14 cuadros, 0.47 s, empiezan y terminan en Pistola_Apuntar) sin perder
la pose: sigue apuntando y disparando mientras se estremece.

## Qué se hizo
- Capa nueva **Dano** del controlador base (índice 2): modo **Aditivo**, con la máscara del torso (la misma de Arriba), peso 1. Los tres clips son aditivos con su
  primer cuadro como pose de referencia (se escribe en su .meta con `ImportadorMixamo.AplicarAditivoDano`): lo que suman es solo el estremecimiento.
- Se quitaron DanoFrente, DanoEspalda y DanoFuerte de la capa Arriba (con los clips viejos Dano_*): ahí reemplazaban a la pose de apuntar durante medio segundo.
- Estados de la capa: Vacio, DanoFrente, DanoEspalda, DanoFuerte. Se entra desde cualquier estado con el trigger Herido y DanoTipo; el mismo golpe otra vez
  reinicia el estremecimiento (una sola reacción a la vez) y se sale a Vacio al 95 % del clip. Rapidez 1 (los clips ya son cortos).
- VisualPersonaje: el peso de la capa Dano es 1 y se apaga solo caído (manda la muerte) o manejando (manda el volante). `PesoCapaDano`.

## Dirección y umbral
- **Frente o espalda:** según el golpe contra el frente del personaje (`ReglasAnimacion.ElegirDano`): si la dirección del golpe (del atacante al objetivo) va con su
  frente (coseno > 0.3) le pegaron por la **espalda**; si no (de frente, de lado o sin dirección) es de **frente**.
- **Fuerte:** un golpe de **25 % de la vida máxima o más** (`ReglasAnimacion.FraccionDanoFuerte = 0.25`, usado por `AnimacionJugador`) es Pistola_Dano_Fuerte sin importar la
  dirección. Con 24 % todavía es normal. Los enemigos no mandan fuerza (siempre de frente o de espalda).

## Cómo encaja con T-675 (reacción corta)
- La reacción ya no se corta a la mitad del clip: los Pistola_Dano_* son cortos (0.47 s) y están diseñados para terminar en la pose de apuntar; se dejan completos.
- `ReaccionEnCurso` y `CortarReaccion()` miran ahora la capa Dano. Un esquive (rodar) la corta; un golpe de puños también (`Golpear` llama a `CortarReaccion`), porque
  antes lo cortaba el cambio de estado en la misma capa. Así no hay dos reacciones encimadas (estremecimiento y golpe, ni dos estremecimientos).

## Aliados y enemigos
Comparten el controlador: todos se estremecen con los mismos clips encima de su pose, sin perder el apuntado. Probado con woman2, man, SkullHombre, GuardiaNova, Doctor
y RobotHombre. Visual_Policia usa su propio controlador y no cambia.

## Criterios
- [x] Frente o espalda según de dónde viene el golpe (por el evento de impacto real)
- [x] Fuerte con 25 % de la vida o más; 24 % no
- [x] Sigue apuntando (Arriba en PistolaApuntar) y disparando (la bala sale) mientras se estremece
- [x] Un golpe de puños o un esquive cortan la reacción; muchos golpes seguidos no dejan nada trabado
- [x] Caído la capa se apaga
- [x] Aliados y enemigos no se rompen

## Pruebas
EditMode PersonajeVisual: CapaDanoT735Pruebas (clips, aditivo, umbral, la capa Arriba sin estados de daño). PlayMode Personaje: DanoPJ01T735Pruebas (7).
Capturas: docs/capturas/pj01-dano-frente.png, pj01-dano-espalda.png, pj01-dano-fuerte.png, pj01-dano-apuntando.png.
