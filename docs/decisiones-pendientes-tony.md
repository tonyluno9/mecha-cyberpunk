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
