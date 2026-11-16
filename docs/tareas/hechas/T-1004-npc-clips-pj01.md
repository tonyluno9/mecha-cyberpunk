# T-1004: los NPC humanoides usan los clips de PJ01

Playtest de Tony: "NPC con las mismas animaciones del jugador: bandidos, peatones, policía, guardias de Nova, meseras... Que nadie camine con clips viejos o raros."

## Qué había
- Los visuales (Visual_*.prefab) usaban el ControladorBase con un override por papel. Aliados, NPC, SNAKES, Profes y Robots caminaban con los clips de Mixamo de la base (Loco_*); SKULLS, el jefe y los guardias de Nova con clips propios (Skull_*, Jefe_*, Nova_*).
- Policia, Mecanico y MeseraRobot (esta última también en las meseras del casino de Ciudad_Prologo, que apuntan al .controller del modelo) tenían un controlador propio de modelo con clips del propio FBX.
- Todos los modelos ya eran Humanoid (animationType 3); no hizo falta reconfigurar ninguno.

## Qué se hizo
- `ConstructorControladorBase.ClipsPorPapel`: todos los papeles (menos Jugador, que ya los tenía) usan `ClipsDeJugadorPJ01`: los mismos 80 clips PJ01_* que el jugador (idle, caminar, correr, salto, agachado, puños, pistola, rifle, muertes, cobertura, interacciones). Los parámetros del controlador no cambian, así que `VisualPersonaje` y los scripts de IA siguen igual.
- Policia, Mecanico y MeseraRobot conservan su máquina de estados y parámetros (Velocidad, EnPiso, Apuntando, KO, Golpe...). Solo cambia el clip de cada estado a su equivalente PJ01 (`NpcClipsPJ01T1004.ClipsDeEstado`). Así las meseras de la ciudad (escena sin tocar) también caminan con PJ01.
- Los visuales de NPC traen la velocidad de los clips sin implante (caminar 1.293 m/s, correr 3.19 m/s) para que la rapidez del Animator siga a la velocidad real.
- Visual_Policia: escala del modelo 1.4646 a 1.5235 porque con el idle de PJ01 (más agachado) medía 1.73 m animado y la prueba pide 1.80.
- Herramienta: `NpcClipsPJ01T1004.Aplicar` (batchmode `-executeMethod MechaCyberpunk.Personaje.Visual.Editor.NpcClipsPJ01T1004.Aplicar`). Se puede repetir; reconstruye el ControladorBase, así que si cambia el asset del controlador sin necesidad se descarta con git restore.

## Pruebas
- EditMode `NpcClipsPJ01T1004Pruebas`: recorre todos los prefabs humanoides del proyecto (menos el muñeco gris y Visual_Prueba) y los controladores de la escena Ciudad_Prologo y falla si algún clip no es PJ01_*.
- PlayMode `NpcCaminanConClipsPJ01T1004Pruebas`: peatón, pandillero SKULLS, guardia de Nova y policía caminando; suena `PJ01_Caminar_SinImplante` y ningún clip que no sea de PJ01. Capturas en docs/capturas/t1004/ (frente y lado de cada uno).
- Se actualizaron las pruebas que fijaban los clips viejos: `EnemigosAnim2Pruebas`, `ClipsBlenderPJ01Pruebas.OtrosPapeles_*`, `ImplanteLocomocionT739EditPruebas`, `ControladorPJ01T733Pruebas.LosVisuales_*`, `PoliciaS1Pruebas` (mira el frente del pecho y no la punta del pie, que cambia con la pose).

## Fuera de alcance / notas
- ModeloHumano / ModeloJugador (MunecoHumano, no Humanoid, grises del creador de pruebas) y Visual_Prueba no se tocaron.
- Con los clips de PJ01, los NPC pierden los clips propios de SKULLS (burla de Skull), jefe (Jefe_Burla, Jefe_Ataque, Jefe_Muerte) y Nova (Nova_Punch, Nova_Flexing...): ahora usan la guardia de puños, el disparo y las muertes de PJ01.
