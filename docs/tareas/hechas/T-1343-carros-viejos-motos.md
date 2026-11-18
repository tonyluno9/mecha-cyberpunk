# T-1343: carros viejos y motos con los FBX nuevos de Blender

Rama tony/T-1343-carros-viejos-motos. Los FBX nuevos ya están en Assets/Arte/Vehiculos/Piezas/<código>/<código>.fbx.

## Hecho
- `Vehiculos/Editor/CreadorCarrosT1343.cs` (`ArmarTodos`, `Capturar`, `ArmarYCapturar`; menú "Mecha Cyberpunk/T-1343/..."; informe Logs/t1343-carros.txt; idempotente):
  - V02, V03, V04, V06, V09, V10: se vuelve a armar con T-1312 (conserva escala y lugar) y después `PiezasCarroT1342.ArmarPiezasMoviles` (cristales; puertas y cofre si el FBX los trae).
  - V01a, V01b, V05: la Carroceria que dejó T-742 se renombra a `Carroceria_T742` antes de armar, para que T-1312 la tome como "el modelo de antes" y la nueva quede con el mismo largo y centro. Los cristales laterales `Cristal_Lateral_D/I` no se llaman como los de puerta, así que quedan fijos. Cofre con `Bisagra_Cofre`.
  - V11: no hay `Carro_V11.prefab`; se anota y se salta. (La prueba de V11 queda inconclusa hasta que exista.)
  - Motos V07 y V08: como `ArmarMoto` de T-1312 pero con el código y el nombre del cuerpo; el largo es el del cuerpo que ya tiene el prefab. El caballete usa la zona de T-592 y no se quita si toca más del 5% de los triángulos.
  - MT01 (2.1 m) y MT02 (2.4 m): prefab nuevo copiado de Carro_C19; cuerpo `Carroceria`. No se midió su caballete: la malla sin caballete es la misma malla.
- Prueba EditMode `Tests/EditMode/Vehiculos/CarrosT1343Pruebas.cs`.

## Notas
- La patrulla (`Trafico/Prefabs/Patrulla.prefab`) se arma desde V05 en `CreadorPatrullaPiezasT742`, carpeta de Trafico: no se tocó. Sigue con la carrocería vieja de V05.
- MT01 y MT02 no entran al tráfico todavía (solo existe su prefab).
- Sin tamaños medidos de antes: la prueba comprueba 3 a 6 m en carros y 1.5 a 3 m en V07 y V08; el informe da el largo de cada uno.
- Falta correr `ArmarTodos` y las pruebas en Unity (no se corrió aquí).

## Ajustes del orquestador (al verificar)
- V01a, V01b y V05 ya no usan la llanta de V09 ni el ajuste al hueco de T-1007: llevan sus llantas de Blender. Se quitaron de
  `CreadorRuedasT1331.Carros` y `MedirRuedasT1007.CodigosCarros` (la patrulla sigue ahí) y `CarrosPorPiezasT742Pruebas` ya no exige la
  carrocería en y = 0 (la de Blender va sobre las llantas) ni textura en los `Cristal_*`.
- V07: la zona de caballete del modelo viejo no aplica al cuerpo nuevo; `mallaSinCaballete` = la misma malla.
- Después de armar hay que correr `ColocarS2nCarros.RehacerVariantes` (Arte.Editor) para que las variantes de color de V02/V03/V04 vuelvan a teñir la carrocería nueva.
- V07: el caballete se queda visible al manejar (pendiente medir su zona en el cuerpo nuevo).
