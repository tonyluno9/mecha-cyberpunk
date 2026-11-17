# T-1312 Ruedas de Blender en V02, V03, V04, V06, V09, V10 y C19

Origen: docs/playtest-17nov.md, punto 12 (sesión B).
Área: Vehiculos/ y Assets/Arte/Vehiculos/Piezas/.

## Hecho
- `CreadorCarrosBlenderT1312` importa `imageassets/blender/carros/*.fbx` (carrocería + ruedas separadas) y arma los prefabs
  `Carro_V02..V10` y `Carro_C19`: carrocería escalada al largo del carro, cada rueda en su hueco `Rueda_XX/Direccion/Giro`.
  Medidas en Logs/t1312-carros.txt (radio de rueda y entre ejes por modelo). A la moto se le quitó el caballete.
- `RuedasBlenderT1312Pruebas` (PlayMode, 7 casos): al avanzar, las ruedas giran solo sobre X y tocan el piso. 7/7.
- Capturas de lado y 3/4 en docs/capturas/t-1312/ (revisadas: ruedas en su hueco, sobre el piso).
- Pruebas del área: EditMode Vehiculos 198/198; PlayMode Vehiculos 141/142: falla CiudadCarrosPruebas (el V08 estacionado se atora al robarlo), que también falla en la base 4f96fe3 con los prefabs de antes.
- Las variantes de color del tráfico (V02, V03, V04) tiñen ahora el material de la carrocería de Blender
  (`ColocarS2nCarros.RehacerVariantes`). `CreadorCarrosT511.CrearUno` ya no rearma con el modelo viejo un carro que lleva las
  piezas de Blender (lo hacía `ImportarS2n.MeterTodo`, que corre una prueba de EditMode).
- EditMode completo antes del arreglo: 2578/2581; los 3 fallos eran de esta tarea y quedaron arreglados (CarrosS2nPruebas 4/4,
  T742/T743 al día). PlayMode de carros en Personaje (T694, T696, T704): 23/23.
