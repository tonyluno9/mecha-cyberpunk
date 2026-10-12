# T-054 Entrar y salir de lugares: puertas del lado del jugador
Dueño: Tony | Rama: tony/T-054-puertas-lado-jugador | Modelo: Sonnet 5.5, medium
Depende de: T-051 (jugador a pie y escena). Usa `IPuerta`, `ILugar`, `HorarioLugar` y `EventosLugares`, ya en main.

## Objetivo
El jugador a pie abre puertas con E y entra y sale de los lugares, usando solo los contratos de Core/Mundo/. Se prueba con
dobles propios de Tony (una puerta, un lugar y una zona de interior) que se portan como los de Alan en T-042 y T-043, así
esta tarea no espera a A-F1 y, cuando llegue lo de Alan, funciona igual.

## Contexto
- Diseño: docs/diseno/ciudad.md (entras y sales en tiempo real; un lugar cerrado tiene la puerta cerrada y un letrero con el
  horario), docs/diseno/demo.md (casino y mercado negro cerrados en la demo).
- Contratos (leer los `///`): Core/Mundo/IPuerta.cs (orden de decisión de `IntentarAbrir`), ILugar.cs (`EstaAbierto`,
  `JugadorDentro`, `PosicionEntrada`), EventosLugares.cs (`PublicarEntro`, `PublicarSalio`), HorarioLugar.cs, ResultadoPuerta.cs.
- Cómo lo hará Alan (solo leer, para que los dobles se parezcan): docs/tareas/pendientes/T-042 y T-043 y
  docs/decisiones-pendientes-alan.md (D-alan-5, D-alan-6, D-alan-8). Las zonas de Alan detectan al jugador con
  `GetComponentInParent<IJugador>()` y avisan una sola vez por entrada aunque el jugador tenga varios colliders.
- Propuesta D-tony-22 (interacción por registro, no por collider) en docs/decisiones-pendientes-tony.md: el jugador no asume
  nada de la jerarquía de las puertas de Alan; busca en `Registro<IPuerta>` por `Posicion`.

## Qué hacer
1. `Personaje/Datos/ConfigInteraccionAPie.cs`: alcance (2.5 m, medido en el plano horizontal desde los pies) y ángulo máximo
   respecto a hacia dónde mira la cámara (60°). Asset `ConfigInteraccionAPie_Base.asset`.
2. Lógica pura `Personaje/Interaccion/SelectorInteraccion.cs`: con la posición y la dirección de la vista del jugador y una lista
   de puntos candidatos, devuelve el índice del mejor (el más cercano dentro del alcance y del ángulo; a igual distancia, el más
   centrado) o -1. Pensada para que T-058 agregue los puntos de subida de los carros sin cambiarla.
3. `Personaje/Interaccion/InteraccionAPie.cs` en el jugador: cada frame elige entre las puertas de `Registro<IPuerta>` que no
   están abiertas; con Interactuar (E) llama `IntentarAbrir()` de la elegida. Expone la puerta candidata (o null), el lugar al
   que da, y un evento `PuertaIntentada(IPuerta, ResultadoPuerta)`. Escribe "[Personaje] Puerta <lugar>: <resultado>" en la
   consola solo al intentar. No funciona con el jugador en KO.
4. Dobles en `Personaje/Pruebas/` (solo para escenas y pruebas de Tony):
   - `LugarDePrueba : MonoBehaviour, ILugar`: `IdLugar`, nombre, horario (abre, cierra), `cerradoEnDemo`, y un "abierto por
     horario" que se cambia a mano con `PonerAbiertoPorHorario(bool)` (no necesita reloj). `EstaAbierto` = no cerrado en demo y
     abierto por horario; avisa `EstadoCambio` y `EventosLugares.PublicarEstadoLugar` solo en cambios reales. `PosicionEntrada`
     es su hijo `Entrada`. Se registra en `Registro<ILugar>`.
   - `PuertaDePrueba : MonoBehaviour, IPuerta`: sigue al pie de la letra el orden de decisión de `IPuerta.IntentarAbrir` con su
     `LugarDePrueba`; al abrir, la hoja (cubo con collider) se desliza a un lado en 0.3 s y deja pasar; `Cerrar()` la regresa.
     Se registra en `Registro<IPuerta>`.
   - `ZonaLugarDePrueba`: trigger que cubre el interior; con `GetComponentInParent<IJugador>()` pone `JugadorDentro` del lugar y
     publica `EventosLugares.PublicarEntro` / `PublicarSalio` una sola vez por entrada y salida, aunque entren varios colliders.
   - Prefab `Personaje/Pruebas/LocalDePrueba.prefab`: un cuarto greybox de 8 × 6 × 3.5 m con un hueco de puerta de 1.2 × 2.4 m,
     la puerta, la zona, el lugar y su `Entrada` a 1.5 m frente a la puerta del lado de la calle.
5. Creador: en Pruebas_APie, tres locales: Bar (abierto), Clandestino (cerrado por horario, 20:00 a 06:00) y Casino (cerrado en
   la demo). Regenera la escena.

## Carpetas permitidas
Assets/_Proyecto/Personaje/, Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity, Assets/_Proyecto/Tests/EditMode/Personaje/ y
Assets/_Proyecto/Tests/PlayMode/Personaje/
No tocar: Core/, Mundo/ y todo lo de Alan (solo leer sus tareas), Pruebas_Tony.unity.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode SelectorInteraccion: elige el más cercano dentro del alcance y del ángulo; descarta los de atrás (más de 60°) y los
      de más de 2.5 m; con lista vacía o null devuelve -1; entre dos a la misma distancia elige el más centrado
- [ ] EditMode o PlayMode PuertaDePrueba: los cinco casos de `IntentarAbrir` (ya abierta, jugador dentro con el lugar cerrado,
      cerrado en demo, fuera de horario, abierta) dan el resultado del contrato; `Cambio` avisa solo al abrir y al cerrar de verdad
- [ ] PlayMode con entrada simulada: el jugador a 1.5 m de la puerta del Bar, mirándola: E da `Abierta`; con W 3 s cruza el
      umbral; llega `EventosLugares.JugadorEntro(Bar)` una vez y `JugadorDentro` del Bar es true; da media vuelta con el ratón,
      sale con W y llega `JugadorSalio(Bar)` una vez
- [ ] PlayMode con entrada simulada: frente al Clandestino, E da `CerradaPorHorario` y con W 3 s el jugador no pasa la puerta;
      frente al Casino, E da `Bloqueada`
- [ ] PlayMode con entrada simulada: dentro del Clandestino con la puerta cerrada y el lugar cerrado por horario, E da `Abierta`
      y el jugador sale (regla de D-alan-6)
- [ ] PlayMode con entrada simulada: de espaldas a la puerta a 1.5 m, o de frente a 4 m, E no llama a `IntentarAbrir`
      (contado con un contador en el doble)
- [ ] Captura del jugador frente al Bar con la puerta abierta: docs/capturas/T-054-1.png
- [ ] Consola sin errores entrando y saliendo de los tres locales en Pruebas_APie

## Criterios de sensación (para un humano)
- Abrir con E se siente inmediato y no hay que apuntar con precisión: basta con estar frente a la puerta.
- Entrar y salir del local se siente continuo, sin tirones.

## Pruebas automáticas
- EditMode (Tests/EditMode/Personaje/): SelectorInteraccionPruebas, PuertaDePruebaPruebas (si la regla se puede probar sin escena).
- PlayMode (Tests/PlayMode/Personaje/): PuertasAPiePruebas con InputTestFixture y los locales instanciados en la prueba.

## Fuera de alcance
- Texto en pantalla de "E · Abrir" y del horario (T-057)
- Puertas, letreros, reloj e interiores reales (Alan, T-041 a T-043); probarlo en el barrio de Alan (T-060, espera A-F1)
- Subir al carro (T-058)
