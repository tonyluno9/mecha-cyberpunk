# Decisiones

Cada decisión lleva ID, fecha y sesión. Las marcadas "Propuesta" las puso Claude y Tony puede cambiarlas;
si cambian, se anota una decisión nueva que reemplaza a la anterior.

## Sesión T-000b (2026-10-11, Opus 5.5): preparación del repo y fase 1

### D-001 Ramas y pull requests
main solo recibe pull requests. Cada tarea tiene su rama `<persona>/T-xxx-nombre-corto`, con `tony` o `compa`.
Antes del pull request se pide la revisión del subagente `revisor`.

### D-002 Dueños de carpetas
Tony es dueño de Mecha/, Combate/ y UI/. El compañero es dueño de Arena/, IA/ y el flujo de pelea, que vive en Arena/Flujo/.
Core/, ProjectSettings/ y Packages/ no tienen dueño: solo se cambian en una tarea de Core, revisada por la otra persona.

### D-003 Escenas
Las escenas viven en Assets/_Proyecto/Escenas/. Tony prueba en Pruebas_Tony.unity. El compañero es dueño de
Clandestino.unity y prueba en Pruebas_Compa.unity. Cada escena la crea la primera tarea de su dueño que la usa.
La SampleScene y la carpeta TutorialInfo de la plantilla de Unity se quedan como están y no se usan.

### D-004 Ensamblados
Un asmdef por área (`MechaCyberpunk.<Área>`). Cada área referencia solo a Core y a paquetes de Unity,
y a otra área del mismo dueño solo si su tarea lo pide. Las áreas de dueños distintos se hablan por los contratos de Core/.
Tests/EditMode y Tests/PlayMode tienen un asmdef cada uno que referencia a todas las áreas.

### D-005 Datos de peleadores en Core, assets en la carpeta del dueño
Las clases `DatosMecha` y `DatosRival` (ScriptableObject) viven en Core/ porque el jugador y el rival usan los mismos
números. Los assets (`DatosMecha_Jugador.asset`, `DatosRival_Prueba.asset`) viven en `<Área>/Datos/` de su dueño,
así balancear no toca Core/. Los valores de sensación o presentación de un área van en un `Config*` propio del área.

### D-006 Vida compartida
`ModeloVida`, una clase C# pura en Core/, lleva la vida del jugador y del rival con las mismas reglas:
no baja de 0, avisa el KO una sola vez y se restaura completa.

### D-007 Eventos de pelea
`EventosPelea` es una clase estática en Core/. El flujo publica el conteo, el inicio y el fin de la pelea.
Quien recibe daño publica `ImpactoRecibido` después de aplicarlo. Los suscriptores se limpian al entrar a Play.

### D-008 Entrada
Solo Input System. Tony es dueño de Mecha/Entrada/ControlesMecha.inputactions, con mover, mirar, golpear y bloquear.
El flujo de pelea usa su propia `InputAction` para la revancha. El InputSystem_Actions.inputactions de la plantilla no se usa.

### D-009 Sin capas ni tags personalizados en la fase 1
Viven en ProjectSettings/, un archivo compartido. Para no golpearse a sí mismo, cada golpe ignora su propio `transform.root`.

### D-010 Texto con uGUI Text
Los textos del mundo usan uGUI `Text` en un Canvas World Space. TextMeshPro pide importar recursos a una carpeta compartida,
así que espera a una tarea de Core.

### D-011 Pantallas diegéticas
Basado en estetica-hud.md ("en el mecha todo es diegético"). La vida del jugador se ve en una pantalla de la cabina (UI/).
El conteo y el resultado se ven en una pantalla grande de la arena (Arena/Flujo/). Nada flota sobre la vista.
La vida del rival no se muestra en la fase 1, porque personaje-progresion.md la liga al implante de ojos;
se comprueba en el inspector y con el parpadeo del rival al recibir un golpe.

### D-012 Golpe y bloqueo
El golpe tiene tres momentos: preparación, impacto y recarga. En el rival, la preparación es el aviso visible.
La guardia tarda `tiempoSubirGuardia` en quedar arriba. Si está arriba al momento del impacto, el bloqueo es normal y quita
`reduccionBloqueo` del daño. Si quedó arriba dentro de los últimos `ventanaBloqueoPerfecto` segundos antes del impacto,
el bloqueo es perfecto y no entra daño. Esta es la ventana de tiempo para bloquear. El contraataque queda para otra fase.

### D-013 La revancha restaura la vida
En la fase 1, la revancha empieza con la vida completa. El daño persistente y las reparaciones son el paso 5
del orden de construcción y se harán después.

### D-014 Escala
1 unidad = 1 metro. El mecha greybox mide unos 4 m y la cámara de la cabina va a unos 3.5 m del piso.
La jaula mide unos 24 × 24 m. Los peleadores empiezan a unos 12 m uno del otro.

### D-015 Movimiento sin NavMesh
Los mechas usan `CharacterController`. El rival camina directo hacia su objetivo, porque la jaula es abierta.

### D-016 Fin de pelea
Gana quien deja al otro en KO primero. Si los dos caen en el mismo frame, cuenta el primer aviso de KO que llegue al flujo.
No hay límite de tiempo ni rounds en la fase 1.

### D-017 IDs de tareas
T-00x son tareas de Core y preparación. En la fase 1, T-01x son de Tony y T-02x del compañero.

### D-018 Herramientas de prueba
Los muñecos y atacantes de prueba viven en `<Área>/Pruebas/` de su dueño y solo se ponen en escenas de pruebas.

### D-019 Confirmación de D-011 a D-016
Tony confirmó D-011, D-012, D-013, D-014, D-015 y D-016 el 2026-10-11. Ya no son propuestas.

## Modo automático (2026-10-11, preparado por Claude en chat con Tony)

### D-020 Integraciones de preparación
T-001 (contratos de Core) y T-002 (verificación automática) las integra a main el orquestador de Tony en cuanto
estén verificadas, sin esperar la revisión nocturna, porque las dos personas dependen de ellas.

### D-021 El compañero es Alan
El compañero se llama Alan. Prefijo de ramas `alan/`. Su escena de pruebas es Pruebas_Alan.unity.
Donde los documentos viejos digan "compañero", "compa" o "Pruebas_Compa", se refiere a Alan.

### D-022 Modo automático y revisión nocturna
Cada persona arranca a Claude con el `/goal` de docs/ARRANQUE.md y Claude hace todas sus tareas: Opus 5.5 orquesta,
Sonnet 5.5 programa y verifica, Opus 5.5 revisa. El trabajo se integra en `<persona>/fase-N`. main solo cambia
cuando la persona lo aprueba en la revisión nocturna (excepto D-020). Mientras trabaja en automático, cada persona
escribe solo en docs/estado-<persona>.md, docs/decisiones-pendientes-<persona>.md y docs/revision-<persona>.md.

### D-023 Verificación sin humanos
Un criterio de aceptación se da por cumplido solo con evidencia: prueba automática, valor medido en Unity en vivo o
captura revisada. Las tareas jugables llevan pruebas de PlayMode con entrada simulada. Solo los criterios de
sensación o gusto visual quedan para la revisión nocturna.

### D-024 Turnos de día y de noche
De día trabaja solo Sonnet 5.5: el orquestador, el implementador y el verificador. Hace y verifica las tareas y las
integra en `<persona>/fase-N` marcadas "hecha (sin revisar)". De noche, cuando la persona lo pide, el orquestador corre
en Opus 5.5 y el revisor (Opus 5.5, high) revisa cada tarea del día y el conjunto, y se corrige lo importante.
main solo cambia cuando la persona aprueba después de probar. Reemplaza el paso del revisor por tarea de D-022.

## Aprobación de la fase 1 y nueva división (2026-10-11, sesión de aprobación de Tony)

### D-025 Fase 1 de Tony aprobada e integrada
Tony probó tony/fase-1 y aprobó T-001, T-002, T-010, T-011, T-012 y T-013. Se integró a main y se verificó con
herramientas/verificar.sh todo (compilar, EditMode 51/51, PlayMode 64/64). Tony aceptó el cambio de .gitattributes de T-002
(`*.sh eol=lf`) y los cambios de T-013 fuera de carpeta (una línea en CabinaPruebas.cs y las referencias a Combate y UI
del asmdef de editor de Mecha). Tony autorizó el merge a main en esa sesión. Valores de sensación (sacudida, pantalla de
vida, brazos) quedan como primera versión y se ajustan en fases posteriores.

### D-026 Nueva división del trabajo: Tony = sistemas, Alan = mundo
Reemplaza D-002 y la parte de dueños de D-003 y D-004. Para que los dos Claude nunca choquen:
- **Tony: el jugador y todo lo que sea sistema y datos.** Mecha, combate, rivales e IA de pelea, flujo de pelea,
  personaje a pie y su combate básico, creación de personaje, implantes, economía (dinero, premios, reparaciones, renta,
  apuestas), ligas y rivales, misiones y reputación como lógica y datos, guardado, HUD a pie y de cabina y radio.
- **Alan: el mundo.** Mapa de la ciudad (un barrio de unas 6 manzanas con calles y vueltas, sin pantallas de carga),
  arena y edificio del clandestino, casa con taller, bar, casino (la idea del Casino FOX: tragamonedas, fichas y cajera,
  con estética cyberpunk), mercado negro con clínica, carros y manejo, ciclo de día y noche con horarios de locales,
  tráfico y peatones de fachada, persecuciones de drones y patrullas, ambiente (lluvia, neón, audio de ciudad) y,
  después, el arte con assets de Tripo.
- T-020 (arena) sigue siendo de Alan. T-021 (rival) y T-022 (flujo de pelea) pasan a Tony.
- Cada carpeta y cada escena tiene un solo dueño (tabla en CLAUDE.md). Alan no edita el prefab del jugador: solo lo coloca.
  Tony no edita escenas del mundo: sus sistemas se enchufan por contratos de Core/.

### D-027 Contratos entre Tony y Alan en Core/
Lo que los dos se tienen que decir va como interfaz, evento o dato en Core/, escrito antes por una tarea de Core
(asignada a Tony, T-030 a T-034) para que Alan espere lo menos posible. Cubren: arena y puntos de inicio de pelea,
aparición y modo del jugador (a pie, en carro, en mecha), reloj del mundo y horarios, lugares y puertas, cartera y
ofertas de tiendas, calor y objetivos. Regla: quien posee el dato lo implementa; el otro lo consume por la interfaz.
Cambiar un contrato ya mergeado es una tarea de Core nueva, avisada en docs/revision-<persona>.md de las dos personas.

### D-028 Carpetas y ensamblados nuevos
Cada carpeta lleva su asmdef `MechaCyberpunk.<Área>` (D-004 sigue vigente: las áreas de dueños distintos no se referencian).
- Tony: Mecha/, Combate/, UI/, IA/ (rivales), Pelea/ (flujo de pelea, antes Arena/Flujo/), Personaje/ (a pie, creación),
  Economia/ (dinero, precios, renta, apuestas), Progresion/ (ligas, implantes, misiones, reputación, historia, guardado)
  y Radio/.
- Alan: Arena/ (jaula, luces), Mundo/ (ciudad, locales, interiores, día y noche, ambiente), Vehiculos/ (carros),
  Casino/ (tragamonedas, fichas, cajera), Trafico/ (tráfico, peatones, drones y patrullas) y Assets/Arte/ (lo dice la tarea).
La carpeta Arena/Flujo/ de D-002 ya no existe. T-030 crea las carpetas, asmdefs y los asmdefs de pruebas nuevos.

### D-029 Escenas con un solo dueño
Pruebas_Tony.unity y Escenas/Tony/: Tony. Clandestino.unity, Pruebas_Alan.unity, Ciudad.unity y Flashback.unity: Alan.
Tony crea las escenas de pantallas sin mundo (creación de personaje) bajo Escenas/Tony/. El prefab MechaJugador es de
Tony; Alan lo coloca en sus escenas sin editarlo. Tony entrega en un prefab propio lo que necesite estar dentro de una
escena de Alan (por ejemplo `FlujoPelea.prefab`) y Alan solo lo coloca. Si Alan necesita un cambio en un prefab de Tony,
lo pide en docs/revision-alan.md y se vuelve tarea de Tony.

### D-030 IDs de tareas
Reemplaza D-017. El ID ya no dice el dueño: el dueño está en el archivo de la tarea. T-00x y T-03x son de Core y preparación
(las hace Tony), T-01x y T-02x siguen con sus dueños actuales (T-021 y T-022 ahora de Tony), T-04x en adelante se
reparten por fase según docs/hoja-de-ruta.md. Las ramas llevan el prefijo del dueño: tony/ o alan/.

### D-031 Orden de construcción por demo
La hoja de ruta (docs/hoja-de-ruta.md) ordena las fases para que la demo (docs/diseno/demo.md) esté jugable lo antes
posible. Casino, mercado negro, clínica, implantes, ligas completas, misiones, radio, persecuciones y el arte final van
después de la demo. Las dos personas avanzan en paralelo y se encuentran en hitos de integración (H1 a H6).

### D-032 Cambios de dueño en Core/ y en archivos compartidos
Core/, ProjectSettings/, Packages/ y herramientas/ siguen sin dueño (regla 3 de CLAUDE.md), pero las tareas de Core las
hace el Claude de Tony y las revisa el revisor con la mirada de Alan (¿le alcanza para su parte?). Alan pide cambios
de Core escribiendo en docs/revision-alan.md qué le falta, y el orquestador de Tony lo vuelve tarea de Core.

## Decisiones de Tony, T-F2 a T-F4 (aprobadas 2026-10-15; antes D-tony-1 a D-tony-39)

## Planeación de T-F2 y A-F1 (2026-10-11, planificador Opus 5.5)

### D-033 Un ensamblado de pruebas por área (T-030) (antes D-tony-1)
Se parten los dos ensamblados de pruebas en `Tests/<Modo>/<Área>/MechaCyberpunk.Tests.<Modo>.<Área>.asmdef`, uno por
área y otro para Core. Los de Tony referencian todas las áreas de Tony y los de Alan todas las de Alan, porque las pruebas
jugables cruzan áreas del mismo dueño (por ejemplo, Combate usa Mecha y UI). Así nadie edita un asmdef de pruebas compartido y
una prueba rota de uno no tumba las del otro. `herramientas/verificar.sh` arma la lista sola con las carpetas que tienen .cs, y
la prueba ReglasEnsamblados falla si un área referencia a un área de la otra persona o si aparece un asmdef sin dueño.

### D-034 Registro<T> para encontrar contratos (T-031) (antes D-tony-2)
Las implementaciones de contratos de Core (arena, jugador, puntos de aparición, reloj, lugares, puertas, tiendas,
cartera, calor, objetivos) se agregan a `Registro<T>` en `OnEnable` y se quitan en `OnDisable`; quien las usa las busca en
`Start` o se suscribe a `Agregado`. Se limpia al entrar a Play. Evita referencias entre áreas y búsquedas con `Find`.

### D-035 Pantalla de la arena (T-031, T-022) (antes D-tony-3)
`IArenaPelea.PantallaArena` es un Transform en el centro de la cara visible; su +Z apunta de la jaula hacia la
pantalla, así un Canvas World Space alineado con él se lee desde la jaula. `TamanoPantalla` da el ancho y alto en metros.
El flujo pone su Canvas 5 cm hacia la jaula. Si hay varias arenas, el flujo usa la más cercana.

### D-036 IdLugar como enum en Core (T-032) (antes D-tony-4)
`IdLugar { Ninguno, Calle, Casa, Taller, Bar, Clandestino, Casino, MercadoNegro, Clinica }`: los seis lugares reales de
ciudad.md más el taller y la clínica. Es fijo por diseño y lo usan los dos; agregar un lugar es una tarea de Core.

### D-037 Horarios: datos de Alan, regla en Core (T-032, T-042) (antes D-tony-5)
`HorarioLugar` (abre, cierra, siempre abierto, y la regla que cruza la medianoche) vive en Core porque lo leen los
dos. Las horas de amanecer y anochecer y los horarios de cada lugar son datos de Alan (Mundo/Datos/), porque el ciclo de día y
noche y los locales son del mundo (D-026).

### D-038 Core solo lleva contratos (T-033, T-034) (antes D-tony-6)
Core lleva interfaces, eventos, enums, structs y las clases de datos que leen los dos. Los modelos (`ModeloCartera`,
`TiendaBasica`, `ModeloCalor`, `ModeloObjetivos`) viven en Economia/ y Progresion/, porque quien posee el dato lo implementa
(D-027). En sus pruebas, Alan usa dobles propios de las interfaces. `ModeloVida` sigue en Core (D-006).

### D-039 Dinero en enteros y deuda permitida (T-033) (antes D-tony-7)
El dinero es `int`. `IntentarPagar` solo cobra si alcanza (compras); `CargarDeuda` puede dejar el dinero en negativo
(reparaciones, doctor), porque economia-misiones.md dice que quedar en negativo es intencional. Precios de T-033: de prueba.

### D-040 Calor de 0 a 100 en 3 niveles, en Progresion/ (T-034) (antes D-tony-8)
El calor va de 0 a 100 con niveles 1, 2 y 3 desde 25, 50 y 75, empieza a bajar 10 s después del último aumento a
5 puntos/s (DatosCalor_Base). Los drones y patrullas de Alan leen el nivel; Alan puede enfriar o borrar desde el mundo.

### D-041 El rival reusa Combate (T-021) (antes D-tony-9)
`MechaCyberpunk.IA` referencia `MechaCyberpunk.Combate` (mismo dueño). El rival usa `VidaMecha`, `PeleadorMecha` y
`CicloGolpe`, así el jugador y el rival tienen las mismas reglas de vida, bloqueo y golpe. `PeleadorMecha` gana el evento
`PeleaPreparada` para que la presentación del rival se levante en la revancha.

### D-042 Cómo encuentra todo FlujoPelea.prefab (T-022) (antes D-tony-10)
El director busca la arena en `Registro<IArenaPelea>`, al jugador como el `IPeleador` del lado Jugador e instancia
el rival desde su prefab, con campos opcionales para fijarlos a mano. Arranca solo al dar Play (`empezarAlArrancar`). En
Pruebas_Tony.unity arranca con P, porque las pruebas de T-012 y T-013 cargan esa escena y esperan al jugador libre.

### D-043 Orden de T-F2 (antes D-tony-11)
T-030, T-031, T-032, T-021, T-022, T-033 y T-034. Cambia el orden de docs/estado.md (que ponía T-033 y T-034 antes
de la pelea) porque Alan no necesita cartera ni calor hasta A-F5 y el hito H1 sí necesita el rival y el flujo.

### D-044 Arena de prueba de Tony (T-022) (antes D-tony-12)
Tony prueba el flujo con `Pelea/Pruebas/ArenaDePrueba.prefab`, propia, que implementa `IArenaPelea`. No usa la jaula
de Alan, y sirve de ejemplo del contrato.

### D-045 El rival busca solo a su objetivo (T-021) (antes D-tony-13)
Si no tiene objetivo asignado, el rival busca en `Start` el `IPeleador` del lado contrario. Así Pelea/ no necesita
referenciar IA/ para presentarle el jugador.

## Planeación de T-F3 (2026-10-12, planificador Opus 5.5)

### D-046 Contrato para subir y bajar: IMontable (T-050, T-058) (antes D-tony-14)
Carro (Alan, A-F2) y mecha (Tony, T-F4) implementan `Core/Jugador/IMontable` y se registran en `Registro<IMontable>`.
`IJugador` y `ModoJugador` no cambian de forma: quien implementa `IJugador` (el jugador a pie de Tony) decide subir y bajar con la
tecla E (que el montable no lee), cambia `Modo` y `Camara` y avisa `ModoCambio`. Mientras va dentro, el jugador apaga su cámara,
su `AudioListener` y su `CharacterController` y sigue al `Asiento` del montable en `LateUpdate` (sin cambiar de padre); el
montable prende su cámara al subir y da una pose libre en el piso al bajar (`IntentarBajar(out Pose)`). Consecuencia para el
mundo: en carro los triggers no ven al jugador; para zonas usa `Registro<IJugador>.Actual.Raiz.position` o los colliders del
montable. Bajar a la fuerza (carro destruido, mecha en KO) queda para una tarea de Core de T-F4 si hace falta.

### D-047 Daño a pie sin IPeleador y daño de disparo (T-050, T-052, T-053) (antes D-tony-15)
`TipoDanio` gana `Disparo` (pistola a pie). La persona a pie y los muñecos de prueba implementan `IVida` e
`IRecibeDanio` pero NO `IPeleador`, porque `DirectorPelea` y `RivalBasico` buscan "el IPeleador de lado Jugador" entre todos los
MonoBehaviour y la confundirían con el mecha. Lo que no es peleador publica `EventosPelea.ImpactoRecibido` con `objetivo` null;
los suscriptores actuales ya comparan con el suyo y toleran null.

### D-048 UbicacionObjetivo en Core (T-050, T-055, T-057) (antes D-tony-16)
`Core/Objetivos/UbicacionObjetivo.Resolver(objetivo, lugares, out posicion)` es la regla única para saber dónde está
el objetivo (su posición, o la `PosicionEntrada` de su lugar). La usan el servicio de objetivos y el HUD de Tony, y Alan si
dibuja marcadores en el mundo; así nadie la reescribe.

### D-049 Referencias entre áreas de Tony en T-F3 (antes D-tony-17)
`MechaCyberpunk.Personaje` referencia `MechaCyberpunk.Combate` para reutilizar `CicloGolpe` y `BarridoGolpe` en los
golpes a pie. `MechaCyberpunk.UI` referencia `Personaje` (vida, armas, interacción) y `Progresion` (seguimiento del objetivo)
para el HUD a pie. Personaje y Progresion no referencian UI, así no hay ciclos. Ninguna referencia cruza a un área de Alan.

### D-050 Controles a pie (T-051) (antes D-tony-18)
`Personaje/Entrada/ControlesAPie.inputactions`, propio y separado de ControlesMecha: WASD mover, ratón mirar, Shift
correr, Espacio saltar, E interactuar (puertas, carro, más adelante el mecha), clic izquierdo atacar, 1 puños, 2 pistola,
R recargar y la misma tecla que el mecha para liberar el cursor. Caminar 4.5 m/s, correr 7.5 m/s, salto de 1.1 m y ojos a
1.65 m; velocidad y salto son `DatosPersonaje` (los cambiará el implante de piernas), lo demás `ConfigControlesAPie`.

### D-051 Pistola (T-053) (antes D-tony-19)
Tiro instantáneo al centro de la vista, sin apuntar con mira: 25 de daño, 60 m de alcance, 0.25 s entre tiros,
cargador de 8 y recarga de 1.4 s (`DatosPistola`). En T-F3 la reserva de balas es infinita; comprar balas o pistolas es de T-F7.

### D-052 HUD a pie por modo y canción de espera (T-056, T-057) (antes D-tony-20)
El HUD a pie es un Canvas Screen Space Overlay (flotante, como pide estetica-hud.md). A pie muestra vida, mira, arma,
objetivo, canción y avisos de interacción; en carro solo objetivo y canción (hay que poder seguir el objetivo manejando); en
mecha nada (D-011). Hasta que exista la radio (T-F7) la canción muestra "RADIO · sin señal" y la radio la cambiará con
`MostrarCancion`.

### D-053 Servicio de objetivos y guía de vuelta (T-055, T-057) (antes D-tony-21)
`ServicioObjetivos` (Progresion/) es un MonoBehaviour que implementa `IObjetivos` envolviendo `ModeloObjetivos` y se
registra en `Registro<IObjetivos>`. Sigue al jugador: distancia, mejor distancia, "se aleja" (25 m más que la mejor, durante
1.5 s, y se apaga a 10 m) y llegada (4 m o entrar al lugar del objetivo, por `EventosLugares`). La guía de vuelta del prólogo es
la flecha del HUD más el aviso "REGRESA"; la flecha apunta en línea recta, así que el director del prólogo (T-F6) pone objetivos
intermedios en las esquinas. El HUD usa el seguimiento solo si el `IObjetivos` registrado es un `ServicioObjetivos`.

### D-054 El jugador interactúa por registro, no por collider (T-054, T-058) (antes D-tony-22)
Para abrir una puerta o subir a un montable, el jugador busca en `Registro<IPuerta>` (por `Posicion`, solo las
cerradas) y `Registro<IMontable>` (por `PuntoSubida`, solo los que `PuedeSubir`) el más cercano a 2.5 m o menos y a 60° o menos
de hacia dónde mira. No asume nada de la jerarquía ni de los colliders de los objetos de Alan, y no hay que apuntar con precisión.

### D-055 Escena de pruebas a pie y un solo prefab del jugador (T-051, T-059) (antes D-tony-23)
Tony prueba a pie en `Escenas/Tony/Pruebas_APie.unity`, generada solo por `Personaje/Editor/` (cada tarea agrega su
parte al creador y la regenera; nunca se mezcla a mano). Los dobles de lo de Alan (`LugarDePrueba`, `PuertaDePrueba`,
`ZonaLugarDePrueba`, `CarroDePrueba`, `PuntoAparicionDePrueba`) viven en `Personaje/Pruebas/` y sirven de ejemplo del contrato.
`JugadorAPie.prefab` lleva anidados `HudAPie.prefab` y `ServiciosProgresion.prefab`, así Alan coloca un solo prefab y hay un
solo HUD y un solo `IObjetivos` por jugador. `AparicionJugador` lo coloca en el `IPuntoAparicion` cuyo `Id` se le indique.
Pruebas_Tony.unity no se toca, porque las pruebas de T-012, T-013 y T-022 la cargan.

### D-056 KO a pie mínimo (T-052) (antes D-tony-24)
En T-F3, al llegar a 0 de vida a pie el jugador pierde el control y la vista cae al piso; `Restaurar()` lo levanta
(en pruebas con la tecla K). Despertar en la clínica y que el mecha vuelva dañado al taller (personaje-progresion.md) es de T-F5/T-F6.

### D-057 Lo que espera a Alan en T-F3 (T-060, T-061) (antes D-tony-25)
Todo T-F3 se hace con dobles propios. Solo dos tareas de integración esperan a Alan y quedan en bloqueadas: T-060
(el jugador en Pruebas_Alan.unity: barrio, reloj, puertas y bar; espera A-F1) y T-061 (subir al carro de Alan; espera A-F2).
Sus pruebas cargan la escena de Alan e instancian el prefab en tiempo de juego, sin guardar nada en sus escenas. Pedido a Alan
(cuando T-059 esté integrada): colocar `Personaje/Prefabs/JugadorAPie.prefab` en Ciudad.unity y Pruebas_Alan.unity con
`idAparicion` "Casa_Puerta" y apagar ahí su cámara libre, para que haya un solo `IJugador` y Tony pueda hacer la prueba humana
de H2 en Ciudad.unity.

### D-058 Orden de T-F3 (antes D-tony-26)
T-050 (Core, se integra a main en cuanto se verifica, porque Alan necesita `IMontable` para A-F2), T-051, T-055,
T-054, T-052, T-053, T-056, T-057, T-058, T-059; luego T-060 y T-061 cuando Alan entregue. El mecha como montable (llamarlo,
subir y el encendido) es de T-F4: `MechaJugador` implementará `IMontable` con modo Mecha y por ahora no es `IJugador`.

## Planeación de T-F4 (2026-10-12, planificador Opus 5.5)

### D-059 Contratos de T-F4 en Core (T-062) (antes D-tony-27)
Tres contratos nuevos, escritos antes de usarse (D-027) e integrados a main en cuanto se verifiquen, porque Alan los
necesita en A-F3: `Core/Mecha/IPuntoLlamadoMecha` (Alan: `Llegada`, `Radio`, `Habilitado`; la zona permitida es el círculo del
radio y fuera de todos es zona prohibida, así no hay polígonos), `Core/Mecha/ITallerMecha` (Alan: `Estacionamiento` donde descansa
el mecha y `PuntoInteraccion` para reparar en T-F5) y `Core/Jugador/IPerfilJugador` (Tony: `Nombre`, `Alias`, `Cambio`; la
apariencia se agrega en T-F6 con otra tarea de Core). Bajar a la fuerza de un montable (D-tony-14) no hace falta en T-F4 porque
la emboscada no se puede perder: queda para T-F5/T-F6 (morir en la calle).

### D-060 El mecha es montable, con dos arranques y un permiso de piloto (T-064) (antes D-tony-28)
`MontajeMecha` (Mecha/) implementa `IMontable` modo Mecha. Arranque `Directo`: como hasta T-F3 (sin piloto, cámara y
control propios, no se registra como montable); es el de `MechaJugador.prefab`, para Pruebas_Tony, las pruebas de pelea y lo que
Alan ya coloque en Clandestino. Arranque `Estacionado`: vacío hasta que el jugador sube con E; es la variante
`MechaEstacionado.prefab` que Alan coloca en la ciudad. `PeleadorMecha` gana un permiso de piloto: control efectivo = permiso de
pelea (DirectorPelea) y permiso de piloto (montaje y encendido) y no KO, así el director y el montaje no se pisan.
`MechaCyberpunk.Mecha` referencia `MechaCyberpunk.Combate` (mismo dueño).

### D-061 Encendido de 3 pasos (T-065) (antes D-tony-29)
Cada vez que se sube: F confirma la entrada (1.0 s, se prenden luces y pantallas), mantener Espacio 1.5 s levanta el
mecha (la cámara sube desde agachado) y Q desbloquea el cristal (1.2 s, sube la persiana), con 0.3 s entre pasos: unos 5 s con
entrada inmediata (mecha-taller.md). Mientras enciende no se mueve, no golpea, no deja bajar y sí recibe daño. Las indicaciones van
en una pantalla de la cabina (D-011). Tecla equivocada: parpadeo rojo, sin castigo. Una pelea de arena (`PeleaPreparada`) y el
arranque Directo lo dejan encendido. Las acciones nuevas se agregan a ControlesMecha.inputactions conservando los id de las
existentes, para no romper las referencias del prefab.

### D-062 Llamado del mecha (T-066) (antes D-tony-30)
Tecla M, solo a pie. Llega si el jugador está dentro del radio de un `IPuntoLlamadoMecha` habilitado (el más cercano);
si no, aviso "AQUÍ NO PUEDE LLEGAR TU MECHA". Tarda 6 s (combate.md: "unos segundos"): desaparece del taller y en los últimos 1.5 s
baja del cielo desde 25 m a la `Llegada`, con una marca de neón en el piso; mientras llega no se puede subir. Con el mecha vacío
en la calle, M lo devuelve al taller (2 s), y regresa solo si el jugador se aleja más de 60 m. Sin taller registrado, su casa es
su pose inicial. Los avisos van en el HUD a pie (UI referencia Mecha; Mecha no referencia UI). Valores en ConfigLlamadoMecha.

### D-063 Calor por el mecha (T-067) (antes D-tony-31)
`ServicioCalor` (Progresion/, dentro de ServiciosProgresion.prefab) registra `ICalor`. El mecha suma 10 al llamarlo y
2 por segundo mientras el jugador va dentro y el mecha está en la calle (estado `EnCalle` del llamado): nivel 3 en unos 30 s.
Un mecha en arranque Directo (arena) no da calor, así no hace falta saber en qué lugar está el jugador (en el mecha los triggers
de Alan no lo ven). Se ve siempre en una pantalla de la cabina y en el HUD a pie y en carro solo con nivel mayor que 0.

### D-064 "No se puede perder" es dato del rival (T-068) (antes D-tony-32)
`PiedadRival` va en el prefab del rival, no en la escena: sus golpes nunca dejan al objetivo por debajo del 15 % de su
vida máxima (en el piso pegan 0) y, con el objetivo en 35 % o menos, se debilita hasta la revancha (daño ×0.5, espera ×1.6,
velocidad ×0.7, recibe daño ×1.5) y se ve débil. La llevan `RivalTutorial` y `RivalPandilla`; el rival normal no, así la pelea
casera de T-F6 sí se puede perder (demo.md). `IPeleador.Nombre` del rival pasa a ser el `Alias` de su `DatosRival` (lo necesita el
presentador). Rival del tutorial provisional: "EL CAMPEÓN", vida 120 y daño 12.

### D-065 Pandilla con turno de ataque (T-069) (antes D-tony-33)
Solo un rival por lado prepara golpe a la vez (turno compartido); los demás esperan a su distancia de ataque más 2 m
rodeando despacio. Los rivales no se pegan entre ellos y buscan objetivo cada 0.5 s si no tienen. `RivalBasico.usarTurnos` es
falso por defecto (el rival de arena no cambia). Pandilla provisional: "PANDILLERO", vida 60, daño 8; dos en la emboscada.

### D-066 Presentador (T-070) (antes D-tony-34)
Etapa `Presentacion` antes del conteo con líneas de `ConfigPresentador` (`{alias}`, `{rival}`, 1.6 s por línea, una
línea de grito más grande); sin config, el flujo es igual que hoy. Solo en la primera pelea, no en la revancha (configurable). El
alias sale de `Registro<IPerfilJugador>`; sin perfil, "NOVATO". Texto en la pantalla de la arena y evento `LineaDicha` para el audio
futuro. `ServicioPerfil` (Progresion/) va en ServiciosProgresion.prefab. `FlujoPeleaTutorial.prefab` (RivalTutorial + presentador)
queda listo para el prólogo.

### D-067 Emboscada como prefab de Tony que Alan coloca (T-071) (antes D-tony-35)
`Emboscada.prefab` (Pelea/) se coloca en el callejón con su +Z hacia el fondo; los rivales aparecen en poses de
`DatosEmboscada` relativas al prefab (con gizmos para que Alan vea dónde). Se activa al entrar el jugador a 12 m (o con
`Empezar()` desde el director del prólogo), una sola vez. Los rivales solo tienen control mientras el jugador va en el mecha: a pie
un golpe de mecha mataría a la persona, y si se baja a media pelea se detienen. Termina cuando todos están en KO y avisa
`Terminada`. Opcionalmente fija objetivos ("Llama a tu mecha (M)", "Sube a tu mecha", "Derrota a la pandilla"). Pelea sigue sin
referenciar IA ni Mecha.

### D-068 Escena de pruebas de la calle (T-064 a T-072) (antes D-tony-36)
`Escenas/Tony/Pruebas_Calle.unity` (calle, callejón, taller y punto de llamado de prueba, jugador a pie, mecha
estacionado y emboscada), generada solo por `Mecha/Editor/CreadorEscenaCalle.cs`; cada tarea agrega su parte y la regenera.
`MechaCyberpunk.Mecha.Editor` puede referenciar las áreas de Tony que necesite para armarla (solo editor). Los dobles de Alan van en
`Mecha/Pruebas/` (`PuntoLlamadoDePrueba`, `TallerDePrueba`). Pruebas_Tony.unity y Pruebas_APie.unity no se tocan.

### D-069 Lo que espera a Alan en T-F4 (T-073) (antes D-tony-37)
Todo T-F4 se hace con dobles. Solo T-073 (la emboscada en el callejón de Alan) queda bloqueada hasta A-F3. Pedido a
Alan para A-F3: un `ITallerMecha` en la casa; un `IPuntoLlamadoMecha` que cubra la entrada del callejón de la emboscada (y los que
quiera en calles abiertas, ninguno en interiores ni bajo techos); y, cuando estén en main, colocar sin editarlos
`Mecha/Prefabs/MechaEstacionado.prefab` (en cualquier sitio: se coloca solo en el taller) y `Pelea/Prefabs/Emboscada.prefab` en el
callejón. Aparecer ya dentro del mecha (pendiente de T-059) se pasa a T-F6, donde lo necesita el director del prólogo.

### D-070 Deudas de T-F3 primero (T-063) (antes D-tony-38)
Antes de tocar Combate y el HUD se quitan las observaciones menores de la revisión de T-F3: la prueba inestable de
GolpeBloqueoPruebas (se arregla la medición, no se afloja), el HUD repetido en pruebas, los textos de FormatoHud a ConfigHudAPie y
la captura T-059-2.

### D-071 Orden de T-F4 (antes D-tony-39)
T-062 (Core, a main en cuanto se verifique), T-063, T-064, T-065, T-066, T-067, T-068, T-069, T-070, T-071, T-072;
T-073 cuando Alan entregue A-F3. T-068 a T-070 no dependen del mecha montable y pueden adelantarse si algo se bloquea.

### D-072 Pelea referencia Progresion (T-071)
`MechaCyberpunk.Pelea` referencia `MechaCyberpunk.Progresion` porque `DirectorEmboscada` fija objetivos con `ServicioObjetivos`
(mismo dueño, sin ciclos: Progresion no referencia Pelea). Sigue sin referenciar IA ni Mecha.
