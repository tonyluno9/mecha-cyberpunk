# Herramientas de verificación (T-002)

Dos scripts en `herramientas/`, para correr con Git Bash desde la raíz del repo. Probados en la máquina de Tony
(Unity 6000.3.25f1, Unity CLI 1.0.0-beta.12, paquete com.unity.pipeline 0.8.0-exp.1).

## verificar.sh: compilar y pruebas

```bash
bash herramientas/verificar.sh {compilar|editmode|playmode|playmode-por-area|todo}
```

- Lee la ruta de Unity de `UNITY_EXE` o de la línea `Unity.exe:` (o `unity_exe:`) de `.claude/maquina.local.md`.
- Una línea por paso: `compilar OK`, `editmode 18/18`, `playmode 3/3`. Si falla: `<paso> FALLO ...` con las primeras
  líneas de `error CS` o los nombres de las pruebas que fallaron. Sale con 0 solo si todo pasó (`todo` se detiene en el primer fallo).
- Logs y resultados en `Logs/` (`compilar.log`, `editmode.log`, `resultados-editmode.xml`, etc.). Tarda ~1 min en total.
- Si hay un editor de Unity abierto, imprime `UNITY ABIERTO` y sale con 3 en vez de esperar.
  El `unity.exe` del Hub/CLI no cuenta; solo el de `...\Editor\Unity.exe`. Un `Temp/UnityLockfile` viejo sin proceso se ignora.
- Cada modo corre los ensamblados de pruebas de todas las áreas, unidos con `;` en `-assemblyNames`: cada
  `Tests/<Modo>/<Área>/MechaCyberpunk.Tests.<Modo>.<Área>.asmdef` cuya carpeta tenga al menos un `.cs`. Un área nueva con
  pruebas entra sola y una carpeta vacía no estorba. Sin `-assemblyNames`, `testables` de com.unity.inputsystem mete las
  pruebas del propio paquete (en PlayMode 2 más, ignoradas) y el conteo cambia.
- `bash herramientas/verificar.sh ensamblados` imprime, sin abrir Unity, la lista de EditMode y la de PlayMode, un nombre por línea.
- `bash herramientas/verificar.sh playmode-por-area` (T-141) corre PlayMode un ensamblado por llamada de Unity (la lista de
  `ensamblados`), con `Logs/resultados-playmode-<Area>.xml` y `Logs/playmode-<Area>.log` por área. Imprime una línea por área
  (`Economia 9/9`, `Arena FALLO 8/17 (fallaron 9)` con los nombres, `Mecha LIMITE 900 s`) y un total; sale con 0 solo si todas pasaron.
  El tope por área sale de una tabla en el script (`limite_de_area`): Progresion 2400 s, las demás 900 s; las líneas de éxito imprimen "(límite N s)". `LIMITE_AREA=<segundos>`, si se define, manda sobre todas. Al pasar el tope: al pasarlo cierra SOLO el Unity que lanzó (`taskkill //PID` de su PID de
  Windows, con su árbol), borra `Temp/UnityLockfile`, marca `LIMITE` y sigue con la siguiente. `AREAS="Economia Core"` (opcional,
  nombres separados por espacio) limita las áreas que corre. Un cuelgue en un área ya no tumba a las demás. Los otros modos no cambian.
- Una prueba nueva solo cuenta si su carpeta (`Tests/<Modo>/<Área>/`) tiene asmdef: sin él no la corre nadie. Si la lista de un
  modo queda vacía o `total=0`, el paso sale como fallo.

## unity-vivo.sh: editor vivo

```bash
bash herramientas/unity-vivo.sh iniciar [escena]
bash herramientas/unity-vivo.sh play
bash herramientas/unity-vivo.sh captura Logs/captura.png
bash herramientas/unity-vivo.sh eval 'return UnityEngine.Application.unityVersion;'
bash herramientas/unity-vivo.sh consola          # consola de Unity (errores y avisos)
bash herramientas/unity-vivo.sh detener-play
bash herramientas/unity-vivo.sh comandos         # lista de comandos de la CLI en JSON
bash herramientas/unity-vivo.sh detener
```

- `iniciar` lanza Unity en batchmode sin `-quit`, guarda el PID de Windows en `Logs/unity-vivo.pid` y espera hasta 3 min
  a que `unity command editor_status` diga `ready` (en esta máquina tarda ~20-40 s). El log queda en `Logs/unity-vivo.log`.
  Con escena usa `open_scene --path <ruta>` (la ruta es relativa a `Assets/`; esta ruta NO se probó, no hay escenas todavía).
- `detener` mata el árbol de procesos por PID (taskkill /T). Siempre ciérralo al terminar.
- Un editor lanzado así empieza con una escena vacía sin cámara: `captura` falla con "No camera found" hasta que haya una
  escena con cámara (ver recetas). Con escenas reales, usa `iniciar Assets/_Proyecto/Escenas/<escena>.unity`.
- La captura usa `screenshot` (vista Game). El script convierte la ruta a absoluta porque el editor resuelve las relativas
  desde su propia carpeta de trabajo.

### Lo que funcionó

`iniciar`, `play`, `captura` (imagen real 1280x720 con cubo, cielo y suelo), `eval` (versión de Unity, posición de un
objeto en Play), `consola`, `detener-play` y `detener` (no queda ningún Unity de editor vivo). Captura de prueba: `Logs/captura-prueba.png`.

### Lo que no funcionó o difiere de la tarea

- Los comandos reales no se llaman `play`/`screenshot --output` como suponía la tarea: son `editor_play`, `editor_stop`,
  `screenshot` (acepta `--output --width --height`), `eval`, `open_scene`, `console`. El script ya los usa.
- `capture_game_view` falla en batchmode ("No Game view render target"). Con `--source camera` sí funciona, pero guarda
  el archivo dentro de `Assets/` (ensucia el proyecto con .meta). Usa `screenshot`.
- `quit` de la CLI falla; el cierre es por PID. Al matar el editor quedan unos segundos entradas "zombi" de
  `AssetImportWorker` (HasExited=True) visibles en `Get-Process`; no son procesos vivos.
- Un `iniciar` justo después de un `detener` falló una vez ("Unity terminó antes de responder", sin causa en el log);
  repetirlo funcionó. Si pasa, espera unos segundos y reintenta.
- Si se canaliza la salida (`| tail`), Unity no debe heredar la tubería o el comando no termina: el script ya lo redirige a /dev/null.
- Con el editor corriendo en batchmode no se puede usar `verificar.sh` (UNITY ABIERTO): detenlo antes.
- Tras entrar o salir de Play con batchmode el editor puede dejar `ProjectSettings/ProjectSettings.asset` y
  `ProjectSettings/SceneTemplateSettings.json` modificados: descártalos (`git restore`, borrar el json) si no son parte de la tarea.

## Recetas (`unity command --project-path . ...`; `eval` recibe C# con `return`)

```bash
S=herramientas/unity-vivo.sh
# Escena de prueba en memoria (sin guardar nada en Assets/)
bash $S eval 'var sc=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,UnityEditor.SceneManagement.NewSceneMode.Single); var c=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube); c.name="CuboPrueba"; c.transform.position=new UnityEngine.Vector3(0,0,5); UnityEngine.Camera.main.transform.position=new UnityEngine.Vector3(0,1,0); return sc.rootCount;'
bash $S play
# Posición de un objeto en Play
bash $S eval 'return UnityEngine.GameObject.Find("CuboPrueba").transform.position.ToString();'
# Campo de un componente (tipo y campo públicos o accesibles)
bash $S eval 'return UnityEngine.GameObject.Find("Jugador").GetComponent<UnityEngine.CharacterController>().velocity.ToString();'
#   alternativa sin C#: unity command --project-path . get_serialized_fields --help
# Errores de consola
bash $S consola          # JSON con level (error/warn/log) y message; clear_console para limpiar
# Captura de la cámara del jugador (vista Game)
bash $S captura Logs/jugador.png
```

## Comandos de `unity command` en esta versión (160, nombres y descripción corta)

La lista completa con descripciones: `bash herramientas/unity-vivo.sh comandos` (editor vivo) o
`unity command --project-path . --detail compact`. Parámetros de uno: `unity command --project-path . --query <nombre> --detail full`.
Se pasan como `--parametro valor` o, en `eval`, el código como argumento.

    add_animator_layer add_animator_parameter add_animator_state add_animator_transition add_component add_scene_to_build 
    add_timeline_clip add_timeline_track apply_prefab_overrides attach_script audit audit_status bake_lighting 
    bake_navmesh bake_navmesh_surfaces bake_occlusion_culling batch build build_status cancel_lighting_bake 
    cancel_navmesh_bake cancel_occlusion_bake cancel_tests capture_game_view capture_scene_view cleanup_codereload 
    clear_baked_lighting clear_console clear_navmesh clear_occlusion_culling codereload_status console console_status 
    copy_asset create_animation_clip create_animator_controller create_asset create_folder create_gameobject 
    create_gameobjects create_prefab create_prefab_variant create_scene create_script create_timeline delete_asset 
    delete_gameobject editor_focus editor_pause editor_play editor_status editor_stop eval eval_file find_assets 
    find_gameobjects get_animation_clip get_animator_controller get_audio_settings get_authoring_root get_build_settings 
    get_component_properties get_graphics_settings get_import_settings get_input_settings get_lighting_settings 
    get_material_properties get_navmesh_settings get_performance_stats get_physics_settings get_player_settings 
    get_quality_settings get_runtime_pipeline_settings get_scene_hierarchy get_selection get_serialized_fields 
    get_shader_properties get_tags_layers get_time_settings get_timeline import_asset instantiate_prefab 
    lighting_bake_status list_build_profiles list_build_targets list_open_scenes list_shaders list_tests log menu 
    move_asset navmesh_bake_status occlusion_bake_status open_scene package_add package_list package_remove 
    package_resolve package_search package_status quit read_text_file recompile recompile_status reload_file 
    reload_file_editor_interpreter reload_file_player_interpreter remove_animation_curve remove_component 
    remove_scene_from_build rename_asset rename_gameobject report_evals revert_prefab_overrides run_script run_tests 
    runtime_status save_all save_prefab_contents save_scene screenshot search set_active set_active_scene 
    set_animation_curve set_audio_settings set_authoring_root set_autotick set_build_settings set_component_properties 
    set_graphics_settings set_import_settings set_input_settings set_layer set_lighting_settings set_material_properties 
    set_navmesh_settings set_parent set_physics_settings set_player_settings set_quality_settings 
    set_runtime_pipeline_settings set_selection set_serialized_field set_tag set_tags_layers set_target_framerate 
    set_time_settings set_timescale set_transform simulate_key simulate_pointer switch_build_target 
    switch_build_target_status test_status unpack_prefab wait_cancel wait_for wait_status write_text_file 
## Build de la demo (T-137)
Con Unity cerrado, desde la raíz del repo:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -projectPath . \
  -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.Construir -logFile Logs/build-demo.log
```

- Pone en Build Settings las escenas de `DatosEscenas_Demo` (CreacionPersonaje, Ciudad_Prologo, Flashback), el nombre
  "Mecha Cyberpunk (demo)" y pantalla completa nativa (Alt+Enter: ventana), y construye `Builds/Demo/MechaCyberpunk.exe`
  (Windows 64 bits). Sale con 0 si el build salió bien y con 1 si no; el resumen está en el log con `[Progresion] ConstruirDemo`.
- Solo Build Settings: `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.AplicarEscenas -quit`.
  En el editor: menú Mecha Cyberpunk > Demo.
- Comprobar el ejecutable: `Builds/Demo/MechaCyberpunk.exe -batchmode -nographics -logFile Logs/demo-player.log` unos segundos
  y luego cerrarlo; el log no debe tener `Exception` ni `Error` y debe decir que cargó CreacionPersonaje.
- La prueba de EditMode `BuildSettingsDemoPruebas` comprueba la lista de escenas.
