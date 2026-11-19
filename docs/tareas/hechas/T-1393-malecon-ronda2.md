# T-1393 Malecón, ronda 2

Script: Assets/Arte/Editor/ZonaMaleconT1393.cs. Corre DESPUÉS de ZonaMaleconT1384.Aplicar; edita el prefab T1354_Malecon (todo lo nuevo bajo "T1393_Malecon").
Métodos: Aplicar, Validar (Logs/t1393-aplicar.txt y t1393-validar.txt) y, en corrida aparte, Fotos (docs/capturas/t-1393/malecon-k11|k12|a11.png).

Causa de que el skyline no saliera: SK01 es un arco de 900 m de radio centrado en su pivote; en z -700 como cinta plana solo caía en el borde izquierdo
del cuadro de K11 (el cuadro mira al oeste y el mar queda al sur). Ahora son dos arcos (uno espejo) a 0.55x (radio 495 m) centrados en la cámara K11.

Sin verificar en Unity todavía (escrito sin Library): lo corre el orquestador.
