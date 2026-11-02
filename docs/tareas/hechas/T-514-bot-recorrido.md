# T-514 Paso 6 del plan de personajes y carros: bot del recorrido
Dueño: Tony | Rama: tony/T-514-bot-recorrido | Modelo: Opus 5.5 | docs/plan-personajes-carros.md, paso 6
Hecho: el bot de RecorridoDemoPruebas (Tests/PlayMode/Demo) llega a El Tianguis, al jefe de los SKULLS, a EL HACKER y a CONTINUARÁ sin que la prueba mate enemigos por él. La dificultad del juego no cambia: todo está en la prueba.
Por qué se quedaba en El Tianguis (trazas [T-476] del log):
1. Apuntaba al jefe: era el enemigo más cercano mientras VENCE A LOS SKULLS cuenta a los 7 pandilleros; gastaba la pelea en él. Ahora, en el tramo 10, el jefe no es blanco (PandilleroCercaSinJefe).
2. A menos de 5 m daba por visto a un SKULL escondido tras una cubierta y le disparaba a la pared; avanzaba y retrocedía sin acertar. Ahora solo cuenta como visto con línea de vista (o a menos de 2.5 m); si no lo ve y está a menos de 12 m lo flanquea (8 m, 70° a un costado, cambia de lado cada 3 s) y si lleva 8 s sin verlo lo deja 10 s y elige otro.
3. Caía KO, el KO le quita respeto (70 → 40/50) y El Tianguis ya no pasaba al jefe ni con el rescate. Ventaja solo de la prueba: en las peleas del Tianguis y del jefe, si la vida baja de 45 % se cura (VidaAPie.CurarTodo); el bot se sigue cubriendo con Q al 50 % como un jugador.
4. Las peleas largas llevaban el reloj del mundo a la mañana y el Casino (camino a EL HACKER) cerraba por horario; con las peleas cortas ya no pasa.
La prueba ahora falla si tuvo que usar el rescate (matar a los SKULLS o al jefe por el bot) y anota las curas de prueba en Logs/t474-tiempos.txt. La pelea del Tianguis tiene 300 s de tope (antes 240).
Código: Tests/PlayMode/Demo/RecorridoDemoPruebas.cs.
Pruebas: RecorridoDemoPruebas 2/2 en 3 corridas seguidas: 357, 351 y 344 s en total; 0 bugs, 0 KO del jugador, sin rescate; curas de prueba 2, 3 y 0 (la tercera sin ninguna ayuda).

## Bitácora
- 2026-11-02: primera vuelta (solo curas): 1/2, 2 de 7 SKULLS en 300 s (el bot le tiraba al jefe). Segunda (sin el jefe como blanco): 1/2, 0 de 7 (pegado a uno tras una cubierta). Tercera (flanqueo y sin "visto a 5 m"): 2/2, Tianguis 169 s, jefe 43 s, EL HACKER 56 s, 0 bugs, 2 curas de prueba.
- 2026-11-02: tres corridas seguidas 2/2 (curas de prueba 2, 3 y 0). Builds/Demo y Builds/Juego regenerados (el cambio es solo de prueba).
