# T-410 Pausa con Esc: continuar, guardar, cargar, opciones y salir al título
Dueño: Tony | Rama: tony/T-410-pausa | Modelo: Sonnet 5.5, medium | MODO RÁPIDO (docs/ronda3.md)
Depende de: T-408 (kit de UI) y T-412 (ranuras y FlujoPartida).

## Objetivo
Esc pausa el juego de verdad: se detienen el tiempo, la IA, los disparos y el mundo, y sale un menú con CONTINUAR, GUARDAR, CARGAR,
OPCIONES y SALIR AL TÍTULO. Si hay otra pantalla abierta (mapa, tienda, ranuras, continuará), Esc cierra esa y no pausa.

## Contexto
- Guía de estilo: docs/diseno/ui-cyberfix.md. Kit: `UI/Kit/` (T-408, `MenuCyberfix` ya lee Esc y las flechas).
- Hoy Esc suelta el cursor en `Personaje/Movimiento/ControladorAPie.cs` (acción `liberarCursor` de ControlesAPie); el mapa
  (`UI/Mapa/PantallaMapa.cs`) congela al jugador con `ActivarControl(false)`; la tienda (`UI/Tiendas/PantallaTienda.cs`) y el
  continuará (`UI/Historia/PantallaHistoriaPandillas.cs`) se cierran con Esc.
- En el carro, `Vehiculos/Carro/CamaraCarro.cs` lee el ratón cada cuadro y seguiría girando en pausa.
- Ranuras y salida: `UI/Guardado/PantallaRanuras.cs` y `Progresion/Partida/FlujoPartida.cs` (T-412).
- Desde el paso 2 también son modales la tienda del CIC (T-417, la misma `PantallaTienda`) y la mira del sniper (T-416, `MiraSniper`): al pausar
  se suelta el zoom y la mira se apaga; al continuar no vuelve sola hasta que vuelvas a apuntar.
- Decisiones: D-tony-242 y D-tony-259. **Cambio de Core autorizado por Tony**: `Core/Juego/Pausa.cs` (estático, como `EventosLugares`).

## Carpetas permitidas
Core/Juego/ (nuevo), UI/ (Pausa/ nuevo, Kit/, Mapa/, Tiendas/, Historia/, Guardado/, Editor/, Datos/), Personaje/Movimiento/ y Personaje/Entrada/
(Esc ya no suelta el cursor), Vehiculos/Carro/ (no leer entrada en pausa), Progresion/Prologo/Pandillas/ (F6 no en pausa),
Progresion/Prologo/Editor/CreadorCiudadPandillas.cs, Escenas/Ciudad_Prologo.unity (solo con código), Tests/PlayMode/UI/, docs de Tony.
No tocar: Radio/, Mundo/, IA/ (la IA se detiene con `Time.timeScale`).

## Qué hacer
1. **`Core/Juego/Pausa.cs`**: `bool Activa`, `event Action<bool> Cambio` y `Fijar(bool)`. Solo la pantalla de pausa lo cambia. Lo leen el
   carro (no lee entrada ni gira la cámara), DirectorPandillas (F6) y el mapa (M no abre en pausa).
2. **`UI/Kit/PilaPantallas.cs`**: cada pantalla modal se apila al abrirse y se quita al cerrarse (mapa, tienda, ranuras, continuará,
   opciones y pausa). Esc lo atiende solo la de arriba; la pausa se abre con Esc únicamente si la pila está vacía.
3. **`UI/Pausa/PantallaPausa.cs`** (prefab con el kit; canvas de orden 60, fondo `#000000A6` a pantalla completa y panel centrado):
   "PAUSA" (encabezado naranja) y CONTINUAR, GUARDAR, CARGAR, OPCIONES y SALIR AL TÍTULO. Al abrir: `Time.timeScale = 0`,
   `AudioListener.pause = true` (los sonidos de Interfaz siguen), cursor visible y jugador congelado. Al continuar se deja todo como estaba.
   GUARDAR y CARGAR abren `PantallaRanuras` en su modo, encima de la pausa. OPCIONES queda desactivado hasta T-411. SALIR AL TÍTULO
   pregunta "¿SALIR? LO QUE NO GUARDASTE SE PIERDE" (SÍ/NO, en magenta) y llama `FlujoPartida.IrAlTitulo()`.
4. Esc deja de soltar el cursor en ControladorAPie (quita esa lectura y su acción del creador de la entrada); F5 y F9 de T-412 se quitan:
   ahora se guarda desde la pausa. `T410()` pone la pausa bajo `GTA_T410`.

## Criterios de aceptación
- [ ] Con un pandillero disparándote, en 2 s de pausa (tiempo real) tu vida no baja, el pandillero no se mueve y `Time.timeScale` es 0.
- [ ] En pausa, el clic izquierdo no dispara (el cargador no cambia) y WASD no te mueven; en el carro, el ratón no gira la cámara.
- [ ] Con el mapa abierto, Esc cierra el mapa y no abre la pausa; el siguiente Esc la abre.
- [ ] CONTINUAR (Enter o clic) vuelve con `Time.timeScale` en 1 y el cursor bloqueado; GUARDAR abre las ranuras en modo guardar.
- [ ] Captura revisada a ojo y corregida si se ve mal: docs/capturas/T-410-1.png (pausa sobre la calle de noche) en 1920x1080 y 2560x1440.

## Pruebas (solo esta nueva)
- PlayMode `PausaPruebas.RecorridoPausa` (InputTestFixture): Ciudad_Prologo con un pandillero armado a 15 m; M abre el mapa y Esc lo
  cierra sin pausar; Esc pausa → se comprueban los tres primeros criterios durante 2 s reales; ↓ elige GUARDAR, Enter abre las ranuras,
  Esc vuelve a la pausa; el ratón pasa por encima de CONTINUAR y el clic continúa. Toma la captura.

## Al terminar
- Builds: `ConstruirDemo.ConstruirJuego` → Builds/Juego/Cyberfix.exe y `ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/Cyberfix.exe.
- docs/guia-para-jugar.md, sección de pandillas: 3 líneas de qué probar (y cambia "Esc suelta el cursor" en los controles).
- Bitácora de una línea, fila de docs/estado-tony.md en "hecha", merge a tony/fase-g y a main con push (autorizado en docs/ronda3.md).
  Nunca force push; `git restore` a lo que Unity cambia solo.

## Sensación (Tony)
- ¿La pausa se siente instantánea? ¿Se nota que el mundo se congeló? ¿El menú es claro con ratón y con teclado?

## Fuera de alcance
- Las opciones (T-411), el título (T-413), pausar sola al perder el foco de la ventana.
