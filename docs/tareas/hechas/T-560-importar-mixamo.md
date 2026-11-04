# T-560 Importar las 46 animaciones de Mixamo (paso 1 de docs/noche-B-tony.md)
Dueño: Tony | Rama: tony/T-560-importar-mixamo (sale de tony/anim-noche) | Modelo: Sonnet 5.5
Carpetas permitidas: Assets/Arte/Personajes/ (Animaciones/ y su Editor/), Assets/_Proyecto/Personaje/Visual/ (y su Editor/), Tests/EditMode/Personaje/, Tests/PlayMode/Personaje/.
No tocar: sonido, policía/tráfico, carros, mundo, UI, minimapa, CIC (Sesión A). No CreadorTodo.

## Qué
- Origen: imageassets/animaciones/Movimiento_base.zip (13 clips Loco_*) y Combate_a_pie.zip (33: Pistola_*, Rifle_*, Punos_*, Cobertura_*, Dano_*, Muerte_*, "X Bot@Rifle Crouch Walk").
- Descomprimir en Temp/ (no en Assets/), copiar a Assets/Arte/Personajes/Animaciones/<Grupo>/ con grupos Locomocion, Pistola, Rifle, Punos, Cobertura, Dano, Muerte.
  Quitar los puntos dobles ("Loco_Idle..fbx" → "Loco_Idle.fbx") y renombrar "X Bot@Rifle Crouch Walk" a Rifle_AgachadoCaminar.fbx.
- Importación con código de editor (AssetPostprocessor o método -executeMethod que fija ModelImporter): Humanoid (avatar propio de cada FBX o el del X Bot, lo que funcione mejor con el retargeting a los modelos de Assets/Arte/Personajes), sin materiales, clip con el nombre del archivo.
  Bucle en idle/caminar/correr/apuntar/apuntar-caminar/guardia/agachado; sin bucle en disparo, recarga, golpes, salto inicio/aterrizaje, frenar, giros, daño, muertes, levantarse, cobertura entrar.
  In Place: bake into pose de posición XZ y rotación Y en todos, salvo caídas y muertes si se ven mejor con root motion (anótalo).
- Prueba de EditMode: los 46 clips existen en su carpeta, son Humanoid y tienen el loop correcto.

## Hecho cuando
Compila, la prueba pasa, los 46 FBX y sus .meta están en el commit (FBX van por Git LFS si .gitattributes lo cubre).
