# Pendientes del 19-nov (lo que no quedó o no se pudo, y por qué)

Se actualiza durante la noche. El detalle de cada tarea está en docs/tareas/hechas/T-13xx/T-14xx-*.md.

## Modelos y Blender
- **ML06_EdificioMalecon**: solo hay .png (_a y _b), sin zip con el modelo. No se importó.
- **Variantes sin zip** (solo .png): DR03_b, DR04_b, E17_b, E18_b, NP10_b y NP12_b. No hay modelo que importar ni rigear.
- **E13 (dron de SKULLS)**: no existe un modelo 3D, solo el .png. Es provisional: DR02_b reducido a 1.1 m. Necesita su propio modelo de Tripo.
- **E16_RobotPesadoNova_b**: T-1408 lo rehízo en Blender con el esqueleto de E16_a, pero Unity no lo toma como humano: primero porque no encontraba la cabeza, y con el mapeo de E16_a copiado da un avatar válido pero no humano (`isHuman` = falso). Sigue fuera de Nova; E16_a funciona. Hay que revisarlo a mano en el configurador de avatar de Unity.
- **E19_PandilleroSkulls_a**: es la misma malla que E20_a (Tripo la duplicó). En el juego se usan E19_b y E19_a_2.
- **E17, el perro robot**: rig propio de 4 patas con Perro_Idle y Perro_Caminar. Entra en la respuesta de Nova como PandilleroAPie con NavMeshAgent. No se probó en PlayMode si la IA lo mueve bien.
- **Puertas traseras**: en V02, V03, V04, V06, V09 y V10 solo se abre la delantera.
- **Cristales con bordes dentados**: V10, V22 y V23.
- **V01a, V01b y V05**: el hueco de arriba sigue abierto (estilo buggy) y no tienen cristal de puerta.
- **W10**: el arma de Nova solo existía como imagen y zip sin versionar. Se renombró a W10_ArmaNova en imageassets, pero no hay modelo en el juego. El holograma sigue como H09_ArmaNova.

## Juego
- **Peatones por zona**: quedó con T-1409, con el contrato `IZonaDelMundo` y `ZonasMundo` en Core.
- **Zonas de tráfico de los barrios viejos**: las cajas de LasLomas, Avenida, NeonAlto y Deshuesadero son un reparto provisional de T-1364/T-1389a. Las deben confirmar Tony o Alan.
- **Barrio.prefab (de Alan)**: solo cambió el campo `prefabCamioneta`, para que con 5 estrellas salga la camioneta V11.

## Mapa
- **Calle del Cerro**: no se rehízo el trazado del zigzag para que suba más (sigue en 5 % a 9 %), porque eso regeneraba la zona y borraba los arreglos de T-1360 a T-1377. Para la foto K13 se usa la calle base con la escalera CE01.
- **Torre de reflectores de K18**: en la referencia sale a la izquierda del túnel; en el juego queda al norte.
- **Skyline sin reflejo**: el SK01 no se refleja en el agua del Malecón, porque el shader del mar no lo permite de forma simple.
- **Aéreas (A11, A13, A15, A18)**: se revisaron menos que las de calle. A13 sigue lejos de la referencia: faltan las calles iluminadas que suben y la ciudad brillante abajo.
- **K13 (Cerro)**: la cámara está en la calle base y no en un callejón que suba con escaleras como en la referencia. Las "cintas negras" eran cables a menos de 4 m del lente; en el juego son normales.
- **K18 (Autopista)**: no está la columna gruesa al centro ni el tablero en curva arriba a la derecha. El tablero es recto y la torre de reflectores no tiene haces visibles en la foto.
- **K12 (muelle)**: faltan el puesto de mariscos con lona, los bancos de plástico y las lanchas. El skyline se ve plano.
- **Paseo del Malecón**: no hay modelo de banca en el proyecto, así que no se pusieron bancas.
- **Acceso de CC07 en U4**: `DespejarBocasUniones` apagó el acceso (escalera de servicio) de la casa CC07_a_0 de T-1394, porque estaba en la boca de U4. Esa azotea se baja saltando.
- **Torres de Neón Alto en la vista aérea del Cerro**: con la niebla parecen flotar. Habría que bajar la niebla en altura o ponerles base.
- **Pruebas**: no se corrió el EditMode completo ni PlayMode, por la memoria. Solo se corrieron las clases nuevas, las de zonas y las de tráfico.
- **Fotos nocturnas en el editor**: las nubes y las estrellas solo aparecen en las fotos de T-1399 (`CieloCompleto`); `ColocarS2cLook.Foto` no las arma por defecto.
- **ReglasEnsamblados**: la prueba PlayMode de Mundo (de Alan) referencia a Personaje desde T-1361 (18-nov). Hay que pasar `RecorridoZonasT1361Pruebas` a un contrato de Core.
- **K13**: la ciudad no queda a la derecha, porque la ladera sube al oeste y la ciudad está al este. Para tenerla a la derecha habría que hacer un callejón que suba al norte.
- **Peatones por zona (T-1409)**: no se probaron en PlayMode; solo con pruebas EditMode.
- **Lámparas y tinacos de las bodegas del Puerto**: la prueba de encimados de T-995 ahora los permite (`ParejasPermitidas`).
