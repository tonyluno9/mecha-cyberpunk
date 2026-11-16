# T-1107: riña SNAKES contra SKULLS y SNAKES con el visual E10

Rama `tony/T-1107-rina-snakes-skulls`. Diseño: `docs/diseno/enemigos-nuevos-16nov.md`, sección 1.

## Qué se hizo
- `ReglaRina` (pura): elige al rival válido más cercano de otra pandilla (radio 15 m), cuándo se suelta (jugador a pie a 6 m o le pegó a alguno), bando caído, rearme (jugador a más de 80 m y nadie en cámara), daño 35% y gritos de riña.
- `ConductaRina : IConductaAPie`: busca rival, se acerca, prepara y golpea con el alcance y el ritmo de sus datos pero con 35% del daño; grita ("¡Fuera de aquí, calaca!", "¡Lárgate, víbora!"...).
- `RinaPandillas`: arma la riña con 2 `PandillaAPie`. Estados Rina / Liberada (los seis van contra el jugador) / Terminada (un bando cayó: vuelven a su punto con la IA normal). Rearma sola (Reaparecer de todos, de frente al otro bando).
- `ConstructorRina` (solo editor): arma una riña con prefabs y datos dados; lo usan el colocador y las pruebas.
- Prefab `IA/Prefabs/PandilleroArmado_Snakes.prefab` (visual E10 + pistola W01, datos Viboras), usado por las riñas.
- `Assets/Arte/Editor/ColocarT1107Rina.cs` (raíz `T1107_Rinas`, idempotente): 2 riñas por sondeo de física (calle plana y despejada): `Rina_Frontera` en (75, 0.2, -16) y `Rina_NeonAlto` en (101, 0.2, -12) (plazas de Neón Alto; las únicas libres con la regla de carriles), a 63 y 65 m de la ruta de la demo (mínimo exigido 30 m; el colocador pide 45 m). Cada huella queda a 6.4 m o más del eje de las calles de tráfico (carril a 2.4 m + 4 m; el mismo grafo del tráfico) y a 7 m o más de cualquier puerta. Evita la calle de Nova y la de los suburbios (z=-43) y la franja de salida este (z=0, x>=110) porque otras pruebas las exigen libres.

## Corrección de la revisión
- SNAKES verde plano: `PandilleroAPie` pinta el cuerpo con `colorNormal` de sus datos (verde de Viboras) sobre la textura. Los SNAKES de las riñas usan ahora `DatosPandilleroAPie_ViborasE10.asset` (copia de Viboras con color base blanco); el prefab `PandilleroArmado_Snakes` también. Prueba `Ciudad_LosSnakesSeVenConSuTexturaE10_SinTinteDePandilla`: material E10 con su `_BaseMap`, nunca el verde, y en algún cuadro todos con el color base del material.
- Gritos en espejo: solo era la cámara de captura (el grito mira a `Camera.main`); la captura ahora es MainCamera y las demás se apagan. En juego sale bien.
- Tráfico: ver sitios arriba; prueba `Ciudad_NadieEstaEnUnCarrilDeTrafico_AlMenosCuatroMetrosDelCarril`.

## Decisiones
- La riña no mata: nadie baja de 25% de vida por golpes de riña. Si no, cada KO sumaría búsqueda (`calorAlVencer`) y registraría vencidos en el territorio sin que el jugador haga nada. "Un bando cae" = nadie queda en pie por encima de 25%.
- Con el 35% de daño el bando cae en unos 20-40 s, no 1 min; se ajusta con `ReglaRina.FraccionDanio` / `FraccionMinimaVida`.
- Mientras dura la riña se marca `Acosando = true` (no piden refuerzos ni cambian de alerta), `ModoMision` y `TerritorioSuprimido`, como `GrupoAcoso`: no hizo falta tocar `PandilleroAPie.cs`.
- Los 3 SNAKES que ya había (grupo T402, `Pandilla_Viboras`) NO pasan a E10: `PersonajesT510Pruebas.Pandilleros_DeCadaPandilla_UsanManConSuColor` exige man_Viboras en ellos y esa prueba es de otra área. Cambiarlos es sencillo (instanciar `PandilleroArmado_Snakes` en su lugar) pero hay que actualizar esa prueba.
- Al volver a su punto patrullan alrededor de él (radio de patrulla de sus datos).

## Criterios
- [x] EditMode `ReglaRinaPruebas`: 8/8 pasan.
- [x] PlayMode `RinaPandillasT1107Pruebas`: 5/5 pasan (dos corridas seguidas): se pegan entre sí (vida de ambos baja en 10 s), jugador a 5 m: 6 con `ObjetivoActual` = jugador, daño del jugador a uno suelta a los dos bandos, un bando cae y los demás vuelven a su punto, rearme.
- [x] PlayMode `RinasCiudadT1107Pruebas`: 3/3 pasan (dos corridas): 2 riñas, 6 por riña, sobre el piso, sin nadie dentro de paredes, a 30 m o más de la ruta, SNAKES con material E10, pelean sin jugador.
- [x] EditMode completo: 2523/2523 pasan (antes de T-1108).
- [x] `PandillasCiudadS2Pruebas` 2/2, `GrupoAcosoPruebas` 8/8, `PandillaPruebas` 5/5, `RecorridoDemoPruebas` 3/3.
- [x] Capturas en `docs/capturas/t-1107/` (rina-1-8m, rina-1-20m, rina-2-8m, rina-2-20m).

## Pendiente
- Los SKULLS se ven casi negros de noche (su material); no es de esta tarea.

## Nota
- `RecorridoDemoPruebas` es intermitente: falló 3 veces en el tramo 09 (el jugador se queda en el Doc, x=-101, lejos de las riñas) y pasó 3/3 en 3 corridas siguientes con la misma escena y con la escena sin riñas. No depende de ellas.
