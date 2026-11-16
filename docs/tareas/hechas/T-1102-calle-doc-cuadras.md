# T-1102: calle junto a la calle del Doc rehecha con cuadras

Rama: tony/T-1102-calle-doc-cuadras. Pedido de Tony (playtest 16-nov, punto 11): "Calle infinita junto a la calle del Doc (assets repetidos): rehacerla con cuadras, esquinas, lámparas y casas armadas como el kit."

## Qué estaba mal
La calle de norte a sur x = -110.5 (de la avenida z = -10 al sur) eran frentes continuos con los mismos estilos A-F de ColocarS2cSuburbios a los dos lados, cortinas iguales, 4 postes y un muro liso oscuro al fondo: se leía como una calle infinita de piezas repetidas.

## Qué se hizo
- `ColocarT1102CalleDoc` (Arte/Editor, menú "Mecha Cyberpunk/S2c/T-1102 Calle del Doc con cuadras", idempotente, raíz T1102_CalleDoc, no re-corre ColocarS2cSuburbios). Apaga con SetActive(false) 12 casas viejas de S2c_Suburbios (9, 10, 11, 14, 15, 16, 17, 20, 45, 46, 47, 48), los 4 postes viejos y los pretiles de S2_Techos que quedaban sobre ellas.
- Casas armadas a mano con las piezas del kit (KitS2c.Casa con un `Estilo` elegido, no al azar): 13 fachadas de 1 a 3 pisos, cada una con su color; vecindad de 3 pisos con balcones y 2 tinacos, tiendita de abarrotes con cortina a medio subir, letrero y 2 sillas, casa verde agua, callejoncito con contenedor y bolsas, taquería (toldo, parrilla con brasas y luz naranja, barra y 3 bancos), vulcanizadora (llantas y letrero pintado) y lavandería (vitrina con luz fría y letrero) sobre las casas 12 y 18, lote baldío con malla ciclónica y mural dibujado en código, casa de 3 pisos, casita de 1 piso, tienda de esquina con toldo y el remate del fondo.
- Remate: tres fachadas de 2-3 pisos (neón grande de cantina, ventanas iluminadas, poste con luz) centradas en x = -110.5 y pegadas al muro oscuro de las Orillas, que queda detrás (ya no es lo único que se ve).
- Postes: 6 nuevos con lámpara de sodio y halo (5 a lo largo de la calle y 1 en el remate) más los 2 de las esquinas K05 que caen en las banquetas; cables entre ellos y a las fachadas; 8 charcos.
- KitS2c (Arte/Editor): `Estilo.Pisos` (1 a 3, por omisión 2) y `Estilo.Toldo` (null = al azar como antes); `TexMuro` pública. Sin cambio de comportamiento para lo ya colocado.
- Banquetas libres (>= 1.5 m): la caja de collider de los postes C07 mide 3 x 1.9 m (cubre el brazo) y tapaba la banqueta entera. En los postes nuevos y en los 2 de K05 sobre estas banquetas se adelgazó al pie del poste (0.5 m).

## Qué NO se tocó y por qué
- I+D+I del Doc, las 4 esquinas K05 (con sus 4 semáforos ES01 y sus 4 cruces peatonales pintados, uno por brazo: ya cubren lo que pedía el diseño en NE/SO y los 4 lados), arroyo, banquetas, callejón y andador.
- Casas 12, 13, 18 y 19 (flanquean el andador z -59..-64) se quedan: sostienen los accesos de techo, el tablón y la ruta de parkour 02 y 03. La cuadra centro solo mide 4 m entre la esquina K05 (z = -54.75) y el andador, así que el diseño (2 casas por lado) no cabe: la vulcanizadora y la lavandería van como adornos sobre las casas 12 y 18. Casas 23 y 24 (esquina de la avenida) también se quedan.
- La ruta de parkour Ruta_S2c_Suburbios_01 (barda, contenedor y escalera por atrás en z = -28.75) subía a la Casa_9, que se apagó: la taquería nueva es de 2 pisos (con vivienda arriba) para que su azotea siga a 6.69 m y la ruta siga sirviendo (TechosS2Pruebas y EscalerasTechosT1101Pruebas pasan).
- Los pretiles nuevos de las casas no llevan collider (así los pone KitS2c.Casa): las azoteas nuevas no tienen barandal sólido como las viejas.
- El andador al poniente termina a 3 m en el contenedor de la ruta de parkour 03 (S2_Techos/Contenedor en x -122): ya era así.

## Criterios
- [x] Casas, esquinas (K05, 4), lámparas (6 nuevos + 2 K05 con luz de sodio), cuadras con alturas 1, 2 y 3 pisos mezcladas, ninguna pareja vecina con mismo color y altura: CalleDocT1102Pruebas (EditMode).
- [x] Remate al sur con vida (neón grande, ventanas, poste) en lugar del muro liso; al norte se ve la avenida con edificios: capturas norte-a-sur y sur-a-norte.
- [x] Sin flotantes, encimados ni texturas rotas: FlotantesS2tPruebas, RevisarCiudadT995Pruebas, SinGreyboxT506Pruebas.
- [x] Caminar con W (con Shift: 70 m no caben en 25 s a paso de caminata) por las dos banquetas de z = -12 a z = -82, entrar y salir del andador al poniente, y de la casa del jugador a la puerta del Doc sin bloqueo nuevo (barrido de cápsula: la escena no hornea NavMesh): CalleDocT1102Pruebas (PlayMode).

## Evidencia
- EditMode CalleDocT1102Pruebas + FlotantesS2tPruebas + RevisarCiudadT995Pruebas + SinGreyboxT506Pruebas: 23 total, 23 pasan, 0 fallan.
- PlayMode CalleDocT1102Pruebas: 3 total, 3 pasan. TechosS2Pruebas 6/6. EscalerasTechosT1101Pruebas 4 (3 pasan, 1 saltada: captura). RecorridoCiudadT995Pruebas (con T995_CAPTURAS=1): 1 total, 1 pasa. verificar compilar: OK.
- Capturas en docs/capturas/t-1102/ (1376x768, de noche y con una luz direccional temporal): norte-a-sur, sur-a-norte, cruce-k01, taqueria, andador, remate, alto; comparacion-k05.png lado a lado con la referencia.

## Archivos
Arte/Editor: ColocarT1102CalleDoc.cs, KitS2c.cs. Arte/Mundo/S2c: materiales T1102_*.mat, texturas T1102_Mural y T1102_Malla, mallas T1102_CalleDoc.asset. Escenas: Ciudad_Prologo.unity. Tests: Tests/EditMode/Mundo/CalleDocT1102Pruebas.cs, Tests/PlayMode/Personaje/CalleDocT1102Pruebas.cs.

## Pendientes
- Casas 12, 13, 18 y 19 siguen con el estilo viejo (ver arriba); rehacerlas pide mover tablones, accesos y rutas de parkour.
- Las caras laterales lisas de las casas viejas junto al andador se ven planas desde la calle.
