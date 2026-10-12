# T-022 Flujo de pelea
Dueño: Tony (D-026) | Rama: tony/T-022-flujo-pelea | Modelo: Sonnet 5.5, medium
Depende de: T-030 (carpeta Pelea/), T-031 (`IArenaPelea` y `Registro<T>`), T-021 (rival)

## Objetivo
Una pelea completa: conteo, pelea, KO, resultado y revancha, entre el MechaJugador y el rival, en cualquier arena que cumpla
`IArenaPelea`. Tony la prueba en Pruebas_Tony.unity con una arena propia y entrega `Pelea/Prefabs/FlujoPelea.prefab`, que
Alan solo coloca en Clandestino.unity (T-044) para cerrar H1.

## Contexto
- Diseño: docs/diseno/combate.md, docs/diseno/personaje-progresion.md (perder no es game over: puedes volver a retar),
  docs/diseno/estetica-hud.md (en el mecha nada flota).
- Decisiones: D-007 (EventosPelea), D-008 (la revancha usa su propia `InputAction`), D-010 (uGUI Text), D-011 (conteo y resultado
  en una pantalla de la arena), D-013 (la revancha restaura la vida), D-016 (gana el primer KO), D-029 (FlujoPelea.prefab).
  Propuestas: D-tony-3 (pantalla de la arena), D-tony-10 (cómo encuentra todo el flujo y por qué en Pruebas_Tony arranca con P),
  D-tony-12 (ArenaDePrueba de Tony).
- Contratos: `IPeleador` (`ActivarControl`, `PrepararPelea`, `Vida`, `Lado`), `IVida.KO`, `ResultadoPelea`, `EventosPelea`,
  `IArenaPelea` (lee la convención de `PantallaArena` en su comentario) y `Registro<IArenaPelea>`.
- Esta tarea reemplaza la T-022 vieja de Alan: el flujo vive en Pelea/ (ya no en Arena/Flujo/), no usa Clandestino.unity
  ni la jaula de Alan, y encuentra la arena por el contrato.
- Pelea/ no referencia IA/: el rival busca solo a su objetivo (T-021), así que el flujo lo trata como `IPeleador`.
- Cuidado: `GolpeBloqueoPruebas` y `VidaJugadorPruebas` cargan Pruebas_Tony.unity y esperan al jugador libre en su lugar.
  Por eso en Pruebas_Tony la pelea no arranca sola: se inicia con P.

## Qué hacer
1. Lógica pura `Pelea/Flujo/MaquinaFlujoPelea.cs`: estados Preparando, Conteo, Peleando, KO y Resultado.
   `Avanzar(dt)` lleva el conteo 3, 2, 1, 0 con la duración de cada número y avisa cada cambio (un `dt` grande no se salta
   números); `NotificarKO(LadoPelea)` solo cuenta en Peleando y el segundo KO se ignora; después del KO espera una pausa y pasa
   a Resultado con el ganador y la duración; `PedirRevancha()` solo funciona en Resultado y vuelve a Preparando.
2. `Pelea/Datos/ConfigFlujoPelea.cs` y `ConfigFlujoPelea_Base.asset`: segundos por número del conteo, pausa después del KO,
   textos ("¡PELEA!", "KO", "GANASTE", "PERDISTE", "R: revancha"), tamaño de letra y colores de la pantalla.
3. `Pelea/Flujo/DirectorPelea.cs` (MonoBehaviour):
   - Arena: campo `GameObject` opcional; si está vacío, en `Start` usa la `IArenaPelea` registrada más cercana a él; si no hay
     ninguna, se suscribe a `Registro<IArenaPelea>.Agregado` y avisa una sola vez con `Debug.LogWarning`.
   - Jugador: campo opcional; si está vacío, el `IPeleador` con `Lado` Jugador de la escena (búsqueda en `Start`, nunca en `Update`).
   - Rival: campo `rivalEnEscena` opcional; si está vacío, instancia `prefabRival` (IA/Prefabs/RivalBasico.prefab) en
     `InicioRival`.
   - Campo `empezarAlArrancar` (true en el prefab) y `public void EmpezarPelea()`.
   - Preparando: `PrepararPelea` de los dos con `InicioJugador` e `InicioRival`. Conteo: `EventosPelea.PublicarConteo` en cada
     número. Peleando: control a los dos y `PublicarInicio`. En el KO de cualquiera quita el control a los dos.
     Resultado: `PublicarFin` con el `ResultadoPelea`. Revancha con su `InputAction` (tecla R), solo en Resultado.
4. `Pelea/Flujo/PantallaPelea.cs`: Canvas World Space con un uGUI `Text` grande. En `Start` se coloca en
   `IArenaPelea.PantallaArena` (unos 5 cm hacia la jaula para no hundirse en la pantalla de la arena) y se ajusta a
   `TamanoPantalla`. Escucha `EventosPelea` y muestra 3, 2, 1, "¡PELEA!", "KO" y el resultado con "R: revancha".
5. Prefab `Pelea/Prefabs/FlujoPelea.prefab` (con un creador en `Pelea/Editor/`): raíz con `DirectorPelea` y el config, hijo
   `PantallaPelea`, `prefabRival` asignado, `empezarAlArrancar` true. Alan lo coloca tal cual: no debe necesitar ningún ajuste.
6. Arena propia de Tony en `Pelea/Pruebas/`: `ArenaDePrueba.cs : MonoBehaviour, IArenaPelea` (se agrega al registro en
   `OnEnable` y se quita en `OnDisable`) y `ArenaDePrueba.prefab`: piso de 24 × 24 m, muros de 6 m con colliders, `InicioJugador`
   e `InicioRival` a 12 m mirándose, y un quad oscuro de unos 8 × 4.5 m con su `PantallaArena` según la convención del
   contrato. Sirve también de ejemplo para la jaula de Alan.
7. `Pelea/Pruebas/IniciadorPeleaDePrueba.cs`: con una `InputAction` del inspector (tecla P) llama a `EmpezarPelea`.
8. En Pruebas_Tony.unity: la ArenaDePrueba lejos del saco y del atacante (por ejemplo a 80 m del origen), una instancia de
   FlujoPelea con `empezarAlArrancar` en false y `rivalEnEscena` apuntando al rival de T-021, y el iniciador.

## Carpetas permitidas
Assets/_Proyecto/Pelea/, Assets/_Proyecto/Tests/EditMode/Pelea/, Assets/_Proyecto/Tests/PlayMode/Pelea/ y
Assets/_Proyecto/Escenas/Pruebas_Tony.unity
No tocar: Core/, Mecha/ (ni el prefab del jugador), Combate/, UI/, IA/ (salvo usar su prefab), carpetas y escenas de Alan
(Arena/, Clandestino.unity), ProjectSettings/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, incluidas las pruebas de T-012, T-013 y T-021 sin cambios
- [ ] EditMode: MaquinaFlujoPelea cumple todos los casos de "Pruebas automáticas"
- [ ] PlayMode (arena, jugador y FlujoPelea instanciados en la prueba, sin campos asignados): el director encuentra la
      ArenaDePrueba por el registro y coloca al jugador y al rival en sus inicios (a menos de 0.1 m y 5°); con dos arenas usa la
      más cercana; sin arena avisa una sola vez y no lanza excepciones
- [ ] PlayMode con entrada simulada: durante el conteo, mantener W no mueve al jugador; después de "¡PELEA!", sí
- [ ] PlayMode: el texto de la pantalla pasa por "3", "2", "1", "¡PELEA!"; con el rival en KO dice "KO" y luego "GANASTE" con
      "R: revancha"; con el jugador en KO, "PERDISTE"; `PeleaTerminada` llega una vez con el ganador correcto
- [ ] PlayMode: después del KO ni el jugador ni el rival tienen control
- [ ] PlayMode con entrada simulada: R durante el conteo o la pelea no hace nada; R en el resultado deja a los dos en sus
      inicios con vida completa, el rival de pie, y vuelve el conteo
- [ ] PlayMode: tres peleas seguidas con revancha (gana el jugador, gana el rival, gana el jugador) sin errores ni excepciones
      en la consola (`LogAssert.NoUnexpectedReceived`)
- [ ] PlayMode sobre la escena real Pruebas_Tony: P inicia la pelea en la ArenaDePrueba y el flujo llega a Peleando
- [ ] El Canvas de la pantalla queda a menos de 0.1 m de `PantallaArena` y alineado con ella (medido en vivo)
- [ ] Captura desde la cabina en `InicioJugador` durante el conteo y otra con "GANASTE": docs/capturas/T-022-1.png y -2.png;
      el texto se lee completo

## Criterios de sensación (para un humano)
- En Pruebas_Tony, con P y R: tres peleas seguidas, ganando y perdiendo. El ritmo del conteo y la pausa después del KO se
  sienten bien; la pantalla se lee desde la cabina sin esfuerzo.

## Pruebas automáticas
- EditMode (Tests/EditMode/Pelea/): MaquinaFlujoPelea: el conteo avisa 3, 2, 1 y 0 en orden y luego pasa a Peleando; un `dt`
  grande no se salta números; un KO fuera de Peleando se ignora; el segundo KO se ignora y gana el lado del primero; después
  de la pausa llega a Resultado con el ganador correcto; `PedirRevancha` solo funciona en Resultado y vuelve a Preparando.
- PlayMode (Tests/PlayMode/Pelea/): los criterios de arriba, con un `ConfigFlujoPelea` de prueba con tiempos cortos.

## Fuera de alcance
- Colocar el flujo en Clandestino.unity (T-044, Alan)
- Presentador que dice el alias, apuestas, premios y dinero; rounds y límite de tiempo; ligas
- Daño persistente entre peleas (D-013); menús, pausa y transiciones de cámara
