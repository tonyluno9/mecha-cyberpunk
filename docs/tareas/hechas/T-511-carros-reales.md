# T-511 Paso 2 del plan de personajes y carros: carros reales y todos robables
Dueño: Tony | Rama: tony/T-511-carros | Modelo: Sonnet 5.5 (importación y colocación) y Opus 5.5 (cierre y pruebas) | docs/plan-personajes-carros.md, paso 2
Hecho: V02, V03, V04, V05, V06 y V08 importados con el flujo de T-250 (ImportarT511.CrearTodo: Visual con frente a +Z, largo real, pies en y=0, URP Lit, Mesh LOD, BoxCollider); con V01a y V01b son 8 modelos. Un carro manejable por modelo (Vehiculos/Prefabs/Carro_V*.prefab, CreadorCarrosT511): ruedas de greybox ocultas pero girando para la física, sin cilindros visibles, cámara del carro a la altura de cada modelo; un carro sin conductor que lleva un rato quieto se duerme (no gasta física).
Ciudad (ColocarT511.Aplicar): los carros de adorno Carro_N del barrio quedan apagados (los de la autopista elevada siguen: no se alcanzan) y en su lugar hay un carro robable con alarma y calor, de los 8 modelos con baraja de semilla fija; los 6 robables de T-401 se quitan. Cada carro se prueba en su lugar, girado al revés y corrido por la calle hasta que no choca con nada y tiene 10 m libres adelante; el lugar 1 (en el retén de Neón Alto) no cabe con ningún modelo y queda vacío: 16 carros robables.
Tráfico (CreadorTraficoT511): CarroTrafico_V*.prefab con cuerpo cinemático; TraficoCalle reparte los 8 modelos. Junto a uno detenido, E lo roba: el conductor se baja y huye (ConductorHuye, sin collider), sube el calor y el carro pasa a física de carro del jugador. Uno en marcha no se roba.
Pruebas: varias pruebas cargaban una escena con .inputactions y la dejaban viva o no limpiaban los mapas de entrada, y las que seguían dejaban de ver el teclado. Se agregó MapasDePruebaVehiculos/MapasDePruebaTrafico (copia de Progresion.Pruebas.MapasDeAccionesDePrueba) y un UnityTearDown que vacía la escena en CiudadCarros, RecorridoCarro, RobarCarro, RobarModelos y TraficoRobar.
Código: Arte/Editor (ImportarT511, ColocarT511), Vehiculos (CarroViejo, CamaraCarro, Editor/CreadorCarrosT511, Prefabs/Carro_V*), Trafico (CarroTrafico, ConductorHuye, TraficoCalle, Editor/CreadorTraficoT511, Prefabs/CarroTrafico_V*), Prologo/Editor/CreadorCiudadPandillasT451, Ciudad_Prologo.
Pruebas: EditMode Vehiculos+Trafico+Mundo 277/277 (incluye CarrosRealesPruebas). PlayMode Vehiculos 42/43 (CiudadCarrosPruebas 2/2: los 16 se roban, suben el calor y salen 4 m o más en 2 s sin atorarse; RobarModelosPruebas 9/9); la que falla es MontableCarroPruebas.Camara_PrimeraPersona, roja desde antes de T-511. PlayMode Trafico 25/25 (TraficoRobarPruebas 3/3; PeatonesBarrioPruebas ahora ignora triggers, los volúmenes de zona no son obstáculos).
Capturas: docs/capturas/T-511-modelos.png (los 8 modelos) y T-511-calle-noche.png (calle principal de noche con los estacionados, del build de medición).

## FPS (build de medición, 1920x1080, Medio, hora 23, Logs/med511/medicion.txt)
| Punto | FPS | 1% bajo |
| --- | --- | --- |
| calle | 170.8 | 103.3 |
| tianguis | 171.4 | 99.0 |
| carros | 195.1 | 137.3 |
| neon_calle | 247.5 | 119.0 |
| suburbios | 319.4 | 222.0 |
Todos muy por encima de 60 (T-510: calle 150.0, tianguis 146.9).

## Bitácora
- 2026-11-02: importados y colocados (commits f556289, 745bcf0).
- 2026-11-02: CiudadCarrosPruebas fallaba por la prueba, no por los carros: la cámara medía antes de asentarse, un Release suelto no siempre se aplicaba, el carro manejado empujaba al siguiente y el calor alto tumbaba al jugador. Arreglado en la prueba (calor en 0 por carro, carros de vuelta a su lugar, teclas que se sueltan de verdad).
- 2026-11-02: RecorridoDemoPruebas 1/2: NavMesh pasa; DemoDePrincipioAFin se queda en El Tianguis (KO del bot en la pelea con pistola, 6 bugs anotados; el fallo conocido del paso 6). Builds/Demo y Builds/Juego regenerados (Result: Success).
- 2026-11-02: pendiente para Tony: V05 (patrulla sedán) y V06 (patrulla camioneta) salieron del generador con la misma malla y textura, y V08 es la moto patrulla; los tres salen como carros estacionados de civiles porque el plan pide los 8 modelos al azar.
