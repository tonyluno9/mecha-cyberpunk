# T-1362: rendimiento de la Colonia del Cerro y del Puerto

Rama tony/T-1362-rendimiento-cerro. El Cerro tiene ~1400 casas, ~7400 renderers y ~76 millones de triángulos en el prefab; el Puerto muchos contenedores y rejas.

## Hecho (parte 1: medición)
- PlayMode `Assets/_Proyecto/Tests/PlayMode/Mundo/RendimientoCerroT1362Pruebas.cs` (asmdef MechaCyberpunk.Tests.PlayMode.Mundo): instancia cada zona sola en una escena vacía con una cámara como la del jugador (FOV 60, near 0.05, far 1000), luz direccional tenue y ambiente oscuro. 8 puntos en el Cerro y 4 en el Puerto; 60 frames de calentamiento y 180 medidos. Solo informa; falla si un punto da menos de 20 FPS.
- Editor `Assets/Arte/Editor/MedirZonasT1362.cs`, método `Contar()`: conteo por zona y tipo de prop en `Logs/t1362-conteo.txt`.

## Cómo correr
- Sufijo: `-sufijo antes|despues` en la línea de comandos de Unity, o variable `T1362_SUFIJO` (por defecto `antes`).
- Conteo: `-executeMethod MechaCyberpunk.Arte.Editor.MedirZonasT1362.Contar`.
- Salidas: `Logs/t1362-rendimiento-<sufijo>.txt` y `.json`, capturas `docs/capturas/bugs-18nov-B/fps-<zona>-<n>-<sufijo>.png`.

## Medición
Pendiente de correr (la corre el orquestador). Pegar aquí la tabla de `Logs/t1362-rendimiento-antes.txt`, los 30 renderers con más triángulos y el conteo por tipo.

## Notas
- Los datos de oclusión horneados viven en la escena, no en el prefab: el conteo no puede saberlos.
- Los triángulos por LOD usan reflexión sobre Mesh.lodCount y GetLodSubMesh; si no existen, solo LOD0.
- Los arreglos de rendimiento vendrán después con estos números.
