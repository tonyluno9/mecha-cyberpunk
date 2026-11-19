# T-1397: carros arreglados de Blender, y drones y naves con rotores

Rama: tony/T-1397-drones-carros. Dueño: Tony.

## Qué se hizo
- Carros: los 11 FBX de T-1380 (V01A, V01B, V02, V03, V04, V05, V06, V09, V10, V22, V23) se copiaron sobre Assets/Arte/Vehiculos/Piezas/<código>/<código>.fbx (V01A -> V01a, V01B -> V01b; el .meta no se tocó, GUID igual).
  `Arte/Editor/CarrosBlenderT1397.Aplicar` rearma los prefabs: V01a, V01b, V02, V03, V04, V05, V06, V09, V10 con `CreadorCarrosT1343.ArmarCarroPrefab` (ahora público) y V22, V23 con `CreadorCarrosT1342.ArmarUno`. Puertas y cofre con bisagra (PuertasCarro) y cristales transparentes salen de PiezasCarroT1342.
- Drones y naves: `Arte/Editor/DronesT1397.Aplicar` copia los 9 FBX a Assets/Arte/Vehiculos/Drones/<código>/, materiales URP/Lit, y un prefab por modelo en Assets/Arte/Vehiculos/Drones/Prefabs/Dron_<código>.prefab con `GiroRotores`.
- `GiroRotores` (Trafico/Drones/): gira cada descendiente `Rotor_*` sobre su Y local, velocidad en °/s serializada, pares contrarrotantes; lógica pura en `CalculoGiroRotores`.
- Dron de la policía (Trafico/Prefabs/Dron.prefab, el único dron del juego): conserva DronMundo, colisión, luces y sonido; los cubos greybox y el Visual_Noche (CY07b) quedan con el renderer apagado y se cuelga DR01 (`Visual_T1397`).

## Orden de ejecución (Unity cerrado, -executeMethod)
1. MechaCyberpunk.Arte.Editor.CarrosBlenderT1397.Aplicar
2. MechaCyberpunk.Arte.Editor.DronesT1397.Aplicar
3. MechaCyberpunk.Arte.Editor.DronesT1397.Fotos
4. EditMode: DronesT1397Pruebas

## Criterios
- [ ] Aplicar de carros sin errores; Logs/t1397-carros.txt sin FALTA.
- [ ] Aplicar de drones sin errores; 9 prefabs.
- [ ] Fotos docs/capturas/t-1397/<código>.png con los rotores girados.
- [ ] DronesT1397Pruebas pasan.
- [ ] Pruebas de Vehiculos y Trafico existentes siguen pasando (ArteNochePruebas.Dron_UsaCY07b sigue cubierta).

## Pendiente (no hay drones de estas facciones en el código)
- No existen drones de Nova, de SKULLS ni tráfico aéreo en el juego: DR02, DR03, DR04, E13, NV01 y NV03 quedan como prefabs listos, sin colocar.
- E13 es provisional (DR02_b reducido).
- Si se vuelve a correr CreadorPersecucion.CrearAssets se pierde el visual DR01 de Dron.prefab: correr DronesT1397.Aplicar después.
