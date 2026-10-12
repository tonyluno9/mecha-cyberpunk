# Cómo arrancar el modo automático

Claude trabaja en dos turnos. **De día** solo usa Sonnet 5.5: hace las tareas y las verifica en Unity.
**De noche** entra Opus 5.5: revisa a fondo todo lo del día, corrige y te deja la revisión lista.
Tú solo arrancas cada turno y, cuando quieras, pruebas lo que quedó y lo apruebas.

## Reglas para las personas
- Mientras Claude trabaja, no abras Unity en esta carpeta: lo usa él.
- Deja la terminal abierta y la computadora sin suspenderse. Si se acaba el límite de uso del plan,
  Claude espera solo y continúa cuando se renueva.
- Claude Code actualizado (`claude update`). Con la versión 2.1.283 o mayor arranca en modo automático
  y no te pide permiso para cada comando.
- Para cambiar de turno, cierra la sesión con `/exit` y abre otra con el modelo del turno.

## Tony

### Turno de día (Sonnet)
En la terminal, dentro de la carpeta del proyecto:
```
claude --model sonnet
```
Dentro de Claude:
```
/effort medium
```
```
/goal Turno de día como Tony: sigue .claude/skills/trabajar/SKILL.md usando solo Sonnet. Termina cuando no quede ninguna tarea de Tony en docs/tareas/pendientes ni en docs/tareas/en-curso (las que no se puedan hacer quedan en docs/tareas/bloqueadas con su motivo), todas las hechas estén integradas en la rama tony/fase-N con push, y docs/revision-tony.md esté actualizado.
```

### Turno de noche (Opus)
Si el turno de día sigue corriendo, escribe `/goal clear` y espera a que termine lo que está haciendo. Luego `/exit` y:
```
claude --model opus
```
Dentro de Claude:
```
/effort medium
```
```
/goal Turno de noche como Tony: sigue .claude/skills/trabajar/SKILL.md. Termina cuando ninguna tarea de Tony quede como "hecha (sin revisar)" en docs/estado-tony.md, la revisión de conjunto de tony/fase-N esté hecha, todas las pruebas de EditMode y PlayMode de esa rama pasen, y docs/revision-tony.md esté listo para mí con push.
```

## Alan

Solo la primera vez, en una terminal, en la carpeta donde quieras el proyecto:
```
git clone https://github.com/tonyluno9/mecha-cyberpunk.git
cd mecha-cyberpunk
```
Después usa los mismos turnos que Tony, cambiando "como Tony" por "como Alan", `tony/fase-N` por `alan/fase-N`,
`estado-tony.md` por `estado-alan.md` y `revision-tony.md` por `revision-alan.md`.
Claude instala lo que falte en tu máquina (Unity CLI, y Unity 6000.3.25f1 si no lo tienes). Si Unity te pide
iniciar sesión, hazlo una vez y vuelve a lanzar el `/goal`.

## Cuando quieras revisar tú
1. Abre docs/revision-<tu nombre>.md: dice qué probar, en qué escena y qué observar.
2. Con Claude detenido, abre Unity en la rama `<tu nombre>/fase-N` y juega.
3. Cierra Unity, abre Claude y escribe tu revisión, por ejemplo:
   `apruebo T-010 y T-013; en T-011 la cámara se siente muy alta`
   Lo aprobado pasa a main. Lo que pidas cambiar se vuelve una tarea para el siguiente turno de día.
