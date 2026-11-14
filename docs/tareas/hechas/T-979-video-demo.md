# T-979: video de la demo (2 a 3 min, 1080p)

**Hecho.** Pedido de Tony (13-nov): después de los builds, grabar un video de 2 a 3 minutos de la demo: inicio, ciudad de noche, asalto y persecución,
Doc y clínica, banco de Tino, casino y Nova. En `Entrega/CyberCity_demo.mp4`, en 1080p y fuera de git.

## Resultado
`Entrega/CyberCity_demo.mp4`: **2:01**, 1920×1080, 30 cuadros/s, H.264 (crf 18) + AAC 192 kb/s, 123 MB. `Entrega/` va en .gitignore.

Orden: título CYBERFIX y NUEVA PARTIDA → creación del personaje → despierta en su cuarto → la calle principal de noche (toma de cámara sin HUD)
→ sale de casa → el asalto (cinemática: la víctima y el carro) → la persecución (cámara detrás del asaltante, con patrullas) → I+D+I y EL DOC en su clínica
→ el banco de Tino (pistola en holograma, piezas y mejoras) y Tino → la fachada VivaCoin → el casino por dentro (mesas, meseras, barra)
→ EL HACKER en Neón Alto: "NOVA arma a los SNAKES" → CONTINUARÁ. Calidad ALTO (`-calidad Alto`).

## Cómo se grabó
No se usó el modo video: la demo final (DEMO_FINAL) lo quita, y no está Unity Recorder. Lo juega el mismo bot del recorrido:
- `Tests/PlayMode/Demo/RecorridoDemoVideoT979.cs`: prueba explícita `RecorridoDemoPruebas.VideoDemoT979` (parte de la clase, para usar al bot). Solo graba los tramos que se ven bien.
- `GrabadorVideoT979`: graba la vista de juego con la interfaz en tiempo real (lectura de GPU sin bloquear → ffmpeg), 30 cuadros/s constantes. El sonido sale del AudioListener activo (`OidoGrabadorT979`).
- Necesita el editor **con ventana** (en batchmode no hay pantalla que capturar) y ffmpeg:
  `T979_FFMPEG=C:/ffmpeg/.../ffmpeg.exe Unity.exe -projectPath . -runTests -testPlatform PlayMode -assemblyNames MechaCyberpunk.Tests.PlayMode.Demo -testFilter "RecorridoDemoPruebas.VideoDemoT979" -calidad Alto`
  Deja `Logs/video-t979/video.mp4` y `audio.wav`; el final se arma con ffmpeg (audio normalizado con loudnorm, fundidos de entrada y salida).
- Se revisó con hojas de cuadros cada 3 s. Se grabó 3 veces: la 1.ª salió de cabeza y sin sonido; la 2.ª tenía 15 s del jugador frente a una puerta
  durante la pelea del asalto y tomas negras al llegar al Doc. En la 3.ª la pelea no se graba y el Doc se toma de cerca.

## Lo que hay que saber
- La pantalla de implantes de la clínica (MostradorClinica) no abre en ese momento de la historia: sale la clínica con EL DOC, no el menú de implantes.
- La pelea con el asaltante no sale en el video (el bot la hace pegado a una puerta).
- El audio es el del juego (ambiente y efectos; la música está en 0 por defecto). Se oye bajo en los tramos de cámara.

## Arreglo de paso en el recorrido
`RecorridoDemoPruebas` fallaba al empezar: la pasarela PS01 (T-910) tiene MeshCollider con malla sin lectura y el NavMesh que hornea el bot
lo marcaba como error. Ahora esas mallas se dejan fuera del horneado (la pasarela va por encima de la calle). Con eso `DemoDePrincipioAFin` pasa:
248 s, 0 bugs, 0 muertes. `NavMesh_LugaresDeLaHistoria_TienenCamino` sigue fallando en "casa → DOC" (el punto del DOC queda a 7 m del NavMesh);
falla igual sin la pasarela y el recorrido real sí llega al Doc. Anotado, no se tocó.
