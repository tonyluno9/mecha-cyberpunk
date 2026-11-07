# T-719 Las expendedoras leen su posición de la escena
Dueño: Tony (S1) | Rama: tony/T-719-expendedoras-marcadores | Modelo: Sonnet 5.5
Depende de: T-685

## Objetivo
Las tres expendedoras (casa, tianguis, Nova) dejan de usar coordenadas fijas: leen su posición de un marcador de la escena o de un lugar de Core, para que la sesión 2 pueda rehacer la ciudad sin que las máquinas queden en medio de un edificio.

## Qué se hizo
- DatosExpendedoras.Posicion: campos `marcador` (nombre de un objeto de la escena), `lugar` (IdLugar de Core) y `desplazamiento`; `posicion` queda solo para pruebas y escenas sin lugares.
- ColocadorExpendedoras.ResolverPosicion: el marcador de la escena gana; si no existe, la entrada del lugar de Core más el desplazamiento; solo sin marcador ni lugar se usa la posición fija. Si piden un lugar o un marcador y no está, la máquina no se coloca y avisa con un warning (no cae a coordenadas inventadas).
- CreadorExpendedoras (editor, corrido en batch) regeneró DatosExpendedoras_Ciudad: casa = Lugar_Casa + (0, 0, 6); tianguis = Lugar_Bar + (-36, 0, 0.49); Nova = Lugar_Bar + (64, 0, -0.01); las mismas posiciones medidas antes. Marcadores acordados: Marcador_Expendedora_Casa, _Tianguis y _Nova (la sesión 2 los puede agregar; no se tocó la escena: ver docs/decisiones-pendientes-tony.md).
- Aparte: TeclasHud.Nombre muestra "Ctrl" y no "Left Control" (se vio en la captura del tutorial).

## Criterios
- [x] En la ciudad las máquinas salen en la entrada del lugar más su desplazamiento
- [x] Un marcador con el nombre acordado en la escena manda sobre el lugar
- [x] Sin el lugar no se coloca la máquina ni cae a coordenadas fijas
- [x] Los datos de la ciudad no llevan coordenadas fijas

## Pruebas
- EditMode UI: ExpendedorasMarcadoresT719Pruebas (7), TutorialTeclasRealesT718Pruebas (+1: Ctrl)
- PlayMode UI: ExpendedorasMarcadoresT719Pruebas (1, en Ciudad_Prologo); siguen las de T-685 (ExpendedoraT685Pruebas, ConsumiblesT685Pruebas). Captura docs/capturas/s1c-T-719-expendedora-casa.png
