# Assets que faltan (playtest 17-nov-b)

El código ya está listo con un hueco para cada uno. Cuando llegue el asset se conecta en ese hueco; no se sustituyó nada con primitivos ni con otros modelos.

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| Camioneta de policía | 5 estrellas | `GestorPersecucion.prefabCamioneta` (hoy salen patrullas normales) |
| Nave/helicóptero que baja policías o enemigos con sogas | 5 estrellas y Nova | No se hizo nada (falta el modelo) |
| Sonido de radio de policía | Patrullas que llegan o vigilan | `ConfigPersecucion.IdSonidoRadio`, vacío. Al llegar: agregar el ID a CatalogoSonidos y a `ReglaMezclaSonidos` con sus pruebas |
| Sonido especial de llegada de Nova (más de 5 estrellas) | Aviso | `ConfigRespuestaNova.SonidoLlegada`; hoy usa `PAN_JEFE_ENTRADA` |
| Sonidos de enemigos nuevos (Nova, escudo), dron (vuelo y explosión), patrulla mejorada, otra explosión | Sonido | Sin hueco nuevo |
| Arma especial de Nova que dispare | Enemigos de Nova | No se hizo (falta el modelo) |
| Carros con cofre y puertas por separado (o carrocería rota) | Que la granada los deje "en pedazos" | `DanioCarro.LanzarPiezas` ya suelta hijos `Puerta*`, `Cofre*` y `Capo*` si existen; hoy solo salen las llantas |
| FBX con ruedas separadas para V07 y V08 | Ruedas que giren | V01a, V01b, V05 y la patrulla ya usan la rueda de V09 |
| Animaciones de NPC (platicar, gesticular, asustarse) | NPCs | No hay clips NPC_* en el proyecto; se usan `AcuerdoCompleto` y `Enojo` |
| Más NPCs de distintos tipos | Variedad | No se hizo |
| Parque con árboles (referencia K09) | Ciudad | 18-nov B: prefabs listos en `Assets/Arte/Zonas/PZ/Plazas/` (T-1341); falta colocarlos en la ciudad (sesión A o Tony) y el piso de adoquín |
| Texturas de techos y calles | Ciudad | Pedidas al chat de arte |
| Maletín del dinero (MA01 por Tripo) | Atraco | T-1499: importado desde imageassets/modelos3d/MA01_Maletin.zip (ImportarMaletinT1499); ya no falta |
| Lugares nuevos | Ciudad | Pedir referencia a Tony antes de armarlos |

## T-1337: mira del rifle de plasma (W07)

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| Pieza de mira propia en el modelo W07 (el FBX de Tripo es una sola malla; la "Mira" del banco es un corte de región) | Una mira visible en el arma | Sin hueco: hoy la mira es la línea de `MiraLaserPlasma` en `ArmasEspecialesAPie` |

## T-1357: Puerto

Cada pieza se busca por nombre; si no esta, ColocarT1357Puerto deja un Hueco_CODIGO vacio. Al 18-nov no existia ninguna en la rama.

| Codigo | Que | Origen |
| --- | --- | --- |
| PT01_Contenedor40, PT02_BitaAmarre, DS01_RejaPuas | Contenedor de 40, bita, reja | Blender (T-1353, ZonasModulos/Prefabs) |
| PU01_Grua, PU02_Barco, PU03_Bodega, PU04_Bodega, PU05_Montacargas, PU06_Pallets | Grua, carguero, bodegas, montacargas, pallets | Tripo (Assets/Arte/Zonas) |
| LO04_Caseta | Caseta de la entrada U5 | Solo si existe en Assets/Arte/Zonas |
| PluminaEntrada | Pluma de la entrada | Sin modelo, siempre hueco |
## T-1356: Autopista

El script `ColocarT1356Autopista.Aplicar` deja un `Hueco_<CODIGO>` (GameObject vacío, sin malla) cuando falta la pieza; la lista exacta de cada corrida queda en `Logs/t1356-autopista.txt`.
Al escribir la tarea no existían `Assets/Arte/ZonasModulos/Prefabs/` ni `Assets/Arte/Zonas/`, así que se esperan estos huecos hasta que lleguen las piezas.

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| AU01_AutopistaRecta (Blender) | Tablero recto z = 250 | Sin hueco: se ve el tablero generado |
| AU04_ColumnaAutopista (Blender) | Columnas cada 24 m | `Hueco_AU04` en `T1356_Autopista/Columnas` |
| AU05_Guardarrail (Blender) | Guardarraíl a los dos lados | `Hueco_AU05`; se ve el parapeto generado (con collider) |
| AU06_EntradaTunel (Blender) | Bocas del túnel | `Hueco_AU06` en `T1356_Autopista/Tunel` |
| AU01_Luminaria (Tripo) | Luminarias cada 30 m | `Hueco_AU01` en `T1356_Autopista/Luminarias` |
| T20, T21, T23 (kit de torres) | Fondo al norte | `Hueco_FondoTorres` si no están |
## T-1354: Malecón

Cada hueco es un GameObject vacío `Hueco_<CODIGO>` bajo `T1354_Malecon`; Logs/t1354-malecon.txt lista cuáles quedaron.

| Qué | Para qué | Hueco |
| --- | --- | --- |
| Prefabs de Blender de T-1353 (Assets/Arte/ZonasModulos/Prefabs): ML01_BarandalMalecon, ML02_EscaleraPlaya, ML03_MuelleRecto, ML04_MuelleEsquina, ML05_Escollera | Barandal, escaleras, muelle, escollera | `Hueco_ML01_BarandalMalecon`, etc. (solo si T-1353 no los creó) |
| Tripo (Assets/Arte/Zonas): AR03_Palmera, ML01_Farol, ML02_Lancha, ML03_PuestoMariscos, ML04_Faro, PZ01_Banca | Palmeras, faroles, lanchas, puesto, faro, bancas | `Hueco_AR03_Palmera`, etc. (esa carpeta no existe en esta rama) |
| Hieleras y bancos de plástico del puesto (K12) | Puesto de mariscos | `Hueco_Hielera`, `Hueco_BancoPlastico` (sin código de modelo) |
| Material de arena T04_Arena en Assets | Playa | Color liso `T1354_Arena` |
| CreadorMarT1351.CrearPlanoMar y CrearOrilla (T-1351) | Agua | `Hueco_Mar` y `Hueco_Orilla` |
## Colonia del Cerro (T-1355)

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| CC01_CasaCerro, CC02_CasaCerro, CC03_CasaCerro (Tripo) | Casas del cerro | Sin hueco: mientras no estén se usan las casas del kit (KitS2c.Casa) |
| CC04_Tendedero, CC05_Lavadero (Tripo) | Azoteas | `Hueco_CC04_Tendedero`, `Hueco_CC05_Lavadero` bajo `T1355_Cerro/Huecos` |
| CE02_BarandalTubo, CE03_BarandalTuboEscalera (Blender, Assets/Arte/ZonasModulos/Prefabs) | Barandales de escaleras y plaza | `Hueco_CE02_BarandalTubo`, `Hueco_CE03_BarandalTuboEscalera` |
| CE01_EscaleraCerro (Blender) | Escaleras | Sin hueco: las escaleras se generan como malla de concreto |
| T05_ConcretoCerro.png (imageassets/calle) | Textura del terreno | No existe en el repo: Cerro_Terreno.mat usa Barrio_Concreto; si el archivo aparece, se copia al borrar el .mat y correr Aplicar |

## 18-nov, sesión B: modelos nuevos (nuevos-17nov)


### Modelos de zonas sin zip en imageassets/modelos-nuevos/nuevos-17nov/
| Código | Qué es | Estado |
| --- | --- | --- |
| CC03_CasaCerro | Tercera casa escalonada de la Colonia del Cerro | Solo está el .png de referencia, falta el zip de Tripo |
| CH03_EdificioColonial | Segunda variante del edificio colonial con arcos | Solo está el .png, falta el zip |
| NV01_NaveTransporte | Nave de transporte | Solo está el .png, falta el zip (no estaba en la lista de zonas) |
| V11_CamionetaPolicia | Camioneta de policía | Solo está el .png, falta el zip |

### Blender: llegó en la madrugada del 18 (ya integrado)
- Módulos de zonas (18 FBX): T-1346, en `Assets/Arte/Zonas/Modulos/`. Falta colocarlos en el mapa (sesión A o Tony).
- Carros V12–V24 y motos MT01, MT02: T-1342/T-1343 con puertas, cofre y cristal; al tráfico por zonas en T-1345.
- Cristales de los carros viejos: T-1343 (V01a, V01b, V02–V06, V09, V10). V07, V08 y C19 son motos: no llevan cristal.

### Vehículos: lo que sigue faltando
| Qué | Para qué | Hueco |
| --- | --- | --- |
| Prefab `Carro_V11` (camioneta de policía) | 5 estrellas | El FBX de Blender ya está en `Assets/Arte/Vehiculos/Piezas/V11/` con puertas, cofre y cristales; falta armarlo como carro de policía y conectarlo a `GestorPersecucion.prefabCamioneta` |
| Patrulla con su FBX de Blender | Que la patrulla también tenga cristal y cofre | `Patrulla.prefab` (Trafico) sigue con la carrocería de Tripo de V05 y la llanta de V09 (T-742/T-1331) |
| Zona de caballete del V07 en el cuerpo nuevo | Que no se vea el caballete al manejar | `ManejoMoto.mallaSinCaballete` del V07 es la malla completa |
| Luz neón abajo de V20/V21 (arrancones) | Deshuesadero | Sin hueco: hoy solo es la textura |
| Colocar los `ZonaTrafico_*` en el mapa | Que los carros salgan por zona | `Assets/_Proyecto/Trafico/Prefabs/Zonas/` (sesión A o Tony) |

### NPCs NP01–NP06
El chat de Blender les está poniendo el rig (ya no van por Mixamo). Hasta las 02:10 del 18-nov no habían llegado a imageassets/blender/.
Al llegar: importar como Humanoid con su avatar (`CreadorVisualPersonaje`/`CreadorVisualTripo`), darles las animaciones de peatón
que ya existen, mezclarlos con la gente de banqueta (`GenteDeBanqueta`, `ColocarT510`) y que los que platican queden de frente
(`PlaticarCon`). Ojo: el peatón de tráfico (`Trafico/Prefabs/Peaton.prefab`) es un solo prefab de cubos: para mezclarlos ahí hay que
cambiar `GrupoPeatones` a una lista de prefabs.

### Plazas
Los prefabs de plaza (T-1341) no llevan piso: el adoquín (T06) y la colocación en la ciudad los pone la sesión A o Tony.

### Zona pendiente: Las Lomas del M12
No existe aún. `ZonaTrafico_LasLomas` (T-1364) cubre por ahora los corredores norte x = ±47, z 88..201 de las afueras. Moverla cuando exista la zona.

## 18-nov D: Cerro
| Qué | Para qué | Hueco |
| --- | --- | --- |
| Antena parabólica de azotea | Variedad de azoteas del Cerro (T-1364) | No hay prefab; hoy solo CY04 (antena de TV) |
| Cubeta, tambos y botes de pintura | Detalle de azotea | No hay prefab; hoy solo macetas C20d |
| Más cables de fachada a poste (kit de conectores) | Que los cables se vean amarrados | Hoy son tubos de 4 lados sin pieza de amarre |
| Tendido de ropa más ligero | Rendimiento (CC04/CC05 pesan ~40 mil triángulos) | Versión baja del tendedero y del lavadero |

## 18-nov D: Puerto

| Qué | Para qué | Nota |
| --- | --- | --- |
| Escalera de mano de ~2.6 m (peldaños y rieles, ancho ~0.6 m) | Visual de `Escalera_Suelo` y `Escalera_Fila` de los bloques de contenedores (hoy solo `PuntoTrepar`, sin modelo) | `L03_EscaleraEmergencia` mide 4.5 x 3.5 x 4.8 m: taparía el pasillo de 3.5 m; los `Peldano`/`Riel` del Cerro son cajas de primitivos |
| Grafitis para los contenedores y las bodegas (K15/A15) | Look de la referencia | No hay textura ni decal de grafiti |
| Pluma de la entrada (`PluminaEntrada`) | Entrada U5 | Sigue siendo hueco |

## Sesión A, 19-nov (look de Malecón y Autopista, T-1461 a T-1464)

| Qué | Para qué | Nota |
| --- | --- | --- |
| Textura de arena de playa mojada | Playa del Malecón (K11, A11) | Hoy `T1463_Arena` reusa la textura de T1354_Arena con tinte; desde arriba se ve como franja lisa |
| Clips de NP "recargado en el barandal" y "platicando" | Gente del paseo de K11/K12 | Solo hay Idle, Caminar y Recargar (arma); los del barandal quedan en Idle |
| Visuales de peatones NP01 a NP06 | Variedad de gente | Solo existen NP07 a NP14 |
| Tramo de tablero elevado en curva (pieza AU) | K18 y A18: la curva que entra al túnel | AU02 es recto; la curva del ramal T-1415 se armó con tramos rectos |
| Material o shader aditivo de estela de luz | Estelas del tráfico en el tablero (K18, A18) | Las de T-1464 son cintas opacas con degradado |
| Columna AU04 más gruesa (5 m) con capitel | Primer plano de K18 | Hoy se escalan las de la rampa |

## T-1499 (19-nov): pedidos para Blender y sin modelo
| Qué | Para qué | Nota |
| --- | --- | --- |
| Rig (humanoide, como T-1381) de NP01_Senora, NP02_Trabajador, NP03_Joven, NP04_Senor, NP05_Vendedor y NP06_ChicaPunk | Completar la mezcla de peatones (hoy NP07 a NP14, 14 variantes en Peaton.prefab) | Solo existen los zips de Tripo sin rig en imageassets/modelos-nuevos/nuevos-17nov/; no hay FBX en imageassets/blender/personajes ni en Assets. Al llegar: copiar a imageassets/blender/personajes, agregar a PostprocesadorAssetsT250.PersonajesDeTripo y correr PersonajesNuevosT1389b y PeatonesBarriosT1482 |
| Modelo 3D de las tragamonedas TR01 y TR02 | Casino (hoy Casino/Prefabs/Tragamonedas.prefab es de primitivas, CreadorTragamonedas) | Solo hay imageassets/modelos-nuevos/TR01_Tragamonedas.png y TR02_Tragamonedas.png; sin zip ni FBX no se pueden meter sin inventarlas |
## 20-nov: edificios de primitivas (T-1502)
Medidas del cubo (ancho x alto x fondo, m) leídas de Ciudad_Prologo. Lo de las afueras (`T530_Afueras/Salida_N/Edificio`, 102 cubos) lo reemplaza `ReemplazarEdificiosT1502.Reemplazar` con edificios que ya existen; lo que no llene su hueco queda con la marca `T1502_Pendiente` y en `Logs/t1502-pendientes.txt`.

| Qué | Para qué | Medida y lugar |
| --- | --- | --- |
| Torre alta de oficinas (con azotea y antena) | Skyline de NeonAlto: 7 torres, 14 cuerpos | ~34 x 128 x 28 en `T530_NeonAlto/Torres/Torre_N/Cuerpo` |
| Torre de fondo muy alta | Horizonte de Suburbios: 24 torres | ~29 x 209 x 48 en `T530_Suburbios/Horizonte/Torre_N/Cuerpo` |
| Muro de la ciudad (modelo exacto, modular) | Orillas de las afueras: 10 tramos | 2 x 6 x ~78 en `T530_Afueras/Orillas/Muro` |
| Edificio de 10 a 28 m con fachada propia (sin kit) | Cuerpos de barrio hoy vestidos con piezas C01/C02/C03 sobre un cubo | ~22 x 12 x 23 en `S2n_Calles/Edificios/Edificio` (8 cubos, con LODGroup: no se cambian) |
| Cuerpo del taller mecánico | Local con interior | 24.7 x 17.9 x 8.3 en `S2n_TallerMecanico/Cuerpo/*` (el interior depende de él: se deja) |

Se dejan sin cambiar a propósito (el interior o el kit dependen de ellos): `Lugar_*`, `Cuerpo_T*`, muros de `Locales/*`, Armería (T-1503) y el parque de Plaza_K09 (T-1499).

## 20-nov: segunda vuelta de edificios (T-1512)
Ya cubierto: ED01 a ED12 (13 de 24 variantes importadas, ver docs/identificacion-nuevos-19nov.md), colocados en las afueras por `ReemplazarEdificiosT1512.Aplicar`. Los cubos de las afueras que ni con ED entren en su hueco siguen con la marca `T1502_Pendiente` y quedan en `Logs/t1512-diagnostico.txt` (medida y lugar). Lo demás de la lista de T-1502 sigue igual (torres del skyline, muro de la ciudad, edificios con kit y LODGroup, taller).

| Qué | Para qué | Nota |
| --- | --- | --- |
| Variantes sin modelo: ED01_b, ED02_b, ED03_a, ED04_a, ED05_b, ED06_b, ED07_b, ED09_a, ED10_b, ED11_b, ED12_b, MU02_b, MU03_b | Más variedad por barrio | Solo hay PNG en nuevos-19nov |

## T-1541 (21-nov): modelos nuevos

Barrido de nuevos-17nov, 18nov y 19nov contra Assets (por código, prefab y referencias en escenas y prefabs):

| Qué | Estado |
| --- | --- |
| W11 granada, EM01 portón, EM02 puesto (nuevos-21nov) | Importados por T-1541 (`ImportarModelosT1541`): W11 en la mano, al volar y en el piso; EM01/EM02 en la entrada del mercado de armas |
| Carros V12-V24, MT01/MT02, V11, PA01/PA02, drones DR02-DR04, NV03, ED/MU/CB/PU/ML/CC/PZ/AR/AU | Ya estaban en Assets con prefab. Los de zonas que se sacaron del mapa (CH, IN, LO, Cerro, Puerto, Malecón, Autopista) quedan sin colocar a propósito (mapa de 4 barrios) |
| ED01_CasaSuburbios_a, ED12_Estacionamiento_a | En Assets, sin ninguna referencia: quedan para T-1544 (casas caja a modelos reales) |
| NP01-NP06 (Tripo, nuevos-17nov) | Solo el zip sin rig; sin FBX en Assets. Esperan el rig de Blender (NP07-NP14 ya llegaron). No se importan estáticos |
| Sin 3D, solo PNG | CC03_CasaCerro, CH03_EdificioColonial, NV01_NaveTransporte, V11 (ya hay Carro_V11 de Blender), W10 (ya hay), ED01_b, ED02_b, ED03_a, ED04_a, ED05_b, ED06_b, ED07_b, ED09_a, ED10_b, ED11_b, ED12_b, MU02_b, MU03_b, CB04_b, PA01_b, PA02_b, DR03_b, DR04_b, E17_b, E18_b, ML06, NP10_b, NP12_b, SK01 |
| Enemigo que lanza granadas | No existe en IA/: solo el jugador usa la granada |
