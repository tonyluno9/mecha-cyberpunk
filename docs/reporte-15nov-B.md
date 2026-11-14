# Reporte 15-nov, Sesión B

Todo quedó en `main` con merge `--no-ff` y push (sin force). La copia de trabajo `.sesiones/s2` se quitó al final.
Sin builds y sin correr CreadorTodo.

## Qué se subió

| Rama | Qué hace | Merge en main |
| --- | --- | --- |
| tony/s2-14nov | Meseras R01 de frente, sillas I12 mirando a su mesa, techos que se suben (22 escaleras/tubos, 6 rutas de parkour, 512 pretiles, tablones), 5 grupos de SKULLS que acosan a un civil (emboscada con retraso, civil que huye, bono de $40) y plan de enemigos | a4c74eb |
| tony/T-998-plan-contenido | docs/plan-contenido-nuevo.md (solo plan) | 49f7236 |
| tony/T-997-creditos-audio | Autor y licencia de los 193 sonidos en los créditos | 6dabd7e |
| tony/T-996-escalera-pasarela-nova | Las escaleras de las 2 pasarelas PS01 ya se suben | 33a034f |
| tony/T-995-recorrido-ciudad | Revisión de flotantes, encimados y texturas rotas en la ciudad | cfc69fc |
| tony/T-999-emboscada-techo | Emboscada desde techos probada en la ciudad real | 31c74ea |

Los números T-995 a T-999 se tomaron altos para no chocar con la Sesión A (T-989 a T-991).

## Detalle

**Antes de subir lo de pandillas** salieron 2 fallos de EditMode por cosas mías y se corrigieron:
- Las escaleras y tubos de techo tenían material liso. Ahora tienen la textura de metal de la escalera L03.
- El detector de flotantes contaba como pieza al civil de la azotea. La raíz `S2_PandillasAcoso` quedó fuera, igual que los demás personajes.

TreparAPiePruebas fallaba por el orden de las pruebas: la clase anterior dejaba la entrada y la ciudad cargadas. Se arregló y la misma limpieza se puso en TechosS2Pruebas (T-999). EditMode completo: 2421/2421.

**T-996 Pasarela de Nova:**
- El primer peldaño de las PS01 queda a 0.92 m de la banqueta y el jugador sube escalones de 0.35 m. Se puso una rampa invisible de la banqueta al primer peldaño.
- Siete props tapaban el pie de las escaleras y se apagaron: 3 neones N07, 3 cargadores CY10 y 1 huacal C17.
- Prueba: subir por cada escalera, cruzar el tablero y bajar por la otra, 3/3. Capturas en docs/capturas/t996/.

**T-995 Recorrido por la ciudad:**
- Flotantes: 0. Texturas rotas: 0.
- Encimados: 3 piezas apagadas (una farola metida en el puesto de tacos del Mercado y 2 tinacos C09).
- 13 capturas a la altura de los ojos de cada zona en docs/capturas/t995/.
- Detector y pruebas en `RevisarCiudadT995`.

**T-999 Emboscada desde el techo** (con disparo real del jugador):
- Desde la azotea vecina del grupo de la azotea: no lo ven y la alerta llega a +1.62 s del tiro.
- Desde el techo de Calle Dorada junto a Acoso_3_Lote, 6 m más alto: alerta a +2.61 s.
- A ras de suelo, de frente: alerta a 0.13 s.
- Un pretil C05 impedía cruzar a pie de la azotea vecina a la del grupo. Se abrió quitando 2 piezas.

**T-997 Créditos del audio:** 127 sonidos son CC0, 52 CC BY y 14 no permiten uso comercial (CC BY-NC o Sampling+). Hay que cambiar esos 14 o pedir permiso antes de vender el juego. La lista está en docs/creditos.md.

**T-998 Plan de contenido nuevo:** docs/plan-contenido-nuevo.md. Cubre:
- Enemigos E10 a E14, armas W05 a W08 y prótesis IMP04 e IMP05: cómo funcionan, dónde salen en la historia, balance propuesto y arte faltante.
- La tienda de armas M32 como pendiente.
- 12 decisiones para Tony.

## Pruebas que fallan y no son de esta sesión

- **PlayMode IA:** `BalasCiudadT711`, `HeadshotPandilleroT677`, `PandillaArmada.VencerlosPagaYTraeDrones` y `PoliciaCriminalT729`. Fallan igual sin los grupos de acoso.
- **PlayMode Mundo** (13):
  - CamaraLibreCarro (5), CamaraLibre (1) y CallesLlenasT508 (1).
  - Horario del Mercado (4): ahora dice "24 horas" y las pruebas esperan "09:00 a 18:00".
  - `Casa_EstacionamientoLibre`: el estacionamiento lo ocupa la estación del exo T02 en la escena Ciudad.
  - `RecorridoPrologoCiudad`: en esa calle no hay nada de esta sesión.
- **PlayMode Personaje:** no se pudo correr completo. El sistema se quedó sin memoria dos veces y se cortó, así que solo corrieron las clases que tocan lo de hoy, y todas pasan.

## Pendientes y para arte

- Arte de techos (docs/pedido-arte-techos.md): escalera marina, tubo, tablón y los clips Trepar_Escalera, Trepar_Tubo, Agarre_Borde y Salto_Largo.
- Arte de pandillas (docs/pedido-arte-pandillas.md): Skull_Empujon y los clips del civil (asustado, suplicar, huir, caer y levantarse). De noche los SKULLS se ven casi negros; parece su material.
- No se revisó si la azotea vecina del grupo de la azotea tiene acceso propio desde la calle. El techo del grupo sí tiene escalera.
- Nada se probó jugando a mano. Todo se probó con entrada simulada y revisando capturas.
