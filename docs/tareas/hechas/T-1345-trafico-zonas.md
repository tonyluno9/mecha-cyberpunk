# T-1345: tráfico por zonas con los carros nuevos

Rama tony/T-1345 (sesión s2). No se tocó ninguna escena.

## Hecho
- `ZonaTrafico` (caja orientada con `Contiene`, lista estática de activas) y `ConfigCarrosZonas` (extras generales + zonas con carros y mezcla) en Trafico.
- `TraficoCalle`: `configZonas` opcional (si es nulo carga `Resources/ConfigCarrosZonas`). Al instanciar cada carro elige el modelo según la zona del nodo donde sale; `Elegir` es pública y pura. Sin config ni zonas queda igual que antes (`general[i % Count]`, mismo orden de azar).
- Editor `Mecha Cyberpunk/T-1345/Zonas de tráfico`: CarroTrafico_V12..V24 (los que tengan Carro_<cod>), `Resources/ConfigCarrosZonas.asset` y 6 prefabs `Prefabs/Zonas/ZonaTrafico_<Nombre>.prefab` (80x20x80). Informe en `Logs/t1345-zonas.txt`.

## Pendiente
- La sesión A o Tony deben colocar los `ZonaTrafico_*` en el mapa (Deshuesadero, Malecon, NeonAlto, LasLomas, Avenida, Puerto) y ajustar tamaño y rotación. Sin zonas en la escena solo cambia que los extras V12, V13, V14, V19 se suman a la lista general.
- Las motos MT01/MT02 no entran al tráfico en esta tarea.
- La reaparición no cambia de modelo: el modelo se elige al armar el pool (cada instancia conserva su prefab).

## Pruebas
- EditMode `ZonasTraficoT1345Pruebas`.
