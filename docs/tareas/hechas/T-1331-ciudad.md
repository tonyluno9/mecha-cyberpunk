# T-1331 Ciudad: toldos, Tino, estela, carros, ruedas y postes

Origen: playtest 17-nov, parte B (docs/playtest-17nov-b.md no está en el repo; se trabajó con el encargo). Hecho sin abrir Unity (sin Library):
nada de esto está compilado ni corrido. El orquestador debe compilar y correr los métodos en este orden.

## Orden de ejecución
1. `MechaCyberpunk.Vehiculos.Editor.CreadorRuedasT1331.Aplicar` (prefabs Carro_V01a, V01b, V05 y Patrulla).
2. `MechaCyberpunk.Arte.Editor.ColocarT1331Ajustes.Aplicar` (escena Ciudad_Prologo: toldos, Tino, estelas, carros, postes; informe en Logs/t1331-ajustes.txt).
3. `MechaCyberpunk.Arte.Editor.ColocarT1331Ajustes.Fotos` (docs/capturas/t-1331/, no guarda).
Pruebas: EditMode `AjustesT1331Pruebas` (Mundo) y `RuedasBlenderT1331Pruebas` (Vehiculos); `AjustesT1317Pruebas` ahora espera 140.

## Causas y qué se hizo
- Toldos: hay cuatro generadores (T-501 `Toldo_N`, T-508 `Toldo_Lona/Lamina` (apagados por S2tLimpieza), kit `Toldo` en viviendas, T-1320 `TL01_Toldo`). Los de T-501/T-508 se colgaron con rayos contra el
  muro de entonces; al cambiar edificios y calles (T-1102, T-1318, T-1320) quedaron sin fachada. `ToldosMalos` los detecta (sin pared de 2 m a 0.5 m a los lados, o a menos de 4.5 m del eje de una calle del grafo del tráfico) y `Aplicar` los quita
  (los que son parte de un prefab se apagan). Los creadores viejos no se tocaron (no se vuelven a correr).
- Tino: no pude ver la fachada sin Unity. La fachada exterior (`Frente_*_Fuera`) usa S2_Pared_X03 a 10 cm del muro tallado de Edificio_7 (T-531/T-811). `Aplicar` revisa la zona (x -78.8..-67.8, z 6..15):
  cambia materiales nulos, con shader de error o magenta por S2_Pared_X03 y apaga los planos delgados encimados (z-fighting). El informe dice qué encontró; si no encuentra nada, la causa es otra y hay que mirar la captura.
- Estela: la escena no tiene TrailRenderer. Tiene 185 LineRenderers (cables, series de focos) y 19 sistemas de partículas (Vapor_*). `EstelasRaras` quita TrailRenderer, líneas que no sean Cable*/Serie*, partículas con rastro o estiradas, vapor a más de 11 m y todo lo que use el shader TorreHaz.
  No pude confirmar cuál era; si el informe sale en 0, la estela es otra cosa (p. ej. el haz de la torre ya se quitó en T-1317, o es la losa AzoteaT1318).
- Carros sobre objetos: T-1317 puso colliders a 60 barriles que antes no tenían: carros estacionados que "pasaban" por barriles ahora se montan encima. El tráfico vivo ya revisa `EspacioLibre` (T-728) al aparecer, así que no cambió.
  `Aplicar` revisa cada `CarroViejo` de la ciudad con una caja orientada (sin contar los primeros 0.35 m de piso) y lo recorre a lo largo de su eje (hasta 15 m, mismo nivel de piso) o lo apaga si no hay lugar.
- Ruedas: V01a, V01b, V05 y la patrulla seguían con la llanta RU01 (T-742); T-1312 solo hizo V02, V03, V04, V06, V09, V10 y C19. `CreadorRuedasT1331` les pone la rueda de Blender de V09 con el radio de la llanta que reemplaza;
  `CreadorCarrosPiezasT742.Armar` la llama al final para que un rearmado no vuelva a RU01. Los CarroTrafico_V* son variantes de Carro_V* y lo heredan.
- Postes: intensidad 70 -> 140, alcance 12 -> 16 m, cono 80 -> 100° (`ColocarS2oLuz`; `PostesS2t.AnguloCono` sube a 100 porque PostesS2tPruebas lo usa de tope).

## Assets que faltan
- FBX de Blender con ruedas separadas para V07 y V08 (hoy las ruedas van pegadas a la carrocería y no giran).
- Rueda propia de V01/V05 si no gusta la de V09 (hay una sola rueda de Blender con textura de llanta: V09/V10).
- Playtest 17-nov-b (docs/playtest-17nov-b.md) para confirmar el punto de la estela y de la tienda de Tino.
