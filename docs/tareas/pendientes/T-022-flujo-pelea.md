# T-022 Flujo de pelea
Dueño: Compañero | Rama: compa/T-022-flujo-pelea | Modelo: Sonnet 5.5, medium
Depende de: T-001, T-020, T-021, y T-013 en main para el paso 6 (el prefab del jugador con vida y `IPeleador`)

## Objetivo
Una pelea completa en Clandestino.unity: conteo, pelea, KO, resultado y revancha, con el mecha de Tony contra el rival.
Con esto se cumple la meta de la fase 1: puedo golpear a un rival que me golpea, y uno de los dos gana.

## Contexto
- Diseño: docs/diseno/combate.md, docs/diseno/personaje-progresion.md (perder no es game over: puedes volver a retar)
  y docs/diseno/estetica-hud.md (en el mecha nada flota).
- Decisiones: D-007 (EventosPelea), D-010 (uGUI Text), D-011 (pantalla de la arena), D-013 (la revancha restaura la vida),
  D-016 (fin de pelea: primer KO).
- Contratos de Core: `IPeleador` (`ActivarControl`, `PrepararPelea`, `Vida`), `IVida.KO`, `LadoPelea`, `ResultadoPelea`,
  `EventosPelea.PublicarConteo`, `PublicarInicio` y `PublicarFin`.
- Usa la jaula y `PantallaArena` (T-020), el rival y el PeleadorDePrueba (T-021) y Mecha/Prefabs/MechaJugador.prefab de Tony (T-013).
  No edites el prefab de Tony: solo colócalo en la escena.
- Ensamblado: Arena/ referencia Core, Unity.InputSystem y UnityEngine.UI.

## Qué hacer
1. Lógica pura en `Arena/Flujo/MaquinaFlujoPelea.cs`: estados Preparando, Conteo, Peleando, KO y Resultado.
   - `Avanzar(dt)` lleva el conteo 3, 2, 1, 0 con la duración de cada número y avisa cada cambio.
   - `NotificarKO(LadoPelea)` solo cuenta en Peleando; el segundo KO se ignora (D-016).
   - Después del KO espera una pausa y pasa a Resultado con el ganador y la duración de la pelea.
   - `PedirRevancha()` solo funciona en Resultado y regresa a Preparando.
2. `Arena/Flujo/ConfigFlujoPelea.cs` y su asset en Arena/Datos/: segundos por número del conteo, pausa después del KO
   y textos de la pantalla ("¡PELEA!", "KO", "GANASTE", "PERDISTE", "R: revancha").
3. `Arena/Flujo/DirectorPelea.cs` (MonoBehaviour): recibe en el inspector el jugador, el rival (como `GameObject`, saca
   `IPeleador` en `Awake`), `InicioJugador` e `InicioRival`, y una `InputAction` para la revancha (tecla R).
   - Preparando: `PrepararPelea` en los dos con sus poses y los deja sin control.
   - Conteo: publica `EventosPelea.PublicarConteo` en cada número.
   - Peleando: da control a los dos y publica `PublicarInicio`.
   - Escucha `KO` de la vida de cada uno; en el KO quita el control a los dos.
   - Resultado: publica `PublicarFin` con el `ResultadoPelea`.
4. `Arena/Flujo/PantallaArena.cs`: Canvas World Space sobre `PantallaArena` con un uGUI `Text` grande. Escucha
   `EventosPelea` y muestra 3, 2, 1, "¡PELEA!", "KO" y el resultado con "R: revancha". Debe leerse desde la cabina.
5. En Pruebas_Compa.unity arma la pelea con el PeleadorDePrueba como jugador y verifica el flujo completo.
6. Cuando T-013 esté en main: en Clandestino.unity pon MechaJugador.prefab en `InicioJugador` (borra cualquier otra cámara),
   el rival en `InicioRival` con el jugador como objetivo, el `DirectorPelea` y la pantalla. Si T-013 no está en main,
   para al terminar el paso 5 y dilo.

## Carpetas permitidas
Assets/_Proyecto/Arena/, Assets/_Proyecto/Tests/EditMode/Arena/, Assets/_Proyecto/Escenas/Clandestino.unity
y Assets/_Proyecto/Escenas/Pruebas_Compa.unity
No tocar: Core/, Mecha/ (ni el prefab del jugador), Combate/, UI/, IA/ (salvo usar sus prefabs), Pruebas_Tony.unity
ni ProjectSettings/.

## Criterios de aceptación (se comprueban jugando, en Clandestino.unity)
- [ ] Al dar Play, la pantalla de la arena muestra 3, 2, 1 y "¡PELEA!", y ni mi mecha ni el rival se mueven antes
- [ ] Durante la pelea el rival me golpea y yo a él, con la vida bajando en mi cabina
- [ ] Si mi vida llega a 0, todo se detiene, la pantalla dice "KO" y luego "PERDISTE" con "R: revancha"
- [ ] Si la vida del rival llega a 0, el rival cae, la pantalla dice "KO" y luego "GANASTE"
- [ ] Después del KO, nadie se puede mover ni golpear
- [ ] Con R en el resultado, los dos vuelven a su punto de inicio con la vida completa, el rival de pie, y vuelve el conteo
- [ ] R durante el conteo o la pelea no hace nada
- [ ] Tres peleas seguidas con revancha, ganando y perdiendo, sin errores en la consola

## Pruebas automáticas
EditMode, en Tests/EditMode/Arena/:
- MaquinaFlujoPelea: el conteo avisa 3, 2, 1 y 0 en orden y luego pasa a Peleando; un `dt` grande no se salta números;
  un KO fuera de Peleando se ignora; el segundo KO se ignora y gana el lado del primero; después de la pausa llega a
  Resultado con el ganador correcto; `PedirRevancha` solo funciona en Resultado y vuelve a Preparando.

## Fuera de alcance
- Presentador que dice el alias, apuestas, premios y dinero
- Rounds, límite de tiempo, ligas
- Daño persistente entre peleas (D-013)
- Menús, pausa, transiciones de cámara
