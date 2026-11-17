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
