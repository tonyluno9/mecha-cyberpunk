# T-121 Deudas de T-F5: daño persistente solo en el mecha de la historia y creador de Pruebas_Calle
Dueño: Tony | Rama: tony/T-121-deudas-t-f5 | Modelo: Sonnet 5.5, medium
Depende de: nada (T-F5 de Tony cerrada)

## Objetivo
Se quitan las dos deudas de T-F5 que estorban a T-F6: (1) la prueba de Alan `PeleaClandestinoPruebas.TresPeleasSeguidasConRevancha`
vuelve a pasar sin tocar nada de Alan, porque el daño persistente queda solo en el mecha de la historia (`MechaEstacionado.prefab`)
y el `MechaJugador.prefab` de arranque Directo (pruebas de pelea y el Clandestino.unity suelto de Alan) se restaura en cada pelea;
(2) `CreadorEscenaCalle.Crear` vuelve a ser la única forma de armar Pruebas_Calle, completa e idempotente, y `LlamadoMechaPruebas`
deja de depender del orden de los objetos.

## Contexto
- Revisión de T-F5 (docs/revision-tony.md, "Deudas y avisos" a y c; bitácoras 2026-10-16 T-111 y T-112): la prueba de Alan espera
  vida completa en la revancha y falla desde T-103; `Crear` reordena objetos y rompe `LlamadoMechaPruebas` (que usa
  `FindFirstObjectByType`), por eso T-111 y T-112 agregaron objetos con métodos sueltos (`AgregarGuardadoDePrueba`,
  `AgregarPeleaDePrueba`).
- Decisiones: D-tony-42 (daño persistente; lo modifica D-tony-59), D-060 (arranque Directo y Estacionado), D-068 (Pruebas_Calle la
  genera solo su creador), D-029 (no se toca nada de Alan). Propuesta nueva: D-tony-59 en docs/decisiones-pendientes-tony.md.
- Hoy `CreadorMechaPersistente` pone `MechaPersistente` en `MechaJugador.prefab` y la variante `MechaEstacionado.prefab` lo hereda.
  `BancoTaller` busca `MechaPersistente` con `GetComponent` (solo se registra en arranque Estacionado).
- `Tests/PlayMode/Mecha/DanioPersistentePruebas.cs` prueba la persistencia en Pruebas_Tony (arranque Directo) y en Pruebas_Calle.

## Qué hacer
1. `CreadorMechaPersistente`: `MechaPersistente` va en `MechaEstacionado.prefab` (como componente agregado en la variante) y no en
   `MechaJugador.prefab`. Idempotente: si el prefab base ya lo tiene, lo quita. Corre el creador y los creadores de los que dependa
   para que los prefabs queden bien (nunca YAML a mano).
2. Lo que esté en `MechaJugador.prefab` y use `MechaPersistente` (`BancoTaller` y cualquier otro que encuentres) tolera que no exista
   sin errores ni warnings en arranque Directo.
3. `DanioPersistentePruebas`: los casos de Pruebas_Tony que esperaban persistencia pasan a usar un mecha con `MechaPersistente`
   (instancia `MechaEstacionado.prefab` o arma el caso en Pruebas_Calle); agrega el caso
   `Directo_SinMechaPersistente_LaRevanchaRestauraLaVida` (MechaJugador.prefab en Pruebas_Tony: pelea con daño, revancha a vida
   completa). Las demás pruebas de Tony que dependan de la persistencia en Directo se ajustan igual, sin aflojar lo que miden.
4. `CreadorEscenaCalle.Crear` arma la escena completa en un orden fijo, incluyendo lo que hoy agregan `AgregarPeleaDePrueba` y
   `AgregarGuardadoDePrueba` (esos métodos pueden quedar, idempotentes, llamados desde `Crear`). Correr `Crear` dos veces seguidas no
   cambia el archivo de la escena la segunda vez.
5. `LlamadoMechaPruebas` (y cualquier prueba de Pruebas_Calle que use `FindFirstObjectByType` sobre un tipo que puede haber más de
   una vez) busca lo que necesita sin depender del orden: por nombre del objeto, por el registro del contrato o comprobando que hay
   exactamente uno.
6. Regenera Pruebas_Calle con `Crear` y corre todo.

Nota para el orquestador (no es trabajo del implementador): `docs/tareas/en-curso/T-106-premios-pelea.md` quedó sin mover; muévelo a
docs/tareas/hechas/ (T-106 ya está cerrada y en tony/fase-5). En docs/revision-tony.md, la prueba humana de T-103 en Pruebas_Tony
cambia: el daño persistente se prueba en Pruebas_Calle (o Pruebas_Prologo cuando exista), no en Pruebas_Tony.

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Editor/CreadorMechaPersistente.cs, Editor/CreadorEscenaCalle.cs, Prefabs/ con sus creadores, Taller/ y lo que
toque tolerar la ausencia), Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity (con su creador), Assets/_Proyecto/Tests/PlayMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/Pelea/ (solo si una prueba depende de la persistencia en Directo)
No tocar: Core/, Arena/ y todo lo de Alan (en especial Tests/PlayMode/Arena/), Pruebas_Tony.unity, Pruebas_APie.unity.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa salvo los fallos de Alan ya conocidos de `JaulaClandestinoPruebas` (los 9 de main puro);
      el verificador lista los fallos y comprueba que ninguno es de Tony
- [ ] `PeleaClandestinoPruebas.TresPeleasSeguidasConRevancha_GanaJugadorGanaRivalGanaJugador_SinErrores` pasa sobre la rama (sin cambiar
      nada de Arena/ ni de Tests/PlayMode/Arena/)
- [ ] `MechaEstacionado.prefab` tiene `MechaPersistente` y `MechaJugador.prefab` no (comprobado leyendo los prefabs con código o en el
      editor, no a ojo en el YAML)
- [ ] `DanioPersistentePruebas` pasa, incluido el caso nuevo de Directo; `RecorridoTallerPruebas` sigue pasando
- [ ] Correr `CreadorEscenaCalle.Crear` dos veces deja `git status` sin cambios en Pruebas_Calle.unity la segunda vez
- [ ] `LlamadoMechaPruebas` pasa después de regenerar Pruebas_Calle con `Crear`
- [ ] `git diff tony/fase-5 --stat` no toca nada fuera de las carpetas permitidas

## Criterios de sensación (para un humano)
- Ninguno nuevo. (Tony: en Pruebas_Tony la revancha vuelve a empezar a vida completa; el daño persistente se juega en Pruebas_Calle.)

## Pruebas automáticas
- PlayMode: DanioPersistentePruebas (caso nuevo de Directo), LlamadoMechaPruebas, RecorridoTallerPruebas; la de Alan
  PeleaClandestinoPruebas como comprobación.

## Fuera de alcance
- Arreglar `JaulaClandestinoPruebas` (Alan); pruebas frágiles por tiempo de frame (T-122)
- Qué mecha pelea en el clandestino durante la demo (T-128: el de la historia, con `EntradaArena`)
