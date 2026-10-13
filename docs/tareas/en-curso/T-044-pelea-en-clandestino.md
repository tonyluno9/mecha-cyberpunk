# T-044 Pelea completa en Clandestino.unity (hito H1)
Dueño: Alan | Rama: alan/T-044-pelea-en-clandestino | Modelo: Sonnet 5.5, medium
Depende de: T-020 con su parte B (la jaula cumple `IArenaPelea`), T-022 en main (`Pelea/Prefabs/FlujoPelea.prefab`) y
MechaJugador.prefab (T-013, ya en main)

## Objetivo
En Clandestino.unity se pelea de verdad: el mecha de Tony contra su rival, en la jaula de Alan, con conteo, KO, resultado y
revancha. Alan solo coloca los prefabs de Tony, sin editarlos. Con esto se cierra el hito H1.

## Contexto
- Hoja de ruta: H1 (3 peleas seguidas con revancha en Clandestino.unity, sin errores).
- Decisiones: D-026 y D-029 (Alan coloca los prefabs de Tony sin editarlos; si algo no le sirve, lo pide en
  docs/revision-alan.md y se vuelve tarea de Tony). Propuesta: D-alan-7 (esta tarea aparte) en docs/decisiones-pendientes-alan.md.
- `FlujoPelea.prefab` encuentra solo la arena (por `Registro<IArenaPelea>`), al jugador (el `IPeleador` del lado Jugador) y crea
  al rival en `InicioRival`. Arranca la pelea solo al dar Play. La revancha es con R.
- Las pruebas de Alan no pueden referenciar los ensamblados de Tony: se habla con la pelea solo por Core (`IPeleador`,
  `IRecibeDanio`, `EventosPelea`, `Registro<IArenaPelea>`), buscando los peleadores con
  `FindObjectsByType<MonoBehaviour>` y filtrando por la interfaz.
- Cómo saber si llegó T-022: `git show main:Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab`.

## Qué hacer
1. Con el creador de `Arena/Editor/`: en Clandestino.unity coloca `Mecha/Prefabs/MechaJugador.prefab` en `InicioJugador` y
   `Pelea/Prefabs/FlujoPelea.prefab` en el centro de la jaula, sin overrides; quita `CamaraVistaArena` (la cámara es la de la
   cabina). No cambies nada más de la escena.
2. Prueba de PlayMode `Tests/PlayMode/Arena/PeleaClandestinoPruebas.cs` sobre la escena real (con `InputTestFixture` para la R):
   - Al empezar, el jugador está en `InicioJugador` y el rival en `InicioRival` de la jaula (a menos de 0.1 m), y durante el
     conteo ninguno tiene control.
   - Pelea 1: después de `PeleaIniciada`, le aplica al rival un daño mayor que su vida por `IRecibeDanio`;
     `PeleaTerminada` llega una vez con ganador Jugador.
   - R: los dos vuelven a sus inicios con vida completa y vuelve el conteo.
   - Pelea 2: daño al jugador hasta KO; gana el Rival. Pelea 3: gana el Jugador.
   - Sin errores ni excepciones en toda la prueba (`LogAssert.NoUnexpectedReceived`).

## Carpetas permitidas
Assets/_Proyecto/Escenas/Clandestino.unity, Assets/_Proyecto/Arena/Editor/ y Assets/_Proyecto/Tests/PlayMode/Arena/
No tocar: los prefabs y carpetas de Tony (Mecha/, Pelea/, IA/...), Core/, Pruebas_Tony.unity, ProjectSettings/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, con PeleaClandestinoPruebas contada
- [ ] Clandestino.unity tiene una sola cámara activa (la de la cabina) y ningún override sobre MechaJugador ni FlujoPelea
      (`PrefabUtility.GetPropertyModifications` solo con la posición y rotación de la raíz)
- [ ] PeleaClandestinoPruebas pasa: tres peleas seguidas con revancha, ganando y perdiendo, sin errores
- [ ] Unity en vivo: el Canvas del flujo queda sobre la `PantallaArena` de la jaula (a menos de 0.1 m)
- [ ] Capturas desde la cabina durante el conteo y en el resultado: docs/capturas/T-044-1.png y -2.png; se ve la jaula y el
      texto de la pantalla se lee completo
- [ ] `git diff main --stat` no toca nada de Tony

## Criterios de sensación (para un humano)
- Prueba de cierre de H1, para Tony y Alan: en Clandestino.unity, tres peleas seguidas con R, ganando y perdiendo. La luz de la
  jaula deja leer al rival y su aviso naranja; la pantalla se lee desde la cabina; la pelea se siente igual que en Pruebas_Tony.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Arena/): PeleaClandestinoPruebas, descrita arriba.

## Fuera de alcance
- Cambios al flujo, al rival o al jugador (si hacen falta, se piden en docs/revision-alan.md)
- Presentador, público animado, sonido
