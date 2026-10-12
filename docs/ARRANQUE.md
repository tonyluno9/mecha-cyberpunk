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

Tu fase actual es T-F2 (contratos de Core y pelea completa). Tu rama de integración es `tony/fase-2`; cambia `fase-N` por `fase-2` en el `/goal`.

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

Tú haces **el mundo**: la ciudad (un barrio de unas 6 manzanas), el clandestino y su arena, la casa con taller, el bar,
el casino, el mercado negro con clínica, los carros, el día y la noche, el tráfico, las persecuciones y el ambiente.
Tony hace el jugador y los sistemas (mecha, combate, rivales, dinero, progresión, guardado). Los dos se hablan por
contratos en `Assets/_Proyecto/Core/` y nunca editan la carpeta del otro.

### Qué es tuyo y qué no
- Tuyo: `Arena/`, `Mundo/`, `Vehiculos/`, `Casino/`, `Trafico/`, `Assets/Arte/` y las escenas `Clandestino.unity`,
  `Pruebas_Alan.unity`, `Ciudad.unity` y `Flashback.unity`. Tus ramas empiezan con `alan/`.
- De Tony: todo lo demás, incluido el prefab `MechaJugador`. Tú lo colocas en tus escenas, no lo editas.
- Si te falta algo de Core o de Tony, escríbelo en `docs/revision-alan.md`; el Claude de Tony lo vuelve tarea de Core.
- Tabla completa de dueños: `CLAUDE.md`. Decisiones: `docs/decisiones.md` (D-025 a D-032). Fases: `docs/hoja-de-ruta.md`.

### Tu primera fase: A-F1 "Arena y barrio greybox"
Tareas en `docs/tareas/pendientes/`: T-020 (arena), T-040 (barrio greybox), T-041 (reloj y día y noche), T-042 (locales con
puertas y horarios) y T-043 (bar interior sin carga). Algunas esperan contratos de Core que Tony escribe primero
(T-030 a T-032): tu Claude hace lo que no los necesita y deja el resto anotado en tu revisión. No pasa nada si tarda.
Prueba de cierre de la fase: ver docs/hoja-de-ruta.md.

### Cómo arrancar
Solo la primera vez, en una terminal, en la carpeta donde quieras el proyecto:
```
git clone https://github.com/tonyluno9/mecha-cyberpunk.git
cd mecha-cyberpunk
git lfs install
git lfs pull
```
Después usa los mismos turnos que Tony, cambiando "como Tony" por "como Alan", `tony/fase-N` por `alan/fase-N`,
`estado-tony.md` por `estado-alan.md` y `revision-tony.md` por `revision-alan.md`. Tu rama de integración es `alan/fase-1`.
Por ejemplo, el turno de día:
```
/goal Turno de día como Alan: sigue .claude/skills/trabajar/SKILL.md usando solo Sonnet. Termina cuando no quede ninguna tarea de Alan en docs/tareas/pendientes ni en docs/tareas/en-curso (las que no se puedan hacer quedan en docs/tareas/bloqueadas con su motivo), todas las hechas estén integradas en la rama alan/fase-N con push, y docs/revision-alan.md esté actualizado.
```
Claude instala lo que falte en tu máquina (Unity CLI, y Unity 6000.3.25f1 si no lo tienes). Si Unity te pide
iniciar sesión, hazlo una vez y vuelve a lanzar el `/goal`.

## Cuando quieras revisar tú
1. Abre docs/revision-<tu nombre>.md: dice qué probar, en qué escena y qué observar.
2. Con Claude detenido, abre Unity en la rama `<tu nombre>/fase-N` y juega.
3. Cierra Unity, abre Claude y escribe tu revisión, por ejemplo:
   `apruebo T-010 y T-013; en T-011 la cámara se siente muy alta`
   Lo aprobado pasa a main. Lo que pidas cambiar se vuelve una tarea para el siguiente turno de día.
