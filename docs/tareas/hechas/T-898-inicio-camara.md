# T-898: menú de inicio con cámara real (C1)

- `ColocarS2mInicio.GenerarEscena` crea `Menu_Calles.unity`: copia de la ciudad solo con lo que se ve y las luces (sin scripts, físicas, personajes ni audio).
- `RecorridoCallesMenu` pasa la cámara por K01, el tianguis, Neón Alto y Nova, con corte cada 8 s.
- Las 4 fotos de T-893 quedan solo mientras carga la escena; luego se desvanecen.
- Logo: LG24 (llegó a `imageassets/referencias-privadas/logo/`).
- Se agregó `Menu_Calles.unity` a `ProjectSettings/EditorBuildSettings.asset`: hace falta para que entre al juego y lo pidió Tony (normalmente es zona de Core).
- Si la ciudad cambia, hay que volver a correr `GenerarEscena`.

Pruebas: PlayMode `MenuInicioS2mPruebas` 4/4 con la ciudad cargada y los cortes. Capturas: docs/capturas/s2m-c1-calle-*.png.
