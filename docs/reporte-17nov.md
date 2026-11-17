# Reporte 17-nov (Sesión A)

Todo está en `main` y subido. Commits en orden:

| Commit | Qué |
| --- | --- |
| `0dc8442` | Merge de `s2-integ`: T-1109 guardia pesado de Nova y T-1110 jefe final de Nova |
| `2627b2a` | `docs/reporte-16nov-B.md` (lo que hizo la sesión B, T-1101 a T-1113) |
| `9facfa7` | T-1201 carros que explotan quedan como chatarra |
| `e59c762` | T-1202 policías sentados dentro de la patrulla |
| `9416127` | T-1203 bug de los techos |
| `ce2d422` | T-1204 animaciones de escalera |
| `c564a08` | T-1205 enemigos nuevos confirmados en la ciudad |

## 1. Integración de la sesión B

- `s2-integ` se revisó y se integró con `--no-ff`. Después de integrarla: EditMode 2577/2577 y PlayMode 35 pasan, 3 fallan.
  - Las pruebas de esquivar de JefeFinal y JefeSnakes dependen del orden. Corridas solas pasan, 17/17.
  - HeadshotPandilleroT677 también falla en `main` sin S2, así que el fallo ya existía.
- Se quitaron `.sesiones/s2` y su worktree, y se corrió `git worktree prune`.
- `docs/reporte-16nov-B.md` se armó con sus commits. T-1111 no existió.

## 2. T-1201: carros que explotan

- Al explotar, el carro pierde su velocidad y su giro.
- Ya quieto, se vuelve chatarra fija (kinematic): no avanza ni se desliza.
- Tiene humo y llamas sobre el motor, que se apagan poco a poco.
- Se ennegrece entero, no solo la carrocería.
- Los carros del tráfico se marcan como `Destruido` y dejan de manejarse.
- En los carros reciclados del tráfico, el humo volvía apagado; ahora se vuelve a prender.
- Pruebas: `ChatarraCarroT1201Pruebas`, 2/2. Capturas en `docs/capturas/t1201/`.
- Pendiente: en las capturas tomadas con la cámara del juego en la ciudad no salen las partículas, aunque el conteo de partículas confirma que existen. En las capturas sobre piso simple sí se ven. Revísalo jugando.

## 3. T-1202: policías dentro de la patrulla

- Los policías salen sentados dentro de la patrulla, visibles por la ventana:
  - el conductor en el asiento del jugador
  - el copiloto en el asiento de al lado
  - los demás atrás
- Al llegar se bajan con `PJ01_Carro_Salir`, a la misma velocidad que el jugador.
- Al tocar el piso junto a su puerta, la figura se cambia por el policía de verdad. Antes de eso no aparece ninguno.
- Si roban o explota la patrulla, los policías se bajan de inmediato.
- Pruebas:
  - `PoliciasSentadosT1202Pruebas` 2/2
  - `PoliciaRondaT933` ajustada: ahora espera a que se bajen
- Capturas en `docs/capturas/t1202/`.
- Pendiente: los policías no se vuelven a subir a la patrulla cuando se retira.

## 4. T-1203: bug de los techos

- Causa: al subir, el CharacterController se apaga, y eso hace que Unity no avise la salida del disparador de la escalera. El jugador seguía "cerca de la escalera" en el techo, y al caminar con W se volvía a enganchar, lo que lo regresaba o empujaba.
- Arreglo: antes de enganchar se comprueba que de verdad sigue junto a la escalera. Al terminar se olvida la escalera.
- Comprobado caminando con entrada simulada (W/A/D) en las 30 escaleras de la ciudad:
  - sin el arreglo, fallaban las 30
  - con el arreglo, pasan las 30

## 5. T-1204: animaciones de escalera

- El importador acepta dos fuentes:
  - los cinco clips de PJ01: `PJ01_Escalera_Entrar`, `_Subir`, `_Bajar`, `_SalirArriba` y `_SalirAbajo`, de `imageassets/blender/pj01_clips/`, como Humanoid con el avatar de PJ01 y Scale Factor 1.8367
  - de respaldo, `Escalera_Subir` y `Escalera_Bajar` de Mixamo
- Los cinco clips ya están importados y asignados en `ConfigAnimacionEscalera`.
- Subir y bajar avanzan con los metros que recorre el jugador.
- La salida arriba al techo y la salida abajo al piso usan sus clips.
- Nuevo: bajar desde el techo. Párate en la orilla de la escalera mirando a la calle y presiona W.
- Las pruebas de escalada se corrieron por clases chicas y pasan. Capturas en `docs/capturas/t1204/`.
- Detalles en `docs/pedido-animaciones.md`.
- Pendiente: revisar jugando cómo se ve SalirArriba cuando pasa sobre el pretil.

## 6. T-1205: enemigos nuevos en el juego

Se confirmaron con capturas en la ciudad:
- las riñas de SNAKES contra SKULLS, con capturas en `docs/capturas/t-1107/`
- el jefe SNAKES, con capturas en `docs/capturas/t-1108/`
- el guardia pesado de Nova, con capturas en `docs/capturas/t-1109/`
- el jefe final de Nova, con capturas en `docs/capturas/t-1110/`

Pruebas: RinasCiudadT1107 + JefeSnakesCiudadT1108 9/9, y GuardiasPesadosCiudadT1109 + JefeFinalCiudadT1110 10/10.

Pendiente: de noche los SKULLS se ven casi negros.

## Builds

Se hizo uno a la vez, sin nada más corriendo. Hora de México.

| Build | Ruta | Terminó | Resultado |
| --- | --- | --- | --- |
| Demo | `Builds/Demo/CyberCity.exe` | 16-nov 23:59 | Success, 0 errores, 2072 MB |
| Juego | `Builds/Juego/CyberCity.exe` | 17-nov 00:01 | Success, 0 errores, 2072 MB |

Logs: `Logs/build-demo-17nov.log` y `Logs/build-juego-17nov.log`.

## RecorridoDemoPruebas

(Se corre al final, sola; resultado abajo.)

## Pendientes conocidos

- Partículas de la chatarra en capturas de la ciudad: revisar jugando.
- Los policías no se vuelven a subir a una patrulla que se retira.
- SalirArriba sobre el pretil: revisar jugando.
- SKULLS casi negros de noche.
- HeadshotPandilleroT677 falla desde antes de esta sesión.
- Pruebas que dependen del orden: AsaltoDineroT728 y el esquivar de JefeFinal y JefeSnakes. Solas pasan.
