# T-1485: cámara a pie pulida

Rama `tony/T-1485-camara`. Sin Unity en esta sesión: nada compilado ni probado en vivo (ver "Qué probar").

## Qué cambió
- `Personaje/Movimiento/CamaraHombro.cs`: balanceo, FOV al correr, suavizado de apuntar propio, colisión con recuperación suave, `CinemachineImpulseListener`.
- `Personaje/Datos/ConfigCamaraHombro.cs`: sección "Pulido (T-1485)" con Tooltip y unidades. Los campos nuevos tienen valor por defecto en C#, así que `ConfigCamaraHombro_Base.asset` los toma solo al cargar (no se tocó el asset; se guardan la próxima vez que Unity lo guarde).
- Nuevos en `Personaje/Camara/`: `LogicaCamaraAPie` (FOV, suavizado), `BalanceoCamara` (balanceo), `TemblorCamara` (contrato de temblor). Los dos primeros son lógica pura.
- Pruebas: `Tests/EditMode/Personaje/CamaraPulidaT1485EditPruebas.cs`.

## Valores (Config, sección Pulido)
| Campo | Valor | Unidad |
| --- | --- | --- |
| fovExtraCorrer | 7 | ° |
| velocidadInicioFovCorrer / FinFovCorrer | 5 / 7.5 | m/s (correr = 7.5) |
| suavizadoFov | 5 | 1/s |
| suavizadoApuntar | 10 (antes `suavizado` 12) | 1/s |
| balanceoVertical / Lateral | 0.035 / 0.02 | m |
| balanceoCiclosPorMetro | 0.25 | ciclos/m |
| balanceoVelocidadReferencia | 7.5 | m/s |
| balanceoSuavizado | 6 | 1/s |
| recuperacionColision | 0.35 | s |
| gananciaTemblor | 1 | x |

## Revisión de jitter (causa)
- El jugador se mueve con CharacterController en `Update`, sin Rigidbody: no hay interpolación ni FixedUpdate que desfasen. `CamaraHombro` corre en `LateUpdate` con orden -200 y el Brain en `LateUpdate` (UpdateMethod y BlendUpdateMethod): la cámara lee la posición final del cuadro. No se encontró causa de jitter ahí.
- Lo que sí daba saltos: al despegarse de un muro la cámara recuperaba su distancia con 0.2 s en el ThirdPersonFollow y 0 s en el Deoccluder (salto brusco). Ahora ambos usan `recuperacionColision`. Entrar en colisión sigue siendo instantáneo (para no atravesar paredes). El radio de colisión sigue siendo `radioColision` (0.2 m).
- El balanceo se suma después de calcular techo y hombro, para no disparar esas colisiones.

## Comportamiento
- Balanceo: fase por distancia recorrida; amplitud proporcional a la rapidez real (`controlador.Velocidad`); 0 quieto, apuntando, en el aire o con zoom de sniper.
- FOV: +7° al correr (de 5 a 7.5 m/s), suave; apuntando va al FOV de apuntar sin extra.
- Apuntar: ya existía (hombro, distancia, FOV 45); solo se pulió el tiempo (`suavizadoApuntar`).

## Contrato común de temblor (para T-1486, T-1487, T-1489)
La cámara a pie (VcamAPie) tiene un `CinemachineImpulseListener` en el canal `TemblorCamara.Canal` (=1), ganancia `gananciaTemblor`, en espacio de cámara.
Cada sistema:
1. Pone un `CinemachineImpulseSource` en su objeto (por código con `AddComponent` o desde el prefab).
2. Una vez: `TemblorCamara.Preparar(fuente)` (canal 1, impulso uniforme sin depender de la distancia, forma Bump, 0.2 s; los otros parámetros cambian duración y forma).
3. Cada vez: `TemblorCamara.Generar(fuente, fuerza, direccion)`.

| Evento | Fuerza | Dirección | Duración / forma |
| --- | --- | --- | --- |
| Disparo chico (pistola) | `TemblorCamara.DisparoChico` 0.25 | `Vector3.down` | 0.12 s, Bump |
| Disparo grande (escopeta, rifle) | `DisparoGrande` 0.6 | `Vector3.down` | 0.2 s, Bump |
| Golpe (cuerpo a cuerpo) | `Golpe` 0.8 | hacia donde pega | 0.2 s, Bump |
| Daño recibido | `Danio` 1.0 (escalar con el daño: 0.4 a 1.5) | de dónde viene el golpe | 0.3 s, Rumble o Bump |

Son valores de partida: afinar viéndolo en juego (la ganancia global está en el Config).

## Métodos de editor
Ninguno. Todo se arma por código al despertar la cámara; no se tocó el prefab ni la escena.

## Qué probar (cuando haya Unity)
1. Compilar: confirmar las firmas de Cinemachine 3 usadas: `CinemachineImpulseListener` (ChannelMask, Gain, Use2DDistance, UseCameraSpace) y `CinemachineImpulseDefinition` (ImpulseChannel, ImpulseType.Uniform, ImpulseShape, ImpulseDuration). Si algún nombre difiere, es un ajuste de una línea.
2. EditMode `CamaraPulidaT1485EditPruebas`, y que `CamaraHombroT1001EditPruebas` y `CamaraCinemachineT723EditPruebas` sigan pasando.
3. En juego: correr (balanceo y FOV +7°), quieto (sin balanceo), apuntar (transición suave), caminar pegado a una pared y despegarse (sin salto), un `GenerateImpulse` de prueba mueve la cámara.
