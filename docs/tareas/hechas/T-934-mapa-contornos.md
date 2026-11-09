# T-934: mapa grande transparente

- `LineasMapa` dibuja en una sola malla los bordes de las calles y, más tenues, los contornos de los edificios. Se quitó la captura de la ciudad y los rellenos de las manzanas. El minimapa no cambia.
- Los dueños se siguen leyendo por el color del nombre de la zona; manzana y etiqueta conservan el dato.
- El I+D+I siempre sale con su icono y su nombre: si el objeto de la clínica está apagado, el catálogo lo busca también entre lo apagado y recuerda la última posición.
- El destino marcado no se borraba, pero el marcador en pantalla solo seguía al objetivo de la misión: ahora sigue también lo marcado, igual que el minimapa.

Pruebas: PlayMode `MapaContornosT934Pruebas`, `MapaPlayT747Pruebas`, `DestinosMapaPruebas`, `DestinosMapaCiudadPruebas`, `PausaMapaH10Pruebas`. Captura: docs/capturas/ui-mapa-contornos.png.
