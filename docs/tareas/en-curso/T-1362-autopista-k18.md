# T-1362 Autopista como K18

Rama: tony/T-1362-autopista-k18 (sale de f41c439). Carpetas: Assets/Arte/Editor/ColocarT1356*.cs, Assets/_Proyecto/Tests/EditMode/Mundo/AutopistaT1356Pruebas.cs y docs/.
Referencias: imageassets/referencias/look/K18_AutopistaElevada.png y A18_Autopista_Aerea.png. Plan: docs/plan-bugs-18nov.md, docs/plan-zonas-18nov.md.

## Qué cambió
- **Calle oeste** (CalleZona banquetas, zona "Autopista", 14 m, rayas): de (-242,0,60), extremo norte abierto de la cruzada oeste de T530, recta al norte por x = -242 y vuelta de radio 30 a la calle baja en (-212,0,215). Casitas bajas del kit al oeste, postes y cables. No toca el corredor oeste (z = 0) ni el Cerro (x < -295). Arranca en z = 60 (final real de la cruzada) y no en 67 para que el asfalto quede pegado.
- **Calle baja** (CalleZona banquetas, zona "Autopista"): z = 215, x -212 a -120 (une la calle oeste con el enlace de la rampa), paralela y al sur del tablero. Casitas de 1 a 3 pisos solo de su lado sur
  (huellas en `ColocarT1356Trazado.HuellasCasas`, a más de 8 m en planta del borde del tablero). Nada de casas debajo del tablero: un lote con
  charcos, carros chatarra y basura.
- **Rampa norte (U7 reubicada)**: sigue saliendo de la calle cruzada norte (-107,0,201) y pasa por el enlace (-120,0,215) sobre la calle baja, pero ahora sube
  hacia el OESTE por z = 232 y se une al tablero en (-300,10,250) (antes subía al este y se unía en (30,10,250)). Pendiente ~7.3 % (tope 9 %); va a ras los primeros 12 m.
- **Se lee elevada**: losa de 1.4 m, viga central debajo, cabezal bajo cada columna, columnas de 2.6 m de diámetro (AU04 escalada en grosor) cada ~24 m,
  parapeto de concreto de 1 m con tapa de 15 cm y canto saliente (siempre visible; se quitó el guardarraíl AU05 para no duplicar). Sin parapeto donde el
  tablero va a ras del piso (entrada de la calle).
- **Túnel**: igual (boca AU06 en el acantilado de roca con collider, material T1356_Roca); luces de tubo emisivas y 1 Light cada 20 m.
- **Tráfico arriba**: se eligió `TraficoAutopista` (Mundo/Ambiente) sobre el tablero recto (z 250, y 10): 8 carros V01a/V01b con luces, 4 al este por el carril
  sur (15 m/s) y 4 al oeste por el norte (18 m/s), sin collider, siempre en movimiento (reaparecen al llegar a la boca o a la curva). No se usó CarroTrafico
  porque TraficoAutopista es decorativo y no necesita escena ni grafo; el tráfico real ya usa las CalleZona de la autopista.
- **Luz**: sodio cálido en luminarias (1 spot cada 36 m) y postes; neón chico en pocas casitas; tope 90 Light (el informe Logs/t1356-autopista.txt dice cuántas).
- **Fotos()**: K18 a 2 m sobre la calle baja (-150,2,213) mirando al oeste con giro al sur; A18 aérea a 45 m.

## Pendiente de verificar con Unity (no se pudo correr aquí: sin Library)
1. Compilar y correr `AutopistaT1356Pruebas` (EditMode).
2. `ColocarT1356Autopista.Aplicar` y `.Fotos` (genera docs/capturas/zonas/autopista-k18.png y -a18.png), revisar el encuadre y la cuenta de Light, luego
   `CorrerZonasT1350.GuardarComoPrefabs` y `PodarLuces`.
3. Revisar que el tráfico real (TraficoZonasT1350Pruebas) siga llegando a la rampa por el enlace (-120,0,215) y que la calle baja (sin salida al oeste) no rompa la prueba de calles sin salida.
