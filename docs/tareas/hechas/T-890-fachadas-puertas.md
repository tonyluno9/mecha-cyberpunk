# T-890: Fachadas y puertas (B3 de docs/tareas-piezas-08nov.md)

Rama `tony/T-890-fachadas-puertas`. Importador: `Assets/Arte/Editor/ImportarS2p.cs`. Colocación: `ColocarS2pPuertas.Todo`.

## Qué quedó
- **Kit (KitS2c):** C01_Fachada, C02_Fachada y C03_Fachada sustituyen a las `_cy`. `Medir` ahora encuentra el vano (el agujero que atraviesa la
  pieza). Cada planta baja lleva su puerta cerrada y sin collider: PU02_Garaje en el local, PU01_Puerta en la vivienda.
- **C03 no lleva puerta:** es el piso alto, con dos ventanas y sin vano.
- **P11 + PU03:** en la entrada de la casa; la hoja de siempre (la que abre con E) se mudó al vano y lleva la reja. Vano 0.99 x 2.13 m. Los escalones
  de P11 quedan bajo la banqueta porque el piso de la casa está al nivel de la calle.
- **P12 + PU04:** cierra el frente del consultorio del Doc. Vano 1.45 x 2.00 m. El vano interior del consultorio se recorrió a x = -102 para que
  coincida. Puerta nueva `PuertaSinHorario`: abre con E a cualquier hora, para no dejar a nadie encerrado ni cambiar el horario de la clínica.
- Se rehicieron kit, calle de K01, Suburbios, Mercado, Neón Alto y Nova, y la luz y los postes de T-880 y T-881.

## Evidencia
- EditMode: 2112 de 2112.
- `FachadasPuertasS2pPruebas` (PlayMode): 3 de 3. Una cápsula del jugador choca con la puerta cerrada y cruza el vano con ella abierta.
- PlayMode de Mundo: 184 de 198. De las 14 fallas, 12 ya estaban en el reporte anterior (cámara libre, horarios del mercado negro, estacionamiento).
  `Gente_SeQuedaParadaConAnimadorVivo` y `Recorrido_Regreso_EnCarroPorLasEsquinasYCamaDeLaCasa` fallan igual con la escena de antes de esta tarea.
- Capturas revisadas: docs/capturas/s2p-b3-k01, k05, k06, p11-dia, p11-noche, p12-dia, p12-noche.
