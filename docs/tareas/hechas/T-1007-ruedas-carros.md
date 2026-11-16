# T-1007: ruedas al tamaño de su hueco, y la moto

Playtest de Tony: ruedas de todos los carros (patrullas incluidas) al tamaño correcto de su hueco; revisar que la corrección de la moto (T-976) esté en el build.

## Cómo se mide el hueco
`MedirRuedasT1007` (Vehiculos/Editor; `-executeMethod MechaCyberpunk.Vehiculos.Editor.MedirRuedasT1007.Medir` solo mide, `.Ajustar` mide y ajusta; escribe Logs/t1007-ruedas.txt y, con `Depurar`, Logs/t1007-<carro>.png).
Pinta en una cuadrícula de 5 mm (z, y) los triángulos de la carrocería sin ruedas (Visual_T511/Carroceria) que están del plano del centro de la llanta hacia afuera; lo que queda sin pintar alrededor del centro de la llanta es el paso de rueda.
El radio del hueco es la mitad de su ancho a la altura del centro de la llanta (adelante + atrás). El techo del arco no cuenta: la llanta asoma por delante de la salpicadera (diseño del modelo).
Ajuste: las cuatro llantas con el mismo radio = 0.90 x el radio medio del hueco (punto fijo: el centro de la llanta va a y = radio, apoyada en el piso, y se centra adelante/atrás en su hueco, máximo 8 cm; la llanta no crece más de 1.25x).

## Números (m)
| carro | radio antes | radio hueco (DI/TI) | radio ahora | desplazamiento z (DI / TI) |
| --- | --- | --- | --- | --- |
| V01a | 0.339 | 0.355 / 0.365 | 0.324 | -0.032 / +0.015 |
| V01b | 0.339 | 0.392 / 0.445 | 0.377 | -0.080 / +0.040 |
| V05 | 0.319 | 0.375 / 0.380 | 0.340 | -0.080 / +0.080 |
| Patrulla | 0.319 | 0.375 / 0.380 | 0.340 | -0.080 / +0.080 |
Los CarroTrafico_V01a/V01b/V05 son variantes de esos prefabs y heredan el cambio. `RuedasVisualesCarro.radio` queda en el radio nuevo.
Motos V07, V08 y C19 (llanta RU02): ya medían la altura de su centro; en V07 la llanta llena 0.89 del cilindro que se borró y en V08 0.99-1.00, así que no se tocaron. C19 viene sin llanta horneada (no hay hueco que medir). V02, V03, V04, V06, V09 y V10 son una sola malla con las ruedas horneadas (no hay llanta visual separable): no se tocaron.
Nota: en V07 se ven restos de la llanta horneada vieja detrás de la trasera (T-743, no de esta tarea).

## Pruebas
- EditMode `RuedasAlHuecoT1007Pruebas` (mide el hueco de cada rueda en el prefab; falla si la llanta queda fuera de 0.80-1.02 del hueco, no toca y = 0 o se descentra más de 9 cm) + motos V07/V08 contra el cilindro borrado. El asmdef de pruebas EditMode de Vehiculos ahora referencia Vehiculos.Editor.
- PlayMode `RuedasHuecoT1007Pruebas`: llantas en y = 0 y radio igual al de `RuedasVisualesCarro` en 10 prefabs; capturas de lado en docs/capturas/t1007/ (CAPTURAR_S1=1).
- Moto: T-976 (74ef520) está en main; Ciudad_Prologo es la escena del build (EditorBuildSettings). `MotoBajarSuaveT976Pruebas` y `MotoPJ01T963Pruebas` pasan.
