# T-408 Kit de UI y marcador de objetivo (icono, distancia y flecha en el borde)
Dueño: Tony | Rama: tony/T-408-marcador-objetivo | Modelo: Opus 5.5, medium (diseño de UI) | MODO RÁPIDO (docs/ronda3.md)
Depende de: T-407 (sonidos UI y nombre).

## Objetivo
Siempre sabes a dónde ir: un rombo sobre el objetivo con su distancia en metros; si queda fuera de la pantalla o detrás, una flecha en el
borde apunta hacia él; arriba, un texto corto ("VE AL MERCADO NEGRO", "VENCE A LOS CALAVERAS 2/7"). El mismo objetivo sale en el mapa.
De paso nace el kit de UI que usan todas las pantallas de la ronda 3.

## Contexto
- Guía de estilo: **docs/diseno/ui-cyberfix.md** (la sigues al pie de la letra; si algo no funciona, corrige la guía y anótalo).
- Objetivo: `Core/Objetivos/IObjetivos.cs` y `Objetivo.cs` (texto y posición); lo fija `DirectorPandillas.FijarObjetivo`
  (`Progresion/Prologo/Pandillas/`). Hoy lo muestra `UI/HudAPie/HudAPie.cs` arriba al centro con una flecha que gira
  (`CalculoFlechaObjetivo`); el marcador nuevo la reemplaza (el texto se queda, con el estilo nuevo).
- `UI/Historia/PantallaHistoriaPandillas.cs` usa OnGUI: pásala al kit (mismo API público para que DirectorPandillas no cambie).
- `UI/Mapa/FormatoTerritorio.cs` ya crea canvas y textos por código: que llame al kit.
- Decisiones: D-tony-238 y D-tony-239 (kit y paleta), D-tony-240 (marcador y textos).

## Carpetas permitidas
UI/ (Kit/ nuevo, Datos/, Editor/, HudAPie/, Historia/, Mapa/), Progresion/Prologo/Pandillas/ (solo los textos de objetivo),
Progresion/Prologo/Editor/CreadorCiudadPandillas.cs, Escenas/Ciudad_Prologo.unity (solo con código), Tests/EditMode/UI/, Tests/PlayMode/UI/,
docs/diseno/ui-cyberfix.md, docs de Tony. No tocar: Core/, Radio/, Mundo/.

## Qué hacer
1. **Kit** (`UI/Kit/`): `EstiloCyberfix` (ScriptableObject con fuente, los 6 colores, los 7 tamaños, márgenes y medidas de botón de la
   guía) y su asset `UI/Datos/EstiloCyberfix_Base.asset` hecho por código; `KitUI` estático: `CrearCanvas(padre, nombre, orden)`,
   `CrearPanel`, `CrearTexto(rol)`, `CrearBoton(texto, accion)`, `CrearDeslizador`, `CrearConmutador`, `CrearSelector(opciones)`,
   `AsegurarEventSystem()` (InputSystemUIInputModule); `BotonCyberfix` (estados normal, elegido, pulsado, desactivado de la guía; el ratón
   encima elige; sonidos UI_MOVER, UI_ACEPTAR, UI_ERROR por `Sonidos`); `MenuCyberfix` (navegación con ↑ ↓ W S, ← → A D, Enter, Espacio y
   Esc, elige el primer botón al abrir, muestra y oculta el cursor). Sirve igual desde un creador de editor que en tiempo de juego.
2. **Ayudante de capturas** `Tests/PlayMode/UI/CapturaUI.cs`: pone los canvas en Screen Space Camera, renderiza a 1920x1080 y a 2560x1440,
   guarda el PNG y deja todo como estaba. Lo usan T-408 a T-413.
3. **Marcador** `UI/HudAPie/MarcadorObjetivo.cs` (canvas propio, orden 15) con la lógica pura en `CalculoMarcador.cs`: el rombo naranja
   de 36 px va 2.2 m arriba del objetivo con la distancia turquesa abajo ("45 m"); si el objetivo está detrás de la cámara o fuera de la
   pantalla, una flecha naranja en el borde (a 64 px) apunta hacia él, con la distancia; a menos de 3 m, el rombo se apaga. Sonidos
   UI_OBJETIVO_NUEVO al cambiar de objetivo y UI_OBJETIVO_CUMPLIDO al quitarlo. Quita la flecha giratoria de HudAPie con su creador.
4. **Textos cortos** en DirectorPandillas (MAYÚSCULAS, 32 caracteres como máximo): "SAL A LA CALLE", "DEFIENDE A DOÑA LUPE", "RECOGE LA
   PISTOLA (E)", "HABLA CON EL CHATO", "VENCE A LOS CALAVERAS n/7" (respeto ÷ 10) y "VENCE AL JEFE". Si la historia te manda a comprar,
   usa "VE AL MERCADO NEGRO".
5. **Mapa**: `PantallaMapa` dibuja el objetivo con el mismo rombo y pasa su canvas y sus textos al kit. `HudTerritorio`, `HudAliados` y
   `PantallaHistoriaPandillas` (ya sin OnGUI) usan el kit y la paleta: misma fuente, mismos colores y tamaño mínimo de 20.

## Criterios de aceptación
- [ ] Ningún `OnGUI` en Assets/_Proyecto/UI/ (grep).
- [ ] Con el objetivo enfrente, el rombo queda a menos de 10 px de la posición proyectada y la distancia baja al caminar hacia él.
- [ ] Con el objetivo detrás, el rombo se apaga y la flecha queda en el borde, a 64 px ± 2, apuntando hacia el lado correcto.
- [ ] El mapa (M) muestra el rombo del objetivo en su posición; el texto de arriba cabe en una línea en 1080p.
- [ ] Captura revisada a ojo y corregida si se ve mal: docs/capturas/T-408-1.png (rombo en pantalla y flecha en el borde, mapa abierto en otra)
      en 1920x1080 y 2560x1440.

## Pruebas (solo estas 2 nuevas)
- EditMode `CalculoMarcadorPruebas.DentroFueraYDetras`: un punto en pantalla queda donde se proyecta; uno fuera a la derecha va al borde
  derecho; uno detrás de la cámara nunca sale en el centro y va al borde del lado correcto; distancias "3 m", "45 m" y "1.2 km".
- PlayMode `MarcadorObjetivoPruebas.RecorridoJugadorNuevo` (InputTestFixture): Ciudad_Prologo en el paso DESPIERTA (objetivo: la puerta);
  W unos segundos → el rombo se ve y la distancia baja; el ratón gira la cámara 180° → aparece la flecha en el borde; M abre el mapa con el
  rombo y M lo cierra; al salir de casa el texto cambia a "DEFIENDE A DOÑA LUPE". Al final toma la captura.

## Al terminar
- Builds: `ConstruirDemo.ConstruirJuego` → Builds/Juego/Cyberfix.exe y `ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/Cyberfix.exe.
- docs/guia-para-jugar.md, sección de pandillas: 3 líneas de qué probar.
- Bitácora de una línea, fila de docs/estado-tony.md en "hecha", merge a tony/fase-g y a main con push (autorizado en docs/ronda3.md).
  Nunca force push; `git restore` a lo que Unity cambia solo.

## Sensación (Tony)
- ¿Encuentras el objetivo sin pensar? ¿El rombo estorba al pelear? ¿La flecha del borde se entiende? ¿Te gustan los colores y la letra?

## Fuera de alcance
- El minimapa (T-409) y la fuente cyberpunk (cuando Tony la tenga).
