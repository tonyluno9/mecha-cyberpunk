# Animaciones que faltan (al día: 5-nov-2026)

Solo lo que NO cubren los 46 clips de T-560 ni los 24 de T-621 (Carro, Interaccion, Skull, Jefe y Nova, en Assets/Arte/Personajes/Animaciones/). Bájalos de mixamo.com con
**Format: FBX for Unity, Skin: Without Skin, 30 fps** y marca **In Place** si la opción aparece. Ponles exactamente el
nombre de archivo de la tabla y déjalos en la carpeta indicada, dentro de Assets/Arte/Personajes/Animaciones/. También
puedes dejarlos en imageassets/animaciones/ en un zip por grupo.
El importador (ImportadorMixamo) los deja solos como Humanoid y sin materiales. Hace bucle si el archivo termina en `_Bucle`.

| Para qué | Búscalo en Mixamo como | Archivo | Carpeta |
| --- | --- | --- | --- |
| Rodar (agachado + ESPACIO) | Forward Roll (o Rifle Roll, Dive Roll) | Loco_Rodar.fbx | Locomocion |
| Agachado quieto sin arma (hoy: pose baja de Cobertura_Entrar, T-683) | Crouch Idle (o Crouching Idle) | Loco_AgachadoIdle_Bucle.fbx | Locomocion |
| Agachado caminando sin arma (hoy: Rifle_AgachadoCaminar con los brazos sueltos, T-683) | Crouched Walking (o Sneaking, Crouch Walk) | Loco_AgachadoCaminar_Bucle.fbx | Locomocion |
| El Hacker sentado programando en la compu (y NPC escribiendo) | Sitting Typing | NPC_SentadoEscribiendo_Bucle.fbx | NPC |
| Hablar por teléfono | Talking On Phone | NPC_TelefonoHablar_Bucle.fbx | NPC |
| Profes y NPC platicando (dos distintas) | Talking | NPC_Platicar1_Bucle.fbx, NPC_Platicar2_Bucle.fbx | NPC |
| Recargado en la pared | Leaning On A Wall | NPC_RecargadoPared_Bucle.fbx | NPC |
| Viendo alrededor | Looking Around | NPC_MirarAlrededor_Bucle.fbx | NPC |
| Peatón con miedo en una balacera | Terrified | NPC_Aterrado_Bucle.fbx | NPC |
| Peatón huyendo | Running Scared (o Scared Run) | NPC_Huir_Bucle.fbx | NPC |
| Sentado (bancas) | Sitting Idle | NPC_Sentado_Bucle.fbx | NPC |
| Sentarse y pararse | Stand To Sit, Sit To Stand | NPC_Sentarse.fbx, NPC_Pararse.fbx | NPC |
| Festejar al ganar o conquistar una zona | Victory (o Cheering) | Gesto_Victoria.fbx | Gestos |
| Subir a la moto y manejarla sentado | Busca "Motorcycle", "Motorbike" y "Biker"; baja lo que haya | Moto_Subir.fbx, Moto_Manejar_Bucle.fbx | Vehiculos |

Ya conectadas (T-621 a T-624, no hay que bajarlas): Carro_Entrar, Carro_Salir, Carro_Manejar_Bucle, Carro_Pitar (Honking Horn), Inter_AbrirPuerta,
Inter_Boton, Inter_Recoger, Inter_Telefono, Inter_VerTelefono_Bucle, Skull_Idle/Caminar/Burla, Jefe_Idle/Caminar/Ataque/Burla/Muerte y
Nova_Idle/Caminar/Correr/Punch/Barrido/Flexing/Muerte. Sentarse al volante usa Carro_Manejar; la moto sigue por IK (PoseMoto) hasta que lleguen los de Moto.

Notas:
- Si un clip no existe en Mixamo, sáltalo. La moto ya tiene una pose provisional por IK (PoseMoto, T-562) y rodar
  conserva el giro de siempre hasta que llegue Loco_Rodar. Mixamo no dio "Walking While Texting" ni "Taking Phone Out": el NPC con celular usa Inter_VerTelefono.
- Después de bajarlos, alguien (Claude) tiene que engancharlos en ControladorBase y en el override de cada papel. No se
  conectan solos al Animator.

## Revisión del jugador, 8-nov (T-724)

El jugador usa solo clips de Tony (carpeta Assets/Arte/Personajes/Animaciones, sin override propio ni clips de Tripo). Caminar y correr son Loco_Caminar y Loco_Correr
de Movimiento_base tal como vienen, a rapidez 1 y sin IK de pies. Lo que hoy se resuelve con código (procedural) y no con un clip, y qué bajar de Mixamo:

- Correr hacia atrás y a los lados: hoy Correr mezcla Loco_CaminarAtras/Der/Izq. Busca "Run Backward", "Left Strafe Run" y "Right Strafe Run" (Loco_CorrerAtras, Loco_CorrerDer, Loco_CorrerIzq, carpeta Locomocion).
- Rodar (ESPACIO agachado): `CubiertaYRodada` gira el modelo por código; falta Loco_Rodar (ya está en la tabla de arriba).
- Moto: el conductor se coloca por IK (`PoseMoto`); faltan Moto_Subir y Moto_Manejar_Bucle (tabla de arriba).
- Apuntar con la pistola: los brazos se llevan por IK (`ApuntadoIK`); según el playtest de 7-nov Tony lo arregla en Blender.
- Agachado quieto y cobertura mantenida: son una pose congelada de Cobertura_Entrar; faltan Crouch Idle y un clip de cobertura mantenida (Cover Idle).
- Ritmo de los pies: Loco_Caminar avanza 1.28 m/s y Loco_Correr 2.79 m/s tal como vienen y el jugador va a 4.5 y 7.5 m/s, así que los pies se deslizan. Si se ve mal, hay que
  bajar de Mixamo versiones más rápidas (o decidir la velocidad del cuerpo); no se aceleran los clips por código.

## Asalto a un tercero, 8-nov (T-728)

La cinemática del asalto usa Idle y Caminar/Correr de Skull (asaltante) y el Idle de la víctima. Faltan, de Mixamo, para que se vea el robo:

| Para qué | Búscalo en Mixamo como | Archivo | Carpeta |
| --- | --- | --- | --- |
| La víctima levanta las manos al ser asaltada | Hands Up (o Surrender, Being Robbed) | NPC_ManosArriba_Bucle.fbx | NPC |
| El asaltante apunta o amenaza con el arma | Pistol Aim / Threatening | Skull_Amenazar_Bucle.fbx | Skull |
| La víctima se asusta y retrocede | Terrified (ya está en la tabla de arriba como NPC_Aterrado_Bucle) | NPC_Aterrado_Bucle.fbx | NPC |
