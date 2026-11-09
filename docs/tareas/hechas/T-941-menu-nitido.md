# T-941 Menú de inicio nítido (hecha)

- VolumenInicio.asset: ChromaticAberration y FilmGrain en intensidad 0 y apagados; CreadorEscenaInicio ya no los agrega.
- Quitada la capa `Lineas` de barrido de MenuInicio y el zoom 1.04-1.14 de FondoCallesInicio (la foto queda a escala 1).
- Quitados el Lema "SOBREVIVE A LA NOCHE" y su línea cian de LogoInicio (PantallaCargando sigue usando LogoInicio sin cambios) y el rótulo `MenuInicio.Version`.
- Resolución: la escala de render de URP (PC_RPAsset, Medio_RPAsset) es 1, así que la cámara del menú renderiza 1:1.
- Capturas: docs/capturas/ui-inicio.png y ui-inicio-aviso.png. Pruebas UI PlayMode (InicioPruebas, InicioTrazoH10Pruebas, MenuInicioS2mPruebas): 20/20.
