# Reporte 21-nov B: animaciones y decisiones de la revisión de Blender

Rama `integ/21nov-b` → main. Capturas en `docs/capturas/_21nov-b/` (en Play, en Ciudad_Prologo). Bitácora en `docs/progreso-21nov-b.md`.
No se editó ningún clip ni FBX de Blender (solo su importación). No se tocaron PROMPTS_ASSETS.md ni registro.csv.

## Resumen
- Los 5 bloques están metidos y vistos en Play: locomoción, combate y eventos, alturas, robots Nova y vehículos.
- Lo que no quedó:
  - Los brazos del avatar del E18 están a -20° de la T. Hay que hacer a mano Rig > Configure > Enforce T-Pose.
  - La prueba de muerte del GuardiaNova sigue fallando, igual que anoche.
  - Una medición de velocidad de un clip del 8-nov queda 0.003 fuera de la tolerancia.
- Pruebas:
  - EditMode de todo lo tocado: 769, de las que 767 pasan; las 2 que fallan son las de arriba.
  - PlayMode por clase:
    - SaltoLocomocionT1550 5/5
    - LocomocionBlenderT1505 + SaltoMedidaT944 8/8
    - RobotsNovaT1553 6/6
    - ClipsNpcNovaT1506 10/10 (1 omitida)
    - PuertasCarroT1344 4/4
    - EnemigosAnim2 5/6 (Guardia_Muerte)

## 1. Locomoción y salto de PJ01 (T-1550)
| Punto | Quedó | Evidencia |
| --- | --- | --- |
| Importar PJ01_CorrerRapido | Sí: Humanoid con el avatar de PJ01, frames 1–34, en bucle, con pisadas en 0.424 y 0.909. **Escala 1.88**, igual que PJ01_Trote y el resto de clips; el doc decía 1.8367 | `03-locomocion-salto.png` |
| Velocidades | Caminar 1.62, Shift 4.53. Correr rápido 6.25 solo con velocidad rápida (implante, piernas o café) | prueba `LocomocionT1550Pruebas` |
| Mezcla caminar → trote → correr rápido, sin PJ01_Sprint | Sí. En Play se ven PJ01_Caminar, PJ01_Trote y PJ01_CorrerRapido | `03-locomocion-salto.png` |
| Salto | Hecho en el constructor y en `AgregarEstados`, así que un rearmado no lo pierde. Incluye: velocidades de estado a 1x; impulso retrasado 0.1 s (agachada de 3 cuadros); SaltoAire sin ciclo; salto corriendo a partir de 3 m/s; aterrizaje corriendo y rodada sin bloquear; aterrizaje fuerte | PlayMode `SaltoLocomocionT1550` 5/5, `SaltoMedidaT944` |
| Pisadas a tiempo, sin patinar | Las pisadas salen de los eventos AlPasoPJ01 de los clips. La rapidez del Animator es la velocidad real entre la del clip | PlayMode `LocomocionBlenderT1505` |
- `SaltoMedidaT944`: ahora "en el piso" es menos de 5 cm (antes 2 cm). Tras aterrizar, la cápsula queda apoyada (`EnPiso`) 3 cm arriba de la línea base. No es que flote.
- `ImportadorClipsPJ01`: CorrerRapido entra en el bucle y Salto_Aire ya no es ciclo, para que reimportar no lo deshaga.
- El FOV de correr de la cámara empieza a abrir en 5 m/s, así que Shift (4.53) ya no lo abre. No lo cambié.

## 2. Combate y eventos (T-1551 + T-1540 de anoche)
- **Conectado hoy:**
  - Muertes aprobadas (PJ01_Muerte_Frente y Espaldas) en Aliados, SKULLS, SNAKES, Profes, Jefe, Hacker y Robots.
  - E15_Cubrirse.
  - Daño y muerte del policía.
- **Ya estaba:**
  - Daños PJ01_Dano_Frente y Espalda en Override_Jugador (el único que usa el jugador) y en los demás papeles.
  - Muertes por explosión, atropello y caída.
  - Derribo y levantarse.
  - Robar carro y sacar al conductor.
  - Subir y bajar de carro y moto.
  - Recargas de pistola y rifle.
  - Interacciones e implantes.
  - Miedo y vida diaria de NPC.
  - Burla de los pandilleros.
  - Caída del cielo de Nova.
- **Sin clip:** granada, recargas de revólver, subfusil, lanzagranadas y plasma, empujón propio del maleante, ataques de implantes y gestos de los personajes con nombre. La tabla completa está en `AnimacionesT1551.Matriz`.
- **Pendiente de Tony, sin tocar:** prótesis de ataque y E14 jefe.
- `EnemigosAnim2.GuardiaNova_VigilaPresumeGolpeaYMuere`: el guardia no llega a `Guardia_Muerte`. Ya fallaba anoche y no se aisló la causa.

## 3. Alturas (T-1552)
| Punto | Quedó | Evidencia |
| --- | --- | --- |
| Tabla aplicada a todos | Sí, pero **no con el factor**: la columna "hoy" de Blender no coincide con Unity, donde los NPC ya medían ~1.84. El factor los dejaba en 2.0–2.2 m, así que se ajustó midiendo hasta la altura nueva de la tabla. 40 de 41 quedan a ±5 cm; woman2 se mide en Idle (1.80 animada) | `01-alturas-fila.png` (de izquierda a derecha: abuela, PJ01, elotero, Tino, Doc, woman2, E15, E11, E12, E18, E14, E16) y `Logs/t1552-alturas.txt` |
| EscalasNova / EscalasHumanos | E15 1.94, E16 3.14, E18 2.14, E14 3.14; humanos con la tabla completa | EditMode `AlturasT1552Pruebas` |
| Tino en Ciudad_Prologo | La instancia ya estaba en 1 (escala efectiva 1.4949 en el prefab); el prefab se ajustó a 1.84 | `Logs/t1552-aplicar.txt` |
| Cápsulas y NavMesh E14/E16 | Cápsulas a 3.05 m (97 % de 3.14). `RespuestaNova.AlturaAgente` 3.44. No hay NavMeshAgent en el proyecto; todos usan CharacterController | EditMode |

## 4. Robots Nova (T-1553)
| Punto | Quedó | Evidencia |
| --- | --- | --- |
| Rehacer avatar E18 | Rehecho y válido. **Los brazos quedan a -20.7° y -20.5° de la T:** falta Rig > Configure > Enforce T-Pose a mano. La prueba `E18_Avatar_TieneLosBrazosEnT` falla por eso | `Logs/robots-nova-t1553.txt` |
| E18: clips propios, sin arma, jetpack, puños | E18_Caminar, Correr y Golpe_Der conectados, sin W10. Vuela con E18_Jetpack_Volar, aterriza y golpea quieto | `02-robots-a.png`; PlayMode `RobotsNovaT1553` 6/6 |
| E16: caminar con escudo, y se detiene a disparar el cañón | Con escudo arriba usa E16_CaminarEscudo y sin él E16_Caminar. Para el cañón: aviso, se queda quieto y E16_Disparar_Canon | `02-robots-a.png`; PlayMode 6/6 y `ClipsNpcNovaT1506` |
| E15 con la W10 | Medida con E15_Caminar y E15_Correr: giro de 2.0° y 3.2° y cabeceo de 3.3° y 3.6°, bien orientada | `Logs/robots-nova-t1553.txt` |
| Mover clips a su carpeta | E16_CaminarEscudo pasó a `E16_RobotPesadoNova/Clips/` y los E18_* a `E18_EliteNova/Clips/`, con su .meta y el mismo GUID | git |
- Los 4 clips con rig propio (E18_Caminar, Correr y Golpe_Der, y E16_CaminarEscudo) **no traen hueso Head**, solo 39 huesos mixamorig. Copiando el avatar del personaje, Unity no los importaba. Ahora cada uno lleva su avatar propio, con la cabeza mapeada al cuello. Conviene revisarlo en Blender.
- Arreglo extra: el E16 no entraba al estado de disparo porque su tipo de arma visual era "Ninguna".

## 5. Vehículos (T-1554)
| Punto | Quedó | Evidencia |
| --- | --- | --- |
| Bisagra del cofre en PA01, PA02 y V22 | Sí. Patrulla y PatrullaCamioneta rehechas con el cofre | `04-cofres.png` (cofres abiertos con el motor) |
| V16–V19 con su escala nueva | Ajustados: ancla de suspensión, ruedas, cámara, asiento (ojos al 78 % del alto) y puerta. Medidas: V16 3.10 × 4.97, V17 3.68 × 5.78, V18 4.02 × 8.40, V19 3.19 × 5.11 (alto × largo, m) | `05-grandes-dron.png` |
| Hélices de drones | No había overrides de posición. **El temblor venía del eje de giro:** el disco de cada rotor está en el plano XY local (FBX con Z arriba) y giraban sobre Y. Ahora giran sobre el eje de su disco | `05-grandes-dron.png`; EditMode `DronesT1397` y `VehiculosT1554` |
| Carros viejos → Carro_Vxx | Barrio: 17 cambiados. Ciudad_Prologo: los sueltos que había | EditMode `VehiculosT1554` |
| Quitar V10 y V11 del tráfico | Sí. Además `ImportarS2nCarros` ya no los vuelve a meter (la prueba `Importador_SinZip` los regresaba a la escena) | EditMode `CarrosS2nPruebas` |

## Pruebas que se ajustaron (y por qué)
- **Locomoción y salto (decisión de Tony 20-nov):**
  - Salto_Aire sin bucle.
  - Umbrales 1.62 y 4.53.
  - 121 clips PJ01.
  - PJ01_Sprint y PJ01_Moto_Manejar marcados como reemplazados.
- **Burla:** se permite Skull_Burla (decisión de la revisión de Blender).
- **Alturas:** la prueba compara contra la altura nueva, no contra el factor.
- **Robots en Idle:** los Nova no usan el Idle de PJ01 en el juego, así que quedan fuera de esa prueba.
- **Drones:** se prueba el eje del disco, no la Y.
- **E16 con escudo:** la prueba espera a que la mezcla del escudo termine.
- **Clips del E16 y E18:** se buscan en sus carpetas nuevas.
- **SaltoMedidaT944:** 5 cm para "en el piso".
- **T739** (Loco_Correr_ConImplante mide 3.155 contra 3.052 ± 0.1): no se tocó. El clip es del 8-nov y nadie lo cambió hoy.

## Builds
Ver abajo.

## Qué probar (Tony)
1. **WASD, Shift y café:** caminar, trotar y correr rápido sin patinar, con las pisadas a tiempo.
2. **Saltar:** parado (se agacha y despega), corriendo (sigue corriendo al caer) y desde ~2.6 m (tres puntos).
3. **E16 y E18 en la respuesta de 4 estrellas o en Pruebas_Tony:**
   - el E16 se cubre con el escudo y se detiene a disparar el cañón;
   - el E18 vuela, aterriza y golpea;
   - revisar los codos del E18, que todavía no tiene la T forzada.
4. **E18 en Unity:** Rig > Configure > Enforce T-Pose > Apply y volver a correr `RobotsNovaT1553.Aplicar`.
5. **Vehículos:** abrir el cofre de las patrullas, manejar V16–V19 (cámara y asiento) y mirar los drones de cerca.
