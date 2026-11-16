# T-1113: la demo falla a veces en el tramo 09 con el jugador atorado en el Doc

Rama `tony/T-1113-demo-intermitente-doc` (sale de origin/main c0618a7).

## Causa real
- Los fallos se vieron en las ramas T-1107/T-1108, que salían de un main anterior a T-1019 (e91d86e). En ese main el bot, tras usar al Doc, pulsaba Esc una sola vez si el control estaba apagado (T-1112), pero desde T-1005 el Doc habla primero y la clínica (`MostradorClinica.AbrirDesdeDoc`, con `CongelaControlAPie`) abre después: el Esc llegaba antes, la clínica abría con el bot ya caminando y el control del jugador quedaba apagado.
- Evidencia del fallo (log): al empezar el tramo 09 `estado: control False` en (-100.98, 0.32, -13.40), la posición del Doc, con "Tope al caminar: EL INVENTOR" cinco veces y luego los BUG 2 a 5 en cascada (banco del CIC, Esc, CONTINUARÁ).
- origin/main (c0618a7) ya trae el arreglo de T-1019 (espera hasta 15 s a que abra la clínica y la cierra con Esc). Con él, 3 corridas seguidas pasaron 3/3. El diagnóstico muestra que a veces la clínica no abre nunca (el Doc "cura" en lugar de abrir IMPLANTES): `clínica visible False, mostrador abierta False, control True, paso Inventor` durante los 15 s.
- No se encontró choque con el collider ni el NavMesh: el jugador no se mueve porque el control está en false.

## Qué se hizo (solo `RecorridoDemoPruebas`)
- `AsegurarControlDelJugador`: pulsa Esc hasta que la clínica (pantalla y mostrador) esté cerrada y el control haya vuelto (máximo 10 s; si el mostrador está operando espera a que termine). Se usa al terminar la espera del tramo 08 y al empezar el 09, por si la clínica abre tarde.
- Si tras eso sigue abierta o sin control, `Bug(...)` con el estado en el mensaje (antes solo miraba la clínica).
- Log `[T-1113]` con el estado de clínica, mostrador, control y paso al terminar la espera.

## Evidencia
- `RecorridoDemoPruebas` 4 corridas seguidas en origin/main + este cambio: 4 corridas seguidas: 3/3, 3/3, 3/3, 3/3 (XML total=3 passed=3 failed=0 cada una). Antes en T-1107/T-1108 (main viejo) fallaba 4 de 9 corridas.
