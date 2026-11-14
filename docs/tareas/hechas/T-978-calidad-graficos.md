# T-978: calidad BAJO, MEDIO y ALTO en Opciones

**Hecho.** Pedido de Tony (13-nov): calidad Bajo, Medio y Alto en Opciones (sombras, distancia de dibujo, reflejos de charcos y postproceso),
que se guarde, con Medio por defecto, y medir los FPS en Bajo.

Ya existía el selector CALIDAD (T-411) en el menú de inicio y en la pausa, guardado en `opciones.json`, que cambiaba escala de render,
sombras y MSAA sobre una copia del asset de URP. Se completó:

| | BAJO | MEDIO (por defecto) | ALTO |
| --- | --- | --- | --- |
| Escala de render / MSAA | 0.75 / 1 | 0.9 / 2 | 1 / 4 |
| Distancia de sombras | 40 m | 70 m | 120 m |
| Distancia de dibujo | 150 m | 300 m | sin límite (plano lejano) |
| Reflejos de charcos | no | sí | sí |
| Postproceso | no | sí | sí |

- Textos BAJO / MEDIO / ALTO. Partida nueva (sin `opciones.json`) en MEDIO; un `opciones.json` que ya existe conserva lo que tenía.
- **Distancia de dibujo:** por capa (`Camera.layerCullDistances`, esférica), porque Cinemachine reescribe el plano lejano cada cuadro.
- **Postproceso:** se apaga en BAJO en cada cámara; en MEDIO y ALTO vuelve el que traía la cámara (no se prende en cámaras que no lo usaban).
- `ServicioOpciones` lo aplica a todas las cámaras activas, una vez por cámara y calidad (sin GetComponent por cuadro).
- **Reflejos de charcos:** contrato nuevo `Core/Opciones/CalidadVisual` (lo fija ServicioOpciones, lo lee `Mundo/Ambiente/ReflejoCharcos`;
  sin servicio queda prendido). Toca Core y una línea de Mundo (Alan): avisarle.
- `-calidad Bajo|Medio|Alto` al lanzar el .exe manda sobre `opciones.json` sin guardarlo (para medir y grabar).

## FPS medidos en BAJO
Build de medición (`ConstruirMedicionAmbiente`, Ciudad_Prologo a las 23 h, 1920×1080 en ventana, sin vsync), NVIDIA GeForce RTX 5070 Laptop, D3D12,
8 s por punto. Archivos: `docs/capturas/t978/medicion-*.txt`.

| Punto | BAJO prom. / 1 % bajo | MEDIO | ALTO |
| --- | --- | --- | --- |
| calle | 624 / 370 | 631 / 370 | 621 / 369 |
| callejón | 634 / 371 | 643 / 382 | 639 / 366 |
| clandestino | 647 / 375 | 646 / 376 | 646 / 378 |

En esta laptop la GPU sobra y los tres niveles dan lo mismo (la escena va limitada por otra cosa). BAJO sirve para equipos más débiles;
falta medirlo en uno. Los contadores de triángulos y llamadas de la herramienta no funcionan en un build que no es de desarrollo (salen 7 / 1013).
Capturas `docs/capturas/t978/calle-Bajo|Medio|Alto.jpg`: en BAJO se va el color de noche del postproceso (la calle se ve más gris).

## Pruebas
EditMode 2356/2356 (`ReglaOpcionesPruebas`: defecto MEDIO, tablas y `-calidad`). PlayMode: `OpcionesPruebas` (BAJO apaga postproceso,
dibuja 150 m y quita reflejos; ALTO lo regresa), `InicioPruebas`, `MenusCyberfixPruebas`, `HudCinematicaT726Pruebas`, `TituloCreacionPruebas`,
`TutorialLigeroPruebas` y `CalleDoradaS2cPlayPruebas` (charcos): todo en verde.
