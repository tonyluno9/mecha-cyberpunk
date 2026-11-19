# T-1475 Sonidos de vehículos

Rama: tony/T-1475-vehiculos-sonidos. Sin compilar ni correr pruebas (el worktree no tiene Library): lo hace el orquestador.

## Tipo / evento -> ID

| Tipo | Arranque | Ralentí (bucle) | Apagar | Claxon | Tono base |
| --- | --- | --- | --- | --- | --- |
| Normal | VEH_NORMAL_ARRANQUE | VEH_NORMAL_RALENTI | VEH_NORMAL_APAGAR | VEH_CLAXON_NORMAL | x1 |
| Moto | MOTO_ARRANQUE | MOTO_RALENTI | MOTO_APAGAR | VEH_CLAXON_NORMAL | x1 |
| Moto grande | MOTO_* | MOTO_* | MOTO_* | VEH_CLAXON_NORMAL | x0.8 |
| Pesado/camión | VEH_PESADO_ARRANQUE | VEH_PESADO_RALENTI | VEH_PESADO_APAGAR | VEH_CLAXON_CAMION | x1 |
| Superdeportivo | grupo normal | grupo normal | grupo normal | VEH_CLAXON_NORMAL | x1.25 |
| Eléctrico | VEH_ELECTRICO_ARRANQUE | VEH_NORMAL_RALENTI | VEH_NORMAL_APAGAR | VEH_CLAXON_NORMAL | x1.4 (por revisar a oído) |

El tipo se deduce en CarroViejo (ManejoMoto, masa >= 2500 kg o nombre con camión = pesado; moto >= 300 kg = grande; nombre con "deportivo" = superdeportivo,
"eléctrico" = eléctrico). Se puede forzar con `tipoSonidoManual` + `tipoSonido` (sin tocar prefabs hoy).

| Evento | ID | Dónde |
| --- | --- | --- |
| Subir (jugador) | arranque del grupo, una vez; 0.8 s después el bucle de ralentí siguiendo al carro | CarroViejo.ActualizarMotor / SonidoMotorCarro |
| Tráfico con IA | solo el bucle de ralentí (sin arranque) | igual |
| Tono | 0.8 en reposo a 1.8 a VelocidadMaxima (por el factor del grupo), suavizado, solo se manda si cambia >= 0.01 | ReglasSonidoVehiculoT1475.TonoObjetivo |
| Pisar a fondo desde lento | VEH_ACELERON (pedal suelto a >= 0.9, rapidez <= 25 % del máximo, mínimo 3 s) | SonidoMotorCarro |
| Bajar | detiene el bucle y APAGAR una vez | IntentarBajar |
| Choque | VEH_CHOQUE (> umbral), VEH_CHOQUE_FUERTE (>= UmbralChoqueFuerte), + VEH_CRISTAL_ROMPER (>= 1.5 x fuerte) | DanioCarro.OnCollisionEnter |
| Puerta | VEH_PUERTA_ABRIR / VEH_PUERTA_CERRAR (ya existía) | CarroEstacionado |
| Robar carro estacionado | VEH_FORZAR_PUERTA (ya existía) + VEH_ALARMA en bucle 8 s | CarroEstacionado / AlarmaCarro |
| Tope o bache | VEH_TOPE (salto de velocidad vertical >= 2.5 m/s, rapidez >= 6, mínimo 1.5 s) | CarroViejo.ActualizarTope |
| Claxon del tráfico | VEH_CLAXON_CAMION (pesado) o VEH_CLAXON_NORMAL | CarroTrafico |
| Derrape | sigue VEH_FRENO_DERRAPE (T-1471 lo mapea a VEH_FRENO_FUERTE) | CarroViejo |

Pruebas: Tests/EditMode/Vehiculos/SonidosVehiculosT1475Pruebas.cs. SonidoCarrosT595 no cambia.

## Falta
- VEH_LLANTAS_GRAVA: no se sabe la superficie bajo el carro.
- VEH_PUENTE: no hay juntas de puente marcadas.
- Claxon del jugador: no existe tecla de claxon.
- Alarma al robar un carro del tráfico (solo estacionados).
- Explosiones de carro son de T-1472.
- Los IDs VEH_CLAXON_NORMAL/CAMION, VEH_NORMAL_*, etc. deben estar en la mezcla de T-1471 (VEH_CLAXON_TRAFICO ya no se pide desde CarroTrafico).
