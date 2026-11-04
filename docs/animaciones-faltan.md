# Animaciones que faltan (al día: 4-nov-2026)

Solo lo que NO cubren los 46 clips de T-560 (en Assets/Arte/Personajes/Animaciones/). Bájalos de mixamo.com con
**Format: FBX for Unity, Skin: Without Skin, 30 fps** y marca **In Place** si la opción aparece. Ponles exactamente el
nombre de archivo de la tabla y déjalos en la carpeta indicada, dentro de Assets/Arte/Personajes/Animaciones/. También
puedes dejarlos en imageassets/animaciones/ en un zip por grupo.
El importador (ImportadorMixamo) los deja solos como Humanoid y sin materiales. Hace bucle si el archivo termina en `_Bucle`.

| Para qué | Búscalo en Mixamo como | Archivo | Carpeta |
| --- | --- | --- | --- |
| Rodar con CTRL | Forward Roll | Loco_Rodar.fbx | Locomocion |
| NPC escribiendo en la compu | Sitting Typing | NPC_SentadoEscribiendo_Bucle.fbx | NPC |
| NPC con el celular | Texting (o Texting While Standing) | NPC_Celular_Bucle.fbx | NPC |
| Sacar el teléfono | Taking Phone Out (si existe) | NPC_TelefonoSacar.fbx | NPC |
| Hablar por teléfono | Talking On Phone | NPC_TelefonoHablar_Bucle.fbx | NPC |
| Profes y NPC platicando (dos distintas) | Talking | NPC_Platicar1_Bucle.fbx, NPC_Platicar2_Bucle.fbx | NPC |
| Recargado en la pared | Leaning On A Wall | NPC_RecargadoPared_Bucle.fbx | NPC |
| Viendo alrededor | Looking Around | NPC_MirarAlrededor_Bucle.fbx | NPC |
| Peatón con miedo en una balacera | Terrified | NPC_Aterrado_Bucle.fbx | NPC |
| Peatón huyendo | Running Scared (o Scared Run) | NPC_Huir_Bucle.fbx | NPC |
| Sentado (bancas, El Hacker en el casino) | Sitting Idle | NPC_Sentado_Bucle.fbx | NPC |
| Sentarse y pararse | Stand To Sit, Sit To Stand | NPC_Sentarse.fbx, NPC_Pararse.fbx | NPC |
| Festejar al ganar o conquistar una zona | Victory (o Cheering) | Gesto_Victoria.fbx | Gestos |
| Subir al carro | Entering Car (si existe) | Carro_Subir.fbx | Vehiculos |
| Bajar del carro | Exiting Car (si existe) | Carro_Bajar.fbx | Vehiculos |
| Manejar sentado | Driving (si existe) | Carro_Manejar_Bucle.fbx | Vehiculos |
| Moto | Busca "Motorcycle", "Motorbike" y "Biker"; baja lo que haya | Moto_<lo que sea>.fbx (`_Bucle` si se repite) | Vehiculos |

Notas:
- Si un clip no existe en Mixamo, sáltalo. La moto ya tiene una pose provisional por IK (PoseMoto, T-562) y rodar
  conserva el giro de siempre hasta que llegue Loco_Rodar.
- Después de bajarlos, alguien (Claude) tiene que engancharlos en ControladorBase y en el override de cada papel. No se
  conectan solos al Animator.
