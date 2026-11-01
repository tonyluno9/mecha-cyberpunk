# T-505 Paso A5: Neón Alto por fuera como la referencia (y cierre de la Parte A)
Dueño: Tony | Rama: tony/T-505-neon-alto | Modelo: Sonnet 5.5 | docs/plan-ciudad-ui-personajes.md, paso A5
Hecho: colocador idempotente `ColocarT505.Colocar` (menú Mecha Cyberpunk/T-505/Colocar Neón Alto, Assets/Arte/Editor/) que rehace la raíz `T505_NeonAlto` en la calle cruzada de Neón Alto de Ciudad_Prologo (fachadas x 37 y 57, z 8 a 86; casino en el muro este a z 47): 2 tramos de autopista elevada N02 (z 38 y 72, escala x 2 para que los pilares queden dentro de los edificios, sin collider, cubierta a más de 7 m, tira de neón verde y magenta debajo), 2 pantallas gigantes E03 pegadas a las fachadas (planas, con brillo propio), 8 anuncios CY02 y 7 N08 pegados a las fachadas con brillo propio, 18 piezas de valla N07 (escala 0.3) en las orillas con huecos de cruce, la entrada E01 del casino mejorada (15 foquitos cálidos, 3 tubos de neón magenta y turquesa, alfombra roja, 2 cargadores CY10 y una moto C19; el E01, la puerta y el horario no se tocan), 6 luces sin sombra (verde ácido, magenta y cálida), 8 charcos de color, 7 capas de niebla verde, 14 torres de fondo con el shader TorreHorizonte (pantallas verde, magenta y E03, antenas con luz roja) y un posproceso local verde (PerfilNeonAlto, VolumenSigueNoche: solo de noche). Lluvia: la existente (LluviaSigueJugador). Corredor de la calle a la puerta del casino (x 37 a 57.5, z 44.5 a 49.5) y ruta a EL HACKER (x 74.5, z 47) libres de colliders de T505. Sin importación nueva: N02, N07, N08, E01, E03, CY02, CY10 y C19 ya estaban. Extra: `MedicionNoche` trae el punto `neon_calle` (47, 0.2, 12) mirando al norte.
Pruebas: EditMode NeonAltoT505Pruebas 10/10 (con T-501 a T-504: 41/41), PlayMode NeonAltoT505PlayPruebas 4/4 (con T-501/503/504: 13/13), RecorridoDemoPruebas (ver bitácora). Capturas: docs/capturas/ref-neon-alto-noche-antes, -despues y -comparacion (referencia, antes, después).

## FPS (build de medición, 1920x1080, Medio, hora 23)
| Punto | Antes | Después | 1% bajo después |
| --- | --- | --- | --- |
| neon_calle (47, 12 mirando al norte, la vista de la captura) | 359.9 | 281.3 | 172.0 |
| neon (50, -4, entrada de Neón Alto desde la calle principal) | 299.6 | 286.7 | 182.3 |
| vivacoin (frente al casino) | 405.3 | 343.2 | 199.2 |
Muy por encima de 60. La tabla de toda la Parte A está en docs/estado-tony.md.

## Qué se ajustó (2 vueltas)
1. Primer pase: demasiado verde (el filtro del volumen tiñó todo), el magenta casi no se veía.
2. Segunda: filtro 0.92/1.08/0.96, anuncios con emisión 2, luces magenta 7 y 8 de intensidad. Queda por debajo de la referencia en densidad de pantallas grandes y de lluvia en el cielo; la entrada del casino ya traía E01 y el cuerpo amarillo de VivaCoin domina la fachada este.

## Bitácora
- 2026-11-01: hecho; las fachadas ya traían azulejos planos de T-419 que bloqueaban toda pieza colgada, por eso `LibreFachada` los ignora (delgados en x y largos en z).
- 2026-11-01: RecorridoDemoPruebas 1/2 en dos corridas: el bot cae KO en la pelea con pistola de los SKULLS del Tianguis (x -37, z 76 a 80, a 100 m de Neón Alto; mismo fallo intermitente de T-501 y T-504) y nunca llega al tramo de EL HACKER, así que ese tramo no corrió. Neón Alto se comprobó aparte con NeonAltoT505PlayPruebas: cápsula del jugador de la calle a la puerta del casino y a x 74.5 (EL HACKER) sin chocar con T505.
