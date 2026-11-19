# T-1413 Malecón, muelle K12

Script: Assets/Arte/Editor/ZonaMaleconT1413.cs. Corre DESPUÉS de T1384, T1393, T1400 y T1404; edita T1354_Malecon (todo lo nuevo bajo "T1413_Malecon").
Métodos: MechaCyberpunk.Arte.Editor.ZonaMaleconT1413.Aplicar, Validar (Logs/t1413-aplicar.txt y t1413-validar.txt) y, en corrida aparte, Fotos (docs/capturas/t-1413/malecon-k12|a11.png).

- Los modelos ya existían (ML03 puesto con lona, ML02 lancha, ML07 hielera con bancos): la ronda 2 solo los miraba de espaldas. K12 nueva: banqueta del mar, x 108, mirando al oeste.
- Puesto ML03 nuevo al este de la escalera del muelle (x ~ 97 a 103) con ML07 a/b, C17 y C16, y 3 puestos más (x 64, 24, -14).
- 4 lanchas ML02 al este del muelle y bitas PT02 en la cubierta.
- Faro ML04 lejano al oeste sobre un islote de ML05 con haz estático (el faro de T-1354 queda al este, a espaldas de K12).
- A11: cámara a (122, 34, -276) mirando a (42, 0, -226).

Sin verificar en Unity (escrito sin Library; compila sin errores propios con csc). Lo corre el orquestador.
