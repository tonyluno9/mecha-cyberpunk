# Tareas Unity — vehículos (revisión con Tony, 20-nov)

## Ya hecho desde Blender (respaldos en `imageassets/blender/_REVISION_TONY/respaldos/vehiculos/`)
- **Puertas que chocaban al abrir** (V02, V04, V09, V23): se recortó el hueco de cada puerta en la carrocería (`Assets/Arte/Vehiculos/Piezas/Vxx/Vxx.fbx`, mismos nombres de piezas). **V01a, V01b y V05 se regresaron a su versión anterior** (Tony quiere su carrocería original; la versión por piezas desde el modelo original está en Blender y queda para el final).
- Cofre y motor nuevos en PA01, PA02 y V22: el FBX trae `Cofre` (pivote en la bisagra) y `Motor`.
- **Vidrios: SIN CAMBIO.** Se probó volverlos opacos, pero Tony pidió dejarlos como venían; `CristalCarro.mat` se regresó al original. No tocar.
- Tamaños: escala de la raíz de `Carro_V19` 0.78, `Carro_V16` 0.70, `Carro_V17` 0.80, `Carro_V18` 0.70.
- Hélices: pivote de los rotores centrado en DR01, DR02_a/b, DR03_a, DR04_a, E13 y NV01.
- Descartados por Tony: V10 y V11.

## Falta (Unity)
1. PA01, PA02, V22: sus prefabs no tienen `Bisagra_Cofre`. Agregarla como en `Carro_V05` (o volver a correr el creador de carros por piezas).
2. V16–V19 con escala 0.7–0.8: revisar Rigidbody (masa), WheelCollider (radio y suspensión), cámara del carro, asiento y punto de puerta.
3. Drones: comprobar que las hélices ya no tiemblan; quitar overrides de posición del rotor si los hay.
4. Escenas con carros viejos de una pieza (Barrio 17, Ciudad_Prologo 4; T1356_Autopista y T1355_Cerro ya no se usan): cambiar `Vxx.prefab` / `V01*_Carro*.prefab` por `Carro_Vxx.prefab`.
5. Quitar V10 y V11 de spawners y tráfico.
6. Probar: abrir puertas de V01a–V23, cofre de patrullas, tamaño de los grandes.
