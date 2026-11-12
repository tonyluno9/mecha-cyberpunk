# T-962: golpes, muertes y levantarse con los clips de Blender (Sesión A, A3)

**Hecho.** El jugador usa `PJ01_Punos_GolpeIzq`, `GolpeDer`, `Fuerte`, `PJ01_Muerte_Adelante`, `Atras`, `Desmayo` y `PJ01_Dano_Levantarse_BocaArriba`, `BocaAbajo`.
`PJ01_Punos_Guardia` queda importado pero sin estado: el Animator no tiene guardia de puños y no se inventó una.

## Desplazamiento de raíz: decisión
Medido (`MedirRaizClipsPJ01`): las muertes avanzan 0.9 a 1.2 m y levantarse 0.7 m; los puños regresan a su sitio (hasta 0.43 m de ida y vuelta).
- **Muertes y levantarse: root motion**, no Bake Into Pose. El jugador se levanta a la vista (al abrirse el fundido de la clínica y tras salir volando de la moto); con Bake Into Pose el cuerpo quedaba hasta 1.2 m lejos de la cápsula y al levantarse regresaba deslizándose. `RaizCaidaJugador` (lo agrega `AnimacionJugador` al Animator) mueve el CharacterController con el avance del clip solo en esos cinco estados. Medido: la cápsula se movió 0.90 m al morir y, ya de pie, la cadera queda a 0.08 m de ella.
- **Puños: en su sitio** (como estaban): son de la capa de arriba y con Bake Into Pose los pies (de la capa base) se deslizarían con la estocada.
- Los NPC no cambian: siguen con sus clips de Mixamo y sin root motion.

## Rapidez
- Muerte del jugador: `RapidezMuerteJugador` 1.8 → 1 (los clips ya vienen 1.7 veces más rápidos que los de Mixamo, como pidió Tony). Caen en 1.3 a 1.5 s.
- Puños del jugador: `RapidezGolpeJugador` 3.3 → 2.7. Los clips nuevos duran 0.8 de los de Mixamo con el impacto en la misma fracción: conectan a 0.13-0.21 s y terminan a 0.53-0.68 s, como antes.

## Capturas (revisadas contra 04_vista_frente, 04_vista_lado y 04_vista_golpe)
`docs/capturas/pj01-golpes-*.png` y `pj01-muertes-*.png`. Pies en el piso; las muertes llegan al suelo sin atravesarlo (punto más bajo -1.2 cm).

## Pruebas
EditMode 2306/2306. PlayMode `MuerteRaizT962Pruebas` (nueva), `MuerteJugadorAnimadaPruebas`, `VelocidadesAnimacionT652Pruebas`, `LanzamientoMotoT696Pruebas`, `DanoPJ01T735Pruebas`, `JugadorPJ01T732Pruebas`, `JugadorVisualPruebas`, `AnimatorPJ01T733Pruebas`: 35/35.
