# Integrar en Unity lo de Blender (9-nov noche) — CyberCity

Bitácora de Blender con todo el detalle: `imageassets/blender/_PENDIENTE_BLENDER/_REVISION/BITACORA.md` (leer el RESUMEN del final).
Vistas aprobadas por Tony: `_REVISION/*_vista_*.png` (comparar las capturas del juego contra ellas).
**Paso 0 en cada tarea:** cerrar Unity y Unity Hub de tu carpeta, borrar Temp/UnityLockfile, repetir si vuelve.

## SESIÓN A — Clips de PJ01 (carpeta principal)
Origen: `imageassets/blender/pj01_clips/PJ01_*.fbx` (81 clips, armature PJ01 de 78 huesos, 30 fps, sin malla, sin leaf bones).
Import: Scale Factor **1.8367**, Humanoid con el avatar de PJ01 (Copy From Other Avatar), como T-749. Reemplazan a los de Mixamo en el jugador.
- **A1 Carro y moto:** PJ01_Carro_Entrar/Manejar/Salir (postura nueva: más abajo, piernas al piso, manos en el volante) y
  PJ01_Moto_Subir/Manejar/Bajar. El final de la moto (Moto_Bajar y la transición al terminar de manejar) está pendiente en Blender:
  dejarlo lo mejor posible y anotarlo.
- **A2 Salto:** PJ01_Loco_SaltoInicio, **PJ01_Loco_SaltoAire SIN Loop Time** (12 cuadros), PJ01_Loco_SaltoAterrizaje, PJ01_Loco_Caida
  CON loop (solo caídas largas). Ajustar el CharacterController a altura de salto ~0.45 m y gravedad ~-16 (≈0.47 s en el aire).
- **A3 Golpes y muertes:** PJ01_Punos_* y PJ01_Muerte_*, PJ01_Dano_Levantarse_*. Traen desplazamiento de raíz: Bake Into Pose en XZ (o
  root motion si se ve mejor; decide y anota). La muerte rápida, como pidió Tony.
- **A4 Locomoción base:** PJ01_Loco_Idle, CaminarAtras/Der/Izq, GirarDer/Izq, Frenar, y sus versiones _ConImplante (se usan con IMP01 o
  IMP02 puestos, como caminar/correr).
- **A5 Rifle, cobertura e interacciones:** PJ01_Rifle_* para escopeta/fusil/francotirador; PJ01_Cobertura_* (son con pistola);
  PJ01_Inter_* (Inter_Telefono dura 37 s: recortarlo con eventos al tramo útil).
- **A6 Fusil y armas largas en la mano:** hijo de R_Hand, valores de partida posición (0.003, 0.068, 0.003) m, rotación (1.6, -169.3, -0.5)°.
  Ajustar escopeta y francotirador igual; actualizar AgarresArmas y sus pruebas.
- **A7 Cierre:** correr RecorridoDemoPruebas completo y revisar las 3 pruebas que fallan (CiudadCarros, PatrullaFisicaT707,
  BalasCiudadT711). Build Builds/Demo y Builds/Juego como CyberCity.exe.

## SESIÓN B — Armas, carros y mesera (copia .sesiones/s2)
- **B1 Armas en piezas** (`imageassets/blender/armas/`): W02.fbx (Armazon, Canon, Bomba), W03.fbx y W04.fbx (Armazon, Canon, Cargador, Mira).
  Reemplazan los modelos de escopeta, fusil y francotirador en la mano, en el piso y en el banco de Tino (las piezas reales se separan en el
  holograma; quitar las piezas holográficas simples de T-942). Cada pieza trae un 2.º material (Corte_Interior) en las caras del corte.
- **B2 Miras:** la pistola usa `Mira_PuntoRojo.fbx` como mejora "Mira" (su vidrio en material Transparent). Fusil y francotirador usan su
  propia pieza Mira. La escopeta NO lleva mira (quitar esa mejora para la escopeta). Mira_Holografica y Mira_Telescopica no se usan.
- **B3 Carros SIN puertas:** decisión de Tony. V01a, V01b, V05 y la patrulla usan la carrocería sin puertas y sin animación de puerta (no
  usar `imageassets/blender/puertas/`). Quitar el código de abrir/cerrar puerta de esos carros sin romper subir y bajar.
- **B4 Mesera R01:** `imageassets/blender/R01/R01.fbx` (ahora esqueleto mixamorig, charola pesada a mixamorig:LeftHand, ya no se dobla).
  Reemplaza a las meseras del casino; Humanoid con su propio avatar.
