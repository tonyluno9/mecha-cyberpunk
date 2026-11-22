# Tareas Unity — locomoción PJ01 (revisión con Tony, 20-nov)

Decidido con Tony:
- **WASD = caminar**
- **Shift = sprint con el correr viejo (Loco_Correr)**
- **Velocidad rápida (implante o consumible) = carrera "sin gravedad"**
- **Salto:** estático "Callejero", salto corriendo con un pie, rodada al caer corriendo y aterrizaje fuerte en tres puntos.
- **Caminar de lado apuntando (derecha):** el caminar de lado viejo, igual, solo con los brazos sujetando la pistola. El de la izquierda se queda como está.

## Ya hecho desde Blender
| Archivo en `Assets/Arte/Personajes/PJ01/Clips` | Contenido nuevo | Velocidad | Cambios en .meta |
|---|---|---|---|
| `PJ01_Caminar.fbx` | Loco_Caminar viejo con los hombros abiertos 10° | 1.62 m/s | lastFrame 31→32; AlPasoPJ01 en 0.29 (int 0, izq.) y 0.806 (int 1, der.) |
| `PJ01_Trote.fbx` | Loco_Correr viejo, de 22 a 21 frames | 4.53 m/s | AlPasoPJ01 en 0.381 (int 0) y 0.857 (int 1) |
| `PJ01_Loco_Idle.fbx` | Idle con los pies girados hacia adentro (izq. 22°, der. 33°) | — | ninguno (1–301) |
| `PJ01_Salto_Despegue.fbx` | Salto "Callejero": sale del idle, agachada de 3 frames, despega en el frame 4 | — | lastFrame 16→8 |
| `PJ01_Salto_Aire.fbx` | Cima con rodillas recogidas y bajada buscando el piso | — | lastFrame 17→10; loopTime 1→0 |
| `PJ01_Salto_Aterrizaje.fbx` | Cae con los pies del idle y termina exactamente en el idle | — | lastFrame 23→14 |
| `PJ01_Aterrizaje_Fuerte.fbx` | Tres puntos: impacto, pose corta (≈0.2 s) y se para al idle | — | lastFrame 45→23 |
| `PJ01_Salto_Correr_Despegue.fbx` | Nuevo. Desde Loco_Correr: empuja con el pie derecho; despega en el frame 4 | 4.5 m/s | .meta nuevo: 1–8 |
| `PJ01_Salto_Correr_Aire.fbx` | Nuevo. Zancada larga en el aire | — | .meta nuevo: 1–10, loopTime 0 |
| `PJ01_Salto_Correr_Aterrizaje.fbx` | Nuevo. Cae con el pie izq. y sigue corriendo | 4.5 m/s | .meta nuevo: 1–14 |
| `PJ01_Salto_Correr_Rodada.fbx` | Nuevo. Cae corriendo, rueda y se levanta corriendo | ≈4.5 m/s | .meta nuevo: 1–58 |
| `PJ01_CaminarApuntando_Derecha.fbx` | Loco_CaminarDer viejo + brazos sujetando la pistola a dos manos | 1.57 m/s (antes 2.0) | lastFrame 23→32 |

Respaldos en `imageassets/blender/_REVISION_TONY/respaldos/`. Prototipos en `_REVISION_TONY/revision_PJ01_animaciones.blend`.

## Falta (Unity) — locomoción
1. Importar `imageassets/blender/pj01_clips/PJ01_CorrerRapido.fbx` en `Assets/Arte/Personajes/PJ01/Clips` (igual que PJ01_Trote: Humanoid, avatar de PJ01, escala 1.8367, toma `CorrerRapido`, frames 1–34, loopTime 1; eventos AlPasoPJ01 en 0.424 (int 0) y 0.909 (int 1); velocidad 6.25 m/s).
2. Velocidades: `DatosPersonaje.velocidadCaminar` de 4.5 a ≈1.6; `velocidadCorrer` (Shift) de 7.5 a ≈4.5.
3. ReglasAnimacion: `VelocidadPJ01Caminar = 1.62`, `VelocidadPJ01Trote = 4.53`, clip rápido = 6.25 (reemplaza el sprint de 7.25); caminar lateral apuntando a la derecha = 1.57 m/s.
4. Override_Jugador / AnimacionJugador: caminar (1.6) → PJ01_Trote (4.5) → PJ01_CorrerRapido (6.25, solo con velocidad rápida activa). `PJ01_Sprint` deja de usarse.
5. Probar que las pisadas suenen a tiempo, que los pies no patinen, y caminar de lado apuntando con la pistola al frente.

## Falta (Unity) — salto
Los clips del salto vienen a velocidad real (30 fps, 3 frames antes de despegar, 14 en el aire con altura 0.45 m y gravedad 16). El controlador se genera con `ConstructorControladorBase.cs`: los cambios van ahí, no a mano en `ControladorBase.controller`.
6. Velocidad de estados: `SaltoInicio` 2.5 → 1; `SaltoAterrizaje` 2 → 1 (si el controlador es compartido, usar un multiplicador por personaje; revisar `AjustarSaltoT961.cs`).
7. Retrasar el impulso del salto 3 frames (0.1 s) en `MovimientoAPie.cs`, para que salga en el frame 4 del despegue.
8. `SaltoAire` ya no es ciclo: se queda en la pose de bajada.
9. Salto corriendo: si la velocidad horizontal es ≥ ~3 m/s, usar `PJ01_Salto_Correr_Despegue` → `PJ01_Salto_Correr_Aire`. Al tocar el piso corriendo: `PJ01_Salto_Correr_Aterrizaje` sin bloquear el movimiento. Tras una caída larga (caída entre ~6 y 9 m/s) corriendo: `PJ01_Salto_Correr_Rodada`, sin bloquear. Sin input al tocar el piso: `PJ01_Salto_Aterrizaje`.
10. Aterrizaje fuerte: el clip dura 23 frames (antes 45); solo probarlo.
11. Probar: saltar parado y corriendo, caer de una cornisa de ~2.6 m (tres puntos), y que todo regrese al idle o a correr sin brinco de pose.
