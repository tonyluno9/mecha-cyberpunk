# T-historia-libre-23nov · Después de comprar armas y probar el campo de tiro: juego libre de venganza
**Estado:** HECHA (24-nov, ver docs/reporte-noche-24nov.md)

> **Cambio 24-nov (Tony probó en VR):** la historia termina con la CINEMÁTICA DE TINO dándote el arma. NO hay aviso "ERES LIBRE" ni ningún otro aviso. Al salir de la tienda de Tino empieza sola la escalada: primero llegan unos policías a pie; los vences y llegan las patrullas/camiones; luego NOVA; y así hasta que te maten. Los SKULLS también te atacan si los encuentras. Siempre se puede volver a hablar con Tino. Esto manda sobre lo de abajo.
**Lo que decidió Tony (cambio drástico):** la historia se queda como está HASTA que compras tu arma con Tino y practicas en el campo de tiro. De ahí en adelante ya no hay más historia ni cinemáticas: eres libre. El juego se enfoca en vengarte de los SKULLS: sales y les repartes balazos; luego te llega la policía y la balaceas; luego llega NOVA y la balaceas; y así, cada vez más fuerte, hasta que mueras.

## Qué se queda
Todo el arranque hasta el campo de tiro: C01 EL RECUERDO, C02 EL MALETÍN, C03 LA DECISIÓN, el nuevo arranque con 3 SKULLS y los Ƀ20,000 (VR24), el Doc y la prótesis, Tino guiado (T84) y el campo de tiro (T85). Las cinemáticas de esa parte (videos de T-cinematicas-videos-23nov) se quedan.

## Qué se desconecta (NO borrar: desconectar, como se hizo con el mecha y los aliados)
Todo lo que venía después en la historia (docs/tarea-historia-en-juego.md, pasos 10–19): C05 EL INVENTOR como cinemática obligatoria, C06 LA CHICA, LIBERA EL TIANGUIS, NEÓN ALTO / SNAKES, C07 EL HACKER, robots que bajan por historia, C08 EL HERMANO, jefe final, C09 LAS PUERTAS, el final y "CONTINUARÁ". Los objetivos de historia y candados posteriores al campo de tiro se apagan. También las "peleas protegidas" (sin policía) dejan de aplicar después del campo de tiro.

## Qué hacer
1. (Cambiado 24-nov) Al salir de la tienda de Tino por primera vez, sin aviso: objetivo pequeño "SOBREVIVE" en el HUD y empieza la escalada. Todos los candados restantes se abren (mapa, carros, tiendas, máquinas, Doc).
2. Escalada (usar el director de respuesta y la jerarquía que ya existen, no inventar sistemas nuevos):
   - Fase SKULLS: los SKULLS del mapa te reconocen y te atacan en grupos (pistola, bate, matón); siguen saliendo mientras estés en su zona.
   - Fase POLICÍA: los disparos y muertes suben el calor; llegan patrullas y policías a pie, luego más patrullas y drones de calor.
   - Fase NOVA: con calor alto llegan las cápsulas de NOVA (T7) con sus robots E15/E16/E18 y drones; después más y más fuertes.
   - Sin tope: la presión sigue subiendo hasta que mueras. Respetar los enfriamientos y topes de rendimiento ya definidos para no tirar los fps.
3. Contador en el HUD (pequeño, estilo del HUD): SKULLS, POLICÍAS y NOVA eliminados, y tiempo sobrevivido.
4. Al morir: pantalla "HAS MUERTO" con las estadísticas (eliminados de cada bando, tiempo, estrellas máximas, dinero) y dos opciones: REINTENTAR (vuelves al momento justo después del campo de tiro, con tu arma, mejoras y dinero de ese momento) y MENÚ PRINCIPAL. Guardar el mejor récord.
5. CONTINUAR en el menú, si la partida ya pasó el campo de tiro, carga directo al juego libre.
6. Funciona igual en PC y en VR (código común).

## Cómo comprobar (Tony jugando)
Partida nueva hasta el campo de tiro → sale "ERES LIBRE" → matar SKULLS → llega la policía → llega NOVA → morir → pantalla con estadísticas → REINTENTAR te deja justo después del campo de tiro.

## Escalada PROGRESIVA (regla final de Tony, 24-nov 02:29) — esto manda sobre todo lo anterior
Cada fase dura un TIEMPO MÁXIMO o hasta que matas un NÚMERO de enemigos de esa fase: lo que pase PRIMERO te sube a la siguiente. Ejemplo de Tony: los policías vienen durante 5 min, a menos que mates a 20 policías; ahí salta a la siguiente fase. Un evento grande (explotar un camión) NUNCA salta fases.
Valores iniciales (en ConfigRespuesta, para ajustar jugando):
| Fase | Qué llega | Sube cuando pasan… | …o cuando matas |
|---|---|---|---|
| 0 (T94) | Solo SKULLS: goteo de grupos de 2–3 que te buscan (máx. 6 vivos); ~20 % con pistola, el resto a golpes. Sin policía aunque tengas estrellas. Música SKULLS | al menos 2 min (tope 4 min) | 10 SKULLS, pero solo cuenta pasados los 2 min |
| 1 | Policías a pie y en patrulla | 5 min | 20 policías |
| 2 | Más patrullas (persecución) | 5 min | 6 patrullas inutilizadas o 30 policías |
| 3 | Camiones / refuerzos pesados de policía | 5 min | 40 policías |
| 4 | Drones de NOVA | 4 min | 10 drones |
| 5 | Soldados NOVA (E15/E18) en cápsulas | 5 min | 20 soldados |
| 6 | NOVA pesada (E16) + soldados + drones, sin fin | — | — |
- **Fase 0 SKULLS (T94, 24-nov):** al salir de Tino, antes de la policía. Dura al menos `SegundosFaseSkulls` (120 s); pasado ese tiempo termina en cuanto llevas `SkullsFaseSkulls` (10) SKULLS vencidos (los del goteo y los del mapa), y a los `SegundosMaximosFaseSkulls` (240 s) termina siempre para que no se atore. El goteo (`GoteoSkullsLibre`, regla pura `ReglaFaseSkulls`) manda un grupo de `SkullsPorGrupoMin`–`SkullsPorGrupoMax` (2–3) cada `SegundosEntreGruposSkulls` (12 s; el primero a los 4 s), hasta `SkullsVivosFaseSkulls` (6) vivos y siempre dentro del tope de enemigos (24 PC / 12 VR: los SKULLS del goteo cuentan en ese tope también después de la fase 0). Aparecen entre 25 y 45 m, fuera de tu vista y con camino por el NavMesh, y vienen a buscarte. `FraccionSkullsArmados` (20 %) llega con pistola (SkullRefuerzo; nunca rifles) y al acercarse pelea con su IA de siempre; el resto pelea a golpes (`ConductaSkullLibre`, con el daño y ritmo de sus datos, dos pegando a la vez). Durante la fase 0 el director de respuesta tiene una fase sin tipos: no llega policía aunque tengas estrellas. Al terminar empieza la fase 1 y la música cambia de SKULLS a policía (FaseJuego). Todo en ConfigRespuesta y común a PC y VR.
- Llegan POCO A POCO y sin huecos: un goteo constante que reemplaza a los que matas, hasta el tope de enemigos simultáneos (ver T-rendimiento-tope-24nov). Nunca "de repente nada".
- Las patrullas llegan MANEJANDO desde otras calles, se estacionan cerca, ABREN LA PUERTA con la animación que ya existía (PuertasCarro de PA01: la de la patrulla que abre la puerta y con ruedas que giran — volver a usarla), se bajan los policías y te atacan.
- Los refuerzos aparecen lejos y fuera de tu vista (nunca a tus espaldas a menos de 25 m).
- Los SKULLS siguen hostiles en su zona en todas las fases.
- En el reporte: valores usados y en qué minuto llega cada fase jugando normal.

### Minutos de cada fase si solo pasa el tiempo (con la fase 0 de T94)
| Fase | Empieza en el minuto (solo tiempo) | Lo más pronto (matando rápido) |
|---|---|---|
| 0 SKULLS | 0:00 | 0:00 |
| 1 Policía a pie y patrullas | 4:00 (tope de la fase 0 si no matas 10 SKULLS) | 2:00 (10 SKULLS vencidos en los 2 primeros minutos) |
| 2 Más patrullas | 9:00 | 2:00 + 20 policías |
| 3 Camiones | 14:00 | — |
| 4 Drones de NOVA | 19:00 | — |
| 5 Soldados NOVA | 23:00 | — |
| 6 NOVA pesada (sin fin) | 28:00 | — |
Jugando normal (peleando con los SKULLS que llegan) la fase 0 dura ~2 min, así que todo lo demás llega ~2 min antes que en la columna de solo tiempo.
