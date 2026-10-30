# T-418 Historia de la demo: Suburbios, El Doc, El Inventor, el jefe de los SKULLS y Nova (cierre del paso 2)
Dueño: Tony | Rama: tony/T-418-historia-demo | Modelo: Opus 5.5, medium (historia y guardado; une todo el paso 2) | MODO RÁPIDO (docs/plan-noche.md, paso 2)
Depende de: T-414, T-415, T-416 y T-417. Es la última del paso 2: al terminar, merge a main, builds y guía.

## Objetivo
La demo cuenta la historia del documento del juego: despiertas en Suburbios, dos SKULLS molestan a Doña Lupe, peleas a golpes, recoges la pistola,
reclutas al CHATO, EL DOC te cura en I+D+I, EL INVENTOR te mejora la pistola en el CIC, conquistas El Tianguis venciendo al jefe de los SKULLS, vas a
Neón Alto, EL HACKER descubre que NOVA les paga y les da armas a los SNAKES, y sale "CONTINUARÁ". Después, en el juego libre, Neón Alto se conquista.

## Contexto
- Historia: `Progresion/Prologo/Pandillas/MaquinaHistoriaPandillas.cs` (`PasoHistoria` y `AvisoHistoria` van alineados por número: un aviso solo
  avanza el paso del mismo número) y `DirectorPandillas.cs` (objetivos, subtítulos, grupos del Tianguis apagados hasta `Tianguis`, F6 salta un paso
  fuera de `DEMO_FINAL`). El paso se guarda **por nombre** (`TryLeer`, clave `historia_pandillas`): se pueden meter pasos en medio. Ojo con las
  comparaciones de orden (`paso >= PasoHistoria.Tianguis`): revísalas todas.
- Profes de T-417: `Progresion/Apoyo/PersonajeApoyo` (`Usado(id)` y `Regalo`). Inventario de armas: `Personaje/Jugador/ArsenalJugador.cs` y los
  DatosArma `arma_pistola_N` (T-416). Vida: `VidaAPie`. Textos: `UI/Historia/PantallaHistoriaPandillas.cs` (subtítulo, ayuda y CONTINUARÁ; sigue en
  OnGUI hasta T-408: aquí solo cambian sus textos y su API). Neón Alto: zona `neon`, `PuntoJefe_neon` y los grupos de SNAKES de `T404()`.
- Nombres de T-414 y la pandilla de T-415 (`IPerfilJugador.NombrePandilla`). Objetivos en MAYÚSCULAS de 32 caracteres como máximo (D-tony-240).
- Prueba de la historia que ya hay: `Tests/PlayMode/Progresion/HistoriaPandillasPruebas.cs` (`RecorridoHastaContinuara` y `MaquinaDePasos`): se ajusta.
- Decisiones: D-tony-257 (pasos nuevos) y D-tony-258 (Megalopolis y Nova en la historia).

## Carpetas permitidas
Progresion/Prologo/ (Pandillas/, Editor/CreadorCiudadPandillas.cs, Editor/ConstruirDemo.cs solo si hace falta), Progresion/Apoyo/ (enganchar la historia),
UI/Historia/PantallaHistoriaPandillas.cs (textos y API), Personaje/Jugador/ArsenalJugador.cs (dar una mejora), Escenas/Ciudad_Prologo.unity (solo con código),
Tests/EditMode/Progresion/, Tests/PlayMode/Progresion/ (y ajustar HistoriaPandillasPruebas), docs/guia-para-jugar.md, docs/diseno/pandillas.md (la demo),
docs de Tony. No tocar: Radio/, Mundo/, Barrio.prefab.

## Qué hacer
1. **Pasos** (con su aviso alineado; los nuevos en negrita):
   | Paso | Objetivo (texto y lugar) | Avanza cuando |
   | --- | --- | --- |
   | Despierta | SAL A LA CALLE · la puerta | sales de casa |
   | PrimeraPelea | DEFIENDE A DOÑA LUPE · Doña Lupe | caen los 2 SKULLS (a puños) |
   | Pistola | RECOGE LA PISTOLA (E) · el arma | la recoges |
   | Chato | HABLA CON EL CHATO · el CHATO | se une (gratis) |
   | **Doc** | VE CON EL DOC A I+D+I · EL DOC | usas a EL DOC: te cura gratis (su `Regalo`) |
   | **Inventor** | VE CON EL INVENTOR AL CIC · EL INVENTOR | usas a EL INVENTOR: te da la PISTOLA I gratis (su `Regalo`) |
   | Tianguis | VENCE A LOS SKULLS n/7 · el Tianguis | respeto 70 (n = respeto ÷ 10) |
   | Jefe | VENCE AL JEFE DE LOS SKULLS · el jefe | cae el jefe y El Tianguis es tuyo |
   | **NeonAlto** | VE CON EL HACKER A NEÓN ALTO · EL HACKER | usas a EL HACKER |
   | Continuara | — | Enter en CONTINUARÁ |
   | Libre | lo de siempre | — |
2. **Textos**: al arrancar, ayuda "MEGALOPOLIS · SUBURBIOS" 4 s. Doc: "—Aguanta, mijo. Esto no te va a doler… mucho." Inventor: "—Esa pistola está
   de museo. Ya quedó." y ayuda "PISTOLA I: MÁS DAÑO Y MÁS BALAS". Jefe vencido: "EL TIANGUIS ES DE <TU PANDILLA>" (T-415). Hacker, tres subtítulos
   seguidos de 4 s: "—Rastreé el dinero de los SNAKES.", "—Les paga NOVA. Y también les da las armas.", "—Si NOVA está detrás, esto apenas empieza."
   CONTINUARÁ: "CONTINUARÁ...", abajo "NOVA ARMA A LOS SNAKES. NEÓN ALTO TE ESPERA." y "ENTER: SEGUIR JUGANDO". Cambia también el subtítulo y la
   ayuda del paso Tianguis ("—Los SKULLS se creen dueños del Tianguis" y "EN EL MERCADO VENDEN ARMAS · EL CIC LAS MEJORA").
3. **Neón Alto**: los grupos de SNAKES quedan apagados hasta `Libre` (como los del Tianguis hasta `Tianguis`), para que llegar con EL HACKER no sea una
   balacera. En `Libre` se prenden y Neón Alto se conquista igual que El Tianguis: con 70 de respeto aparece el jefe de los SNAKES y al vencerlo la zona
   es tuya (si algo de T-404 lo impide, arréglalo aquí). EL HACKER queda a la entrada de VivaCoin, al borde de la zona.
4. **Guardado**: los nombres nuevos se guardan y cargan; un guardado con un paso que ya no existe empieza en `Despierta` sin errores. Con `DEMO_FINAL`,
   todo igual que hoy (D-tony-231).
5. `T418()` (sumado a `Todo()`) conecta los profes con el director (los regalos de Doc e Inventor y la escucha de `Usado`).

## Criterios de aceptación
- [ ] La máquina recorre los 11 pasos en orden; un aviso fuera de orden no avanza; cargar "Tianguis", "Doc" o "NeonAlto" deja ese paso; cargar "Mecha"
      (no existe) deja `Despierta` sin excepción.
- [ ] En el paso Doc, E con EL DOC con vida 50 te deja la vida al máximo y no cobra; en el paso Inventor, E con EL INVENTOR cambia tu pistola a
      PISTOLA I (cargador 10) sin cobrar; fuera de esos pasos cobran como en T-417.
- [ ] Antes de `Libre` no hay ningún SNAKE prendido; en `Libre`, con respeto 70 en `neon`, aparece el jefe de los SNAKES y al vencerlo `neon` es tuya.
- [ ] Todos los objetivos caben en 32 caracteres (prueba con la lista de constantes).
- [ ] 60 FPS o más de noche en la calle principal en el build de la demo (medido y anotado: cierra el paso 2).
- [ ] Capturas revisadas a ojo y corregidas si se ven mal: docs/capturas/T-418-1.png (EL HACKER con el subtítulo de NOVA) y T-418-2.png (CONTINUARÁ),
      en 1920x1080.

## Pruebas (solo estas 2 nuevas; HistoriaPandillasPruebas se ajusta a los pasos nuevos)
- EditMode `MaquinaHistoriaPandillasPruebas.PasosNuevosEnOrden`: los 11 pasos en orden con sus avisos; avisos fuera de orden; `TryLeer` con los nombres
  nuevos y con uno que no existe; la longitud de los textos de objetivo.
- PlayMode `HistoriaDemoPruebas.RecorridoDocInventorHacker` (InputTestFixture, sigue el estilo de `HistoriaPandillasPruebas`): Ciudad_Prologo cargando el
  paso `Chato` con el CHATO cerca; E lo recluta → objetivo "VE CON EL DOC A I+D+I"; el jugador aparece junto a EL DOC con vida 50 y E lo cura gratis;
  aparece junto a EL INVENTOR y E le da la PISTOLA I; F6 (o el atajo de la prueba) lleva al paso Jefe y la prueba vence al jefe por código → objetivo
  "VE CON EL HACKER A NEÓN ALTO"; aparece junto a EL HACKER y E saca los tres subtítulos (captura 1) y luego CONTINUARÁ (captura 2); Enter pasa a
  `Libre` y los SNAKES se prenden.

## Al terminar (cierre del paso 2)
- Builds: `ConstruirDemo.ConstruirJuego` → Builds/Juego/Cyberfix.exe y `ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/Cyberfix.exe (Unity cerrado).
- docs/guia-para-jugar.md, sección de pandillas: 3 líneas de qué probar del paso 2 (nombres y Ƀ, el sniper en El Mercado, la historia con los profes y NOVA).
- docs/diseno/pandillas.md: la demo nueva en una línea.
- Bitácora de una línea, fila de docs/estado-tony.md en "hecha", merge --no-ff a tony/fase-g y después a main, con push de las dos (autorizado por Tony
  en docs/plan-noche.md). Nunca force push; `git restore` a lo que Unity cambia solo y no subas `Assets/InitTestScene*`.

## Sensación (Tony)
- ¿La historia se entiende sin explicarla? ¿Ir con El Doc y El Inventor rompe el ritmo o ayuda? ¿La revelación de NOVA da ganas de seguir? ¿La demo
  sigue durando unos 15 minutos?

## Fuera de alcance
- Voces, cinemáticas con cámara, el modelo de los profes, una cuarta zona (punto 8) y más historia después de CONTINUARÁ.
