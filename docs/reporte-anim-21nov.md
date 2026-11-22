# Reporte: animaciones revisadas en Blender (20 y 21-nov) y transiciones con lógica

Rama `integ/anim21nov` → main. Capturas en Play en `docs/capturas/_anim21nov/`: tiras de 7 u 8 cuadros por escenario, con los tiempos en el `.txt` de al lado. Bitácora en `docs/progreso-anim-21nov.md`.
No se tocó el contenido de ningún FBX de Blender: solo su importación (.meta), el controlador, los overrides y el código.

## Parte 1: por qué se deformaban (y qué se ajustó)
El diagnóstico clip por clip está en `Logs/t1560-clips.txt`. Antes daba 57 clips con problema y 120 problemas en controladores y prefabs. Ahora da **0 y 0**.

| Causa | Clips | Arreglo |
| --- | --- | --- |
| **Los 6 clips reemplazados el 21-nov no importaban.** La raíz del FBX nuevo se llama `PJ01_REV` y su .meta viejo traía el esqueleto del FBX anterior, sin ese hueso. Unity fallaba al crear el avatar ("Transform 'PJ01_REV' not found in HumanDescription") y **no sacaba ningún clip**. Los overrides caían al clip viejo de Mixamo | PJ01_Muerte_Frente, PJ01_Dano_Levantarse_BocaArriba, NPC_Balazo_Frente y Espalda, PJ01_Carro_SacarConductor, PJ01_Conductor_SerSacado | Esqueleto regenerado desde el FBX actual, y luego la pose T de PJ01 copiada. Ahora importan. ConfigAnimacionParkour vuelve a apuntar a los dos clips del carro |
| **~70 clips nuevos sin .meta.** Unity armó su avatar con un cuadro cualquiera del clip, no con la pose T de PJ01. Esa es la "deformación" | PJ01_* y NPC_* del 21-nov | Copia de la descripción humanoide de PJ01.fbx (su pose T) |
| **Rangos viejos** en los .meta de clips reemplazados | Muerte_Frente decía 1-46 (ahora 1-51), Levantarse 1-110 (1-88), Balazos (1-25), SerSacado 1-80 (1-74), SacarConductor 1-80 (1-98), E15_Muerte 1-61 (1-71) | Rangos de la bitácora |
| **Escala y avatar ajenos** en los clips con esqueleto propio de los robots (mixamorig, sin hueso Head) | E15_*, E16_*, E20_Golpe_Brazo | Avatar y escala de su personaje (E15 0.911, E16 0.836, E20 0.903), con la cabeza mapeada al cuello |
| **Compresión 0.5** (dedos y codos) | todos | Compresión **Off** |
| **Loop adivinado por nombre** | Mixamo nuevos | Lista explícita de ciclos: idle, caminar, platicar, discutir, celular, teclear, herido, cojear, sentado, colgado y escalera |
| **Raíz** | todos los del 21-nov | Ver detalle abajo |

**Raíz, por tipo de clip**
- Los que casi no se desplazan (golpes, gestos, plática, heridos) hornean rotación, Y y XZ.
- Los que traen el desplazamiento (muertes, derribo, empujón, levantarse, caídas, saltos, parkour) usan root motion en XZ.
- Escalera y Colgado_Subir dejan la altura como root motion.
- La locomoción no hornea XZ, porque el CharacterController la mueve.

**Escala**
- La del modelo de PJ01 es **1.88** (PJ01.fbx.meta), no 1.8367.
- El 1.8367 es la escala de la armadura en Blender. Todos los clips de PJ01 van a 1.88.

**Avatar**
- No se usó *Copy From Other Avatar*: con esta jerarquía Unity no saca el clip.
- Cada clip lleva su avatar con la descripción y la pose T de PJ01 copiadas, que retargetea igual.

**Overrides (decisiones de Tony)**
- **Jugador:** golpe derecho PJ01_Golpe_Der, izquierdo PJ01_Golpe_Izq, fuerte **PJ01_Golpe_Protesis**, guardia PJ01_Punos_Guardia, daño PJ01_Dano_Frente y Espalda.
- **Clips descartados:** Golpe1_Directo, Golpe2_Gancho y Punos_Fuerte ya no los usa nadie. Los demás golpean con Golpe_Der e Izq, y el fuerte de los demás también es Golpe_Der.
- **Todos:** mueren con PJ01_Muerte_Frente (v2) y PJ01_Muerte_Espaldas. Los guardias de Nova conservan Guardia_Muerte.
- **NPC:** daño izquierdo y derecho NPC_Dano_Izquierda y Derecha.
- **Todos:** Skull_Burla → **PJ01_Burla**, Nova_Flexing → **PJ01_Presumir**.
- **E15 y E16:** sus caídas, muerte, cojear y herido. E16_CaminarEscudo, Bloquear_Tambaleo y Encenderse son solo para el E16.
- **E18:** E18_Caminar, Correr y Golpe_Der.
- **Override_E20 nuevo:** E20_Golpe_Brazo en sus golpes. Es decisión mía y es revertible.
- Ningún estado queda sin clip en los 17 overrides.

**Estados nuevos**
- Acciones de NPC: platicar A-D, discutir 1-3, teclear, trabajar con aparato, llamar a la policía, pedir ayuda, herido, arrastrarse, cojear, celular y mensajear caminando, caminar casual o en círculo, señalar y voltear.
- Pandilleros: burlas, gritar, empujar, patada y sacar arma.
- Rendirse y arrestado.
- E16: tambaleo y encenderse.
- Jugador: sacar y guardar el rifle, granada de pie y agachado, beber, camilla, sentarse, colgarse y subir del colgado.

## Parte 2: transiciones
| De → a | Condición | Duración | Capa o código | Probado en Play |
| --- | --- | --- | --- | --- |
| Guardia o golpe → caminar o correr | Movimiento a más de 2 m/s o Shift (sale del modo pelea solo) | 0.2 s | Arriba: PunosGuardia → Vacío | ✔ `01_golpear_correr`; PlayMode TransicionesT1561 |
| Correr → golpe | Primero sube la guardia (~0.2 s) y luego golpea; parado, golpea al instante | 0.15 s | GolpeAPie.GuardiaListaParaGolpear | ✔ `02_correr_golpear` |
| Sin arma → disparar | Sacar → apuntar → tiro (el disparo sale cuando el arma llega a la pose) | 0.1–0.15 s | PistolaAPie.EsperarArmaEnPose | ✔ `04_correr_disparar_sin_arma` (se ve el fogonazo al final) |
| Apuntar o disparar → correr | Sale de apuntar. La pistola corre con ella; el rifle se guarda (RifleGuardar) | 0.1–0.15 s | Arriba | ✔ `03_disparar_correr_pistola` |
| Recargar → disparar | Bloqueado hasta que termina | – | PuedeDisparar | ✔ PlayMode |
| Recargar → correr | Pistola: sigue con RecargarCaminar. Rifle: cancela | 0.15 s | Arriba | ✔ `05_recargar_correr` |
| Granada | De pie o agachado; se suelta en el cuadro del lanzamiento (54 de pie, 56 agachado), no al apretar | 0.1 s | Arriba + ArmasEspecialesAPie | ✔ `06_granada_de_pie_y_agachado`; PlayMode |
| Daño → (golpe, recarga, granada) | Daño de más del 8 % de la vida: corta y vuelve al reposo | 0.1 s | trigger Interrumpir | ✔ PlayMode (granada interrumpida) |
| Muerte o derribo | Ganan a todo; la capa de brazos va a 0 y el arma se guarda | – | VisualPersonaje.Caido | ✔ `07_morir_por_explosion` (sale volando y queda en el piso) |
| Derribo por explosión → levantarse → idle | Encadenado | 0.15 s | Base | ✔ `08_derribo_por_explosion` |
| Salto | La capa de brazos se apaga en el aire y vuelve al aterrizar | – | VisualPersonaje | ✔ PlayMode |
| Robo de carro | SacarConductor (98 f) → Carro_Entrar; antes guarda el arma y sale de pelea; el conductor arranca 0.75 s después con NPC_SacadoDelCarro_Huir | – | AnimacionCarro, VisualPersonaje | ✔ `09_robo_del_carro` (saca al conductor, sube y maneja). ✘ La prueba PlayMode no detecta el clip del conductor (ver pendientes) |
| Interacciones (beber, botón, puerta, recoger, sentarse, camilla, colgarse, escalera) | Antes guarda el arma y sale de pelea; bloquea el movimiento solo mientras dura | 0.1–0.15 s | Gestear | Beber se ve con la bebida del inventario. Sentarse, camilla y colgarse no tienen disparador en el juego |
| NPC platicando → asustado o huyendo | Mezcla corta, no de golpe | 0.2 s | ReproductorClipUnico | ✔ `12_npc_huyendo` |
| E16 golpe fuerte → Bloquear_Tambaleo | Golpe fuerte | – | ConductaGuardiaPesado | EditMode |
| E16 cae del cielo y muere | – | – | CaidaNova | `13_e16_cae_y_muere`: **revisar.** Aterriza de rodillas, pero en los últimos cuadros parece elevarse; puede ser el encuadre o la raíz de su muerte |
| Escalera y saltar barda | – | – | AnimacionEscalera, parkour | `11_saltar_barda` ✔. La escalera no tiene montaje fuera de la ciudad (sin captura) |

## Pruebas
- **EditMode** de animación: **496/497**. Falla solo el avatar del E18: brazos a -20° de la T; falta *Enforce T-Pose* a mano en Unity.
- **PlayMode:**
  - TransicionesT1561 **11/11**, ControladorBaseVisual 15/15, ClipsNpcNovaT1506 10/10, SaltoLocomocionT1550 5/5 y RobotsNovaT1553 6/6.
  - CapturasAnim21nov: 13 escenarios y 1 omitido (escalera).
  - EnemigosAnim2: 5/6. `Guardia_Muerte` ya fallaba antes de esta noche.
  - RobarCarroSacarConductorT1335: 0/1. La prueba no detecta el clip del conductor, aunque en la captura sí se ve sacado y en el piso.

**Pruebas ajustadas (decisiones de Tony 21-nov)**
- Burla y presumir con los clips PJ01.
- Golpes PJ01_Golpe_*.
- Los clips de rig propio llevan la escala de su robot.
- La escalera Mixamo es propuesta, no va en la config.
- Los estados de T-1561 se suman al conteo.
- Las muertes de NPC son las del jugador.

## Música del menú (lo que pediste en el camino)
- Quité el cruce: el tema suena con **loop normal** en una sola fuente, como lo hiciste.
- Comprobado en el build Juego: al terminar la pista vuelve al inicio (vuelta 1, pista en 0.5 s) sin cortar. Los fundidos de entrada y salida siguen igual.

## Builds
`Builds/Demo/CyberCity.exe` y luego `Builds/Juego/CyberCity.exe`, uno por uno, con "Build Finished, Result: Success".

## Pendientes
1. **E18:** en Unity, abrir E18_EliteNova_a.fbx → Rig → Configure → Enforce T-Pose → Apply, y luego `RobotsNovaT1553.Aplicar`.
2. **E16 al morir:** revisar `13_e16_cae_y_muere` en el juego.
3. **Conductor robado:** se ve sacado en la captura, pero la prueba no ve su clip. Además, `ConductorRobado` (Tráfico, de Alan) lo hace huir a los ~3 s y NPC_SacadoDelCarro_Huir dura 7.25 s, así que huye antes de que el clip llegue a "huye".
4. **Sin disparador en el juego:** sentarse, camilla, colgarse, NPC llamando a la policía (no hay evento de "muerte vista"), daño lateral del NPC y las acciones de NPC y pandilleros. Los estados están listos.
5. **Clips de comparación para Tony:** Rifle_CaminarMixamo, Salto_AdelanteMixamo, Salto_B_Mixamo, Escalera_SubirMixamo y NPC_Teclear frente a NPC_Hacker_Tecleando.
6. **Granada:** gasta la granada al apretar. Si un daño interrumpe el lanzamiento, se pierde.

## Qué probar (Tony)
1. Pelea: golpear y luego correr con Shift (los puños bajan); correr y golpear (sube la guardia y golpea).
2. Pistola: clic sin tenerla en la mano (la saca, apunta y dispara). Rifle: apuntar y correr (lo guarda).
3. R y clic (no dispara durante la recarga). R y correr.
4. Granada de pie y agachado (Ctrl): sale de la mano en el momento del lanzamiento.
5. Morir por explosión y derribo con levantarse.
6. Robar un carro con conductor.
7. Burla de pandilleros y jefe (PJ01_Burla) y presumir de Nova (PJ01_Presumir).
8. Ver que ningún personaje se vea deformado: muertes, NPC platicando y robots heridos o cojeando.
