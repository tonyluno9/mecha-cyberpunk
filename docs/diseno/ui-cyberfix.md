# Guía de estilo de la UI de CYBERFIX
Para todas las pantallas desde T-407 (D-tony-238, D-tony-239). Se diseña a 1920x1080 y se revisa en 1080p y 1440p.

## Herramienta
- **uGUI con un kit común** en `UI/Kit/`: `KitUI` crea canvas, panel, texto, botón, deslizador, conmutador y selector; `EstiloCyberfix_Base.asset`
  (UI/Datos) guarda la fuente, los colores, los tamaños y los márgenes. Las pantallas nuevas son prefabs que hace un creador de editor
  llamando a `KitUI`; los HUD que arman su canvas por código (HudTerritorio, HudAliados, PantallaMapa) usan las mismas funciones.
- **Nada nuevo con OnGUI.** El único OnGUI (`UI/Historia/PantallaHistoriaPandillas`) pasa al kit en T-408. OnGUI no escala con el
  CanvasScaler, no comparte fuente ni foco y no se ve en las capturas de las pruebas.
- Texto: `UnityEngine.UI.Text`, como las pantallas que ya hay. No TextMeshPro, para no mezclar dos sistemas de texto.
- Canvas: Screen Space Overlay, `CanvasScaler` ScaleWithScreenSize, referencia 1920x1080, match 0.5. Orden de dibujo: HUD 10, marcador 15,
  historia 20, minimapa 25, mapa 40, tienda 45, pausa 60, ranuras y opciones 70, título 80, fundido 100.
- Un solo `EventSystem` con `InputSystemUIInputModule` (`KitUI.AsegurarEventSystem`): el Input Manager viejo está apagado.

## Fuente
Una sola en todo el juego: la integrada de Unity (`LegacyRuntime.ttf`), porque el proyecto no trae ninguna libre. Cuando Tony consiga la
cyberpunk (licencia OFL o CC0), se cambia en un solo campo de `EstiloCyberfix`. MAYÚSCULAS en títulos, botones, HUD y objetivos;
oraciones normales en los textos largos.

## Colores (RGBA hex)
| Rol | Color | Uso |
| --- | --- | --- |
| Naranja | `#FF801AFF` | Tú, el foco (botón elegido), el objetivo, el título |
| Turquesa | `#1AE6E6FF` | Información: valores, distancias, teclas de ayuda; bordes de panel al 60 % |
| Magenta | `#FF1ACCFF` | Alertas y peligro: jefe, calor, "¿seguro?"; también SKULLS |
| Negro translúcido | `#0A0A12D9` | Fondo de panel (85 %); detrás de la pausa, a pantalla completa, `#000000A6` |
| Texto | `#F2F2F2FF` | Texto normal |
| Desactivado | `#7A7A85FF` | Botón o fila que no se puede usar |
Fuera de la UI: SNAKES `#73FF1AFF` y los enemigos del minimapa `#FF3333FF`. No pongas texto magenta sobre naranja ni al revés.

## Tamaños (px a 1920x1080; en 1440p salen x1.33)
Título 96 · encabezado 48 · botón 34 · texto 28 · HUD 26 · ayuda 22 · **mínimo 20**. Todo texto lleva sombra negra al 85 % (1.5, −1.5).

## Márgenes, botones y foco
- 48 px al borde de la pantalla, 24 entre bloques y 16 de relleno dentro de un panel. Borde de panel: 2 px turquesa al 60 %.
- Botón de menú de 440x64, con 12 px entre botones; fila de opciones de 760x56 (etiqueta a la izquierda y valor turquesa a la derecha).
- Estados: **normal**: fondo negro translúcido y texto `#F2F2F2`. **Elegido** (foco del teclado o ratón encima): borde naranja de 3 px,
  fondo naranja al 25 %, texto naranja y "›" a la izquierda. **Pulsado**: fondo naranja al 60 %. **Desactivado**: texto gris, sin borde
  y no se puede elegir.
- Teclado y ratón comparten una sola selección: pasar el ratón por encima elige. ↑ ↓ (o W S) mueven, ← → (o A D) cambian valores,
  Enter o Espacio aceptan, Esc regresa. El clic acepta y la rueda cambia los deslizadores y selectores.
- Al abrir un menú se elige su primer botón activo y se muestra el cursor; al cerrar el último menú el cursor vuelve a quedar bloqueado.
- Sonidos del kit (T-407): UI_MOVER al cambiar de elegido, UI_ACEPTAR, UI_CANCELAR con Esc y UI_ERROR al pulsar algo desactivado.

## Revisión de cada pantalla
Una captura en 1920x1080 y otra en 2560x1440 en docs/capturas/T-xxx-n.png. Se revisa a ojo: nada se sale ni se encima, se lee a un
metro de la pantalla y el elegido se nota. Si se ve mal, se arregla antes de seguir. Los canvas Overlay no salen en `Camera.Render()`:
el ayudante `Tests/PlayMode/UI/CapturaUI.cs` (T-408) los pasa a Screen Space Camera mientras captura.
