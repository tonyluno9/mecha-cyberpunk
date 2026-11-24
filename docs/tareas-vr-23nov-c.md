# Tareas VR · 23-nov noche (segunda prueba de Tony, build VR4)
Bien según Tony: intro, video, menú, láser (perfecto), carga tipo cine. Rama vr/quest3, carpeta wtVR. Build final en Builds\VR5.

| # | Tarea | Prioridad | Estado |
|---|---|---|---|
| VR29 | GRAVE: menús del Doc, Tino y máquinas bugueados; no se puede salir | 1 | PENDIENTE |
| VR27 | Armas con gestos, recarga manual, rueda sin parpadeo, nuevo reparto (Y pausa) | 1 | PENDIENTE |
| VR25 | HUD sin borde negro | 1 | PENDIENTE |
| VR26 | Viñeta al girar mucho menor y rápida | 1 | PENDIENTE |
| VR28 | Todos los avisos con botones de VR | 2 | PENDIENTE |
| VR24 | Nuevo arranque: 3 SKULLS al regresar, Ƀ20,000, sin ranura | 2 | PENDIENTE |
| VR30 | Ruta minimalista de puntos | 2 | PENDIENTE |
| VR23 | Cosas flotando (de la tanda anterior) | 3 | NO QUEDÓ antes |
| VR31 | Imágenes de la guía de controles | 2 | PENDIENTE |
| VR32 | Videos reales de las cinemáticas | — | LO HACE LA SESIÓN DE MAIN (docs/tareas/pendientes/T-cinematicas-videos-23nov.md); llega a VR con merge de main |
| — | Voces de personajes | — | LO HACE LA SESIÓN DE MAIN (docs/tareas/pendientes/T-voces-personajes-23nov.md); llega a VR con merge de main |
| VR34 | Cuerpo completo en VR (manos → IK → piernas) | — | EN ESPERA (hasta que Tony diga) |
# VR24 · Nuevo arranque: pelea contra 3 SKULLS al regresar del muro
**Lo que dijo Tony:** la intro se queda igual (C01 EL RECUERDO, C02 EL MALETÍN, la pelea/robo del maletín, C03 LA DECISIÓN: se regresa). Lo que cambia: después de que se regresa, empieza el juego justo donde aparece al inicio; ahí salen 3 SKULLS y se los golpea. Al vencerlos sale el aviso de que tiene Ƀ20,000 en el banco y sigue la historia (VE CON EL DOC).
**Qué hacer:**
1. Quitar el objetivo "VE A LA RANURA DEL MURO" y el paso jugable de ir a la ranura: después de C03 el jugador aparece en su punto de inicio.
2. Ahí aparecen 3 SKULLS normales (los de los modelos actuales, con sus clips y voces) que lo encaran; pelea a puños, PELEA PROTEGIDA (sin policía ni NOVA, calor congelado). El jugador SÍ puede golpear.
3. Al vencer a los 3: aviso "TIENES Ƀ20,000 EN EL BANCO" (estilo de avisos del juego), se dan los Ƀ20,000 y sigue "VE CON EL DOC".
4. El paso "Lo molestan" (empujón, "NO PUEDES CON ELLOS") sigue saltado.
5. Es cambio de historia: que funcione igual en VR y en PC (código común de la historia).
**Cómo comprobar:** partida nueva: cinemáticas → apareces en tu punto → 3 SKULLS → los vences → aviso de Ƀ20,000 → VE CON EL DOC. Sin objetivo de la ranura.
**Estado:** PENDIENTE
# VR25 · HUD sin borde negro
**Lo que vio Tony:** el HUD pegado a la cabeza está bien (que se quede así, siguiendo la vista), pero tiene un borde/esquinas negras que marcan que es una pantalla.
**Qué hacer:** quitar todo borde, marco, viñeta o fondo del panel del HUD: solo los elementos (vida, mapa, prótesis, objetivo…) flotando sobre el mundo, con fondo 100% transparente (revisar alfa de la RenderTexture, el shader del panel curvo y el clear color de la cámara de UI).
**Cómo comprobar:** en el visor no se ve ningún borde ni esquina oscura alrededor del HUD.
**Estado:** PENDIENTE
# VR26 · Viñeta al girar: mucho menos y más rápida
**Lo que vio Tony:** al girar con el stick derecho la vista se cierra muchísimo y se queda cerrada mucho tiempo. No se marea.
**Qué hacer:** viñeta por defecto muy leve (que cierre ~20% de lo que cierra hoy, solo las orillas), entra y sale rápido (~0.1 s). Igual al caminar. Opción VIÑETA en Opciones sigue (NO / BAJA / MEDIA), por defecto BAJA.
**Cómo comprobar:** al girar apenas se oscurecen las orillas y vuelve al instante.
**Estado:** PENDIENTE
# VR27 · Armas con gestos, recarga manual y nuevo reparto de botones
**Lo que dijo Tony:** la rueda de armas parpadea y no se queda al mantener el botón. Prefiere sacar armas con un gesto. Recargar: con el botón de abajo del control derecho (A) sale el cargador, tomas uno nuevo y recargas. El botón de abajo del izquierdo (X) no hace nada. El botón de la rueda mejor que sea pausa.
**Qué hacer:**
1. Sacar arma con gesto: mano derecha a la cadera derecha + grip = sacar/guardar la pistola (o el arma corta equipada); mano derecha sobre el hombro derecho + grip = arma larga equipada (rifle, plasma, lanzagranadas). Funda y sonido de sacar/guardar. Vibración.
2. Recarga manual: A suelta el cargador (cae con física, sonido). La mano izquierda a la cadera izquierda + grip toma un cargador nuevo (modelo del cargador del arma si existe; si no, el del arma); acercarlo a la pistola lo mete (clic, vibración) y recarga. Si no tienes balas de reserva, no sale cargador.
3. Arreglar la rueda de armas (parpadeo): mantener X la abre y se queda mientras lo mantienes; eliges con la mano izquierda; queda para granadas y armas especiales.
4. Reparto final:
| Acción | Botón |
|---|---|
| Caminar / correr | Stick izquierdo / clic del stick izquierdo |
| Girar | Stick derecho |
| Saltar | Clic del stick derecho |
| Disparar / clic en menús | Gatillo derecho |
| Sacar arma | Gesto cadera / hombro + grip derecho |
| Recargar | A suelta cargador + gesto cadera izquierda |
| Interactuar | B |
| Rueda de armas | Mantener X |
| Pausa | Y (y también Menú) |
| Rueda de prótesis | Grip izquierdo + gatillo izquierdo, o gestos |
| Agarrar / lanzar (implante) | Grip con la mano libre cerca del enemigo |
| Recentrar | Mantener stick derecho abajo 1 s |
5. Actualizar guía de controles, letreros y textos.
**Cómo comprobar:** sacar y guardar pistola con el gesto; A suelta cargador y recargas con la izquierda; X mantiene la rueda sin parpadeo; Y pausa; clic del stick derecho salta.
**Estado:** PENDIENTE
# VR28 · Todos los avisos con los botones de VR
**Lo que vio Tony:** con Joel (el Doc) sale "E para hablar". En VR debe decir B.
**Qué hacer:** en ModoVR todo texto con teclas de PC (E, Tab, R, Shift, Esc, Espacio, clic, rueda del ratón…) se cambia por el botón o gesto de VR de la tabla de VR27: avisos de interactuar, tutoriales generales, consejos de la pantalla de carga, ayudas de tiendas y menús, cinemáticas. Hacerlo con una sola tabla de traducción, no texto por texto.
**Cómo comprobar:** en VR no aparece ninguna tecla de teclado en ningún aviso.
**Estado:** PENDIENTE
# VR29 · GRAVE: menús del Doc (cuerpo/prótesis), Tino y máquinas bugueados en VR
**Lo que vio Tony:** al hablar con el Doc sale la interfaz del cuerpo pero bugueadísima; solo alcanza a ver partes si mueve la cabeza rápido. No pudo salir de ella. Seguro pasa igual con Tino.
**Qué hacer:**
1. Toda pantalla completa de PC (cuerpo/prótesis del Doc, tienda y banco de Tino, mejoras, máquinas expendedoras, campo de tiro, pausa, opciones, mapa) se ve COMPLETA en VR en un panel grande fijo frente a ti (como el menú de inicio, que ya funciona perfecto), se elige con el láser y el gatillo.
2. Siempre hay salida: B o Y cierran / regresan (además del botón SALIR).
3. Las vistas 3D de esas pantallas (el cuerpo rotando, el arma en la tienda) se ven dentro del panel.
4. Revisar uno por uno: Doc, Tino (tienda, banco guiado de T84, mejoras), máquinas, campo de tiro, pausa, opciones.
**Cómo comprobar:** en el visor, abrir cada pantalla, ver todo, elegir con el láser y salir.
**Estado:** PENDIENTE
# VR30 · Ruta al objetivo minimalista
**Lo que dijo Tony:** debe marcar una ruta, aunque sea con puntitos o algo súper tecnológico y minimalista, que no sea la línea que había antes.
**Qué hacer:** en VR, puntos o chevrones holográficos pequeños en el piso (cian del juego, brillo suave) cada ~2 m siguiendo el NavMesh hacia el objetivo actual; solo los siguientes ~25 m, se desvanecen con la distancia y se animan suave hacia adelante. Desaparecen al llegar. Opción en Opciones "RUTA: SÍ/NO" (en VR SÍ por defecto). No usar la línea vieja.
**Cómo comprobar:** al tener objetivo (VE CON EL DOC) se ven los puntitos en el piso y te llevan.
**Estado:** PENDIENTE
# VR31 · Imágenes de la guía de controles VR
**Estado:** PENDIENTE (las imágenes ya están)
**Dónde están:** `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\ui\controles-vr\` (carpeta principal, no wtVR). Copiarlas a wtVR en `Assets/Arte/UI/ControlesVR/`.
**Usar estas 6 (16:9):**
- VR_Controles_Botones.png: los dos controles; brillan sticks, X, Y, A, B, gatillos y grips, con un punto cian donde va cada etiqueta.
- VR_Gesto_SacarPistola.png: mano derecha a la cadera derecha, saca la pistola.
- VR_Gesto_ArmaLarga.png: mano derecha sobre el hombro derecho, toma el arma larga.
- VR_Gesto_Recargar.png: suelta cargador, la izquierda toma uno de la cadera izquierda.
- VR_Gesto_ProtesisPiernas.png: manos en los muslos, brillan prótesis de piernas.
- VR_Gesto_ProtesisBrazos.png: brazos abiertos, brillan prótesis de brazos.
**NO usar** los que terminan en `_v1` (VR_Controles_Botones_v1.png, VR_Gesto_SacarPistola_v1.png): versión vieja con controles equivocados.
**Qué hacer:**
1. Importar como Sprite (2D and UI), sin compresión fuerte (que se lean en el visor), mip maps apagados.
2. La guía (primera vez en VR y Pausa → CONTROLES) pasa a ser estas imágenes en páginas (botones → sacar pistola → arma larga → recargar → prótesis piernas → prótesis brazos), con SIGUIENTE/ANTERIOR/CERRAR con el láser. Reemplaza los modelos 3D de los Starter Assets de VR20.
3. Las imágenes no traen texto: etiquetas en Unity encima, estilo HUD (letra delgada, cian), en el espacio libre. En VR_Controles_Botones cada etiqueta junto a su punto cian según la tabla final de VR27 (stick izquierdo = moverse / clic correr, stick derecho = girar / clic saltar, A = soltar cargador, B = interactuar, X = rueda de armas, Y = pausa, gatillo derecho = disparar, grips = agarrar / sacar arma, grip+gatillo izquierdo = prótesis). En cada gesto, una frase corta (p. ej. "MANO A LA CADERA + GRIP: SACAR PISTOLA").
4. Captura de la guía (simulador o visor) en docs/capturas/_tareas/VR31/.
**Cómo comprobar:** en el visor, la guía se lee bien y las etiquetas coinciden con los botones reales.
# VR32 · Agregar los videos reales de las cinemáticas (reemplazar placeholders de T10)
**Lo que pidió Tony:** ya hay videos para las cinemáticas; usarlos para reemplazar los placeholders de CinematicaPendiente (T10, docs/tarea-historia-en-juego.md). Vale para PC y VR (en VR se ven en la pantalla grande de VR15).

**Ubicación:** `C:\Users\Angel Perez\Desktop\videojuego\trailer-cybercity-referencias\`
- `Videos_Cinematicas\` → TODOS los videos listos, en orden (01_ … 18_). Tomar los videos de aquí.
- `Cinematicas\<CXX_nombre>\` → una carpeta por cinemática con frames PNG, referencias, PROMPT.txt (qué pasa) y videos. Desde C04, los videos de cada cinemática están en su subcarpeta `Videos\`.
- `Cinematicas\_zips\` → respaldos, ignorar.

**Cómo están hechos:**
- C00_Logo, C01_Recuerdo_Campo (T01, T02), C02_Maletin_A_Llegan y C03_Maletin_B_Lo_roba: un video completo por cinemática.
- Desde C04, cada cinemática es una secuencia de clips cortos (uno por frame) en orden 01_, 02_, 03_…: reproducirlos uno tras otro SIN cortes negros, como una sola cinemática (precargar el siguiente con dos VideoPlayer alternados).
- Los videos traen solo efectos de sonido, sin voces. NO agregar voces, marco NOVA ni glitch (eso va en edición aparte).

**Qué video va en cada cinemática del juego:**
| Cinemática del juego | Videos, en orden |
|---|---|
| C02 EL MALETÍN (paso 4) | C02 + C03 + C04_Huida (F08–F11) + C05_Cuarto_Contenido (F12–F15) |
| C03 LA DECISIÓN (paso 6) | C06_Ranura_A_El_grito (G01–G03) + C07_Ranura_B_Regresa (G04–G06). C07 aún sin video: placeholder solo en esa parte |
| C04 EL SUERO (paso 8) | C08_Suero (I01–I03). Aún sin video: dejar placeholder |
| Pantalla de inicio / intro | C00_Logo |
| C01 EL RECUERDO (flashback del campo) | C01_Recuerdo_Campo |

**Qué hacer:**
1. Copiar los videos usados a `Assets/Cinematicas/<CXX>/` (Git LFS para .mp4, revisar .gitattributes) y ponerlos en cada `DatosCinematica` (permitir una LISTA de VideoClips en orden; si hoy solo admite uno, ampliarlo).
2. VideoPlayer en orden, fundidos cortos al entrar y salir, audio por el grupo de mezcla correcto (Efectos o Música, no Voces).
3. Saltar con el botón de saltar (ENTER en PC, B en VR), como hoy.
4. Donde no hay video (C07, C08): placeholder actual. Si aparecen clips nuevos en `Cinematicas\C07_Ranura_B_Regresa\Videos\` o `Cinematicas\C08_Suero\Videos\`, usarlos con el mismo esquema.
5. Ojo con VR24: la ranura ya no es paso jugable; C03 LA DECISIÓN se ve justo antes de aparecer en tu punto para pelear con los 3 SKULLS.
6. Probar en Play el paso 4 (EL MALETÍN) de principio a fin: anotar en el reporte cuánto dura y si algún clip se corta, se traba o hay negro entre clips.

**Cómo comprobar:** partida nueva: C01 con video, C02 EL MALETÍN completo sin cortes, C03 con video + placeholder de C07, C04 placeholder. Se saltan con ENTER/B.
**Estado:** PENDIENTE
# VR34 · Ver tu cuerpo completo en VR (manos, brazos, torso y piernas)
**Estado:** EN ESPERA. NO hacer hasta que Tony diga.
**Lo que quiere Tony:** verse el cuerpo y las manos del protagonista (PJ01) en VR: sujetar el arma con su mano, ver las prótesis al activarlas (brazos y piernas), todo el cuerpo.

## Qué hacer (en este orden; cada fase se commitea y se puede probar sola)
### Fase 1 · Manos y antebrazos
- Usar el modelo real de PJ01 (no primitivos ni manos genéricas). Las manos de PJ01 siguen a los controles (pose grip de OpenXR) con un offset para que la palma quede donde está la mano real.
- Dedos: grip cierra los tres dedos inferiores, gatillo el índice, el pulgar baja al tocar A/B/X/Y (poses hechas con los huesos del avatar, mezcladas por el valor analógico; sin animar a mano: dos poses abierta/cerrada por dedo).
- La pistola y las armas se sujetan en la mano de PJ01 (punto de agarre de cada arma), el cargador en la izquierda al recargar (VR27).
- Prótesis de brazos visibles y con su brillo al activarse.
- Ocultar los controles de los Starter Assets cuando se ven las manos.

### Fase 2 · Cuerpo completo con IK
- Paquete Animation Rigging (de Unity, gratis). Two Bone IK en los dos brazos (manos a los controles, codos con pista hacia abajo y afuera), Multi-Aim/Damped en cuello y columna siguiendo a la cabeza.
- Calibración por estatura: PJ01 mide 1.84 m (ojos a 1.72). Escalar el rig a la estatura medida al recentrar para que los brazos no se estiren.
- La cabeza de PJ01 se oculta solo para la cámara (sigue dando sombra); cuello y hombros recortados para no ver por dentro.
- Torso gira con la cabeza a partir de ~45°, con suavizado (no al instante).

### Fase 3 · Piernas
- Las piernas usan los clips que ya existen (PJ01_Caminar 1.62, PJ01_Trote 4.53, idle, salto, caída), con la velocidad del stick; en Idle, IK de pies al suelo (Two Bone IK + raycast) para que no floten en escalones.
- Al mirar abajo se ven las piernas; prótesis de piernas visibles con su brillo al activarse.
- Agachado real: si la cabeza baja, las rodillas se doblan (IK de cadera).

## Reglas
- Nada animado a mano ni hecho con primitivos; no editar FBX ni clips de Blender.
- Si un brazo se ve torcido o estirado sin solución en 20 min, quedarse en la Fase 1 (manos y antebrazos) y marcar el resto NO QUEDÓ.
- Opción en Opciones de VR: "CUERPO: COMPLETO / SOLO MANOS".

## Cómo comprobar (Tony con el visor)
1. Mirar tus manos: se ven las de PJ01, los dedos se cierran con grip y gatillo.
2. Sacar la pistola: queda en tu mano.
3. Activar prótesis de brazos: brillan en tus brazos.
4. Mirar abajo caminando y quieto: piernas sin patinar ni flotar; prótesis de piernas brillan.
5. Mover los brazos en todas direcciones: codos sin torcerse raro, brazos sin estirarse.
