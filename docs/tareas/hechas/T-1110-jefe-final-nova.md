# T-1110: jefe final de Nova (E14) y su recinto

Rama `tony/T-1110-jefe-final-nova` (sale de `tony/T-1109-guardia-pesado-nova`). Diseño: `docs/diseno/enemigos-nuevos-16nov.md`, sección 4.

## Qué se hizo
- `IA/APie`: `ReglaJefeFinalNova` (pura: fases por vida 66/33, ciclos de ataques, escudo 140, regreso a 10 s, aturdido 3 s, avisos -20% y +30% de velocidad en Furia, cañonazos con aros programados), `EscudoEnergia : IFiltroDanioAPie` (absorbe 100%, TipoBloqueo.Perfecto, esfera cian que pulsa al recibir golpe), `ConductaJefeFinalNova : IConductaAPie` (movimiento, puñetazo en arco de 120 grados y radio 3.5, aros del piso con LineRenderer, onda de choque, gritos "¡Escudo activado!" y "¡Sobrecarga!" con reintento si el enfriamiento de gritos los tapa, brillo rojo/cian/violeta, registro de jefe vencido de Nova), `RecintoJefe` (zona trigger revisada cada cuadro, barrera, grito "Nadie entra a la torre de Nova.", provoca guardias, reset a más de 40 m o si el jugador muere, abre y apaga guardias al vencer) y `ConstructorRecintoJefe` (solo editor, lo usan el colocador y las pruebas).
- `ConductaGuardiaPesado.Reiniciar` y `PuestoGuardiasPesados.ReiniciarTodos/ApagarTodos` (T-1109) para el recinto.
- Datos `DatosPandilleroAPie_JefeFinalNovaE14` (vida 600, velocidad 2.8, color base blanco), prefab `JefeFinalNova_E14` (cápsula 2.3 m), materiales aditivos `Mat_AroEmisivoNova`, `Mat_EsferaEscudoEnergia`, `Mat_BarreraEnergiaNova`.
- Colocador `Assets/Arte/Editor/ColocarT1110JefeFinal.cs` (raíz `T1110_JefeFinalNova`, idempotente): recinto en la calle de Nova, zona x 107.5 a 127.5 y z -52 a -34, barrera en x=103.5 (abierta y apagada al inicio), jefe en (124, -46) mirando al oeste, guardias de T-1109 junto a la puerta de la torre. A 105 m de la ruta de la demo; el eje z=-43 queda libre.

## Decisiones
- La torre principal no deja un círculo de 14 m al pie (la banqueta es de 4 m): el recinto es el tramo final de la calle de Nova, sin tocar su eje.
- Ráfaga de 3 cañonazos: uno en el jugador y otros dos a +3 m y -3 m sobre la dirección en que se mueve (si está quieto, la dirección jefe-jugador); explotan escalonados 0.25 s. El doble cañonazo de la fase 2 es secuencial (el segundo aro aparece cuando explota el primero).
- Pausas entre ataques (el diseño no las fija): 0.8 s tras puñetazo y cañonazo, 1 s tras onda, 0.2 s entre puñetazo y onda en Furia.
- El jefe solo pelea mientras el recinto lo activa; fuera de eso espera en su punto.

## Criterios y evidencia
- [x] EditMode `ReglaJefeFinalNovaPruebas` (25 con los TestCase): suite completa 2577/2577.
- [x] PlayMode `JefeFinalNovaT1110Pruebas`: 11 pasan + 3 de capturas, dos corridas iguales (puñetazo 25 a 2 m, cañonazo 30 quieto y 0 con A/D simuladas, escudo Perfecto, romper el escudo aturde 3 s y vuelve a 10 s, onda 20 a 5 m, barrera, guardias provocados, jefe vencido abre y apaga, reset a 45 m, reset si el jugador muere).
- [x] PlayMode `JefeFinalCiudadT1110Pruebas`: 4 pasan + 1 de capturas, dos corridas. Regresión: GuardiaPesadoT1109 9/9, GuardiasPesadosCiudadT1109 4/4, JefeSnakesT1108 6/6, JefeNovaT693 3/4 (la falla ya está en la base), `RecorridoDemoPruebas` 3/3 (en la escena con el recinto).
- [x] Capturas en `docs/capturas/t-1110/`: recinto.png, fase1-canonazo.png, fase2-escudo.png, fase2-onda.png, fase3-rafaga.png.

## Notas
- `JefeNovaT693Pruebas.CadaTresGolpes_CargaEnLinea` ya falla en la base.
- Corrección: la esfera del escudo usa el shader `MechaCyberpunk/EscudoEnergia` (alfa 0.2 en el centro, borde con fresnel; el golpe sube el brillo 0.25 s) y se ve al jefe con su textura dentro. Recapturas fase2-escudo y fase2-onda.
