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

### D-011 Pantallas diegéticas. Propuesta.
Basado en estetica-hud.md ("en el mecha todo es diegético"). La vida del jugador se ve en una pantalla de la cabina (UI/).
El conteo y el resultado se ven en una pantalla grande de la arena (Arena/Flujo/). Nada flota sobre la vista.
La vida del rival no se muestra en la fase 1, porque personaje-progresion.md la liga al implante de ojos;
se comprueba en el inspector y con el parpadeo del rival al recibir un golpe.

### D-012 Golpe y bloqueo. Propuesta.
El golpe tiene tres momentos: preparación, impacto y recarga. En el rival, la preparación es el aviso visible.
La guardia tarda `tiempoSubirGuardia` en quedar arriba. Si está arriba al momento del impacto, el bloqueo es normal y quita
`reduccionBloqueo` del daño. Si quedó arriba dentro de los últimos `ventanaBloqueoPerfecto` segundos antes del impacto,
el bloqueo es perfecto y no entra daño. Esta es la ventana de tiempo para bloquear. El contraataque queda para otra fase.

### D-013 La revancha restaura la vida. Propuesta.
En la fase 1, la revancha empieza con la vida completa. El daño persistente y las reparaciones son el paso 5
del orden de construcción y se harán después.

### D-014 Escala. Propuesta.
1 unidad = 1 metro. El mecha greybox mide unos 4 m y la cámara de la cabina va a unos 3.5 m del piso.
La jaula mide unos 24 × 24 m. Los peleadores empiezan a unos 12 m uno del otro.

### D-015 Movimiento sin NavMesh. Propuesta.
Los mechas usan `CharacterController`. El rival camina directo hacia su objetivo, porque la jaula es abierta.

### D-016 Fin de pelea. Propuesta.
Gana quien deja al otro en KO primero. Si los dos caen en el mismo frame, cuenta el primer aviso de KO que llegue al flujo.
No hay límite de tiempo ni rounds en la fase 1.

### D-017 IDs de tareas
T-00x son tareas de Core y preparación. En la fase 1, T-01x son de Tony y T-02x del compañero.

### D-018 Herramientas de prueba
Los muñecos y atacantes de prueba viven en `<Área>/Pruebas/` de su dueño y solo se ponen en escenas de pruebas.
