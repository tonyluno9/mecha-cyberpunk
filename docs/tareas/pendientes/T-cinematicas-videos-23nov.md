# T-cinematicas-videos-23nov (antes VR32) · Agregar los videos reales de las cinemáticas
**Lo que pidió Tony:** ya hay videos para las cinemáticas; usarlos para reemplazar los placeholders de CinematicaPendiente (T10, docs/tarea-historia-en-juego.md). Vale para PC y VR (en VR se ven en la pantalla grande de VR15).

**Ubicación:** `C:\Users\Angel Perez\Desktop\videojuego\trailer-cybercity-referencias\`
- `Videos_Cinematicas\` → TODOS los videos listos, en orden (01_ … 18_). Tomar los videos de aquí.
- `Cinematicas\<CXX_nombre>\` → una carpeta por cinemática con frames PNG, referencias, PROMPT.txt (qué pasa) y videos. Desde C04, los videos de cada cinemática están en su subcarpeta `Videos\`.
- `Cinematicas\_zips\` → respaldos, ignorar.

**Cómo están hechos:**
- C00_Logo, C01_Recuerdo_Campo (T01, T02), C02_Maletin_A_Llegan y C03_Maletin_B_Lo_roba: un video completo por cinemática.
- Desde C04, cada cinemática es una secuencia de clips cortos (uno por frame) en orden 01_, 02_, 03_…: reproducirlos uno tras otro SIN cortes negros, como una sola cinemática (precargar el siguiente con dos VideoPlayer alternados).
- Los videos traen solo efectos de sonido, sin voces. NO agregar voces, marco NOVA ni glitch (eso va en edición aparte).

**Qué video va en cada cinemática del juego:**
| Cinemática del juego | Videos, en orden |
|---|---|
| C02 EL MALETÍN (paso 4) | C02 + C03 + C04_Huida (F08–F11) + C05_Cuarto_Contenido (F12–F15) |
| C03 LA DECISIÓN (paso 6) | C06_Ranura_A_El_grito (G01–G03) + C07_Ranura_B_Regresa (G04–G06). C07 aún sin video: placeholder solo en esa parte |
| C04 EL SUERO (paso 8) | C08_Suero (I01–I03). Aún sin video: dejar placeholder |
| Pantalla de inicio / intro | C00_Logo |
| C01 EL RECUERDO (flashback del campo) | C01_Recuerdo_Campo |

**Qué hacer:**
1. Copiar los videos usados a `Assets/Cinematicas/<CXX>/` (Git LFS para .mp4, revisar .gitattributes) y ponerlos en cada `DatosCinematica` (permitir una LISTA de VideoClips en orden; si hoy solo admite uno, ampliarlo).
2. VideoPlayer en orden, fundidos cortos al entrar y salir, audio por el grupo de mezcla correcto (Efectos o Música, no Voces).
3. Saltar con el botón de saltar (ENTER en PC, B en VR), como hoy.
4. Donde no hay video (C07, C08): placeholder actual. Si aparecen clips nuevos en `Cinematicas\C07_Ranura_B_Regresa\Videos\` o `Cinematicas\C08_Suero\Videos\`, usarlos con el mismo esquema.
5. Ojo con VR24: la ranura ya no es paso jugable; C03 LA DECISIÓN se ve justo antes de aparecer en tu punto para pelear con los 3 SKULLS.
6. Probar en Play el paso 4 (EL MALETÍN) de principio a fin: anotar en el reporte cuánto dura y si algún clip se corta, se traba o hay negro entre clips.

**Cómo comprobar:** partida nueva: C01 con video, C02 EL MALETÍN completo sin cortes, C03 con video + placeholder de C07, C04 placeholder. Se saltan con ENTER/B.
**Estado:** HECHA (23-nov, ver docs/reporte-voces-cinematicas-23nov.md). C07 y C08 siguen con placeholder: no hay videos.
