# Cómo arrancar el modo automático

Claude hace todo: programa, verifica en Unity, revisa e integra. Tú solo arrancas la sesión y, en la noche,
pruebas lo que quedó y le dices qué te pareció.

## Reglas para las personas
- Mientras Claude trabaja, no abras Unity en esta carpeta: lo usa él.
- Deja la terminal abierta y la computadora sin suspenderse. Si se acaba el límite de uso del plan,
  Claude espera solo y continúa cuando se renueva.
- Claude Code actualizado (`claude update`). Con la versión 2.1.283 o mayor arranca en modo automático
  y no te pide permiso para cada comando.

## Tony

En la terminal, dentro de la carpeta del proyecto:

```
claude
```

(Por defecto usa Opus 5.5 en esfuerzo medio, que es lo que necesita el orquestador.)

Solo la primera vez, para instalar el modo automático:

```
Copia el contenido de docs/modo-automatico/claude/ dentro de .claude/ (sobrescribe revisor.md), borra docs/modo-automatico/, y haz commit en main con todos los cambios sin commit (.claude/, CLAUDE.md, .gitignore, docs/) y push.
```

Luego, cada vez que quieras que trabaje, escribe:

```
/goal Sigue .claude/skills/trabajar/SKILL.md como Tony. Termina cuando no quede ninguna tarea de Tony en docs/tareas/pendientes ni en docs/tareas/en-curso (las que no se puedan hacer quedan en docs/tareas/bloqueadas con su motivo), y docs/revision-tony.md esté completo y con push.
```

## Alan

Solo la primera vez, en una terminal, en la carpeta donde quieras el proyecto:

```
git clone https://github.com/tonyluno9/mecha-cyberpunk.git
cd mecha-cyberpunk
claude
```

Cada vez que quieras que trabaje (dentro de la carpeta `mecha-cyberpunk`, con `claude` abierto):

```
/goal Sigue .claude/skills/trabajar/SKILL.md como Alan. Termina cuando no quede ninguna tarea de Alan en docs/tareas/pendientes ni en docs/tareas/en-curso (las que no se puedan hacer quedan en docs/tareas/bloqueadas con su motivo), y docs/revision-alan.md esté completo y con push.
```

Claude instala lo que falte en tu máquina (Unity CLI, y Unity 6000.3.25f1 si no lo tienes). Si Unity te pide
iniciar sesión, hazlo una vez y vuelve a lanzar el `/goal`.

## En la noche
1. Espera a que Claude termine (o escribe `/goal clear` para pararlo).
2. Abre docs/revision-<tu nombre>.md: dice qué probar, en qué escena y qué observar.
3. Abre Unity en la rama `<tu nombre>/fase-N` y juega.
4. En la misma sesión de Claude escribe tu revisión, por ejemplo:
   `revisión: T-010 aprobada; T-011 la cámara se siente muy alta; sigue`
   Lo aprobado pasa a main. Lo que pidas cambiar se vuelve una tarea y Claude la hace.
5. Cierra Unity antes de que Claude vuelva a trabajar.
