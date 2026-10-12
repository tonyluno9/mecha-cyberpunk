# T-002 Verificación automática
Dueño: Tony (tarea de Core y herramientas) | Rama: tony/T-002-verificacion-automatica | Modelo: implementador, Sonnet 5.5 medium
Depende de: T-001
Integración: al terminar y verificarla, el orquestador la mezcla directo a main (D-020), porque Alan también la usa.

## Objetivo
Los agentes comprueban las tareas sin una persona: compilan, corren pruebas de EditMode y PlayMode con entrada simulada,
abren Unity sin ventana, entran a Play, leen valores de la escena y sacan capturas. Con esto el verificador reemplaza
la prueba manual en el editor, salvo lo que es de sensación.

## Contexto
- El proyecto ya tiene el paquete `com.unity.pipeline` en Packages/manifest.json: es el que deja que la Unity CLI
  controle un editor abierto (`unity command`).
- La Unity CLI se instala en la máquina, no en el repo (paso 1d del orquestador).
- CLAUDE.md, sección "Pruebas desde la terminal", tiene el comando de batchmode que ya funciona.
- Escenas: todavía no hay ninguna en Assets/_Proyecto/Escenas/.

## Qué hacer
1. Packages/manifest.json: agrega `"testables": ["com.unity.inputsystem"]` para poder usar `InputTestFixture`.
2. Assets/_Proyecto/Tests/PlayMode/MechaCyberpunk.Tests.PlayMode.asmdef: agrega la referencia
   `Unity.InputSystem.TestFramework`.
3. Prueba de ejemplo `Tests/PlayMode/Core/EntradaSimuladaPruebas.cs`: con `InputTestFixture`, agrega un teclado,
   presiona W, comprueba que `Keyboard.current.wKey.isPressed` es verdadero, suéltala y comprueba que es falso.
   Otra prueba crea un GameObject con un componente de prueba que se mueve con W durante 0.5 s y comprueba que avanzó.
   Es la guía que copia el implementador para las pruebas jugables.
4. `herramientas/verificar.sh` (Bash, lo corre Git Bash en Windows):
   - Lee la ruta de Unity de la variable `UNITY_EXE` o de la línea `unity_exe:` en `.claude/maquina.local.md`.
   - Subcomandos: `compilar`, `editmode`, `playmode`, `todo` (los tres en orden, se detiene en el primer fallo).
   - Escribe logs y resultados en Logs/ y al final imprime una línea por paso: `compilar OK`, `editmode 18/18`,
     `playmode 2/2`, o el fallo con las primeras líneas de `error CS` o de las pruebas que fallaron.
   - Sale con 0 solo si todo pasó. Detecta Unity abierto (Temp/UnityLockfile en uso) y lo dice en vez de esperar.
5. `herramientas/unity-vivo.sh`:
   - `iniciar [escena]`: lanza Unity en batchmode sin `-quit` (se queda vivo), guarda el PID en Logs/unity-vivo.pid,
     espera hasta que `unity command --project-path .` responda (máximo 3 minutos) y, si se dio una escena, la abre.
   - `play` y `detener-play`: entran y salen de Play con `unity command`.
   - `captura <archivo>`: `unity command screenshot --output <archivo> --width 1280 --height 720`.
   - `eval '<código C#>'`: pasa el código a `unity command eval` y muestra la respuesta.
   - `detener`: cierra ese editor por su PID y borra el archivo de PID.
   - Usa los nombres de comando reales que liste `unity command --project-path . --format json`; si alguno no existe
     en esta versión, adapta el script y anótalo.
6. Pruébalo de verdad: crea temporalmente una escena de prueba en Logs/ o en memoria, inicia el editor vivo, entra a Play,
   saca una captura a Logs/captura-prueba.png, mírala, lee con eval la versión de Unity y detén el editor.
   No dejes escenas de prueba en Assets/.
7. `docs/herramientas-verificacion.md`: cómo usar los dos scripts, qué comandos de `unity command` existen en esta
   versión (pega la lista), qué funcionó y qué no, y recetas cortas: leer la posición de un objeto en Play, leer un
   campo de un componente, revisar errores de consola, sacar una captura de la cámara del jugador.
8. CLAUDE.md, sección "Pruebas desde la terminal": agrega una línea que diga que la forma normal es
   herramientas/verificar.sh y que el editor vivo se usa con herramientas/unity-vivo.sh.

## Carpetas permitidas
Packages/manifest.json, Assets/_Proyecto/Tests/PlayMode/ (asmdef y Core/), herramientas/, docs/, CLAUDE.md.
No tocar: código de las áreas, ProjectSettings/ (salvo descartar el cambio de SENTIS_ANALYTICS_ENABLED).

## Criterios de aceptación (los comprueba el verificador)
- [ ] `herramientas/verificar.sh todo` sale con 0 e imprime compilar, editmode y playmode con sus números
- [ ] Las pruebas de EntradaSimuladaPruebas pasan en PlayMode
- [ ] `herramientas/unity-vivo.sh iniciar`, `play`, `captura`, `eval` y `detener` funcionan en esta máquina,
      y queda una captura de prueba que se ve como una imagen real del juego (no negra ni vacía)
- [ ] Después de `detener` no queda ningún proceso de Unity de este proyecto
- [ ] docs/herramientas-verificacion.md tiene la lista real de comandos de `unity command`

## Si algo no funciona
Si la Unity CLI no puede controlar el editor en esta máquina, deja verificar.sh funcionando con PlayMode y entrada
simulada (eso ya cubre la mayoría de criterios), documenta exactamente qué falló en docs/herramientas-verificacion.md
y en la revisión nocturna, y termina la tarea con lo demás.

## Fuera de alcance
- Código del juego, escenas del juego, cambios a los contratos de Core

## Bloqueada
Implementada en la rama tony/T-002-verificacion-automatica (pusheada), pero sin verificar: el clasificador de permisos denegó correr herramientas/verificar.sh y matar 3 AssetImportWorker huérfanos (PIDs 32504, 44408, 44440). Necesita que Tony autorice o lo haga, y decida sobre el cambio fuera de alcance en .gitattributes.
