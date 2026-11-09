# Reporte de la Sesión A (playtest del 9-nov)

Toda la lista de la Sesión A de `docs/playtest-09nov.md` quedó hecha e integrada en `main`, rama por tarea. El build es `Builds/Demo/CyberCity.exe`.

## Qué cambió

| Tarea | Pedido | Resultado |
| --- | --- | --- |
| T-930, T-945 | Quitar aliados | No queda código, HUD, mapa ni objetos en la ciudad. En la historia, el paso de Chato avanza solo. |
| T-931 | Rayos y marcas | Sin trazos de jugador, enemigos, policía ni dron, y sin el láser de aviso. Los huecos solo van sobre superficies visibles y se apagan si la superficie se mueve o se va. |
| T-932 | Fuego en carros | Causa: los sistemas del efecto emitían por distancia recorrida. Un carro intacto ya no emite; el fuego real va pegado al carro. |
| T-933 | Policía | Solo te busca si una patrulla o un policía te ve. Cámaras y civiles fuera. Hay patrullas de ronda sin sirena, que van contra los rateros que atacan. Sin búsqueda, los policías no te atacan. |
| T-934 | Mapa grande | Transparente, solo contornos turquesa, iconos y ruta. El I+D+I siempre sale, con nombre. El destino se sigue viendo al cerrar: también lo apunta el marcador en pantalla. |
| T-943 | Respawn, giro y muerte | El Doc no atendía en partidas nuevas: la historia se quedaba en `PeleaTutorial`. Giro hacia donde disparas por 0.6 s. Muerte más rápida: 1.2 s de caída y 1.4 s de pantalla. |
| T-936 | Disparar agachado | Se para solo y dispara con la misma pulsación, con daño. |
| T-935 | Vida | Recupera 3 por segundo tras 8 s sin daño (antes 8 y 4). |
| T-944 | Salto | Causa: en la cima se creía en el suelo y aterrizaba en el aire. Con el arreglo se ven las tres fases. No hizo falta nada de Blender. |
| T-937 | Conductor robado | Causa: su propio rayo al piso chocaba con su cápsula y lo subía. Ahora sale, cae, se levanta y huye. |
| T-938 | Drones | Desde el nivel 2. Salen a 90 m, llegan volando y no se atraviesan. |
| T-939 | Asaltos | Ataques rivales: de 7-13 min a 35-65 min, nunca menos de 10 min. Refuerzos: de 60 s a 600 s. |
| T-940 | Ruedas | V01a y V01b radio 0.34, V05 y patrulla 0.32. Motos sin cambio. |
| T-941 | Menú de inicio | Sin grano, aberración, barrido ni zoom de fotos. Sin lema, línea bajo el logo ni rótulo "DEMO · CYBERCITY". |
| T-942 | Banco de Tino | Escopeta, fusil y francotirador en 3D con cargador, cañón y mira que se separan. Interfaz sin cambio. |

Capturas revisadas en `docs/capturas/`: `ui-mapa-contornos`, `ui-inicio`, `ui-ruedas-*`, `ui-banco-*`, `ui-salto-*`.

## Decisiones para revisar

- "Logo de abajo" lo interpreté como la línea bajo el logo y el rótulo inferior. El logo grande se quedó.
- En el banco, las otras armas son una sola malla: cargador y cañón son piezas holográficas simples. Partir la malla dejaba bordes rasgados. Con piezas separadas en Blender se verían mejor.
- Son 3 patrullas de ronda. Si te ven robar un carro te persiguen: es la regla pedida, pero se notará.
- El mapa muestra al dueño de cada zona por el color de su nombre, no por relleno.

## Pruebas

- EditMode: 2264 de 2264.
- PlayMode: por clases de cada tarea, en verde. Como pediste, no se corrió PlayMode completo.
- Fallan también en la base, sin estos cambios: `CiudadCarrosPruebas.Ciudad_CadaCarroEstacionado...` y `PatrullaFisicaT707.Patrulla_EsUnCarroSolido...`.
- `BalasCiudadT711.EnElCallejonDeLosSkulls...` también falla en la base, según la corrida de Sonnet.
- `MinimapaPruebas.RecorridoMapaYPunto` pasa aislada y falla detrás de otras clases, igual que antes.
- `RecorridoDemoPruebas` se ajustó pero no se corrió.
- El build arrancó 25 s sin excepciones, sin gráficos. Falta jugarlo de principio a fin.

## Incidente

El merge de T-934 subió a `main` con una prueba EditMode fallando (el catálogo aún esperaba "EL DOC"), porque lancé el merge junto con las pruebas. Se corrigió en T-940. Desde entonces esperé el resultado antes de integrar.
