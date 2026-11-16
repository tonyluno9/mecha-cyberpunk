# T-1108: jefe SNAKES (E11), mini jefe con cuchilla

Rama `tony/T-1108-jefe-snakes` (sale de `tony/T-1107-rina-snakes-skulls`). Diseño: `docs/diseno/enemigos-nuevos-16nov.md`, sección 2.

## Qué se hizo
- `ReglaJefeSnakes` (pura): fases Acecho (rodea de lado, cambia de sentido cada 1.5-2.5 s), AvisoTajos 0.45 s, 3 tajos cada 0.25 s, recuperación 0.9 s, AvisoEstocada 0.6 s, estocada 0.5 s, recuperación 1.4 s (castigable solo ahí). Nunca dos estocadas seguidas. Al 50% de vida pide refuerzos una vez y los avisos bajan 25%.
- `ConductaJefeSnakes : IConductaAPie`: traduce la regla a movimiento y daño. Tajos de 10 en arco de 90 grados y radio 2.2 m; estocada a 12 m/s en línea recta con la dirección fijada al empezar el aviso (si el jugador se aparta, falla), 22 de daño a 1.2 m; brillo verde con `MostrarAviso`; grito "¡Sssss!" en el aviso de la estocada y "¡A mí, víboras!" al llamar refuerzos. Pelea a menos de 25 m del jugador y lo suelta a más de 40 m (vuelve a su punto). Al vencerlo: `ITerritorio.RegistrarVencido(..., Viboras, jefe: true)` una vez y grito final.
- Datos `IA/Datos/DatosPandilleroAPie_JefeSnakesE11.asset` (vida 260, velocidad 4.2, castigo x1.5; salen de los del jefe de Nova porque el pandillero sin pistola exige el modo cuerpo a cuerpo) y prefab `IA/Prefabs/JefeSnakes_E11.prefab` (PandilleroAPie + ConductaJefeSnakes + Visual_E11_JefeSnakes).
- `ConstructorJefeSnakes` (solo editor): jefe + grupo "Escoltas" (2 SNAKES E10, siempre) + grupo "Refuerzos" (2 SNAKES apagados que se activan al 50%).
- `Assets/Arte/Editor/ColocarT1108JefeSnakes.cs` (raíz `T1108_JefeSnakes`, idempotente): el puesto en (50, 0, -60), en la calle de Neón Alto que baja por x=47, a 93 m de la ruta de la demo; no coincide con las riñas de T-1107.

## Decisiones
- La estocada se lanza de 5 a 7 m (el diseño decía 5 a 9): 12 m/s x 0.5 s recorren 6 m y, para tocar a 1.2 m a quien no se mueve, la salida máxima es 7.2 m. Se ajusta en `ReglaJefeSnakes.Parametros.Diseno`.
- Los tiempos y distancias del patrón están en `ReglaJefeSnakes.Parametros` (el diseño no pedía campos nuevos en `DatosPandilleroAPie`, que otra sesión toca).
- Al vencerlo se registra como jefe vencido de Viboras (como pide la tarea): puede conquistar Neón Alto en el territorio real.
- El lugar es un tramo de calle, no un patio: la ciudad no tiene lotes libres en Neón Alto (edificios de 24 m).

## Criterios
- [x] EditMode `ReglaJefeSnakesPruebas`: 8/8 pasan.
- [x] PlayMode `JefeSnakesT1108Pruebas`: 7/7 pasan (dos corridas, una con capturas): tajos 30 en <= 3 s a 2 m, estocada desde 6.5 m conecta 22, apartarse con D en el aviso la esquiva, refuerzos una sola vez al 50%, jefe vencido registrado, datos (260 / 4.2).
- [x] PlayMode `JefeSnakesCiudadT1108Pruebas`: 1 pasa + 1 de capturas (dos corridas): existe, 2 escoltas, sobre el piso, sin nadie encimado ni dentro de paredes, a 30 m o más de la ruta y de las riñas.
- [x] Capturas en `docs/capturas/t-1108/` (jefe-en-su-lugar, jefe-aviso, jefe-estocada).
