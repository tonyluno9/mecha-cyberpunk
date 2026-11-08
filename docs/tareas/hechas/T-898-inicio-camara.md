# T-898: menú de inicio con cámara real (C1)

- `ColocarS2mInicio.GenerarEscena` crea `Menu_Calles.unity`: copia de la ciudad solo con lo que se ve y las luces (sin scripts, físicas, personajes ni audio).
- `RecorridoCallesMenu` pasa la cámara por K01, el tianguis, Neón Alto y Nova, con corte cada 8 s.
- Las 4 fotos de T-893 quedan solo mientras carga la escena; luego se desvanecen.
- Logo: LG24 (llegó a `imageassets/referencias-privadas/logo/`).
- `Menu_Calles.unity` no está en Build Settings (la demo lleva solo sus tres escenas, y `Inicio.unity` tampoco está): en el editor se carga por ruta. Para un build con el menú hay que agregar `Inicio` y `Menu_Calles`; sin ella se quedan las fotos.
- Si la ciudad cambia, hay que volver a correr `GenerarEscena`.

Pruebas: PlayMode `MenuInicioS2mPruebas` 4/4 con la ciudad cargada y los cortes. Capturas: docs/capturas/s2m-c1-calle-*.png.
