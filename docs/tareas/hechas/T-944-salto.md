# T-944: salto

Medido en un salto real: a 1.05 m de altura, en la cima, el Animator ya entraba al aterrizaje y nunca pasaba por `SaltoAire`, así que el personaje aterrizaba medio segundo antes de tocar el piso. `VisualPersonaje` contaba como suelo cualquier momento con velocidad vertical casi cero, y en la cima también es casi cero. Ahora solo cuenta si hay piso de verdad bajo los pies.

Se usan los clips que ya tenía el proyecto (Loco_SaltoInicio, Aire y Aterrizaje); no hizo falta cambiarlos ni pedir nada en Blender.

Pruebas: PlayMode `SaltoMedidaT944Pruebas`, `VelocidadesAnimacionT652Pruebas`, `AnimatorPJ01T733Pruebas`, `ControladorBaseVisualPruebas`. Capturas: docs/capturas/ui-salto-1-inicio.png y ui-salto-3-aterrizaje.png.
