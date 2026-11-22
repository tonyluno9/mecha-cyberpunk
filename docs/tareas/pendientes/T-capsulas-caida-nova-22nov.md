# TAREA: Cápsulas de caída CP01 para todos los Nova (robots, perro y drones)

Origen: chat de Blender, 22-nov. Tony lo aprobó todo en Blender. Esta tarea es solo de Unity.
Prioridad: normal. Se agrega a pendientes.

## Reglas
- Paso 0, antes de tocar el proyecto: cierra Unity y Unity Hub, confirma que no quede ningún Unity.exe, borra Temp/ y UnityLockfile, y repite la revisión.
- NO modifiques los FBX ni los clips que entregó Blender. Todo se ajusta en Unity (import settings, prefabs, scripts, shader).
- No corras CreadorTodo.
- Nunca hagas force push. Haz merge --no-ff a main y push (autorizado por Tony).

## Qué es
Los enemigos Nova ya no caen solos del cielo. Ahora cae una cápsula (CP01) con el robot dentro:
1. La caja cae rápido y pesada desde 30 m y rebota un poco al pegar en el suelo.
2. Un segundo después la tapa sale disparada hacia arriba girando.
3. Las 4 paredes se abren hacia afuera como una flor, girando sobre su borde inferior exterior.
4. La tapa (mientras vuela) y luego las paredes y la base se vuelven holograma cian, parpadean y desaparecen.
5. El robot aparece en pose de héroe, la sostiene 1 s, se levanta y empieza a buscar (pasa a su IA normal: patrulla/búsqueda).
Cada enemigo usa la MISMA cápsula escalada a su tamaño.

## Archivos (rutas desde la raíz del repo mecha-cyberpunk)
Cápsula:
- Assets/Arte/Nova/CP01_Capsula/CP01_Capsula.fbx
  - Raíz vacía "CP01" con hijos CP01_Base, CP01_Tapa, CP01_Pared_Frente, CP01_Pared_Atras, CP01_Pared_Izq, CP01_Pared_Der.
  - Animación de objetos en los frames 1–90 (24 fps), al tamaño del E16 (≈5.4 m de alto).
  - Importar con Animation Type = Legacy o Generic (no Humanoid) y crear un AnimationClip "CP01_Abrir".

Clips de salida del robot (1 clip por enemigo, a 24 fps):
| Enemigo | Clip | Frames | Contenido |
|---|---|---|---|
| E15 Centinela | Assets/Arte/Personajes/PJ01/Clips/E15_Capsula_Salida.fbx | 1–122 | héroe 1 s + levantarse |
| E16 Pesado | Assets/Arte/Personajes/PJ01/Clips/E16_Capsula_Salida.fbx | 1–140 | héroe 1 s + levantarse |
| E18 Élite | Assets/Arte/Personajes/E18_EliteNova/Clips/E18_Capsula_Salida.fbx | 1–96 | aterrizaje de héroe + 1 s + levantarse |
| E17 Perro | Assets/Arte/Personajes/E17_PerroRobotNova/Clips/E17_Capsula_Salida.fbx | 1–98 | agachado 1 s + ladrar x2 |
Los clips de E15, E16 y E18 son de esqueleto mixamorig (Humanoid, igual que sus otros clips). El del E17 usa el esqueleto del perro (Generic, igual que E17_Caminar/Correr).
Modelos: los que ya están en Assets/Arte/Personajes/E15_CentinelaNova, E16_RobotPesadoNova, E17_PerroRobotNova y E18_EliteNova.

Drones (sin clip; se mueven por script). Usar los prefabs existentes:
- Assets/Arte/Vehiculos/Drones/Prefabs/Dron_DR04_a.prefab (explorador, 0.7 m)
- Assets/Arte/Vehiculos/Drones/Prefabs/Dron_DR01.prefab (0.8 m)
- Assets/Arte/Vehiculos/Drones/Prefabs/Dron_E13.prefab (1.1 m)
- Assets/Arte/Vehiculos/Drones/Prefabs/Dron_DR02_a.prefab (pesado, 1.8 m)
No usar NV01/NV03 (son naves de 9 a 12 m) ni CY07b_Dron (es un objeto de calle).

## Escala de la cápsula por enemigo (localScale de la raíz CP01)
| Enemigo | Escala |
|---|---|
| E16 Pesado | 1.00 |
| E18 Élite | 0.85 |
| E15 Centinela | 0.66 |
| E17 Perro | 0.45 |
| DR02_a | 0.70 |
| E13 | 0.46 |
| DR01 | 0.33 |
| DR04_a | 0.30 |
Al escalar la raíz también se escalan la altura de caída y el rebote. Está bien así.

## Línea de tiempo (frames a 24 fps, contados desde que empieza la caída)
- 1–12: la caja cae de 30 m (z = 30·(1−t²), aceleración). En el frame 12 toca el suelo: aquí poner sacudida de cámara cercana, polvo y sonido de impacto pesado.
- 14–20: rebotes pequeños (14, 16, 18, 20).
- 36–64: la tapa sale volando hacia arriba girando.
- 38–56: se abren las paredes: 0° en el 38, 8° en el 41, 100° en el 50, 90° en el 53, 97° en el 56. Hay un pequeño rebote al final.
- 39: el robot se hace visible dentro de la caja (antes está oculto).
- 12: arranca el clip del robot (*_Capsula_Salida); se reproduce desde el frame 12 de la caída.
- Al terminar el clip: el robot pasa a idle/búsqueda y se activa su IA. La cápsula ya desapareció.
- Drones: aparecen en el 39 a 0.1 m, se quedan quietos hasta el 52, suben a 1.6 m en el 64, siguen con un vaivén (1.9 / 1.7 / 1.9 / 1.75 m en los frames 76 / 88 / 100 / 112, con un giro en Y de ±10–20°) y pasan a su IA de vuelo normal (DronMundo / CalculoVueloDron).

## Efecto holográfico (NO viene en el FBX: hay que hacerlo en Unity)
Crear un shader o material de disolución holográfica (URP Shader Graph) con 2 parámetros:
- Holo (0→1): mezcla el material original con emisión cian (0.2, 0.9, 1.0) de intensidad 4. Opcionalmente se le agregan scanlines o fresnel.
- Fade (0→1): opacidad de 1 a 0 (transparente o dithered).
Tiempos (s = frame de inicio: tapa = 46; base y paredes = 58):
- s → s+3: Holo 0→1.
- s+5 … s+15: 6 parpadeos, uno cada 2 frames; Fade aleatorio entre 0.15 y 0.65.
- s+17: Fade 0.6.
- s+23: Fade 1 (invisible). En s+24, desactivar el GameObject de esa pieza.
Hacerlo en un script (CapsulaCaida.cs) que lea los tiempos, no con keys del clip, para poder reusarlo.

## Implementación sugerida
1. Prefab "CP01_Capsula" con Animation (CP01_Abrir), el material holográfico en las 6 piezas y el script CapsulaCaida.
2. CapsulaCaida: Iniciar(enemigoPrefab, escala) → instancia la cápsula escalada en el punto de spawn, la reproduce, oculta al enemigo hasta el frame 39, lanza el clip *_Capsula_Salida en el frame 12 (o al drone su vuelo), corre el holograma y al terminar activa la IA del enemigo y se destruye.
3. Agregar el estado "SalidaCapsula" al Animator de E15, E16, E18 y E17 como estado inicial cuando el enemigo llega por cápsula, con transición por tiempo de salida a Idle/Buscar.
4. Conectar los spawns de refuerzos Nova (donde antes estaba la caída del cielo E15/E16/E18_Caida_*) para que usen la cápsula. No borres los clips viejos de caída.
5. Sonidos, si existen en Assets/Arte/Audio: impacto pesado al aterrizar, siseo o pistón cuando sale la tapa, zumbido holográfico al desaparecer y CAL_DRON_ZUMBIDO_01 para los drones.

## Pruebas (EditMode/PlayMode)
- Para cada enemigo de la tabla: spawnea con cápsula. El robot no se ve antes del frame 39, termina en Idle con la IA activa y no queda ninguna pieza de la cápsula activa.
- La escala de la cápsula corresponde a la tabla.
- El robot no atraviesa el suelo durante la pose de héroe.
- El dron termina volando con su IA normal.
- Captura de pantalla de cada uno en docs/capturas/.

## Referencia en Blender
imageassets/blender/_REVISION_TONY/revision_PJ01_animaciones.blend, colección REV_CAPSULA. Ahí está todo montado y aprobado.
Bitácora: imageassets/blender/_PENDIENTE_BLENDER_2/_REVISION/BITACORA.md (sección "22-nov — Cápsula de caída CP01").
