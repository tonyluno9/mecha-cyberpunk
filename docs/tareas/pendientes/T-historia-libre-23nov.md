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
| 1 | Policías a pie y en patrulla | 5 min | 20 policías |
| 2 | Más patrullas (persecución) | 5 min | 6 patrullas inutilizadas o 30 policías |
| 3 | Camiones / refuerzos pesados de policía | 5 min | 40 policías |
| 4 | Drones de NOVA | 4 min | 10 drones |
| 5 | Soldados NOVA (E15/E18) en cápsulas | 5 min | 20 soldados |
| 6 | NOVA pesada (E16) + soldados + drones, sin fin | — | — |
- Llegan POCO A POCO y sin huecos: un goteo constante que reemplaza a los que matas, hasta el tope de enemigos simultáneos (ver T-rendimiento-tope-24nov). Nunca "de repente nada".
- Las patrullas llegan MANEJANDO desde otras calles, se estacionan cerca, ABREN LA PUERTA con la animación que ya existía (PuertasCarro de PA01: la de la patrulla que abre la puerta y con ruedas que giran — volver a usarla), se bajan los policías y te atacan.
- Los refuerzos aparecen lejos y fuera de tu vista (nunca a tus espaldas a menos de 25 m).
- Los SKULLS siguen hostiles en su zona en todas las fases.
- En el reporte: valores usados y en qué minuto llega cada fase jugando normal.
