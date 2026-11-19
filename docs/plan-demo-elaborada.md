# Plan: la demo de CYBERCITY más elaborada (10 al 12 de noviembre)

Escrito el 10-nov-2026 por Claude (chat de Tony), para que lo ejecute el Claude Code de Tony (cuenta Max) en esta carpeta.
Corte: **12-nov a las 23:59, nada nuevo después.** Entrega: 18-nov.

## Objetivo
Que la demo actual se vea más elaborada y cinematográfica sin arriesgar lo que ya funciona. Todo es con cámara, texto, el kit de UI
(`UI/Kit/`, guía `docs/diseno/ui-cyberfix.md`) y piezas que ya existen. **No se inventan animaciones ni gráficos del HUD** (regla del
playtest del 7-nov): si algo necesita un gráfico nuevo, se usa texto del kit y se anota el pedido en `docs/pedido-arte-<fecha>.md`.

La demo de hoy (DirectorPandillas / MaquinaHistoriaPandillas): Despierta → SAL A LA CALLE → Asalto → DEFIENDE A DOÑA LUPE → RECOGE LA PISTOLA
→ HABLA CON EL CHATO → VE CON EL DOC A I+D+I → VE CON EL INVENTOR AL CIC → VENCE A LOS SKULLS (7) → VENCE AL JEFE DE LOS SKULLS
→ VE CON EL HACKER A NEÓN ALTO (NOVA arma a los SNAKES) → CONTINUARÁ → juego libre.

## Reglas para estas tareas
1. **Cada mejora se puede apagar** con un interruptor en un ScriptableObject de config (por ejemplo `ConfigDemoElaborada` en `Progresion/Datos/`
   o donde encaje mejor). Si el 12-nov algo no queda bien, se apaga y la demo queda como hoy.
2. **Toda cinemática se salta** con la tecla que ya usa `HudCinematica.TextoSaltar()` y usa `HudCinematica.Entrar()/Salir()` para ocultar el HUD.
   Durante una cinemática el jugador no se mueve ni recibe daño.
3. Ejemplo de cinemática que ya existe: el asalto (`Progresion/Prologo/Pandillas/AsaltoDinero.cs`, `ReglaAsalto.cs`). Cámara de recorrido que
   ya existe: `Progresion/Partida/CamaraTitulo.cs`. Subtítulos: `UI/Historia/PantallaSubtitulos.cs`. Final: `UI/Historia/PantallaContinuara.cs`.
   Créditos: `UI/Creditos/PantallaCreditos.cs`. Reutiliza esas piezas antes de crear otras.
4. Nada en las carpetas de Alan (Arena/, Mundo/, Vehiculos/, Casino/, Trafico/). Si hace falta un punto de cámara en su ciudad, se pone
   desde las herramientas de colocación de Tony o se anota.
5. `RecorridoDemoPruebas` y `PrologoCiudadPruebas` deben seguir en verde: las pruebas saltan las cinemáticas (o el interruptor las apaga en pruebas).
6. Una tarea, una rama, merge a main con push al cerrar cada una (Tony lo autoriza). Pruebas solo de lo que toca; build de la demo al final de cada tarea.

## Paso 0 (antes de todo): la demo de hoy funciona de punta a punta
- Mezcla `origin/main` y corre `RecorridoDemoPruebas` completo (el reporte del 9-nov dice que se ajustó pero no se corrió).
- Regenera `Builds/Demo/CyberCity.exe` y arráncalo; revisa que no haya excepciones en el log.
- Si algo se rompe aquí, se arregla primero. Esto manda sobre todo lo demás.

## T-950 Intro cinemática de Megalopolis (antes de despertar)
- Al empezar NUEVA PARTIDA, antes del paso Despierta: recorrido de cámara de 25 a 40 s por la ciudad de noche (calles principales, la calle
  de NOVA con su cambio de luz, el Tianguis, Neón Alto a lo lejos), con 4 líneas de texto en pantalla, una por plano.
- Texto propuesto (Tony lo cambia en el config si quiere):
  1. "MEGALOPOLIS."
  2. "LAS PANDILLAS SE REPARTEN LAS CALLES. LOS SKULLS TIENEN EL TIANGUIS. LOS SNAKES, NEÓN ALTO."
  3. "Y ARRIBA DE TODOS, NOVA."
  4. "TÚ SOLO TIENES TU CUARTO EN SUBURBIOS. POR AHORA."
- Termina en un fundido que entra directo al paso Despierta (donde ya sale "MEGALOPOLIS · SUBURBIOS").
- Solo en partida nueva; CONTINUAR no la muestra. Saltable.
- Criterios: prueba de PlayMode que arranca partida nueva, comprueba que la intro corre, que se puede saltar y que después el paso es Despierta;
  captura de 2 planos revisada.

## T-951 Tarjetas de presentación de personajes
- La primera vez que aparece cada personaje importante: cámara lenta corta (≈0.15 de velocidad, 1.5 a 2 s reales), su nombre grande y su papel
  debajo, estilo GTA/Yakuza. Una sola vez por partida (se guarda).
- Personajes y texto:
  | Personaje | Nombre (naranja) | Papel (turquesa) |
  | --- | --- | --- |
  | Doña Lupe | DOÑA LUPE | LA VECINA DE SUBURBIOS |
  | El Chato | EL CHATO | TU CARNAL |
  | El Doc | EL DOC | I+D+I |
  | El Inventor | EL INVENTOR | CIC |
  | El Hacker | EL HACKER | NEÓN ALTO |
  | Jefe de los SKULLS | (su nombre de datos; en magenta) | JEFE DE LOS SKULLS |
- El jefe va en T-952 con su propia entrada. Usa los colores y tamaños de `ui-cyberfix.md` (título 96, encabezado 48, sombra).
- Criterios: prueba que la tarjeta sale una vez y no dos; el tiempo vuelve a 1 al terminar; captura de una tarjeta en 1080p revisada.

## T-952 Entrada del jefe de los SKULLS
- Al aparecer el jefe (paso VENCE AL JEFE): HUD oculto, corte de cámara a él 3 a 4 s, su tarjeta en magenta y 1 o 2 líneas de subtítulo
  (propuesta: "—¿Tú eres el que anda tumbando a mi gente?" / "—Aquí se acaba tu racha."). Luego vuelve la cámara al jugador y empieza la pelea.
- El jefe no ataca ni recibe daño durante la entrada. Saltable.
- Criterios: prueba de PlayMode que la entrada corre, que el jefe no hace daño durante ella y que la pelea sigue normal después.

## T-953 La revelación de NOVA y el adelanto del CONTINUARÁ
- Con El Hacker: cámara cerca de él o de su escritorio mientras salen sus 3 líneas (ya existen en DirectorPandillas). Si hay una pantalla o
  monitor en su mesa, que muestre el texto "NOVA" (texto del kit; no un logo nuevo).
- Antes del letrero CONTINUARÁ: 3 planos rápidos de 2 s cada uno: Neón Alto, un grupo de SNAKES y la calle de NOVA. Luego el letrero
  ("NOVA ARMA A LOS SNAKES. NEÓN ALTO TE ESPERA.").
- Criterios: prueba que la secuencia corre completa y que se puede saltar; capturas de los 3 planos revisadas.

## T-954 Resultados y créditos al terminar la demo
- Después de CONTINUARÁ: pantalla de resultados con el kit, y luego los créditos (PantallaCreditos), y luego el juego libre.
- Datos: tiempo de juego de la demo, Ƀ ganados, SKULLS vencidos, precisión de disparo (aciertos / disparos), veces que caíste, carros robados.
  Si alguno no tiene de dónde contarse, se cuenta en un servicio pequeño de Tony (Progresion) a partir de los eventos que ya existen; si no hay
  evento, se omite esa línea (no inventar).
- Criterios: prueba de EditMode del conteo; prueba de PlayMode que tras CONTINUARÁ salen resultados, luego créditos, luego juego libre;
  captura revisada.

## T-955 (solo si sobra tiempo el 12) Diálogos con más personalidad
- De 2 a 4 líneas por personaje, cortas, con habla del barrio. **Propuesta para que Tony la apruebe**, en `docs/dialogos-demo.md`, sin
  meterla al juego hasta que diga "sí".

## T-956 Cierre (12-nov)
- `RecorridoDemoPruebas` completo en verde, build final `Builds/Demo/CyberCity.exe`, y **reescribir `docs/guia-demo.md`**: hoy describe la
  versión vieja con mechas; debe describir la demo de CYBERCITY paso por paso, teclas y minutos, incluyendo las cinemáticas nuevas.
- Lista corta para Tony: qué probar jugando y qué interruptores puede apagar si algo no le gusta.

## Orden
Paso 0 → T-950 → T-951 → T-952 → T-954 → T-953 → T-956. T-955 solo si sobra tiempo.
Si el 12-nov a las 18:00 no van todas, se cierra con las que estén en verde (T-956) y las demás quedan apagadas.
