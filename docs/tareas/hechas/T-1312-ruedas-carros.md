# T-1312 Ruedas de Blender en V02, V03, V04, V06, V09, V10 y C19

Origen: docs/playtest-17nov.md, punto 12 (sesión B).
Área: Vehiculos/ y Assets/Arte/Vehiculos/Piezas/.

## Hecho
- `CreadorCarrosBlenderT1312` importa `imageassets/blender/carros/*.fbx` (carrocería + ruedas separadas) y arma los prefabs
  `Carro_V02..V10` y `Carro_C19`: carrocería escalada al largo del carro, cada rueda en su hueco `Rueda_XX/Direccion/Giro`.
  Medidas en Logs/t1312-carros.txt (radio de rueda y entre ejes por modelo). A la moto se le quitó el caballete.
- `RuedasBlenderT1312Pruebas` (PlayMode, 7 casos): al avanzar, las ruedas giran solo sobre X y tocan el piso. 7/7.
- Capturas de lado y 3/4 en docs/capturas/t-1312/ (revisadas: ruedas en su hueco, sobre el piso).
