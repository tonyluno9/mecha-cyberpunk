# T-986: las patrullas manejan con la física del carro del jugador

Pedido de Tony (14-nov, Sesión A, punto 3). Carpetas: Assets/_Proyecto/Trafico/ (Persecucion/, Editor/, Prefabs/ de la patrulla), Assets/_Proyecto/Vehiculos/Carro/
(solo lo necesario para que CarroViejo acepte un conductor IA), Assets/_Proyecto/Tests/EditMode|PlayMode/Trafico/ y Vehiculos/, docs/.
No tocar ciudad, techos, casino ni pandillas (la otra sesión). No correr CreadorTodo.

## Antes (diagnóstico de Opus)
`PatrullaMundo` mueve la patrulla con `transform.position`/`rotation` y el Rigidbody cinemático (Preparar pone `isKinematic = true`):
un punto `posicion` avanza por el grafo de calles, `GirarHacia` limita el giro por los metros avanzados, `AplicarCarril` empuja de lado y `SeguirElSuelo` fija la altura.
No hay inercia, ni derrape, ni frenado real; de lado se desliza; contra algo sólido se atraviesa o se queda temblando.

## Qué se pide
1. La patrulla es un `CarroViejo` **no cinemático** con la misma configuración y física que el carro del jugador (suspensión por rayos, motor, agarre, freno de mano).
2. Un conductor IA que solo produce `EntradaCarro` (acelerar, frenar, volante, freno de mano) a partir de: pose y velocidad del carro, un punto objetivo por delante
   en la ruta (persecución pura con distancia de mira que crece con la velocidad) y una velocidad deseada (baja en curvas cerradas, cerca de la meta y si hay obstáculo).
   La lógica pura del conductor va en una clase sin MonoBehaviour con pruebas EditMode.
3. `PatrullaMundo` conserva su cerebro (a dónde ir: grafo, directo al jugador, migas, interceptar, ronda, retirada, carril, obstáculos, bajar policías, sirena),
   pero ya no mueve el transform: le da al conductor el punto y la velocidad.
4. Sin girar sobre su eje, sin teletransportes (solo la colocación inicial al aparecer fuera de la vista), sin movimientos raros. Si se atora: reversa con el volante
   contrario y vuelve a intentar. Derrapa y frena como un carro.
5. Robarla sigue funcionando (al subir el jugador, el conductor IA se suelta y manda el jugador).

## Pruebas y evidencia
- EditMode del conductor (recta, curva, frenar al llegar, reversa al atorarse, volante con signo correcto).
- PlayMode: persecución completa en la escena de pruebas de persecución que ya usan las pruebas de Trafico: el jugador en carro huye por varias calles, sube el calor,
  salen patrullas, lo persiguen y lo alcanzan o lo pierden. Medir cada cuadro de cada patrulla: Rigidbody no cinemático, salto de posición <= v·dt + margen,
  giro (°/s) acotado por la rapidez (detenida no gira), derrape (velocidad lateral > 0 en alguna curva cerrada) y frenado con desaceleración física.
- Capturas de la persecución (varios momentos) en docs/capturas/t986/.
- Las pruebas existentes de patrullas (PatrullaGiroT746, ConduccionPatrullasT666, PatrullaLlegaT708, PatrullaParedT581, PatrullaFisicaT707, PersecucionRealistaT587,
  PoliciaRondaT933, BusquedaT665, PatrullasLejosT664, ArrestoT690, PoliciasT709) se actualizan si medían el movimiento cinemático, sin quitarles lo que comprueban.

## Después (hecho)
- `PatrullaMundo` ya no escribe `transform.position/rotation` (solo la colocación al aparecer, fuera de la vista, y orientarse hacia su ruta). Su cerebro corre en `FixedUpdate`: calcula un punto de mira sobre
  la ruta (por delante del carro real, `DistanciaMira` = mínima + 0.45 s x rapidez, corrido al carril) y una velocidad deseada, y se los pasa a `ConductorCarroIA`.
- `Patrulla.prefab`: Rigidbody no cinemático con interpolación (misma física y config que el carro del jugador). `CarroViejo` acepta `ConducirConIA(Func<EntradaCarro>)` / `SoltarIA()`:
  mismo volante suavizado, no se duerme, el motor suena, `PuedeSubir` solo detenido, y `IntentarSubir` suelta la IA (robo).
- `ConductorCarroIA` (clase pura, Trafico/Persecucion): persecución pura normalizada al ángulo máximo del volante; acelerador/freno por error de velocidad; freno de mano solo en giros muy cerrados a velocidad;
  reversa con volante contrario tras `SegundosParaAtoro` quieta queriendo moverse. Números en `ConfigPersecucion` (sección "Conducción de las patrullas").
- Velocidad deseada: la de `CalculoPersecucion.VelocidadDeseada` (frena con 5 m/s2 para quedar a la distancia de seguimiento), frenado antes de cada curva (90 grados = 10 m/s, vuelta en U = 4 m/s) y de la meta,
  0 con obstáculo en los dos carriles (tras 2.5 s avanza al 15 %), 4 m/s si hay estorbo en su carril mientras cambia de carril.
- Carriles: derecha, izquierda y, si algo los tapa, 2.75 x el ancho a cada lado. Volcada 4 s y fuera de la vista: se quita (Destroy); a la vista se queda.

## Decisiones
- `CarroViejo.AplicarSuspension` ignoraba todo collider con la misma `transform.root`: la patrulla cuelga del barrio y no veía el piso. Ahora solo ignora los suyos (`attachedRigidbody` o hijos).
- Las patrullas no pueden atravesar los cierres del barrio: `BloqueosDeCalles.Marcar` (dos barridos de esfera por tramo, uno por carril) cierra tramos del `GrafoCalles` y los caminos, la salida y la retirada
  (`MasLejano(punto, desde)`) los respetan. En el Barrio.prefab solo queda libre un tramo entre dos cruces, así que las patrullas salen una por una (esperan a que la anterior se aleje): las pruebas de
  cantidad (PersecucionBarrio) esperan unos segundos en vez de 3 cuadros.
- Las pruebas que miden el cerebro de la patrulla esperan tiempo de juego, no cuadros (el cerebro corre en el paso de física; en batchmode 5 cuadros pueden no traer ningún paso).
- T746: "detenida no gira" pasó de 0 grados exactos a menos de 10 grados/s acumulados con la patrulla parada (con física se asienta con micro giros). T707 (EditMode y PlayMode): cuerpo no cinemático.
- Telemetría de la prueba nueva: las muestras con choque (0.4 s tras tocar otro carro, un policía o la ciudad) se cuentan aparte; en ellas el empujón ajeno sí gira o mueve el cuerpo. "Detenida" = menos de 0.5 m/s por 0.4 s seguidos.

## Números medidos (PersecucionFisicaT986Pruebas)
- Cuadrícula abierta (6 patrullas, 2791 muestras libres): cinemáticas 0; salto de posición nunca pasa de v*dt + 0.05 m (peor exceso -0.050 m); giro nunca pasa de rapidez/radio mínimo + 0.35 rad/s
  (peor -0.22 rad/s); giro detenida máx 0.67 grados/s; velocidad lateral máx 0.72 m/s (derrape); frenado máx 16.4 m/s2; rapidez máx 14.2 m/s; volcadas 0; alcanzó al jugador a los 6.1 s del calor 3.
- Barrio real (4 patrullas, 1368 muestras libres): cinemáticas 0; saltos malos 0; giros malos 0; giro detenida máx 1.8 grados/s; frenado máx 17.6 m/s2; alcanzó a los 7.3 s. Capturas en docs/capturas/t986/ (8 PNG).
- El derrape medido es modesto (0.5 a 0.7 m/s): el agarre lateral del carro es alto; mejora con el freno de mano en giros muy cerrados (65 grados y 6 m/s).

## Pruebas
- EditMode: ConductorCarroIAT986Pruebas (14) y PatrullaFisicaT707Pruebas actualizada; todo EditMode 2381/2381.
- PlayMode: PersecucionFisicaT986Pruebas (2), CarroConductorIAT986Pruebas (Vehiculos, 5) y las existentes (PatrullaGiro, ConduccionPatrullas, PatrullaLlega, PatrullaPared, PatrullaFisica, PersecucionRealista,
  PoliciaRonda, Busqueda, PatrullasLejos, PersecucionBarrio, Arresto, Policias, DronesLlegando, Testigos, ExplosionArea, EfectosCombate).
