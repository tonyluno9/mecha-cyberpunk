# T-1387: Puerto idéntico a K15 y A15

Script: `Assets/Arte/Editor/ZonaPuertoT1387.cs` (`MechaCyberpunk.Arte.Editor.ZonaPuertoT1387`). Edita `T1357_Puerto.prefab` sin correr `ColocarT1357Puerto.Aplicar`.
Orden: `Aplicar`, luego `Validar` (Logs/t1387-validar.txt), y `Fotos` en otra corrida (docs/capturas/t-1387/).

Escrito sin Unity (sin Library en el worktree): pendiente de compilar y correr en la carpeta principal.

- PU07 (variante _a) reemplaza las casetas LO04 de U5 y U8 pegada al borde del carril, sin invadirlo.
- PU08 (variante _a) reemplaza 55% de los contenedores de las filas sur y norte de cada bloque, con los mismos límites del original.
- Piso con M_T09_ConcretoManchado y calles con M_T07_AsfaltoMojado; bodegas con M_T08_LaminaOxidada en materiales lisos.
- Esferas rojas viejas de las grúas (primitivos, flotando) quitadas; balizas rojas emisivas en lo alto de cada grúa.
- Basura BS01a..d sin collider en el patio. Barco: solo existe PU02_Barco (no hay otro modelo de carguero).
- Cámara K15 movida a (158.6, 2.3, -84): la caja amarilla era el C17_Huacales de la primera bodega, a 4.5 m de la cámara.
