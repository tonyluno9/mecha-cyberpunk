# T-1006: patrullas que se quedan raras o atoradas

Rama: tony/T-1006-patrullas-atoradas. Área: Trafico.

## Causa
- El conductor solo detectaba atasco con la patrulla casi quieta (2.5 s). Empujando despacio contra algo, o girando sin avanzar, nunca daba reversa.
- Tras varias reversas en el mismo lugar no había salida: seguía intentándolo para siempre.
- Volcada y a la vista de la cámara se quedaba boca abajo para siempre (solo se quitaba si nadie la veía).
- El volante seguía al objetivo sin límite y podía temblar de un lado a otro.

## Qué se hizo
- `DetectorAtasco` (lógica pura): mide el avance en ventanas mientras pide avanzar; poco avance pide reversa; varias reversas o mucho tiempo en el mismo lugar piden recolocar.
- `PatrullaMundo`: reversa forzada (`ConductorCarroIA.ForzarReversa`), recolocación en el cruce más cercano que la cámara no ve (lejos del jugador y sin vehículo encima), y enderezado si queda volcada a la vista.
- `ReglaSuavizadoVolante`: limita cuánto cambia el volante por segundo.
- Valores en `ConfigPersecucion` (encabezado Atascos T-1006).

## Pruebas
- EditMode: `DetectorAtascoT1006Pruebas` (12).
- PlayMode: `PatrullaAtoradaT1006Pruebas` (muro de lado a lado: reversa, recolocación y vuelve a avanzar; volcada a la vista se endereza).
