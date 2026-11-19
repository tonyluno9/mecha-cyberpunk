# T-1462: la ciudad se ve bien desde arriba (antes T-1442)

Rama tony/T-1462-aereas (base integ/look-19nov-A). Brief: docs/brief-19nov-A.md. Cupo de luces: 0.

## Problema
Edificios negros que flotan, bloques gigantes, paredes azules lisas (A18), mancha blanca en la autopista (A18), torres que flotan en la niebla,
skyline del Malecón como panel plano sobre el mar (K11) y niebla que tapa la zona entera en las aéreas.

## Paso 1 (hecho): diagnóstico
`Assets/Arte/Editor/AereasT1462.cs`, método `Diagnosticar()`. Salida: `Logs/t1462-diagnostico.txt` y fotos `Logs/t1462-diag-*.png`
(candidatos grandes pintados; el % es del rectángulo del bounds, no descuenta oclusión).

## Causas encontradas
- Torres que flotan en la niebla y bloques negros arriba (A11, A18): paneles pintados T513_Fondo (Tramo_0 nova al norte, Tramo_3 suburbios al oeste), vistos desde arriba como placas. Tramo_3 era la aguja oscura sobre el mar de K11 (no era SK01: es aditivo y no tapa nada).
- Mancha blanca de A18 y velo lechoso: planos BrumaAltura (T1399_Bruma/Bruma_*, a 10 m sobre la calle; su opacidad crece con la distancia, así que desde 50-150 m son una losa blanca).
- Paredes azules lisas de A18: Acantilado (T1356) y Ladera (T1355) con la niebla exponencial a 100+ m; la niebla por altura las deja leer.
- Lo demás marcado A/B son calles, mares, cables y letreros fusionados en una malla (legítimos).

## Paso 2 (hecho, sin poder compilar: lo corre Opus)
- `Mundo/Reloj`: `CalculoNieblaAltura` (lógica pura: 1 hasta 12 m, 0.45 a 40, 0.35 a 80, 0.15 a 150, 0.1 a 250), `NieblaPorAltura` (publica el factor), `CalculoOcultarAltura` y `OcultarPorAltura` (apaga renderers sobre un umbral, con histéresis). `CicloDiaNoche.Aplicar` multiplica la densidad por `NieblaPorAltura.FactorActual` (la fija cada frame, así conviven).
- Pruebas EditMode: `Tests/EditMode/Mundo/NieblaPorAlturaPruebas.cs` (7).
- `ColocarS2cLook.Foto` aplica niebla y ocultado según la altura de la cámara de la foto y luego los restaura.
- `AereasT1462.Aplicar`: Tramo_3 apagado; raíz de escena `T1462_Aereas` con OcultarPorAltura para T513_Fondo (25 m), BrumaAltura (14 m) y Skyline SK01 (100 m); NieblaPorAltura en el objeto de CicloDiaNoche. Guarda la escena.
- `AereasT1462.Validar`: repite el diagnóstico aplicando cada altura; falla (Debug.LogError y Logs/t1462-validar.txt) si queda encendido un fondo sobre su umbral, Tramo_3 activo, un C/D de más de 1 % no permitido o la niebla fuera de lo pedido.
- Correr19nov: `AereasT1462.Aplicar`, luego `AereasT1462.Validar`; después `Vista19b.Fotos` en corrida aparte.

## Para la sesión B (Cerro y Puerto)
- T1355_Cerro/Terreno/Ladera (168x40x388 m) es una pared lisa vista desde arriba (A13, A15 y de fondo en A18). Los Cables y Extra_Cables del Cerro son mallas fusionadas enormes (legítimas).
- T1357_Puerto/Patio (203x3x193) y el Mar de T1357 (470x855): losas grandes, revisar que se vean bien con la niebla baja.
- T1355 y T1357 no se tocaron.

## Pendiente
- Sin ID por píxel: si tras Validar siguen bloques en A11 arriba a la izquierda, hace falta un segundo diagnóstico.
- Barrio/PulidoFinales/Autopista (Losa y Barandal de 340 m a 10 m de altura) es un resto viejo; no se tocó.

## Assets que faltan
(vacío)

## Ronda 2
- Vacío azul alrededor de la isla (A18, norte de A11, A13): era el fondo (color de niebla/cielo) donde no hay geometría, al apagar T513 por altura. Nuevo `CiudadRespaldo` (Aplicar): plano de 4000 m a y -8 (bajo el mar, FondoMar y los pisos de respaldo) con la textura T1363_VentanasLejanas (mosaico de 96 m), material `Assets/Arte/Mundo/Look/T1462_CiudadRespaldo.mat`, sin collider. `OcultarPorAltura` ganó `soloArriba`: el plano solo existe con la cámara sobre 25 m, así a nivel de calle nada cambia.
- `T1365_Uniones/PisoRespaldo_*` son losas de asfalto (S2c_Asfalto, liso 0.7, reflejan el cielo azul de noche); legítimas, se permiten.
- Permitidos nuevos en Validar: DerramesNeon, Estelas, T1464_Autopista, PisoRespaldo, Skyline (bajo 100 m), CiudadRespaldo.
- Cajas sobre el horizonte de A11: no pude nombrarlas con certeza. Validar ahora escribe una sección "ARRIBA" por cada aérea con los 30 renderers que más ocupan el tercio superior (ruta, malla, material, distancia).
- Pruebas EditMode: 9 en NieblaPorAlturaPruebas (2 nuevas de OcultoPorDebajo y soloArriba).

## Ronda 3
- Quitada `CiudadRespaldo` (la rejilla de ventanas). Ahora `T1462_Aereas/SueloLejano`: plano de 4000 m a y -8, URP Lit azul-gris muy oscuro, mate, sin reflejos (`Assets/Arte/Mundo/Look/T1462_SueloLejano.mat`); solo se ve con la cámara sobre 25 m. Aplicar borra el material viejo.
- Niebla: sobre 12 m de altura `NieblaPorAltura.AplicarNiebla` (la llama `CicloDiaNoche`) pasa a niebla Linear: inicio 0 a 200 m y fin 100 a 1200 m según la altura (calle = lo de siempre: exponencial cuadrática). Así los primeros 150-200 m se ven claros y el horizonte lejano se cubre poco a poco. Pruebas EditMode nuevas (Lineal y AplicarNiebla).
- Validar: permitidos nuevos (Autopista/Fondo/Torre_*, TorreReflectores, FondoMar, Asfalto, Arroyo: son ciudad lejana y calles legítimas, eran los 475 falsos positivos).
- Validar ahora mide "QUÉ SE VE" por píxel (id de color por renderer, pasada de opacos y pasada de transparentes) en el tercio superior (izq, centro, der) de A11, A13, A15 y A18, y en dos regiones de K12 (forma blanca sobre el mar y cruz en el cielo). Con eso se nombran las cajas negras de A13/A11 y lo blanco de K12 en la ronda siguiente.

## Ronda 4 (id por píxel)
- A13 arriba a la derecha: `T530_Afueras/Salida_*/Edificio` (cubos con MuroMundo, restos de las afueras viejas, base y 0 pero sin suelo alrededor) y SueloLejano. Ahora se apagan sobre 25 m (grupo `EdificiosAfueras`).
- A13 arriba a la izquierda: `T1356_Autopista/Fondo/Torre_*` (y TorreReflectores), cientos de piezas pequeñas (por eso no salían en el top 8). Se apagan sobre 25 m (grupo `TorresFondoAutopista`).
- Si siguen bloques negros en el borde superior de A13, la región nueva "franja-superior" de Validar (12 % de arriba, todo el ancho) los nombra.
- K12, forma blanca sobre el mar: `T1354_Malecon/T1413_Malecon/FaroLejano/ML04_Faro_Lejano/Visual` (malla tripo_node_3ebf74a8, 16.6x30.6x17.7, centro -60;20.3;-335, base y 5.0: flota 5 m sobre el mar) más los haces `T1413_Malecon/FaroLejano/Haz` y `T1404_Malecon/Faro/Faro_0/HazFaro_T1404/Haz`. Son de T-1413/T-1463, no míos.
- K12, cruz blanca en el cielo: dron `T1398_DronesAereos/Ciudad/Ciudad_NV01_2/Dron/NV01/Cuerpo` (y Rotor_*), centro -70;100;-200 (tarea de drones/tráfico, no mía).
- Validar: permitidos NucleosSodio (T1461), MuroFondo (interior del casino), T1355_Cerro/ y T1357_Puerto/ (de la sesión B). Ahora espera 6 componentes OcultarPorAltura.

## Ronda 5
- Paneles verticales con ventanas sobre el vacío (arriba a la derecha de A13): es el arco `T1354_Malecon/T1393_Malecon/Skyline/SK01_Este/Arco` (aditivo, a 850 m, centro 410;78;-203), el skyline pintado visto desde 68 m. Mío (Malecón). Ahora SK01_Este se apaga sobre 25 m (grupo `SkylineSK01_Este`); SK01_Oeste (skyline de neón sobre el mar de A11) sigue hasta 100 m.
- La torre alta de ventanas azul arriba al centro-izquierda de A13 es `T1356_Autopista/T1386_Autopista/Fondo/TorreReflectores` (centro -300;~50;110): se queda porque sale en A18 (la excluyó Opus de TorresFondoAutopista; Validar también la excluye). Si estorba en A13, ocultarla sobre 80 m.
- Validar espera 7 componentes OcultarPorAltura.
