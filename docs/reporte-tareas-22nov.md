# Reporte de las tareas por hacer — 22-nov-2026

Las 15 tareas de docs/tareas-por-hacer.md se repartieron entre dos sesiones:
- **Sesión A** (carpeta principal, la única que editó escenas): T4, T6 (modelos y carros), T9, T13, T14 y lo de docs/para-sesion-A.md.
- **Sesión B** (.sesiones/s2): T1, T2, T3, T5, T6 (animaciones), T7, T8, T10, T11, T12 y T15. El detalle está en docs/progreso-tareas-B.md.

Todo está en main con merge --no-ff y push. Las capturas de cada tarea están en docs/capturas/_tareas/<tarea>/.

## Incidente: disco C: lleno
A las ~02:00 el disco C: llegó a 0 MB y cortó la primera corrida de Unity de T9. Para liberar espacio (con la autorización de Tony):
- Vacié la carpeta Temp de Windows (8 GB).
- Moví los builds viejos a `D:\respaldo-builds\Builds-hasta-22nov` (11 GB, nada borrado).
- Quité 37 worktrees de tareas que ya estaban en main y sin cambios. Las ramas siguen.
- Moví a `D:\respaldo-metas-22nov` los .meta locales de las cápsulas, del conductor corto y del E18, que B ya había subido con los suyos.

Quedaron 79 GB libres.

## Tabla de las 15 tareas

| Tarea | Sesión | Estado | Merge en main | Qué quedó / por qué no |
| --- | --- | --- | --- | --- |
| T1 Brazos del E18 en pose T | B | HECHA | 72ffa5a (f5ecbbf) | Avatar rehecho desde la pose T. |
| T2 Conductor robado huye al terminar su clip | B | HECHA | 72ffa5a (4d9df2b) | Usa la versión corta de Blender (3.1 s). |
| T3 Sentarse, camilla, colgarse, NPC llama a la policía | B (+A coloca) | HECHA | 72ffa5a (f9fea2e) y 85a60e6 | A colocó 4 bancas sentables en el parque (Parque_T1499). La camilla de la clínica ya tenía su punto. **No hay NPC sentado en las bancas** (falta un visual de NPC civil). |
| T4 Balas que pegan en postes (hitboxes) | A | HECHA | 96d66b1 | Capa 8 `NoDetieneBalas` para cables, toldos, letreros colgantes y rejas. Postes con cápsula vertical y cajas ajustadas: los colliders más grandes que su malla bajaron de 43 a 0. BalasHitboxesT1602Pruebas 4/4, ArmasT1324EditPruebas 8/8. En T4/resultado.txt, la bala pega al NPC pegado al poste y la pared la detiene. Las PNG de batchmode se ven mal: hay que probarlo jugando. |
| T5 Voces de los SKULLS | B | HECHA | f3a02f3 | Ver progreso-tareas-B. |
| T6 Importar lo de Blender | A (modelos) + B (animaciones) | HECHA | dd7fd55 (A) | **A:** V01a, V01b y V05 por piezas, reimportados. Carro_* rearmados sin Cristal_*, con largos de 4.41, 4.41 y 4.43 m y 0 referencias nulas (T6/carros-nuevos.png). En Ciudad_Prologo ya no quedaban carros viejos de una sola malla. **B:** E15/E16_Muerte, conductor corto y los 4 *_Capsula_Salida importados y conectados. Pendiente: los anchos de V01a (2.37 m) y V05 (2.50 m) son algo grandes. Hay una prueba vieja que falla aparte (Patrulla_ConservaSirenaRoboYGps busca `Asiento_Conductor`). |
| T7 Cápsula de caída de Nova | B | HECHA | e2f9b4d (9008cd1) | Los drones de persecución todavía no salen en cápsula. |
| T8 Escalada de enemigos | B | HECHA | e2f9b4d (e1f849a) | Registro en T8/. |
| T9 Muro perimetral | A | HECHA | 3c8ccff | Se rellenaron los 12 huecos entre torre y puerta, la torre flotante quedó en el piso y se borraron de la escena 904 objetos de afuera (la escena pasó de 53 a 32 MB). 3552 raycasts cada 1 m, todos chocan con el muro; MuroPerimetroT1601Pruebas 3/3. Capturas: 4 esquinas, 6 puertas y una aérea. Los rellenos son tramos MU01 comprimidos (se ven más cortos). |
| T10 Historia en el juego | B (+A coloca) | HECHA | ad6ac7f (910f567) y 85a60e6 | A colocó Historia_Molestia (-46.5, 0, -56.5), Historia_Ranura en el muro sur (-55, 0, -87.1) e Historia_Torre en el recinto de NOVA (117.5, 0, -43). No se probó en Play: VideoClip, pasos 15–19 ni CONTINUARÁ. |
| T11 NPCs de frente y reacciones | B | HECHA | d9bc6f5 | |
| T12 Los SKULLS molestan a civiles | B | **NO QUEDÓ del todo** | 975ff84 | El código está listo, pero en Play no se vio un bullying completo de forma espontánea. |
| T13 Tráfico ordenado | A | HECHA | dd7fd55 | Reserva de cruces, seguimiento por ruta, 20 carros de día y 15 de noche reciclados fuera de la vista, y destrabe a los 5 s (forzado a los 7 s). En 3 min en la ciudad: 0 atorados de más de 8 s, máximo 19 vivos y **4 choques, todos en un mismo cruce (x≈-47, z≈-2.4), que falta revisar**. Pruebas: TraficoOrdenado 3/3, TraficoCalle 8/8, ChoqueTrafico 5/5. Ante el caos los carros frenan y retoman; no huyen. |
| T14 Más realismo visual | A | HECHA | 4e47779 | Cielo propio (nubes por barrio, estrellas, luna), SSAO, postproceso, normal map en 100 materiales y selector de calidad Bajo/Medio/Alto. En el build de medición a 1080p: 199–245 fps de promedio, mínimo 73. No quedaron: vapor en rejillas (no se encontraron rejillas), SSR (URP no lo tiene) ni sharpening. |
| T15 Escenas del barrio y subtítulos | B (+A coloca) | HECHA | 975ff84 y 85a60e6 | A colocó 10 marcadores MarcadorEscenaBarrio: tianguis, Neón Alto, cortina del Mercado Negro, banquetas de Suburbios y NOVA. |

## Builds
**Pendientes.** El build de Demo (desde main 85a60e6) arrancó, pero Claude Code lo cortó porque la computadora se quedó sin memoria; Unity se cerró con él y no llegó a escribir el .exe. No se relanzó solo. Hay que correr, uno por uno y con Unity cerrado:
`Unity.exe -batchmode -quit -projectPath . -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirDemoPandillas -logFile Logs/build-demo-22nov.log` (Builds/Demo/CyberCity.exe) y luego `...ConstruirDemo.ConstruirJuego -logFile Logs/build-juego-22nov.log` (Builds/Juego/CyberCity.exe). Los builds viejos están en D:espaldo-builds\Builds-hasta-22nov.

## Qué debe probar Tony
1. **Muro (T9):** recorrer el borde caminando, en carro y saltando, sobre todo junto a las 6 puertas, donde están los rellenos nuevos.
2. **Balas (T4):** dispararle a un civil o a un enemigo pegado a un poste, un farol, un letrero o un puesto; tiene que pegarle. Con una pared o un carro en medio, no.
3. **Tráfico (T13):** mirar unos minutos el cruce de x≈-47, z≈-2.4 (Suburbios–Mercado), que es donde hubo choques, y manejar por los 4 barrios.
4. **Carros (T6):** abrir puertas y cofre de V01a, V01b y la patrulla V05, y ver si el ancho se siente bien.
5. **Cielo y calidad (T14):** el cielo naranja de Suburbios (si sigue muy fuerte, bajar `tinteSuburbios` en ConfigRealismo_Mundo) y cambiar la calidad en Opciones.
6. **Historia (T10):** el paso 3 (los SKULLS lo empujan en la calle) y la ranura del muro sur, cerca de la casa.
7. **Bancas (T3):** sentarse con E en las bancas del parque.
8. **Escenas del barrio (T12/T15):** caminar por el tianguis y Neón Alto y contar cuántas escenas aparecen solas.
