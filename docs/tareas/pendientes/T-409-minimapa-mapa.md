# T-409 Minimapa redondo y mapa grande con zoom, leyenda y punto marcado
Dueño: Tony | Rama: tony/T-409-minimapa-mapa | Modelo: Opus 5.5, medium (diseño de UI) | MODO RÁPIDO (docs/ronda3.md)
Depende de: T-408 (kit de UI, rombo del objetivo).

## Objetivo
Abajo a la izquierda, un minimapa redondo que gira contigo: calles, tú y hacia dónde miras, el objetivo, las zonas pintadas de su dueño,
enemigos cerca en rojo, aliados en naranja e iconos del mercado negro, la clínica y la casa. El mapa grande (M) tiene zoom con la rueda,
leyenda, y con un clic marcas un punto que aparece en el minimapa con su distancia.

## Contexto
- Guía de estilo: docs/diseno/ui-cyberfix.md. Kit: `UI/Kit/` (T-408).
- Mapa actual: `UI/Mapa/PantallaMapa.cs` (centros de las 6 manzanas, lado 80 m, paso 94 m; las calles quedan entre manzanas).
  Zonas y dueños: `Core/Territorio/ITerritorio.cs`. Aliados: `Core/Territorio/IPandillaJugador.cs`. Objetivo: `IObjetivos`.
- Enemigos: `IA/APie/PandilleroAPie.cs` y `IA/APie/JefePandilla.cs` (UI no ve IA: por eso el contrato nuevo).
- Lugares: `Lugar_MercadoNegro`, la camilla `Clinica_Camilla` (T-402) y `Casa_Puerta` en Ciudad_Prologo.
- HUD que hoy está abajo a la izquierda: la vida y el dinero (`UI/Editor/CreadorHudAPie.cs`) y la fila de aliados (`HudAliados`).
- Decisión: D-tony-241. **Cambio de Core autorizado por Tony**: `Core/Mapa/IMarcaMapa.cs` y `TipoMarcaMapa`.

## Carpetas permitidas
Core/Mapa/ (nuevo), IA/APie/ (registrar enemigos), UI/ (Mapa/, HudAPie/, Kit/, Editor/, Datos/), Progresion/Prologo/Editor/CreadorCiudadPandillas.cs,
Escenas/Ciudad_Prologo.unity (solo con código), Tests/EditMode/UI/, Tests/PlayMode/UI/, docs de Tony. No tocar: Radio/, Mundo/.

## Qué hacer
1. **Contrato**: `TipoMarcaMapa` (Enemigo, Jefe, MercadoNegro, Clinica, Casa) e `IMarcaMapa` (`Tipo`, `Vector3 Posicion`, `bool Visible`),
   en `Registro<IMarcaMapa>`. `PandilleroAPie` y `JefePandilla` se registran mientras están vivos y activos. `UI/Mapa/MarcaLugar.cs` es
   la marca de un lugar fijo; `T409()` pone tres (mercado negro, clínica y casa) bajo `GTA_T409`.
2. **Geometría compartida** `UI/Mapa/GeometriaBarrio.cs`: centros y lado de las manzanas, zona de cada una, de mundo a mapa. PantallaMapa y
   el minimapa la usan; sale de las constantes de PantallaMapa.
3. **Minimapa** `UI/Mapa/Minimapa.cs` (canvas orden 25) con la lógica pura en `CalculoMinimapa.cs`: un círculo de 260 px a 48 px de la
   esquina de abajo a la izquierda, con máscara redonda y borde turquesa; muestra 60 m a la redonda y gira con la cámara (arriba es hacia
   donde miras). Calles en gris sobre fondo negro y manzanas del color de su dueño al 55 %. Tú eres una flecha blanca fija en el centro.
   El objetivo es el rombo naranja (si está lejos, queda pegado al borde). Los enemigos dentro del radio salen en `#FF3333`, el jefe más
   grande, los aliados en naranja, y el mercado negro, la clínica y la casa con un icono simple ($, + y una casita, en sprites hechos por
   código). El punto marcado sale como una marca turquesa con su distancia ("120 m").
4. **Mapa grande** (`PantallaMapa`): con el kit; zoom de 1x a 3x con la rueda, centrado en el cursor; leyenda a la derecha (tu pandilla,
   LOS CALAVERAS, LAS VÍBORAS, tú, aliados, enemigos, objetivo, mercado negro, clínica, casa y punto marcado); el cursor se ve mientras está
   abierto; un clic izquierdo marca un punto y otro clic sobre él lo quita; dibuja también los enemigos e iconos. M o Esc cierran. Sonidos
   UI_MAPA_ABRIR y UI_MAPA_CERRAR si ya hay clip (si no, el gestor avisa una vez y sigue).
5. **HUD**: la vida y el dinero pasan a la derecha del minimapa (x = 48 + 260 + 24) y la fila de aliados arriba de él; nada se encima en
   1080p ni en 1440p.

## Criterios de aceptación
- [ ] Al girar la cámara 90°, el contenido del minimapa gira 90° (± 2) y la flecha del centro sigue apuntando arriba.
- [ ] Un pandillero a 30 m sale en rojo en su lugar del minimapa; a 80 m no sale. Un aliado sale en naranja.
- [ ] Con el mapa abierto, la rueda cambia el zoom entre 1x y 3x; un clic marca un punto, que sale en el minimapa con su distancia en metros.
- [ ] Los iconos del mercado negro, la clínica y la casa salen en el minimapa cuando están dentro del radio y siempre en el mapa.
- [ ] Captura revisada a ojo y corregida si se ve mal: docs/capturas/T-409-1.png (minimapa en el Tianguis con enemigos y aliado; mapa con
      leyenda y punto marcado) en 1920x1080 y 2560x1440.

## Pruebas (solo estas 2 nuevas)
- EditMode `CalculoMinimapaPruebas.GiraYRecorta`: un punto al norte, mirando al este, queda a la izquierda; uno a 100 m queda en el borde
  (si es objetivo o punto marcado) o no sale (si es enemigo); zoom 2x pone un punto al doble de distancia del centro.
- PlayMode `MinimapaPruebas.RecorridoMapaYPunto` (InputTestFixture): Ciudad_Prologo con un pandillero a 30 m y el CHATO reclutado; el ratón
  gira la cámara → el minimapa gira; M abre el mapa, la rueda hace zoom, un clic marca un punto, M cierra; el minimapa muestra el punto con
  su distancia y W lo acerca. Al final toma la captura.

## Al terminar
- Builds: `ConstruirDemo.ConstruirJuego` → Builds/Juego/Cyberfix.exe y `ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/Cyberfix.exe.
- docs/guia-para-jugar.md, sección de pandillas: 3 líneas de qué probar.
- Bitácora de una línea, fila de docs/estado-tony.md en "hecha", merge a tony/fase-g y a main con push (autorizado en docs/ronda3.md).
  Nunca force push; `git restore` a lo que Unity cambia solo.

## Sensación (Tony)
- ¿Te ubicas con el minimapa sin abrir el mapa? ¿Marea que gire? ¿60 m de radio es poco o mucho? ¿Marcar un punto se siente útil?

## Fuera de alcance
- La ciudad grande, la ruta por calles (GPS), las patrullas y los drones en el minimapa.
