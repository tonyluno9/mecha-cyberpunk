# TAREAS POR HACER — sesión de Unity (Claude Code de la computadora de Tony)

Este documento lo arma el chat orquestador con lo que Tony va pidiendo. Cuando Tony diga "haz las tareas por hacer", la sesión resuelve
TODAS, en orden, con estas reglas:

## Reglas para todas las tareas
- Paso 0: cierra Unity y Unity Hub, borra Temp/UnityLockfile, git pull de main. Si la carpeta principal tiene cambios sin guardar del chat de
  Blender, haz commit en una rama tony/blender-<fecha> y mergéala a main antes de empezar; no borres nada.
- Una sola instancia de Unity. Memoria y disco limitados: nada de PlayMode completo, pruebas por clase, builds uno por uno al final.
- No corras CreadorTodo. Nunca force push. Merge --no-ff a main y push (Tony lo autoriza).
- No edites el contenido de los FBX ni de los clips de Blender: solo su importación, los controladores, los overrides y el código.
  No toques PROMPTS_ASSETS.md ni registro.csv.
- Una tarea por rama T-xxx. Opus dirige y reparte a subagentes Sonnet.
- NADA se marca como hecho sin verlo en Play (y en el build cuando aplique), con captura en docs/capturas/_tareas/<tarea>/.
- Si algo no avanza en 15 minutos, mátalo, anótalo y sigue con la siguiente.
- Al terminar cada tarea, márcala aquí como HECHA (con fecha y commit) o NO QUEDÓ (con la razón). Lo hecho se queda en la sección "Hechas".
- Al final: build en Builds/Demo y luego en Builds/Juego como CyberCity.exe, uno por uno, y docs/reporte-tareas-<fecha>.md con cada tarea,
  su captura, si quedó y qué debe probar Tony.

---

## T1. Brazos del E18 en pose T (avatar)
**HECHA (22-nov, sesión B, T-1626 f5ecbbf): avatar del E18 en T por código; brazos del avatar 0.0/0.0°; E18 caminando en Play (docs/capturas/_tareas/T1).**
Problema: los brazos del avatar del E18 quedan ~20° abajo de la pose T desde que Blender corrigió su rig. Tony NO lo hace a mano.
Cómo:
1. Script de editor que reimporte Assets/Arte/Personajes/E18_EliteNova/E18_EliteNova_a.fbx y rehaga su avatar desde la T-pose: leer el
   HumanDescription del .meta, poner el esqueleto en T (rotar UpperArm/LowerArm a horizontal) y crear el avatar con AvatarBuilder.BuildHumanAvatar,
   o usar ModelImporter con avatarSetup = CreateFromThisModel y la pose T forzada. Equivale a Rig → Configure → Enforce T-Pose → Apply.
2. Volver a correr RobotsNovaT1553.Aplicar.
3. Los clips del E18 (E18_Caminar, E18_Correr, E18_Golpe_Der) copian este avatar.
Comprobar: en Play, el E18 camina, corre y golpea con los brazos a su altura, sin codos metidos en el torso. Pasa la prueba de brazos del E18.

## T2. Conductor robado: huir cuando termina su animación
**HECHA (22-nov, sesión B, T-1627 4d9df2b): el conductor huye al terminar su clip; usa NPC_SacadoDelCarro_Corto (3.04 s); pasan las pruebas de robo T705 y T1335 (la de T1335 miraba solo 1.6 s; el clip del conductor arranca 0.75 s después del del jugador).**
Problema: al robar un carro, el script de tráfico hace huir al conductor a los ~3 s y su clip PJ01_Conductor_SerSacado dura 7.25 s.
Cómo: en el script de tráfico (el código es de Tony, se puede cambiar), que el conductor huya cuando termine el clip (leer su duración o un evento
al final). Si Blender ya entregó la versión corta (~3 s), usarla y ajustar el tiempo a ella.
Comprobar: robar un carro: lo jalan, cae, se levanta y huye, sin cortar la animación. Pasa la prueba de robo de carro (hoy no detecta la animación
del conductor: revisar si mira el estado o el clip correcto).

## T3. Estados sin disparador
**HECHA (22-nov, sesión B, T-1622 y T-1628 f9fea2e): sentarse (Banca_Sentable, E), camilla al despertar, colgarse de bordes a 1.8-2.3 m y NPC que llama a la policía (T11). Falta colocar bancas y el punto Clinica_Camilla en el mapa (docs/para-sesion-A.md).**
Problema: sentarse, camilla, colgarse y "NPC llama a la policía" tienen su estado en el controlador pero nada los activa.
Cómo:
- Sentarse: el jugador junto a una banca o silla interactuable (E) se sienta; NPCs sentados en las bancas de las plazas.
- Camilla: al despertar en la clínica del Doc después de morir, empieza acostado en la camilla y se levanta.
- Colgarse: al saltar hacia un borde alto que alcanza con las manos, se cuelga y puede trepar (usar el sistema de parkour que ya existe).
- NPC llama a la policía: cuando un civil ve un crimen y no huye, saca el celular, hace la animación y sube una estrella si termina.
Comprobar: cada uno en Play con captura.

## T4. Balas que pegan en postes y objetos en vez del personaje (hitboxes)
**HECHA (sesión A, 22-nov, merge 96d66b1):** capa 8 NoDetieneBalas (cables, toldos, letreros colgantes, rejas), postes en cápsula vertical, cajas ajustadas (colliders grandes 43→0, salvo 3 botes con MeshCollider); BalasHitboxesT1602Pruebas 4/4, ArmasT1324EditPruebas 8/8; docs/capturas/_tareas/T4/resultado.txt (pega al NPC junto al poste; la pared detiene). Las PNG de batchmode se ven mal: probar en el juego.
Problema: si un civil o enemigo está junto a un poste u otro objeto y le disparas, la bala pega en el poste y no al personaje. Los colliders de
muchos objetos del mapa son más grandes que su modelo.
Cómo:
1. Barrido de todos los colliders de objetos del mapa (postes, faroles, letreros, semáforos, bancas, botes, puestos, toldos, árboles, cables,
   bolardos, barreras, rejas, edificios): ajustarlos a la forma real del modelo (cápsula delgada en postes, mesh collider convexo o cajas
   ajustadas en lo demás). Nada de una caja del tamaño de todo el modelo.
2. Los objetos delgados o huecos (cables, toldos, lonas, letreros colgantes, rejas de malla, ramas) NO detienen balas: ponlos en una capa que
   el raycast de disparo ignore. Siguen bloqueando al jugador y a los carros si hace falta.
3. El disparo sale de la cámara hacia la mira y luego del arma; revisar que la capa de los personajes (jugador, NPC, enemigos) tenga prioridad
   y que su hitbox sea su cuerpo (cápsula o huesos), no más grande ni más chica.
4. Las balas sí se siguen deteniendo en paredes, carros y objetos sólidos: no deben atravesar los assets.
Comprobar: en Play, un civil y un enemigo parados pegados a un poste, un farol, un letrero y un puesto: el disparo a su cuerpo les pega. Disparar
a una pared o carro con alguien detrás: no le pega. Captura de cada caso.

## T5. Voces de los SKULLS
**HECHA (22-nov, sesión B, T-1623 f3a02f3): 38 WAV, 13 IDs SKU_*, cola sin dos a la vez, 3D a 25 m en el grupo Voces, baja la música; los 13 momentos sonaron en Play ([VozSkulls]).**
Archivos: 37 WAV en "3d sonidos/19_Voces/SKULLS/" (leer su LEEME.md e INDICE_SKULLS.csv). "3d sonidos" sigue en .gitignore.
Cómo:
1. Copiar a Assets/Arte/Audio/19_Voces/SKULLS/ (Git LFS). Importar: 3D, mono, Decompress On Load, grupo Voces del AudioMixer.
2. Agregar los IDs al catálogo de sonidos con volumen, distancia (audible a ~25 m) y variación de pitch leve.
3. Conectarlos por momento:
   - SKU_AMBIENTE: platicando entre ellos en su zona (cada 15–40 s, al azar).
   - SKU_PROVOCAR y SKU_ULTIMO_AVISO: cuando se acercan a provocar al jugador; el último aviso justo antes de atacar.
   - SKU_INSULTO y SKU_RISA_BURLA: con la animación de burla y al pegarle al jugador.
   - SKU_GRITO_ALERTA: al ver al jugador o al empezar la pelea.
   - SKU_COBERTURA: al cubrirse. SKU_RECARGANDO: al recargar.
   - SKU_DOLOR: al recibir daño. SKU_QUEMADO: al quemarse.
   - SKU_HUIR: al huir. SKU_GANAMOS: cuando matan al jugador o ganan la pelea. SKU_LO_PERDIMOS: cuando el jugador escapa.
4. Reglas: variante al azar sin repetir la misma seguida; que no hablen dos pandilleros a la vez (cola o tiempo mínimo entre voces);
   la voz sale del pandillero que habla; las voces bajan un poco la música y el ambiente mientras suenan.
Comprobar: en Play, provocar, pelear, recargar, herir, quemar y escapar de un grupo de SKULLS; anotar qué voz sonó en cada momento.

## T6. Importar todo lo que entregue el chat de Blender
**Modelos y carros (sesión A): HECHA, merge dd7fd55.** V01a/V01b/V05 por piezas reimportados y Carro_* rearmados sin Cristal_* (largos 4.41/4.41/4.43 m, 0 referencias nulas); ya no había carros viejos de una malla en Ciudad_Prologo. Captura docs/capturas/_tareas/T6/carros-nuevos.png. Falla vieja ajena: Patrulla_ConservaSirenaRoboYGps (busca Asiento_Conductor). Anchos de V01a (2.37) y V05 (2.50) algo grandes. Las animaciones las conectó la sesión B.
Cómo:
1. Leer imageassets/blender/_REVISION/BITACORA.md, imageassets/blender/_REVISION_TONY/ (REVISION.md, ANIMACIONES_FALTAN.md) y los
   docs/tareas-unity-*.md: todo lo entregado después del último reporte se importa y se conecta.
2. Cuidado con los FBX reemplazados: si un FBX ya existía en Assets y Blender lo sobrescribió, su .meta viejo puede traer el esqueleto anterior y
   Unity no lo importa (pasó con 6 clips el 21-nov). Revisar raíz del esqueleto, avatar (desde la pose T de su personaje), rango de cuadros,
   bucle, raíz y compresión (apagada) clip por clip.
3. Pendientes conocidos que Blender va a entregar: muerte del E16 corregida (hoy al final parece elevarse), clips del E18 y E16_CaminarEscudo con
   hueso de cabeza (hoy la cabeza está mapeada al cuello: quitar ese parche cuando lleguen), versión corta del conductor robado, y las compuertas
   separadas de la cápsula de Nova.
4. Modelos (personajes, carros, piezas): escala real, materiales URP, colliders ajustados (ver T4), prefabs.
Comprobar: diagnóstico de clips y controladores en 0 problemas, y cada clip nuevo visto en Play.

## T7. Cápsula de caída de Nova (CP01) — ver documento propio
**HECHA (22-nov, sesión B, T-1621 9008cd1): cápsula CP01 con holograma; E16, E17 y dron vistos en Play (docs/capturas/_tareas/T7). Drones de persecución aún sin enganchar a la cápsula.**
El chat de Blender hizo la cápsula y Tony la aprobó. TODO el detalle (archivos, escalas por enemigo, línea de tiempo, efecto holográfico,
implementación y pruebas) está en docs/tareas/pendientes/T-capsulas-caida-nova-22nov.md: sigue ese documento al pie de la letra.
Resumen: los Nova (E15, E16, E18, perro E17 y drones DR01, DR02_a, DR04_a, E13) llegan en la cápsula CP01 escalada a su tamaño: cae, rebota,
la tapa sale volando, las paredes se abren como flor y se vuelven holograma cian que desaparece, y el enemigo sale con su clip *_Capsula_Salida
y pasa a su IA. Se combina con T8: el director de respuesta decide CUÁNDO y QUIÉN llega; la cápsula es CÓMO llega.
Ya no se usan las imágenes CP01/CP02 del chat de arte ni Tripo para esto.

## T8. Escalada lógica de enemigos: sin oleadas infinitas, con jerarquía y tiempos
**HECHA (22-nov, sesión B, T-1620 e1f849a): director de respuesta en Core/Respuesta con ConfigRespuesta; registro en docs/capturas/_tareas/T8 (patrulla repuesta ~30 s, máx. 3, jerarquía en orden, Nova de a uno). El tope total de 8 lo llenan las unidades bajas: si se quiere que lleguen más Nova, bajar topes de policía en ConfigRespuesta.**
Problema: llegan demasiados enemigos de golpe. Al destruir una patrulla empiezan a llegar patrullas sin fin; con Nova es peor: cae uno, lo matas y
empiezan a caer infinitos, y los perros salen todos juntos. No tiene lógica: en la vida real no mandan 1000 soldados; mandan uno, y si no te
detiene, mandan otro, cada vez más fuerte.
Cómo (un "director de respuesta" central que decide quién viene y cuándo; buscar el sistema actual: GestorPersecucion, RespuestaNova y los
spawners de drones, perros y Nova, y hacer que todos pasen por él):
1. JERARQUÍA fija, de menor a mayor. Solo se sube al siguiente nivel cuando el anterior no te pudo detener (sus unidades murieron o pasó su
   tiempo y sigues ahí):
   1) policías a pie y patrullas
   2) camionetas de policía (PA02)
   3) drones de policía
   4) soldados de Nova humanoides (guardias, E15)
   5) robots medianos (E18 élite y similares)
   6) perros robot (E17)
   7) robots grandes (E16)
   8) jefe (E14), solo en su misión o al tope máximo
   Las estrellas marcan hasta qué nivel PUEDE llegar la respuesta, pero los niveles se desbloquean en orden, no todos a la vez.
2. REPOSICIÓN CON TIEMPO (cooldown): cuando matas una unidad, su reemplazo NO sale al instante.
   - Patrulla destruida: la siguiente llega unos 30 s después, manejando desde lejos (no aparece al lado).
   - Soldado de Nova muerto: el siguiente cae unos 20–30 s después, en su cápsula (T7).
   - Con cada reemplazo que también pierden, pueden mandar uno más fuerte o uno más a la vez, pero de a poco.
   Tiempos en un ScriptableObject (ConfigRespuesta) para que Tony los ajuste sin tocar código.
3. TOPES de unidades vivas a la vez, por tipo y en total (ejemplo: patrullas 3, policías a pie 6, drones 2, Nova humanoides 2, perros 2,
   robots grandes 1, total 8). Nunca generar si ya se llegó al tope ni reponer todas de golpe.
4. EMPIEZA CHICO: al subir de estrellas primero llega UNA unidad del nivel nuevo. Si la vences, viene la siguiente con su cooldown.
5. BAJAR LA PRESIÓN: si escapas o te escondes, los niveles altos se retiran primero y los tiempos se reinician al perder las estrellas.
6. Los que ya llegaron no desaparecen mientras te ven (ya se arregló antes; no romperlo).
Comprobar en Play con un registro (hora, tipo, nivel, vivos) en consola o docs: destruir 5 patrullas seguidas (cada reemplazo ~30 s, nunca más
de 3); llegar a más de 5 estrellas y matar a cada Nova (cae uno, luego otro tras su tiempo, nunca una lluvia); ver el orden de la jerarquía.

## T9. Muro perimetral completo y bien unido; nada afuera
**HECHA (sesión A, 22-nov, merge 3c8ccff):** 12 huecos torre-puerta rellenos con MU01, torre flotante bajada, 904 objetos de afuera borrados (escena 53→32 MB); 3552 raycasts cada 1 m sin hueco; pruebas MuroPerimetroT1601Pruebas 3/3; capturas en docs/capturas/_tareas/T9/. Falta: probar a mano salir en carro y saltando.
Problema: el muro que rodea la ciudad tiene tramos mal unidos (huecos, encimados, desniveles) y todavía hay cosas afuera.
Cómo:
1. Recorre TODO el perímetro del muro (los 4 lados, las 4 esquinas y las 6 puertas) con un script que siga el borde y reporte cada unión:
   huecos entre tramos, tramos encimados o atravesados, saltos de altura o de rotación, tramos flotando o enterrados, y torres desalineadas.
2. Arregla cada unión: los tramos MU01 se tocan exactamente extremo con extremo (misma altura, misma rotación, sin espacio y sin encimarse); en las
   esquinas, una torre MU02 tapa la unión; en las puertas, MU03 queda alineada con los tramos de los lados. Collider continuo sin huecos: ni el
   jugador, ni un carro, ni una bala, ni un NPC pueden pasar por ningún punto. Que el muro baje hasta el piso en todo su largo (sin rendijas
   abajo, también donde el terreno cambia de altura).
3. Elimina TODO lo que quede afuera del muro: edificios, rellenos, props, luces, calles, terreno de sobra, fondos y cualquier objeto. Solo queda
   el muro y, del otro lado, el cielo. Borra de verdad de la escena lo que solo estaba apagado afuera (o déjalo en un prefab aparte fuera de la
   escena, si se quiere conservar), para que no pese ni vuelva a aparecer.
4. Las casas y calles de adentro llegan hasta el muro sin huecos ni terrenos vacíos entre la ciudad y el muro.
5. Prueba automática: nada activo con renderer o collider fuera del polígono del muro (Escena_NadaActivoFueraDelMuro), y una prueba que recorra el
   perímetro buscando huecos (raycasts cada 1 m a varias alturas desde adentro hacia afuera: todos deben chocar con el muro).
Comprobar: capturas de las 4 esquinas, las 6 puertas y una vista aérea completa del muro; intentar salir caminando, en carro y saltando en
varios puntos.

## T10. El juego sigue la historia nueva (con huecos para las cinemáticas de IA)
**HECHA (22-nov, sesión B, T-1625 910f567): recorrido de 19 pasos con cinemáticas pendientes C01-C09. Visto en Play: C01 dura lo previsto, paso 3 completo, paso 12 protegido (calor 0, la patrulla se va manejando). No comprobado en Play: VideoClip de prueba, pasos 15-19 y CONTINUARÁ (el paso 14 espera hablar con el Hacker). Marcadores a colocar en docs/para-sesion-A.md.**
Detalle completo en docs/tarea-historia-en-juego.md (léelo entero antes de empezar). Resumen:
1. Sistema `CinematicaPendiente` + `DatosCinematica`: en cada momento donde irá un video de IA, pantalla negra "CINEMÁTICA · <NOMBRE>" con
   lo que pasa y una barra, que dura exactamente lo que durará el video y se puede saltar; si se le pone el VideoClip, reproduce el video.
2. El recorrido de la historia en 19 pasos: recuerdo → despierta → los SKULLS lo empujan y lo tiran sin que pueda pegarles → maletín →
   ranura → la decisión → Ƀ20,000 → suero del Doc y primera mejora → arma → Inventor → la chica → liberar el Tianguis → Neón Alto →
   Hacker (fin de la demo, "CONTINUARÁ") → robots de NOVA → el hermano → jefe final → puertas → fin "¿YA SALISTE?".
   Sin aliados: EL CHATO, LA GÜERA y EL TOPO se desconectan. La ciudad se llama CYBER CITY en pantalla.
3. Peleas protegidas: en las misiones de historia no entra la policía ni sube el calor; lo que ya estaba se va manejando o volando fuera de
   la vista (nunca desaparece frente al jugador), aprovechando las cinemáticas para limpiar. Al terminar, el calor vuelve a la normalidad.
Comprobar: lo de la sección 10.4 del documento de detalle.

## T11. NPCs vivos: platican de frente y reaccionan a los balazos y a las muertes
**HECHA (22-nov, sesión B, T-1622 d9bc6f5): platican de frente (0° de desvío medido) alternando clips, oyen balazos por radio, ven muertes, huyen, se agachan, se calman y algunos llaman a la policía. No hay voz de miedo en el catálogo: gritan con NPC_*_DOLOR.**
Problema: los NPCs que platican se ponen de espaldas entre ellos y se ve rarísimo; y no reaccionan a los balazos ni a que maten a alguien a su lado.
Cómo:
1. PLATICAR DE FRENTE: al formar una pareja o grupo que platica, cada NPC se gira para mirar al otro (en grupo, hacia el centro), con una
   distancia de 1–1.5 m, y usa los clips de platicar y gesticular de Blender (NPC_Platicar*, discutir, señalar) alternando entre ellos para que no
   se vean iguales. Revisa por qué hoy quedan de espaldas: rotación del prefab o del modelo (Visual_*) girada 180°, el clip con la raíz girada,
   o el script que los acomoda mirando hacia afuera. Arréglalo para TODOS los tipos de NPC (NP01–NP14, woman, man, etc.).
2. OÍR BALAZOS: un disparo (del jugador, de la policía o de pandilleros) avisa a los NPCs dentro de un radio según el arma (~25 m pistola,
   ~40 m rifles y escopeta, ~60 m explosiones). Cada NPC que lo oye: se sobresalta (clip de asustarse), voltea hacia el ruido y huye corriendo
   en dirección contraria; algunos se agachan cubriéndose la cabeza o se tiran al piso si el tiroteo está muy cerca. Dejan de platicar.
3. VER UNA MUERTE: si matan a alguien a menos de ~15 m o en su vista, el NPC grita (voz de miedo hombre/mujer), puede llevarse las manos a la
   cabeza y huye; los que están en el mismo grupo huyen todos.
4. RECIBIR DAÑO: reacción de dolor según el lado (frente, espalda, izquierda, derecha), cojea si está herido, y muere con su animación.
5. CALMARSE: después de ~20–40 s sin balazos y lejos del peligro, vuelven a caminar normal; los que huyeron no regresan al mismo punto de
   inmediato. Algunos sacan el celular para grabar o llamar a la policía (T3).
6. Cuidado con el rendimiento: el aviso de ruido es por evento (no Update en cada NPC) y con un tope de reacciones por segundo.
Comprobar en Play: parejas y grupos platicando de frente en las plazas, el tianguis y la banqueta (capturas); disparar al aire cerca de un grupo
(huyen y se agachan); matar a un NPC frente a otro (el otro grita y huye); esperar a que se calmen.

## T12. Los SKULLS molestan a los civiles
**PARCIAL (22-nov, sesión B, T-1624 975ff84): el sistema de escenas y bullying está hecho, pero en Play no se pudo ver un bullying dinámico completo: los 25 SKULLS de la ciudad ya están en sus grupos de acoso fijos (GrupoAcoso, que sí molestan a un civil) y la prueba con SNAKES terminó cuando atacaron al jugador. Forzar con DirectorEscenasBarrio.ForzarEscena("bully_snakes_senora") lejos de ellos.**
Problema: los pandilleros solo esperan al jugador; deberían hacer la vida difícil a la gente del barrio, como parte de la historia.
Cómo:
1. De vez en cuando (cada 1–3 min por grupo, solo en su territorio y si el jugador está cerca para verlo), un pandillero o dos eligen a un civil
   que pasa y lo molestan: se le acercan, le gritan o le dicen cosas (voces SKU_INSULTO, SKU_PROVOCAR, SKU_RISA_BURLA de T5), lo empujan (clip de
   empujón) y a veces lo tiran al suelo (el civil cae con el clip de derribado y se levanta) o le quitan algo (animación de recoger).
2. El civil reacciona: se defiende con las manos, suplica (manos arriba), cae, y al soltarlo huye asustado; los NPCs de alrededor miran o se
   alejan (T11).
3. Si el jugador interviene (golpea o dispara a los pandilleros), dejan al civil y van contra el jugador; el civil huye. Si el jugador no hace
   nada, los pandilleros se ríen y regresan a su lugar.
4. No pasa en misiones de historia protegidas ni durante cinemáticas (T10), y nunca más de uno de estos eventos a la vez cerca del jugador.
5. Esto encaja con la historia: es lo que el jugador ve que pasa en su barrio (paso 3 y paso 6 de T10).
Comprobar en Play: caminar por el territorio de los SKULLS hasta ver uno de estos eventos (o forzarlo con un menú de depuración); que el empujón y
la caída se vean bien, que suenen las voces, que el civil huya, y que al intervenir los pandilleros cambien hacia el jugador. Capturas.

## T13. Tráfico ordenado: rutas fijas, sin choques ni amontonamientos
**HECHA (sesión A, 22-nov, merge dd7fd55):** reserva de cruces (ReservaCruces), seguimiento por ruta (parada 6 m, frenado 16 m), 20 carros de día / 15 de noche reciclados lejos y fuera de la vista, destrabar a los 5 s (forzado 7 s), EstadisticasTrafico. 3 min en Ciudad_Prologo: 0 atorados >8 s, máx. 19 vivos, 4 choques todos en un mismo cruce (x≈-47, z≈-2.4) que falta revisar. Pruebas TraficoOrdenadoT1603 3/3 PlayMode, TraficoCallePruebas 8/8, ChoqueTraficoT703 5/5. Capturas docs/capturas/_tareas/T13/. Los carros no huyen ante el caos (frenan y retoman).
Problema: los carros del tráfico se chocan, se amontonan, se atoran (uno se echa para atrás, otro para adelante) y hacen un desastre que se ve
mal. Además se sienten como tráfico infinito sin orden.
Cómo:
1. RUTAS FIJAS POR CARRIL: el tráfico civil sigue una red de carriles con waypoints o splines (uno por sentido de cada calle, por el centro de su
   carril), con conexiones definidas en cada cruce (seguir recto, girar a la derecha o a la izquierda). Cada carro elige su siguiente tramo en el
   cruce y nada más; no conduce "libre" por física buscando su camino. Revisa el sistema actual (SistemaTrafico, carriles, AparecerTrafico) y
   reutiliza lo que sirva.
2. CONDUCCIÓN SIMPLE Y ESTABLE: el carro se mueve suavemente sobre su carril (seguir la curva con velocidad según la curvatura), sin reversa ni
   maniobras raras. Si está atorado más de 5–8 s, se recoloca en su carril fuera de la vista del jugador o se borra fuera de la vista y reaparece en
   otro punto (nunca frente al jugador).
3. DISTANCIA DE SEGUIMIENTO: cada carro mira al de adelante en su carril (raycast o consulta a la red) y frena para mantener 6–10 m; nunca lo
   choca. Frenan también ante peatones, el jugador y obstáculos.
4. CRUCES ORDENADOS: semáforos (los que ya existen) o una regla de paso por cruce (reservar el cruce: solo pasa un carro o un sentido a la vez),
   para que no se metan dos al mismo tiempo ni se queden trabados en medio.
5. CANTIDAD FIJA Y RAZONABLE: un número fijo de carros vivos alrededor del jugador (por ejemplo 15–25 según rendimiento), que ciclan dentro del
   muro: cuando uno se aleja mucho, se recicla y reaparece lejos y fuera de la vista en otra parte de la red. Nada de generar sin fin. Carros
   estacionados aparte, quietos en su lugar.
6. SOLO CUANDO HAY CAOS: si el jugador choca un carro, dispara o roba, los conductores reaccionan (frenan, se quedan detenidos o huyen), pero
   en cuanto se calma vuelven a su ruta; los carros chocados se quedan detenidos y se reciclan fuera de la vista.
7. Policía y persecuciones usan su propia conducción (no tocar lo que ya funciona), pero respetando que el tráfico civil se aparte.
Comprobar en Play: 5 minutos de observar la ciudad desde varios cruces (captura de cada cruce y un registro de choques entre carros del tráfico:
debe ser 0 o casi 0, y ningún carro atorado más de 8 s); recorrer los 4 barrios manejando y confirmar que el tráfico se ve ordenado y que no hay
amontonamientos.

## T14. Más realismo visual: cielo, charcos, luz y materiales
**HECHA (sesión A, 22-nov, merge 4e47779):** cielo propio (degradado, nubes que se mueven teñidas por barrio, estrellas, luna con halo), SSAO, postproceso (bloom, ACES, split toning, viñeta, aberración leve), normal map genérico en 100 materiales, polvo cerca del jugador, selector Bajo/Medio/Alto conectado a Opciones. Medido en build a 1080p en 13 puntos: 199–245 fps de promedio, mínimo 73 (antes 233–296). Capturas antes/después en docs/capturas/_tareas/T14/. No quedó: vapor de rejillas (no hay rejillas con ese nombre), SSR (URP no lo trae; se usan las sondas y el reflejo planar de charcos que ya había), sharpening, ventanas cálidas nuevas.
Objetivo: que la ciudad se vea más real y rica, como las referencias K01–K10 y mapa-real/*_final.png, sin bajar de 50 fps.
Cómo:
1. CIELO: cielo de noche con profundidad (degradado azul profundo a morado cerca del horizonte, nubes bajas iluminadas por la ciudad con tono
   naranja y magenta, algunas estrellas tenues arriba, luna con halo). Skybox o cielo procedural con nubes que se mueven lento. Que el reflejo de
   la ciudad en las nubes cambie por barrio (naranja en Suburbios, magenta en Neón Alto, azul frío en Nova). Mismo cielo de día con su versión.
2. CHARCOS Y PISO MOJADO: charcos con bordes irregulares (máscara T10_Charcos o T07_AsfaltoMojado), reflejos nítidos de luces y neón
   (Screen Space Reflections si el URP del proyecto lo permite, o sondas de reflexión más densas y reflexión planar solo cerca del jugador),
   ondas al pisar (lo que ya hay) y gotas de lluvia cuando llueve. Asfalto con rugosidad variable (más brillante en lo mojado, mate en lo seco).
3. LUZ: luces de poste con su cono y halo volumétrico en la niebla, neones que iluminan su alrededor con su color, ventanas con luz cálida al
   azar, sombras suaves de los postes y los carros, oclusión ambiental (SSAO) para dar profundidad en esquinas y bajo los toldos.
4. MATERIALES: revisar que todo tenga normal map y rugosidad (paredes, banquetas, metal, carros con brillo y reflejos de pintura), y quitar
   cualquier material plano o sin textura que quede.
5. ATMÓSFERA: niebla con color y altura (más densa abajo y a lo lejos), partículas finas en el aire cerca de las luces (polvo, vapor de
   alcantarillas, humo de los puestos), y vapor saliendo de rejillas y tuberías.
6. POSTPROCESO: bloom controlado en neones y luces, gradación de color cine (sombras azules, luces cálidas), viñeta suave, un poco de
   aberración cromática solo en los bordes si no ensucia, y nitidez (sharpening) leve.
7. RENDIMIENTO: medir antes y después en los puntos de la medición del 21-nov; si algo baja de 50 fps, dejar una opción de calidad en el menú
   (Bajo / Medio / Alto) que apague lo más pesado.
Comprobar: capturas antes y después en los mismos puntos (K20–K34), lado a lado con su referencia _final, y tabla de fps.

## T15. Vida entre NPCs: diálogos con subtítulos, bullying de las pandillas y escenas del barrio (amplía T12)
**HECHA (22-nov, sesión B, T-1624 975ff84): subtítulos (panel y opción en el menú), 13 guiones EscenaBarrio y docs/dialogos-barrio.md; escenas normales completas en Play. El bullying dinámico, como en T12.**
Objetivo: que el barrio se sienta vivo y que se note que los SKULLS y los SNAKES abusan de la gente normal.
Cómo:
1. SUBTÍTULOS: sistema de subtítulos para cualquier línea de voz del mundo (abajo al centro, letra fina, nombre o tipo del que habla en color
   —"SKULL:", "SEÑORA:"— y fondo semitransparente), solo si el que habla está cerca del jugador (~15 m) y a la vista. Se muestran aunque todavía no
   haya audio (duración según el largo del texto), y cuando exista el audio se sincronizan con él. Opción en el menú para apagarlos.
2. GUIONES DE ESCENAS: un ScriptableObject por escena (EscenaBarrio) con las líneas (quién habla, texto, audio opcional, animación) para que Tony
   grabe las voces después y solo las asigne. Usa las voces que ya acomodó el chat de audio (SKULLS en 19_Voces/SKULLS, T5) donde encajen; el
   resto se queda solo con subtítulo.
3. ESCENAS DE BULLYING (mínimo 6, con animación de empujar, tirar al suelo y levantarse, rendirse, burla):
   - Dos SKULLS le cobran "piso" a un vendedor del tianguis: "¿Y la cuota de esta semana, jefe?" / "Ya les pagué ayer..." / "Pues hoy pagas doble."
     Le tiran la mercancía.
   - Un SKULL le quita el celular a un joven y lo empuja al suelo: "Préstame tu cel, carnal... gracias." Risas.
   - SNAKES acorralan a una señora con su mandado en Neón Alto: "Esta calle es nuestra, doña. Aquí se paga por pasar."
   - Un pandillero tira a un repartidor de su moto o bici y patea su caja.
   - Dos SKULLS se burlan de un señor mayor y lo empujan; él se cae, ellos se van riendo: "Ándale, abuelo, más rápido."
   - Un grupo intimida a un comerciante que cierra su cortina: "Mañana venimos por lo de la protección."
   Cada víctima reacciona (súplica, manos arriba, cae, huye) y los civiles de alrededor se alejan o miran con miedo (T11). Si el jugador interviene,
   los pandilleros lo encaran (T12).
4. ESCENAS NORMALES DEL BARRIO (para que no todo sea violencia): vendedores ofreciendo ("¡Lleve sus tacos!", "¡Elotes, esquites!"), vecinos
   platicando del muro y de NOVA ("Dicen que nadie ha salido del muro en años..."), una pareja discutiendo, alguien
   hablando por celular, el borracho de la esquina. Con subtítulos.
5. FRECUENCIA: cada escena se dispara cerca del jugador (20–40 m) con un tiempo mínimo entre escenas, nunca dos a la vez, no durante misiones
   protegidas ni cinemáticas (T10), y no se repite la misma seguida.
6. Lista de todas las líneas en docs/dialogos-barrio.md (quién, texto, escena, archivo de audio esperado), para que Tony sepa qué grabar.
Comprobar: forzar cada escena con un menú de depuración y verla completa (animaciones, subtítulos, reacción de los alrededores), y caminar 10
minutos por los barrios contando cuántas escenas aparecen solas. Capturas de cada escena.

---
# NUEVAS (22-nov, después de la prueba de las 15 tareas)

## T16. Tráfico: los civiles ya no manejan, solo patrullas patrullando (y con semáforos)
**HECHA (22-nov, merge 45695c3):** civiles en movimiento apagados (TraficoCalle.circulanCiviles=false), 57 carros estacionados junto al bordillo (a ≥20 m de los cruces), 3 patrullas de ronda que se paran en rojo, semáforos SemaforoCruce con 4 ES01 en cada uno de los 2 cruces de la red. 3 min en la ciudad: 0 choques, 0 atorados, 17 paradas en rojo, estacionados quietos; robar un estacionado funciona y un delito junto a la ronda arranca la persecución. Pruebas 13/13 EditMode, 4/4 y 8/8 PlayMode. Capturas docs/capturas/_tareas/T16/. Visto aparte: un CarroRobable_17 que nace en (-20, -127.8), fuera del muro, se cae del mapa (otro sistema).
Decisión de Tony: para acabar con los choques y amontonamientos, los carros civiles ya NO circulan.
Cómo:
1. Los carros civiles quedan solo ESTACIONADOS junto a la banqueta (quietos, bien alineados, sin encimarse ni salir a la calle); el jugador
   los puede robar como siempre. Apaga la generación del tráfico civil en movimiento (el sistema de T13 se reutiliza para las patrullas).
2. Lo único que circula sin persecución son POCAS patrullas patrullando (2 a 4 en toda la ciudad), siguiendo las rutas por carril de T13,
   a velocidad tranquila, sin torreta. Si el jugador comete un delito cerca, la patrulla reacciona y empieza la persecución (sistema actual).
3. SEMÁFOROS que funcionen en los cruces: ciclo verde/amarillo/rojo por sentido, con sus luces encendidas en el modelo (los semáforos ES01 que ya
   hay; si un cruce no tiene, ponlo). Las patrullas que patrullan se detienen en rojo; en persecución pueden pasarse el alto con la sirena.
4. Con esto, el cruce de los choques (x≈-47, z≈-2.4) deja de tener problema; revísalo de todos modos.
5. Peatones (NPCs) siguen caminando normal por las banquetas.
Comprobar: 3 minutos mirando la ciudad: solo patrullas circulando, que se detienen en los semáforos en rojo, 0 choques; robar un carro
estacionado; cometer un delito junto a una patrulla que patrulla y que empiece la persecución.

## T17. Escalada final: con el máximo de estrellas la policía se retira y solo llega Nova (progresivo)
Decisión de Tony: si llegas al máximo de estrellas es porque la policía no pudo frenarte; entonces deja de llegar policía y empiezan a llegar
SOLO los Nova, de forma progresiva. Ajusta el director de respuesta de T8.
Cómo:
1. Estrellas 1 a 4: solo policía (patrullas, policías a pie, camionetas, drones de policía) con la jerarquía y tiempos de T8.
2. Al llegar al máximo (5 estrellas): aviso especial (sonido y texto "NOVA INTERVIENE"), deja de generarse policía nueva y la que hay se va
   retirando manejando o caminando fuera de la vista (nunca desaparece frente al jugador). Los topes que antes ocupaban las patrullas quedan libres
   para Nova (así ya no frenan su llegada).
3. Nova llega progresivo, en cápsulas (T7), con su jerarquía: primero 1 soldado humanoide (guardia/E15); si lo vences, después de su tiempo 1 o 2
   más; luego E18 élite; luego perros E17; luego el robot grande E16; drones de Nova en cápsula también (conectar los drones que faltaron).
   Máximo de Nova vivos a la vez (por ejemplo 3, con 1 robot grande). Tiempos en ConfigRespuesta.
4. Si escapas, se retiran primero los niveles altos y bajan las estrellas como ya funciona.
Comprobar en Play con el registro de T8: subir a 5 estrellas y ver que deja de llegar policía, la que estaba se va, y llegan Nova de a poco en
cápsulas en el orden de la jerarquía.

## T18. NPCs sentados con la animación de sentado del jugador
Decisión de Tony: para los NPCs sentados en las bancas (y donde haga falta) se usa el clip de SENTADO DEL JUGADOR (PJ01), retargeteado por
Humanoid a los NPCs. No hace falta un modelo nuevo de civil sentado.
Cómo: en las 4 bancas sentables del parque y en otras bancas y sillas del mapa (puestos, plazas), poner NPCs (de NP01–NP14 y los demás) sentados
con ese clip, alineados al asiento (cadera sobre el asiento, pies en el piso, mirando al frente), con variaciones pequeñas (algunos con el
celular, platicando de frente si son dos — T11). Cuando oyen balazos se levantan y huyen (T11).
Comprobar: capturas de NPCs sentados en el parque y en otro punto, bien alineados al asiento, y que se levanten y huyan al disparar.
