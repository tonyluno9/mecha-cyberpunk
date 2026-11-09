# T-938 Drones

Rama: tony/T-938-drones (sale de T-937). Playtest 09-nov.

Antes y después (ConfigPersecucion_Trafico.asset):
- dronesPorNivel: [1,1,2] -> [0,1,2] (ningún dron en el nivel 1; el valor por defecto del código pasó de [1,2,2] a [0,1,2]).
- distanciaAparicionDron: 40 m -> 90 m. Siguen saliendo fuera de la vista y en el aire (altura 7 m) y llegan volando a 9 m/s.
- Nuevos en ConfigPersecucion: radioOrbitaDron 4 m y distanciaMinimaEntreDrones 3 m.

Separación: `CalculoSeparacionDrones` (lógica pura): cada dron tiene su punto en un círculo alrededor del jugador por índice (en la salida y al perseguir) y una repulsión entre drones para que no queden a menos de 3 m. Lo usan GestorPersecucion.CrearDron y DronMundo.

Pruebas: EditMode CalculoSeparacionDronesPruebas (7) y CalculoPersecucionPruebas (default nuevo); PlayMode nueva DronesLlegandoT938Pruebas (sin dron en nivel 1, salida a 90 m y a altura, llegada volando, nunca a menos de 3 m). Ajustadas por el nivel 1 sin drones: PersecucionBarrio, PandillaArmada, EfectosCombateT745.
