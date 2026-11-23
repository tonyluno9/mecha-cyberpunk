# T67 verificacion (verificador A, 23-nov)
- minimapa-pelea-tianguis.png y minimapa-zoom.png: en la pelea del Tianguis (paso GuerraSkulls forzado) el minimapa muestra puntos rojos. Solo se dibujan los que estan "en pelea" (EsHostil/Alerta/Objetivo), 2 de 20 pandilleros a menos de 30 m en esa captura.
- Nova violeta: NO confirmado. Los guardias de Nova (Guardia_Poniente/Oriente, refineria) tienen IMarcaMapa.Visible=True y su Marca_Nova (color violeta 0.62,0.4,1) existe con posicion correcta, pero su GameObject quedo activeSelf=False en cada muestra y no sale en minimapa-nova-zoom.png (solo puntos rojos de otros). Revisar Minimapa.DibujarMarcas con Nova (tiposMarcas/ordenes de Registro<IMarcaMapa>).
- Policia azul: no habia policias en la escena (aparecen con calor); no probado.
- Nota: la ventana de aviso (VentanaAviso) pausa el juego (timeScale 0) al llegar al paso del hambre; la cerre por codigo con VentanaAviso.Entender().
