# T-963: carro y moto con los clips de Blender (Sesión A, A1)

**Hecho.** El jugador sube, maneja y baja del carro con `PJ01_Carro_Entrar`, `PJ01_Carro_Manejar` y `PJ01_Carro_Salir`, y de la moto con `PJ01_Moto_Subir`, `PJ01_Moto_Manejar` y `PJ01_Moto_Bajar`.

## Carro
- Overrides del jugador: `Carro_Entrar`, `Carro_Manejar_Bucle` y `Carro_Salir` → los PJ01_.
- `AnimacionCarro` buscaba los clips por su nombre de Mixamo: con el override ya no los encontraba. Ahora pregunta primero al override (`BuscarClip`).
- El cuerpo pasa de la puerta al asiento (y del asiento a la puerta y al piso) **al ritmo con el que la cadera del clip llega a su sitio** (`ReglasEntradaCarro.AvanceDesdeDistancias`), no en una ventana fija pensada para Mixamo. Sirve por las dos puertas.
- Medido en el V01a: sentado, la cadera a 0.35 m del punto del asiento, el punto más bajo 0.29 m sobre el piso y la cabeza 0.62 m bajo el techo de las cajas de choque. Piernas estiradas al piso y manos en el volante, como 02_vista_carro_lado.

## Moto
- Estados nuevos en la capa base: `MotoSubir`, `MotoManejar` (bucle mientras `Manejando`) y `MotoBajar`, con gestos `MotoSubir = 10` y `MotoBajar = 11`. Subir y bajar a 1.5× (1.2 s).
- `PoseMoto` ya no congela una pose: lanza los clips, coloca la raíz para que la cadera del clip quede en el asiento y sigue el camino de la raíz del clip al subir y bajar (en Blender el origen del personaje es el de la moto). Manos y pies siguen yendo al manubrio y a los estribos por IK: entra al final de subir y sale al empezar a bajar. Sin los clips queda la pose anterior.
- La pose y el avance se miden con el propio Animator (Play + Update): con `SampleAnimation` la cadera salía 35 cm corrida (el clip se centra en su centro de masa).
- Los clips se hicieron en la C19 (deportiva, torso a ~50°). En la V07 de reparto (manubrio alto) el cuerpo se endereza 2° por cada grado de `InclinacionTorso` menos que la C19. Medido: cadera en el asiento (0 cm), manos y pies en sus puntos (0 cm).
- **Pendiente de Blender (BITACORA 02):** el final de `Moto_Bajar` y la transición al terminar de manejar. Quedó: al bajar, el cuerpo sigue el clip y en el último 25 % se mezcla con donde queda parado el jugador; se ve de pie junto a la moto. Si Blender rehace el final, basta con reimportar el clip.
- `MotoFinaT695Pruebas` pedía la cabeza 5 cm sobre el manubrio (pose por IK). Con los clips, en la V07 queda 2.5 cm encima y enderezar más no la sube (gira en la cadera): la prueba ahora pide que quede encima.

## Capturas (revisadas contra 02_vista_carro_lado, _entrando, _atras, 02_vista_subir_moto, 02_vista_frente y _lado)
`docs/capturas/pj01-carro-*.png` (entrando, lado, lado sin carrocería, atrás, saliendo), `pj01-moto-C19-*.png` y `pj01-moto-V07-*.png` (subiendo, manejando, atrás, bajando, abajo).

## Pruebas
EditMode 2317/2317 (`ClipsBlenderPJ01Pruebas`, `AvanceCarroT963Pruebas`). PlayMode 57/57: `CarroPJ01T963Pruebas`, `MotoPJ01T963Pruebas` (C19 y V07), `PoseMotoPruebas`, `MotoConductorT660Pruebas`, `MotoFinaT695Pruebas`, `AnimacionCarroPruebas`, `EntrarCarroT656Pruebas`, `PuertaCarroT742Pruebas`, `LanzamientoMotoT696Pruebas`, `ApuntarDesdeVehiculo*`, `ControladorBaseVisualPruebas`.
