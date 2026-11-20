# T-1480 Escena sin zonas (revertir el mapa)

Rama: `tony/T-1480-escena-sin-zonas`. Pedido de Tony: quitar de `Ciudad_Prologo.unity` la expansión del 18-19 nov (Malecón, Colonia del Cerro, Autopista elevada con túnel, Puerto) y dejar los 4 barrios con sus bordes como antes (base `8b33987^1`). Los prefabs y modelos de las zonas se quedan en el proyecto; solo salen de la escena.

## Qué hace `Assets/Arte/Editor/RevertirMapaT1480.cs`

`Aplicar()` (idempotente):
- Destruye las raíces `T1354_Malecon`, `T1355_Cerro`, `T1356_Autopista`, `T1357_Puerto`, `T1388_Uniones`, `T1461_Look`, `T1358_Orillas`, `T1365_Uniones`, `T1392_Volumenes`, `T1399_Bruma`, `T1462_Aereas`. No toca `T1398_DronesAereos`.
- Bajo `Zonas` destruye `ZonaTrafico_Malecon`, `_Cerro`, `_Puerto`, `_Autopista`, `_Autopista_RampaEste`.
- Vuelve a encender 61 GameObjects y 1 Renderer que la expansión apagó, y regresa a identidad los 4 `Tramo_*` de `T513_Fondo` (lista de 66 entradas con fileID local; si no encuentra el id cae a la ruta y lo anota).
- Guarda la escena y escribe `Logs/t1480-revertir.txt`.

`Validar()`: abre la escena y escribe `Logs/t1480-validar.txt` con raíces o ZonaTrafico que sobran, objetos de la lista aún apagados, huérfanos (Light, Collider no trigger y Renderer activo fuera del rectángulo de `LimiteInvisible` de `T530_Afueras` + 5 m; solo para revisión), scripts faltantes y referencias rotas. En batch sale con 0 o 1 según fallas duras.

## Cómo correrlo (Unity cerrado, desde la raíz)

```bash
Unity.exe -batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Arte.Editor.RevertirMapaT1480.Aplicar -logFile Logs/t1480-aplicar.log
Unity.exe -batchmode -projectPath . -executeMethod MechaCyberpunk.Arte.Editor.RevertirMapaT1480.Validar -logFile Logs/t1480-validar.log
```

Prueba EditMode: `Assets/_Proyecto/Tests/EditMode/Mundo/RevertirMapaT1480Pruebas.cs`.

## Falta probar en Unity (escrito sin compilar)

- Que compile (el worktree no tenía Library).
- Que `GlobalObjectId.GetGlobalObjectIdSlow(...).targetObjectId` coincida con los fileID de la lista (si no, cae a ruta; revisar el informe).
- Correr Aplicar, luego Validar (debe salir 0) y las pruebas EditMode.
- Revisar los huérfanos y referencias rotas del informe de Validar (p. ej. rutas de `T1398_DronesAereos`, que otra tarea ajusta).
- Capturas de los 4 barrios y sus bordes contra la escena de antes.

## Fotos

`Assets/Arte/Editor/FotosT1480.cs` toma las mismas fotos de la escena de ahora y de la de antes de la expansión
(`git show 8b33987^1:Assets/_Proyecto/Escenas/Ciudad_Prologo.unity`, copiada a `Assets/_Temp_T1480/Ciudad_Antes.unity`, sin versionar).
Con Unity cerrado, desde la raíz:

```
Unity.exe -batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Arte.Editor.FotosT1480.FotosAhora
Unity.exe -batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Arte.Editor.FotosT1480.FotosAntes
```

Salen a `docs/capturas/t-1480/<nombre>-ahora.png` y `-antes.png` (noche) y `<nombre>-dia-<sufijo>.png` (13 h); la lista queda en `Logs/t1480-fotos.txt` (se agrega, bórrala antes de repetir).
