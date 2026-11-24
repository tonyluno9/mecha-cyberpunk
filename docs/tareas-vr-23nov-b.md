# Tareas VR · 23-nov noche (después de la primera prueba de Tony con el visor, build VR3)
Rama vr/quest3, carpeta wtVR. Orden = prioridad. Build final en Builds\VR4.

| # | Tarea | Prioridad | Estado |
|---|---|---|---|
| VR13 | GRAVE: el ojo derecho no ve varios modelos | 1 | PENDIENTE |
| VR14 | Interfaz del juego real como visor (no en la muñeca), minimapa | 1 | PENDIENTE |
| VR15 | Inicio y cinemáticas en una sola pantalla grande | 1 | PENDIENTE |
| VR17 | Láser demasiado arriba | 2 | PENDIENTE |
| VR18 | Giro suave por defecto | 2 | PENDIENTE |
| VR19 | Nuevo reparto de botones | 2 | PENDIENTE |
| VR16 | Quitar SKULLS que molestan al inicio (VR) | 2 | PENDIENTE |
| VR20 | Prótesis con gestos + guía de controles | 3 | PENDIENTE |
| VR22 | Desconexión del visor | 3 | PENDIENTE |
| VR21 | Manejar carros y motos | 4 | PENDIENTE |
| VR23 | Cosas flotando | 4 | PENDIENTE |
# VR13 · GRAVE: el ojo derecho no ve varios modelos
**Lo que vio Tony:** con el ojo izquierdo los modelos se ven; con el derecho varios no se ven (se ve a través): edificios altos con anuncios de neón junto al mercado, el bar, el cartel de tacos. Los charcos se ven sin su textura correcta. Parecen "objetos que desaparecen".
**Causa probable:** Single Pass Instanced de OpenXR con shaders propios (MechaCyberpunk/*, charcos, neón, letreros, decals, partículas) que no tienen las macros de estéreo (UNITY_VERTEX_INPUT_INSTANCE_ID, UNITY_VERTEX_OUTPUT_STEREO, UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX, texturas con TEXTURE2D_X donde aplique), o materiales/Shader Graph viejos.
**Qué hacer:**
1. Arreglo inmediato: en OpenXR (Windows) pon Render Mode = Multi-pass. Con eso los dos ojos dibujan todo.
2. Si sobra tiempo al final: agrega las macros de estéreo a los shaders propios y vuelve a Single Pass Instanced, solo si todo se ve igual en los dos ojos.
3. Revisa también los charcos (T11_Charco, reflejos) y cualquier efecto de pantalla que lea texturas de cámara.
**Cómo comprobar:** en el build, con el visor, cerrar un ojo y luego el otro frente al bar, los edificios de neón del mercado y el cartel de tacos: se ven igual con los dos.
**Estado:** PENDIENTE
# VR14 · La interfaz del juego real como visor (no en la muñeca)
**Lo que vio Tony:** no ve la interfaz del juego: ni dinero, ni minimapa, ni guía de misión/objetivo, ni avisos. Lo único que sale es "VIDA 150 / DINERO 500" en la muñeca, que parece una interfaz antiquísima. No le gusta en la muñeca. Pensó que cargaba una versión vieja del juego.
**Qué quiere:** conservar la interfaz final del juego tal como es en PC, como si llevara un visor/casco: pegada a la cabeza, siempre visible.
**Qué hacer:**
1. Quita el HUD de la muñeca (HudMunecaVR) en VR.
2. El HUD real de PC (todos sus Canvas de pantalla: vida, barras, dinero, minimapa, objetivo/guía de misión, arma y munición, prótesis, avisos y tutoriales, subtítulos, estrellas) se muestra completo en un panel curvo pegado a la cabeza (head-locked con un leve retraso suave), a ~1.2–1.5 m, ocupando ~70° de ancho para que las esquinas queden en la visión periférica cómoda. Mismo diseño, mismos colores, mismas posiciones relativas.
3. El minimapa debe verse y funcionar.
4. Confirma que el build trae el último main (HUD final, T80–T86) y no algo viejo; anota en el reporte el commit de main que lleva.
**Cómo comprobar:** en el visor, al mirar a cualquier lado, el HUD final del juego sigue en su lugar y se lee: dinero, vida, minimapa, objetivo.
**Estado:** PENDIENTE
# VR15 · Inicio y cinemáticas: una sola pantalla grande, inmersiva
**Lo que vio Tony:** al iniciar salen tres pantallas separadas: una con la cámara del fondo, otra con el video del logo y otra con el menú. Luego la pantalla de la ciudad arriba y el menú abajo; al dar Nueva partida, la carga abajo y la ciudad arriba. Las cinemáticas salen en un recuadro chico, no a pantalla completa.
**Qué hacer:**
1. Una sola pantalla grande curva frente a él (~100–110° de ancho, que llene casi toda la vista, a ~3–4 m), y todo pasa EN ella, en orden, como en PC: primero el video del logo; después el recorrido de calles con el menú encima (igual que el menú de PC); la carga también ahí. Nada de paneles separados arriba y abajo.
2. Cinemáticas (cámara y video) en esa misma pantalla grande inmersiva, no en un recuadro chico.
3. Cuarto oscuro quieto; la cabeza nunca se mueve por animación.
**Cómo comprobar:** al abrir el juego solo hay una pantalla grande; logo → menú sobre la ciudad → carga, todo en la misma. Las cinemáticas llenan casi toda la vista.
**Estado:** PENDIENTE
# VR16 · Quitar la escena de los SKULLS que te molestan al inicio (en VR)
**Lo que vio Tony:** al empezar, los SKULLS se le acercan, le buscan pleito, lo empujan y sale "NO PUEDES CON ELLOS". No puede hacer nada. No le gusta.
**Qué hacer:** en VR se salta por completo el paso "Lo molestan": los SKULLS no se acercan, no lo empujan, no hay aviso. La historia sigue al siguiente paso como si ese ya hubiera pasado (que no se rompan candados ni objetivos). No borrar contenido: solo saltarlo en ModoVR.
**Cómo comprobar:** partida nueva en el visor: sin SKULLS encima; aparece directo el siguiente objetivo.
**Estado:** PENDIENTE
# VR17 · El láser/puntero sale demasiado arriba
**Lo que vio Tony:** el puntero del control aparece demasiado arriba; como mal calibrado.
**Qué hacer:** el rayo debe salir de la punta del control y apuntar a donde apunta el control, usando la pose "aim" (pointer) de OpenXR del Quest Touch Plus, no la "grip". Igual para el cañón de la pistola. Revisa offsets y rotaciones del rayo y del punto final.
**Cómo comprobar:** apuntar a un botón del menú con el control: el punto cae justo donde apuntas.
**Estado:** PENDIENTE
# VR18 · Giro suave por defecto
**Lo que vio Tony:** el stick derecho gira la vista "abruptamente" en saltos; no entendía cómo girar. Quiere que gire suave, como si él lo controlara.
**Qué hacer:** GIRO = SUAVE por defecto (velocidad proporcional al stick, máx ~120°/s, configurable en Opciones). El giro por pasos queda como opción. Viñeta suave al girar (se puede apagar).
**Cómo comprobar:** mover el stick derecho a medias gira lento, a fondo gira más rápido, sin saltos.
**Estado:** PENDIENTE
# VR19 · Nuevo reparto de botones
**Lo que pidió Tony:** saltar en el control derecho; la rueda de armas con la mano izquierda.
**Reparto nuevo:**
| Acción | Botón |
|---|---|
| Caminar / correr | Stick izquierdo / clic del stick izquierdo |
| Girar | Stick derecho (suave) |
| Saltar | A (derecho) |
| Interactuar | B (derecho) |
| Disparar | Gatillo derecho |
| Agarrar / lanzar | Grip (cualquier mano) |
| Recargar | X (izquierdo) |
| Rueda de armas | Mantener Y (izquierdo), elegir moviendo la mano izquierda o con el stick izquierdo |
| Rueda de prótesis (Tab) | Mantener grip izquierdo + gatillo izquierdo, o el gesto de VR20 |
| Pausa / Opciones | Menú (izquierdo) |
| Recentrar | Mantener stick derecho abajo 1 s |
**Qué hacer:** aplicar el reparto, actualizar letreros y textos de ayuda, quitar el cambio de arma del stick derecho (ahora es giro).
**Cómo comprobar:** cada botón hace lo de la tabla.
**Estado:** PENDIENTE
# VR20 · Prótesis con gestos + guía de controles
**Lo que dijo Tony:** tocar las piernas activa la prótesis de piernas; abrir los brazos activa la de brazos. No entendía qué hacer. Hay que crear imágenes que expliquen los controles.
**Qué hacer:**
1. Gestos (solo con la prótesis instalada): las dos manos en los muslos con grip ~0.5 s = prótesis de piernas; abrir los dos brazos a los lados con grip ~0.5 s = prótesis de brazos. Vibración y sonido al activar. Igual que activarla desde la rueda.
2. Guía de controles: panel con los modelos 3D reales de los dos controles (de los Starter Assets del XR Interaction Toolkit) y etiquetas con líneas a cada botón (tabla de VR19 + gestos). Sale al empezar partida en VR la primera vez y en Pausa → CONTROLES. Mismo estilo de UI del juego. Nada de dibujos hechos con primitivos.
3. Los tutoriales/avisos de prótesis en VR dicen los botones/gestos de VR, no "Tab".
**Cómo comprobar:** con prótesis, el gesto la activa; la guía se ve y se entiende.
**Estado:** PENDIENTE
# VR21 · Manejar carros y motos en VR
**Lo que vio Tony:** las motos dicen "NO DISPONIBLE EN VR". No le gusta.
**Qué hacer:** subir con B. Vista desde el asiento del conductor (la cabeza dentro de la cabina, a la altura de los ojos). Gatillo derecho acelera, gatillo izquierdo frena/reversa, stick izquierdo dirige (o volante con las dos manos si da tiempo, no obligatorio). B baja. Sin sacudidas de cámara; viñeta suave al girar. Igual para motos.
**Cómo comprobar:** subir a un carro y a una moto, manejar una cuadra y bajar.
**Estado:** PENDIENTE (prioridad baja: al final)
# VR22 · Si se desconecta el visor, el juego no se rompe
**Lo que vio Tony:** se desconectaron las gafas, al volver el juego quedó bugueado y tuvo que cerrarlo.
**Qué hacer:** detectar cuando se pierde la sesión XR o el visor se quita/desconecta: pausar el juego. Al volver: recentrar, quitar la pausa con un botón y seguir donde iba. Si el visor no vuelve, que no se quede colgado.
**Cómo comprobar:** quitarse el visor (o desconectar Link) y volver: el juego sigue bien.
**Estado:** PENDIENTE
# VR23 · Cosas flotando
**Lo que vio Tony:** en VR se notan muchas cosas flotando (como los tinacos y otros objetos).
**Qué hacer:** recorrer el mapa en la escena y bajar al suelo/techo todo objeto decorativo que quede flotando (raycast hacia abajo). Lista en el reporte de qué se movió. No mover nada de Trafico/ (carpeta de Alan) sin anotarlo.
**Cómo comprobar:** en el mercado y calles principales nada flota.
**Estado:** PENDIENTE (si da tiempo)
