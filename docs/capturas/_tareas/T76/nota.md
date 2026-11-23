# T76: puertas que abren en los carros básicos de Tripo (investigación)

Conclusión: no se implementó nada. Los carros básicos de Tripo no tienen la puerta del conductor como pieza separada.

Evidencia:
- Los FBX de V01a, V02, V03, V09, V13 y V14 (Assets/Arte/Vehiculos/...) son binarios con una sola geometría (V02 y V03 con una segunda entrada de la cabecera); ninguno trae nodos con nombre "puerta", "door" ni "bisagra" (grep -a).
- T24 / T-1644 (`Vehiculos/Editor/CarrosTripoT1644.cs`) devolvió a propósito esos carros al modelo original de Tripo de una sola malla y llamó a `SoltarBisagras`: en Carro_V01a, V02, V03, V09, V13 y V14 los campos `bisagraPuertaIzquierda`, `bisagraPuertaDerecha` y `bisagraCofre` de `PuertasCarro` están en `fileID: 0`.
- El mecanismo de abrir la puerta al subir y bajar ya existe (`Vehiculos/Carro/PuertasCarro.cs`, T-1344: gira la bisagra del lado de `PuntoSubida`, 70 grados). Solo funciona con carros que tengan bisagras: la patrulla buena (Carro_PA01 / Patrulla.prefab) y los carros de piezas de Blender.

Para tenerlo en un carro básico haría falta separar la puerta en Blender (pieza con su pivote en la bisagra) y asignar la bisagra en `PuertasCarro`; sin eso, no se inventa. No hay cambios de código.
