# Prompt de arranque (sesión T-000b, Opus 5.5 en esfuerzo alto)

<contexto>
Proyecto nuevo de Unity 6 con URP y C#, para PC con teclado y ratón. Es un juego de
peleas de mechas en primera persona, desde la cabina, con estética cyberpunk low-poly.
Somos dos personas, Tony y su compañero, cada una con Claude Code en su computadora.
Opus 5.5 planea y escribe tareas; Sonnet 5.5 las ejecuta, una por sesión.
Cada sesión empieza con el contexto vacío, así que todo lo que el proyecto sabe
tiene que vivir en archivos del repo.
El diseño completo del juego está en docs/diseno/paquete-original.md.
La carpeta del proyecto ya tiene un repo de git iniciado, sin commits.
</contexto>

<objetivo>
Deja el repo listo para que dos agentes trabajen en paralelo sin pisarse, y escribe
las tareas de la fase 1. No escribas código del juego en esta sesión.
</objetivo>

<entregables>
1. .gitignore estándar de Unity (que excluya Library/, Temp/, Logs/, UserSettings/,
   obj/, Build/, *.csproj y *.slnx/*.sln generados) y .gitattributes con Git LFS para
   .fbx, .glb, .obj, .png, .jpg, .tga, .psd, .wav, .mp3 y .ogg. Corre git lfs install.
2. Carpetas Assets/_Proyecto/ con Core, Mecha, Combate, IA, Arena, UI, Tests/EditMode
   y Tests/PlayMode, más Assets/Arte/. Assembly definitions para el código y para cada
   carpeta de pruebas, con referencia al Unity Test Framework.
3. CLAUDE.md corto: qué es el juego en tres líneas, versión exacta de Unity (léela de
   ProjectSettings/ProjectVersion.txt), mapa de carpetas con su dueño, reglas de trabajo
   (una tarea por sesión, solo carpetas permitidas, Core/ solo en su propia tarea,
   verificar antes de cerrar, actualizar estado y bitácora), cómo correr las pruebas de
   EditMode desde la terminal en batchmode, y qué hacer si Unity está abierto y bloquea
   el proyecto.
4. .claude/rules/unity-csharp.md con convenciones de C#: nombres, un MonoBehaviour por
   archivo, lógica pura separada de los MonoBehaviour cuando se pueda, ScriptableObjects
   para los valores que se ajustan al balancear, sin números mágicos en el código.
5. En docs/diseno/, separa el paquete original en un archivo por bloque: concepto e
   historia, combate, ciudad, personaje y progresión, economía y misiones, mecha y
   taller, estética y HUD, demo. Copia el contenido sin inventar decisiones; donde el
   paquete dice "Propuesta", conserva la marca. Deja paquete-original.md como está.
6. docs/estado.md, docs/decisiones.md con las decisiones de esta sesión, las carpetas
   docs/tareas/pendientes, en-curso y hechas, docs/tareas/_plantilla.md y
   docs/bitacora/_plantilla.md, con las plantillas de abajo.
7. .claude/skills/tarea/SKILL.md con el prompt de sesión y .claude/agents/revisor.md
   con el revisor, ambos de abajo.
8. La tarea T-001 en pendientes: contratos de Core/ (recibir daño, información del daño,
   eventos de pelea, datos del mecha y del rival como ScriptableObjects), pensada para
   que las tareas de Tony y del compañero avancen en paralelo sin tocar Core/ después.
9. Las tareas de la fase 1 de la tabla de abajo, cada una con criterios de aceptación
   que se comprueben jugando y que dependan de T-001.
10. Una nota en docs/bitacora/ de esta sesión.
11. Primer commit en main con un mensaje claro. Si hay un remote configurado, haz push;
    si no, dime que falta.
</entregables>

<plantillas>

## Plantilla 1: prompt de sesión (va en .claude/skills/tarea/SKILL.md)

Agrégale un frontmatter con name: tarea y una description que diga que se usa para
ejecutar una tarea del proyecto por su ID, por ejemplo /tarea T-012.

```markdown
<contexto>
Juego de mechas en primera persona, Unity 6 con URP y C#. Las reglas están en CLAUDE.md.
Otra persona y otro agente trabajan al mismo tiempo en otras carpetas de este repo,
por eso solo tocas las carpetas que permite tu tarea.
</contexto>

<tarea>
Ejecuta la tarea $ARGUMENTS. Su archivo está en docs/tareas/pendientes/.
</tarea>

<como_trabajar>
1. Lee docs/estado.md y el archivo de la tarea. Lee el código que la tarea menciona antes de cambiarlo.
2. Mueve la tarea a docs/tareas/en-curso/ y cambia a la rama que indica.
3. Implementa solo lo que pide, dentro de las carpetas permitidas.
4. Escribe las pruebas que pide la tarea.
5. Verifica con una prueba real: compila y corre las pruebas de EditMode. Una prueba
   que no llegó a correr no cuenta. Si no puedes correr alguna, di cuál y por qué
   en vez de marcar la tarea como hecha.
6. Al terminar: mueve la tarea a hechas/, actualiza docs/estado.md, escribe la nota
   en docs/bitacora/ con la plantilla, y haz commit y push de la rama.
</como_trabajar>

<limites>
Sigue hasta terminar todo lo que pide la tarea. Solo para a preguntar si no puedes
seguir sin mí, o antes de algo difícil de deshacer: borrar archivos, force push,
tocar main o cambiar Core/.
No agregues funciones, archivos, refactors ni documentación que la tarea no pide.
Si crees que algo extra ayudaría, menciónalo al final en una línea.
Si la tarea contradice el código o está incompleta, para y explícame qué encontraste.
</limites>

<al_final>
Dime qué archivos cambiaste, qué verificaste y con qué resultado, qué no pudiste
verificar, y los pasos exactos para que yo lo pruebe en Unity.
</al_final>
```

## Plantilla 2: tarea (va en docs/tareas/_plantilla.md)

Los criterios de este ejemplo son ilustrativos; los reales salen del diseño.

```markdown
# T-012 Golpe y bloqueo
Dueño: Tony | Rama: tony/T-012-golpe-bloqueo | Modelo: Sonnet 5.5, medium
Depende de: T-001, T-011

## Objetivo
Qué existe al terminar y por qué importa para el juego, en una o dos frases.

## Contexto
Archivos relevantes, decisiones de diseño que aplican, enlace a docs/diseno/combate.md.

## Qué hacer
1. ...
2. ...

## Carpetas permitidas
Assets/_Proyecto/Combate/ y Assets/_Proyecto/Tests/EditMode/Combate/
No tocar: Core/, Arena/, IA/, ni escenas que no sean Pruebas_Tony.unity

## Criterios de aceptación (se comprueban jugando)
- [ ] Con clic izquierdo, el brazo derecho lanza un golpe visible
- [ ] Manteniendo clic derecho, los brazos se cruzan y el daño recibido baja
- [ ] No aparecen errores en la consola durante una pelea completa

## Pruebas automáticas
- EditMode: el bloqueo reduce el daño; el golpe no se repite antes de su recarga

## Fuera de alcance
- Contraataque, sonido, partículas
```

## Plantilla 3: nota de bitácora (va en docs/bitacora/_plantilla.md)

Una por sesión, con nombre AAAA-MM-DD-persona-T-xxx.md.

```markdown
# AAAA-MM-DD | Persona | T-xxx Nombre de la tarea
Modelo: Sonnet 5.5, medium. Tarea planeada por Opus 5.5 en la sesión de la fase N.
Resultado: hecha / parcial / bloqueada
Qué se hizo:
Qué se verificó y cómo:
Problemas y decisiones:
Siguiente paso:
```

## Plantilla 4: prompt de planeación para Opus (guárdalo en docs/tareas/_prompt-planeacion.md)

```markdown
<contexto>
Lee CLAUDE.md, docs/estado.md, docs/decisiones.md y docs/diseno/.
Dueños: Tony tiene Mecha/, Combate/ y UI/. El compañero tiene Arena/, IA/ y el flujo de pelea.
</contexto>

<objetivo>
Planea la fase [N]: [meta de la fase, tal como está en el plan].
</objetivo>

<entrega>
Escribe de 3 a 6 tareas en docs/tareas/pendientes/ con docs/tareas/_plantilla.md.
Cada tarea es para Sonnet 5.5 en esfuerzo medio: una o dos horas de trabajo, un solo
dueño, criterios que se comprueban jugando y pruebas de EditMode donde haya lógica pura.
Si dos tareas necesitan el mismo contrato de Core/, crea primero una tarea de Core/.
Anota en docs/decisiones.md cada decisión nueva. No escribas código del juego en esta sesión.
</entrega>

<al_final>
Lista las tareas con dueño, dependencias y el orden en que conviene hacerlas.
</al_final>
```

## Subagente revisor (va en .claude/agents/revisor.md)

```markdown
---
name: revisor
description: Revisa una tarea terminada contra su archivo de tarea antes del pull request. Úsalo cuando pidan revisar una tarea.
model: opus
effort: high
tools: Read, Grep, Glob, Bash
---
Revisas cambios que hizo otro agente. No editas archivos.
1. Lee el archivo de la tarea y el diff de la rama contra main (git diff main...HEAD).
2. Comprueba cada criterio de aceptación contra el código.
3. Busca cambios fuera de las carpetas permitidas, cambios en Core/, valores fijos
   que deberían ser datos, código que nunca se llama y pruebas que no prueban nada.
4. Responde con: criterios cumplidos, criterios dudosos con archivo y línea, riesgos,
   y si recomiendas aprobar.
```

</plantillas>

<fase_1>
Meta: puedo golpear a un rival que me golpea, y uno de los dos gana. Todo con cubos y
cápsulas (greybox), sin modelos finales.

| ID | Tarea | Dueño |
| --- | --- | --- |
| T-010 | Movimiento del mecha en primera persona: WASD y ratón, suave, con sensación de peso | Tony |
| T-011 | Cabina greybox: cámara dentro, marco de la cabina y brazos visibles | Tony |
| T-012 | Golpe y bloqueo, con ventana de tiempo para bloquear | Tony |
| T-013 | Vida del mecha y recibir daño | Tony |
| T-020 | Arena greybox del clandestino: jaula, piso, luces básicas | Compañero |
| T-021 | Rival básico: se acerca, golpea, recibe daño, cae | Compañero |
| T-022 | Flujo de pelea: conteo, pelea, KO, resultado, revancha | Compañero |

Prueba de cierre de la fase: cada uno juega 3 peleas completas sin errores en consola.
Escenas: Tony prueba en Pruebas_Tony.unity; el compañero es dueño de Clandestino.unity
y prueba en Pruebas_Compa.unity.
</fase_1>

<al_final>
Dime qué creaste, qué no pudiste hacer y qué tengo que revisar a mano en Unity.
</al_final>
