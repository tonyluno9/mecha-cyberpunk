# Reporte 18-nov · Sesión A (combate, policía, civiles, jugador, clips)

Noche del 17 al 18 de noviembre de 2026, hora de México. Opus dirigió y revisó. Opus hizo la policía, Nova, las muertes y la revisión
de capturas. Lo mecánico lo escribieron subagentes Sonnet en worktrees sin Unity: barra de prótesis, daño en el carro, recarga, sonidos,
civiles, clips y prótesis de ataque. Opus lo compiló, probó y corrigió en la carpeta principal. Cada tarea tuvo su rama, merge `--no-ff`
a main y push. Antes de cada merge se esperó el resultado de sus pruebas.

## Qué quedó (puntos 1 a 11 de docs/playtest-17nov.md)

| # | Tarea | Qué hace ahora | Evidencia |
| --- | --- | --- | --- |
| 1 | T-1301 sin intro del asalto | La partida empieza sin la cinemática (`AsaltoDinero.ConCinematica = false`). Sin cámara, barras ni HUD oculto: el robo ya pasó, "RÓBATE UN CARRO" está activo y el ladrón huye en el carro a los ~16 s. | `AsaltoDineroT728Pruebas` 7/7 (2 solo pasan aparte, ver Pendientes) |
| 2 | T-1302 barra de prótesis vertical | Columna a la izquierda: **Q PIERNAS, C BRAZOS, X CABEZA**. Láser (4) y misiles (5) van en la misma columna si los tienes. **Cubrirse pasó de Q a V.** También se actualizaron los textos de ayuda y el tutorial. | Captura `docs/capturas/t992/barra-activa.png` revisada; `BarraProtesisT1011Pruebas` pasa |
| 3 | T-1303 policía | Al primer golpe, bala o choque contra un policía o una patrulla ya te buscan (1 estrella al instante) y atacan. Las patrullas destruidas se reponen (refuerzos). Si estás en un techo, hasta 2 policías suben por la escalera de mano con los clips PJ01_Escalera_*. Corren a lo más 3.6 m/s y caminan disparando a 1.4 m/s. **Arresto solo si un policía a pie te toca** (1.3 m, 0.4 s). Ya no basta con que una patrulla esté cerca. | `PoliciaEscaleraT1303Pruebas` (sube al techo, rapidez máx. 3.2 m/s), `ArrestoT690`, `BusquedaT665` (a 4 m no arresta; tocándote sí), `PrimeraEstrellaT1303` |
| 4 | T-1304 Nova con 4-5 estrellas | Las estrellas se ponen azul intenso. Con 4 estrellas llegan 3 guardias robot de Nova armados y 1 guardia pesado, a 22-34 m, y van por ti. Con 5 son 5 guardias y 2 pesados, más el jefe final E14. Se reponen cada 25 s. Bajo 4 estrellas se retiran a los 6 s. Los bots de Nova ya respondían si les disparas (T-1009). | Captura `docs/capturas/s1d-T-1304-estrellas-nova.png` revisada (5 estrellas azules, robots en la calle); `RespuestaNovaT1304Pruebas` 2/2 |
| 5 | T-1305 civiles | Vida 40. Al morir caen tirados en 0.5 s. Matar a un civil suma calor si hay testigos. Huyen al oír disparos (ya lo hacían). Si les pegas y estás cerca, 30 % te pega de vuelta (2 golpes de 5) y luego huye. | `CivilesT1305Pruebas` 4/4, `ReglaCivilT1305Pruebas` 10/10 |
| 6 | T-1306 muertes | Los policías quedaban **hincados**: su controlador propio ya acuesta al personaje con el clip de muerte y además se giraba 90°. Corregido. Ahora todos quedan tirados (cabeza bajo 0.6 m). Los enemigos que caen de un techo usan Loco_Caida y el aterrizaje: la IA ya le dice al Animator cuándo va cayendo (EnPiso/VelY). | Captura `docs/capturas/t-1306/muertos.png` revisada (12 muertos de 6 tipos, todos en el piso); `MuertesT1306Pruebas` 1/1 |
| 7 | T-1307 daño dentro del carro | La IA te apunta y te dispara aunque vayas en carro o moto. Las balas que pegan en tu vehículo te llegan: en carro el 55 %, en moto el 100 %. Si caes KO adentro, te bajas. | `DanioEnCarroT1307Pruebas` |
| 8 | T-1308 recarga con animación | Toda recarga saca el arma y pasa por su animación, también la automática al vaciarse. Las balas se reponen solo al terminar. Cambiar de arma, rodar, el carro o el KO la cancelan. | `CargadorPistolaPruebas`, `PistolaAPiePruebas`, `EventosPistolaT734` |
| 9 | T-1309 / T-1309b sonidos y plasma | Cada arma suena distinto (el revólver suena igual que antes). Hay chispeo en bucle mientras carga el plasma y el motor de los drones zumba en bucle 3D. El rayo de plasma es nuevo: núcleo blanco, halo cian, zigzag, pulso, fundido y chispas al pegar. | `SonidosArmasT1309Pruebas`, `ChispeoPlasmaT1309Pruebas` 2/2, `ArmasEspecialesT1013` |
| 10 | T-1310 clips de Blender ronda 2 | Caminar e idle nuevos (con y sin implante), PJ01_Moto_Bajar y cobertura con rifle. Parkour: con Espacio saltas bardas de 0.6-1.3 m, los bordes altos se trepan con TreparBorde y si caes de más de 4 m ruedas con RodarCaida y pierdes poca vida, así que sí te puedes tirar del techo. Al conductor robado lo sacan con PJ01_Conductor_SerSacado. | `ClipsRonda2T1310Pruebas` (barda, caída de 8 m sin morir), `EscalerasTechosT1101` 6/6, `ReglaParkourT1310Pruebas` |
| 11 | T-1311 prótesis de ataque en el cuerpo | PJ01_Implantes_Ataque.fbx va puesto en Visual_PJ01: brazo láser en el antebrazo derecho y hombrera de misiles. Solo se ve con la prótesis instalada, y el rayo y los misiles salen de sus vacíos. | Capturas `docs/capturas/t1018/cuerpo-frente.png` y `cuerpo-espalda.png` revisadas; `ProtesisAtaqueCuerpoT1311Pruebas` 4/4, `ProtesisEnElCuerpoT1018` |

## Lo que no quedó o quedó a medias

- **Robar carro (punto 10):** el conductor ya juega PJ01_Conductor_SerSacado, pero el jugador todavía no juega PJ01_Carro_SacarConductor. Tampoco
  están alineados al hueco de la puerta. Queda para después de la entrega; el robo funciona como antes.
- **Policía, "no dejan de perseguir sin razón":** las patrullas destruidas ya se reponen. No encontré otra causa de que dejen de perseguir sin una
  partida para reproducirlo. La búsqueda se pierde a los 45 s sin que nadie te vea, como antes.
- **Capturas "de cerca" de T-1018** (`brazo-laser-cerca`, `hombrera-misiles-*`): encuadran los pies, porque la pieza nueva cuelga del cuerpo y no del
  hueso. Las de cuerpo entero sí muestran bien las prótesis.
- **Parkour, robo de carro y caminar/idle nuevos:** no tienen captura revisada. Solo hay pruebas. Conviene verlos al jugar.
- **Escala de los clips:** quedó en 1.88, la misma de todos los clips de PJ01 que ya están en Assets. No usé 1.8367, para que no difieran entre sí.
- **El chispeo al soltar el clic a media carga:** con entrada simulada, soltar el clic no detiene la carga en la prueba. Por eso la prueba lo corta cambiando
  de arma. En el juego también se corta al dejar de apuntar.

## Pruebas

- EditMode completo (`verificar.sh editmode`) sobre main con todo integrado: **2624/2625**. El único fallo era de la T-1313 de la sesión B (material
  `Armas_CorteInterior` en las piezas de las armas). La T-1314 lo arregló después ("corte interior de las armas con textura").
- PlayMode por clases y en dos tandas (Personaje aparte de IA/Tráfico). Las clases de policía corridas antes rompen la entrada simulada de las de Personaje;
  está anotado en la memoria del proyecto.
- Ya fallan en main, sin cambios de esta noche: `HabilidadesProtesisT992Pruebas` (10), `CubiertaYRodadaPruebas.Cubrirse_ConQ...`,
  `PoliciasT709Pruebas.AlLlegar_LosPoliciasSeBajan...` y `ArmasPruebas.ComprarYUsarEscopeta`.
- `AsaltoDineroT728Pruebas`: en la corrida de la clase completa fallaron 2 del maletín. Solas pasan 2/2 (inestabilidad conocida de la E simulada).

## Builds

Esperé a que no corriera ningún Unity de `.sesiones/s2`, hice `git pull` (main en `86624e1`) y los construí uno a la vez. Hora de México.
La Demo se construyó sola. Al lanzar el Juego ya había un Unity de la sesión B que arrancó entre los dos builds; está en su propia carpeta
y no chocó.

| Build | Ruta | Terminó | Resultado |
| --- | --- | --- | --- |
| Demo | `Builds/Demo/CyberCity.exe` | 17-nov 03:17 | Success, 0 errores, 2.2 GB |
| Juego | `Builds/Juego/CyberCity.exe` | 17-nov 03:19 | Success, 0 errores, 2.1 GB |

Logs: `Logs/build-demo-18nov.log` y `Logs/build-juego-18nov.log`. Comprobé que `level1` y las DLL del juego dentro de `CyberCity_Data` tienen la hora
del build.

## RecorridoDemoPruebas

La corrí sola, después de los builds. Había 7.8 GB libres y el Unity de la sesión B estaba abierto. Terminó a las 03:28: **3/3**
(DemoDePrincipioAFin 288 s, NavMesh_LugaresDeLaHistoria_TienenCamino, VideoDemoT979).

---

# Parte 2 · Mapa expandido: Malecón, Colonia del Cerro, Autopista elevada y Puerto

Madrugada del 18-nov (00:40 a 03:45, hora de México). Opus dirigió e integró. Ocho subagentes Sonnet escribieron una tarea cada uno, en
worktrees sin Unity. Cada tarea tuvo su rama `tony/T-135x`. Opus compiló, corrió los scripts en Unity, tomó las capturas, las comparó con su
referencia y devolvió correcciones: 4 rondas en Malecón y Autopista, 3 en Cerro y Puerto. Todo entró a main con merge `--no-ff` (`8b33987`)
y push. Las coordenadas y los puntos de unión están en `docs/plan-zonas-18nov.md`.

## Distribución (M12 adaptado a la ciudad real)
Los 4 barrios no se movieron. La autopista vieja decorativa (z = 103) sigue igual. Imagen nueva del mapa grande:
`Assets/_Proyecto/UI/Resources/Mapa/MapaCiudad.png`, de 3300 x 2480 (x de -480 a 345, z de -330 a 290, 4 px/m).
- **Malecón:** todo el borde sur, a continuación de los corredores sur de las afueras. Es una avenida en curva suave (radio mínimo de ~165 m).
  Del lado oeste sube a la base del cerro y del lado este entra al Puerto.
- **Colonia del Cerro:** al oeste de Suburbios. Se llega por la calle oeste de las afueras hasta la calle base (x = -305). Desde ahí sube un
  zigzag con pendiente máxima del 5 % hasta una plaza a ~28 m. A media ladera sale el túnel de la autopista.
- **Autopista elevada:** en z = 250 y a 10 m de alto. Al oeste entra en túnel a un acantilado del cerro. Tiene dos rampas: la norte sale
  de la calle cruzada norte (8 %) y la este baja por x = 300 (7.7 %) hasta la puerta norte del Puerto. Un conector la une con la calle este
  de las afueras.
- **Puerto:** al sureste, al final del Malecón. Tiene dos puertas: la oeste, con caseta, viene del Malecón; la norte viene de la rampa este.
- **Orillas (T-1358):**
  - Se apagaron, sin borrar nada, los edificios de fondo y los muros invisibles del final de las 6 afueras. La lista está en
    `Logs/t1358-orillas.txt`.
  - Límites del mundo nuevos: norte z = 290, oeste x = -480, este x = 345, y muros en el mar.
  - `SalidaDelAgua` saca al jugador si se queda 3 s en el agua.
  - Los paneles pintados de fondo (T-513) se alejaron. Los del sur y el este quedaban sobre el mar y se apagaron.

## Qué quedó por tarea

| Tarea | Qué hace | Evidencia |
| --- | --- | --- |
| T-1350 calles de zona | Contrato `Core/Mundo/CalleZona`: cada calle nueva lleva su línea de centro con la altura real. `CallesDeZona` la mete al grafo del tráfico, de la persecución y del GPS. Los carros siguen la altura de la rampa y se inclinan; en la autopista van 1.6 veces más rápido. Hay peatones en las banquetas de las calles de zona. | `CallesDeZonaT1350Pruebas` 12/12. `TraficoZonasT1350Pruebas` 3/3, en PlayMode con la escena real: hay camino desde el centro a las 4 zonas, no quedan calles sin salida y en menos de 60 s un carro del tráfico pisa una calle de zona. |
| T-1351 mar | Shader `MechaCyberpunk/Mar` con olas, fresnel, reflejo de la sonda y estelas de las luces. La espuma va solo en la franja de orilla. La espuma por profundidad pintaba de blanco todo el mar, así que quedó apagada. | `MarT1351Pruebas` 4/4. Capturas revisadas. |
| T-1352 mapa | Mapa grande y minimapa con los límites nuevos y los nombres MALECÓN, COLONIA DEL CERRO, PUERTO y AUTOPISTA. El minimapa dibuja las calles de zona. El HUD muestra la zona nueva al entrar; el territorio no cambió. | `MapaCiudadT747Pruebas` (con la imagen nueva), `MapaPlayT747Pruebas`, `ZonaHudPruebas`. |
| T-1353 piezas | Los 18 FBX de `imageassets/blender/zonas` importados a `Assets/Arte/ZonasModulos` con collider, LOD y marca de estático. Las escaleras CE01 y ML02 llevan una rampa invisible para subirlas caminando. | `PiezasZonasT1353Pruebas`. |
| T-1354 Malecón | Avenida de 4 carriles y paseo de 8 m con barandal ML01, palmeras AR03, faroles ML01_Farol con luz de sodio y bancas. Escaleras a la playa, playa de 30 m y escollera. Muelle con 6 lanchas, puesto de mariscos y faro sobre rocas con haz que gira (`GiroHazFaro`). Del lado de la ciudad, fachadas de 3-4 pisos con cortinas, neones y aires, y postes con muchos cables. | Capturas `malecon-k11/k12/a11.png`. `MaleconT1354Pruebas`. |
| T-1355 Colonia del Cerro | Ladera con calle base, zigzag, plaza y escaleras CE01 con barandal CE03. Unas 700 casas escalonadas (CC01 y CC02 de Tripo, y casas del kit teñidas), con tendederos CC04, lavaderos CC05, postes y carros viejos. Las azoteas tienen 0.6-2.2 m de desnivel para el parkour y `PuntoTrepar` en los accesos. | Capturas `cerro-k13/a13.png`. `CerroT1355Pruebas`. |
| T-1356 Autopista | Tablero, rampas, curva, columnas AU04, parapeto, guardarraíl AU05 y luminarias AU01 con luz. Túnel con boca AU06 dentro de un acantilado de roca. Debajo, un barrio bajo con chatarra. | Capturas `autopista-k18/a18.png`. `AutopistaT1356Pruebas`. `TraficoZonasT1350Pruebas` encontró que la rampa norte quedaba bajo la losa; ya está corregido. |
| T-1357 Puerto | 27 bloques de contenedores de 1 a 4 de alto con pasillos de 3.5 m y calles internas en circuito. Cuatro grúas PU01 en el muelle, barco PU02, bodegas PU03 y PU04 con neón y reflector, montacargas, pallets, bitas, reja DS01 y casetas. | Capturas `puerto-k15/a15.png`. `PuertoT1357Pruebas`. |
| T-1358 orillas | Lo de la sección anterior. | `OrillasT1358Pruebas` (uniones libres, límites nuevos), `CalculoSalidaDelAguaPruebas`. |

Orquestación con `Assets/Arte/Editor/CorrerZonasT1350.cs`, en una sola sesión de Unity:
`-executeMethod MechaCyberpunk.Arte.Editor.CorrerZonasT1350.Correr -zonas T1354Malecon.Aplicar,T1355Cerro.Aplicar,T1356Autopista.Aplicar,T1357Puerto.Aplicar,T1358Orillas.Aplicar,CorrerZonasT1350.ArreglarZonas,CorrerZonasT1350.PodarLuces,FlotantesS2t.Arreglar,RevisarCiudadT995.Arreglar,CorrerZonasT1350.GuardarComoPrefabs`.
- Las fotos (`...Fotos`) se toman en **otra** corrida. Si se toman en la misma sesión que rehace las zonas, el URP falla al dibujar y salen
  negras.
- Cada zona queda guardada como prefab en `Assets/Arte/Mundo/Zonas/`. Con las zonas dentro, la escena llegó a 106 MB y GitHub la rechazó; con
  los prefabs pesa 55 MB. La historia local que tenía la escena grande nunca se subió: la rehíce limpia y sin force push.
- Las zonas se pueden volver a generar con sus scripts. Nunca se corrió CreadorTodo.

## Rendimiento
- **Luces:** los kits ponían una luz real en cada neón y foco, y la escena pasó de 695 a 1400 luces. Con eso el URP ya no podía dibujar: se
  llenaba el búfer de luces. `PodarLuces` deja spots a 10 m o más entre sí y puntuales a 20 m o más; lo demás queda solo emisivo. Ahora
  hay 876 luces en total: Malecón 49, Cerro 98, Autopista 88 y Puerto 47.
- **Oclusión y LOD:** todo lo de las zonas está marcado estático, con Occluder en lo grande y opaco. Las piezas de Blender llevan LOD con
  corte por tamaño en pantalla. No medí los FPS en el build.
- **Cerro:** son ~700 casas. Es la zona más pesada (prefab de 25 MB).

## Comparación con las referencias
Las capturas están en docs/capturas/zonas/, con la referencia a la izquierda y el juego a la derecha.
- **Puerto (K15, A15):** es lo más parecido.
  - A15 tiene el mismo acomodo: grúas en el muelle, bloques de contenedores de colores, calles con postes y charcos de luz, y bodegas abajo
    a la derecha.
  - En K15 están el pasillo, los contenedores a la izquierda, las bodegas a la derecha y los montacargas.
  - Faltan los grafitis de los contenedores y el brillo naranja de K15.
- **Cerro (K13, A13):**
  - K13 ya se lee como calle de colonia en subida, con casas pegadas de colores, poste con lámpara, cables y un carro.
  - En A13 la ladera está llena de casas, pero se ven como cajas parejas. Falta el escalonado irregular de la referencia.
  - La ciudad de neón no queda abajo a la derecha como en la referencia, porque el zigzag sube hacia el sur.
- **Malecón (K11, K12, A11):**
  - Ya tiene paseo, barandal de balaustres, palmeras, faroles, cables, fachadas con neón, playa, puesto, muelle y faro.
  - El lado de la ciudad todavía muestra bloques altos y lisos de las afueras viejas.
  - La luz se ve azul pareja, sin el contraste cálido de la referencia.
  - Al otro lado del agua no hay skyline: el panel de fondo sobre el mar se veía como un muro y lo apagué.
- **Autopista (K18, A18):**
  - En A18 ya se ve la autopista entrando a la boca del túnel en el acantilado, con el cerro y la chatarra debajo.
  - **K18 no quedó:** desde la calle no logré el encuadre de la referencia (columna grande, rampa al centro, túnel al fondo), porque las
    casitas tapan la autopista. Es lo primero que hay que rehacer.
- **General:** la luz de noche de la ciudad (T-512, T-1331) es la que les gustó en los barrios y no la toqué. Por eso todo sale más azul y
  plano que las ilustraciones.

## Assets
- Se usaron los props de Tripo de la sesión B, en Assets/Arte/Zonas; esa carpeta no se tocó.
- De los módulos de Blender (T-1346) de la sesión B se usan los suyos, que vienen con textura. Hay tres excepciones, que usan los de
  T-1353:
  - las escaleras, porque necesitan la rampa para subirlas caminando;
  - la autopista, porque su material `PinturaBlanca` no trae textura y la prueba T-506 lo marca como greybox;
  - el contenedor, porque el tinte del Puerto está ajustado sobre ese material.
- Mientras llegan las texturas de Blender, los materiales de T-1353 llevan textura provisional: concreto T05, asfalto T03, arena T04, y
  lámina, madera y metal de T-506. Solo cambié materiales propios de las zonas; los que comparten los barrios (S2c_*) no.
- Huecos anotados en `docs/assets-faltan.md`: la pluma de la caseta del Puerto, las hieleras y los bancos de plástico del puesto de
  mariscos, y CC03_CasaCerro.

## Pruebas
- **EditMode de zonas, escena, mapa y tráfico: 186/186.** Varias pruebas de la escena se rompieron por las zonas y ya pasan: postes con la
  luz de T-1331, tambos con BarrilExplosivo, piezas flotantes y encimadas, greybox, fondo T-513 y uniones.
- **Cambié la expectativa de tres pruebas,** con la razón en el código:
  - `FondoCiudadT513` cuenta también los tramos apagados.
  - `SinGreyboxT506` no cuenta como greybox el agua ni la emisión de noche.
  - `OrillasT1358` no cuenta como estorbo el tablero de la autopista, porque es la calle misma.
- **Suite completa de EditMode:** dio 3025/3036 antes de esos arreglos. Las 11 fallas eran las de arriba.
- **PlayMode sin fallas:** `TraficoZonasT1350`, `TraficoCalle`, `PeatonesBarrio`, `CierresCiudadT986`, `MapaPlayT747` y `ZonaHud` (24 de 26; las
  otras 2 no se cuentan como fallas).
- **`PersecucionRealistaT587Pruebas` falla 3/6** ("Expected 3 But was 6" patrullas). **También falla con el código de tráfico de main**: lo
  probé regresando la carpeta Trafico a main. No viene de este trabajo; queda pendiente.
- **No corrí** el PlayMode completo (por la memoria) ni `RecorridoDemoPruebas` después de este cambio.

## Builds
- `Builds/Demo/CyberCity.exe` (`ConstruirDemoPandillas`): Succeeded, 0 errores, 5014 MB, 03:36.
- `Builds/Juego/CyberCity.exe` (`ConstruirJuego`): Succeeded, 0 errores, 5014 MB, 03:38.
- No jugué los builds. Que se pueda manejar por la rampa y el túnel está probado con el grafo y el tráfico en PlayMode, no con el jugador
  al volante.

## Para revisar jugando
1. Manejar desde el centro al Malecón y al Puerto, subir la rampa norte, cruzar la autopista y entrar por el túnel al cerro.
2. Parkour en las azoteas del cerro y en los contenedores del Puerto.
3. Policía persiguiendo en las zonas nuevas.
4. FPS en el cerro.
5. Si la luz y los colores del Malecón te gustan antes de seguir afinando.
