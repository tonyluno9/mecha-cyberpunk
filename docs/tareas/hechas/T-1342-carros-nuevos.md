# T-1342: carros nuevos V12 a V24

Rama tony/T-1342-carros-nuevos. Integra los carros V12 a V24 desde los FBX de Blender que ya traen las piezas separadas (Assets/Arte/Vehiculos/Piezas/<código>/<código>.fbx).

## Hecho
- `PiezasCarroT1342` (Vehiculos/Editor): material `CristalCarro` (URP Lit transparente) y `ArmarPiezasMoviles`: cristales fijos, puertas y cofre colgando de `Bisagra_Puerta_I`, `Bisagra_Puerta_D` y `Bisagra_Cofre`, y `PuertasCarro` en la raíz con las tres bisagras. Reutilizable por T-1343 para los carros viejos.
- `CreadorCarrosT1342` (Vehiculos/Editor): `ArmarTodos` arma `Carro_V12..V24.prefab` desde una copia del CarroViejo (colliders, física, asiento, punto de subida, cámara y luces) escalada al largo real; ruedas con la cadena Direccion / Giro / Modelo y `RuedasVisualesCarro`. V18 (tráiler) lleva 8 ruedas: `Rueda_TI`/`Rueda_TD` son el eje trasero del medio y los otros dos ejes llevan anclas extra. `Capturar` y `ArmarYCapturar` sacan `docs/capturas/T-1342-<código>.png` y `-abierto.png`.
- En `CreadorCarrosBlenderT1312` solo se volvieron públicos `ConfigurarImportador`, `Pieza`, `ArmarCadena`, `Poner` y `Materiales` (sin cambiar su comportamiento).
- Informe: `Logs/t1342-carros.txt`.

## Fuera de esta tarea
Agregar los carros al tráfico (T-1345) y los carros viejos (T-1343).

## Pruebas
- EditMode `MechaCyberpunk.Tests.Vehiculos.CarrosT1342Pruebas`: prefab y largo (±5%), ruedas sin nulos, bisagras con su pieza, cristales con `CristalCarro` transparente, bisagra izquierda en x < 0 y al frente de su puerta.
