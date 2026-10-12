# Decisiones pendientes de Tony (se pasan a docs/decisiones.md al aprobar)

## Planeación de T-F2 y A-F1 (2026-10-11, planificador Opus 5.5)

### D-tony-1 Un ensamblado de pruebas por área (T-030)
Propuesta. Se parten los dos ensamblados de pruebas en `Tests/<Modo>/<Área>/MechaCyberpunk.Tests.<Modo>.<Área>.asmdef`, uno por
área y otro para Core. Los de Tony referencian todas las áreas de Tony y los de Alan todas las de Alan, porque las pruebas
jugables cruzan áreas del mismo dueño (por ejemplo, Combate usa Mecha y UI). Así nadie edita un asmdef de pruebas compartido y
una prueba rota de uno no tumba las del otro. `herramientas/verificar.sh` arma la lista sola con las carpetas que tienen .cs, y
la prueba ReglasEnsamblados falla si un área referencia a un área de la otra persona o si aparece un asmdef sin dueño.

### D-tony-2 Registro<T> para encontrar contratos (T-031)
Propuesta. Las implementaciones de contratos de Core (arena, jugador, puntos de aparición, reloj, lugares, puertas, tiendas,
cartera, calor, objetivos) se agregan a `Registro<T>` en `OnEnable` y se quitan en `OnDisable`; quien las usa las busca en
`Start` o se suscribe a `Agregado`. Se limpia al entrar a Play. Evita referencias entre áreas y búsquedas con `Find`.

### D-tony-3 Pantalla de la arena (T-031, T-022)
Propuesta. `IArenaPelea.PantallaArena` es un Transform en el centro de la cara visible; su +Z apunta de la jaula hacia la
pantalla, así un Canvas World Space alineado con él se lee desde la jaula. `TamanoPantalla` da el ancho y alto en metros.
El flujo pone su Canvas 5 cm hacia la jaula. Si hay varias arenas, el flujo usa la más cercana.

### D-tony-4 IdLugar como enum en Core (T-032)
Propuesta. `IdLugar { Ninguno, Calle, Casa, Taller, Bar, Clandestino, Casino, MercadoNegro, Clinica }`: los seis lugares reales de
ciudad.md más el taller y la clínica. Es fijo por diseño y lo usan los dos; agregar un lugar es una tarea de Core.

### D-tony-5 Horarios: datos de Alan, regla en Core (T-032, T-042)
Propuesta. `HorarioLugar` (abre, cierra, siempre abierto, y la regla que cruza la medianoche) vive en Core porque lo leen los
dos. Las horas de amanecer y anochecer y los horarios de cada lugar son datos de Alan (Mundo/Datos/), porque el ciclo de día y
noche y los locales son del mundo (D-026).

### D-tony-6 Core solo lleva contratos (T-033, T-034)
Propuesta. Core lleva interfaces, eventos, enums, structs y las clases de datos que leen los dos. Los modelos (`ModeloCartera`,
`TiendaBasica`, `ModeloCalor`, `ModeloObjetivos`) viven en Economia/ y Progresion/, porque quien posee el dato lo implementa
(D-027). En sus pruebas, Alan usa dobles propios de las interfaces. `ModeloVida` sigue en Core (D-006).

### D-tony-7 Dinero en enteros y deuda permitida (T-033)
Propuesta. El dinero es `int`. `IntentarPagar` solo cobra si alcanza (compras); `CargarDeuda` puede dejar el dinero en negativo
(reparaciones, doctor), porque economia-misiones.md dice que quedar en negativo es intencional. Precios de T-033: de prueba.

### D-tony-8 Calor de 0 a 100 en 3 niveles, en Progresion/ (T-034)
Propuesta. El calor va de 0 a 100 con niveles 1, 2 y 3 desde 25, 50 y 75, empieza a bajar 10 s después del último aumento a
5 puntos/s (DatosCalor_Base). Los drones y patrullas de Alan leen el nivel; Alan puede enfriar o borrar desde el mundo.

### D-tony-9 El rival reusa Combate (T-021)
Propuesta. `MechaCyberpunk.IA` referencia `MechaCyberpunk.Combate` (mismo dueño). El rival usa `VidaMecha`, `PeleadorMecha` y
`CicloGolpe`, así el jugador y el rival tienen las mismas reglas de vida, bloqueo y golpe. `PeleadorMecha` gana el evento
`PeleaPreparada` para que la presentación del rival se levante en la revancha.

### D-tony-10 Cómo encuentra todo FlujoPelea.prefab (T-022)
Propuesta. El director busca la arena en `Registro<IArenaPelea>`, al jugador como el `IPeleador` del lado Jugador e instancia
el rival desde su prefab, con campos opcionales para fijarlos a mano. Arranca solo al dar Play (`empezarAlArrancar`). En
Pruebas_Tony.unity arranca con P, porque las pruebas de T-012 y T-013 cargan esa escena y esperan al jugador libre.

### D-tony-11 Orden de T-F2
Propuesta. T-030, T-031, T-032, T-021, T-022, T-033 y T-034. Cambia el orden de docs/estado.md (que ponía T-033 y T-034 antes
de la pelea) porque Alan no necesita cartera ni calor hasta A-F5 y el hito H1 sí necesita el rival y el flujo.

### D-tony-12 Arena de prueba de Tony (T-022)
Propuesta. Tony prueba el flujo con `Pelea/Pruebas/ArenaDePrueba.prefab`, propia, que implementa `IArenaPelea`. No usa la jaula
de Alan, y sirve de ejemplo del contrato.

### D-tony-13 El rival busca solo a su objetivo (T-021)
Propuesta. Si no tiene objetivo asignado, el rival busca en `Start` el `IPeleador` del lado contrario. Así Pelea/ no necesita
referenciar IA/ para presentarle el jugador.

## Planeación de T-F3 (2026-10-12, planificador Opus 5.5)

### D-tony-14 Contrato para subir y bajar: IMontable (T-050, T-058)
Propuesta. Carro (Alan, A-F2) y mecha (Tony, T-F4) implementan `Core/Jugador/IMontable` y se registran en `Registro<IMontable>`.
`IJugador` y `ModoJugador` no cambian de forma: quien implementa `IJugador` (el jugador a pie de Tony) decide subir y bajar con la
tecla E (que el montable no lee), cambia `Modo` y `Camara` y avisa `ModoCambio`. Mientras va dentro, el jugador apaga su cámara,
su `AudioListener` y su `CharacterController` y sigue al `Asiento` del montable en `LateUpdate` (sin cambiar de padre); el
montable prende su cámara al subir y da una pose libre en el piso al bajar (`IntentarBajar(out Pose)`). Consecuencia para el
mundo: en carro los triggers no ven al jugador; para zonas usa `Registro<IJugador>.Actual.Raiz.position` o los colliders del
montable. Bajar a la fuerza (carro destruido, mecha en KO) queda para una tarea de Core de T-F4 si hace falta.

### D-tony-15 Daño a pie sin IPeleador y daño de disparo (T-050, T-052, T-053)
Propuesta. `TipoDanio` gana `Disparo` (pistola a pie). La persona a pie y los muñecos de prueba implementan `IVida` e
`IRecibeDanio` pero NO `IPeleador`, porque `DirectorPelea` y `RivalBasico` buscan "el IPeleador de lado Jugador" entre todos los
MonoBehaviour y la confundirían con el mecha. Lo que no es peleador publica `EventosPelea.ImpactoRecibido` con `objetivo` null;
los suscriptores actuales ya comparan con el suyo y toleran null.

### D-tony-16 UbicacionObjetivo en Core (T-050, T-055, T-057)
Propuesta. `Core/Objetivos/UbicacionObjetivo.Resolver(objetivo, lugares, out posicion)` es la regla única para saber dónde está
el objetivo (su posición, o la `PosicionEntrada` de su lugar). La usan el servicio de objetivos y el HUD de Tony, y Alan si
dibuja marcadores en el mundo; así nadie la reescribe.

### D-tony-17 Referencias entre áreas de Tony en T-F3
Propuesta. `MechaCyberpunk.Personaje` referencia `MechaCyberpunk.Combate` para reutilizar `CicloGolpe` y `BarridoGolpe` en los
golpes a pie. `MechaCyberpunk.UI` referencia `Personaje` (vida, armas, interacción) y `Progresion` (seguimiento del objetivo)
para el HUD a pie. Personaje y Progresion no referencian UI, así no hay ciclos. Ninguna referencia cruza a un área de Alan.

### D-tony-18 Controles a pie (T-051)
Propuesta. `Personaje/Entrada/ControlesAPie.inputactions`, propio y separado de ControlesMecha: WASD mover, ratón mirar, Shift
correr, Espacio saltar, E interactuar (puertas, carro, más adelante el mecha), clic izquierdo atacar, 1 puños, 2 pistola,
R recargar y la misma tecla que el mecha para liberar el cursor. Caminar 4.5 m/s, correr 7.5 m/s, salto de 1.1 m y ojos a
1.65 m; velocidad y salto son `DatosPersonaje` (los cambiará el implante de piernas), lo demás `ConfigControlesAPie`.

### D-tony-19 Pistola (T-053)
Propuesta. Tiro instantáneo al centro de la vista, sin apuntar con mira: 25 de daño, 60 m de alcance, 0.25 s entre tiros,
cargador de 8 y recarga de 1.4 s (`DatosPistola`). En T-F3 la reserva de balas es infinita; comprar balas o pistolas es de T-F7.

### D-tony-20 HUD a pie por modo y canción de espera (T-056, T-057)
Propuesta. El HUD a pie es un Canvas Screen Space Overlay (flotante, como pide estetica-hud.md). A pie muestra vida, mira, arma,
objetivo, canción y avisos de interacción; en carro solo objetivo y canción (hay que poder seguir el objetivo manejando); en
mecha nada (D-011). Hasta que exista la radio (T-F7) la canción muestra "RADIO · sin señal" y la radio la cambiará con
`MostrarCancion`.

### D-tony-21 Servicio de objetivos y guía de vuelta (T-055, T-057)
Propuesta. `ServicioObjetivos` (Progresion/) es un MonoBehaviour que implementa `IObjetivos` envolviendo `ModeloObjetivos` y se
registra en `Registro<IObjetivos>`. Sigue al jugador: distancia, mejor distancia, "se aleja" (25 m más que la mejor, durante
1.5 s, y se apaga a 10 m) y llegada (4 m o entrar al lugar del objetivo, por `EventosLugares`). La guía de vuelta del prólogo es
la flecha del HUD más el aviso "REGRESA"; la flecha apunta en línea recta, así que el director del prólogo (T-F6) pone objetivos
intermedios en las esquinas. El HUD usa el seguimiento solo si el `IObjetivos` registrado es un `ServicioObjetivos`.

### D-tony-22 El jugador interactúa por registro, no por collider (T-054, T-058)
Propuesta. Para abrir una puerta o subir a un montable, el jugador busca en `Registro<IPuerta>` (por `Posicion`, solo las
cerradas) y `Registro<IMontable>` (por `PuntoSubida`, solo los que `PuedeSubir`) el más cercano a 2.5 m o menos y a 60° o menos
de hacia dónde mira. No asume nada de la jerarquía ni de los colliders de los objetos de Alan, y no hay que apuntar con precisión.

### D-tony-23 Escena de pruebas a pie y un solo prefab del jugador (T-051, T-059)
Propuesta. Tony prueba a pie en `Escenas/Tony/Pruebas_APie.unity`, generada solo por `Personaje/Editor/` (cada tarea agrega su
parte al creador y la regenera; nunca se mezcla a mano). Los dobles de lo de Alan (`LugarDePrueba`, `PuertaDePrueba`,
`ZonaLugarDePrueba`, `CarroDePrueba`, `PuntoAparicionDePrueba`) viven en `Personaje/Pruebas/` y sirven de ejemplo del contrato.
`JugadorAPie.prefab` lleva anidados `HudAPie.prefab` y `ServiciosProgresion.prefab`, así Alan coloca un solo prefab y hay un
solo HUD y un solo `IObjetivos` por jugador. `AparicionJugador` lo coloca en el `IPuntoAparicion` cuyo `Id` se le indique.
Pruebas_Tony.unity no se toca, porque las pruebas de T-012, T-013 y T-022 la cargan.

### D-tony-24 KO a pie mínimo (T-052)
Propuesta. En T-F3, al llegar a 0 de vida a pie el jugador pierde el control y la vista cae al piso; `Restaurar()` lo levanta
(en pruebas con la tecla K). Despertar en la clínica y que el mecha vuelva dañado al taller (personaje-progresion.md) es de T-F5/T-F6.

### D-tony-25 Lo que espera a Alan en T-F3 (T-060, T-061)
Propuesta. Todo T-F3 se hace con dobles propios. Solo dos tareas de integración esperan a Alan y quedan en bloqueadas: T-060
(el jugador en Pruebas_Alan.unity: barrio, reloj, puertas y bar; espera A-F1) y T-061 (subir al carro de Alan; espera A-F2).
Sus pruebas cargan la escena de Alan e instancian el prefab en tiempo de juego, sin guardar nada en sus escenas. Pedido a Alan
(cuando T-059 esté integrada): colocar `Personaje/Prefabs/JugadorAPie.prefab` en Ciudad.unity y Pruebas_Alan.unity con
`idAparicion` "Casa_Puerta" y apagar ahí su cámara libre, para que haya un solo `IJugador` y Tony pueda hacer la prueba humana
de H2 en Ciudad.unity.

### D-tony-26 Orden de T-F3
Propuesta. T-050 (Core, se integra a main en cuanto se verifica, porque Alan necesita `IMontable` para A-F2), T-051, T-055,
T-054, T-052, T-053, T-056, T-057, T-058, T-059; luego T-060 y T-061 cuando Alan entregue. El mecha como montable (llamarlo,
subir y el encendido) es de T-F4: `MechaJugador` implementará `IMontable` con modo Mecha y por ahora no es `IJugador`.
