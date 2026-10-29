# T-137 Escenas de la demo en Build Settings y build de Windows
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-137-build-demo | Modelo: Sonnet 5.5, medium
Depende de: T-136
**Espera A-F4.** Motivo: Build Settings referencia Ciudad.unity y Flashback.unity de Alan en su forma final, y la demo solo tiene sentido
como build cuando H6 pasa en el editor (T-136). El orquestador la deja en docs/tareas/bloqueadas/ hasta que T-136 esté hecha.
Cómo saber si llegó: T-136 está en docs/tareas/hechas/.

## Objetivo
La demo se juega como programa de Windows: arranca en la creación de personaje y pasa a la ciudad y al flashback con `SceneManager`
(por ruta), sin el editor. Es lo que se le da a la persona nueva de H6 y lo que se presenta en PC.

## Contexto
- concepto-historia.md, Plataforma: "la base es PC con teclado y ratón, jugable de principio a fin"; "la demo completa se juega en PC sin
  necesitar el visor".
- Hoy ProjectSettings/EditorBuildSettings.asset solo tiene Assets/Scenes/SampleScene.unity (de la plantilla, D-003 dice que no se usa).
- `CambioEscena` (T-124) carga por ruta: en el editor con `EditorSceneManager.LoadSceneAsyncInPlayMode`, en un build con
  `SceneManager.LoadSceneAsync`, que exige la escena en Build Settings. /Build/ y /Builds/ están en .gitignore.
- Es tarea de Core porque cambia ProjectSettings/ (regla 3 de CLAUDE.md). Propuesta: D-tony-74.

## Qué hacer
1. ProjectSettings/EditorBuildSettings.asset (con código de editor o la Unity CLI, nunca a mano): escenas en este orden y habilitadas:
   Escenas/Tony/CreacionPersonaje.unity (índice 0), Escenas/Ciudad.unity, Escenas/Flashback.unity. SampleScene sale de la lista.
2. ProjectSettings/ProjectSettings.asset: nombre del producto "Mecha Cyberpunk (demo)" (o el que diga Tony), pantalla completa a la
   resolución nativa con opción de ventana; nada más.
3. Un método de editor que construye el build de Windows de 64 bits en Builds/Demo/ (por ejemplo
   `Progresion/Prologo/Editor/ConstruirDemo.cs`, llamado con `-executeMethod` en batch) y su uso anotado en
   docs/herramientas-verificacion.md.
4. Una comprobación del build: correr el ejecutable en batch con `-batchmode -nographics -logFile` unos segundos y revisar que el log no
   tenga errores ni excepciones y que se cargó CreacionPersonaje.

## Carpetas permitidas
ProjectSettings/EditorBuildSettings.asset, ProjectSettings/ProjectSettings.asset (solo lo del punto 2),
Assets/_Proyecto/Progresion/Prologo/Editor/, docs/herramientas-verificacion.md
No tocar: Packages/, herramientas/ (salvo que Tony pida un script y entonces se anota), escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditorBuildSettings tiene exactamente las tres escenas en ese orden (leído con `EditorBuildSettings.scenes` en una prueba de EditMode
      de Core o del área)
- [ ] El build de Windows termina sin errores (log del batch) y el ejecutable arranca en batch con el log limpio y CreacionPersonaje cargada
- [ ] `git diff tony/fase-N --stat` solo muestra lo permitido; en ProjectSettings.asset solo cambia lo del punto 2 (y si aparece
      `SENTIS_ANALYTICS_ENABLED`, se restaura)

## Criterios de sensación (para un humano)
- Tony abre Builds/Demo/, ejecuta la demo y juega del título a "continuará": ¿todo funciona igual que en el editor, con buena fluidez?
  Después, la persona nueva de H6 la juega desde el build.

## Pruebas automáticas
- EditMode: comprobación de la lista de Build Settings.

## Fuera de alcance
- Instalador, íconos y arte final; builds de otras plataformas; VR
