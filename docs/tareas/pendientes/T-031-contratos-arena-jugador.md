# T-031 Contratos de arena y jugador
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-031-contratos-arena-jugador | Modelo: Sonnet 5.5, medium
Depende de: T-030 (el ensamblado de pruebas de Core)

## Objetivo
En Core/ existen los contratos para que la jaula de Alan y el flujo de pelea de Tony se encuentren sin referenciarse
(`IArenaPelea`), para que el mundo sepa dónde está el jugador y en qué modo va (`IJugador`, `ModoJugador`) y dónde puede
aparecer (`IPuntoAparicion`), y una forma única de encontrar quién implementa cada contrato (`Registro<T>`).
Alan los necesita para T-020 (parte B) y T-040 (parte B); Tony para T-022.

## Contexto
- Decisiones: D-014 (escala: jaula de 24 × 24 m, peleadores a 12 m), D-026, D-027 (quien posee el dato lo implementa),
  D-029 (Tony entrega `FlujoPelea.prefab` y Alan solo lo coloca). Propuestas: D-tony-2 (Registro), D-tony-3 (pantalla de la
  arena) en docs/decisiones-pendientes-tony.md.
- Convenciones: .claude/rules/unity-csharp.md (un tipo por archivo, estado estático limpio con
  `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, como `EventosPelea`).
- Los comentarios `///` de cada interfaz SON el contrato: escribe ahí quién lo implementa, cuándo se registra y qué garantiza
  cada miembro, como en `IRecibeDanio` e `IPeleador`. El revisor los lee con la mirada de Alan (D-032): ¿le alcanzan?
- Es una tarea de Core: se integra a main en cuanto el verificador la apruebe (paso 1e de la skill trabajar).

## Qué hacer
1. `Core/Servicios/Registro.cs`: `public static class Registro<T> where T : class`
   - `void Agregar(T elemento)`: ignora null y duplicados. `void Quitar(T elemento)`: si no estaba, no hace nada.
   - `IReadOnlyList<T> Todos`, `T Actual` (el último agregado que sigue registrado, o null), `int Cantidad`.
   - `event Action<T> Agregado` y `event Action<T> Quitado`, una vez por cambio real.
   - Contrato en el comentario: los MonoBehaviour se agregan en `OnEnable` y se quitan en `OnDisable`; quien busca lo hace
     en `Start` o después (no en `Awake`), o se suscribe a `Agregado` si lo que busca puede llegar después.
2. `Core/Servicios/Registros.cs`: clase estática no genérica con `LimpiarTodos()`, que vacía todos los `Registro<T>` que se
   hayan usado y quita sus suscriptores. Cada `Registro<T>` se apunta aquí en su constructor estático. `LimpiarTodos` corre
   sola al entrar a Play (SubsystemRegistration), porque el proyecto no recarga el dominio.
3. `Core/Arena/IArenaPelea.cs` (la implementa Alan en la jaula; la usa el flujo de Tony):
   - `string Nombre`, `Pose InicioJugador`, `Pose InicioRival` (posición en el piso y mirando hacia el otro peleador),
     `Vector3 Centro` (centro del piso), `Transform PantallaArena`, `Vector2 TamanoPantalla` (ancho y alto en metros).
   - Convención de la pantalla, escrita en el comentario: `PantallaArena` está en el centro de la cara visible de la pantalla;
     su +Z (forward) apunta desde la jaula hacia la pantalla, como la mirada de quien la ve, así que un Canvas World Space
     alineado con ese Transform se lee desde dentro de la jaula. El flujo pone ahí el conteo y el resultado.
   - La arena se agrega a `Registro<IArenaPelea>` en `OnEnable`. Si hay varias, el flujo usa la más cercana a él.
4. `Core/Jugador/ModoJugador.cs`: `enum ModoJugador { APie, Carro, Mecha }`.
5. `Core/Jugador/IJugador.cs` (la implementa Tony en el jugador; Alan puede implementarla en su cámara libre de pruebas):
   - `Transform Raiz`, `ModoJugador Modo`, `Camera Camara` (la que ve el jugador ahora), `event Action<ModoJugador> ModoCambio`,
     `void Colocar(Pose pose)` (teletransporta sin atravesar nada: si tiene CharacterController lo apaga, mueve y lo prende).
   - Contrato: el componente está en la raíz del jugador, que tiene un collider (CharacterController) para que los triggers del
     mundo lo detecten con `GetComponentInParent<IJugador>()`; se agrega a `Registro<IJugador>` en `OnEnable`; solo hay un
     jugador registrado a la vez.
6. `Core/Jugador/IPuntoAparicion.cs` (la implementa Alan en el mundo): `string Id` (único en la escena, por ejemplo
   "Casa_Cama" o "Clandestino_Puerta"), `Pose Pose`, `ModoJugador Modo` (en qué modo aparece el jugador ahí). Se agrega a
   `Registro<IPuntoAparicion>` en `OnEnable`.
7. Pruebas en `Tests/EditMode/Core/RegistroPruebas.cs` con clases falsas definidas en la misma prueba (por ejemplo una
   `ArenaFalsa : IArenaPelea`), y `Registros.LimpiarTodos()` en `SetUp` y `TearDown`.

## Carpetas permitidas
Assets/_Proyecto/Core/Servicios/, Core/Arena/, Core/Jugador/ y Assets/_Proyecto/Tests/EditMode/Core/
No tocar: el resto de Core/ (los contratos de la fase 1 no cambian), las áreas de Tony y de Alan, escenas, prefabs,
ProjectSettings/, Packages/ ni herramientas/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa: compilar OK, EditMode y PlayMode sin fallas, con las pruebas nuevas contadas
- [ ] RegistroPruebas cubre y pasa: agregar y quitar; null y duplicados ignorados; `Actual` es el último vigente y vuelve al
      anterior al quitarlo; `Agregado` y `Quitado` avisan una sola vez por cambio real; quitar algo que no estaba no avisa;
      dos tipos distintos no se mezclan; `LimpiarTodos` vacía todos los tipos usados y quita los suscriptores
- [ ] Hay una implementación falsa de `IArenaPelea`, `IJugador` e `IPuntoAparicion` en las pruebas (prueba de que los
      contratos se pueden implementar sin referenciar ningún área)
- [ ] ReglasEnsamblados (T-030) sigue pasando: Core no referencia ningún área
- [ ] Cada interfaz y cada miembro tiene comentario `///` en español con quién la implementa, cuándo se registra y qué
      garantiza (lectura del diff)
- [ ] `git diff main --stat` solo toca las carpetas permitidas

## Criterios de sensación (para un humano)
- Alan, o Tony leyendo como Alan: con los comentarios de `IArenaPelea` sabe exactamente qué poner en su jaula y hacia
  dónde debe mirar la pantalla, sin preguntar.

## Pruebas automáticas
- EditMode (Tests/EditMode/Core/): RegistroPruebas, con los casos de arriba.

## Fuera de alcance
- Implementar los contratos: la jaula (T-020, Alan), el flujo (T-022), el jugador a pie (T-F3), los puntos de aparición (T-040)
- Subir al carro o al mecha (T-F3 y A-F2)
- Reloj, lugares y puertas (T-032)
