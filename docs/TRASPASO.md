# TRASPASO COMPLETO — Mecha Cyberpunk (Tony y Alan)

Documento de traspaso para que otra cuenta de Claude (plan Max) siga el proyecto exactamente donde quedó.
Escrito el **26 de octubre de 2026** con el repositorio revisado ese día. Todo lo que dice aquí viene del repo,
de la máquina de Tony y de lo hablado con Tony; lo que es recomendación mía está marcado como **Recomendación**.

---

## 0. Cómo usar este documento

### Para Tony
- **La forma más fácil:** en tu misma laptop, dentro de la carpeta del proyecto, cierra la sesión de Claude Code de la
  cuenta vieja (`/logout`) y entra con la cuenta Max (`/login`). El repo, `CLAUDE.md`, la skill `trabajar`, los subagentes,
  `.claude/maquina.local.md`, Unity y tus credenciales de GitHub ya están ahí: no hay que mover nada. Este documento es
  para que la nueva cuenta sepa el contexto y el estado.
- Guarda este archivo en el repo como `docs/TRASPASO.md` (ya te dejé una copia ahí, sin commit) y pégale al nuevo
  Claude el prompt de la sección 0.1.
- Si la cuenta Max va a correr en **otra computadora**, sigue la sección 4.6 (instalación desde cero).

### 0.1 Prompt de arranque para el nuevo Claude (pegar tal cual)
```
Soy Tony. Este proyecto es un juego de mechas cyberpunk en Unity que hago con Alan para una entrega del 18 de noviembre de 2026.
Lee completo docs/TRASPASO.md, luego CLAUDE.md, docs/hoja-de-ruta.md, docs/estado-tony.md y docs/guia-para-jugar.md.
No cambies nada todavía: dime en 10 líneas dónde estamos, qué sigue y qué está bloqueado, y espera mi /goal.
```
Después de eso, el `/goal` de trabajo está en la sección 12.

---

## 1. Resumen en 30 segundos

- Juego de peleas de **mechas en primera persona desde la cabina**, mundo libre **cyberpunk low-poly** en una megaciudad
  inspirada en México. PC con teclado y ratón. Unity 6000.3.25f1 + URP.
- Equipo de 2: **Tony** (jugador, combate, sistemas, datos, HUD y todo `Core/`) y **Alan** (el mundo: ciudad, arena,
  carros, casino, tráfico, arte).
- Repo público: **https://github.com/tonyluno9/mecha-cyberpunk**. Carpeta en la laptop de Tony:
  `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk`.
- El código lo escribe Claude Code en "modo automático": Opus planea, Sonnet construye y verifica con pruebas
  automáticas (EditMode y PlayMode con entrada simulada). Tony dirige; casi no escribe código a mano.
- **Entrega: 18 de noviembre de 2026.** Desde el 12 de noviembre: nada nuevo, solo bugs.
- **Estado (26-oct):**
  - Tony: fases T-F1 a T-F6 terminadas (toda su parte de la demo) + T-F7 bloques 1 a 4 (economía, ligas, piezas,
    habilidades) en `main`. Bloque 5 (misiones y reputación) a la mitad en `tony/fase-7`.
  - Alan: A-F1 a A-F4 en `main` (arena, barrio, carro, casa, callejón, clandestino, flashback, casino y mercado
    por fuera, lluvia, neón, audio, iluminación). **No sube nada desde el 17 de octubre.**
  - **Bloqueo de la demo:** Alan no ha colocado `DirectorPrologo.prefab` y los demás prefabs/puntos de Tony en
    `Ciudad.unity` y `Flashback.unity`. Sin eso no se pueden hacer T-136 (prólogo en la ciudad real) ni T-137 (build final).
  - Hay un **build provisional** jugable con las escenas de Tony: `Builds\Provisional\MechaCyberpunk.exe`.
- ~140 tareas hechas, ~920 pruebas de EditMode y ~820 de PlayMode.

---

## 2. Equipo, entrega y contexto

- **Tony** = Ángel Antonio Pérez Reyes, estudiante de programación en la Universidad Autónoma de Campeche (Facultad de
  Ingeniería), Campeche, México. Habla español (inglés conversacional). Construye dirigiendo IA más que escribiendo código.
  Usuario de GitHub: `tonyluno9` (dueño del repo).
- **Alan** = el compañero. En documentos viejos aparece como "compañero", "compa" o "Pruebas_Compa" (D-021).
  Su usuario de git aparece como `Alan`.
- Es para una competencia/entrega universitaria de videojuegos. La materia pide usar IA. Se presenta en PC.
- VR (Meta Quest 3) era un extra opcional solo para las peleas en mecha; **no se ha trabajado y no es prioridad**.
- Los dos tienen su propia cuenta de Claude. La cuenta vieja de Tony era Pro y se quedaba sin límite semanal tras ~2 días
  corriendo sin parar; por eso se pasa a Max.
- Tony **no quiere que lo manden a verificar cosas**: Claude comprueba solo. Las revisiones humanas las hace Tony
  **jugando el juego ya construido**, al final de cada bloque.
- Tony prefiere respuestas directas, sin adornos, en español. Los prompts para generadores de imágenes (Nano Banana Pro,
  etc.) los quiere **en inglés**.

---

## 3. El juego (diseño)

El diseño completo está en `docs/diseno/` (paquete-original.md y uno por bloque). Lo que dice "Propuesta" ahí lo puso
Claude y no está confirmado; lo demás lo decidió Tony. Resumen fiel:

### 3.1 Concepto e historia (`concepto-historia.md`)
- Mundo libre cyberpunk en primera persona, sin copiar nombres, marcas, jerga ni personajes de otros juegos.
- **El prólogo es el futuro:** pelea en el clandestino, emboscada al salir, peleas en mecha, regresas a casa en carro y
  duermes. Luego **flashback**: de niño tus papás se fueron y te dejaron el mecha. Despiertas con el mecha fregado y
  empiezas a juntar dinero. Al llegar otra vez a esa noche el juego no termina: es una bisagra.
- Hilo principal: el misterio de tus papás, ligado a una corporación. Te vuelves el justiciero del barrio contra las
  corporaciones. Dos reputaciones que se jalan: **barrio** y **corporaciones**.
- Preguntas abiertas: nombre del juego y de la ciudad, qué corporación y qué les pasó a los papás, final, pandillas.
  El título sigue "pendiente".

### 3.2 Combate (`combate.md`)
- Llamas al mecha y llega desde tu taller en segundos, solo en ciertas zonas. Usarlo en la calle da **calor**.
- A pie: combate básico (golpes y pistola). Lo importante es en mecha: brazos de precisión (golpe, bloqueo, contraataque).
- Habilidades especiales (misiles, etc.) con **cooldown y munición limitada** que se compra. Sin munición, solo brazos.
- **Daño persistente** hasta reparar. Con vida baja: cabina parpadea en rojo, cristal cuarteado, mecha más lento.

### 3.3 La ciudad (`ciudad.md`) — es de Alan
- Seis lugares reales: casa con taller, bar/club, casino, clandestino, mercado negro y la calle. Lo demás es fachada.
- Sin pantallas de carga. ~6 manzanas, ~1.5 min en carro. Casa en el extremo pobre; clandestino y bar frente a frente
  en el centro; casino a una cuadra; mercado negro en un callejón.
- Día y noche: **un día = 20 min reales (12 de noche, 8 de día)**. De día: mercado negro y contratos corporativos.
  De noche: clandestino, bar, casino y pandillas.
- Solo tus carros (no se roban). Calor → drones y patrullas; baja escondiéndote o llegando a casa.
- Las calles terminan en algo creíble (retén, obra, autopista), no en muros invisibles.

### 3.4 Personaje y progresión (`personaje-progresion.md`)
- Nombre, alias y apariencia (cara, ropa, implantes visibles). Se ve en el espejo de tu casa.
- **4 ligas** en el clandestino (sótano, calle, distrito, campeonato), **5 rivales cada una**, el último es jefe.
  Vencerlo sube de liga. El presentador grita tu alias.
- Perder no es game over. Morir en la calle: despiertas con el doctor de la clínica, que cobra; el mecha vuelve dañado al taller.
- **4 implantes** (en la clínica del mercado negro): ojos (ves vida y próximo ataque del rival), brazos (más daño a pie),
  piernas (corres y saltas más), sistema nervioso (reaccionas más rápido en el mecha). Hackeo después (fuera de alcance).

### 3.5 Economía y misiones (`economia-misiones.md`)
- Peleas = fuente principal de dinero. Complementos: contratos corporativos (teléfono, de día), misiones del barrio (en la
  calle; pagan poco pero dan reputación y favores: descuentos, información, escondites), ilegales y robos (bar, de noche),
  apuestas, casino y venta en el mercado negro.
- **Renta cada 7 días de juego**; si no pagas, el casero bloquea el taller.
- Puedes quedar en negativo (reparar cuesta más que el premio): es intencional.

### 3.6 El mecha y el taller (`mecha-taller.md`)
- **5 piezas:** brazo izquierdo, brazo derecho, torso, piernas, cabina con cristal.
- **4 ramas mezclables:** militar, tech/eléctrica, cuerpo a cuerpo, fuego. Cuerpo a cuerpo: más vida y velocidad, menos
  daño. Militar y fuego: más daño, más frágiles. Tech: medio, habilidades con cooldown más corto.
- **4 niveles por rama**, desbloqueados con las 4 ligas.
- **Encendido de 3 pasos (~5 s)** cada vez que te subes: confirmar (F), levantar (Espacio), cristal (Q).
- Reparación completa en el taller (costo según daño); kits de reparación del mercado negro para curar en pelea.

### 3.7 Estética, audio y HUD (`estetica-hud.md`)
- Low-poly con iluminación cinematográfica: niebla, asfalto mojado, neón azul, magenta, naranja y turquesa.
- A pie: HUD mínimo flotante (vida, objetivo, canción). En el mecha todo es diegético en la cabina.
- Radio = audífonos del protagonista, siempre suena. **3 estaciones** (cumbia electrónica, electrónica oscura, hip hop).
  Covers del equipo solo para uso escolar.

### 3.8 La demo (`demo.md`) — lo que se entrega
Unos **15 minutos**, en este orden:
1. Creación de personaje (nombre, alias, pocas opciones de apariencia).
2. Pelea de campeonato tutorial, imposible de perder; el presentador grita tu alias.
3. Sales y te emboscan; llamas al mecha y aprendes el encendido (tampoco se puede perder).
4. Regresas a casa en carro y duermes.
5. Flashback de los papás.
6. Despiertas con el mecha fregado y peleas tu primera pelea casera (esta sí se puede perder). Pantalla de "continuará".
- Casino y mercado negro se ven por fuera y cerrados.
- Prueba de cierre H6: **una persona nueva termina la demo en ~15 min sin ayuda.**
- Decisión D-tony-58 (aceptada por Tony): al despertar tras el flashback se reinicia el estado: $500, mecha al 45 %,
  renta reiniciada, calor 0, reloj a las 19:30.

---

## 4. Tecnología, repo y máquina

### 4.1 Repo
- URL: **https://github.com/tonyluno9/mecha-cyberpunk** (público; dueño `tonyluno9`).
- Remote en la máquina de Tony: `origin https://github.com/tonyluno9/mecha-cyberpunk.git`.
- Git LFS para binarios (modelos, texturas, audio). YAML de Unity con `eol=lf` (ver `.gitattributes`).
- Unity con *Force Text* y *Visible Meta Files*. Cada asset va con su `.meta` en el mismo commit; carpeta vacía lleva `.gitkeep`.
- `.gitignore` ignora `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, `.claude/maquina.local.md`, etc.

### 4.2 Versiones
- **Unity 6000.3.25f1** (Unity 6.3), URP 17.3, **Input System 1.20** (Input Manager viejo apagado), Test Framework 1.6.
- `com.unity.pipeline` 0.8.0-exp.1 (deja que la Unity CLI controle un editor abierto).
- `Packages/manifest.json` tiene `"testables": ["com.unity.inputsystem"]` (para `InputTestFixture`).
- **Unity CLI 1.0.0-beta.12** instalada en la máquina de Tony.

### 4.3 Máquina de Tony (Windows)
- Carpeta del proyecto: `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk`.
- Unity.exe: `C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe`.
- `.claude/maquina.local.md` (no se sube):
  ```
  # Máquina de Tony
  - Unity.exe: C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe
  - Unity CLI: 1.0.0-beta.12
  ```
- Claude Code corre en Git Bash/PowerShell en esa carpeta. Los scripts de `herramientas/` son Bash (Git Bash).
- Claude Code en versión 2.1.283 o mayor arranca en modo automático (no pide permiso por cada comando).

### 4.4 Build provisional
- `Builds\Provisional\MechaCyberpunk.exe` (≈126 MB, fuera de git). Arranca en CreacionPersonaje.
- Guía: `docs/guia-para-jugar.md` (controles, escenas incluidas, teclas de prueba, 10 cosas que probar).

### 4.5 Escenas
| Escena | Dueño | Qué es |
| --- | --- | --- |
| `Escenas/Ciudad.unity` | Alan | La ciudad real (barrio, casa, clandestino, callejón, etc.) |
| `Escenas/Clandestino.unity` | Alan | Clandestino y arena |
| `Escenas/Flashback.unity` | Alan | Cuarto de tarde del flashback |
| `Escenas/Pruebas_Alan.unity` | Alan | Pruebas del mundo |
| `Escenas/Pruebas_Tony.unity` | Tony | Banco de pruebas del mecha y la pelea |
| `Escenas/Tony/CreacionPersonaje.unity` | Tony | Título y creación de personaje (primera del build) |
| `Escenas/Tony/Pruebas_Prologo.unity` | Tony | Prólogo en ciudad de prueba |
| `Escenas/Tony/Pruebas_Flashback.unity` | Tony | Flashback de prueba |
| `Escenas/Tony/Pruebas_APie.unity` | Tony | Jugador a pie |
| `Escenas/Tony/Pruebas_Calle.unity` | Tony | Calle con rival |
| `Escenas/Tony/Pruebas_Economia.unity` | Tony | Taller, banco, mercado negro, mostrador de peleas |
| `Escenas/Tony/Pruebas_Misiones.unity` | Tony | Misiones (bloque 5) |
| `Escenas/Tony/Pruebas_Tutorial.unity` | Tony | Tutorial |
(Todas bajo `Assets/_Proyecto/`.)

### 4.6 Si la cuenta Max trabaja en OTRA computadora
1. Instalar Git, Git LFS, Unity Hub y **Unity 6000.3.25f1** (o `unity install 6000.3.25f1 --yes --accept-eula` con la CLI).
2. Unity CLI en PowerShell: `$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex`
3. `git clone https://github.com/tonyluno9/mecha-cyberpunk.git`, luego `git lfs install` y `git lfs pull`.
4. Crear `.claude/maquina.local.md` con la ruta de Unity.exe y la versión de la CLI.
5. **Para hacer push** esa computadora necesita credenciales de GitHub con acceso de escritura: o la cuenta de Tony, o
   que Tony agregue a esa cuenta de GitHub como colaborador (repo → Settings → Collaborators). La cuenta de Claude no da
   acceso a GitHub; git usa las credenciales de la máquina.
6. Abrir Unity una vez a mano para la licencia (si pide iniciar sesión) y cerrarlo antes de que Claude trabaje.

---

## 5. Estructura del código y dueños

Todo bajo `Assets/_Proyecto/`. Cada área tiene asmdef `MechaCyberpunk.<Área>` que referencia `MechaCyberpunk.Core` y
paquetes de Unity. **Áreas de dueños distintos nunca se referencian entre sí: se hablan solo por contratos en `Core/`.**

| Carpeta | Dueño | Qué va |
| --- | --- | --- |
| `Core/` | Solo tareas de Core (las hace Tony, D-032) | Contratos: daño, vida, peleadores, eventos, arena, jugador, reloj, lugares, puertas, cartera, calor, objetivos, guardado (`IGuardable`), interacción (`IInteractuable`), renta (`IRenta`), historia (`IHistoria`, `IPuntoHistoria`, `EtapaPrologo`), apuestas (`IApuestasPelea`), ligas, valores del mecha, reputación y misiones |
| `Mecha/` | Tony | Movimiento, cabina, entrada, prefab `MechaJugador` |
| `Combate/` | Tony | Golpe, bloqueo, vida, habilidades |
| `UI/` | Tony | Pantallas de cabina y HUD a pie |
| `IA/` | Tony | Rivales y su cerebro |
| `Pelea/` | Tony | Flujo de pelea, conteo, resultado, `DirectorPelea` |
| `Personaje/` | Tony | Jugador a pie, combate a pie, creación de personaje |
| `Economia/` | Tony | Dinero, precios, premios, reparaciones, renta, apuestas, tienda |
| `Progresion/` | Tony | Ligas, piezas, misiones, reputación, historia/prólogo, guardado |
| `Radio/` | Tony | Radio y estaciones (vacía todavía) |
| `Arena/` | Alan | Jaula y luces del clandestino |
| `Mundo/` | Alan | Ciudad, calles, locales, interiores, día/noche (`RelojMundo`), ambiente |
| `Vehiculos/` | Alan | Carros y manejo (`IMontable`) |
| `Casino/` | Alan | Tragamonedas, fichas, cajera |
| `Trafico/` | Alan | Tráfico, peatones, drones, patrullas |
| `Escenas/` | Por archivo (ver 4.5) | |
| `Tests/EditMode/<Área>/`, `Tests/PlayMode/<Área>/` | Dueño del área | Pruebas (cada carpeta con su asmdef) |
| `Assets/Arte/` | Por defecto Alan | Modelos, materiales, audio (LFS). Fuentes de Blender en `Assets/Arte/Fuentes~/` |
| `ProjectSettings/`, `Packages/`, `herramientas/` | Solo tareas de Core | |

- Datos (ScriptableObjects) en `<Área>/Datos/`; herramientas de prueba en `<Área>/Pruebas/`; código de editor en `<Área>/Editor/`.
- Alan coloca `MechaJugador` y los prefabs de Tony en sus escenas pero **no los edita** (D-029).
- Convenciones de C#: `.claude/rules/unity-csharp.md` (nombres en español, lógica pura separada de MonoBehaviour,
  números en ScriptableObjects, pruebas).
- **Escenas, prefabs y assets se crean con código de editor (`-executeMethod`) o con la Unity CLI, nunca escribiendo YAML a mano.**

---

## 6. Ramas y git

- `main`: lo aprobado. En la práctica actual, Tony autoriza por escrito en el `/goal` que cada bloque cerrado se mezcle a
  `main` con push ("yo, Tony, lo autorizo"). Sin esa frase el clasificador de permisos de Claude Code a veces bloquea el push a main.
- `tony/fase-7`: rama de integración actual de Tony. (Fases anteriores: `tony/fase-3`, `-4`, `-5`; la T-F6 se hizo en `tony/fase-5`.)
- `tony/T-xxx-nombre`: una rama por tarea; sale de la de integración y vuelve con `git merge --no-ff`.
- `alan/fase-1`, `alan/fase-2`, `alan/fase-3` (A-F3 y A-F4 se hicieron en `alan/fase-3`), `alan/T-xxx-...`.
- **Nunca force push. Nadie toca ramas ni carpetas del otro.** Push de cada rama cada vez que cambia.
- Cambiar de rama con Unity cerrado.
- `ProjectSettings/ProjectSettings.asset` cambia solo en `SENTIS_ANALYTICS_ENABLED` (el editor lo agrega y batchmode lo quita):
  se descarta con `git restore`. Igual `SceneTemplateSettings.json`.
- Basura local sin seguimiento que NO se sube: `Assets/InitTestScene*.unity(.meta)` (las crea el Test Runner), `Assets/_Recovery*`,
  `.claude/worktrees/`, `.agents/`, `.codex/`, `AGENTS.md`.
- La máquina de Tony tiene ~128 ramas locales; es normal.

### Historial de `main` (merges principales)
| Fecha | Merge |
| --- | --- |
| 15-oct | tony/fase-3 y fase-4 (aprobadas) + decisiones D-033 a D-072 |
| 16-oct 00:38 | alan/fase-1 (A-F1) |
| 16-oct 14:08 | alan/fase-2 (A-F2) |
| 16-oct 14:40 | tony/fase-5 (T-F5, T-060, T-100, T-120 a T-123) |
| 17-oct 07:43 | T-F6 (T-124 a T-135) |
| 17-oct 09:10 | T-140 y T-141 (Core) |
| 17-oct 16:18 | T-F7 bloque 1 (economía) |
| 17-oct 16:30 | T-073 + alan/fase-3 (A-F3 y A-F4, con T-092) |
| 18-oct 06:08 | T-F7 bloque 2 (ligas) |
| 21-oct 06:55 | T-F7 bloque 3 (piezas) |
| 26-oct 08:02 | T-F7 bloque 4 (habilidades, T-174 a T-180a) — último commit en main |

---

## 7. El modo automático (cómo trabaja Claude en este repo)

### 7.1 Piezas
- `CLAUDE.md`: reglas del repo, carpetas y dueños, ramas, comandos de pruebas.
- `.claude/skills/trabajar/SKILL.md`: el orquestador (turno de día con Sonnet y turno de noche con Opus; preparación,
  ciclo por tarea, revisión, aprobación, cuándo parar).
- `.claude/skills/tarea/SKILL.md`: ejecutar una tarea suelta (`/tarea T-xxx`), modo manual viejo.
- Subagentes en `.claude/agents/`:
  | Subagente | Modelo | Para qué |
  | --- | --- | --- |
  | `implementador` | Sonnet, medium | Programa una tarea en su rama con pruebas EditMode y PlayMode (`InputTestFixture`), crea escenas/prefabs por código, verifica con `verificar.sh`, commit y push. No hace merge. Responde ≤15 líneas. |
  | `verificador` | Sonnet, medium | Comprueba cada criterio: prueba automática → Unity en vivo (`unity-vivo.sh`, `unity command eval/screenshot`) → capturas en `docs/capturas/T-xxx-n.png` que revisa él mismo → juicio humano solo para sensación. Tabla de evidencia. No edita código. |
  | `revisor` | Opus, high | Revisa el diff de una tarea o de una fase. **Ya no se usa** (era muy lento; decisión de Tony). |
  | `planificador` | Opus, high | Escribe las tareas de una fase o bloque nuevo en `docs/tareas/pendientes/` con criterios verificables. |
- `docs/ARRANQUE.md`: guía vieja para personas (turnos día/noche). Está desactualizada respecto al modo esbelto actual.

### 7.2 Cómo se trabaja HOY (modo esbelto, más rápido que lo que dice SKILL.md)
Tony decidió (después de ver que las revisiones tardaban mucho) que:
- **Opus solo planea** cada bloque (planificador) y entra como subagente solo si el implementador falla 2 veces.
- **Sonnet construye** (orquestador e implementador en Sonnet 5.5, `/effort medium`).
- **Sin revisor.** El implementador compila y corre él mismo las pruebas de las áreas que tocó en cada tarea;
  **el verificador y la suite completa solo al cerrar cada bloque.**
- Tareas grandes (6–8 por bloque), bitácoras de una línea, capturas solo si el criterio es visual.
- Al cerrar cada bloque: merge de `tony/fase-7` a `main` con push (autorizado en el `/goal`), regenerar el build provisional
  y agregar a `docs/guia-para-jugar.md` qué probar.
- **Las revisiones humanas las hace Tony jugando**, no leyendo código.
- Con este modo un bloque tarda ~4–6 h de Claude (antes 8–10 h).

### 7.3 Archivos de control
- Tareas: `docs/tareas/{pendientes,en-curso,hechas,bloqueadas}/T-xxx-nombre.md` (plantilla `docs/tareas/_plantilla.md`).
  Cada una con dueño, rama, dependencias, objetivo, contexto, qué hacer, carpetas permitidas, criterios de aceptación y fuera de alcance.
- Estado por persona: `docs/estado-tony.md`, `docs/estado-alan.md` (tabla de tareas + qué sigue + notas de parada).
- Decisiones: `docs/decisiones.md` (aprobadas, D-001 a D-072) y `docs/decisiones-pendientes-tony.md` (D-tony-40 a D-tony-152,
  propuestas por los agentes, ~113 entradas) / `docs/decisiones-pendientes-alan.md`.
- Revisión para la persona: `docs/revision-tony.md` (largo, por tarea) y `docs/revision-alan.md`.
- Bitácora: `docs/bitacora/AAAA-MM-DD-<persona>-T-xxx.md` (~121 archivos). Capturas: `docs/capturas/` (~190).
- `docs/estado.md` (global) está **desactualizado desde el 15-oct**: solo se actualizaba en "aprobaciones". La verdad está
  en `docs/estado-tony.md`, `docs/estado-alan.md`, `git log` y `docs/hoja-de-ruta.md`.
- `docs/prompt-arranque.md`: el prompt original con el que se montó el sistema (T-000b).

### 7.4 Decisiones clave del proceso (en `docs/decisiones.md`)
- D-020 Integraciones de preparación (Core directo a main) · D-021 El compañero es Alan · D-022 Modo automático y revisión
  nocturna · D-023 Verificación sin humanos · D-024 Turnos día/noche · D-025 Fase 1 aprobada · **D-026 Tony = sistemas,
  Alan = mundo** · **D-027 Contratos entre Tony y Alan en Core/** · D-028 Carpetas y ensamblados · **D-029 Escenas con un solo
  dueño** · D-030 IDs de tareas · D-031 Orden de construcción por demo · D-032 Cambios de dueño en Core/.

---

## 8. Herramientas de verificación (`docs/herramientas-verificacion.md`)

Desde la raíz del repo, en Git Bash, **con Unity cerrado**:
```bash
bash herramientas/verificar.sh compilar|editmode|playmode|playmode-por-area|todo|ensamblados
```
- Lee la ruta de Unity de `UNITY_EXE` o de `.claude/maquina.local.md`. Logs y XML en `Logs/`.
- Si hay un editor abierto imprime `UNITY ABIERTO` y sale con 3.
- Corre todas las áreas con `-assemblyNames` (cada `Tests/<Modo>/<Área>/` con asmdef y al menos un `.cs`; desde T-183 también
  sub-asmdef anidados como `Progresion.Misiones`).
- `playmode-por-area`: un Unity por área, con tope de tiempo (Progresion 2400 s, las demás 900 s; `LIMITE_AREA=` lo cambia;
  `AREAS="Economia Core"` limita las áreas). Un cuelgue no tumba a las demás.
- Para correr solo algunas pruebas: Unity.exe con `-testFilter` o `-assemblyNames` (ver comando base en CLAUDE.md).
- Códigos de salida de Unity en pruebas: 0 pasaron, 2 alguna falló, otro = error. Revisa `total` y `failed` en el XML; `total="0"` = no corrió nada.

Editor vivo:
```bash
bash herramientas/unity-vivo.sh iniciar [Assets/_Proyecto/Escenas/<escena>.unity]
bash herramientas/unity-vivo.sh play | captura Logs/x.png | eval '<C# con return>' | consola | detener-play | comandos | detener
```
- Usa `unity command` (`editor_play`, `editor_stop`, `screenshot`, `eval`, `open_scene`, `console`). Siempre `detener` al final.
- `capture_game_view` no sirve en batchmode; usar `screenshot`.

---

## 9. Estado detallado al 26-oct-2026

### 9.1 Hoja de ruta (`docs/hoja-de-ruta.md`) e hitos
| Hito | Qué se puede hacer | Tony | Alan | Estado |
| --- | --- | --- | --- | --- |
| H1 | Pelea completa en el clandestino | T-F2 | A-F1 | ✅ |
| H2 | Caminar el barrio, entrar y salir de locales | T-F3 | A-F1 | ✅ |
| H3 | Carro conducible | T-F3 | A-F2 | ✅ |
| H4 | Llamar al mecha y emboscada | T-F4 | A-F3 | ✅ (T-073 en main) |
| H5 | Daño persistente, reparaciones, taller | T-F5 | A-F3 | ✅ lado Tony; cama/espejo/taller de Alan (T-092) en main |
| H6 | **La demo completa** | T-F6 | A-F4 | ⛔ falta T-136/T-137 (espera a Alan) |
| H7 | Contenido posterior a la demo | T-F7 | A-F5 | 🟡 Tony bloques 1–4 hechos; Alan A-F5 sin empezar |

### 9.2 Tony — fases
| Fase | Tareas | Estado |
| --- | --- | --- |
| T-F1 Pelea greybox | T-001, T-002, T-010 a T-013 | ✅ main |
| T-F2 Contratos y pelea completa | T-030 a T-034, T-021, T-022 | ✅ main |
| T-F3 Jugador a pie y HUD | T-050 a T-059, T-060, T-061 | ✅ main (T-061 hecha pero su archivo sigue en `en-curso/`) |
| T-F4 Mecha en la calle y emboscada | T-062 a T-073 | ✅ main |
| T-F5 Daño, taller, guardado | T-100 a T-112 | ✅ main |
| T-F6 Creación de personaje, prólogo, cierre demo | T-120 a T-137 | ✅ main salvo **T-136 y T-137 (bloqueadas por Alan)** |
| T-F7 bloque 1 Economía completa | T-140 a T-151 | ✅ main (apuestas, tienda del mercado negro, vender piezas, kits en pelea, reparar a medias, precios) |
| T-F7 bloque 2 Ligas | T-152 a T-162 | ✅ main (4 ligas × 5 rivales, jefes con furia, calendario, retar desde mostrador, cartelera, subir de liga) |
| T-F7 bloque 3 Piezas | T-163 a T-173 | ✅ main (5 ranuras, 4 ramas, 4 niveles, equipo del mecha, taller, se ven en el mecha, piezas en apuestas, balance) |
| T-F7 bloque 4 Habilidades | T-174 a T-180a | ✅ main (teclas 1 y 2 en la cabina, munición y enfriamiento, se ven en cabina/taller/mercado, jefes con habilidad, balance) |
| **T-F7 bloque 5 Misiones y reputación** | T-181 a T-188 | 🟡 en `tony/fase-7`: T-181 a T-184 hechas; **T-185 escrita sin verificar** en `tony/T-185-pelear-escapar`; T-186 a T-188 pendientes |
| T-F7 bloque 6 Implantes | desde T-189 | ⬜ |
| T-F7 bloque 7 Radio | | ⬜ |
| T-F7 bloque 8 Adaptación del rival | | ⬜ |
| Hackeo | | ❌ fuera de alcance |

**Bloque 5 detalle:**
- T-181 Contratos de reputación, misiones y puntos de misión (Core) — hecha
- T-182 Reputación de barrio y corporaciones, favores — hecha
- T-183 11 misiones como datos (tabla, reglas, máquina de pasos, balance) — hecha (también arregló `verificar.sh` para asmdef anidados)
- T-184 Servicio de misiones en escena y `Pruebas_Misiones` — hecha
- **T-185 Pelear y escapar: peleas callejeras, calor, robos con chatarra — A MEDIAS:** código, datos y pruebas escritos, sin
  compilar/verificar (la corrida se cortó por parada manual). Falta: compilar, correr EditMode y PlayMode de `Progresion.Misiones`
  y `Pelea`, revisar capturas T-185-1/2, cerrar y mezclar en `tony/fase-7`.
- T-186 Teléfono de día, contacto del bar de noche, encargos en la calle; reputación a la vista — pendiente
- T-187 Usar los favores: descuento, escondite, información — pendiente
- T-188 Recorrido de misiones y reputación (cierre del bloque) — pendiente
- Preguntas abiertas para Tony (D-tony-152): ¿teléfono en la tecla T? ¿las corporativas dan algo más que dinero (piezas, info de
  los papás)? ¿descuento también en taller o clínica? ¿robos que salen mal con patrullas de Alan con calor 3? ¿misiones únicas con
  historia para el hilo de los papás?

**Pruebas (último cierre):** EditMode ~922/922; PlayMode ~807/820, con **9 fallos conocidos de Alan en
`JaulaClandestinoPruebas` (área Arena)** — no son de Tony.

### 9.3 Alan
| Fase | Tareas | Estado |
| --- | --- | --- |
| A-F1 Arena y barrio greybox | T-020, T-040 a T-044 | ✅ main |
| A-F2 Carro | T-074 a T-083 | ✅ main · **T-084 (tráfico de fachada) y T-085 (modelo del carro viejo) pendientes** (el modo auto se las negó) |
| A-F3 Casa, callejón, interiores | T-086 a T-093 | ✅ main T-086 a T-090 y T-092 · **T-093 (recorrido del prólogo de su lado) bloqueada sin razón real** |
| A-F4 Cierre visual de la demo | T-094 a T-099 | ✅ main (exteriores de casino y mercado, grafitis, lluvia y niebla, audio, calles finales, iluminación) |
| A-F5 Después de la demo | casino jugable, mercado negro y clínica, peatones, persecuciones, personalización de carros | ⬜ |
- Último commit de Alan: **17-oct 15:44** (T-092). `docs/estado-alan.md` está desactualizado (dice cosas de A-F2).
- Pedidos de Tony a Alan para A-F5: colocar `MostradorMercadoNegro.prefab` en su mercado negro; su casino puede tomar apuestas
  con `IApuestasPelea` (T-141).

### 9.4 El bloqueo de la demo (lo más importante)
T-136 (`docs/tareas/bloqueadas/T-136-prologo-en-ciudad-alan.md`) necesita que Alan, en **sus** escenas, coloque (pedido D-tony-72):
- En `Ciudad.unity`: `DirectorPrologo.prefab`, `CamaJugador.prefab`, `EspejoJugador.prefab`, `MostradorPeleas.prefab`
  (de `Progresion/Prologo/Prefabs/`), `MechaEstacionado.prefab`, `Emboscada.prefab` y `JugadorAPie.prefab`.
- En `Flashback.unity`: `Flashback.prefab`.
- Apariciones "Casa_Cama", "Casa_Puerta", "Clandestino_Salida".
- `IPuntoHistoria`: "Prologo_Carro" (donde queda el carro al salir del clandestino), "Prologo_Esquina_N" en las vueltas del
  regreso a casa y "Flashback_Plano_1" a "Flashback_Plano_6".
- `RelojMundo` como `IGuardable` (clave `reloj`) — ya hecho en T-092; su carro como `IMontable` — ya existe.
- Cómo saber si llegó: `git grep -l "DirectorPrologo" main -- Assets/_Proyecto/Escenas/Ciudad.unity` y
  `git grep -l "IPuntoHistoria" main -- Assets/_Proyecto/Mundo/`.

Luego T-136 conecta `DatosEscenas_Demo` (CreacionPersonaje → Ciudad → Flashback → Ciudad), ajusta `DatosPrologo_Base` y
`ConfigAyudas_Base`, y hace la prueba `PrologoCiudadPruebas`. T-137 pone las escenas en Build Settings y saca el build de Windows (Core).

**Opciones si Alan no responde:** (a) Alan autoriza por escrito que el Claude de Tony coloque solo esos prefabs y puntos en
`Ciudad.unity` y `Flashback.unity` (rompe D-029 con permiso); (b) entregar con el prólogo en las escenas de prueba de Tony
(build provisional). **Recomendación:** fecha límite a Alan el miércoles 28-oct; si no, opción (a).

Mensaje que se le mandó a Alan para que su Claude suba todo (reutilizable):
```
/goal Sube todos mis cambios. 1) Revisa git status y git stash list en mi carpeta y en todas mis ramas locales alan/*. Haz commit de todo lo que sea del proyecto: en una tarea terminada, con su mensaje normal; en una tarea a medias, como "WIP" en su propia rama. No subas basura: Assets/InitTestScene*, Temp/, Logs/, .claude/worktrees/, ni el cambio de SENTIS_ANALYTICS_ENABLED (ese con git restore). 2) Haz push de todas mis ramas alan/* a origin. 3) Lo terminado y verificado de alan/fase-3 que no esté en main, mézclalo en main con push (yo, Alan, lo autorizo); lo WIP nunca va a main. 4) Después, lo que bloquea la demo de Tony: en Ciudad.unity y Flashback.unity coloca DirectorPrologo.prefab de Tony y los puntos de historia con IPuntoHistoria según docs/tareas/bloqueadas/T-136-prologo-en-ciudad-alan.md, verifícalo, y mézclalo a main con push. 5) Actualiza docs/estado-alan.md con qué quedó en main y qué falta. Nunca toques Mecha/, Combate/, UI/, IA/, Pelea/, Personaje/, Economia/, Progresion/ ni Radio/. Termina cuando git status esté limpio, todas mis ramas estén en origin y main tenga mi trabajo terminado.
```

### 9.5 Controles (de `docs/guia-para-jugar.md`)
- **A pie:** WASD, ratón, Shift correr, Espacio saltar, **E interactuar** (subir/bajar del mecha, bancos, mostradores),
  clic izquierdo atacar, 1/2 puños/pistola, R recargar, Esc cursor.
- **Mecha:** WASD/ratón, clic izquierdo golpe, clic derecho bloqueo, **F confirmar, Espacio levantar, Q cristal** (encendido),
  **M llamar al mecha**, **1/2 habilidad del brazo/torso** (solo en pelea), Esc cursor.
- Pantallas: flechas + Enter, Esc cierra.
- Teclas de prueba (solo escenas de prueba): P iniciar pelea, H hacerse daño, G usar kit, T entrar a la arena, F5/F9 guardar/cargar,
  8/9/0 deuda/brazo/kit o calor, J saltar reloj, L vencer rival de la liga, O siguiente objetivo.
- Limitación del build provisional: solo el flujo de la historia cambia de escena; no hay menú para saltar a Pruebas_Economia, etc.

---

## 10. Pendientes concretos (lista de trabajo)

### De Tony (en orden)
1. Terminar T-185 (compilar, verificar `Progresion.Misiones` y `Pelea`, capturas, cerrar, merge a `tony/fase-7`).
2. T-186, T-187, T-188 → cerrar bloque 5: verificador + suite completa + merge a `main` + build provisional + guía.
3. Mover `T-061` de `en-curso/` a `hechas/` (está hecha).
4. Bloque 6 implantes → bloque 7 radio → bloque 8 adaptación del rival (planificador primero, 6–8 tareas por bloque).
5. En cuanto llegue lo de Alan: **T-136 y T-137 antes que todo lo demás.**
6. Contestar (Tony) las preguntas de D-tony-152 y, en general, revisar `docs/decisiones-pendientes-tony.md` (D-tony-40 a 152 son propuestas de los agentes; nunca se pasaron a `docs/decisiones.md`).
7. Deudas conocidas: `CreadorKitReparacion` ensuciaba `MechaJugador.prefab` (arreglado en T-153; vigilar); materiales que Unity
   modifica solo al correr pruebas (`Rival_Cuerpo.mat`, `Cabina_Grietas.mat`, `Cabina_AlarmaPrendida.mat`) → `git restore`;
   `CambioEscenaPruebas`/`PrologoPresentePruebas` fallaban a veces por orden en la primera corrida de Progresion (arreglado en T-164; vigilar);
   aviso de 2 AudioListener al subir al carro; PlayMode de Progresion es lento (~900–2400 s).

### De Alan
1. Colocar los prefabs y puntos de Tony en `Ciudad.unity` y `Flashback.unity` (sección 9.4) y mezclar a main.
2. T-093, arreglar las 9 pruebas de `JaulaClandestinoPruebas`, T-084, T-085.
3. A-F5 si hay tiempo (casino jugable con `IApuestasPelea`, mercado negro con `MostradorMercadoNegro.prefab`, clínica para implantes,
   persecuciones de drones y patrullas con calor).

### De Tony como humano
- **Jugar el build provisional** (todavía no lo ha hecho a fondo) con `docs/guia-para-jugar.md` y pasar la lista de bugs a Claude.
- Que una persona nueva juegue la demo (prueba H6).
- Preparar entrega: video corto, documento o presentación si la materia lo pide.

---

## 11. Plan hasta la entrega (18-nov-2026)

| Semana | Qué |
| --- | --- |
| 26-oct – 1-nov | Cerrar bloque 5. Tony juega el build y pasa bugs. **Alan coloca prefabs en Ciudad.unity (límite 28-oct)** → T-136 y T-137 = `.exe` de la demo completa. |
| 2 – 8-nov | Prueba con persona nueva (H6) y arreglos. Bloques 6 (implantes) y, si da, 7 y 8. |
| 9 – 15-nov | **Desde el 12-nov nada nuevo.** Solo bugs, build final, video, materiales de entrega. |
| 16 – 18-nov | Margen y entrega. |

**Recomendación (de mí, no decidido por Tony):** con Max hay más margen, pero lo que más sube la calificación es que la demo
de 15 min funcione redonda en la ciudad real. Priorizar: (1) T-136/T-137, (2) bugs encontrados jugando, (3) bloques nuevos.
Con Max se puede volver a pasar el verificador por tarea si hay muchos bugs, o usar Opus como orquestador en el cierre final.

---

## 12. Prompts que funcionan

### 12.1 Arrancar Claude Code
En la carpeta del proyecto (Unity cerrado):
```
claude --model sonnet
```
Dentro:
```
/effort medium
```
y luego el `/goal`.

### 12.2 `/goal` de construcción actual (el que mejor ha rendido)
```
/goal Sigue el turno de construcción de Tony, rápido y con pocos tokens. Reglas: sin revisor. El implementador hace cada tarea completa: compila, corre él mismo solo las pruebas de las áreas que tocó, hace commit y push, y la mezcla en tony/fase-7. No uses el verificador por tarea: úsalo una vez al cerrar cada bloque, con la suite completa y capturas solo si algo es visual. Subagente en Opus solo si el implementador falla 2 veces. Bitácoras de una línea, y docs/estado-tony.md corto: solo la tabla del bloque actual y lo pendiente. Retoma T-185 en tony/T-185-pelear-escapar: compila, corre Progresion.Misiones y Pelea, revisa las capturas, ciérrala y mézclala en tony/fase-7. Mueve T-061 a hechas. Luego T-186, T-187 y T-188, y al cerrar el bloque 5 mezcla tony/fase-7 en main con push (yo, Tony, lo autorizo). Después, para cada bloque, el planificador (Opus) escribe de 6 a 8 tareas grandes: bloque 6 (implantes), bloque 7 (radio) y bloque 8 (adaptación del rival), en ese orden; sin hackeo. Al cerrar cada bloque, mezcla en main con push, regenera el build provisional en Builds/Provisional y agrega a docs/guia-para-jugar.md qué probar. Entre bloques haz git fetch: si Ciudad.unity de Alan ya trae DirectorPrologo en main, haz T-136 y T-137 antes de seguir. Si la fecha es 12 de noviembre de 2026 o después, no empieces bloques nuevos: termina el que esté en curso y para. Nunca toques Arena/, Mundo/, Vehiculos/, Casino/ ni Trafico/. No te detengas: si algo falla fuera de mi área, anótalo en docs/estado-tony.md y sigue; si se acaba el límite, espera a que se renueve y continúa; decide lo razonable sin preguntarme y anótalo en docs/decisiones-pendientes-tony.md. Termina cuando acabes los bloques 5 a 8 o llegue el 12 de noviembre.
```

### 12.3 Turno corto para arreglar bugs encontrados jugando
```
/goal Turno corto de Tony, sin revisor. Arregla esto que encontré jugando el build: <lista de bugs en mis palabras>. Para cada uno: tarea T-xxxa en docs/tareas, rama propia, arreglo, pruebas del área, merge a tony/fase-7. Al final suite completa, merge a main con push (yo, Tony, lo autorizo), regenera Builds/Provisional y dime qué volver a probar. No construyas nada nuevo.
```

### 12.4 Cerrar la demo cuando Alan haya colocado sus prefabs
```
/goal Haz git fetch y mezcla main en tony/fase-7. Comprueba con git grep que Ciudad.unity trae DirectorPrologo y que Mundo/ tiene IPuntoHistoria. Haz T-136 y T-137 (docs/tareas/bloqueadas/), con el verificador al final, suite completa, merge a main con push (yo, Tony, lo autorizo) y el build de Windows de la demo en Builds/Demo. Actualiza docs/guia-para-jugar.md con cómo jugar la demo completa de 15 minutos.
```

### 12.5 Pausar sin perder nada
1. `Esc` (interrumpe), 2. `/goal clear`, 3. mensaje: "Detente aquí. Haz commit y push de lo que tengas (WIP en su rama si está a medias) y anota en docs/estado-tony.md qué quedó a medias y qué sigue.",
4. `/exit`. 5. Verificar que no quede Unity abierto. Las "paradas manuales" del historial son exactamente esto.

---

## 13. Problemas conocidos y trucos

- **Unity debe estar cerrado** para pruebas en batch. Si Tony lo tiene abierto, `verificar.sh` dice `UNITY ABIERTO`. Un
  `Temp/UnityLockfile` huérfano (sin proceso) se puede borrar.
- **El clasificador del modo auto** a veces bloquea: push a `main`, auto-editar `.claude/settings.json`, o tareas que parecen
  riesgosas (a Alan le negó T-084/T-085). Solución: autorización explícita en el `/goal` ("yo, Tony, lo autorizo") o que la persona
  corra el comando a mano. Una vez T-100 se quedó en una rama local `tony/core-pendiente-push-main` porque el push a main fue denegado.
- Unity regenera solos algunos `.mat` y `ProjectSettings` al correr pruebas → `git restore` si no son parte de la tarea.
- El Test Runner deja `Assets/InitTestScene<guid>.unity` sin seguimiento: no se suben.
- PlayMode completo tarda mucho (Progresion hasta 40 min). Por tarea, correr solo las áreas tocadas.
- `docs/estado.md` y `docs/estado-alan.md` están viejos; no fiarse de ellos.
- En PowerShell, `Unity.exe` no espera: usar `Start-Process -Wait -PassThru`. Mejor Git Bash con los scripts.
- Límite de uso: con Pro el límite semanal se acababa en ~2 días corriendo sin parar; la sesión de Claude Code espera sola a que
  se renueve si el `/goal` lo dice. Con Max hay ~5× (Max 5x) o ~20× (Max 20x) de margen por sesión.
- La skill `trabajar` describe el flujo viejo (día/noche con revisor). El `/goal` manda sobre ella; si se quiere, se puede
  actualizar la skill al modo esbelto (sección 7.2).

---

## 14. Glosario de IDs

- **T-xxx:** tareas. T-001/T-002 base; T-010–013 F1; T-020–022, T-030–034 F2; T-040–044 Alan F1; T-050–061 F3; T-062–073 F4;
  T-074–085 Alan F2; T-086–093 Alan F3; T-094–099 Alan F4; T-100–112 F5; T-120–137 F6; T-140–151 bloque 1; T-152–162 bloque 2;
  T-163–173 bloque 3; T-174–180a bloque 4; T-181–188 bloque 5; T-189+ siguientes. Sufijo `a` = corrección.
- **D-001 a D-072:** decisiones aprobadas (`docs/decisiones.md`). **D-tony-N / D-alan-N:** propuestas pendientes.
- **T-F1…T-F7:** fases de Tony. **A-F1…A-F5:** fases de Alan. **H1…H7:** hitos de integración.

---

## 15. Cronología breve

- **Antes del 11-oct:** entrevista de diseño (primero "mecha de madera que defiende una aldea", luego cambió a mecha cyberpunk
  de mundo libre). Paquete de diseño de 8 bloques. Estilo low-poly elegido con imágenes de referencia.
- **~11-oct:** repo creado, T-001 contratos, T-002 verificación automática, montaje del modo automático (skill trabajar,
  subagentes, CLAUDE.md, ARRANQUE.md). División Tony/Alan (D-026).
- **13–15-oct:** Tony T-F1 a T-F4; Alan A-F1 y A-F2. Aprobación de Tony el 15 (D-033 a D-072).
- **15-oct:** Tony decide quitar las revisiones de Opus ("Opus estructura, Sonnet construye, yo reviso jugando").
- **16-oct:** T-F5 a main; Alan A-F1/A-F2 a main; arranca T-F6. Se acaba el límite semanal de Pro.
- **17-oct:** T-F6 a main; T-F7 bloque 1 (economía) a main; A-F3/A-F4 de Alan a main. **Último trabajo de Alan.**
- **18-oct:** bloque 2 (ligas) a main; bloque 3 a medias.
- **21-oct:** bloque 3 (piezas) a main.
- **26-oct:** build provisional; bloque 4 (habilidades) a main; bloque 5 a la mitad. Traspaso a cuenta Max.

---

## 16. Checklist para el nuevo Claude antes de su primer `/goal`

- [ ] `git status`, `git fetch`, rama actual (debería ser `tony/T-185-pelear-escapar` o `tony/fase-7`).
- [ ] Unity cerrado; `.claude/maquina.local.md` existe y la ruta funciona (`bash herramientas/verificar.sh compilar`).
- [ ] Leer `docs/estado-tony.md`, `docs/tareas/en-curso/T-185-pelear-escapar-robos.md` y las pendientes T-186 a T-188.
- [ ] Revisar si Alan subió algo (`git log origin/alan/fase-3 -3`, `git grep DirectorPrologo origin/main -- Assets/_Proyecto/Escenas/Ciudad.unity`).
- [ ] Confirmar con Tony el `/goal` (sección 12.2) y arrancar.
