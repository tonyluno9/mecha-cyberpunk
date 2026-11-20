# T-1482 Tráfico, peatones y drones en los 4 barrios (revertir mapa 19-nov)

Rama: `tony/T-1482-trafico-peatones-barrios`. Base de comparación: `8b33987^1`. Va junto con T-1480 (saca de la escena las zonas y los `ZonaTrafico_*`).

## Qué encontré
- **Calles (grafo)**: todo el código de zonas es condicional a que haya `CalleZona` en la escena: `CallesDeZona.Agregar(grafo)` devuelve 0 si `CalleZona.Activas` está vacío;
  `GrafoCalles.TieneAlturas` es false sin nodos altos (vuelve a medir en plano); `EsRapido` es false sin tramos de autopista (factor de velocidad 1);
  `AlargarHaciaLasAfueras` solo salta nodos de zona (`EsDeZona`), que ya no existen; `GrupoPeatones.CrearPeatonesDeZonas` no crea ninguno sin `CalleZona`; `BloqueosDeCalles` no cambió.
  No hay coordenadas fijas de zona en Trafico (las cajas de zona viven en `Arte/Editor/ColocarT1364ZonasTrafico.Cajas`, no tocada). Conclusión: no hizo falta cambiar código de calles, el comportamiento
  vuelve solo al de antes cuando T-1480 quita los `CalleZona`. El código se queda (inofensivo y con sus pruebas puras).
- **Carros**: `DisenoZonasTrafico` repartía por zona. Zonas que se quedan: Deshuesadero, NeonAlto, LasLomas, Avenida (cajas `ZonaTrafico_*` que T-1480 conserva).
- **Misiones/objetivos/IA/Progresión**: ningún cambio de 8b33987^1 a HEAD toca misiones, objetivos ni puntos de aparición (solo sonidos); `Mision_Destino_Muelle` es del prólogo viejo (22.5, 7.5), no del Puerto nuevo.
  `Mundo/Ambiente/TraficoAutopista.cs` es de T-098: la autopista decorativa vieja (z = 103 + `VestirFinales`), se queda. `SalidaDelAgua`, `GiroHazFaro`, `PoseQuieta`, `CreadorMarT1351` solo se usan en objetos de las zonas quitadas;
  quedan como código sin uso (no estorban). La UI (`BarriosMapa`, `HudH10`, `Minimapa`) menciona las zonas nuevas pero es de otra área: ver pendientes.

## Qué cambié
1. `Trafico/Datos/DisenoZonasTrafico.cs`: se quitan las zonas Malecon, Puerto, Autopista y Cerro. Carros que solo salían ahí y adónde van:
   | Carro | Antes | Ahora |
   | --- | --- | --- |
   | MT01, MT02 (motos) | Malecón, Autopista, Cerro | Extras generales (toda la ciudad) |
   | V20, V21 | Deshuesadero, Autopista | siguen en Deshuesadero y además en Extras (Deshuesadero queda al borde norte) |
   | V22, V23, V24, V12 | Malecón/Autopista | siguen en NeonAlto (V12 también en Extras) |
   | V15 | Malecón, Autopista | LasLomas |
   | V09, V10 | Malecón / Puerto, Cerro | Avenida |
   | V16, V17, V18 | Puerto, Autopista | Avenida |
   | V13, V14, V19 | Autopista, Cerro | Avenida / Extras |
   | V01a, V01b, V02, V03, V04 | Cerro/Puerto | ya salen en la lista general del `TraficoCalle` (los 8 carros de T-511) |
   Se verificó que cada uno de `CarrosNuevos` sale en una zona que queda o en Extras (prueba nueva).
2. `Trafico/Editor/ZonasTraficoT1345.cs`: se quitan Malecon y Puerto de su tabla vieja (para que re-correrlo no los vuelva a crear).
3. `Trafico/Editor/ZonasTraficoT1482.cs` (nuevo): `Aplicar` rehace `ConfigCarrosZonas.asset` y los prefabs `ZonaTrafico_*` solo de las 4 zonas; `QuitarPrefabsViejos` borra `ZonaTrafico_Malecon/Puerto/Autopista/Cerro.prefab`.
4. `Arte/Editor/PeatonesBarriosT1482.cs` (nuevo): rehace `ConfigPeatonesZona_Base.asset` con las zonas Avenida, NeonAlto, LasLomas y Deshuesadero y reconecta el prefab Peaton.
   Dónde sale cada NPC: pescador y turista a Avenida (mercado); turista y corredor a NeonAlto; abuela, elotero y corredor a LasLomas (suburbios); estibador a Deshuesadero; repartidor y oficinista en las cuatro.
   Fuera de toda caja (Suburbios del oeste, Nova) usan la lista general del prefab Peaton, que ya trae las 14 variantes NP07..NP14.
5. `Arte/Editor/DronesZonasT1398.cs`: el plan del grupo Ciudad y las dos naves NV03 de Nova ahora quedan dentro de x -240..240, z -180..180 (antes x -480..345, z -330..290). Misma cantidad de drones (19), mismas alturas y códigos.
   Se separó `ConstruirPrefab`; nuevo `SoloPrefab` rehace el prefab sin abrir ni guardar la escena (la instancia en Ciudad_Prologo se actualiza sola). `PlanDentroDeLosBarrios()` y `LimiteX/LimiteZ` para pruebas.
6. Pruebas: ignoradas con `[Ignore("T-1482: ...")]` las que solo prueban zonas nuevas (`TraficoZonasT1389Pruebas` x4, `ZonasTraficoT1364Pruebas` x3, `PeatonesZonaT1409Pruebas` x2, clase `TraficoZonasT1350Pruebas` de PlayMode).
   Ajustadas `ZonasTraficoT1345Pruebas` (4 zonas) y `TraficoZonasT1389Pruebas.Diseno_CadaCajaTieneSuZonaYViceversa` (ignora las cajas de zonas quitadas, que siguen en la tabla de Arte).
   Nuevas: `PeatonesBarriosT1482Pruebas`, `DronesBarriosT1482Pruebas`, y `T1482_SoloQuedanLas4ZonasDeLosBarrios_YLasMotosSalenEnLosExtras`.

## Métodos de editor a correr, en orden (Unity cerrado, batch)
1. (Después de compilar) `-executeMethod MechaCyberpunk.Trafico.Editor.ZonasTraficoT1482.Aplicar` (config de carros y prefabs de zona; informe `Logs/t1482-trafico.txt`).
2. `-executeMethod MechaCyberpunk.Arte.Editor.PeatonesBarriosT1482.Aplicar` (config de peatones y Peaton.prefab; `Logs/t1482-peatones.txt`).
3. `-executeMethod MechaCyberpunk.Arte.Editor.DronesZonasT1398.SoloPrefab` (prefab de drones; no toca la escena).
4. Cuando T-1480 ya esté integrada y la escena no tenga los `ZonaTrafico_*` quitados: `-executeMethod MechaCyberpunk.Trafico.Editor.ZonasTraficoT1482.QuitarPrefabsViejos` (opcional).

## Qué hay que comprobar en Unity
- `ConfigCarrosZonas.asset` con 4 zonas y extras MT01/MT02/V20/V21; en Play, tráfico con motos y sin carros fuera de los 4 barrios.
- Sin `CalleZona` en la escena: `GestorPersecucion` arma el grafo (sin avisos), patrullas y tráfico no salen por las afueras del mar/cerro.
- Peatones: en Avenida/NeonAlto/LasLomas aparecen variantes de zona; el log de peatones no pide zonas inexistentes.
- Drones: en Ciudad_Prologo el prefab T1398 sigue instanciado y ninguna ruta pasa de las afueras; `DronesZonasT1398.Validar` sigue en "todo bien" (7 Ciudad, 6 Nova, 3 Skulls, 2 Policía).
- EditMode del área Trafico y PersonajeVisual compilando y verdes (las ignoradas no cuentan).

## Supuestos y pendientes
- Límites de los barrios tomados del encargo (x -250..250, z -190..190); no se midió LimiteInvisible en la escena.
- `Arte/Editor/ColocarT1364ZonasTrafico.Cajas` (no es mi carpeta) sigue con las cajas de Malecon/Puerto/Autopista/Cerro; si T-1480 las quita, también hay que quitar las 3 pruebas ignoradas de `ZonasTraficoT1364Pruebas`.
- `PeatonesZonaT1409.Diseno` y `Aplicar` siguen escribiendo las 4 zonas viejas si alguien los corre; usar `PeatonesBarriosT1482.Aplicar`.
- UI (`BarriosMapa`, `HudH10`, `Minimapa`) y pruebas de Mundo/PlayMode de recorrido de zonas (T-1361, T-1354..T-1366) no se tocaron: son de otras tareas.
- Core: `CalleZona`, `IZonaDelMundo` y `ZonasMundo` quedan sin zonas que los usen; no estorban, no se cambió Core.
