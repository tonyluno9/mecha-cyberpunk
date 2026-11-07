# T-716 Objetivo claro en todo momento
Dueño: Tony (S1) | Rama: tony/T-716-objetivo-claro | Modelo: Sonnet 5.5
Depende de: T-700, T-701

## Objetivo
En cada paso de la historia de la demo y del prólogo el HUD H10 muestra el objetivo con una frase corta y la distancia, y el mapa y el minimapa marcan el destino. El destino sale de la escena o de un lugar de Core, nunca de (0, 0, 0).

## Qué se hizo
- DirectorPandillas: `TextoDelPaso`/`LlevaObjetivo` (solo Continuara y Libre no llevan objetivo) y respaldo `LugarRespaldo = IdLugar.Casa` cuando falta el destino de la escena (antes caía a Vector3.zero).
- MaquinaPrologo/DirectorPrologo/DatosPrologo: la pelea del tutorial en la arena lleva objetivo ("Gana tu pelea", apunta al lugar Clandestino).
- DirectorEmboscada: en el mecha el objetivo apunta a la emboscada (antes sin posición) y sin punto de llamado apunta al lugar Taller.

## Criterios
- [x] Cada paso de la historia tiene frase de a lo más 32 caracteres
- [x] En la ciudad, cada paso fija un objetivo con posición resuelta distinta de (0,0,0) y el HUD H10 muestra frase y distancia
- [x] Toda etapa del prólogo con mundo tiene objetivo; todo paso de misión del catálogo tiene frase y punto

## Pruebas
- EditMode Progresion: ObjetivoSiempreT716Pruebas (5)
- PlayMode Progresion: ObjetivoSiempreT716Pruebas (1), recorre todos los pasos; guardado temporal. Capturas docs/capturas/s1c-T-716-*.png

## Notas
- F6 no avanzó el paso en esta prueba (sí en AtaquesZonas); se usa Cargar del guardado para fijar cada paso.
