# T-1350 Calles de las zonas en tráfico, policía, GPS y peatones
Dueño: Tony | Rama: tony/T-1350-calles-zonas | Modelo: Sonnet 5.5, medium
Depende de: contrato CalleZona (Core/Mundo), docs/plan-zonas-18nov.md

## Objetivo
Los carros, patrullas, el GPS y los peatones usan las calles de Malecón, Cerro, Autopista y Puerto (CalleZona.Activas), con rampas y autopista a 10 m.

## Qué hice (sin correr Unity: escrito a ojo, pendiente de verificar)
- Trafico/Persecucion/CallesDeZona.cs (nuevo, lógica pura): `Agregar(grafo)` lee CalleZona.Activas; `Agregar(grafo, definiciones, distanciaUnion)`.
  Un nodo por punto (con su y), tramos entre puntos seguidos, puntos iguales se reusan. Cada extremo se une al nodo más cercano en planta a menos de 12 m
  (otra calle o la cuadrícula) si la diferencia de y es de 4 m o menos (la autopista no se pega a las calles de abajo). Si queda suelto, se une a una calle
  sin salida de la cuadrícula o de las afueras que apunte hacia él (rumbo a menos de 12 m, hasta 140 m): así U1, U2 y U4, que están más lejos que el alargue, quedan unidos.
- GrafoCalles: AgregarNodo, Enlazar, EsDeZona, EsRapido (tramos de autopista), TieneAlturas; MasCercano usa distancia 3D solo si hay nodos con más de 1.5 m de diferencia de altura
  (sin zonas se comporta igual que antes). DistanciaASegmento ya no da NaN con un tramo de largo 0.
- TraficoCalle.AlargarHaciaLasAfueras no alarga nodos de zona (nunca salen hacia el mar o un muro); una calle de afueras unida a una zona ya no es punta, así que no se alarga.
- GestorPersecucion.ArmarCallesDe y ArmarCalles agregan las zonas antes del alargue (cubre tráfico, patrullas y las pruebas que usan ArmarCallesDe).
- RutaCallesBarrio (GPS): rehace su grafo cuando cambia el número de CalleZona activas (se registra antes de cargar la escena, cuando aún no hay ninguna).
- CarroTrafico: +60 % de velocidad en tramos de autopista; en rampas (desnivel de 0.5 m o más entre nodos) inclina el carro según la pendiente (máx. 15 grados). Ya seguía el suelo con rayo.
- Patrullas (física) y drones (relativos al jugador) no necesitaron cambio: ya usan el suelo real y sus cálculos de rumbo ignoran la y.
- Peatones: RutasBanqueta.DeCalleZona (pura) da 2 rutas por calle (a ancho/2 + 1.5 m); GrupoPeatones, al iniciar, crea hasta 16 peatones extra (uno por ruta) en CalleZona con banquetas y sin autopista, activos en la misma proporción día/noche. Se arman en ejecución: no hay que hornear el prefab.

## Pruebas a correr
- EditMode: CallesDeZonaT1350Pruebas (nueva) y las viejas GrafoCallesPruebas, RutaCallesBarrioPruebas, RutasBanquetaPruebas, CalculoLlegadaPatrullaT708Pruebas (deben seguir iguales).
- PlayMode (regresión): TraficoCallePruebas, PersecucionRealistaT587Pruebas, PeatonesBarrioPruebas.

## Riesgos
- Nada compilado ni corrido. Los .meta de archivos nuevos los genera Unity.
- BloqueosDeCalles.Marcar puede cerrar tramos de zona si hay barandales o muros dentro de los 4 carriles; revisar con las zonas colocadas.
- Los peatones de zona dependen de que la escena tenga un GrupoPeatones (el del Barrio) y de que CalleZona exista al iniciar.
- GPS: la ruta sigue siendo plana (y = 0); la autopista sobre calles solo se distingue en planta.
- Las patrullas con física en rampas de 9 % no se probaron.
