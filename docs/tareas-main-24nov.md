# T-historia-libre-23nov · Después de comprar armas y probar el campo de tiro: juego libre de venganza
**Estado:** PENDIENTE

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
# T-rendimiento-tope-24nov · Tope de enemigos para que nunca baje el fps
**Estado:** PENDIENTE
**Lo que pidió Tony:** al jugar y probar el modo libre, evitar que el juego se sature y dé un bajón de fps. El número de enemigos con el que empieza a saturarse es el límite.
**Qué hacer:**
1. Jugar el modo libre (PC y simulador VR, en el build, no solo en el editor) llevando cada fase al máximo, midiendo fps (promedio y 1% más bajo) contra número de enemigos vivos, vehículos, drones, ragdolls, efectos y luces activas.
2. El tope = el número de enemigos simultáneos donde el fps se mantiene: PC ≥ 60 fps; VR ≥ 72 fps estables (con RTX 5070 Laptop). Si VR necesita un tope menor, usar uno por modo.
3. Poner ese tope en ConfigRespuesta (enemigos simultáneos, patrullas, drones, cápsulas, ragdolls vivos, efectos) y que el goteo de refuerzos lo respete: llegan nuevos solo al morir otros.
4. Además: los cuerpos muertos desaparecen tras ~20 s (fundido), las balas/casquillos/efectos tienen tope, y lo que no se ve se apaga.
5. Anotar en el reporte la tabla de mediciones y los topes elegidos.
**Cómo comprobar (Tony):** jugar hasta NOVA pesada sin bajones.
# T-drops-municion-vida-24nov · Los enemigos sueltan munición y vida
**Estado:** PENDIENTE
**Por qué:** en el modo libre la pelea dura muchos minutos; sin esto te quedas sin balas ni vida.
**Qué hacer:** al morir, cada enemigo suelta con probabilidad (valores en un ScriptableObject): munición del tipo de su arma o de tu arma equipada (policía y NOVA casi siempre, SKULLS a veces), y de vez en cuando un botiquín pequeño. Usar modelos que ya existan en el proyecto (cargadores, botiquín/consumibles); nada con primitivos. Brillo suave para verlos, se recogen al pasar cerca (en VR también al acercar la mano), desaparecen a los 30 s. Sonido de recoger. Vale para PC y VR.
**Cómo comprobar:** matar 10 policías: aparecen cargadores y algún botiquín; al recogerlos sube munición/vida.
# T-indicador-danio-24nov · Indicador de dónde te disparan
**Estado:** PENDIENTE
**Qué hacer:** al recibir daño, un destello rojo en la orilla de la vista hacia el lado de donde vino (izquierda, derecha, atrás, arriba), que se desvanece en ~1 s. En PC en el HUD; en VR en el HUD pegado a la cabeza, suave (que no maree). Sin números.
**Cómo comprobar:** que te disparen desde los lados y atrás: el destello marca la dirección.
# T-omitir-cinematicas-24nov · Botón de omitir cinemática
**Estado:** PENDIENTE
**Qué pidió Tony:** en lugar de un "modo presentación", solo un botón para omitir la cinemática.
**Qué hacer:** en toda cinemática aparece abajo a la derecha "OMITIR" (botón visible). PC: clic o ENTER; VR: apuntar con el láser y gatillo, o B. Salta la cinemática completa (con la narración, fade de 0.2 s) y el juego queda en el estado correcto de después.
**Cómo comprobar:** omitir cada cinemática en PC y VR; la historia sigue bien.
# T-partida-completa-24nov · Partida completa de prueba (lo último de la noche)
**Estado:** PENDIENTE (al final, después de juntar todo)
**Qué hacer:** con TODO junto en main, jugar una partida nueva de principio a fin en el build de PC y en el simulador XR: cinemáticas con narración, 3 SKULLS, Doc, Tino, campo de tiro, modo libre hasta NOVA y morir, REINTENTAR. Anotar en docs/reporte-partida-completa-24nov.md todo lo que se rompa o se vea mal, con captura de la ventana; arreglar lo rápido (<15 min) y dejar lo demás listado para Tony.
