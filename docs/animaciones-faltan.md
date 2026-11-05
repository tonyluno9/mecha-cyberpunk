# Animaciones que faltan (al día: 5-nov-2026)

Solo lo que NO cubren los 46 clips de T-560 ni los 24 de T-621 (Carro, Interaccion, Skull, Jefe y Nova, en Assets/Arte/Personajes/Animaciones/). Bájalos de mixamo.com con
**Format: FBX for Unity, Skin: Without Skin, 30 fps** y marca **In Place** si la opción aparece. Ponles exactamente el
nombre de archivo de la tabla y déjalos en la carpeta indicada, dentro de Assets/Arte/Personajes/Animaciones/. También
puedes dejarlos en imageassets/animaciones/ en un zip por grupo.
El importador (ImportadorMixamo) los deja solos como Humanoid y sin materiales. Hace bucle si el archivo termina en `_Bucle`.

| Para qué | Búscalo en Mixamo como | Archivo | Carpeta |
| --- | --- | --- | --- |
| Rodar con CTRL | Forward Roll | Loco_Rodar.fbx | Locomocion |
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
