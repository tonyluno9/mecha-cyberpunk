# Reporte: pantalla de carga minimalista

Rama `integ/carga` → main f65c1cf. Capturas en `docs/capturas/_carga/`.

## Qué cambió
| Punto | Quedó | Evidencia |
| --- | --- | --- |
| 1. Imagen sin deformar | **Sí.** Ver detalle abajo | `play-1920x1080.png`, `play-1366x768.png`, `play-1280x1024.png`; EditMode `PantallaCargaMinimalistaPruebas` |
| 2. Sin glitch | **Sí.** Ver detalle abajo | capturas |
| 3. Logo | **Sí.** Ver detalle abajo | capturas; prueba `Logo_EsElMismoSpriteDelMenuPrincipal` |
| 4. Sin barra | **Sí.** Ver detalle abajo | capturas |
| 5. Minimalista | **Sí.** En pantalla solo quedan la imagen, el logo, el consejo y los tres puntos | capturas |
| 6. Tiempo y sin negro | **La lógica de carga no cambió:** mismos tiempos mínimo y máximo, misma activación de la escena y mismo desvanecido al final. Al empezar, la primera foto sale sin fundido, así que no hay negro | PlayMode `InicioPruebas` 11/11 (carga asíncrona y simulada) y `PantallasT1316Pruebas` 3/3 |

**1. Imagen sin deformar**
- **La causa:** las fotos L01 a L06 (1376×768) se importaban con *Non-Power-of-2 = ToNearest*. Unity las reescalaba, así que su ancho y alto ya no daban la proporción real y la foto se estiraba.
- **La importación:** ahora es sin NPOT, sin mipmaps y sin compresión (RGB24), con filtro bilineal. Lo hace `ImportarFotosCarga.Aplicar`.
- **El tamaño:** se calcula sobre el tamaño real de la pantalla, no sobre 1920×1080 fijo.
- **16:9 y 16:10:** la foto llena la pantalla recortando lo mínimo (menos del 1 % en 16:9).
- **4:3 y 5:4:** si llenar recortaría más del 12 %, se ve entera con bordes negros arriba y abajo.
- **Nunca se deforma:** la proporción medida en Play fue 1.792 en las tres resoluciones, igual que la de la imagen.

**2. Sin glitch**
- Quité las estelas de neón, las manchas de luz que respiraban, la franja de barrido (escaneo), la rejilla y el logo de texto con copias de color y cortes (aberración y parpadeo).
- Queda el fundido simple entre fotos, de 1.3 s cada 6.5 s.
- No hay zoom: ya no existía. Lo había quitado T-1330 para no recortar, así que no lo agregué.

**3. Logo**
- Es el mismo sprite que el menú principal (`Resources/Inicio/Logo`, el amarillo "CyberCity").
- Va chico (230 px de ancho a 1920) abajo a la derecha.
- Antes la carga buscaba un `LogoCybercity` que no existía y caía al texto con glitch.

**4. Sin barra**
- Quité la barra, el porcentaje y el título "CARGANDO".
- Quedan tres puntos muy discretos que se encienden por turno, a la izquierda del logo.

**5. Minimalista**
- El consejo va con letra fina, abajo a la izquierda, al 82 % de opacidad.
- Debajo hay un degradado negro muy suave para que se lea; no es un panel.

## Herramienta para el build
`UI/Pruebas/CapturaCargaBuild` solo actúa si el juego se abre con `-capturaCarga <carpeta>`. Con eso:
- muestra la carga real de Ciudad_Prologo;
- guarda fotos al empezar (0.05 s), a mitad (2 s), en el fundido (7.2 s), al terminar el desvanecido y con la escena ya visible;
- escribe en `tiempos.txt` cuánto tardó;
- cierra el juego.

Sin la bandera no hace nada.

## Builds
- **Builds/Demo/CyberCity.exe:** construido desde main f65c1cf a las 17:33, "Build Finished, Result: Success".
- **Builds/Juego/CyberCity.exe: NO se construyó.** Claude Code detuvo el proceso por falta de memoria justo después del Demo, así que el Juego sigue con el build de las 14:51, sin la pantalla nueva. Hay que volver a lanzarlo cuando haya memoria: `ConstruirDemo.ConstruirJuego` desde `.sesiones/s2`, que tiene `Builds` enlazado a la carpeta principal.
- **Comprobación en el build: pendiente** por la misma razón. Comando:
  `Builds/Demo/CyberCity.exe -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -capturaCarga docs/capturas/_carga/build`
  y lo mismo con `-screen-width 1366 -screen-height 768`.
- **Lo verificado** fue en Play, en el editor, con el canvas real de la carga renderizado a 1920×1080, 1366×768 y 1280×1024.

## Qué probar (Tony)
1. Abrir el Demo, empezar partida y ver la carga en pantalla completa y en ventana. Revisar que no se estire, que no haya glitch, que el logo sea el del menú y que no aparezca la barra.
2. Que al terminar se vea la ciudad sin pasar por negro.
