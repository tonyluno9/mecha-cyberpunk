# Tareas Unity — robots Nova (revisión con Tony, 20-nov)

## Ya hecho desde Blender
- E16_b usaba las animaciones de los guardias: `Visual_E16_RobotPesadoNova_b.prefab` ahora usa `Override_E16`; agregada su fila en `ClipsNpcNovaT1506.cs` (tabla `Visuales`).
- Torreta del E16_a corregida (reemplaza `Assets/Arte/Personajes/E16_RobotPesadoNova/E16_RobotPesadoNova_a.fbx`, mismo .meta).
- Caminar con escudo del E16 aprobado: `Assets/Arte/Personajes/PJ01/Clips/E16_CaminarEscudo.fbx` (45 frames, loop). Copia en `imageassets/blender/nova_clips/`.
- Rig del E18_a corregido (mochila 100 % en Spine2; huesos de brazo centrados). Reemplaza `Assets/Arte/Personajes/E18_EliteNova/E18_EliteNova_a.fbx`, mismo .meta.
- Clips propios del E18 aprobados (en `Assets/Arte/Personajes/PJ01/Clips/`, copias en `nova_clips/`): `E18_Caminar.fbx` (32 frames, loop), `E18_Correr.fbx` (21 frames, loop), `E18_Golpe_Der.fbx` (49 frames, puños cerrados).
- Respaldos en `imageassets/blender/_REVISION_TONY/respaldos/`.

## Decisiones de Tony
- E16 (robot grande): su `E16_Caminar` está perfecto; además camina protegiéndose con el escudo (`E16_CaminarEscudo`). No camina al disparar: se detiene, apunta con el brazo del cañón y dispara.
- E15 (soldado blanco): `E15_Caminar` y `E15_Correr` se quedan. Su arma es el rifle `W10_ArmaNova`.
- E18 (soldado azul): SIN arma. Vuela (jetpack) y pelea cuerpo a cuerpo con puños cerrados, con `E18_Caminar`, `E18_Correr` y `E18_Golpe_Der`.
- Caminar de lado de los Nova: se queda `PJ01_Loco_CaminarDer/Izq`.

## Falta (Unity)
1. E18: rehacer el Avatar tras el arreglo del rig (Rig → Configure → Pose → Enforce T-Pose → Apply, o volver a Create From This Model). Si no, los brazos quedan ~5° desviados.
2. Ataque de cañón del E16: `ConductaGuardiaPesado` hoy solo empuja y golpea con el escudo. Fase de disparo solo para el E16: detenerse (`MoverPlano(Vector3.zero)`), girar hacia el jugador, aviso, `E16_Disparar_Canon`, disparar con el brazo extendido (~frame 10 de 37), volver a caminar. Nunca moverse mientras dispara.
3. E16 caminar con escudo: importar `E16_CaminarEscudo.fbx` (Rig igual que `E16_Caminar`, Loop Time) y usarlo al acercarse o al recibir disparos. Agregarlo en `Override_E16` y en `ClipsNpcNovaT1506.cs`.
4. E18 animaciones: importar `E18_Caminar`, `E18_Correr` y `E18_Golpe_Der` (Humanoid con el Avatar del E18; Loop Time en caminar y correr). En `Override_E18` y en `ClipsNpcNovaT1506.cs` (fila `E18_EliteNova_a`): `Loco_Caminar`/`_ConImplante` → `E18_Caminar`; `Loco_Correr`/`_ConImplante` → `E18_Correr`; golpe → `E18_Golpe_Der`. Velocidades: caminar 1.62, correr 4.53. Quitarle el arma: excluir al E18 de `EscalaNovaT1497` y de la prueba `Enemigo_ConArma_LlevaLaW10EnLaMano`.
5. E18 brazos: ya corregidos en los clips; solo revisar en el juego que no se metan al torso.
6. E18 comportamiento: volar hacia el jugador con el jetpack (`E18_Jetpack_Volar`), aterrizar cerca (`E18_Caida_Aterrizaje`) y pelear a golpes; se detiene para golpear.
7. E15: comprobar que la `W10` quede bien orientada en la mano con `E15_Caminar` y `E15_Correr`.
8. Probar E16_a y E18_a en el juego: torreta derecha, mochila sin deformarse, codos sin meterse, materiales intactos.
