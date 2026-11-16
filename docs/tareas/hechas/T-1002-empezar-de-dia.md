# T-1002: el juego empieza de día

Playtest de Tony: nueva partida y demo arrancan de día; el ciclo sigue normal.

- `ConfigRelojMundo_Pandillas.asset` (reloj de Ciudad_Prologo, la ciudad de la demo y la partida nueva): horaInicial 19 a 10.
- `ConfigRelojMundo_Mundo.asset` (reloj de Ciudad.unity y Pruebas_Alan): horaInicial 21 a 10. Solo ese número.
- `CreadorCiudadPandillas.HoraInicial` 19 a 10, para que regenerar la escena no la devuelva a la noche.
- Sin cambios: el despertar (20:00), la noche del tutorial (22:00) y la carga de partida (el guardado conserva su hora).
- Prueba: `HoraInicialDeDiaPruebas` (EditMode/Mundo).
