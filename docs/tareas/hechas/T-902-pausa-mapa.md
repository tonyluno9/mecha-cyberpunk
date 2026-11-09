# T-902: pausa y mapa grande sobre la guía H10

- Pausa: velo, cabecera PAUSA con el dinero real, panel de opciones a la izquierda (misma lógica: CONTINUAR, GUARDAR, CARGAR, OPCIONES, SALIR AL TÍTULO con confirmación, `Time.timeScale` 0, `PilaPantallas`) y panel SITUACIÓN a la derecha con zona, dueño (color de pandilla), objetivo, hora y día del juego. Se viste por código al despertar: el prefab no se toca.
- Mapa: la vista del mapa crece (escala 1.59) y ocupa todo el centro; cabecera con el dinero, barra de teclas y panel lateral angosto con la leyenda (colores de pandilla y rombo del objetivo sin cambio) y los negocios, una fila por negocio.
- Kit: `VestidoH10` (cabecera, pie, panel y fila H10 para pantallas armadas con el kit viejo) y `BotonCyberfix.UsarFondoReposo`.
- Decisión: el velo de la pausa va al 93 % (la guía dice 82 %) para que el HUD de atrás no estorbe la lectura.

Pruebas: `PausaMapaH10Pruebas` (nueva), `PausaPruebas`, `MenusCyberfixPruebas` (la aserción del color elegido pasó al turquesa H10), `OpcionesPruebas`, `DestinosMapa*`, `MapaPlayT747Pruebas`, `MinimapaPruebas`, `NegociosPruebas`. Capturas: docs/capturas/ui-pausa.png y ui-mapa.png.
