# T-507 Pulido visual, punto 2: iluminación y color
Dueño: Tony | Rama: tony/T-507-luz-color | Modelo: Sonnet 5.5 | Logs/reglas-plan-ciudad.md, "Pasada de pulido visual"
Hecho: colocador idempotente `ColocarT507.Colocar` (menú Mecha Cyberpunk/T-507/Luz y color, Assets/Arte/Editor/). Qué lavaba las fachadas de noche: ambiente plano azul 0.27/0.29/0.58, luna 0.55, exposición +2.2 del PerfilNoche y 54 farolas de luz de punto (intensidad 160, rango 22) que bañaban cada fachada. Ahora: ambiente 0.38/0.33/0.52 (solo noche, el día no cambia), luna 1.0 y exposición 2.8 (con las luces nuevas); farolas como focos naranja hacia abajo (130 grados, rango 26, 700) bajados a 6.8 m para que la cabeza del poste no tape su sombra, 14 con sombra suave (las más cercanas a la ruta, 26 m, nivel Bajo del atlas) y el resto sin sombra; fachadas del kit (C01, C02, C03, C20a, C20b, C21, C22, C23, Fachada_Alta) y T506_Barrio_Edif/Pobre con 6 pinturas (terracota, turquesa, crema sucio, ocre) elegidas por edificio; Volume local por zona con ColorAdjustments y WhiteBalance (Suburbios cálido, Callejón y Tianguis magenta con el nuevo VolumenTianguis, Neón Alto verde; blend 22/8/16/8). Restos de T-506: los charcos Barrio_Charco (50, cuadrados negros opacos rotados, eran el "plano negro inclinado" junto al paso de cebra) ahora son un óvalo suave transparente; los 2 barriles lisos del callejón se cambian por el tambo C16; PistolaEnPiso (DirectorPandillas) pasa de cubo amarillo a pistola de piezas (corredera, cañón, empuñadura y tira de neón) con material de arma. No se encontró en Assets/Arte un modelo de pistola. Los "hologramas naranja en bloques" de El Mercado no se pudieron identificar: lo más parecido es el trompo de los puestos de tacos (modelo T-419), sin cambio.
Pruebas: EditMode Mundo completo 159/159 (LuzColorT507Pruebas 10 nuevas; se actualizaron CieloDiurnoPruebas, TianguisT503Pruebas y AmbientePruebas), PlayMode Mundo con LuzColorT507PlayPruebas 5 (ver bitácora), RecorridoDemoPruebas (ver bitácora). Capturas: docs/capturas/pulido-<zona>-comparacion.png (suburbios-noche, callejon, tianguis, entrada-mercado, neon-alto), -antes, -despues y pulido-dia-calle-despues.png.

## FPS (build de medición, 1920x1080, Medio, hora 23)
| Punto | FPS | 1% bajo |
| --- | --- | --- |
| suburbios | 284.2 | 202.5 |
| callejon | 269.4 | 144.7 |
| tianguis | 157.3 | 102.5 |
| mercado_puerta | 244.5 | 115.1 |
| neon_calle | 235.1 | 175.6 |
| calle | 160.6 | 115.0 |
| clandestino | 197.3 | 106.4 |
Todos muy por encima de 60.

## Qué se ajustó (3 vueltas)
1. Primer pase: demasiado oscuro (ambiente 0.10) y los focos con sombra no alumbraban: la sombra de la cabeza del poste tapaba todo; se bajó el foco 0.6 m y se subió la luz.
2. Neón Alto teñía El Tianguis (blend 18 en su caja): blend 8 y prioridad 6 para el Tianguis; los T506_Barrio_Edif multiplicaban su azul/morado: ahora toman el color de la paleta.
3. Más fondo en fachadas: ambiente 0.38, luna 1.0, focos 130 grados y 700, exposición local de Suburbios +0.25. Queda por debajo de la referencia en niebla y lluvia; de día quedan edificios morado y azul de otros materiales (no tocados).

## Bitácora
- 2026-11-01: hecho (ver pruebas de arriba).
