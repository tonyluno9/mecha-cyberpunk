# T-546 Paso 4 del plan de la noche 3-nov: interiores con lo que hay (docs/referencias-interiores/)
Dueño: Tony | Rama: tony/T-546-interiores | Modelo: Opus 5.5
- Script: Assets/Arte/Editor/ColocarT546.cs (idempotente; todo cuelga de T546_Interiores en Ciudad_Prologo; lo viejo que estorba solo se apaga). Texturas generadas con Assets/Arte/Exteriores/Fuentes~/generar_interiores.py: 3 pósters, alfombra del casino (roja con grecas doradas), papel picado (alfa recortado) y pizarrón de gis. Materiales en Assets/Arte/Interiores/Materiales/.
- Habitación: cuarto de joven, no taller: techo a 3 m, sin cocina, sofá, mesa ni tubos de taller; cama H02, buró H11 con lámpara, tele H06 sobre el banco que hace de escritorio, foco colgante cálido (A05 a 0.4), neón magenta y turquesa por la ventana, 3 pósters planos.
- Consultorio del Doc (I+D+I): luz turquesa, camilla, monitores, estantes, servidor; El Doc junto a la camilla.
- Taller de Tino (CIC): bancos T03, tablero T01, repisa T08, drones CY07b colgando, series de focos, pizarrón; Tino junto al banco.
- Casino: alfombra estampada encima del piso, 18 tragamonedas de adorno en hileras (sin scripts ni focos; la que cobra es la de siempre), barra, mesa del Hacker al fondo, papel picado, luces doradas y magenta.
- Entrada de armas de El Mercado: mostrador, cajas, tambos, reja, papel picado, series de focos.
- Arreglo de paso: los materiales URP/Lit con `globalIlluminationFlags = None` pierden `_EMISSION` al reimportarse (MaterialPostprocessor de URP). Les pasaba a los letreros L10-L14 del paso 2 (de noche ya no brillaban tras reimportar): ahora RealtimeEmissive en ColocarT542 y ColocarT546 lo arregla en los .mat.
- Lo que falta (sin cubos, hueco limpio): docs/interiores-faltan.md. Lo más visible: la habitación sigue siendo la caja grande del greybox, el brazo robótico del Doc, las mesas de póker y el candil del casino, la vitrina y el tablero de rifles del mercado.
- Pruebas: EditMode InterioresT546Pruebas (7) y AssetsNuevosT540Pruebas y PantallaLedPruebas: 12/12. PlayMode: RecorridoDemoPruebas 2/2, CasinoInteriorPruebas 7/7, TragamonedasMundoPruebas 9/9, PrologoMundoPruebas 5/5, MercadoT504PlayPruebas 3/3.
- FPS de noche (23 h, Medio, 1920x1080, RTX 5070 Laptop; puntos nuevos en MedicionNoche): casa 489 (1 % bajo 343), casino_dentro 415 (294), consultorio 433 (294), tino 319 (232), mercado_puerta 229 (118).
- Capturas: docs/capturas/interior-habitacion-, -consultorio-doc-, -taller-tino-, -casino-interior- y -entrada-armas-mercado-comparacion.png (izquierda la referencia, derecha el juego).
- Builds: Builds/Demo (ConstruirDemoPandillas) y Builds/Juego (ConstruirJuego) regenerados, los dos con "Build Finished, Result: Success".
