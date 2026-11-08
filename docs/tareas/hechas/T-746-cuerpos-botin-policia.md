# T-746 Cuerpos en el piso, botín y policía
Dueño: Tony | Rama: tony/T-746-cuerpos-botin-policia | Modelo: Sonnet 5.5, medium
Depende de: T-686 (botín), T-729 (policía), T-707 (patrulla)

## Objetivo
Los enemigos y policías abatidos se quedan tirados con su caída existente, sueltan dinero, balas y arma; el policía mide 1.80 m; la patrulla gira como carro.
Fuente: docs/playtest-07nov.md (sesión 1).

## Qué se hizo
1. Cuerpos: `segundosParaDesaparecer` de todos los DatosPandilleroAPie pasa de 4 a 45 s. El cuerpo ya no tiene colisión (CharacterController apagado) ni dispara (KO). Tope `PandilleroAPie.MaximoCuerpos` = 12: el más viejo se retira primero, de preferencia el que no se ve en cámara.
2. Botín: ya soltaban dinero y arma (T-686). Nuevo `MunicionEnPiso` (balas, se recogen al pasar, suman a ReservaBalas del arma limitada que lleva el jugador); `ColocadorBotin` lo suelta con probabilidad `DatosBotin.ProbabilidadMunicion` (0.6) solo si el jugador tiene un arma que use balas de reserva (la pistola es infinita). Aviso "+N BALAS" en la cola de avisos del HUD (HudAPie escucha `MunicionEnPiso.Recogida`); el dinero ya avisa por la cartera.
3. Policía: no estaba grande: medido con BakeMesh mide 1.80 m (PJ01 1.84). La prueba `PoliciaS1Pruebas` medía los bounds del renderer de piel (2.7 m); ahora mide con BakeMesh.
4. Patrulla: `PatrullaMundo.GirarHacia`: el giro por cuadro no pasa de los metros avanzados / radio mínimo (3.6 m, el del carro del jugador). Detenida no gira; ya no da vueltas sobre su eje (antes 360 grados/s fijos).

## Carpetas permitidas
IA/, Personaje/, UI/HudAPie (una suscripción), Trafico/Persecucion, y sus pruebas. No se tocó ciudad, fachadas, calles, casino, menús, Core, Vehiculos.

## Criterios de aceptación (con pruebas)
- [x] Enemigo abatido con la pistola (entrada simulada) sigue en el piso a los 5 s, sin colisión, y suelta dinero que el jugador recoge (CuerposBotinT746Pruebas)
- [x] Policía abatido se queda tirado y suelta dinero; tope de cuerpos (idem)
- [x] Balas sueltas se recogen al pasar y avisan (idem)
- [x] Policía mide 1.80 m animado (CuerposBotinT746Pruebas, PoliciaS1Pruebas)
- [x] Patrulla que persigue no gira sobre su eje y va sobre el suelo (PatrullaGiroT746Pruebas)

## Pendiente / decisiones
- La patrulla sigue siendo cinemática por calles (T-707); no se pasó a la física del carro del jugador (Vehiculos es de otra área y el intento anterior rompió 10 pruebas): solo se limitó el giro con la misma regla del carro. Pasarla a EntradaCarro queda para una tarea de Vehiculos.
- El arma suelta sigue siendo siempre la pistola del que la lleva (los que no tienen pistola no sueltan arma); pruebas T-686 lo exigen.
- Las pruebas de pandilla (T-195 y recorrido de implantes) esperaban que el vencido desapareciera a los 4 s; ahora esperan 1 s y cuentan solo a los que siguen en pie.
- Fallas que ya existían en la base 25fa3b2 (verificadas sin mis cambios, no son de esta tarea): BalasCiudadT711 (callejón de los SKULLS), HeadshotPandilleroT677, PatrullaFisicaT707.Patrulla_EsUnCarroSolido (Barrio sin piso con collider), PersecucionBarrio.Dron_SeAcercaAlJugador (T-745 le puso collider al dron).
- PandillaArmadaPruebas.VencerlosPagaYTraeDrones: se salta un dinero ya recogido al pasar junto al anterior (con los cuerpos tirados se amontonan).
- Captura s1e-T-746-patrulla-persiguiendo.png: el Barrio de la prueba no trae las fachadas finales; se ve la patrulla de frente en la calle.
