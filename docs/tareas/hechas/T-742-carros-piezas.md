# T-742 Carros V01a, V01b y V05 armados por piezas (B1)
Dueño: Tony | Rama: tony/T-742-carros-piezas | Modelo: Sonnet 5.5, medium
Depende de: T-511, T-694, T-706, T-707 | Origen: docs/tareas-piezas-08nov.md (B1)

## Objetivo
Los tres carros dejan de ser una sola malla: carrocería sin puertas ni ruedas, dos puertas con bisagra y cuatro llantas RU01 que giran y doblan.
Sin vidrio, para ver al conductor al subir y manejar.

## Qué hay
- Importador `Assets/Arte/Editor/ImportarS1nPiezas.cs` (menú S1n > Meter piezas de carros): desempaca V01CARROZA, V01BCARROZA, V05CARROZA y RU01COCHE
  de `imageassets/modelos3d/piezas/` y copia las 6 puertas de `imageassets/blender/puertas/` a `Assets/Arte/Vehiculos/Piezas/<código>/`. Las puertas
  traen su textura incrustada en el .fbx (son modelos de Tripo aparte): se extraen a `<código>_Puerta_basecolor.jpg` y `_normal.jpg`. Mesh LOD en los de más de 15 mil caras.
- Constructor `Vehiculos/Editor/CreadorCarrosPiezasT742.cs` (`Armar(raiz, código)`): `Visual_T511` con `Carroceria`, `Puerta_Izq` y `Puerta_Der` (pivote en la
  bisagra delantera, hijo `Modelo`), `Rueda_xx/Direccion/Giro/Modelo` (RU01 con el pivote en su centro), `Asiento_Conductor` y `Punto_Puerta`
  (antes Asiento y PuntoSubida). La carrocería nueva mide lo mismo que el modelo viejo (V01a 4.2 m, V01b 4.5, V05 4.6; pies en y = 0); los cubos de choque,
  la física y la cámara no cambian. `CreadorCarrosT511.CrearUno` lo llama para estos tres códigos. `Trafico/Editor/CreadorPatrullaPiezasT742` hace lo mismo con Patrulla.prefab.
- `Vehiculos/Carro/RuedasVisualesCarro` (llantas giran con la velocidad; delanteras doblan con el volante si el carro tiene control, o con cuánto gira si lo mueve otro script),
  `CalculoRuedasVisuales` (lógica pura) y `PuertasCarro` (DOTween: abre la del lado por donde subes y cierra).
- `AnimacionCarro` (Personaje) avisa a la raíz del carro con SendMessage (`AbrirPuertaDelLado(x)`, `CerrarPuertas()`): abre al empezar a subir, cierra cuando el conductor ya se deslizó
  al asiento (90 % del clip); al bajar abre al empezar Carro_Salir y cierra al llegar al piso (85 %). Subir por atrás o por el techo no abre puertas.
- Reemplazo: los prefabs de tráfico (CarroTrafico_V01a/V01b) son variantes de Carro_V*, los estacionados de Ciudad_Prologo son instancias del prefab y la patrulla
  (Patrulla.prefab, con PatrullaMundo, sirena, robo y GPS) se rehízo con las mismas piezas. Los demás carros no se tocaron.

## Criterios
- [x] Prefabs con Carroceria, Puerta_Izq, Puerta_Der, 4 ruedas RU01, Asiento_Conductor y Punto_Puerta; pivotes en bisagra y centro de rueda (EditMode)
- [x] Mismo tamaño y mismos colliders (EditMode); física igual: W 6 s llega a 14 m/s, 4 ruedas en piso (PlayMode)
- [x] Llantas giran con W y al revés con S; solo las delanteras doblan con D y A y vuelven al centro; sin control (tráfico) también (PlayMode)
- [x] Puerta del lado por donde subes se abre y cierra con la E, también al bajar (PlayMode con jugador real y entrada simulada)
- [x] Capturas `docs/capturas/s1e-T-742-*.png` revisadas

## Pruebas
- EditMode: CalculoRuedasVisualesPruebas (14), CarrosPorPiezasT742Pruebas (50)
- PlayMode: Vehiculos/RuedasPuertasT742Pruebas (17), Personaje/PuertaCarroT742Pruebas (8)

## Notas
- Las medidas de ajuste (escala, X, Y y cara de afuera de cada puerta; radio y lugar de las llantas) están en `CreadorCarrosPiezasT742.Tabla`; se ven con `CapturasCarrosT742.Capturar` (Logs/t742-*).
- Si alguien corre `CreadorCarrosT511.Crear` los tres carros se rehacen por piezas solos. El aliado en carro (T-706) no abre puerta (no era parte de la tarea).
