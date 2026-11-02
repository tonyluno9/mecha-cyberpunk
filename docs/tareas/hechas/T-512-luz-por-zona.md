# T-512 Paso 4 del plan de personajes y carros: iluminación por zona
Dueño: Tony | Rama: tony/T-512-luz-zonas | Modelo: Opus 5.5 | docs/plan-personajes-carros.md, paso 4
Hecho: colocador idempotente ColocarT512.Colocar (menú Mecha Cyberpunk/T-512/Luz por zona, Assets/Arte/Editor/), raíz T512_LuzZonas en Ciudad_Prologo. Todas las luces nuevas son puntuales sin sombra y solo se prenden de noche (LuzNocturna).
- Callejón: 5 lámparas N01 en los muros con luz cálida (12, 9 m) y 2 manchas de neón magenta (7, 8 m); se ven los objetos y sigue oscuro.
- El Mercado (pasillo y entrada): 5 lámparas N01 con luz cálida (8, 7 m), 2 neón magenta, y la luz naranja del portón de T-504 más fuerte (adentro 6 → 10, derrame 5 → 8, más alcance).
- El Tianguis: 18 puestos nuevos (N03a, N03b, C12, C13) en hileras a las dos orillas de la calle principal, con su collider, contra la banqueta, sin tapar el carril central (|z| < 3.6), sin chocar con nada y a 9 m o más de los carros estacionados.
- Lluvia ligera también de día: ConfigLluvia ganó fraccionDeDia (0.3: de día cae el 30 % de las gotas y de la niebla) y lluviaDeDia quedó en true; CalculoLluvia.Intensidad. Los charcos de T-501 a T-508 se ven de día.
- Azoteas: una luz de luna fría (18, 12 a 29 m) 3 m sobre cada techo de más de 7 m de alto: 105 luces. Los techos y fachadas del fondo se ven; el techo de lámina del frente de la captura sigue oscuro (material casi negro). Dos vueltas de ajuste (5 → 18 de intensidad).
Código: Arte/Editor/ColocarT512.cs; Mundo/Datos/ConfigLluvia.cs, Mundo/Ambiente/CalculoLluvia.cs, LluviaSigueJugador.cs y AmbienteSonoro.cs (área de Alan, autorizado por el plan); ConfigLluvia_Mundo.asset; Ciudad_Prologo.
Pruebas: EditMode Mundo 174/174 (LuzZonasT512Pruebas 5 nuevas, CalculoLluviaPruebas +2). PlayMode CiudadCarrosPruebas + Trafico 27/27 (los puestos no atoran carros ni tráfico). PlayMode Mundo 177/188: LluviaPruebas.Lluvia_AMediodia_CaeLigera y AmbienteSonoroPruebas.Ambiente_AMediodia_LluviaLigeraSinNeon_YTrafico se actualizaron a la lluvia de día (AmbienteSonoro baja el sonido de la lluvia con la misma fracción) y pasan; las 11 rojas son las mismas ajenas de T-509 (horario del mercado 24 h, cámara libre de Alan; CamaraLibreCarro.Bajar_Detenido cayó esta vez y RecorridoPrologoCiudad pasó: intermitentes). RecorridoDemoPruebas 1/2 (ver bitácora).
Capturas (editor vivo, Logs/cap512.sh, 1920x1080): docs/capturas/T-512-<zona>-comparacion.png (referencia | antes | después) para callejon, entrada-mercado, tianguis, azotea y dia-lluvia; mercado-pasillo (antes | después); y sus -antes/-despues.

## FPS (build de medición, 1920x1080, Medio, hora 23, Logs/med512/medicion.txt)
| Punto | FPS | 1% bajo |
| --- | --- | --- |
| calle | 166.9 | 97.6 |
| callejon | 276.3 | 143.8 |
| tianguis | 162.8 | 90.2 |
| mercado | 236.3 | 166.9 |
| mercado_puerta | 265.9 | 186.4 |
| suburbios | 301.8 | 210.1 |
Todos muy por encima de 60 (con 105 luces de luna nuevas).

## Bitácora
- 2026-11-02: hecho; capturas revisadas con Read. Con la cámara de captura dentro de una lona o un piso alto se cambiaron las vistas de mercado-pasillo y azotea antes de comparar.
- 2026-11-02: RecorridoDemoPruebas 1/2: DemoDePrincipioAFin se queda en El Tianguis (KO del bot en la pelea con pistola, el fallo conocido del paso 6). Builds/Demo y Builds/Juego regenerados (Result: Success).
