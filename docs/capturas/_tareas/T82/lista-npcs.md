# T82: lista de NPCs humanos, clips y velocidades

Referencia del jugador: caminar PJ01_Caminar 1.62 m/s; correr (Shift) PJ01_Trote 4.53 m/s (Override_Jugador). Todos los humanos usan Override_HumanoJugador
(T-53, `HumanoJugadorT53.Visuales`) con `ConfigurarComoJugador`: mismos clips y umbrales. El Animator se acelera 0.85x a 1.15x contra la velocidad real, así que más fuera de 1.62/4.53 se desliza.

Causa del deslizamiento de los SKULLS del inicio: `MolestiaSkulls` los movía con la velocidad del clip VIEJO (1.293 m/s x 1.7 = 2.2 m/s al salir y venir, 5.5 m/s al irse y 6.2 al alcanzar),
no con las del jugador. Con 1.62 de clip, 2.2 m/s excede el tope 1.15x (cap de 1.15x: se arrastra) y 5.5 pasa del trote 4.53 x 1.15 = 5.2.

| NPC | Controlador/clips | Caminar (antes -> ahora) | Correr (antes -> ahora) | Dónde |
| --- | --- | --- | --- | --- |
| SKULLS del inicio (SkullRecorrido) | Override_HumanoJugador | 2.2 -> 1.62 (x escala) PENDIENTE director | 5.5 / 6.2 -> 4.53 PENDIENTE director | Progresion/Historia/Recorrido/MolestiaSkulls.cs (otra zona, ver cambio pedido) |
| Pandilleros (SKULLS, SNAKES, Calaveras, Víboras, jefes, Nova humano, E18 élite) | Override_HumanoJugador | patrulla 1.6 (ya) | persecución 4.5 -> 4.53 (Base 5.5 -> 4.53) | DatosPandilleroAPie_*.asset (`velocidad`) |
| Guardia pesado humano E12 | Override_HumanoJugador | 2.2 -> 1.62 | n/a | DatosPandilleroAPie_GuardiaPesadoE12.asset |
| Policía | Override_HumanoJugador | caminar apuntando 1.4 -> 1.62 | 3.6 -> 4.53 (persecución 4.5 -> 4.53) | PandilleroAPie.VelocidadCaminarPolicia / VelocidadCorrerPolicia; DatosPandilleroAPie_Policia |
| Confrontación a pie (el que se acerca a hablarte: Skulls, Snakes, Guardias, Policía, Nova) | Override_HumanoJugador | 1.2 -> 1.62 | n/a | IA/Resources/Confrontacion/Config_*.asset (`velocidadCaminar`) |
| Civil acosado (huye) | su visual civil (humano) | n/a | 5.5 -> 4.53 | DatosAcoso_Base.asset (`velocidadHuida`) |
| Peatones de Trafico (PeatonMundo) | Override_HumanoJugador (visuales NP* de la lista T-53) | 1.4 -> 1.62 | huida 1.4 x 2.6 = 3.64 -> 1.62 x 2.8 = 4.54 | ConfigPeatones_Trafico.asset (`velocidad`, `multiplicadorHuida`) |
| NPCs sentados (NpcSentado) al huir | playable propio | n/a | 4.2 -> 4.53 | Trafico/Peatones/NpcSentado.cs (`VelocidadHuida`) |
| Aliados (Doctor, Mecánico, AL10, AL11, Inventor) | Override_HumanoJugador | los mueve la historia/rival; sin velocidad propia que cambiar | n/a | n/a |
| Guardia Nova (GuardiaNova) | Override_HumanoJugador | patrulla 1.6 (ya) | 4.5 -> 4.53 | DatosPandilleroAPie_Nova |

No se tocan (no son humanos o su diseño es propio): robot pesado E16 (2.2), jefe final Nova E14 (2.8, robot), perro robot E17 (6.5), carga del matón E20 (11 m/s, ataque de carga),
mechas (Rival), jugador.

Assets que cambia `MechaCyberpunk.IA.Editor.VelocidadesNpcsT82.Aplicar()`; medición en Play: `MechaCyberpunk.IA.Editor.VelocidadesNpcsT82.MedirSkulls()`.
