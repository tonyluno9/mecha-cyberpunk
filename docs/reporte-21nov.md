# Reporte 21-nov (noche): mapa final, animaciones, modelos y NPCs

Rama de integración `integ/21nov` → main. Capturas en `docs/capturas/_21nov/` (las `K..-lado.png` son referencia | juego, de noche, a la altura de los ojos).
Bitácora completa en `docs/progreso-21nov.md`.

## Resumen honesto
- **El mapa NO quedó idéntico a las referencias.** Quedó cerrado dentro del muro, sin nada detrás, sin casas caja y con la luz de noche
  corregida. Pero la distribución de los barrios y varios lugares no son los de M13 y K20–K34 (detalle abajo).
- Animaciones, modelos nuevos y NPCs por barrio: metidos. Lo que no tiene clip o modelo está listado.
- 50 fps: cumple en los 13 puntos medidos (mínimo 58 fps, promedio de 233 a 296).

## 1. Mapa
| Punto | Hecho | Evidencia |
| --- | --- | --- |
| Nada detrás del muro (solo cielo) | **Sí.** Se apagaron 954 objetos fuera del muro (alas de afuera, calles de salida, autopista de afuera), 6 líneas de calle que lo cruzaban y los 4 fondos pintados T513. Prueba `Escena_NadaActivoFueraDelMuro`: 0 afuera | `01-aerea-despues.png` (antes: `00-actual-aerea.png`) |
| Muro cerrado con puertas, torres y reflectores (K32–K34) | **En parte.** 16 torres MU02 (4 esquinas y 12 junto a las puertas), luces de reflector en las torres. No hay modelo de reflector ni de concertina: la luz está, el aparato no | `K32_Muro_Calle-lado.png`, `K33_Muro_Puerta-lado.png`, `K34_Muro_Esquina-lado.png` |
| Cero huecos | **En parte.** 17 lotes vacíos se rellenaron con 22 edificios ED/LO/CH/ML/CC. Después quité 5 lotes que estorbaban: 3 en los cruces (x=±47, z=0), 1 en el callejón SKULLS y 1 encima del recinto del jefe. Esos huecos siguen ahí, igual que otros 4 donde no cupo ningún modelo (lista en `Logs/t1543-aplicar.txt`) | aérea |
| Cero formas básicas | **Sí para las casas.** 134 casas caja ahora usan un modelo real (T-1544). La caja de colisión se queda. Lo demás no se revisó objeto por objeto | `K20`, `K22`, `K30` |
| Calle de la casa con El Canepa (K20) | Sí, está | `K20_Suburbios_CallePrincipal-lado.png` |
| Callejón SKULLS (K21) | Está el callejón, pero sin el grafiti de calaveras, el tambo con fuego ni la escalera de la referencia | `K21_Suburbios_Callejon-lado.png` |
| Entrada del mercado (K19, EM01/EM02) | Sí: portón y puestos en el callejón de z=14 (T-1541). Revisar que el portón no esté girado | `K25_Mercado_Armas-lado.png` |
| Tianguis, plaza, casino | Existen. No se parecen a K23/K24/K26: menos gente, menos toldos, sin la marquesina del casino | `K23`, `K24`, `K26` |
| **Refinería de Nova (K29)** | **No.** Nova son oficinas y calles; no hay tanques, tubos ni chimeneas. No hay modelos de refinería en `imageassets/modelos-nuevos` | `K29_Nova_Refineria-lado.png` |
| **Distribución de los barrios (M13)** | **No.** Medidos en el juego: Suburbios a la izquierda (x<-40), El Mercado al centro, Neón Alto arriba a la derecha y Nova abajo a la derecha. M13 pone Nova arriba a la izquierda, Neón arriba a la derecha, Suburbios abajo a la izquierda, el Mercado abajo a la derecha y una glorieta al centro. Moverlos es rehacer el mapa (misiones, apariciones, tráfico, territorios), así que no lo intenté esta noche | `01-M13-vs-aerea.png` |
| Fachadas FC01–FC03 (Clandestino, Bar, Mercado Negro) | **No.** No existen en ningún lado; hay que pedirlas al chat de arte | — |
| Luz (sodio, neón, piso mojado) | Sí. Postes de luz color sodio y neón ×4. Solo se prenden las 96 luces más cercanas. Niebla más ligera (a 150 m se ve el 51 % y no el 16 %), asfalto con brillo 0.80 y banqueta con 0.50. URP 17.3 no tiene SSR: los reflejos son las sondas y los charcos que ya había | todas las `K..-lado.png` |
| Nada flotando/enterrado/encimado | No se revisó todo. Sí revisé los lotes de relleno: 5 estorbaban y los quité | — |

## 2. Animaciones (T-1540)
- 6 borradores nuevos de PJ01 conectados al ControladorBase:
  - muerte por explosión, por atropello y por caída;
  - derribo por explosión y levantarse;
  - empujón y explosión cercana.
- También quedaron conectados el salto corriendo y la rodada (4 clips del 21-nov), los daños PJ01 en los 7 papeles que no son el jugador, `Skull_Burla` original, `E15_Muerte` y `E16_Muerte`.
- Diagnóstico: 12 estados nuevos o actualizados, 135 clips cambiados en 16 overrides, 0 problemas.
- Sonido de impacto por cuadro con `TablaImpactosPJ01`, usando sonidos que ya existen.
- Pruebas: EditMode `AnimacionesT1540Pruebas` y `ClipsPJ01JugadorT985Pruebas` 62/62 (la T985 ahora espera 120 clips y marca `PJ01_Moto_Manejar` como reemplazado). PlayMode `JugadorT1327Pruebas` (atropello) 5/5.
- **Falla 1 prueba:** `EnemigosAnim2Pruebas.GuardiaNova_VigilaPresumeGolpeaYMuere` dio 5/6, porque el guardia de Nova no llega a `Guardia_Muerte` al caer. El override todavía tiene ese clip. No se aisló la causa (límite de 15 min) ni se sabe si ya fallaba desde T-1506. **Pendiente.**
- No probado en juego: el derribo por explosión con levantarse y el salto de posición entre los dos clips; la rodada corriendo.
- Sin hacer:
  - pendientes de Tony (E18 caminar/correr/golpe y su Avatar, cañón del E16, bisagras);
  - E16_CaminarEscudo, porque falta decidir el diseño;
  - el impulso del salto con 0.1 s de retraso, porque cambia cómo se siente el salto.
  - Faltan clips de granada, recargas por arma, implantes, caer de la moto, nadar, arresto y gestos de los personajes con nombre (lista completa en el reporte del subagente, `docs/progreso-21nov.md`).

## 3. Modelos (T-1541)
- W11 granada (prefab, material y versión en el piso) + holograma H11; EM01 portón y EM02 puesto de la entrada del mercado, colocados.
- Los `.meta` de importación de los FBX nuevos se subieron (antes no estaban).

## 4. NPCs (T-1542)
- Por barrio, con los límites reales:
  - 14 pares platicando corregidos para quedar de frente;
  - 12 repartidos;
  - 11 vendedores en sus puestos;
  - 6 en la plaza;
  - 41 en la banqueta;
  - 4 sentados.
- NP01–NP06 no tienen rig: no se pueden animar ni poner.
- La plaza K09 y la tiendita caen en Suburbios con los límites reales. A la tiendita no le tocó ningún NPC.

## Rendimiento (≥50 fps)
Build de medición, 1920×1080, en 13 puntos (`docs/capturas/_21nov/fps/medicion.csv`): todos cumplen, con un mínimo de 58 y un promedio de 233–296 fps.
Las columnas de llamadas y triángulos no sirven en un build que no es de desarrollo.

## Builds
Ver al final (se llenan al terminar).

## Qué probar (Tony)
1. Recorrer los 4 barrios hasta el muro: que nada se vea detrás y que no haya calles cortadas por los rellenos.
2. Morir por explosión (granada o barril) y ser atropellado: animación, sonido del cuerpo al piso y levantarse.
3. La entrada del mercado (z=14, x≈-22): que el portón EM01 no esté girado ni tape el paso.
4. Decidir si se rehace el mapa según M13 (es la diferencia más grande con las referencias) y pedir al chat de arte FC01–FC03 y la refinería.
