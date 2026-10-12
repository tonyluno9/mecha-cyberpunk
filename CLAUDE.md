# Mecha Cyberpunk (título pendiente)

Juego de peleas de mechas en primera persona, desde la cabina, para PC con teclado y ratón.
Estética cyberpunk low-poly en una megaciudad inspirada en México.
Diseño en docs/diseno/, fase actual y tareas en docs/estado.md, decisiones en docs/decisiones.md.

**Unity 6000.3.25f1** (Unity 6.3), URP 17.3, Input System 1.20 (el Input Manager viejo está apagado), Test Framework 1.6.

## Carpetas y dueños

| Carpeta | Dueño | Qué va |
| --- | --- | --- |
| Assets/_Proyecto/Core/ | Nadie: solo en tareas de Core | Contratos: daño, vida, peleadores, eventos de pelea, tipos de datos |
| Assets/_Proyecto/Mecha/ | Tony | Movimiento, cabina, entrada, prefab del jugador |
| Assets/_Proyecto/Combate/ | Tony | Golpe, bloqueo, vida del mecha |
| Assets/_Proyecto/UI/ | Tony | Pantallas de la cabina |
| Assets/_Proyecto/Arena/ | Compañero | Jaula, luces, flujo de pelea (Arena/Flujo/) |
| Assets/_Proyecto/IA/ | Compañero | Rivales |
| Assets/_Proyecto/Escenas/ | Por archivo | Pruebas_Tony.unity: Tony. Clandestino.unity y Pruebas_Compa.unity: compañero |
| Assets/_Proyecto/Tests/EditMode/`<Área>`/ y PlayMode/`<Área>`/ | Dueño del área | Pruebas |
| Assets/Arte/ | Quien diga la tarea | Modelos, texturas y audio finales (Git LFS) |
| ProjectSettings/, Packages/ | Nadie: solo en tareas de Core | Ajustes del proyecto y paquetes |

Cada área tiene su asmdef (`MechaCyberpunk.<Área>`) y referencia a MechaCyberpunk.Core y paquetes de Unity.
Puede referenciar otra área del mismo dueño solo si la tarea lo pide.
Las áreas de dueños distintos nunca se referencian entre sí: se hablan por los contratos de Core/.
Los datos de cada área (assets de ScriptableObject) van en `<Área>/Datos/` y las herramientas de prueba en `<Área>/Pruebas/`.

## Reglas de trabajo

1. Una tarea por sesión, con `/tarea T-xxx`. Las tareas viven en docs/tareas/.
2. Trabajas en la rama de la tarea, nunca en main. Solo tocas las carpetas permitidas de la tarea.
3. Core/, ProjectSettings/ y Packages/ solo se cambian en una tarea de Core. Si te falta algo de Core, para y dilo.
4. Verifica antes de cerrar: compila y corre las pruebas (comando abajo). Una prueba que no corrió no cuenta.
5. Al cerrar: mueve la tarea a docs/tareas/hechas/, actualiza docs/estado.md y escribe la nota en docs/bitacora/ con su plantilla.
6. Antes del pull request, pide la revisión del subagente `revisor`.
7. Convenciones de C# en .claude/rules/unity-csharp.md.
8. Cada asset va con su .meta en el mismo commit. Una carpeta vacía lleva un `.gitkeep` para que su .meta no se pierda.
9. Cambia de rama con Unity cerrado. La primera vez en una máquina: `git lfs install`.

## Pruebas desde la terminal

Con Unity cerrado, desde la raíz del repo (Bash; Unity.exe espera a terminar):

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -projectPath . \
  -runTests -testPlatform EditMode \
  -testResults Logs/resultados-editmode.xml -logFile Logs/editmode.log
```

- Código de salida: 0 pasaron, 2 alguna falló, otro valor es error. Si falla, busca `error CS` en Logs/editmode.log.
- Revisa `total` y `failed` en Logs/resultados-editmode.xml. Con `total="0"` no corrió ninguna prueba.
- PlayMode: igual con `-testPlatform PlayMode` y archivos `resultados-playmode.xml` y `playmode.log`.
- Solo compilar: `-batchmode -quit -projectPath . -logFile Logs/compilar.log` (sale con 1 si hay errores de compilación).
- En PowerShell, Unity.exe no espera: usa `Start-Process -Wait -PassThru` y lee `.ExitCode`.
- Si Unity está en otra ruta, la ruta exacta sale en Unity Hub > Installs.

## Si Unity está abierto

Si existe `Temp/UnityLockfile` o el log dice "another Unity instance is running with this project open", el proyecto está bloqueado.
No borres el lockfile ni cierres Unity por tu cuenta. Pide que guarden las escenas y cierren Unity, y vuelve a correr las pruebas.
Si no lo pueden cerrar, pide que corran las pruebas en Window > General > Test Runner y te pasen el resultado,
y anota en la bitácora que se verificaron así.
