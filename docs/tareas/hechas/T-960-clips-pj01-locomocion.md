# T-960: clips nuevos de PJ01 y locomoción base (Sesión A, A4)

**Hecho.** Los 49 clips nuevos de `imageassets/blender/pj01_clips/` (BITACORA del 9-nov) están en `Assets/Arte/Personajes/PJ01/Clips/`, Humanoid con la descripción de PJ01, igual que T-749. Los 31 de antes no cambiaron (mismos bytes).

## Locomoción (A4)
- `Idle`, `GirarDer`, `GirarIzq` y `Frenar` son ahora un árbol 1D por `ConImplante`: 0 el clip de la base (el jugador lo cambia por el PJ01_ sin implante) y 1 su versión `PJ01_*_ConImplante`.
- El árbol de caminar/correr con implante usa `PJ01_Loco_CaminarAtras/Der/Izq_ConImplante` (antes repetía los de Mixamo).
- El jugador usa `PJ01_Loco_Idle`, `CaminarAtras`, `CaminarDer`, `CaminarIzq`, `GirarDer`, `GirarIzq` y `Frenar`. Los demás papeles no cambian (ConImplante se queda en 0).

## Importación
- Bucle propio de los clips de Blender (`ImportadorClipsPJ01.TieneBucle`): `Loco_SaltoAire` SIN bucle; `Carro_Manejar`, `Moto_Manejar` e `Inter_VerTelefono` con bucle; cada `_ConImplante` como su versión sin implante.
- **Decisión: Scale Factor 1.88, no 1.8367.** El pedido decía 1.8367 "como T-749", pero T-749, PJ01.fbx y los 31 clips que ya funcionan usan 1.88, y el avatar de cada clip copia la pose T de PJ01 a esa escala. Medido con la malla horneada: los clips nuevos dejan los pies a -0.5..1.8 cm del piso, igual que los de T-749 (Pistola_Idle 2.7 cm, Caminar 0.6 cm).

## Capturas (revisadas contra 05_vista_frente, 05_vista_lado y 05_vista_implantes)
`docs/capturas/pj01-locomocion-*.png`, `pj01-giros-frenar-*.png`, `pj01-escala-*.png` (herramienta `CapturasClipsPJ01`). Con implante abre los brazos como en la vista 05. Las piezas de implante de Unity (T-740) son otras que las de Blender; no es parte de esta tarea.

## Pruebas
EditMode `ClipsBlenderPJ01Pruebas` (2289/2289 en EditMode). PlayMode: `AnimatorPJ01T733Pruebas`, `VelocidadesAnimacionT652Pruebas`, `ControladorBaseVisualPruebas`, `SaltoMedidaT944Pruebas`, `ImplantesVisiblesPJ01T740Pruebas`, `VisualPersonajePruebas`: 32/32.
