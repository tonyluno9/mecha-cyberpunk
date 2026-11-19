CONTEXTO COMÚN (sesión B, 19-nov: Cerro y Puerto iguales a sus referencias)

- Proyecto Unity 6000.3 + URP 17.3 "mecha-cyberpunk": juego cyberpunk low-poly en una megaciudad inspirada en México.
  Lee CLAUDE.md y .claude/rules/unity-csharp.md de tu worktree, y docs/brief-zonas-19nov.md (reglas de las rondas anteriores, siguen valiendo).
- Tu worktree NO tiene Library: NO abras Unity ni compiles. Escribe código de editor con mucho cuidado (solo APIs reales de Unity 6;
  copia los `using` y el namespace `MechaCyberpunk.Arte.Editor` de los scripts vecinos). El orquestador corre tus métodos con
  -executeMethod en `.sesiones/s2`, revisa las fotos y te devuelve los errores y las capturas.
- Crea tu rama desde main: `git fetch origin && git checkout -b tony/T-14xx-<nombre> origin/main`.
- Referencias (no están en git): C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\referencias\look\
  K13_ColoniaCerro.png, A13_ColoniaCerro_Aerea.png, K15_Puerto.png, A15_Puerto_Aerea.png. Míralas con Read.
  Comparación actual (izquierda referencia, derecha juego): C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\docs\capturas\_vista19\cerro-k13.jpg,
  cerro-a13.jpg, puerto-k15.jpg, puerto-a15.jpg. Imágenes de los modelos: C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\modelos-nuevos\ (nuevos-17nov, nuevos-18nov).
- SOLO puedes cambiar estos prefabs (y lo que está dentro): Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab y T1357_Puerto.prefab.
  La luz global, el postproceso (volúmenes globales), los materiales compartidos (no edites ningún .mat existente: si necesitas uno, crea el tuyo
  en Assets/Arte/Mundo/Cerro/ o Assets/Arte/Mundo/Puerto/), la escena Ciudad_Prologo.unity, el Malecón, la Autopista y las Uniones son de la sesión A.
  Si necesitas algo de eso, NO lo hagas: escríbelo en tu respuesta final y el orquestador lo pasa a docs/para-sesion-A-19nov.md.
- El prefab es la fuente de verdad. NO vuelvas a correr ColocarT1355Cerro/ColocarT1357Puerto.Aplicar (borran arreglos de T-1360..T-1416).
  Edita el prefab con PrefabUtility.LoadPrefabContents / SaveAsPrefabAsset / UnloadPrefabContents. Todo lo nuevo va bajo un hijo raíz
  "T14xx_<Algo>" que tu Aplicar borra y rehace (se debe poder correr dos veces). Si apagas o mueves cosas viejas, hazlo por ruta exacta,
  de forma reversible (como T1416: lista de apagados que Aplicar vuelve a encender antes de rehacer) y anótalo en el log.
  Lee los scripts que ya editan tu zona (Assets/Arte/Editor/ColocarT1355Cerro*.cs, ZonaCerroT13xx/T14xx.cs, AereasT1414.cs, ColocarT1357Puerto.cs,
  ZonaPuertoT13xx.cs, AsentarZonasT1370.cs, BugsZonasT1410.cs) para entender el trazado, las cámaras y los ayudantes (ColocarS2cLook.Foto,
  ColocarS2.LadoALado, cables, fachadas, asentado).
- Assets: props de zona Assets/Arte/Zonas/<CC|PU|CH|IN|LO|ML|PZ|AR|AU>/*.prefab (CC01, CC02, CC04_Tendedero, CC05_Lavadero, CC06_a/_b, CC07_a/_b,
  CC08_MuroContencion_a/_b; PU01_Grua, PU02_Barco, PU03/PU04_Bodega, PU05_Montacargas, PU06_Pallets, PU07_PlumaCaseta_a/_b, PU08_ContenedorGraffiti_a/_b),
  módulos Assets/Arte/Zonas/Modulos/ y Assets/Arte/ZonasModulos/, carros Assets/_Proyecto/Vehiculos/Prefabs/Carro_*.prefab,
  personajes Assets/Arte/Personajes/NP07..NP14 (fbx _a/_b con su .controller) y los de antes. Busca con Glob antes de suponer una ruta.
  Cárgalos con AssetDatabase.LoadAssetAtPath; si alguno es null, anótalo en el log y sigue (no truenes). Lo que no exista va a tu respuesta
  (el orquestador lo pasa a docs/assets-faltan.md).
- NADA de primitivos visibles (CreatePrimitive). Solo colliders invisibles y losas/pisos generados por malla con material de textura.
- NPCs de adorno: instancia del fbx con su Animator y controller, sin lógica de juego, asentados en el piso, sin collider que estorbe el paso.
- Luz: la escena ya está cerca del tope de luces (≈1340 de 1400). Máximo 8 Light nuevas por tarea (spot de sodio sin sombras,
  color (1, 0.55, 0.2)); todo lo demás (faroles lejanos, neones, ventanas, balizas) con emisión (material propio con _EMISSION y
  globalIlluminationFlags = RealtimeEmissive). Cuenta las Light del prefab antes y después y anótalo.
- CERO BUGS: cada objeto se asienta con Physics.Raycast hacia abajo contra los colliders del prefab (en el prefab cargado usa
  PhysicsScene del contenido o calcula con los bounds de los colliders); nada flotando ni enterrado (tolerancia 5 cm); nada atravesado
  (Physics.OverlapBox o comparación de bounds antes de colocar); nada en el carril de las calles ni tapando puertas, escaleras o bocas;
  sin escalones de más de 0.3 m entre pisos caminables; sin muros invisibles (collider = malla visible); las azoteas y los contenedores
  deben quedar trepables (TreparAPie: tope de 0.5 a 2.3 m sobre los pies) y con salida. Deja `public static void Validar()` que revise
  eso en el prefab y escriba los hallazgos en Logs/t14xx-validar.txt (cuenta de flotantes, encimados, en carril, luces, objetos, MB).
- Rendimiento: 50 fps o más. Usa prefabs con LOD, marca lo estático como Static, sin sombras en luces chicas, combina mallas repetidas
  si son muchas, no más de ~3000 objetos nuevos por tarea y el prefab de zona en menos de 60 MB.
- Fotos: `public static void Fotos()`, que no guarda nada en el prefab ni en la escena. Abre la escena, pon la cámara con el MISMO ángulo,
  altura y encuadre que la referencia (reúsa las marcas de cámara que ya existen: K13 de T1416, A13/A15 de AereasT1414, K15 de T1402/T1396;
  corrígelas si no coinciden) y arma el lado a lado con ColocarS2.LadoALado en docs/capturas/_vista19b/<zona>-<k13|a13|k15|a15>-t14xx.png.
  Las fotos se toman en una corrida de Unity aparte de Aplicar.
- Solo tocas: Assets/Arte/Editor/<TuScript>.cs (+ .meta con un GUID nuevo de 32 hex), materiales o prefabs chicos nuevos en
  Assets/Arte/Mundo/Cerro/ o Assets/Arte/Mundo/Puerto/ (creados por tu código, nunca YAML a mano; si tu código los crea, no los subas tú)
  y docs/tareas/hechas/T-14xx-<nombre>.md. No toques otros scripts, escenas, ProjectSettings ni código de juego.
- Haz commit seguido en tu rama (sin push). Al terminar responde con: los métodos a correr (nombre completo de la clase) y en qué orden,
  qué cambia contra la referencia punto por punto, los riesgos, lo que falte (assets o cosas de la sesión A) y la rama con su último commit.
