# T-413 Pantalla de título CYBERFIX y creación de personaje sin mechas
Dueño: Tony | Rama: tony/T-413-titulo-creacion | Modelo: Opus 5.5, medium (diseño de UI) | MODO RÁPIDO (docs/ronda3.md)
Depende de: T-407 a T-412 (es la última de la ronda 3).

## Objetivo
El juego y la demo arrancan en el título CYBERFIX: la ciudad de noche de fondo con una cámara lenta, y NUEVA PARTIDA, CONTINUAR, OPCIONES y
SALIR. NUEVA PARTIDA abre la creación: eliges uno de 2 personajes, tu nombre y el nombre y el color de tu pandilla, y empieza la historia.

## Contexto
- Guía de estilo: docs/diseno/ui-cyberfix.md. Kit: `UI/Kit/`. Opciones: `UI/Opciones/PantallaOpciones` (T-411). Ranuras y flujo:
  `ServicioGuardado.RanuraMasReciente()`, `ResumenDe()` y `Progresion/Partida/FlujoPartida.cs` (T-412).
- Título y creación viejos (con mechas): `UI/Creacion/PantallaCreacion.cs` y `Escenas/Tony/CreacionPersonaje.unity`. No se tocan; de ahí se
  puede copiar `UI/Creacion/CampoTextoTeclado.cs` (escribir con el teclado) y `Personaje/Apariencia/VistaPreviaApariencia.cs` (el muñeco en
  vivo en una RawImage).
- Perfil: `Core/Jugador/IPerfilJugador.cs` (nombre, alias, apariencia) y `Progresion/Perfil/ServicioPerfil.cs` (clave `perfil`).
  El muñeco: `Assets/Arte/Personajes/MunecoHumano/` (ModeloJugador.prefab, D-tony-204).
- Tu color y tu nombre de pandilla hoy están fijos en `UI/Mapa/FormatoTerritorio.cs` (naranja, "TUYA") y en los aliados (IA/Aliados/).
- La historia empieza en `DirectorPandillas.Start` (paso DESPIERTA, en la cama).
- Decisiones: D-tony-245 (título dentro de Ciudad_Prologo) y D-tony-246 (creación).
- **Cambio de Core autorizado por Tony**: `IPerfilJugador` suma `string NombrePandilla` y `int ColorPandilla` (con sus dobles de prueba).

## Carpetas permitidas
Core/Jugador/IPerfilJugador.cs, Progresion/Perfil/, Progresion/Partida/, Progresion/Prologo/ (Pandillas/, Editor/), Personaje/Apariencia/,
Assets/Arte/Personajes/MunecoHumano/ (variante B por código), IA/Aliados/ (color de tu pandilla), UI/ (Titulo/ nuevo, Mapa/, HudAPie/, Kit/,
Editor/, Datos/), Escenas/Ciudad_Prologo.unity (solo con código), Tests/PlayMode/UI/, los dobles de `IPerfilJugador` en Tests/, docs de Tony.
No tocar: Radio/, Mundo/, Escenas/Tony/CreacionPersonaje.unity.

## Qué hacer
1. **Perfil**: `NombrePandilla` (por defecto "LOS DE LA FOSA"; de 3 a 16 letras, espacios y Ñ) y `ColorPandilla` (0 NARANJA `#FF801A`,
   1 TURQUESA `#1AE6E6`, 2 AMARILLO `#FFD91A`, 3 AZUL `#3380FF`), guardados en `perfil`. `FormatoTerritorio` pinta LA FOSA y tus puntos con
   ese color y escribe el nombre en lugar de "TUYA"; los aliados se visten de ese color; el minimapa y el mapa también. El naranja de la UI
   no cambia.
2. **Dos personajes**: A "CHAMARRA" (chamarra naranja y cabello turquesa) y B "MÁSCARA" (`ModeloJugador_B.prefab`, variante hecha por
   código). Se eligen con `Apariencia.Cara` 0 o 1; `Personaje/Apariencia/ModeloSegunPerfil.cs` prende el que toca.
3. **Flujo**: `FlujoPartida.EnTitulo` es verdadero al abrir Ciudad_Prologo sin nada pendiente: en un build siempre; en el editor solo si
   `FlujoPartida.TituloEnEditor` (lo pone la prueba y un menú del editor), así las pruebas viejas no cambian. En el título el HUD, el
   minimapa y la pausa están apagados, el jugador está oculto y quieto, y DirectorPandillas espera. `IrAlTitulo()` de T-412 ahora va al título.
4. **`Progresion/Partida/CamaraTitulo.cs`**: recorre despacio (unos 2 m/s, a 12 m de alto) una vuelta por las calles del barrio de noche y
   se apaga al empezar a jugar.
5. **`UI/Titulo/PantallaTitulo.cs`** (prefab con el kit; canvas de orden 80): "CYBERFIX" de 96 px en naranja con sombra magenta (un campo
   para el PNG del logo cuando llegue), y NUEVA PARTIDA, CONTINUAR (desactivado si no hay partidas; si hay, abajo en turquesa el resumen
   de la más reciente), OPCIONES y SALIR (`Application.Quit`). Suena UI_TITULO_INICIO al aparecer.
6. **`UI/Titulo/PantallaCreacionPandilla.cs`** (mismo kit): a la izquierda el muñeco elegido girando (vista previa en una RawImage) y a la
   derecha PERSONAJE (← → entre A y B), TU NOMBRE, TU PANDILLA (se escribe), COLOR (← → entre 4 muestras), EMPEZAR y VOLVER. EMPEZAR llena
   el perfil y llama `FlujoPartida.IrANuevaPartida()`: la historia arranca en DESPIERTA con ese perfil. Si hay partida AUTO, avisa
   "SE REEMPLAZA EL AUTOGUARDADO" (SÍ/NO).
7. `T413()` pone el flujo, la cámara y las dos pantallas bajo `GTA_T413`. Los dos builds arrancan en el título.

## Criterios de aceptación
- [ ] En el build, al abrir sale el título con la cámara moviéndose y el HUD apagado; Esc no abre la pausa.
- [ ] Sin partidas, CONTINUAR está desactivado; tras jugar y autoguardar, CONTINUAR carga la más reciente y te deja donde estabas.
- [ ] La creación con teclado (personaje B, nombre "TONY", pandilla "LOS NEÓN", color AZUL) da ese perfil; LA FOSA sale azul en el mapa,
      el HUD de zona dice "LOS NEÓN" y el CHATO se viste de azul.
- [ ] Todo se puede hacer también con el ratón (pasar por encima elige y el clic acepta).
- [ ] Capturas revisadas a ojo y corregidas si se ven mal: docs/capturas/T-413-1.png (título) y T-413-2.png (creación), en 1920x1080 y 2560x1440.

## Pruebas (solo estas 2 nuevas)
- PlayMode `TituloCreacionPruebas.RecorridoJugadorNuevo` (InputTestFixture, `TituloEnEditor` y ruta de guardado temporal): Ciudad_Prologo
  en el título; CONTINUAR desactivado; Enter en NUEVA PARTIDA; → elige B; escribe TONY; escribe LOS NEÓN; → → → hasta AZUL; Enter en
  EMPEZAR; la historia está en DESPIERTA, el perfil tiene esos datos y LA FOSA es azul en el mapa. Toma las dos capturas.
- PlayMode `TituloCreacionPruebas.ContinuarConRaton`: con una partida AUTO escrita antes, en el título el ratón pasa por encima de
  CONTINUAR y el clic carga: dinero y posición son los de la partida; OPCIONES con clic abre las opciones y Esc vuelve al título.

## Al terminar
- Builds: `ConstruirDemo.ConstruirJuego` → Builds/Juego/Cyberfix.exe y `ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/Cyberfix.exe.
- docs/guia-para-jugar.md, sección de pandillas: 3 líneas de qué probar (y "la demo empieza en el título" en Cómo abrir).
- Bitácora de una línea, fila de docs/estado-tony.md en "hecha", merge a tony/fase-g y a main con push (autorizado en docs/ronda3.md).
  Nunca force push; `git restore` a lo que Unity cambia solo.

## Sensación (Tony)
- ¿El título da ganas de jugar? ¿La cámara lenta se ve bien o marea? ¿Los dos muñecos se distinguen? ¿Ponerle nombre a tu pandilla engancha?

## Fuera de alcance
- El logo final (PNG de Tony), la música del título (MUS_TITULO, P2), más de 2 personajes y la ciudad grande.
