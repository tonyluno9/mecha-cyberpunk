# T-060 Jugador a pie en el barrio de Alan (H2, lado de Tony)
Dueño: Tony | Rama: tony/T-060-jugador-en-barrio-alan | Modelo: Sonnet 5.5, medium
Depende de: T-059
**Espera A-F1.** Motivo: necesita en main el barrio y Pruebas_Alan.unity (T-040, con la parte B: `IJugador` en la cámara libre y
los puntos de aparición), el reloj (T-041, parte B: `IRelojMundo` registrado), los locales con `ILugar` e `IPuerta` (T-042) y el
bar con su zona que publica `EventosLugares` (T-043). El orquestador la deja en docs/tareas/bloqueadas/ hasta que estén en main.
Cómo saber si llegó: `git ls-tree main -- Assets/_Proyecto/Mundo/Lugares/` muestra `PuertaLocal.cs` y `ZonaInterior.cs`.

## Objetivo
El jugador a pie de Tony funciona sin cambios en el barrio real de Alan: aparece en la puerta de la casa, camina por la calle,
sigue el objetivo hasta el bar, abre su puerta y entra y sale. Es la prueba de cierre de T-F3 del lado de Tony (hito H2).
Es solo integración: si algo falla por el lado de Alan, se anota, no se arregla aquí.

## Contexto
- Hoja de ruta, T-F3 y H2. Decisiones: D-029 (las escenas de Alan son suyas: la prueba NO las guarda ni las edita; instancia el
  prefab en tiempo de juego). Propuestas D-tony-22 y D-tony-25 en docs/decisiones-pendientes-tony.md.
- Lee las tareas de Alan ya hechas (docs/tareas/hechas/T-040 a T-043) para saber los `Id` de los puntos de aparición, la hora en
  que abre el bar y cómo se llaman sus escenas.
- Los tests de Tony no pueden referenciar ensamblados de Alan: todo se encuentra por `Registro<IJugador>`,
  `Registro<IPuntoAparicion>`, `Registro<IRelojMundo>`, `Registro<ILugar>`, `Registro<IPuerta>` y `EventosLugares`.
- La cámara libre de Alan también es `IJugador`: la prueba apaga el GameObject de todo `IJugador` registrado que no sea el
  `JugadorAPie` (como `Component`), sin referenciar su clase.

## Qué hacer
1. `Tests/PlayMode/Personaje/BarrioAlanPruebas.cs`: carga `Escenas/Pruebas_Alan.unity` con `EditorSceneManager.LoadSceneInPlayMode`,
   instancia `JugadorAPie.prefab` con `idAparicion` "Casa_Puerta" (o el id que diga T-040), apaga los otros `IJugador`, y pone el
   reloj a las 22:00 con `AvanzarHasta`. Con entrada simulada:
   a) camina 4 s con W por la banqueta y avanza más de 12 m sin caer del piso;
   b) se coloca con `Colocar` frente a la `Entrada` del Clandestino, fija el objetivo al Bar y sigue solo la flecha del HUD hasta la
   `PosicionEntrada` del Bar (están frente a frente, a menos de 30 m);
   c) E abre la puerta del Bar (`PuertaIntentada` con `Abierta`), W entra y llega `JugadorEntro(Bar)`; da media vuelta y sale
   con `JugadorSalio(Bar)`;
   d) a las 12:00, frente al Clandestino, E da `CerradaPorHorario` y el HUD dice el horario.
2. Si hace falta un ajuste del lado de Tony (por ejemplo el alcance de la interacción para las puertas de Alan), se hace en
   Personaje/ y se anota. Si falta algo del lado de Alan, se anota para docs/revision-tony.md y la tarea queda bloqueada con la
   evidencia.

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Personaje/ y, solo para ajustes del lado del jugador, Assets/_Proyecto/Personaje/ y
Assets/_Proyecto/UI/HudAPie/
No tocar: nada de Alan (Mundo/, Arena/, Pruebas_Alan.unity, Ciudad.unity), Core/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa con BarrioAlanPruebas contada
- [ ] BarrioAlanPruebas pasa: los pasos a) a d) se cumplen; `JugadorEntro(Bar)` y `JugadorSalio(Bar)` llegan una vez cada uno
- [ ] Durante la prueba hay un solo `IJugador` registrado (el de Tony) y `SceneManager.sceneCount` es 1 al cruzar la puerta
- [ ] `git status` después de correr las pruebas no muestra cambios en escenas de Alan
- [ ] Captura del jugador en Pruebas_Alan, frente al bar con el HUD y el objetivo: docs/capturas/T-060-1.png
- [ ] Consola sin errores durante la prueba

## Criterios de sensación (para un humano)
- Tony, en Ciudad.unity (cuando Alan coloque el prefab, D-tony-25), camina de la casa al clandestino, entra al bar y ve el HUD:
  la escala de la persona cabe bien en las calles, banquetas y puertas de Alan.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Personaje/): BarrioAlanPruebas.

## Fuera de alcance
- Colocar el prefab en Ciudad.unity (lo hace Alan, D-029); el carro de Alan (T-061, espera A-F2)
- Cambiar cualquier cosa del mundo; interiores de casa y clandestino (A-F3)
