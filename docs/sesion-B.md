# Sesión B — Sonido, demo y entrega (carpeta ../cyberfix-b, rama tony/sesion-b)
31-oct-2026. Corre al mismo tiempo que la Sesión A (carpeta principal mecha-cyberpunk, rama tony/fase-g). Lee también docs/sesion-A.md.

## Tus carpetas (solo estas)
Assets/Arte/Audio/, la carpeta del GestorSonidos y sus configs (ScriptableObject de volúmenes y lista de IDs), UI/Creditos/ (nueva),
una carpeta nueva Assets/_Proyecto/Herramientas/ModoVideo/, Assets/_Proyecto/Personaje/Visual/ (nueva, solo el sistema de cambio de modelo),
docs/bugs.md, docs/sonidos-faltan-enganchar.md, docs/creditos.md, docs/guia-para-jugar.md (sección Demo).
NO edites escenas, Barrio.prefab ni código de jugabilidad (Personaje/, Combate/, IA/, Pelea/, Progresion/, Economia/, Vehiculos/, Trafico/, Core/, resto de UI/):
eso es de la Sesión A. Si algo tuyo necesita estar en escena, créalo en ejecución desde el prefab del GestorSonidos (que ya está en escena) o anótalo para la Sesión A.

## Prioridades (en orden)
1. **Sonidos:** descomprime ../mecha-cyberpunk/Audio-20261031T070509Z-1-001.zip (237 WAV en carpetas por sección + CREDITOS_*.txt) a Assets/Arte/Audio/<Sección>/ con el nombre tal cual. No subas el zip. Los WAV van por LFS (revisa .gitattributes). Importación: efectos mono 3D comprimidos (Vorbis, calidad 70), AMB en streaming, UI 2D. Sincroniza la lista de IDs del GestorSonidos con los archivos (ARM_FUS, ARM_SNI, VEH_MOTO_MOTOR, MUN_HOLOGRAMA, MUN_SOLDADORA, AMB_MERCADO, AMB_NEON_ALTO...). Prende AMB por zona (calle de noche/día, callejón, casa, El Mercado, El Tianguis, Neón Alto, I+D+I, El Fox, VivaCoin, clandestino) creando las fuentes en ejecución por zona; MUS sigue apagado. Mezcla: voces y disparos por encima, ambientes bajos; volúmenes en el ScriptableObject. Haz una prueba que liste cada ID usado en el código y diga si tiene archivo; los IDs sin gancho en el código van a docs/sonidos-faltan-enganchar.md con el archivo y el método donde debería sonar (lo hace la Sesión A).
2. **Créditos:** lee los CREDITOS_*.txt y arma docs/creditos.md y una pantalla de créditos (UI/Creditos/) que se abre desde el título: equipo (Tony, Alan, Cesar, Miranda, Mirflur, Castillo), profes, herramientas (Unity, Claude, Tripo, Nano Banana, Mixamo) y licencias de sonidos y modelos. Si para abrirla desde el título hay que tocar la pantalla de título, anótalo en docs/bugs.md para la Sesión A.
3. **Modo video:** tecla F10 oculta el HUD, F11 cámara libre lenta (WASD + ratón, Shift rápido), F12 captura en alta resolución a Capturas/. Solo en Builds/Juego.
4. **Listo para los personajes 3D:** sistema en Personaje/Visual/ para cambiar el muñeco de prueba por un modelo humanoide de Mixamo en UN prefab por personaje, con su Animator (idle, caminar, correr, saltar, golpe, recibir golpe, caer, apuntar y disparar pistola y arma larga, recargar, cubrirse, rodar). Usa animaciones de prueba si no hay de Mixamo. Escribe docs/como-meter-personajes.md con los pasos (FBX de Mixamo → carpeta → un comando de editor). No cambies los prefabs del juego: la Sesión A los conecta al final si hace falta.
5. **Probar la demo como jugador nuevo** con entrada simulada sobre el build, de principio a fin, sin atajos; cada tope o falla va a docs/bugs.md con pasos, captura y en qué carpeta cae. Repite cuando la Sesión A mezcle cambios a main.
6. Al final de cada prioridad: regenera Builds/Demo y anota qué probar.

## Reglas para no chocar con la Sesión A
- Trabaja solo en esta carpeta (worktree). IDs de tarea T-470 a T-499. Rama tony/sesion-b; una rama por tarea que sale de ella.
- Antes de cada merge a main: git fetch, mezcla origin/main en tony/sesion-b, compila. Si hay conflicto en un archivo de la Sesión A, quédate con la versión de origin/main. Nunca corras CreadorTodo ni regeneres Barrio.prefab.
- Merge a main con push al terminar cada prioridad (yo, Tony, lo autorizo). Si el push es rechazado: git pull con merge y reintenta. Nunca force push.
- Sonnet para todo; Opus solo si algo falla 2 veces. Pruebas solo de lo que tocas. Commit y push cada vez que termines algo.
- Si se acaba el límite, espera y continúa. Desde el 12-nov solo bugs.
