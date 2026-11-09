# T-920 Casi sin niebla de noche

- Qué: la niebla de noche pasó de 0.036 a 0.006 (igual que la del mediodía) para que se vea el fondo de la ciudad. De día no cambió (0.006-0.009).
- Dónde: curva densidadNiebla de ConfigCicloDia_Mundo.asset (llaves de 0, 4.5, 21 y 24 h), m_FogDensity de Ciudad_Prologo.unity, constante ColocarS2oLuz.DensidadNiebla (para que no regrese). Script: Assets/Arte/Editor/ColocarT920.cs (repetible, no rehace nada más).
- Pruebas ajustadas: CieloDiurnoPruebas.Noche_ConservaLosValoresDeAntes (0.006) y LuzK01S2oPruebas.Noche_NieblaAzulDelgadaYCieloLiso (<= 0.01), que fijaban el valor viejo.
- Evidencia: docs/capturas/t920-{antes,despues}-{dia,noche}-{av-neon,eje,av-coyote}.png (PlayMode con T92_CAPTURAS=1, prueba Assets/_Proyecto/Tests/PlayMode/Mundo/CapturasS2T92Pruebas.cs). Antes de noche la Av. Neón se perdía a ~60 m; después se ven las torres y el brillo verde del fondo.
- Decisiones: 0.006 y no 0: el color de niebla sigue fundiendo el horizonte con el cielo y la lejanía del telón. No se tocó FondoCiudad3D (menú) ni el clandestino.
