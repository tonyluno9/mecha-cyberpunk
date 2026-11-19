# Pendientes del 19-nov (lo que no quedó o no se pudo, y por qué)

Se actualiza durante la noche. El detalle de cada tarea está en docs/tareas/hechas/T-13xx/T-14xx-*.md.

## Modelos y Blender
- **ML06_EdificioMalecon**: solo hay .png (_a y _b), sin zip con el modelo. No se importó.
- **Variantes sin zip** (solo .png): DR03_b, DR04_b, E17_b, E18_b, NP10_b y NP12_b. No hay modelo que importar ni rigear.
- **E13 (dron de SKULLS)**: no existe un modelo 3D, solo el .png. Es provisional: DR02_b reducido a 1.1 m. Necesita su propio modelo de Tripo.
- **E16_RobotPesadoNova_b**: el rig quedó regular. Venía con los brazos colgando; se llevó a T-pose y Unity no lo acepta como avatar Humanoid válido. Se quitó de Nova; E16_a sí funciona.
- **E19_PandilleroSkulls_a**: es la misma malla que E20_a (Tripo la duplicó). En el juego se usan E19_b y E19_a_2.
- **E17, el perro robot**: rig propio de 4 patas con Perro_Idle y Perro_Caminar. Entra en la respuesta de Nova como PandilleroAPie con NavMeshAgent. No se probó en PlayMode si la IA lo mueve bien.
- **Puertas traseras**: en V02, V03, V04, V06, V09 y V10 solo se abre la delantera.
- **Cristales con bordes dentados**: V10, V22 y V23.
- **V01a, V01b y V05**: el hueco de arriba sigue abierto (estilo buggy) y no tienen cristal de puerta.
- **W10**: el arma de Nova solo existía como imagen y zip sin versionar. Se renombró a W10_ArmaNova en imageassets, pero no hay modelo en el juego. El holograma sigue como H09_ArmaNova.

## Juego
- **Peatones por zona**: el sistema de peatones sale de un solo prefab y no conoce zonas, así que NP07–NP14 van mezclados en la lista general. Repartirlos por zona (pescador y turista en el Malecón, estibador en el Puerto) necesita un contrato nuevo en Core.
- **Zonas de tráfico de los barrios viejos**: las cajas de LasLomas, Avenida, NeonAlto y Deshuesadero son un reparto provisional de T-1364/T-1389a. Las deben confirmar Tony o Alan.
- **Barrio.prefab (de Alan)**: solo cambió el campo `prefabCamioneta`, para que con 5 estrellas salga la camioneta V11.

## Mapa
- **Calle del Cerro**: no se rehízo el trazado del zigzag para que suba más (sigue en 5 % a 9 %), porque eso regeneraba la zona y borraba los arreglos de T-1360 a T-1377. Para la foto K13 se usa la calle base con la escalera CE01.
- **Torre de reflectores de K18**: en la referencia sale a la izquierda del túnel; en el juego queda al norte.
- **Skyline sin reflejo**: el SK01 no se refleja en el agua del Malecón, porque el shader del mar no lo permite de forma simple.
