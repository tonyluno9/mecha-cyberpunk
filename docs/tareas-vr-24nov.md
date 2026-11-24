# Tareas VR · 24-nov madrugada (prueba de Tony con el visor)
Le encantó: moto, subir/bajar de carro y moto (transiciones), controles del carro, pistola (sacarla y dispararla), revólver, lanzagranadas, rifle de plasma, campo de tiro, menú de pausa y máquina expendedora (modelo de selección para todos los menús), interfaces sin sombras.
VR34 (cuerpo completo) CANCELADA: solo manos (VR40).

| # | Tarea | Prioridad | Estado |
|---|---|---|---|
| VR44 | Menús: botón resaltado, láser visible y selección fácil (como la máquina) | 1 | PENDIENTE |
| VR46 | Rueda de armas: arreglar, sin marco, mover con gatillo izquierdo, consumibles | 1 | PENDIENTE |
| VR53 | Después de Tino llegan policías → patrullas → NOVA (modo libre) | 1 | PENDIENTE |
| VR47 | Volver a hablar con Tino; sin aviso "eres libre" | 1 | PENDIENTE |
| VR37 | Golpes desde cualquier ángulo | 1 | PENDIENTE |
| VR36 | Cinemáticas con fondo negro, de corrido y con audio | 1 | PENDIENTE |
| VR38 | Giro con stick marea: más lento y algo de viñeta | 1 | PENDIENTE |
| VR39 | Sonido de caída de cuerpos en bucle | 1 | PENDIENTE |
| VR51 | Mira con gatillo izquierdo, zoom del sniper, trayectoria, carga del plasma | 2 | PENDIENTE |
| VR52 | Recarga solo manual | 2 | PENDIENTE |
| VR42 | El Doc/Tino tapan su pantalla | 2 | PENDIENTE |
| VR43 | No se ve el cuerpo en el Doc | 2 | PENDIENTE |
| VR45 | Tutorial de prótesis con imagen + gesto de piernas más fácil | 2 | PENDIENTE |
| VR48 | Carros: vista desde el cristal, sin carrocería | 2 | PENDIENTE |
| VR50 | Campo de tiro: objetivos reales, nadie atraviesa la sala | 2 | PENDIENTE |
| VR41 | Ruta: volver a la línea de antes | 2 | PENDIENTE |
| VR54 | Quitar textos sobre los enemigos | 2 | PENDIENTE |
| VR31b | Guía de controles nueva (16 páginas), sin rueda de prótesis, gesto de visión | 1 | PENDIENTE |
| VR62 | Tutorial que se presenta en el momento justo + APRENDIDO | 1 | PENDIENTE |
| VR40 | Manos nuevas (rig listo en tony/blender-24nov; tarea T-manos-vr-24nov) | 2 | PENDIENTE |
| VR55 | Pantallas de carga con frames | — | EN ESPERA (carpeta de frames) |
| VR56 | Imágenes de gestos más claras | — | EN ESPERA (arte) |
| VR23 | Cosas flotando | — | EN ESPERA (dónde) |
# VR31b · Guía de controles nueva (16 páginas) + cambios de mecánica
**Imágenes:** `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\ui\controles-vr\` → copiar a wtVR `Assets/Arte/UI/ControlesVR/` (Sprite 2D and UI, sin compresión fuerte, mip maps apagados).
**NO usar:** todo lo que termine en `_v1`; `VR_Gesto_RuedaProtesis_descartada.png`; las versiones SIN `_v2` de SacarPistola, ArmaLarga, Recargar, ProtesisPiernas y ProtesisBrazos.

## Cambios de mecánica
1. **Rueda de prótesis: SE QUITA** en VR (ni grip + gatillo izquierdo, ni rueda). Prótesis solo con gestos.
2. **Prótesis de visión (gesto nuevo):** los DOS grips apretados + las dos manos a la cabeza (a los lados) ~0.5 s → activa la visión. Vibración y sonido como las otras.
3. **Rueda de armas (lo último que dijo Tony):** mantener X la abre; con el GRIP izquierdo apretado, mover la mano izquierda recorre la rueda (el arma bajo el selector se resalta); al SOLTAR el grip se elige y se equipa. (El chat de arte la dibujó con el gatillo izquierdo; manda lo de Tony: grip. Si la imagen muestra el gatillo, el texto dice GRIP.)

## Páginas (en orden; SIGUIENTE / ANTERIOR / CERRAR con láser y resaltado de VR44)
1 VR_Controles_Botones · 2 VR_Gesto_MoverseGirar · 3 VR_Gesto_Interactuar · 4 VR_Gesto_SacarPistola_v2 · 5 VR_Gesto_ArmaLarga_v2 · 6 VR_Gesto_Disparar · 7 VR_Gesto_Recargar_v2 · 8 VR_Gesto_RuedaArmas · 9 VR_Gesto_Granada · 10 VR_Gesto_Golpe · 11 VR_Gesto_AgarrarLanzar · 12 VR_Gesto_ProtesisPiernas_v2 · 13 VR_Gesto_ProtesisBrazos_v2 · 14 VR_Gesto_ProtesisVision · 15 VR_Gesto_Recentrar · 16 VR_Gesto_Manejar (VR21 ya está: incluirla).
- Las imágenes no traen texto: una frase corta por página, estilo HUD (letra delgada, cian). Ej.: "MANO A LA CADERA + GRIP: SACAR PISTOLA".
- Actualizar la tabla de controles y TODOS los avisos y textos de ayuda: quitar la rueda de prótesis, agregar el gesto de visión y la rueda de armas con grip.
- Menú de Pausa → CONTROLES muestra las 16 páginas.
**Estado:** PENDIENTE
# VR36 · Cinemáticas: fondo negro total, de corrido y con audio
**Lo que vio Tony:** durante las cinemáticas el fondo deja ver el juego detrás de la pantalla.
**Qué hacer:** mientras dura una cinemática, todo alrededor de la pantalla grande es negro (como un cine a oscuras; nada del juego visible, ni HUD). Todas las cinemáticas seguidas hasta terminar, sin cortes. Con su audio completo: efectos de los videos + la narración de Tony (T-voces, docs/guion-narracion-cinematicas.md) + música de piano (T-musica). Vale para VR y PC (en PC ya es pantalla completa).
**Comprobar:** partida nueva en el visor: solo negro y la pantalla; se oye la narración y los efectos.
**Estado:** PENDIENTE
# VR37 · Golpes desde cualquier ángulo
**Lo que vio Tony:** cuesta pegarles; casi solo pega de lado o si se para derecho justo enfrente.
**Qué hacer:** que el golpe cuente desde cualquier dirección y altura (gancho, recto, de arriba, al cuerpo o a la cabeza): esfera del nudillo un poco más grande (~0.13 m) contra toda la cápsula/hitboxes del enemigo de pies a cabeza; la velocidad cuenta en cualquier dirección (magnitud, no solo hacia adelante); si la mano entra en el enemigo, cuenta. Revisar que los SKULLS no queden fuera de alcance.
**Comprobar:** pegar de frente, de lado, de arriba y al cuerpo: todos bajan vida.
**Estado:** PENDIENTE
# VR38 · Girar con el stick derecho marea demasiado
**Lo que dijo Tony:** el giro con el stick derecho marea muchísimo; prefiere girar con su cabeza y moverse. Pide 'un poquito de margen, que se vea un poquito'.
**Qué hacer:** al girar con el stick vuelve una viñeta un poco más visible que la BAJA actual (solo al girar, entra y sale rápido); giro suave más lento por defecto (~60°/s) con zona muerta y aceleración suave; caminar sigue sin viñeta fuerte. Todo ajustable en Opciones.
**Comprobar:** girar con el stick ya no marea.
**Estado:** PENDIENTE
# VR39 · El sonido de caída de los cuerpos se repite en bucle
**Lo que vio Tony:** cuando un enemigo muerto cae al suelo o sobre una superficie, el sonido de caída se repite sin parar.
**Qué hacer:** el sonido de caída/impacto del cuerpo suena una sola vez por cuerpo (o máx. una vez por choque fuerte, con enfriamiento y velocidad mínima); cuando el ragdoll está quieto, nunca más. Revisar todos los sonidos de impacto de ragdoll y objetos. Vale para PC y VR.
**Comprobar:** matar 5 enemigos: cada caída suena una vez.
**Estado:** PENDIENTE
# VR40 · Manos nuevas (se ven horribles)
**Lo que dijo Tony:** las manos de VR se ven horribles; hay que generar unas nuevas. Ya no se hace el cuerpo completo (VR34 cancelada): solo las manos.
**Qué hacer:** EN ESPERA del modelo: el chat de arte genera la imagen, Tony la pasa por Tripo. Cuando esté el modelo (zip en imageassets/modelos-nuevos/), riggearlo/usarlo como manos de VR: puño, abierta, grip y gatillo; prótesis de brazos visibles y con brillo.
**Modelo recibido (24-nov):** imageassets/modelos-nuevos/nuevos-24nov/MN01_ManosPJ01.zip (carpeta principal). Rig, poses y animación de prueba: chat de Blender → Assets/Arte/Personajes/PJ01/ManosVR/ + doc docs/tareas/pendientes/T-manos-vr-24nov.md.
**Rig listo (24-nov 02:10):** rama tony/blender-24nov (sin merge a main). Hacer docs/tareas/pendientes/T-manos-vr-24nov.md (en esa rama). Poses no revisadas a la vista: Granada, Cargador_L, AgarreArmaLarga_L.
**Estado:** PENDIENTE
# VR41 · Ruta: volver a la línea de antes
**Lo que dijo Tony:** prefiere por mucho la línea de ruta que había antes a los chevrones/puntitos nuevos.
**Qué hacer:** en VR, la ruta al objetivo es la línea de antes (la que se quitó en PC); quitar los chevrones de VR30. Opción RUTA SÍ/NO se queda. En PC se queda como está ahora.
**Comprobar:** con VE CON EL DOC se ve la línea de antes en el visor.
**Estado:** PENDIENTE
# VR42 · El Doc (y Tino) tapan su pantalla
**Lo que vio Tony:** al hablar con el Doc, el Doc queda enfrente del panel y no deja ver.
**Qué hacer:** mientras está abierta la pantalla de un NPC (Doc, Tino, Canepa, cualquiera), ese NPC y lo que quede entre tu cabeza y el panel se ocultan; al cerrar vuelven.
**Comprobar:** hablar con el Doc y con Tino: nada tapa el panel.
**Estado:** PENDIENTE
# VR43 · No se ve el cuerpo en la pantalla del Doc
**Lo que vio Tony:** la interfaz del Doc se ve perfecta, pero el cuerpo (la vista 3D) no aparece.
**Qué hacer:** la vista 3D del cuerpo (y cualquier vista 3D de menús: arma en Tino, etc.) se dibuja dentro del panel de VR (RenderTexture de su cámara al panel).
**Comprobar:** en el Doc se ve el cuerpo y cambia al elegir prótesis.
**Estado:** PENDIENTE
# VR44 · Todos los menús: botón resaltado, láser siempre visible y selección fácil
**Lo que vio Tony:** en Tino no se resalta el botón al que apuntas, el láser atraviesa la interfaz y es dificilísimo atinar; no pudo comprar. La máquina expendedora y la pausa sí lo hacen bien: ESE es el modelo para todos.
**Qué hacer:** en TODAS las interfaces de VR: (1) el botón bajo el láser se resalta igual que en la máquina expendedora; (2) el láser siempre se ve y se detiene en el panel (no lo atraviesa), con punto en donde toca; (3) selección fácil: zona de clic más grande y magnetismo al botón más cercano; el gatillo hace clic solo en lo resaltado; vibración leve al pasar de botón. Revisar Tino (tienda, banco, mejoras), Doc, campo de tiro, Canepa, opciones, guía.
**Comprobar:** comprar en Tino sin batallar.
**Estado:** PENDIENTE
# VR45 · Tutorial de prótesis con imagen + gestos más fáciles
**Lo que vio Tony:** sale un aviso de solo texto ('manos en los muslos, los dos grips…'); no sale la imagen explicativa. 'Mantén grip + gatillo izquierdo' para el menú de prótesis no hace nada. El gesto de piernas cuesta (lo logró: manos abajo, ~1 s, los dos grips).
**Qué hacer:** (1) el aviso del tutorial muestra la imagen del gesto (VR31; luego las nuevas de VR56) con su texto. (2) Arreglar la rueda de prótesis con grip + gatillo izquierdo (o quitarla y el texto). (3) Gesto de piernas más tolerante: manos por debajo de la cintura (de la cadera a la rodilla), cualquier separación, grips 0.5 s. Brazos: pose T aproximada.
**Comprobar:** el tutorial muestra la imagen; las piernas se activan a la primera.
**Cambio 24-nov 02:29:** la rueda de prótesis YA NO EXISTE. Nuevo gesto (ver VR31b: dos grips + manos a la cabeza ~0.5 s = visión; la rueda de prótesis se quita): las dos manos a la cabeza con los dos grips y los dos gatillos → se activa la visión. Esperar el reporte del chat de arte antes de hacerla.
**Estado:** PENDIENTE
# VR46 · Rueda de armas: arreglar, sin marco, mover solo con gatillo izquierdo
**Lo que vio Tony:** la rueda está bugueadísima, se mueve como loca, no deja elegir la pistola; tiene un marco/pantalla alrededor que no le gusta. Las papitas y consumibles se deberían usar desde la rueda.
**Qué hacer (final, 24-nov 02:33):** mantener X abre la rueda (sin marco ni fondo, solo la rueda flotando); el selector NO se mueve hasta que aprietas el GRIP izquierdo; con el grip apretado mueves la mano izquierda y el arma bajo el selector se resalta; al SOLTAR el grip se elige y se equipa. Incluye armas, granadas y consumibles (papitas, bebida, café: al elegirlos se comen/usan). Imagen de cómo se usa en la guía.
**Comprobar:** cambiar a cada arma y comer unas papitas sin fallar.
**Estado:** PENDIENTE
# VR47 · Tino: volver a hablar con él y sin aviso de 'eres libre'
**Lo que vio Tony:** después de comprar la mejora ya no lo deja volver a hablar con Tino. Sale un aviso tipo 'eres libre' que no le gusta.
**Qué hacer:** siempre se puede volver a hablar con Tino (tienda, banco, mejoras). Quitar el aviso 'ERES LIBRE' (también en T-historia-libre): la historia termina con la cinemática de Tino dándote el arma, sin avisos; después es mundo libre.
**Comprobar:** hablar con Tino varias veces; ningún aviso de 'eres libre'.
**Estado:** PENDIENTE
# VR48 · Carros: vista desde el cristal, sin carrocería visible
**Lo que dijo Tony:** las transiciones de subir y bajar (carro y moto) y los controles están perfectos; la moto perfecta. Pero en el carro se ve la estructura/carrocería: debe verse en primera persona, desde el cristal.
**Qué hacer:** en VR, al subirte a un carro, la carrocería e interior se ocultan para la cámara (render off o capa que la cámara VR no dibuja) y quedas viendo desde el parabrisas; se conservan todos los colliders/hitbox del carro. La moto no se toca.
**Comprobar:** subirte a 3 carros distintos: vista limpia, choques normales.
**Estado:** PENDIENTE
# VR50 · Campo de tiro: objetivos reales y nadie atraviesa la sala
**Lo que vio Tony:** el campo de tiro está hermoso, pero los objetivos salen holográficos (deben salir con su modelo real; un destello holográfico al aparecer está bien). SKULLS y otros NPCs atraviesan el campo de tiro.
**Qué hacer:** objetivos con el modelo y materiales reales del enemigo (SKULLS, policía, NOVA según configuración), con el destello holográfico solo al aparecer. Ningún NPC, peatón, carro ni pandillero puede entrar o pasar por la sala (quitar el NavMesh de la zona, colliders, o mover la sala a un lugar aislado). Menú de la terminal con el resaltado de VR44.
**Comprobar:** en el campo de tiro solo están tus objetivos reales; nadie cruza.
**Estado:** PENDIENTE
# VR51 · Apuntar con el gatillo izquierdo (mira) y armas a dos manos
**Lo que dijo Tony:** pistolas y revólver perfectos a una mano. Para el resto, el gatillo izquierdo (control izquierdo) apunta:
- Subfusil y fusil: con el gatillo izquierdo apretado sale una mira (como la del francotirador pero sin zoom) y apuntas; el gatillo derecho dispara. Mejor con las dos manos (la izquierda sujeta el arma, da realismo).
- Francotirador: igual, y con el stick derecho haces zoom en la mira.
- Lanzagranadas: con el gatillo izquierdo sale la guía de trayectoria de dónde cae; el gatillo derecho dispara.
- Rifle de plasma: el gatillo izquierdo empieza a cargar; al llegar al máximo suena un aviso de carga completa; el gatillo derecho dispara.
**Qué hacer:** eso, con la mano izquierda sujetando el arma larga cuando está cerca (agarre a dos manos opcional). Mira holográfica en el arma (no en pantalla).
**Comprobar:** cada arma en el campo de tiro.
**Estado:** PENDIENTE
# VR52 · Recarga solo manual
**Lo que vio Tony:** al quedarse sin balas recarga solo; no le gusta. A fuerzas hay que apretar A y recargar con el gesto.
**Qué hacer:** en VR se quita la recarga automática: sin balas, el arma hace clic vacío y aparece un aviso discreto 'A: RECARGAR'.
**Comprobar:** vaciar el cargador: no recarga solo.
**Estado:** PENDIENTE
# VR53 · Después de Tino empiezan a llegar policías (modo libre)
**Lo que dijo Tony:** al salir de la tienda de Tino ya es libre y no ve policías. Deben empezar a venir: primero unos policías, los vences, llegan las patrullas/camiones, luego NOVA, y así hasta que te maten. Va con T-historia-libre-23nov (actualizada).
**Qué hacer:** ver docs/tareas/pendientes/T-historia-libre-23nov.md. La escalada empieza sola al salir de Tino (sin aviso). Vale para PC y VR.
**Estado:** PENDIENTE
# VR54 · Quitar los diálogos de texto sobre los enemigos
**Lo que dijo Tony:** fuera los diálogos/textos que aparecen arriba de los enemigos.
**Qué hacer:** quitar los globos/subtítulos de texto sobre la cabeza de enemigos y NPCs (las voces se quedan). Vale para PC y VR.
**Comprobar:** pelear con SKULLS y policía: se oyen, sin texto encima.
**Estado:** PENDIENTE
# VR55 · Pantallas de carga con frames del tráiler (antes E1)
**Lo que dijo Tony:** agregar a las pantallas de carga los frames que ya se generaron (del tráiler/cinemáticas).
**Qué hacer:** EN ESPERA de que Tony diga la carpeta de los frames. Elegir los mejores, upscaler si hace falta (≥1920x1080), meterlos en la pantalla de carga minimalista (sin deformar, con el logo). En VR se ven en la pantalla grande.
**Estado:** EN ESPERA (ubicación de los frames)
# VR56 · Imágenes de gestos más claras (chat de arte)
**Lo que dijo Tony:** las imágenes de los gestos de prótesis no se entienden bien. Piernas: apretar los grips y bajar las manos a los muslos. Brazos: apretar y ponerse en pose T.
**Qué hacer:** EN ESPERA del chat de arte (una imagen por gesto). Cuando estén, reemplazar las de VR31 en la guía y en el aviso del tutorial (VR45).
**Estado:** EN ESPERA (arte)
# VR62 · Tutorial que se va presentando en el momento justo
**Lo que pidió Tony:** las imágenes de la guía se presentan grandes pero sin tapar todo, para que puedas hacer el gesto mientras te explica. Texto que explica + botón "APRENDIDO". Cada cosa se enseña cuando la vas a usar (el campo de tiro explica las armas; al tener prótesis de brazos, agarrar y lanzar). En el menú, una opción para ver todos los controles.
## Cómo se ve
- Tarjeta grande (imagen + frase corta + 1–2 líneas de explicación) flotando un poco abajo y a un lado de la vista (~55% del campo, no al centro), semitransparente al fondo, sin tapar las manos.
- Se cierra con "APRENDIDO" (láser y gatillo) o sola cuando haces el gesto correctamente (con un ✓ y vibración). El juego NO se pausa (para poder hacer el gesto), salvo en peleas, donde sale antes de que empiecen.
- Cada tarjeta sale una sola vez por partida guardada; Pausa → CONTROLES las muestra todas.
## Cuándo sale cada una
1. Al aparecer en tu punto, antes de los 3 SKULLS: MOVERSE Y GIRAR, RECENTRAR, GOLPE (pelea a puños).
2. Al acercarte al Doc: INTERACTUAR (B).
3. Al instalar cada prótesis con el Doc: PIERNAS, BRAZOS o VISIÓN (la que se instale). Con BRAZOS, además AGARRAR Y LANZAR.
4. Al comprar el arma con Tino: SACAR PISTOLA.
5. En el campo de tiro, en orden y una por una mientras practicas: DISPARAR → RECARGAR → ARMA LARGA → RUEDA DE ARMAS → GRANADA (cada una al terminar la anterior o con APRENDIDO).
6. Al acercarte por primera vez a un carro: MANEJAR.
- Si algo se consigue antes (p. ej. un arma), su tarjeta sale la primera vez que la usas.
**Comprobar (Tony):** partida nueva: cada tarjeta sale en su momento, deja hacer el gesto, se cierra con APRENDIDO o al hacerlo.
**Estado:** PENDIENTE
