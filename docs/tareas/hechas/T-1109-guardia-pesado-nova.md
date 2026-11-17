# T-1109: guardia pesado de Nova (E12)

Rama `tony/T-1109-guardia-pesado-nova`. Diseño: `docs/diseno/enemigos-nuevos-16nov.md`, sección 3.

## Qué se hizo
- `IA/APie`: `ReglaEscudoFrontal` (pura: ángulo < 70, reduce 85%, aguante 150, rotura, vuelve a los 5 s), `ReglaGuardiaPesado` (pura: guardia, persigue, empujón 0.7 s/18, mazazo cada 3 ataques 1.1 s/28, recupera 1.2 s, aturdido 1.2 s), `EscudoFrontal : IFiltroDanioAPie` (ráfaga de 24 chispas amarillo-blancas a la altura del escudo, destello de luz de 0.15 s, sonido ARM_BALA_IMPACTO_METAL, el escudo que se ve es el del modelo E12), `ConductaGuardiaPesado : IConductaAPie` (gira 90°/s, solo camina hacia adelante, `Provocar()`, `VolverAlPuesto()`, vuelve a su puesto si el jugador está a más de 30 m) y `PuestoGuardiasPesados` (guarda a los 2 guardias para T-1110).
- Datos `DatosPandilleroAPie_GuardiaPesadoE12` (vida 220, velocidad 2.2, color base blanco), prefab `GuardiaPesado_E12`, material `Mat_ChispaEscudoNova` (chispas aditivas).
- Colocador `Assets/Arte/Editor/ColocarT1109GuardiasPesados.cs` (raíz `T1109_GuardiasPesados`, idempotente): torre principal = Edificio_1 de S2c_Nova (centro 110, -58.8, techo 36.5); guardias en banqueta a ±4 m de la puerta: (105.98, -49.75) y (113.98, -49.75), mirando al norte (a la calle). Eje de la calle z=-43 libre (a 6.75 m).

## Criterios y evidencia
- [x] EditMode `ReglaGuardiaPesadoPruebas` (9) y `EscudoFrontalPruebas` (ángulos 0/60/69/80/180, aguante, rotura, 5 s): suite completa 2552/2552.
- [x] PlayMode `GuardiaPesadoT1109Pruebas`: 9 pasan + 1 de capturas (ignorada sin T1109_CAPTURAS), dos corridas iguales. Cubre puños de frente <= 15% de los de espalda, balas de pistola igual, TipoBloqueo Normal/Ninguno, romper el escudo (aturde, castigable x1.5, vuelve a los 5 s), sin provocar no ataca en 5 s, empujón 18 a 1.5 m y tercer ataque mazazo 28, vuelve a guardia a >30 m, giro lento.
- [x] PlayMode `GuardiasPesadosCiudadT1109Pruebas`: 4 pasan + 1 de capturas, dos corridas (existen 2 en piso, sin encimar, entrada libre, a >= 6.4 m de carriles, de guardia mirando a la calle).
- [x] `GuardiaNovaDanioT715Pruebas` 3/3, `JefeSnakesT1108Pruebas` 6/6 (+1 ignorada), `JefeNovaT693Pruebas` 3/4 (la que falla, `CadaTresGolpes_CargaEnLinea`, ya falla en la base).
- [x] Capturas en `docs/capturas/t-1109/` (guardias-en-la-torre.png, guardia-bloqueando.png).

## Notas
- La prueba de pistola usa un pedestal como `HeadshotPandilleroT677Pruebas` (la cámara sobre el hombro queda más alta que la cabeza del guardia).
- `Provocar()` lo llama T-1110 al entrar el jugador al recinto; los golpes también lo provocan.
- Corrección: se quitó la placa azul (el modelo E12 ya trae su escudo en el antebrazo izquierdo). Un golpe bloqueado (TipoBloqueo.Normal) ya no pinta de rojo ni hace herir al guardia: una condición en `PandilleroAPie.RecibirDanio` (`bloqueoFiltro != TipoBloqueo.Normal`).
