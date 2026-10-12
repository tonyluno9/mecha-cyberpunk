---
name: trabajar
description: Modo automático del proyecto, en dos turnos. Turno de día (Sonnet) ejecuta y verifica todas las tareas de una persona. Turno de noche (Opus) revisa a fondo todo lo del día, corrige y deja la revisión lista para la persona. Úsalo cuando digan "trabaja", "haz lo que me toca", "turno de día", "turno de noche", "revisa lo del día" o /trabajar.
argument-hint: dia|noche tony|alan
---

<rol>
Eres el orquestador del proyecto en esta máquina. No programas las tareas: las delegas a subagentes, exiges evidencia
y decides. La persona no está mirando. No le pidas que compruebe, abra o pruebe nada: compruébalo tú con las
herramientas de verificación. Solo lo que de verdad necesita a un humano (cómo se siente jugar, si algo se ve bien)
va a docs/revision-<persona>.md, que la persona lee cuando quiere.
Sigue trabajando hasta terminar tu turno. Solo te detienes por las causas de <cuando_parar>.
</rol>

<turno_y_persona>
El argumento o el mensaje dicen el turno (día o noche) y la persona (Tony o Alan). Si falta el turno, deduce:
"trabaja", "haz lo que me toca" = día; "revisa", "me voy a dormir", "turno de noche" = noche.
Si falta la persona, pregúntalo una vez.
- Tony: dueño de Mecha/, Combate/, UI/ y Escenas/Pruebas_Tony.unity. Prefijo de ramas `tony/`.
- Alan: dueño de Arena/ (incluye Arena/Flujo/), IA/, Escenas/Clandestino.unity y Escenas/Pruebas_Alan.unity. Prefijo `alan/`.
En documentos viejos "compañero", "compa" o "Pruebas_Compa" se refieren a Alan (D-021).
</turno_y_persona>

<modelos>
| Turno | Quién | Modelo y esfuerzo | Para qué |
| --- | --- | --- | --- |
| Día | Tú (sesión principal) | Sonnet 5.5, medium | Coordinar, decidir, arreglar lo que el implementador no pudo |
| Día y noche | implementador | Sonnet 5.5, medium | Programar una tarea o una corrección |
| Día y noche | verificador | Sonnet 5.5, medium | Comprobar con pruebas, Unity en vivo y capturas |
| Noche | Tú (sesión principal) | Opus 5.5, medium | Coordinar la revisión y decidir qué se corrige |
| Noche | revisor | Opus 5.5, high | Revisar a fondo el diff de cada tarea del día |
| Noche, solo si la persona lo pidió | planificador | Opus 5.5, high | Escribir las tareas de la fase siguiente |

En el turno de día no lances nunca el revisor ni el planificador, ni ningún subagente en Opus: el día es solo Sonnet.
Comprueba al empezar con qué modelo corre la sesión. Si no coincide con el turno, dilo en una línea y sigue.

Lanza cada subagente por su nombre. Si no está disponible (por ejemplo, la sesión empezó fuera del repo), lanza un
subagente general con el modelo de la tabla y dale como instrucciones el contenido de .claude/agents/<nombre>.md
seguido del encargo. Cada subagente empieza sin contexto: dale siempre el ID de la tarea, la rama de la tarea,
la rama base y cualquier dato que necesite. Un subagente a la vez: todos usan el mismo proyecto de Unity.
</modelos>

<ramas>
- `main`: lo aprobado por la persona. Solo cambia cuando la persona lo aprueba (D-022), o en la preparación del paso 1.
- `<persona>/fase-N`: rama de integración de la persona para la fase N. Sale de main.
- `<persona>/T-xxx-nombre`: rama de cada tarea. Sale de la rama de integración y vuelve a ella con `git merge --no-ff`.
  Si el archivo de la tarea dice otra rama con `compa/`, usa `alan/` con el mismo nombre.
Haz push de cada rama cuando la cambies, para que el trabajo no viva solo en esta máquina.
</ramas>

<archivos_por_persona>
No edites docs/estado.md ni docs/decisiones.md en automático: los dos orquestadores chocarían.
- Estado: docs/estado-<persona>.md. Tabla de tareas con columna "Estado": pendiente, en curso, hecha (sin revisar),
  revisada, bloqueada. Más una línea de "qué sigue".
- Decisiones nuevas: docs/decisiones-pendientes-<persona>.md, con IDs D-<persona>-1, D-<persona>-2...
- Revisión para la persona: docs/revision-<persona>.md, con la plantilla docs/revision-plantilla.md.
- Capturas: docs/capturas/T-xxx-<n>.png.
- Bitácora: docs/bitacora/AAAA-MM-DD-<persona>-T-xxx.md, una por tarea; el turno de noche le agrega su revisión.
</archivos_por_persona>

<preparacion>
Paso 1, en los dos turnos. Revisa rápido y arregla solo lo que falte.
  a. Unity debe estar cerrado (no exista Temp/UnityLockfile con Unity corriendo). Si está abierto, anótalo en la
     revisión y detente: no lo cierres a la fuerza.
  b. `git status`. Si ProjectSettings/ProjectSettings.asset solo cambió en `SENTIS_ANALYTICS_ENABLED`, descártalo con
     `git restore`. Si hay cambios del modo automático (.claude/, CLAUDE.md, docs/, herramientas/) sin commit en main,
     haz commit en main y push.
  c. `git checkout main` y `git pull`.
  d. Máquina: si no existe .claude/maquina.local.md, créalo con la ruta de Unity.exe 6000.3.25f1
     (normalmente C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe) y la versión de la Unity CLI.
     - Si `unity --version` falla, instala la Unity CLI en PowerShell:
       `$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex`
       y vuelve a probar en una shell nueva.
     - Si Unity 6000.3.25f1 no está instalado, instálalo con `unity install 6000.3.25f1 --yes --accept-eula`.
       Si pide iniciar sesión o licencia, anótalo en la revisión y detente.
     - Primera vez en la máquina: `git lfs install` y `git lfs pull`.
  e. Solo Tony, turno de día: si T-001 no está en main, pide al verificador que compruebe los criterios del editor de
     T-001 en la rama `tony/T-001-contratos-core` (ya la revisó Opus antes). Si cumplen, actualiza su bitácora a
     "hecha", haz merge a main y push (D-020). Si no, trátala como una tarea más con main como rama base.
  f. Solo Tony, turno de día: si T-002 está en pendientes, hazla con main como rama base y, al verificarla, haz merge a
     main y push (D-020). Queda "hecha (sin revisar)" y el turno de noche la revisa como las demás.
  g. Solo Alan: si T-001 o T-002 no están en main, escribe en la revisión que Tony debe correr primero y detente.
  h. Crea `<persona>/fase-N` desde main si no existe (N = fase actual de docs/estado.md), o cámbiate a ella y mezcla main.
</preparacion>

<turno_de_dia>
Objetivo: hacer y verificar todas las tareas de la persona que se puedan, solo con Sonnet.

Paso 2. Elige la siguiente tarea.
  - Las de la persona en docs/tareas/pendientes/ y en-curso/, en el orden de docs/estado.md y respetando "Depende de".
  - Si depende de una tarea de la persona ya integrada en `<persona>/fase-N`, se puede hacer.
  - Si depende de una tarea de la otra persona que no está en main, haz las partes que no dependen de ella y deja el resto
    anotado; si nada se puede hacer, muévela a docs/tareas/bloqueadas/ con el motivo y sigue con otra.

Paso 3. Ciclo de una tarea. Máximo 3 rondas de corrección.
  1. Crea la rama de la tarea desde `<persona>/fase-N` y mueve el archivo a en-curso/.
  2. implementador: "Ejecuta T-xxx en la rama <rama>. Rama base <base>." Espera su resumen.
  3. verificador: "Verifica T-xxx en la rama <rama>." Recibe la tabla de criterios con evidencia.
  4. Decide:
     - Todo cumple o solo quedan criterios de juicio humano: sigue al punto 5.
     - Algo falla: manda al implementador una corrección concreta con la evidencia (qué se esperaba, qué pasó,
       archivo y línea) y repite el punto 3.
     - Si falla dos rondas seguidas, corrígelo tú directamente y verifica otra vez.
     - Si después de 3 rondas sigue fallando, mueve la tarea a bloqueadas/ con la evidencia, déjala en su rama y sigue.
  5. Cierre: mueve la tarea a hechas/, escribe la bitácora, marca la tarea "hecha (sin revisar)" en
     docs/estado-<persona>.md, agrega su entrada en docs/revision-<persona>.md, haz commit,
     `git merge --no-ff` a `<persona>/fase-N` y push de las dos ramas.
  6. Si tu contexto está muy lleno, guarda en docs/estado-<persona>.md qué sigue y por qué, y continúa.

Paso 4. Fin del turno de día. Cuando no queden tareas que se puedan hacer:
  - Corre una vez todas las pruebas de EditMode y PlayMode en `<persona>/fase-N` y anota el resultado.
  - Completa docs/revision-<persona>.md: qué se hizo, qué quedó bloqueado y qué falta revisar de noche.
  - Haz push y termina con un resumen corto. No planees la fase siguiente.
</turno_de_dia>

<turno_de_noche>
Objetivo: revisar a fondo con Opus todo lo marcado "hecha (sin revisar)", corregir lo importante y dejar a la persona
una revisión clara para cuando despierte. No se integra nada a main.

Paso 2. Lista de revisión: todas las tareas de la persona con estado "hecha (sin revisar)" en docs/estado-<persona>.md,
en el orden en que se hicieron. Incluye T-002 si la persona es Tony y no se ha revisado.

Paso 3. Por cada tarea. Máximo 3 rondas de corrección.
  1. revisor: "Revisa T-xxx: rama <rama de la tarea>, base <commit o rama base que se usó>." Si la rama de la tarea ya
     no tiene diferencias útiles, pásale el rango de commits del merge en `<persona>/fase-N`.
  2. Decide con su veredicto:
     - Aprobada, o solo observaciones menores: anótalas en la bitácora y en la revisión, marca "revisada" y sigue.
     - Problemas importantes: crea una rama `<persona>/T-xxx-correccion` desde `<persona>/fase-N`, manda al
       implementador la corrección con el detalle del revisor, luego el verificador, y otra vez el revisor.
       Si falla dos rondas, corrígelo tú directamente. Si a las 3 rondas sigue mal, márcala "revisada con problemas",
       explica en la revisión qué falta y sigue con la siguiente.
     - Al aprobar una corrección: merge --no-ff a `<persona>/fase-N` y push.
  3. Agrega a la bitácora de la tarea la sección "Revisión de noche" con el veredicto y las correcciones.

Paso 4. Revisión de conjunto, después de revisar cada tarea:
  - revisor: "Revisa el conjunto de `<persona>/fase-N` contra main: consistencia entre tareas, referencias entre áreas,
    código muerto, pruebas que se pisan, datos fijos que deberían ser ScriptableObjects." Corrige lo importante igual que
    en el paso 3.
  - Corre todas las pruebas de EditMode y PlayMode en `<persona>/fase-N` y anota el resultado.

Paso 5. Deja lista la revisión para la persona (docs/revision-<persona>.md):
  - Arriba: qué probar primero, en qué escena, con qué teclas y qué observar. Solo lo que necesita a un humano.
  - Por tarea: verificado automático, veredicto del revisor, correcciones hechas, decisiones del agente.
  - Al final: preguntas que solo la persona puede contestar.
  Haz push y termina con un resumen corto.

Si la persona pidió "siguiente fase" en el mensaje, al final usa el planificador para la fase siguiente.
</turno_de_noche>

<aprobacion>
Cuando la persona diga "apruebo", "revisé" o dé comentarios después de probar:
- Lo aprobado: `git pull` de main, merge de `<persona>/fase-N` a main, corre todas las pruebas y, si pasan, push.
  Pasa las decisiones de docs/decisiones-pendientes-<persona>.md a docs/decisiones.md con IDs nuevos, actualiza
  docs/estado.md con lo integrado, vacía los archivos pendientes de la persona y haz push.
- Lo que pidió cambiar: crea una tarea de corrección (T-xxxa) en pendientes con lo que dijo, en sus palabras, y
  criterios que se puedan comprobar. El siguiente turno de día la ejecuta.
</aprobacion>

<cuando_parar>
Te detienes, dejando todo anotado en docs/revision-<persona>.md y con push hecho, solo si:
- Unity está abierto por la persona, o falta licencia o inicio de sesión de Unity.
- Una tarea necesita cambiar Core/, ProjectSettings/ o Packages/ y no es una tarea de Core: escribe la tarea de Core
  que hace falta en pendientes, márcala "necesita aprobación" y sigue con otras. Si no queda ninguna, detente.
- Se terminó el trabajo del turno.
Nunca: force push, borrar ramas ajenas, tocar carpetas de la otra persona, editar docs/decisiones.md o docs/estado.md
fuera de la aprobación, ni marcar como verificado algo que no corrió.
</cuando_parar>
