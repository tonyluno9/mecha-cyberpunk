---
name: tarea
description: Ejecuta una tarea del proyecto por su ID, de principio a fin, dentro de las carpetas que la tarea permite. Úsalo cuando pidan hacer o ejecutar una tarea, por ejemplo /tarea T-012.
argument-hint: T-xxx
---

<contexto>
Juego de mechas en primera persona, Unity 6 con URP y C#. Las reglas están en AGENTS.md.
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
