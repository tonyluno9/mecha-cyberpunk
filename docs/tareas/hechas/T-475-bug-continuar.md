# T-475 Averiguar si T-474-6 y T-474-1 eran del juego
Dueño: Tony | Rama: tony/sesion-b | Sesión B
Hecho: era de la prueba. `RecorridoDemoPruebas` tomaba como "director anterior" el del tramo 06; al salir al título la escena tiene otro director, así que tras CONTINUAR la prueba leía el del título (paso Despierta) y su `Cargar("Doc")` de rescate dejaba el marcador en (0,0,0). Arreglo: tomar `anterior` justo antes de CONTINUAR; y la ruta A* se usa desde 4 m en IrYUsar (EL DOC está tras una pared). Con eso tras CONTINUAR el paso es Doc (1 director, objetivo del Doc).
Resultado de la corrida completa: tramos 01 a 07 limpios; 08 con rescate (el bot no rodea la pared, anotado); 09 a 13 siguen sin automatizarse (límite del bot). Detalle en docs/bugs.md (T-475).
No se cambió nada del juego: sin Builds/Demo ni guía.
Bitácora: T-474-1 y T-474-6 marcados como "no era del juego".
