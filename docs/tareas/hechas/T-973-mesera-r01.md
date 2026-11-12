# T-973: mesera R01 nueva de Blender en el casino

- Se reemplazó `Assets/Arte/Personajes/MeseraRobot/MeseraRobot.fbx` por R01 reexportada de Blender (mismos nombres de objeto). El `.meta` no cambió (mismo GUID, Humanoid, globalScale).
- Inventario de fileIDs antes/después (`Logs/t973-antes.txt`, `Logs/t973-despues.txt`): 149 líneas idénticas; los 6 fileIDs que usan Ciudad_Prologo y Visual_MeseraRobot siguen (100100000 es la raíz del prefab y no sale en la lista). La escena no se tocó.
- Avatar humanoide válido; material `tripo_mat_3a993998` conservado.
- Charola: 2142 vértices con peso 100 % a `mixamorig:LeftHand` (prueba `MeseraR01T973Pruebas`).
- PlayMode (MeseraCharolaT930, MeserasCasinoS2p, CapturasCasinoT930): 4/4. EditMode completo: 2338/2338.
- Capturas en `docs/capturas/t973/`.
