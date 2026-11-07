# T-702 Pruebas viejas
Dueño: Tony | Rama: tony/T-701-hud-pulido (junto con T-701) | Modelo: Sonnet 5.5

## Qué se hizo
- TituloCreacionPruebas.RecorridoJugadorNuevo ya comparaba con NombreJuego.Nombre; fallaba porque PantallaTitulo.prefab guardaba "CYBERFIX". Arreglo en el juego: PantallaTitulo.Awake pone siempre NombreJuego.Nombre.
- HistoriaDemoPruebas.RecorridoDocInventorHacker y PersonajesApoyoPruebas.RecorridoProfes: sin implantes el Doc ahora dice la frase del regalo de RegaloImplanteDoc. Las pruebas la leen de RegaloImplanteDoc.Frase (propiedad nueva) y, si no hubo regalo, de DirectorPandillas.SubtituloDoc / ConfigSillaClinica.FraseDoc; ya no copian el texto.
- TelefonoMensajesPruebas: dependían del guardado real de la máquina y de que el jugador ya tuviera el control. Ahora usan una carpeta temporal (ServicioGuardado.RutaForzada) y esperan PantallaTelefono.PuedeAbrir.
