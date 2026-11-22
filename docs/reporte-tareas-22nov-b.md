# Reporte de la segunda ronda del 22-nov: T16, T17, T18 y terminar T12

Todo está en main con merge --no-ff y push. Las capturas y los registros están en docs/capturas/_tareas/<tarea>/.

| Tarea | Estado | Merge | Qué quedó |
| --- | --- | --- | --- |
| T16 Solo patrullas, civiles estacionados, semáforos | HECHA | 45695c3 | Los civiles ya no circulan. Hay 57 carros estacionados junto al bordillo, a 20 m o más de los cruces, y se pueden robar. Patrullan 3 patrullas de ronda. Cada uno de los 2 cruces de la red tiene un semáforo con 4 postes ES01. En 3 min: 0 choques, 0 atorados y 17 paradas en rojo; un delito junto a la ronda arranca la persecución. Pruebas: 13/13 EditMode, 4/4 y 8/8 PlayMode. |
| T17 Escalada final: NOVA INTERVIENE | HECHA | e6fa324 | A 5 estrellas sale el aviso en el HUD y suena el sonido de llegada de Nova. Deja de llegar policía y la que había se retira; solo desaparece lejos y fuera de cámara. Nova llega en cápsulas por jerarquía, con un máximo de 3 vivos: E15 a los 37.7 s, E18 a los 80.5 s, drones a los 115 s, perro a los 125 s y E16 a los 170.7 s. Los drones de Nova ya llegan en cápsula. Pruebas: 24/24 EditMode, 3/3 PlayMode. |
| T18 NPCs sentados | HECHA | efd4a15 | 11 NPCs sentados con el clip PJ01_Sentado: 6 en las 4 bancas del parque, 2 en sillas y 3 en taburetes, con la cadera sobre el asiento y los pies en el piso. Al oír un disparo o una muerte cerca se levantan y huyen. Pruebas: 4/4. |
| T12 Bullying (terminar) | HECHA | efd4a15 | La causa del problema: GrupoAcoso tenía ocupados a los 25 SKULLS para siempre. Ahora el grupo presta 1 o 2 pandilleros al evento. En la ciudad se completaron escenas de SKULLS y de SNAKES con voces y subtítulos. Si el jugador interviene, el agresor se vuelve hostil y su grupo se pone en alerta. Con el jugador invulnerable, nadie lo mató. Pruebas: 3/3 y GrupoAcoso 11/11. |

## Builds
Desde main ab85818, uno por uno, los dos con "Build Finished, Result: Success":
- `Builds/Demo/CyberCity.exe`: datos de las 13:51 (Logs/build-demo.log). El .exe conserva la fecha de las 11:30 porque el ejecutable no cambia.
- `Builds/Juego/CyberCity.exe`: datos de las 13:52 (Logs/build-juego.log).

Prueba del Demo (2 min 13 s, con `-pruebaInicio` y los botones reales del menú): arranca a 2560x1600. Nueva partida muestra la cinemática "EL RECUERDO" y la ciudad carga en 3.8 s; el jugador aparece en la calle frente a El Canepa, dentro del muro. Guarda, y Continuar lo deja en el mismo punto. El log no tiene errores. La música del menú no se pudo comprobar porque la prueba no revisa el audio.

## Lo que no quedó o hay que revisar
- **T12:** en las capturas no se distingue el empujón físico; hay voces, subtítulos y fin de escena, pero no se ve el empujón. Los grupos que tienen una pared entre ellos y el civil cancelan la escena a los 25 s, porque se acercan en línea recta, sin buscar camino.
- **T17:** el aviso NOVA INTERVIENE se ve pequeño y pálido. La prueba vieja RespuestaNovaT1304Pruebas.CuatroEstrellas_LlegaNova falla porque espera la llegada de antes de T8 (3 guardias y 1 pesado de golpe); hay que reescribirla. No hay un sonido propio para el aviso: usa el de llegada de Nova.
- **T18:** no hay clip de levantarse ni de celular sentado; el NPC pasa directo a correr.
- **T16:** la red solo tiene 2 cruces con semáforo. Visto aparte: un CarroRobable_17 que aparece en (-20, -127.8), fuera del muro, se cae del mapa (es de otro sistema).

## Qué debe probar Tony
1. **T16:** caminar por la ciudad y ver que solo circulan patrullas, que se paran en rojo en los cruces (-47, 0) y (47, 0), y que los estacionados no estorban. Robar uno. Pelear junto a una patrulla.
2. **T17:** subir a 5 estrellas y ver el aviso, la retirada de la policía y las cápsulas de Nova llegando una por una.
3. **T18:** ir al parque (x≈-75, z≈-30), ver los NPCs sentados, disparar al aire y ver que huyen.
4. **T12:** caminar por el tianguis y Neón Alto hasta ver un bullying (o forzarlo en el editor: Mecha Cyberpunk > Forzar bullying SKULLS/SNAKES), y luego intervenir.
5. **Demo:** escuchar que suene la música del menú.
