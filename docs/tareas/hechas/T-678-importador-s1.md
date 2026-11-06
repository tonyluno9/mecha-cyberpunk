# T-678 Importador de la sesión 1
Dueño: Tony | Rama: tony/T-678-importador-s1 | Modelo: Sonnet 5.5

## Objetivo
Un solo importador que mete los assets nuevos de la sesión 1 (armas W02-W04, policía C01, destello F01, hologramas H02-H04) solo cuando aparecen y sin repetir trabajo.

## Qué se hizo
- `Assets/Arte/Editor/ImportarS1n.cs` (menú Mecha Cyberpunk > S1n > Meter assets nuevos; `ImportarS1n.MeterTodo` en batchmode). No toca las filas de ImportarS2n.
- Zips de Tripo: W02/W03/W04 a `Assets/Arte/Armas/Wxx_Nombre/` (modelo + `Texturas/`, igual que W01_Pistola); C01 a `Assets/Arte/Personajes/Policia/` (`Policia.fbx` + `Policia.fbm/`).
- F01_Destello a `Assets/Arte/VFX/` y asignada a `DatosSensacion.texturaDestello`.
- H01-H04: ya los metió la sesión 2 (T-816); el importador lo comprueba y solo llama a `ImportarS2nHologramas` si falta alguno.
- Busca en `imageassets/` del proyecto y, si no, en `../mecha-cyberpunk/imageassets/`.

## Criterios
- [x] Correrlo dos veces no copia nada la segunda vez (prueba).
- [x] Si un archivo no ha llegado lo anota y sigue.
- [x] F01 queda asignada al destello; H02-H04 están en el HUD.

## Pruebas
EditMode `Tests/EditMode/Arte/ImportarS1nPruebas.cs` (5).
