# T-943: giro al disparar, muerte más rápida y respawn con el Doc

- Giro: al disparar sin apuntar, el cuerpo mira 0.6 s hacia donde va el disparo (`ConfigCamaraHombro.SegundosGiroAlDisparar`). Sin disparar, no cambia nada.
- Muerte: la espera antes de la pantalla baja de 3 a 1.2 s, la entrada de HAS MUERTO de 0.9 a 0.35 s y la pantalla de 2.6 a 1.4 s. La caída del jugador va a 1.8x; los NPC no cambian.
- Respawn: en una partida nueva la historia se queda en `PeleaTutorial` y el Doc se negaba a actuar porque el prólogo ya no está en la ciudad. Ahora la etapa solo cuenta si hay un `DirectorPrologo` en la escena.

Pruebas: PlayMode `GiroAlDispararT943Pruebas`, `MuerteReapareceEnClinicaPruebas` (con partida nueva), `HasMuertoT689Pruebas`, `AgacharseT673Pruebas`, `EventosPistolaT734Pruebas`, `MuerteJugadorAnimadaPruebas`.
