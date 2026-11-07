# T-715 Los guardias de Nova hacen daño de verdad
Dueño: Tony (S1) | Rama: tony/T-715-guardias-nova | Modelo: Sonnet 5.5
Depende de: T-533, T-624, T-693

## Objetivo
Revisar si los robots GuardiaNova (seguridad de Neón Alto) le hacen daño al jugador. No lo hacían: solo jugaban la animación de amenaza (Nova_Punch y Nova_Barrido) sin tocar la vida. Ahora pegan con daño real y aviso visible antes del golpe.

## Qué se hizo
- ReglaGolpeGuardia (nueva, pura): AVISO (0.6 s) - IMPACTO - RECARGA. Se cancela si el jugador se va o se acaba la alerta durante el aviso; cada tercer golpe es la barrida.
- GuardiaNova: en alerta (oyó un disparo o viste un arma) y con el jugador a 2.2 m, se voltea a verlo, arranca la animación, prende una luz roja sobre el pecho que sube durante el aviso y suena el aviso; al llegar el impacto, si el jugador sigue a 2.6 m o menos (y al frente, 110 grados; la barrida pega alrededor y 0.5 m más lejos) le hace daño por IRecibeDanio (TipoDanio.Golpe): 12 el puñetazo, 20 la barrida. Alejarse a tiempo o rodar (invulnerabilidad de VidaAPie) lo esquiva.
- Sin alerta los guardias siguen sin pegar (patrullan, vigilan y presumen como antes).

## Criterios
- [x] Con alerta y el jugador cerca, el guardia avisa con luz roja y no hace daño durante el aviso
- [x] Al terminar el aviso (más de 0.45 s después) la vida del jugador baja 12
- [x] Alejarse durante el aviso esquiva el golpe
- [x] El tercer golpe es la barrida: 20
- [x] Los clips de T-624 siguen (EnemigosAnim2Pruebas 6/6)

## Pruebas
- EditMode PersonajeVisual: ReglaGolpeGuardiaT715Pruebas (5)
- PlayMode Personaje: GuardiaNovaDanioT715Pruebas (3), con el jugador real; captura docs/capturas/s1c-T-715-aviso.png

## Fuera de alcance
- Que el guardia dispare (solo pega); que el jugador le haga daño (no es IRecibeDanio)
