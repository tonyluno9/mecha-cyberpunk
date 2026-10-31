# T-447 Día y noche conectados: más peatones de día, más pandilleros de noche
Dueño: Tony | Rama: tony/T-447-dia-y-noche | Modelo: Sonnet 5.5 | Sesión A (docs/sesion-A.md, prioridad 8)
Hecho:
- Peatones: de día 14 y de noche 6 (antes 6 y 8): `ConfigPeatones_Trafico.asset` cambiado con `CreadorPeatones.AjustarDiaNoche` (menú "Mecha Cyberpunk/T-447 Peatones de día y de noche") y los creadores `CreadorPeatones` y `CreadorCiudadPandillasT417` crean 14/6. `GrupoPeatones` ya prendía los primeros N según `reloj.EsDeNoche`.
- Pandillas: las del modo siempre (la demo) ya no están completas todo el día: de día queda `FraccionDeDia` (0.34, al menos uno: 1 de 3) y de noche salen todos; al amanecer los sobrantes se retiran en cuanto no estén peleando (`PandillaAPie.RetirarSobrantesDeDia`). Sin reloj salen todos. Las pandillas de solo noche no cambian. Dato nuevo `DatosPandilleroAPie.FraccionDeDia` (los assets traen el valor por defecto).
Pruebas: PlayMode `PandillaDiaNochePruebas` (3: fracción de día / todos de noche / retiro al amanecer; sin reloj salen todos; solo noche no sale de día); `RutasBanquetaPruebas.Config_TieneMasDeDiaQueDeNoche...` (roja desde T-418) puesta al diseño nuevo.
