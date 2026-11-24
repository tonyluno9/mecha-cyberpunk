# Reporte 24-nov mediodía (4 ajustes)

Tareas de docs/tareas/24nov-mediodia/: T99, T98, T101 y T100. Una rama por tarea, todas con merge en main y push (0df6c9b).
Paso 0: no había ningún Unity abierto ni lockfile en las dos carpetas. Las partidas se respaldaron en
Desktop/videojuego/respaldos-partida/24nov-mediodia/ y se restauraron al final.

## Qué quedó

| Tarea | Estado | En corto |
|---|---|---|
| T99 escalada | HECHA | Cada fase pasa a la siguiente a 1 min o con 10 bajas de esa fase, lo que pase primero. Sube de una en una. Se quitó el mínimo de 2 min de los SKULLS. NOVA pesada no termina. |
| T98 prótesis ×3 | HECHA | Piernas y brazos 18 s (nivel I) / 30 s (nivel II); visión 24 / 42 s; láser 12 s; misiles 3 s. Las recargas no cambian. |
| T101 mira VR | HECHA (falta probarla con el visor) | Gatillo izquierdo: todo negro salvo un círculo de 45° alineado con el cañón, con borde difuminado y retícula propia (cruz con marcas, anillo rojo, punto, toque cian). El zoom va solo dentro del círculo: francotirador 2×–8× con el stick derecho (que entonces no gira la vista), las demás armas 1.5×. Fundido de 0.1 s. Reemplaza la retícula de T91. PC sin cambios. |
| T100 pantallas de carga | HECHA | 22 frames escalados a 1920×1080. Sale uno al azar por carga, sin repetir el anterior, cubriendo la pantalla sin deformar. Solo salen estos frames. En VR, en la pantalla de cine. |

## Qué no quedó / ojo

- T98: el láser hace daño mientras está activo, así que ahora hace el triple de daño total por uso. Si queda muy fuerte, se baja `danioLaser` (~27 deja el daño de antes).
- T99: en 60 s quizá no lleguen todas las unidades de una fase (las patrullas salen cada 30 s y llegan manejando). Si se queda corto, se sube `SegundosFaseLibre`.
- T101: falta medir en el Quest. Dos riesgos: que pese demasiado (la mira dibuja la escena otra vez, sin sombras) y que el cielo se vea "transparente" dentro del círculo.

## Minuto en que llega cada fase si no matas a nadie

El reloj se para en pausa, en cinemáticas y en el campo de tiro.

| Fase | Minuto |
|---|---|
| 0 SKULLS | 0:00 |
| 1 Policía a pie | 1:00 |
| 2 Patrullas | 2:00 |
| 3 Camiones | 3:00 |
| 4 Drones NOVA | 4:00 |
| 5 Soldados NOVA | 5:00 |
| 6 NOVA pesada (sin fin) | 6:00 |

Matando, cada fase llega antes (10 bajas de la fase actual).

## Pruebas que corrieron

- Compilación limpia.
- EditMode (escalada, prótesis, carga): 91 de 91.
- PlayMode carga 3/3, prótesis 14/14, VR inicio + mira 16/16. La mira tuvo 3 fallas porque la prueba miraba un solo cuadro después de soltar el gatillo. Con 2 cuadros, como la versión anterior de la prueba, pasa 10 de 10.

## Builds

De uno en uno:
- PC: ConstruirDemo.ConstruirDemoPandillas → Builds/Demo: Succeeded, 0 errores, 3156 MB.
- PC: ConstruirDemo.ConstruirJuego → Builds/Juego: Succeeded, 0 errores, 3156 MB.
- VR: en wtVR (vr/quest3, con fast-forward a main), ConfigurarVR.ConstruirVR11 → wtVR/Builds/VR11/CyberCity.exe: Succeeded, 0 errores, 3156 MB (al primer intento).
  El método ConstruirVR11 se agregó en vr/quest3; se subió y se hizo merge a main.
Unity quedó cerrado. Logs: Logs/build-demo-24nov-mediodia.log, Logs/build-juego-24nov-mediodia.log y wtVR/Logs/build-vr11.log.

## Qué probar

En PC:
1. Salir de Tino sin matar a nadie: SKULLS → policía al minuto → patrullas al 2:00 … NOVA pesada al 6:00, con cambio de música en cada fase. Matando 10 de una fase, la siguiente llega antes.
2. Prótesis: cronometrar piernas, brazos, visión y láser (unos 18 / 18 / 24 / 12 s en nivel I); la recarga dura lo mismo que antes.
3. Pantallas de carga (nueva partida, continuar, morir → menú): frames distintos cada vez, nítidos y sin deformar.

Con el visor (VR11):
1. Campo de tiro, con cada arma: gatillo izquierdo → todo negro salvo la mira, y la bala pega en el punto rojo.
2. Francotirador: stick derecho adelante = más zoom, atrás = menos, y la vista no gira mientras apuntas. Al soltar vuelve la vista rápido.
3. Que no maree y que no baje el rendimiento al apuntar.
4. Prótesis con gestos: duran el triple.
5. Pantallas de carga en la pantalla de cine.
