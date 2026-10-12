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
