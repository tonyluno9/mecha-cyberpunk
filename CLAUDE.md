# Mecha Cyberpunk (título pendiente)

Juego de peleas de mechas en primera persona, desde la cabina, para PC con teclado y ratón.
Estética cyberpunk low-poly en una megaciudad inspirada en México.
Diseño en docs/diseno/, fase actual y tareas en docs/estado.md, decisiones en docs/decisiones.md.

**Unity 6000.3.25f1** (Unity 6.3), URP 17.3, Input System 1.20 (el Input Manager viejo está apagado), Test Framework 1.6.

## Equipo y modo de trabajo

Somos Tony y Alan. En documentos viejos, "compañero" o "compa" es Alan (D-021).
El trabajo se hace en **modo automático**: la persona dice "trabaja" o "haz lo que me toca" y Claude sigue
`.claude/skills/trabajar/SKILL.md`. Claude ejecuta, verifica, revisa e integra todas las tareas de esa persona
sin pedirle que compruebe nada; lo que solo un humano puede juzgar queda en docs/revision-<persona>.md
para la revisión nocturna. Si alguien dice "haz lo que indique el repo" o "haz todo lo que me toca", es lo mismo.

| Rol | Modelo | Archivo |
| --- | --- | --- |
| Orquestador (sesión principal) | Opus 5.5, medium | .claude/skills/trabajar/SKILL.md |
| Implementador | Sonnet 5.5, medium | .claude/agents/implementador.md |
| Verificador | Sonnet 5.5, medium | .claude/agents/verificador.md |
| Revisor | Opus 5.5, high | .claude/agents/revisor.md |
| Planificador | Opus 5.5, high | .claude/agents/planificador.md |

La guía para personas está en docs/ARRANQUE.md.

## Carpetas y dueños

| Carpeta | Dueño | Qué va |
| --- | --- | --- |
| Assets/_Proyecto/Core/ | Nadie: solo en tareas de Core | Contratos: daño, vida, peleadores, eventos de pelea, tipos de datos |
| Assets/_Proyecto/Mecha/ | Tony | Movimiento, cabina, entrada, prefab del jugador |
| Assets/_Proyecto/Combate/ | Tony | Golpe, bloqueo, vida del mecha |
| Assets/_Proyecto/UI/ | Tony | Pantallas de la cabina |
| Assets/_Proyecto/Arena/ | Alan | Jaula, luces, flujo de pelea (Arena/Flujo/) |
| Assets/_Proyecto/IA/ | Alan | Rivales |
| Assets/_Proyecto/Escenas/ | Por archivo | Pruebas_Tony.unity: Tony. Clandestino.unity y Pruebas_Alan.unity: Alan |
| Assets/_Proyecto/Tests/EditMode/`<Área>`/ y PlayMode/`<Área>`/ | Dueño del área | Pruebas |
| Assets/Arte/ | Quien diga la tarea | Modelos, texturas y audio finales (Git LFS) |
| ProjectSettings/, Packages/ | Nadie: solo en tareas de Core | Ajustes del proyecto y paquetes |
| herramientas/ | Nadie: solo en tareas de Core | Scripts de verificación |

Cada área tiene su asmdef (`MechaCyberpunk.<Área>`) y referencia a MechaCyberpunk.Core y paquetes de Unity.
Puede referenciar otra área del mismo dueño solo si la tarea lo pide.
Las áreas de dueños distintos nunca se referencian entre sí: se hablan por los contratos de Core/.
Los datos de cada área (assets de ScriptableObject) van en `<Área>/Datos/` y las herramientas de prueba en `<Área>/Pruebas/`.

## Ramas

- `main`: lo aprobado. Cambia solo en la revisión nocturna, con la aprobación de la persona (D-022).
- `<persona>/fase-N`: integración de cada persona en la fase N.
- `<persona>/T-xxx-nombre`: una rama por tarea, sale de la de integración y vuelve a ella.
- Nunca force push. Nadie toca las ramas ni las carpetas de la otra persona.

## Reglas de trabajo

1. Las tareas viven en docs/tareas/ (pendientes, en-curso, hechas, bloqueadas). Una tarea, una rama.
2. Solo se tocan las carpetas permitidas de la tarea.
3. Core/, ProjectSettings/, Packages/ y herramientas/ solo se cambian en una tarea de Core. Si falta algo de Core,
   se escribe esa tarea y se sigue con otras.
4. Nada está verificado sin evidencia: una prueba que corrió, un valor medido en Unity o una captura revisada.
   Las tareas jugables llevan pruebas de PlayMode con entrada simulada.
5. En modo automático, cada persona escribe solo en sus archivos: docs/estado-<persona>.md,
   docs/decisiones-pendientes-<persona>.md, docs/revision-<persona>.md. docs/estado.md y docs/decisiones.md
   se actualizan en la revisión nocturna.
6. Escenas, prefabs y assets se crean con código de editor o con la Unity CLI, nunca escribiendo su YAML a mano.
7. Convenciones de C# en .claude/rules/unity-csharp.md.
8. Cada asset va con su .meta en el mismo commit. Una carpeta vacía lleva un `.gitkeep` para que su .meta no se pierda.
9. Cambia de rama con Unity cerrado. La primera vez en una máquina: `git lfs install` y `git lfs pull`.
10. Si ProjectSettings/ProjectSettings.asset solo cambió en `SENTIS_ANALYTICS_ENABLED`, se descarta con `git restore`:
    el editor lo agrega y el modo batch lo quita.
11. Lo propio de cada máquina (ruta de Unity, versión de la Unity CLI) va en `.claude/maquina.local.md`, que no se sube.

## Pruebas desde la terminal

La forma normal, cuando exista (T-002): `bash herramientas/verificar.sh todo`, y el editor vivo con
`bash herramientas/unity-vivo.sh`. Detalles en docs/herramientas-verificacion.md.

Comando base, con Unity cerrado, desde la raíz del repo (Bash; Unity.exe espera a terminar):

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
- Si Unity está en otra ruta, búscala en C:/Program Files/Unity/Hub/Editor/ o con `unity editors --installed`,
  y anótala en .claude/maquina.local.md.

## Si Unity está abierto

Si existe `Temp/UnityLockfile` y el log dice "another Unity instance is running with this project open", la persona
tiene Unity abierto. No borres el lockfile ni cierres Unity a la fuerza: anótalo en docs/revision-<persona>.md y
detente. Un editor que lanzaste tú con herramientas/unity-vivo.sh sí lo cierras tú con `detener`.
