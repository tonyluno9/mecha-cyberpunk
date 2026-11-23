# Reporte de las tareas T30–T48 (ronda D, noche del 22 al 23-nov-2026)

Dos sesiones en paralelo:
- **Sesión A** (carpeta principal, la única que editó escenas): T35, T36, T37, T38 + E3, T45, T47 y lo de docs/para-sesion-A.md.
- **Sesión B** (.sesiones/s2): T30–T34, T39–T44, T46 y T48. Su detalle está en docs/progreso-tareas-B.md.

Todo está en main con merge --no-ff y push. Las capturas están en docs/capturas/_tareas/<tarea>/. Sonnet tenía el límite semanal agotado
(vuelve el 26-nov), así que los subagentes de A fueron Opus.

## Tabla

| Tarea | Sesión | Estado | Merge | Qué quedó / por qué no |
| --- | --- | --- | --- | --- |
| T30 Aparecer tirado tras la cinemática | B | HECHA | T-1656 | Captura en T30/. |
| T31 SKULLS del inicio | B (+A coloca) | HECHA | T-1656, 29e3705 | Sin voz; esperan a media calle. A colocó `Historia_EsperaSkulls` en (-46.5, 0, -76.5), a 6.5 m de `Inicio_Calle`. |
| T32 Audio: voces, pasos y siseo | B | HECHA | T-1655 | El siseo era la cola de ruido de los pasos de T28. Hay que oírlo en el build. |
| T33 Golpe más lento | B | HECHA | T-1657 | |
| T34 Clínica del Doc | B | HECHA | T-1658 | No se recorrió entera en Play. |
| T35 Muro unido con las torres | A | HECHA | 8340fdf | El cuerpo de las torres MU02 es más angosto que su base: quedaban rendijas de 0.3–1.45 m. Se alargaron 44 tramos MU01 (mismo modelo, con collider) hasta meterse 1.8 m en la torre. Rendijas visibles: 52 → 0; huecos pasables: 0. No se comprobó que la unión de la captura 06 sea exactamente la de la torre Este 1. |
| T36 Siempre de noche | A | HECHA | ce13bcf | Del paso 2 al 11 de la historia, el reloj queda a las 23:00 en pausa. Dormir o cargar una partida de día lo regresa a la noche. Desde el paso 12 corre normal. Prueba Play NocheFijaT1685Play 2/2. Una escena abierta directo en Play (sin la historia) sigue con ciclo normal. |
| T37 Limpieza | A | HECHA (con pendientes) | c62636d | Fuera: el bar de cubos, 12 edificios de relleno mal puestos (la casa rosa a media Calle Dorada, los edificios blancos de Nova y Oriente, 4 dentro del casino y 1 en el Clandestino), unos 100 primitivos visibles, 2 objetos flotando y 83 cosas fuera del muro. Las cosas se apagaron, no se borraron. Casino sin pasillo, con fachada de 65 módulos NV01 y sin postes. Asfalto sin los cuadros oscuros pintados en la textura. **No quedó:** el panel blanco de la captura 05 no está en la escena (quizá sale solo en el juego). Lo que flota en la captura 07 parecen ser los drones de los SKULLS (T40). Siguen siendo cajas con textura: Bar, Clandestino, Mercado Negro, la casa, el CIC y el taller (anotados en docs/assets-faltan.md). MQ01 no está importado. |
| T38 Banquetas + E3 Charcos | A | HECHA | 5f2c0d5 | Las 19 banquetas llevan T02_Banqueta a 0.8 m por losa, con 13 bordillos nuevos de concreto (más los 25 de T22). Los 220 charcos usan la forma de T11_Charco con el mismo shader y el mismo reflejo. Los bordillos S2c se ven oscuros de noche. |
| T39 Sin subir a techos | B (+A) | HECHA | T-1663, 29e3705 | El trepar libre ya no pasa de 2.5 m. A apagó los 32 PuntoTrepar activos (todos subían a azoteas). Las PlayMode viejas EscalerasTechosT1101 y TechosS2 van a fallar. |
| T40 NPCs, aviones y drones | B (+A) | HECHA | T-1664, 29e3705 | A cambió 3 vendedores del tianguis de "pregonar" a "esperar". Los drones de SKULLS vuelan a unos 88 m. |
| T41 Confrontación de pandilleros | B | HECHA | T-1659 | |
| T42 Policía y escalada | B | HECHA | T-1660 | Toca Core/Respuesta. |
| T43 Barras de vida | B | HECHA | T-1661 | Hay que verlas en el HUD del build. |
| T44 Tutorial e historia | B (+A coloca) | HECHA | T-1662, 29e3705 | A puso una expendedora (CY06) en (-64.3, 0.2, 9.5), 9 m al oriente del CIC, camino al tianguis. |
| T45 Menos carros y quemados | A | HECHA | fde5981 | Se quedan V01a, V02, V03, V09, V13, V14, los tuneados V20 y V21, el deportivo V12, la combi V04, V15 y la ambulancia V19. Salen V01b, V16, V17, V18, V22, V23 y V24. Se cambiaron 24 estacionados y robables. La policía, las motos y Nova no se tocaron. Al explotar, todas las mallas (Tripo incluidos) quedan con material quemado: prueba Play 2/2 y capturas T45/quemado_*. Tabla en docs/carros-T45.md. |
| T46 Drones de Nova inmunes | B | HECHA | T-1665 | Solo los Nova_DR; los drones de persecución siguen siendo derribables. |
| T47 Torre de Nova y letreros LED | A | HECHA | 93f3e24 | La torre TN01 mide 470 m (3.2 veces el edificio más alto) y está en el recinto de Nova (126.6, -63), con franja cian emisiva, luces y haz arriba. Se ve desde el centro y, por encima de los edificios, desde Suburbios; en Neón Alto la tapan las torres de 145 m. **Ojo:** el modelo de Tripo es chaparro y se estiró 8.7 veces en vertical, así que de cerca la escalinata parece una rampa alta. Hay 22 letreros nuevos (LT05, LT06, LT07 y LT09) pegados a fachadas, cada uno con su luz, y 17 materiales de neón que ahora sí emiten. Luces: 742 → 773. |
| T48 Menú de pausa moderno | B | HECHA | T-1666 | |

## Builds
Desde main 95cae02 (con todo lo de A y B), uno por uno y con Unity cerrado:
- **Demo:** `Builds/Demo/CyberCity.exe` (ConstruirDemoPandillas), datos de las 21:27, `Build Finished, Result: Success.` (Logs/build-demo-23nov.log).
- **Juego:** `Builds/Juego/CyberCity.exe` (ConstruirJuego), datos de las 21:29, `Build Finished, Result: Success.` (Logs/build-juego-23nov.log).
- **Prueba de inicio del Demo** (`-pruebaInicio`, pulsa los botones reales del menú): Nueva partida carga en 4.2 s con el jugador en (-55.5, 0.2, -77)
  dentro del muro; Continuar regresa al mismo punto (0.00 m). Sin excepciones en el log.

## Qué debe probar Tony
1. **Inicio (T30, T31):** despertar tirado, ver a los SKULLS esperando a media calle frente a la casa y dejar que te tiren donde estés.
2. **Audio (T32):** que los pasos ya no tengan siseo y que los SKULLS no hablen.
3. **Muro (T35):** caminar pegado al muro junto a las 6 puertas, sobre todo la de la captura 06.
4. **Noche (T36):** dormir o esperar; tiene que seguir siendo de noche.
5. **Limpieza (T37):**
   - Calle Dorada, la calle de Nova y la calle Oriente.
   - El casino: entrar directo por la puerta corrediza.
   - El tianguis, donde estaba el panel blanco de la captura 05: si sigue, anotar dónde.
6. **Banquetas y charcos (T38, E3):** las losas y bordillos, y que los charcos sigan reflejando igual de bien.
7. **Carros (T45):** el tráfico, y que un carro de Tripo quede negro al explotar.
8. **Torre y letreros (T47):** ver la torre desde la calle de la casa y desde el centro; acercarse a su pie (la escalinata estirada); ver los letreros nuevos iluminando las paredes.
9. **Lo de B:** el Doc (T34), la confrontación (T41), la policía hasta Nova (T42), las barras de vida (T43), el tutorial y la expendedora (T44), los drones (T46) y la pausa (T48).
