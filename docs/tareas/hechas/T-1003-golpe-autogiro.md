# T-1003 Golpe con auto-giro

Al iniciar un golpe de puños, GolpeAPie busca el oponente vivo (IRecibeDanio, no el propio root) más cercano en un radio de 2.5 m y un cono de ±70° al frente de la cámara, sin muro en medio, y gira el cuerpo hacia él durante la preparación (1440 °/s; al impactar se termina el giro). Apuntando no hace nada.
Valores en DatosPersonaje (Auto-giro del golpe). Lógica pura: Personaje/Combate/SelectorAutoGiro.cs.
Pruebas: EditMode SelectorAutoGiroPruebas (7), PlayMode GolpeAutoGiroT1003Pruebas (4).
