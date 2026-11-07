# T-695 Moto fina
Dueño: Tony | Rama: tony/T-695-moto-fina | Modelo: Sonnet 5.5, medium
Depende de: T-592, T-660, T-687

## Objetivo
Manejar la moto se siente fino: el conductor va quieto, con las manos en el manubrio y los pies en los estribos; la moto acelera sin pasarse (la de reparto más lenta), frena casi de golpe y el humo de daño sale del motor.

## Causas y cambios
- Saltitos y tambaleo del conductor: el Animator leía la rapidez de la moto (el jugador sigue al asiento) y jugaba Correr, y aun parado el idle mueve la cabeza hasta 8 cm. AnimacionCarro (Personaje/Jugador) apaga LeerDelPadre, pone Velocidad 0 y congela la animación (VisualPersonaje.Congelado, nuevo) mientras va en moto; IK de manos y pies sigue activo. Al bajar se restaura todo.
- Manos y asiento: ColocarPuntosMotoS1 (Personaje/Visual/Editor) ahora pone asiento y muñecas en metros por moto (V07, V08, C19). Antes las manos quedaban en el aire delante del manubrio y la cadera sobre el tanque. Medido con el jugador de 1.83 m (T-670): manos a 0 m del punto, pies a 0 m de los estribos; capturas con los puntos marcados (s1t-T-695-*-puntos.png).
- Aceleración: ConfigCarro_Moto fuerzaMotor 2600 a 1800 N (11.3 a 7.8 m/s², tope 28 a 26 m/s); medido 11.8 m/s a los 2 s y 23.7 m/s de tope. ConfigCarro_MotoReparto (nuevo, solo V07): 1250 N y tope 18 m/s; medido 8.2 m/s a los 2 s y 16.4 de tope.
- Frenar: fuerzaFreno 1500 a 6400 N (28 m/s²) y freno de mano 3000 N: de 15.5 m/s para en 0.5 s (antes 3.8 s).
- Humo de daño: DanioCarro.PuntoMotor() lo pone en el centro de la moto (40 % de su altura) en vez de 1.4 m delante, en el aire.
- Decisión: la moto NO se hace 20 % más chica (Tony dijo después que le gusta cómo se ve). El giro y la inclinación de la moto no se tocaron.

## Pruebas
- EditMode Vehiculos/CalibracionMotosT695Pruebas (reparto más lenta, arranque, frenado)
- PlayMode Personaje/MotoFinaT695Pruebas (V07, V08, C19, entrada simulada): conductor quieto respecto del asiento (rango 0.0000 m), moto sin saltitos en plano, manos y pies en su sitio, aceleración, frenado, humo
