# T-727 Pruebas rojas de UI y de orden
Dueño: Tony | Rama: tony/T-727-pruebas-rojas-ui | Modelo: Sonnet 5.5, medium
Depende de: T-722 c, d y f

## Qué se encontró y qué se hizo
- DestinosMapaPruebas.Minimapa_MuestraElIconoDeCadaLugar: prueba vieja, no bug. El minimapa quita los íconos a menos de 35 px del centro para no taparle la flecha al jugador (a 2.17 px/m son 16 m); la prueba se paraba a 2 m. Ahora "Cerca" es 20 m.
- ZonaHudPruebas.Zona_AlCruzarDeZona: prueba vieja, no bug. El HUD nuevo (T-701) lleva el nombre de zona chico arriba al centro (TituloZona en CanvasH10), a propósito; la prueba lo contaba como cartel de lugar viejo. Se excluye ese texto.
- ColisionCochesT580Pruebas con Carro_V08: pasa 3 de 3 en esta rama (tras mezclar origin/main); no hay cambio de prefab. No se pudo reproducir la roja.
- PrologoCiudadPruebas (T-136): retirada. Recorría el prólogo viejo (pelea de campeonato, emboscada, flashback con DirectorPrologo, apagado desde T-400). La historia actual con DirectorPandillas ya la cubren HistoriaPandillasPruebas.RecorridoHastaContinuara y AsaltoDineroT717Pruebas.
- Orden: HistoriaPandillasPruebas cargaba Ciudad_Prologo y la dejaba viva; la siguiente prueba (otro ensamblado) veía dos HudAPie y un jugador de más (TelefonoMensajesPruebas, HudH10Pruebas, MarcadorObjetivoPruebas). Ahora descarga la ciudad al terminar (UnityTearDown, solo si la escena activa es Ciudad_Prologo; MaquinaDePasos pasó a UnityTest porque un UnityTearDown solo corre en corrutinas). Probado: Historia, Telefono, HudH10 y MarcadorObjetivo juntos, en ese orden, 18/18.
