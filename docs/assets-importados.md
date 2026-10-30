# Assets 3D importados (T-250)

Lote de 38 modelos (Tripo, un FBX y sus texturas por zip). Origen: `assetss/` en la raíz del repo, idéntico byte a byte a
los zips de Descargas; no está en git. No venían modelos E (exteriores), X (exos) ni P (personajes).
`L01_Contenedor (1).zip` era una copia exacta de `L01_Contenedor.zip`.

## Cómo se importan
- Archivos: `Assets/Arte/<Zona>/<Codigo>/<Codigo>.fbx`, `<Codigo>_basecolor.jpg` y `<Codigo>_normal.(jpg|png)`. Las extensiones
  `.JPEG`/`.PNG` se pasaron a minúsculas para que entren por LFS (`*.jpg`, `*.png`). Las texturas de metálico y rugosidad no se
  copiaron: el material las fija (Metallic 0, Smoothness 0.2).
- `Assets/Arte/Editor/PostprocesadorAssetsT250.cs`: 1 unidad = 1 m, ejes horneados, sin cámaras, luces, animaciones ni materiales
  del archivo; texturas a 2048 como máximo y normales como Normal Map. Corre después del `PostprocesadorArte` de Alan.
- `ArteT250.CrearTodo` (menú Mecha Cyberpunk/T-250): material URP Lit por modelo, prefab en `Assets/Arte/<Zona>/Prefabs/<Codigo>.prefab`
  (raíz con BoxCollider, hijo `Visual` escalado y con el pivote en la base), escena `Escenas/Tony/Pruebas_Assets.unity` con una fila por
  zona y capturas `docs/capturas/T-250-fila-<Zona>(-atras).png`.
- `ConstruirT250.Construir`: Builds/Provisional (las 9 escenas de la guía + Ciudad_Prologo, Clandestino y Pruebas_Assets) y
  Builds/Provisional_Assets (arranca en Ciudad_Prologo). No toca los Build Settings.
- `ColocarT250.Colocar`: capturas de antes (`T-250-<Zona>-antes.png`), colocación y capturas de después (`T-250-<Zona>.png`). Se
  corre sobre los prefabs limpios de git; no deshace una colocación anterior.
- Frente de los modelos nuevos: -Z. El kit de Alan tiene el frente en +Z, por eso los reemplazos con frente van girados 180°.

## Cómo se colocan
El modelo entra como hijo `Visual_T250` del objeto existente (o como hermano `<nombre>_Visual_T250` si el objeto es un primitivo
con escala no uniforme, para no deformarlo). Se escala a la medida de lo viejo y se apagan los MeshRenderer viejos (no se borran).
Colliders, scripts, luces, nombres, raíces y posiciones existentes no se tocan; los reemplazos no llevan collider propio.
Los props nuevos (sin greybox) sí llevan su BoxCollider, salvo los marcados abajo.

## Tabla
| Código | Prefab | Dónde quedó | Medida (m) | Problemas / ajustes |
| --- | --- | --- | --- | --- |
| A04_Gradas | Clandestino/Prefabs/A04_Gradas | JaulaClandestino/Gradas/Grada0-3, 2 copias por lado | 8.3×3.0×4.1 (en la jaula 14×5.8×8) | Estirado en X para cubrir 28 m por lado. 160 mil triángulos cada uno (8 en la jaula). Parte del público de la primera fila puede quedar un poco alto sobre el primer escalón |
| A05_LamparaColgante | Clandestino/Prefabs/A05_LamparaColgante | JaulaClandestino/Luces, sobre la LuzCenital (y 14) | 1.2 alto | Sin sombra ni collider para no tapar la luz. 194 mil triángulos |
| A06_ParedSotano | Clandestino/Prefabs/A06_ParedSotano | JaulaClandestino/Sotano_Visual_T250: anillo de 44 muros a 24 m del centro | 4.4×4.0×1.2 | Prop nuevo, fuera de las gradas |
| A07_ColumnaSotano | Clandestino/Prefabs/A07_ColumnaSotano | Sotano_Visual_T250: 4 esquinas | 1.3×4.0×1.2 | Prop nuevo |
| C01_FachadaLocal | Calle/Prefabs/C01_FachadaLocal | Barrio: las 422 Fachada_Modular del kit, alternando C01/C02/C03 | 4.6×4.0×3.0 (en el barrio 4.1 de ancho, ~3.6 alto, fondo 0.69) | Fondo aplastado a 0.69 m para no invadir la banqueta. Se pierden los letreros "TIENDA" y las ventanas iluminadas de la fachada vieja; arriba de 3.6 m se ve el cuerpo del edificio |
| C02_FachadaVivienda | Calle/Prefabs/C02_FachadaVivienda | Igual que C01 | 4.1×3.5×2.4 | Igual que C01 |
| C03_FachadaPisoAlto | Calle/Prefabs/C03_FachadaPisoAlto | Igual que C01 | 3.7×3.2×2.4 | Igual que C01 |
| C04_EsquinaPilar | Calle/Prefabs/C04_EsquinaPilar | Casa: orillas de la fachada (z -70.3 y -83.7) | 1.0×3.5 (fondo 0.4) | Prop nuevo, sin collider |
| C05_PretilAzotea | Calle/Prefabs/C05_PretilAzotea | Casa: 3 sobre la orilla del frente de la azotea (y 7.2) | 1.9×1.0×0.35 | Prop nuevo, sin collider. El resto de las azoteas no lo lleva |
| C07_PosteLuz | Calle/Prefabs/C07_PosteLuz | Barrio/Farolas/Farola_1-54 | 2.9×7.0×1.7 | Girado 90° para que el brazo apunte a la calle; la Luz de cada farola sigue en su lugar |
| C08_Transformador | Calle/Prefabs/C08_Transformador | Callejón, muro sur (-64, -63.4) | 1.3×1.4×0.9 | Prop nuevo, fuera de la ruta. Transformador_Poste del kit (poste de 7 m) se quedó igual |
| C09_Tinaco | Calle/Prefabs/C09_Tinaco | Barrio: los 10 Tinaco de TinacoPobre | 1.2×1.6×1.7 (al alto del viejo, 1.1) | |
| C10_AireAcondicionado | Calle/Prefabs/C10_AireAcondicionado | Las 45 Azotea_AC_Tinaco (al lado) y un muro del callejón | 0.7×0.6×0.4 | Sin collider en azoteas |
| C11_Basurero | Calle/Prefabs/C11_Basurero | Barrio/Ambientacion/Basurero_* (22) | 1.2×1.1×0.9 (al alto del viejo, 1.75) | |
| C12_PuestoTacos | Calle/Prefabs/C12_PuestoTacos | Barrio/PuestosTacos (3) | 2.2×2.4×1.7 (al alto del viejo, 3.5) | |
| C14_RegistroMedidores | Calle/Prefabs/C14_RegistroMedidores | Callejón, muro sur (-73, 1.0, -63.7) | 0.9×1.2×0.4 | Prop nuevo |
| C15a_LetreroHorizontal | Calle/Prefabs/C15a_LetreroHorizontal | Edificio_1, cara sur (-118.7, 4.0) | 3.0×1.1 (fondo 0.3) | Sin collider |
| C15b_LetreroBandera | Calle/Prefabs/C15b_LetreroBandera | Edificio_1, cara sur (-110.7, 4.0) | 0.85×1.2 | Sin collider |
| H01a_ParedPuerta | TallerCasa/Prefabs/H01a_ParedPuerta | Casa por detrás, muro del fondo (-69.2, -74) | 5.4×2.8 (fondo 0.25) | Prop nuevo, sin collider; la puerta es decorativa (la funcional está al frente) |
| H01b_ParedVentana | TallerCasa/Prefabs/H01b_ParedVentana | Fachada de la casa, izquierda de la puerta | 4.3×2.4 (fondo 0.25) | Encima del muro viejo, que sigue visible arriba; sin collider |
| H01c_ParedLisa | TallerCasa/Prefabs/H01c_ParedLisa | Fachada de la casa, derecha de la puerta | 3.4×2.4 (fondo 0.25) | Igual que H01b |
| H02_Cama | TallerCasa/Prefabs/H02_Cama | Casa/Interior/Sala/Prop_Cama | 2.0×1.0×1.65 | CamaJugador (la que funciona) no se tocó |
| H03_Espejo | TallerCasa/Prefabs/H03_Espejo | Casa/Interior/Sala/Prop_Espejo | 1.1×1.7×0.4 | EspejoJugador no se tocó |
| H04_MesaSillas | TallerCasa/Prefabs/H04_MesaSillas | Casa/Interior/Sala/Mesa (apaga Mesa, PataMesa, Banco1 y Banco2) | 1.6×0.95×1.5 | |
| H05_Refrigerador | TallerCasa/Prefabs/H05_Refrigerador | Casa/Interior/Sala/Refrigerador | 1.0×1.8×0.9 | |
| H10_Maletas | TallerCasa/Prefabs/H10_Maletas | Sala, contra el muro de atrás (-67.9, -77.1) | 1.0×0.7×0.9 | Prop nuevo |
| L01_Contenedor | Callejon/Prefabs/L01_Contenedor | Callejon/Contenedor | 2.0×1.6×1.25 (al largo del viejo, 2.2) | |
| L02_PuertaTrasera | Callejon/Prefabs/L02_PuertaTrasera | Callejón, muro norte (-70, -54.75) | 1.8×2.4 (fondo 0.5) | Prop nuevo, fuera de la ruta (z -59) |
| L03_EscaleraEmergencia | Callejon/Prefabs/L03_EscaleraEmergencia | Casa por detrás (-71.6, -81) | 4.5×3.5×4.8 | Bajada de 7 a 3.5 m (es una plataforma con escalera). No va en el callejón (taparía media ruta). Sin collider |
| L04_Llantas | Callejon/Prefabs/L04_Llantas | Callejón, muro sur (-79, -63.2) | 1.5×0.8×1.0 | Prop nuevo |
| L05_RejaPorton | Callejon/Prefabs/L05_RejaPorton | Fondo del callejón (-86.4, -59) | 6.9×3.0 (fondo 0.3) | Prop nuevo, cierra el fondo sin tocar cajas ni barriles |
| T01_ParedTaller | TallerCasa/Prefabs/T01_ParedTaller | Casa/Interior/Taller/Herramientas (tablero sobre el banco) | 4.5 de ancho (fondo 0.25) | |
| T02_EstacionExo | TallerCasa/Prefabs/T02_EstacionExo | Taller, alrededor del MechaEstacionado (-64.5, -73.5) | 6.0×4.5×3.4 | Bajada de 5 a 4.5 m para caber entre el banco y el muro del frente. Sin collider para no bloquear el PuntoSubida |
| T03_BancoTrabajo | TallerCasa/Prefabs/T03_BancoTrabajo | Casa/Interior/Taller/Banco, 3 copias a lo largo | 1.5×0.9×0.8 | |
| T04_GabineteHerramientas | TallerCasa/Prefabs/T04_GabineteHerramientas | Taller (-66.9, -71.1) | 1.4×1.2×1.0 | Prop nuevo |
| T05_Generador | TallerCasa/Prefabs/T05_Generador | Taller (-62, -71.3) | 1.4×1.0×1.1 | Prop nuevo |
| V01a_CarroViejito | Vehiculos/Prefabs/V01a_CarroViejito | CarroViejo (Visual_T250 de la raíz) y la mitad de los Carro_* del barrio | 4.2×1.95×2.3 | El modelo es una sola malla: sus ruedas no se separan, así que Rueda_DI/DD/TI/TD siguen con su greybox (quedan dentro del modelo). Más alto que el greybox (1.45): la CamaraCarro (y 1.9) queda cerca del techo. 90 mil triángulos |
| V01b_CarroPro | Vehiculos/Prefabs/V01b_CarroPro | La otra mitad de los Carro_* (17 en total) | 4.5×1.5×2.4 | 90 mil triángulos |

## Pruebas (2026-10-28)
Compilar OK; EditMode 1094/1094; PlayMode 984/994, 9 omitidas, 1 fallo intermitente que no es de T-250
(`MisionesPeleaPruebas.Robo_EsperandoEscondido...`: un cuadro de 1158 ms en Pruebas_Misiones, que no carga Barrio; pasó 3/3 sola).
`BarrioPruebas.Rayos_DesdeElCentro_ChocanTodosYElBordeSeVe` pedía un MeshRenderer encendido bajo el collider que choca: el modelo se
ajustó (Visual_T250 como primer hijo del objeto con collider; los cubos con collider quedan encendidos en "solo sombra").
Las pruebas de 100 ms por cuadro de la ciudad (Lluvia, Bar) pasaron con el arte nuevo.

## Pendiente (para Alan)
Los 38 modelos tienen lugar en el juego. Desde el 2026-10-28 el arte es otra vez de Alan: lo que siga lo coloca él.
- Exos (X) y personajes (P): no venían en el lote; MechaJugador, MechaEstacionado, rivales y JugadorAPie siguen igual.
- Los modelos de gradas, lámpara y carros pesan mucho (90 mil a 194 mil triángulos); conviene una versión con menos polígonos.
- Si Alan vuelve a correr CreadorTodo, Barrio.prefab y la jaula se regeneran sin el arte nuevo: hay que volver a correr
  `ColocarT250.Colocar` después.

# Assets 3D de la noche (T-419, plan-noche paso 4)

Lote de 39 modelos (Tripo, FBX con la basecolor en su `.fbm`). Origen: `C:\Users\Angel Perez\Desktop\videojuego\assets_noche\<Código>\`,
fuera del repo (no en `Temp/assets_noche` como decía el plan: Unity borra `Temp/`). `CY04_AntenaAzotea` venía como `tripo_convert_...`
y se renombró al copiar. Solo traen basecolor: el material no lleva normal map.

## Cómo se importan y colocan
- Mismo flujo que T-250: `Assets/Arte/<Zona>/<Código>/<Código>.fbx` y `<Código>_basecolor.jpg` (LFS), `PostprocesadorAssetsT250`,
  prefab en `Assets/Arte/<Zona>/Prefabs/<Código>.prefab` con hijo `Visual`, pivote en la base, BoxCollider y URP Lit; frente en -Z.
  Los CY van en la zona Calle (empiezan con C) y los E en Exteriores.
- `ArteNoche.CrearTodo` (menú Mecha Cyberpunk/T-419): prefabs, escena `Escenas/Tony/Pruebas_AssetsNoche.unity` y `docs/capturas/noche-fila-*.png`.
- `ColocarNoche.Colocar`: se corre sobre Barrio, JaulaClandestino y Dron tal como quedan después de `ColocarT250.Colocar` (si se regenera
  Barrio: `ColocarT250.Colocar` y luego `ColocarNoche.Colocar`). Los props de calle se calculan con Ciudad_Prologo abierta: en la banqueta del lado
  del cordón (a 0.3 m de la orilla), mirando a la calle, dejando libre la franja de caminar de los peatones junto al muro, sin chocar con ningún collider ni renderer de menos de 9 m, a más de
  3.5 m de cada puerta y entrada y a más de 2.5 m de NPC, mostradores, puntos de aparición, de historia, de jefe y marcas.
  Todo queda bajo `Barrio/ArteNoche/<grupo>` con el sufijo `_Noche`.
- `ColocarNoche.CapturarRevision`: capturas de revisión con luz plana `docs/capturas/noche-revision-*.png`.
- `MedicionNoche.Construir`: build de medición (Builds/Medicion) con 10 puntos de noche y sin el título; capturas `noche-<lugar>-antes/despues.png`.
- Ningún modelo pasa de 30 mil triángulos (el mayor, CY09, 19 431): no hizo falta LOD.

## Tabla
| Código | Prefab | Dónde quedó | Medida (m) | Triángulos | Problemas / ajustes |
| --- | --- | --- | --- | --- | --- |
| A08_Marcador | Clandestino/Prefabs/A08_Marcador | JaulaClandestino/ArteNoche: 2, sobre las esquinas norte de la reja (y 6.6) mirando al centro | 1.9×1.8×1.9 en la jaula | 5 852 | Es un marcador (pantalla), no un letrero de calle: va en la jaula, a los lados de la pantalla. Sin collider |
| A11a_PortalSotano | Clandestino/Prefabs/A11a_PortalSotano | Barrio/ArteNoche/Entradas, marco de la puerta del clandestino (0, 0.2, 9.7) | 3.6×3.8 (fondo 0.6) | 6 843 | Aplanado a 0.6 m; sin collider (la puerta funciona igual) |
| A11b_EscaleraSotano | Clandestino/Prefabs/A11b_EscaleraSotano | JaulaClandestino/ArteNoche, esquina suroeste del sótano (-19.5, -19.5) mirando al centro | 3.6×2.4×3.5 | 5 444 | La escalera sube: frente a la puerta de la calle flotaría, por eso va dentro del sótano. Sin collider |
| C01_FachadaLocal_cy | Calle/Prefabs/C01_FachadaLocal_cy | En lugar de cada C01 de T-250 (misma medida y posición; la de T-250 queda inactiva) | 4.2×4.0×3.0 (en el barrio 4.1×3.6×0.69) | 7 382 | 422 fachadas en total con C02 y C03 |
| C02_FachadaVivienda_cy | Calle/Prefabs/C02_FachadaVivienda_cy | Igual que C01 | 4.0×3.5×2.7 | 7 569 | |
| C03_FachadaPisoAlto_cy | Calle/Prefabs/C03_FachadaPisoAlto_cy | Igual que C01 | 3.4×3.2×2.7 | 7 411 | |
| C13_PuestoCiber | Calle/Prefabs/C13_PuestoCiber | ArteNoche/Tianguis: 2, en los huecos de la fila de puestos (x 27.5 norte, x -26 sur), mirando al centro de la calle | 2.4×2.6×1.3 | 9 320 | |
| C16_Tambo | Calle/Prefabs/C16_Tambo | Junto a los puestos del Tianguis y en sus banquetas (14) | 0.65×0.9×0.7 | 3 997 | |
| C17_Huacales | Calle/Prefabs/C17_Huacales | Junto a los puestos del Tianguis y en sus banquetas (10) | 1.1×0.9×0.9 | 4 706 | |
| C18_BarreraCalle | Calle/Prefabs/C18_BarreraCalle | ArteNoche/Barreras: 8, en las esquinas de las orillas de zona (x ±40 y ±54, z ±11 a ±20) | 1.9×1.0×0.6 | 8 044 | Paralelas a la calle cruzada, en la orilla de la banqueta |
| C19_Moto | Calle/Prefabs/C19_Moto | ArteNoche/Motos: 5, estacionadas junto a la banqueta de la calle principal | 2.0×1.1×0.8 | 9 528 | |
| C20a_PisoVentana_cy | Calle/Prefabs/C20a_PisoVentana_cy | Importado; se coloca en la parte 4b (kit de pisos) | 3.0×3.0×0.9 | 4 620 | |
| C20b_PisoDosVentanas_cy | Calle/Prefabs/C20b_PisoDosVentanas_cy | Parte 4b | 4.0×3.0×0.5 | 4 691 | |
| C20d_Macetas | Calle/Prefabs/C20d_Macetas | Parte 4b | 5.3×3.0×2.2 | 2 653 | La medida de importación (3 m de alto) es provisional: es una jardinera de ventana, 4b la escala |
| C21_PisoLiso_cy | Calle/Prefabs/C21_PisoLiso_cy | Parte 4b | 3.0×3.0×0.6 | 3 287 | |
| C22_Cornisa_cy | Calle/Prefabs/C22_Cornisa_cy | Parte 4b | 1.8×0.8×0.3 | 3 751 | |
| C23_Franja | Calle/Prefabs/C23_Franja | Parte 4b | 4.4×0.6×0.4 | 1 761 | |
| CY01_ProyectorHolo | Calle/Prefabs/CY01_ProyectorHolo | ArteNoche/Calle_tianguis y Calle_neon (14) | 3.9×2.4×0.7 | 9 373 | |
| CY02_PantallaVertical | Calle/Prefabs/CY02_PantallaVertical | Calle_tianguis y Calle_neon (15) | 1.6×3.5×0.9 | 7 185 | |
| CY03_MazoCables | Calle/Prefabs/CY03_MazoCables | Las tres zonas (17) | 0.8×2.5×0.3 | 6 021 | |
| CY04_AntenaAzotea | Calle/Prefabs/CY04_AntenaAzotea | ArteNoche/Azoteas: 30, una por cada dos edificios, lejos de tinacos | 1.1×4.0×1.0 | 8 903 | Sin collider |
| CY05_ServidorCallejero | Calle/Prefabs/CY05_ServidorCallejero | Las tres zonas (18) | 1.2×1.8×0.9 | 6 784 | |
| CY06_Expendedora | Calle/Prefabs/CY06_Expendedora | Las tres zonas (20) | 1.6×2.0×1.0 | 11 442 | |
| CY07a_PlataformaDron | Calle/Prefabs/CY07a_PlataformaDron | ArteNoche/Paredes: 10, en muros de la calle principal a 3.6 m (Tianguis y Neón) | 1.5×1.2×1.4 | 8 128 | Sin collider |
| CY07b_Dron | Calle/Prefabs/CY07b_Dron | Trafico/Prefabs/Dron: hijo Visual_Noche (Cuerpo, Rotor y Lente apagados, Foco sigue) | 1.7 de ancho | 8 925 | Sin collider; girado 180° para que su frente vaya hacia +Z |
| CY08_CamaraCorporativa | Calle/Prefabs/CY08_CamaraCorporativa | ArteNoche/Paredes: 10, en Neón Alto a 4.2 m | 0.8×0.8×0.9 | 7 833 | Sin collider |
| CY09_PuestoImplantes | Calle/Prefabs/CY09_PuestoImplantes | ArteNoche/Mercado: en la calle, en la boca del callejón de El Mercado (-22, 0, 4.7) | 2.6×2.6×1.7 | 19 431 | I+D+I está dentro de El Mercado y su callejón es angosto (ya tiene los puestos de T-250): el carrito va en la calle, como los puestos del Tianguis |
| CY10_CargadorCalle | Calle/Prefabs/CY10_CargadorCalle | Las tres zonas (19) | 0.9×1.6×0.7 | 7 715 | |
| E01_EntradaCasino | Exteriores/Prefabs/E01_EntradaCasino | ArteNoche/Entradas, puerta de VivaCoin (56.5, 0.2, 47) | 7.0×5.0 (fondo 1.0) | 10 311 | Aplanado a 1 m; el marco dorado de T-311 sigue a los lados. Sin collider |
| E02_PortonMercadoNegro | Exteriores/Prefabs/E02_PortonMercadoNegro | ArteNoche/Entradas, puerta de El Mercado (-24.6, 0.2, 26) | 5.5×3.8 (fondo 0.8) | 10 707 | Aplanado a 0.8 m; sin collider |
| E03_PantallaGigante | Exteriores/Prefabs/E03_PantallaGigante | ArteNoche/NeonAlto, azotea del Edificio_27 (99.5, 26.3, 15.6) mirando al suroeste | 12.5×8.0×8.0 | 7 222 | La pantalla es oscura (su textura no tiene imagen ni emisión) |
| H06_TeleVieja | TallerCasa/Prefabs/H06_TeleVieja | Casa/Interior/Sala (-66.0, -79.6) mirando al sofá | 0.8×1.0×0.55 | 4 538 | |
| H07_Sofa | TallerCasa/Prefabs/H07_Sofa | Sala, esquina del frente junto a la cocineta (-61.7, -81.9) | 1.5×0.7×0.7 (al 80 %) | 4 278 | Más chico y en la esquina para dejar libre la línea cama → puerta |
| H08_EstufaTarja | TallerCasa/Prefabs/H08_EstufaTarja | Sala, 2 en el lugar de la Cocineta (apagada, su collider queda) | 1.5×0.9×0.7 cada una | 5 894 | Estiradas a la medida de la cocineta (3 m) |
| H09_RepisaVeladoras | TallerCasa/Prefabs/H09_RepisaVeladoras | Sala, en el muro del fondo a 1.3 m (-68.0, -80.4) | 0.6×0.85×1.3 | 3 719 | Sin collider |
| H11_Buro | TallerCasa/Prefabs/H11_Buro | Sala/Prop_Mesita (el cubo greybox junto a la cama, apagado) | 0.6×0.7×0.5 | 2 756 | Sin collider |
| T06_Soldadora | TallerCasa/Prefabs/T06_Soldadora | Taller, junto al último banco (-68.0, -76.15) | 0.65×1.0×0.86 | 4 527 | |
| T07_GruaMotor | TallerCasa/Prefabs/T07_GruaMotor | Taller, piso libre al sur de la estación del exo (-65.6, -76.6) | 2.1×2.1×1.2 | 5 430 | El taller está lleno (T02 ocupa casi todo): queda fuera de la línea puerta → punto de interacción y puerta → espejo |
| T08_RepisaTaller | TallerCasa/Prefabs/T08_RepisaTaller | Taller, en el lugar de Estante0-2 y Piezas (apagados, sus colliders quedan) | 3.2×2.45×0.5 | 5 264 | Estirada a los 3.2 m de los estantes |
