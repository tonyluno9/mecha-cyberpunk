# T-976: bajar de la moto sin tirones (final de la moto)

**Hecho.** Pedido de Tony (13-nov): suavizar el final de la moto (bajarse y la transición al terminar de manejar) con los clips que ya hay, sin inventar animaciones.
Era lo pendiente de Blender de T-963 (BITACORA 02).

## Qué se medía antes
Prueba nueva `MotoBajarSuaveT976Pruebas` (C19 y V07, 30 cuadros/s fijos): cadera, pies y cabeza cuadro por cuadro desde que se aprieta E hasta 0.6 s de pie.
Peor tirón (cambio de velocidad de un hueso entre cuadros): **1.71 m/cuadro**. Cinco causas:

1. **Primer cuadro chueco.** La raíz del jugador salta a la salida y el modelo (su hijo) salta con ella hasta el LateUpdate; ese cuadro el Animator calculaba el IK con el cuerpo lejos de la moto (pies a 0.7 m de los estribos).
2. **Manejar → bajar.** La mezcla corta (0.06 s) bajaba todo el cuerpo 6 cm a medio camino, aunque el primer cuadro de Moto_Bajar ya es la pose de manejar (2 mm). Además el enderezado del torso (V07) se cortaba de golpe.
3. **El pie derecho salía volando a media bajada (y a media subida).** En Moto_Bajar (cuadros 31–36) y Moto_Subir (17–22) el giro del muslo derecho cambia de "rama" del humanoide (valores de músculo de -2.3 a -6.4 entre cuadros). Cada cuadro es una pose buena, pero lo que Unity interpola entre ellos no: el pie se iba ~25 cm de lado y regresaba.
4. **Se deslizaba 1.4 m y giraba al final.** El clip termina de pie a ~0.75 m de la moto, pero el jugador quedaba en la salida del vehículo (1.4 m) mirando a otro lado: el último 25 % del bajado era un deslizamiento con giro.
5. **Bajar → Idle.** La mezcla de 0.12 s cruzaba la pierna derecha por el aire (pie a 1.5 m). Al quedar de pie además jugaba GirarIzq en el sitio (el visual seguía con el giro de antes de subir).

## Qué se hizo (sin animar nada nuevo)
- `AnimacionCarro.TrasColocar()`: tras colocar la raíz, el modelo vuelve a donde estaba ese cuadro (lo llama `JugadorAPie.Bajar`).
- `ConstructorControladorBase`: AnyState → MotoBajar y MotoBajar → Idle **sin mezcla** (al final del clip: su último cuadro ya es la pose de Idle, pies a menos de 4 cm). Solo cambian esas dos transiciones.
- `PoseMoto`: al bajar, el enderezado y la inclinación se van con el IK (en el primer 40 %), no de golpe. `PoseFinalBajar()` dice dónde termina el clip.
- `JugadorAPie.Bajar`: de la moto se baja **donde termina el clip, mirando como la moto**, si cabe la cápsula; si no, en el punto libre más cercano yendo hacia la salida que dio el vehículo.
- `LimpiarClipsMotoT976` (menú Mecha Cyberpunk/T-976, o `-executeMethod ...LimpiarClipsMotoT976.Aplicar`): copia Moto_Subir y Moto_Bajar a `Assets/Arte/Personajes/PJ01/Clips/Limpios/*.anim`, cuadro por cuadro e igual al original, salvo que en los pasos donde la pierna cambia de rama los cuadros pasan de uno a otro sin interpolar. `ConstructorControladorBase.Clip()` prefiere esas copias. Si Blender rehace los clips, se reimportan y se vuelve a correr la herramienta (o se borran las copias).
- `VisualPersonaje.LeerDelPadre`: al volver a leer del padre parte de la posición y el giro actuales (sin GirarIzq falso al quedar de pie).

## Después
Peor tirón **0.16 m/cuadro** (antes 1.71). Cadera y cabeza por debajo de 0.05 en todo el bajado; queda de pie en Idle a los 1.2 s, sin girar en el sitio.
El pie se mueve por el clip limpio en pasos de a lo más 0.11 m (lo que avanza un cuadro en el tramo rápido), sin ida y vuelta.

**Queda:** los dos pies bajan 8 cm **un solo cuadro** justo después de apretar E (el IK de PoseMoto corre ese cuadro con peso 0.99 y aun así; no encontré la causa). Es el 0.16 que queda.

## Capturas
`docs/capturas/t976/C19-*.png` y `V07-*.png` (8 momentos del bajado, revisadas): se baja junto a la moto, sin deslizarse ni girar.

## Pruebas
EditMode 2356/2356. PlayMode 53/53 de moto, carro y controlador: `MotoBajarSuaveT976Pruebas` (nueva), `MotoPJ01T963Pruebas`, `CarroPJ01T963Pruebas`, `AsientoCarroT694Pruebas`,
`PuertaCarroT742Pruebas`, `AnimacionCarroPruebas`, `ApuntarVehiculoT687Pruebas`, `ApuntarDesdeVehiculo*`, `PoseMotoPruebas`, `MotoConductorT660Pruebas`, `MotoFinaT695Pruebas`,
`LanzamientoMotoT696Pruebas`, `EntrarCarroT656Pruebas`, `ControladorBaseVisualPruebas`.
`AnimacionDireccionT640Pruebas.Saltar_ConEspacio...` y `SaltoMedidaT944Pruebas` fallan igual en main sin estos cambios (comprobado), no son de esta tarea.
