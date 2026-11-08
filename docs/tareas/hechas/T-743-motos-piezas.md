# T-743 Motos V07, V08 y C19 con llantas RU02 (B2)
Dueño: Tony | Rama: tony/T-743-motos-piezas | Modelo: Sonnet 5.5, medium
Depende de: T-742, T-592, T-660, T-695 | Origen: docs/tareas-piezas-08nov.md (B2)

## Objetivo
Las tres motos llevan dos llantas RU02 separadas: giran con la velocidad y la delantera dobla con A y D. Sin puertas.

## Qué hay
- `ImportarS1nPiezas` ahora también mete `RU02MOTO.zip` (`Assets/Arte/Vehiculos/Piezas/RU02/RU02.fbx`) y `C19CUERPO.zip` (`Piezas/C19/C19_Cuerpo.fbx`).
- `Vehiculos/Editor/CreadorMotosPiezasT743.cs` (`ArmarTodas`, `ArmarPrefab`, `Armar`): el cuerpo sigue siendo la malla de `Visual_T511/Visual` (la que usa ManejoMoto para el caballete
  y la inclinación) y las llantas cuelgan de `Visual_T511/Ruedas_T743/Rueda_Del|Rueda_Tras/Direccion/Giro/Modelo`, que se inclina con la moto. Reutiliza `RuedasVisualesCarro` y `CalculoRuedasVisuales`
  (una sola delantera que dobla, dos que ruedan; ángulo máximo 28 grados parado y 10 rápido).
- C19: usa C19CUERPO (largo 2.0 m como antes, girado para mirar a +Z, llantas en sus ejes).
- V07 y V08: Tony aún no manda sus cuerpos (docs/tareas-piezas-08nov.md). Se usa su modelo de siempre y se le borran las llantas horneadas (los triángulos que caen por completo
  en un cilindro por llanta: 6002 en V07 y 4149 en V08); la malla queda en `Vehiculos/Datos/MallaMoto_V0x_SinLlantas.asset`. Cuando lleguen sus cuerpos se cambia la ruta en `CreadorMotosPiezasT743.Tabla`.
- La malla sin caballete (`MallaMoto_*_SinCaballete.asset`) se rehízo; `bajarAlOcultar` es 0 porque las llantas ya tocan el piso.
- Los prefabs de tráfico (CarroTrafico_V08) son variantes; los estacionados y robables de Ciudad_Prologo son instancias. Colliders, física, cámara, puntos del conductor y anclas de suspensión no cambian.
- `CreadorCarrosT511.CrearUno` rehace las motos por piezas si alguien lo corre.

## Límites
- El manubrio y la horquilla van en la malla del cuerpo y no giran con A y D (solo la llanta): ninguno de los cuerpos los trae como pieza aparte.

## Criterios
- [x] Dos llantas RU02 por moto con pivote en su centro, el eje a lo ancho, tocando el piso (EditMode)
- [x] Giran con W y al revés con S; solo la delantera dobla con A y D y vuelve al centro; con otro script moviéndola también (PlayMode, teclas simuladas)
- [x] Física, caballete, colliders, puntos del conductor y anclas intactos (EditMode y PlayMode)
- [x] Capturas `docs/capturas/s1e-T-743-*.png` revisadas

## Pruebas
- EditMode: MotosPorPiezasT743Pruebas (36). PlayMode: Vehiculos/RuedasMotosT743Pruebas (13).
