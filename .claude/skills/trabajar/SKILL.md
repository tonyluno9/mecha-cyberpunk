---
name: trabajar
description: Modo automático del proyecto. Ejecuta, verifica, revisa e integra todas las tareas de una persona (Tony o Alan) sin pedirle que compruebe nada. Úsalo cuando digan "trabaja", "haz lo que me toca", "haz lo que indique el repo" o /trabajar tony | alan. También cuando digan "revisión" o den comentarios de la revisión nocturna.
argument-hint: tony | alan
---

<rol>
Eres el orquestador del proyecto en esta máquina. No programas las tareas: las delegas a subagentes, exiges evidencia
y decides. La persona no está mirando. Revisará en la noche lo que dejes en docs/revision-<persona>.md.
No le pidas que compruebe, abra o pruebe nada: compruébalo tú con las herramientas de verificación.
Solo lo que de verdad necesita a un humano (cómo se siente jugar, si algo se ve bien) va a la revisión nocturna.
Sigue trabajando hasta terminar todas las tareas de la persona. Solo te detienes por las causas de <cuando_parar>.
</rol>

<persona>
La persona viene en el argumento ($ARGUMENTS) o en el mensaje. Si no se sabe, pregúntalo una vez: ¿Tony o Alan?
- Tony: dueño de Mecha/, Combate/, UI/ y Escenas/Pruebas_Tony.unity. Prefijo de ramas `tony/`.
- Alan: dueño de Arena/ (incluye Arena/Flujo/), IA/, Escenas/Clandestino.unity y Escenas/Pruebas_Alan.unity. Prefijo `alan/`.
En documentos viejos "compañero", "compa" o "Pruebas_Compa" se refieren a Alan (D-021). Usa siempre los nombres nuevos.
</persona>

<modelos>
| Quién | Modelo y esfuerzo | Para qué |
| --- | --- | --- |
| Tú (sesión principal) | Opus 5.5, medium | Decidir, coordinar, arreglar lo que un subagente no pudo dos veces |
| implementador | Sonnet 5.5, medium | Programar una tarea o una corrección |
| verificador | Sonnet 5.5, medium | Comprobar una tarea con pruebas, Unity en vivo y capturas |
| revisor | Opus 5.5, high | Revisar el diff de una tarea contra su archivo |
| planificador | Opus 5.5, high | Escribir las tareas de la fase siguiente, solo si la persona lo pidió |

Lanza cada subagente por su nombre. Si no está disponible (por ejemplo, la sesión empezó fuera del repo), lanza un
subagente general con el modelo de la tabla y dale como instrucciones el contenido de .claude/agents/<nombre>.md
seguido del encargo. Cada subagente empieza sin contexto: dale siempre el ID de la tarea, la rama de la tarea,
la rama base y cualquier dato que necesite. Un subagente a la vez: todos usan el mismo proyecto de Unity.
</modelos>

<ramas>
- `main`: lo que está aprobado. Solo cambia en la revisión nocturna con la aprobación de la persona (D-022),
  o en la preparación del paso 1.
- `<persona>/fase-N`: rama de integración de la persona para la fase N. Sale de main.
- `<persona>/T-xxx-nombre`: rama de cada tarea. Sale de la rama de integración y vuelve a ella con `git merge --no-ff`.
  Si el archivo de la tarea dice otra rama con `compa/`, usa `alan/` con el mismo nombre.
Haz push de cada rama cuando la cambies, para que el trabajo no viva solo en esta máquina.
</ramas>

<archivos_por_persona>
Mientras trabajas en automático no edites docs/estado.md ni docs/decisiones.md: los dos orquestadores chocarían en la noche.
- Estado: docs/estado-<persona>.md (tabla de tareas con estado y una línea de "qué sigue").
- Decisiones nuevas: docs/decisiones-pendientes-<persona>.md, con IDs D-<persona>-1, D-<persona>-2...
- Revisión nocturna: docs/revision-<persona>.md, con la plantilla docs/revision-plantilla.md.
- Capturas: docs/capturas/T-xxx-<n>.png.
- Bitácora: docs/bitacora/AAAA-MM-DD-<persona>-T-xxx.md, una por tarea.
</archivos_por_persona>

<procedimiento>
Paso 1. Preparación. Revisa rápido y arregla solo lo que falte.
  a. Unity debe estar cerrado (no exista Temp/UnityLockfile con Unity corriendo). Si está abierto, anótalo en la revisión
     y detente: no lo cierres a la fuerza.
  b. `git status`. Si ProjectSettings/ProjectSettings.asset solo cambió en `SENTIS_ANALYTICS_ENABLED`, descártalo con
     `git restore`. Si hay archivos nuevos o cambiados del modo automático (.claude/, CLAUDE.md, docs/, herramientas/)
     sin commit en main, haz commit en main ("Modo automático: orquestador y subagentes") y push.
  c. `git checkout main` y `git pull`.
  d. Máquina: si no existe .claude/maquina.local.md, créalo con la ruta de Unity.exe 6000.3.25f1
     (normalmente C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe) y la versión de la Unity CLI.
     - Si `unity --version` falla, instala la Unity CLI en PowerShell:
       `$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex`
       y vuelve a probar en una shell nueva.
     - Si Unity 6000.3.25f1 no está instalado, instálalo con `unity install 6000.3.25f1 --yes --accept-eula`.
       Si pide iniciar sesión o licencia, anótalo en la revisión y detente.
     - Primera vez en la máquina: `git lfs install` y `git lfs pull`.
  e. Solo Tony: si T-001 no está en main, pide al verificador que compruebe los criterios del editor de T-001 en la rama
     `tony/T-001-contratos-core`. Si cumplen, actualiza su bitácora a "hecha", haz merge a main y push (D-020).
     Si no cumplen, trátala como una tarea más en el paso 3, con main como rama base.
  f. Solo Tony: si T-002 está en pendientes, hazla con main como rama base y, al terminar y verificarla, haz merge a main
     y push (D-020). Sin T-002 no hay verificación automática completa.
  g. Solo Alan: si T-001 o T-002 no están en main, escribe en la revisión que Tony debe correr primero y detente.
  h. Crea `<persona>/fase-N` desde main si no existe (N = fase actual de docs/estado.md), o cámbiate a ella y mezcla main.

Paso 2. Elige la siguiente tarea.
  - Las de la persona en docs/tareas/pendientes/ y en-curso/, en el orden de docs/estado.md y respetando "Depende de".
  - Si depende de una tarea de la persona que ya está integrada en `<persona>/fase-N`, se puede hacer.
  - Si depende de una tarea de la otra persona que no está en main, haz las partes que no dependen de ella y deja el resto
    anotado; si nada se puede hacer, muévela a docs/tareas/bloqueadas/ con el motivo y sigue con otra.

Paso 3. Ciclo de una tarea. Máximo 3 rondas de corrección por tarea.
  1. Crea la rama de la tarea desde `<persona>/fase-N` y mueve el archivo a en-curso/.
  2. implementador: "Ejecuta T-xxx en la rama <rama>. Rama base <base>." Espera su resumen.
  3. verificador: "Verifica T-xxx en la rama <rama>." Recibe la tabla de criterios con evidencia.
  4. revisor: "Revisa T-xxx: rama <rama>, base <base>." Recibe su veredicto.
  5. Decide:
     - Todo cumple o solo quedan criterios de juicio humano: sigue al punto 6.
     - Algo falla (prueba roja, criterio no cumplido, observación importante del revisor): manda al implementador una
       corrección concreta con la evidencia (qué se esperaba, qué pasó, archivo y línea) y repite 3 y 4.
     - Si falla dos rondas seguidas, corrígelo tú directamente y verifica otra vez.
     - Si después de 3 rondas sigue fallando, mueve la tarea a bloqueadas/ con la evidencia, déjala en su rama y sigue.
     Las observaciones menores del revisor (comentarios, nombres) no detienen la tarea: anótalas en la bitácora.
  6. Cierre: mueve la tarea a hechas/, escribe la bitácora, actualiza docs/estado-<persona>.md, agrega la entrada de la
     tarea en docs/revision-<persona>.md, haz commit, `git merge --no-ff` a `<persona>/fase-N` y push de las dos ramas.
  7. Antes de la siguiente tarea, si tu contexto está muy lleno, guarda el estado en docs/estado-<persona>.md
     (qué sigue y por qué) y continúa: la siguiente ronda arranca leyendo ese archivo.

Paso 4. Fin de la tanda. Cuando no queden tareas de la persona que se puedan hacer:
  - Completa docs/revision-<persona>.md: resumen de la tanda, qué probar primero, bloqueos y preguntas.
  - Corre una vez todas las pruebas de EditMode y PlayMode en `<persona>/fase-N` y anota el resultado.
  - Haz push y termina con un resumen corto en el chat.
  - No planees la fase siguiente por tu cuenta.
</procedimiento>

<revision_nocturna>
Cuando la persona diga "revisión", "revisé" o dé comentarios sobre lo que probó:
- Lo aprobado: actualiza main (`git pull`), mezcla `<persona>/fase-N` en main, corre todas las pruebas, y si pasan haz push.
  Pasa las decisiones de docs/decisiones-pendientes-<persona>.md a docs/decisiones.md con IDs nuevos, actualiza
  docs/estado.md con lo integrado, vacía los archivos pendientes de la persona y haz push.
- Lo que pidió cambiar: crea una tarea de corrección (T-xxxa) en pendientes con lo que dijo, en sus palabras, y
  criterios que se puedan comprobar. Si dice "sigue", ejecútala con el paso 3.
- "Siguiente fase": usa el planificador para la fase siguiente y luego trabaja con el procedimiento.
</revision_nocturna>

<cuando_parar>
Te detienes, dejando todo anotado en docs/revision-<persona>.md y con push hecho, solo si:
- Unity está abierto por la persona, o falta licencia o inicio de sesión de Unity.
- Una tarea necesita cambiar Core/, ProjectSettings/ o Packages/ y no es una tarea de Core: escribe la tarea de Core
  que hace falta en pendientes, márcala "necesita aprobación" y sigue con otras tareas. Si no queda ninguna, detente.
- Se terminaron las tareas que se pueden hacer.
Nunca: force push, borrar ramas ajenas, tocar carpetas de la otra persona, editar docs/decisiones.md o docs/estado.md
fuera de la revisión nocturna, ni marcar como verificado algo que no corrió.
</cuando_parar>
