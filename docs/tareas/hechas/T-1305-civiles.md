# T-1305: civiles que sufren, huyen y se defienden

Origen: playtest 17-nov, punto 5. Rama `tony/T-1305-civiles`. Áreas: Trafico/Peatones, Trafico/Datos, pruebas de Trafico.

Ya existía (T-443, T-663): peatón con cápsula e `IRecibeDanio`, muerte y huida al oír disparos (`EventosRuido`). Se agregó:

- Vida del civil 15 a 40 (`ConfigPeatones.VidaPeaton`).
- Caída suave a 90° en 0.5 s al morir; queda tirado 10 s y se esconde (igual que antes).
- Matar a un civil suma calor (`CalorPorMuerte`, 25, motivo Pelea) por `Registro<ICalor>`, así `CalorConTestigos` exige testigos. Solo si el daño se atribuye al jugador (`ReglaCivil.EsAgresionDelJugador`: sin atacante, golpe a menos de 3 m o bala alineada con el jugador).
- Herido sin morir: `ReglaCivil` decide, con 30 % si el jugador está a 1.5 m o menos, revancha (se voltea y da hasta 2 golpes de 5, cada 0.7 s, máximo 3 s) y luego huye; el resto huye.
- Todos los números en `ConfigPeatones` (sección T-1305). El asset existente usa los valores por defecto.

Pruebas: `Tests/EditMode/Trafico/ReglaCivilT1305Pruebas.cs`, `Tests/PlayMode/Trafico/CivilesT1305Pruebas.cs`.

Pendiente de verificar en Unity (no se compiló ni se corrió nada): compilación y las dos clases de pruebas. Sin animación de correr: los peatones son cubos sin Animator. Los .cs nuevos necesitan que Unity genere su .meta.
