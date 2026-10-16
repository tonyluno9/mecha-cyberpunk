# T-020 Arena greybox del clandestino
Dueño: Alan | Rama: alan/T-020-arena-clandestino | Modelo: Sonnet 5.5, medium
Depende de: nada para la parte A (Arena/ y su asmdef ya están en main). Parte B: T-031 en main (`IArenaPelea`, `Registro<T>`).

## Objetivo
Existe la jaula del clandestino en greybox: piso, reja que no se cruza, gradas de relleno, luces de neón y la pantalla de la
arena. Cuando llega el contrato de Core, la jaula publica sus puntos de inicio y su pantalla por `IArenaPelea`, y así el flujo
de pelea de Tony (T-022) funciona en ella sin que nadie edite nada del otro. Es la parte de Alan del hito H1.

## Contexto
- Diseño: docs/diseno/ciudad.md (el clandestino abre de noche, frente al bar) y docs/diseno/estetica-hud.md (low-poly, niebla,
  neón azul, magenta, naranja y turquesa).
- Decisiones: D-009 (sin capas ni tags), D-011 (conteo y resultado en una pantalla de la arena), D-014 (mecha de 4 m, jaula de
  unos 24 × 24 m, peleadores a unos 12 m), D-026, D-029 (Clandestino.unity y Pruebas_Alan.unity son de Alan).
  Propuestas: D-alan-1 (Pruebas_Alan) en docs/decisiones-pendientes-alan.md.
- Esta tarea reemplaza la T-020 vieja: rama `alan/`, escena Pruebas_Alan.unity (no Pruebas_Compa), y sin flujo de pelea
  (el flujo es de Tony, T-022). Colocar el flujo y el jugador en Clandestino.unity es T-044.
- Escenas y prefabs se crean con un creador de editor (regla 6 de CLAUDE.md): `Arena/Editor/CreadorArena.cs` con su asmdef
  `MechaCyberpunk.Arena.Editor` (solo Editor, referencia Core y Arena). Ejemplo de creador: Mecha/Editor/CreadorEscenaMecha.cs
  (solo leerlo).
- Cómo saber si llegó T-031: `git show main:Assets/_Proyecto/Core/Arena/IArenaPelea.cs`. Si existe, mezcla main en
  `alan/fase-1` (paso 1h de la skill trabajar) y haz también la parte B.

## Qué hacer

### Parte A: sin esperar a Tony
1. Prefab `Arena/Prefabs/JaulaClandestino.prefab`:
   - Piso de 24 × 24 m (cubo plano), oscuro y un poco brillante, como concreto mojado.
   - Reja: postes y barras de cubos de unos 6 m de alto, con colliders que cubran todo el perímetro sin huecos por los que
     quepa una cápsula de 1 m de radio.
   - Fuera de la reja, gradas de cubos escalonados con siluetas de público (cápsulas), sin colliders dentro de la jaula.
   - Hijos vacíos `InicioJugador` e `InicioRival` a 12 m uno del otro, sobre el piso y mirándose.
   - Un quad oscuro de 8 × 4.5 m en lo alto de un lado de la reja, visible desde el centro a 3.5 m de alto, con un hijo vacío
     `PantallaArena` en el centro de su cara visible: su +Z apunta desde la jaula hacia la pantalla (la convención de
     `IArenaPelea`; un Canvas World Space alineado con él se lee desde la jaula).
2. Luces: ambiente bajo, de 4 a 6 luces de punto o foco con los colores del neón sobre la jaula y una luz cenital blanca y fría
   para que la pelea se lea. Materiales en `Arena/Materiales/`.
3. `Escenas/Clandestino.unity`: la jaula, sus luces, niebla leve (Lighting > Environment de la escena) y una cámara
   `CamaraVistaArena` a 3.5 m sobre `InicioJugador` mirando al centro (T-044 la quita cuando llegue el jugador).
4. `Escenas/Pruebas_Alan.unity`: la jaula y una cámara fija igual. T-040 cambia esta cámara por la cámara libre.

### Parte B: cuando T-031 esté en main
5. `Arena/Jaula/ArenaPelea.cs : MonoBehaviour, IArenaPelea` en la raíz de la jaula: referencias serializadas a `InicioJugador`,
   `InicioRival`, `PantallaArena` y al piso; `Centro` es el centro del piso; `TamanoPantalla` es 8 × 4.5; se agrega a
   `Registro<IArenaPelea>` en `OnEnable` y se quita en `OnDisable`; valida referencias en `Awake`; dibuja gizmos de los inicios
   y de la dirección de la pantalla.

## Carpetas permitidas
Assets/_Proyecto/Arena/, Assets/_Proyecto/Tests/PlayMode/Arena/, Assets/_Proyecto/Escenas/Clandestino.unity y
Assets/_Proyecto/Escenas/Pruebas_Alan.unity
No tocar: Core/, las carpetas de Tony (Mecha/, Combate/, UI/, IA/, Pelea/...), Pruebas_Tony.unity, ProjectSettings/ (tampoco
capas, tags ni Lighting global del proyecto) ni asmdefs de pruebas (los crea T-030).

## Criterios de aceptación (los comprueba el verificador)
Parte A:
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] PlayMode: un cubo con Rigidbody soltado sobre la jaula cae y queda sobre el piso
- [ ] PlayMode: una cápsula con `CharacterController` (radio 1 m, alto 4 m) empujada 3 s contra la reja desde el centro, en 8
      direcciones, nunca sale de la jaula
- [ ] Medido en vivo o en PlayMode: el piso mide 24 × 24 m (±0.5); la reja mide al menos 6 m; `InicioJugador` e `InicioRival`
      están a entre 11 y 13 m, sobre el piso y mirándose (producto punto de sus forward < -0.95)
- [ ] Medido: desde 3.5 m sobre `InicioJugador`, un raycast al centro de la pantalla no choca con nada antes; el +Z de
      `PantallaArena` apunta de la jaula hacia la pantalla (producto punto con la dirección centro → pantalla > 0.9)
- [ ] Medido: hay al menos 3 luces con tonos distintos de neón (azul, magenta, naranja o turquesa) y una blanca cenital;
      `RenderSettings.fog` está activo en Clandestino.unity
- [ ] Capturas desde la cámara a 3.5 m en Pruebas_Alan y en Clandestino: docs/capturas/T-020-1.png y -2.png; se ve la jaula
      completa, la reja, las gradas y la pantalla, y el centro de la jaula no está oscuro
- [ ] Consola sin errores al dar Play en las dos escenas
Parte B:
- [ ] PlayMode: al instanciar la jaula, `Registro<IArenaPelea>.Todos` la contiene y sus `InicioJugador`, `InicioRival` y
      `PantallaArena` coinciden con los hijos; al destruirla, sale del registro
- [ ] Si T-031 no estaba en main al terminar la parte A, la tarea queda "hecha" con la parte B anotada en docs/revision-alan.md

## Criterios de sensación (para un humano)
- El neón se ve cyberpunk y aun así el centro de la jaula se lee bien para pelear.
- Las gradas del fondo se pierden un poco en la niebla; la escala se siente de mecha (la reja es más alta que un mecha de 4 m).

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Arena/): las de física y medidas de la parte A y la del registro de la parte B.
  Si T-030 todavía no está en main, esas pruebas compilan en el ensamblado viejo de PlayMode; no crees asmdefs de pruebas.

## Fuera de alcance
- Flujo de pelea, rival y textos de la pantalla (T-021 y T-022, de Tony); colocarlos en Clandestino.unity (T-044)
- Presentador, público animado, sonido
- Exterior e interior del edificio del clandestino (T-042 y A-F3)
- Iluminación horneada y postprocesado
