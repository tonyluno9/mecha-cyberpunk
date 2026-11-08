# T-724 Clips de Tony en el jugador
Dueño: Tony (S1) | Rama: tony/T-724-clips-tony | Modelo: Sonnet 5.5
Depende de: T-560, T-561, T-640, T-671

## Objetivo
Al caminar y correr el jugador reproduce EXACTAMENTE Loco_Caminar y Loco_Correr del zip Movimiento_base de Tony, sin editar y sin capas procedurales encima, y todos los clips del jugador son de Tony.

## Qué se encontró
- Los FBX de Locomocion son idénticos (md5) a los del zip Movimiento_base. El jugador usa ControladorBase con Override_Jugador, que no cambia ningún clip; todos los clips de su controlador salen de Assets/Arte/Personajes/Animaciones (ninguno de Tripo). Los árboles de Caminar y Correr usan Loco_Caminar y Loco_Correr hacia delante.
- La capa Arriba no pone clips al caminar sin apuntar (estado Vacio) y la capa Brazos pesa 0 (T-671, T-683).
- Lo que sí estaba encima: `VisualPersonaje.AjustarRapidez` aceleraba el Animator hasta 2.2x al caminar y 2.1x al correr para igualar la velocidad del cuerpo (1.28 y 2.79 m/s del clip contra 4.5 y 7.5 m/s del jugador), y `IKPiesYMirada` movía los pies por IK. Eso era lo que se veía raro.

## Qué se hizo
- `AnimacionJugador.ClipsDeTony`: el jugador desactiva el ajuste de rapidez (`ConfigurarClips(0, 0)`, Animator.speed = 1) y el IK de pies. Se aplica al despertar y al cambiar de modelo.
- Consecuencia que se deja anotada: con el clip a su rapidez original los pies se deslizan (el cuerpo va más rápido que el clip). Las opciones están en docs/animaciones-faltan.md.
- docs/animaciones-faltan.md: sección "Revisión del jugador, 8-nov" con lo que hoy es procedural y qué bajar de Mixamo.
- JugadorVisualPruebas: dos pruebas que pedían acelerar la animación ahora piden rapidez 1.

## Criterios
- [x] Controlador, overrides, blend trees y capas del jugador: solo clips de Tony y Loco_Caminar/Loco_Correr en los árboles
- [x] En PlayMode, caminando el estado activo es Caminar con solo Loco_Caminar (peso 1.00) y corriendo es Correr con solo Loco_Correr
- [x] Animator.speed = 1, sin clips en otras capas, sin IK de pies ni de apuntado, modelo sin giro extra
- [x] Con pistola sin apuntar sigue siendo Loco_Caminar
- [x] Capturas s1d-T-724-caminar.png y s1d-T-724-correr.png

## Pruebas
- EditMode Personaje: ClipsTonyT724Pruebas (4)
- PlayMode Personaje: ClipsTonyT724Pruebas (3), JugadorVisualPruebas ajustadas
