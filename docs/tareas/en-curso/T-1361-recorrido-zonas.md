# T-1361 Recorrido automático por las zonas nuevas

Rama: tony/T-1361-recorrido-zonas. Archivo: Assets/_Proyecto/Tests/PlayMode/Mundo/RecorridoZonasT1361Pruebas.cs (el asmdef de Tests/PlayMode/Mundo ahora referencia MechaCyberpunk.Personaje).
Estado: escrito sin poder compilar ni correr (worktree sin Library); falta la primera corrida en Unity.

Es un barrido: no falla por los incidentes, solo si no puede empezar. Cada prueba carga Ciudad_Prologo, tiene presupuesto de 215 s y [Timeout] de 300 s.
Salidas: Logs/recorrido-zonas-<parte>.json (y `-traza.csv` cada 0.25 s) y capturas en docs/capturas/bugs-18nov/recorrido-<parte>-<n>.png.
Incidentes: atorado, golpe-fuerte, escalon, caida-al-vacio, carro-volcado, trafico-atorado, sin-camino, sin-salida, caida, error-consola, no-cubierto, no-encontrado.

Correr (Unity cerrado, una parte por vez; el filtro lleva el nombre completo):
```
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "MechaCyberpunk.Tests.Mundo.RecorridoZonasT1361Pruebas.Malecon_EnCarro" -testResults Logs/resultados-playmode.xml -logFile Logs/playmode.log
```
Partes: Malecon_EnCarro, Autopista_EnCarro, Uniones_EnCarro, Malecon_APie, Autopista_APie, Uniones_APie.
Notas: en carro se usa CarroViejo.prefab con el jugador montado (JugadorAPie.Subir); a pie se hornea un NavMesh local y se camina con WASD, Mayús y ratón simulados.
