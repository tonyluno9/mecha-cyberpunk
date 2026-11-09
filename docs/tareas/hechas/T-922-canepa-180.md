# T-922 Canepa mide 1.80 m y una prueba lo vigila

- Qué: se midió a Canepa (GTA_T417/Apoyo_CANEPA, modelo Visual_Mecanico) en Ciudad_Prologo en juego, con BakeMesh y animador siempre activo: 1.807 m. Ya estaba a escala (ColocarS2tTaller.EscalaModelo = 1.52 desde T-883), así que NO hubo cambio de escala ni de escena: no pude reproducir lo de "diminuto" en esta rama.
- Prueba nueva (PlayMode): Assets/_Proyecto/Tests/PlayMode/Mundo/CanepaAlturaT922Pruebas.cs, carga la ciudad, mide a Canepa y exige 1.80 +- 0.05 m. Si una colocación vieja (p. ej. rehacer Suburbios o el taller) lo vuelve a encoger, falla.
- Evidencia: PlayMode CanepaAlturaT922Pruebas pasa; log "[T-922] altura animada de Canepa: 1.807514".
- Pendiente para el humano: si en el juego todavía se ve chico, puede ser otro lugar (el fondo del menú, Menu_Calles, no tiene mallas con piel activas, no lo toqué por ser del menú) o una copia de la carpeta principal con una escena distinta a la de esta rama.
- Decisiones: sin código de producción tocado.
