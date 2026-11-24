# Reporte noche 24-nov

Todo va en `main`, con una rama por tarea, merge --no-ff y push (autorizado por Tony).
- Lo de la sesión VR (vr/quest3, VR1–VR35) ya estaba en main al empezar, en `bc2bf35`.
- Las manos VR de Blender entraron con `044e48d`.

## Qué quedó
| Tarea | Rama | Estado |
|---|---|---|
| T-historia-libre-23nov (escalada progresiva) + VR47 | tony/T-historia-libre-24nov | HECHA (sin probar jugando) |
| T-cinematicas-narracion-24nov + T-omitir-cinematicas-24nov | tony/T-cinematicas-narracion-24nov | HECHA |
| T-voces-personajes-23nov (lo que faltaba: narración, Canepa, Tino, mesera, peatones) | tony/T-voces2-23nov y la de narración | HECHA |
| T-musica-24nov + T-volumen-ajustes-24nov | tony/T-musica-volumen-24nov | HECHA |
| POL_RADIO_DESPACHO → POL_RADIO_AVISO y EXO_RUEDA_CAMARA_LENTA | — | Ya estaban hechos. Hay un alias en MapaSonidos20nov, y ENTRAR/SALIR ya suenan en las dos ruedas. Solo lo comprobé. |
| T-drops-municion-vida-24nov + T-indicador-danio-24nov | tony/T-drops-indicador-24nov | HECHA |
| VR39 + VR54 + VR50 | tony/T-vr39-54-50-24nov | HECHA |

### Detalles por tarea
- **Modo libre:**
  - Al alejarte unos 14 m de Tino con el arma y la mejora empieza solo, sin aviso. Sale el objetivo SOBREVIVE y el contador SKULLS / POLICÍAS / NOVA / tiempo debajo del minimapa.
  - Tino atiende siempre (tienda, banco, mejoras).
  - La historia de después (pasos 10–19) quedó desconectada, no borrada.
  - Las patrullas llegan manejando, abren las dos puertas (PuertasCarro) y bajan policías.
  - Los refuerzos nunca aparecen a menos de 25 m ni a la vista, y NOVA nunca tiene más de 2 cápsulas a la vez.
  - Al morir sale HAS MUERTO con las estadísticas y el récord. REINTENTAR te deja junto a Tino con el arma, las mejoras y el dinero de ese momento. Desde el menú, CONTINUAR entra directo al modo libre.
- **Narración:**
  - Cada clip tiene su frase. Si la frase dura más que el video, el último cuadro se congela; después hay 1 s de pausa y no hay negros.
  - El video baja unos 6 dB y la música unos 10 dB mientras se habla.
  - C03 ya trae C07, C04 EL SUERO tiene video, y los videos de Tino (25–27) salen al poner la primera mejora.
  - **OMITIR:** abajo a la derecha. En PC con clic o ENTER; en VR con B o el gatillo.
- **Música:**
  - Piano en la intro, las cinemáticas y la parte guiada; la del campo de tiro dentro del campo.
  - En el modo libre, SKULLS → POLICÍA → NOVA con un cruce de 2.5 s, sin volver nunca a una anterior.
  - Baja 12 dB dentro de interiores. En HAS MUERTO entra el piano.
- **Volumen:** los cuatro deslizadores (General, Música, Efectos, Voces) ya existían con su mezclador y se guardan. Agregué la muestra de voz al mover VOCES.
- **Drops:**
  - Probabilidad de soltar balas: policía 0.9, NOVA 0.85, SKULLS 0.35.
  - Botiquín: 0.15, cura 25. Como no hay modelo de botiquín en el proyecto, se usa la bebida O01.
  - Todo desaparece a los 30 s. Se recoge al pasar o al acercar la mano en VR.
- **Indicador de daño:** destello rojo en la orilla izquierda, derecha, de atrás (abajo) o de arriba, que se apaga en 1 s. En VR es más suave.
- **VR39:** el choque de un cuerpo suena a 3 m/s o más, con 0.6 s entre sonidos. Un cuerpo muerto suena una sola vez y uno quieto nunca.
- **VR54:** se apagó el texto sobre la cabeza de pandilleros, policías y civiles acosados. Las voces siguen; los subtítulos de abajo, también.
- **VR50:**
  - Los objetivos del campo de tiro son el modelo real, con un destello holográfico solo al aparecer, y hay tipos nuevos SKULL y POLICÍA.
  - Un guardia de la sala apaga a quien se meta (NPC, peatón, carro).
  - El botón del menú bajo el láser se ilumina.

## Pruebas
- **EditMode:** 102/102 en las clases nuevas y en las que tocan estos cambios. Aparte, GestorCandadosT78 pasó 31/31 y las de voces de la mesera y los peatones, 25/25.
- **PlayMode:**
  - `VideosCinematicas23novPruebas`, 4/4 corrida sola. Corrida junto a NocheFijaT1685 falla el caso OMITIR porque un director de historia que deja esa clase encola su propia C03. Es un problema de aislamiento entre clases, no del juego.
  - `IndicadorDanioT1328` y `SaltarMolestiaVR16`: pasan.
  - `NocheFijaT1685.Paso12`: quedó ignorada porque ese paso de la historia está desconectado.
  - `CuerposBotinT746.Enemigo_AbatidoConLaPistola…` falla antes de llegar al botín: la pistola se queda sin balas con el enemigo a 35 de 60. Parece de antes de esta noche, pero no lo comprobé contra la base.

## Duración de las cinemáticas
Medida en Play con la prueba:

| Cinemática | Duración | Detalle |
|---|---|---|
| C02 EL MALETÍN | **62.4 s** | Tramos 5.3, 10.0, 8×4.0, 5.3 y 9.1 s (el último incluye el fundido). Cero cuadros sin imagen. |
| C03 LA DECISIÓN | 29.0 s | |
| C04 EL SUERO | 9.0 s | |
| Tino (C05) | 9.0 s | |

## Escalada: valores y minuto de llegada
Los valores están en ConfigRespuesta:

| Fase | Qué llega | Sube a los… | …o al matar | Llega (solo por tiempo) |
|---|---|---|---|---|
| 0 | Solo SKULLS | 45 s o al ganar estrellas | — | 0:00 |
| 1 | Policías a pie y patrullas | 5 min | 20 policías | 0:45 o al primer disparo |
| 2 | Más patrullas | 5 min | 30 policías o 6 patrullas | 5:45 |
| 3 | Patrullas, camionetas, drones de policía | 5 min | 40 policías | 10:45 |
| 4 | Drones de NOVA | 4 min | 10 drones | 15:45 |
| 5 | Soldados NOVA E15/E18 | 5 min | 20 soldados | ~18:30–19:45 |
| 6 | E16 + soldados + drones, sin fin | — | — | ~23–25 min |

- Los minutos son una estimación del agente, no una medición jugando.
- Un evento grande sube como mucho una fase.

## Topes de rendimiento (T-rendimiento-tope-24nov)
Se midió en el build de PC (`Builds/Juego/CyberCity.exe -medirRendimiento`):
- **Máquina:** RTX 5070 Laptop, Core Ultra 9 275HX, 2560×1600, calidad Ultra, sin vsync.
- **Escenario:** el jugador, invulnerable, se queda frente a su casa y los enemigos llegan con el goteo del modo libre.
- **Duración:** 17 s medidos por paso.

| Paso | Fase | Enemigos vivos | fps promedio | 1 % más bajo |
|---|---|---|---|---|
| 1 | 1 Policía | 0 | 96.7 | 48.9 (el calentamiento del arranque) |
| 2 | 2 Patrullas | 2 | 94.8 | 70.4 |
| 3 | 3 Camiones | 3 | 94.7 | 66.1 |
| 4 | 4 Drones NOVA | 4 (4 drones) | 95.8 | 69.4 |
| 5 | 5 Soldados NOVA | 15 | 92.0 | 66.6 |
| 8 | 6 NOVA pesada | 15 | 91.7 | 66.6 |
| 10 | 6 | 18 | 90.1 | 65.2 |
| 11 | 6 | 24 | 87.6 | 62.8 |
| 12 | 6 | 31 | 83.9 | 60.6 |

**Topes que quedaron en ConfigRespuesta (ParametrosRespuesta):**
- **24 enemigos a la vez en PC**, con margen: el 1 % más bajo queda arriba de 60.
- **12 en VR.** Es un valor conservador y **no está medido en el visor**: hay que confirmarlo con el Quest en 72 fps.
- Los demás se quedan: 3 patrullas, 4 drones, 2 cápsulas y 2 E16.

**Otros cambios de rendimiento:**
- Los cuerpos se hunden y desaparecen a los 20 s.
- Los casquillos ya tenían tope (32 por arma).

**Ojo con la medición:**
- Los enemigos llegaban y atacaban, pero hubo pocos disparos y explosiones, así que con combate fuerte el fps bajará algo.
- Faltó "apagar lo que no se ve".

**Bugs que encontró la medición, ya arreglados:**
- `CapsulaCaida.AvanzarDron` tronaba (NullReferenceException cada cuadro) cuando el dron se retiraba mientras la cápsula lo bajaba.
- El medidor dejaba avanzar la historia y las cinemáticas retiraban a todos los enemigos.

## Partida completa (T-partida-completa-24nov): parcial
No pude jugar una partida a mano de principio a fin, ni en PC ni en el simulador XR.

En su lugar, la corrida de medición en el build recorrió todo esto, con **0 excepciones** después del arreglo de la cápsula:
- arranque, intro y menú;
- NUEVA PARTIDA, C01 y la historia saltada al modo libre;
- las fases 1 a 6;
- tropas de NOVA cayendo en cápsula.

Las únicas advertencias son de antes: `ARM_PIS_RECARGA` y `AMB_CALLE_NOCHE` sin audio, la Tiendita en Inicio y referencias de JaulaClandestino.

**Quedó sin recorrer automáticamente:** la parte guiada jugada (3 SKULLS, Doc, Tino, campo de tiro), morir, HAS MUERTO y REINTENTAR. Esa parte la tiene que probar Tony jugando.

## Qué no quedó
- **Partida completa a mano**, en PC y en el simulador XR, con capturas: ver arriba.
- **VR:** el tope de 12 enemigos no está medido en el visor.
- **Botiquín:** no hay un modelo propio de botiquín (se usa la bebida O01).
- **Voz del Doc en C04 EL SUERO:** no comprobé si los videos 22–24 traen sus frases.
- **OMITIR con el láser en VR:** no lo comprobé (B y el gatillo sí lo saltan).
- **Avisos viejos:** quedan "NOVA INTERVIENE" en la fase 4 y el mensaje de teléfono del Inventor ("Ya eres libre…"). Tony decide si se quitan.
- **Subtítulos de abajo:** los de los enemigos (PanelSubtitulos) se quedan. Solo se quitó el texto sobre la cabeza.
- **Pruebas:** `CuerposBotinT746.Enemigo_AbatidoConLaPistola…` falla. Probablemente ya fallaba antes, pero no lo comprobé contra la base.

## Qué probar jugando
1. **Cinemáticas con narración:** partida nueva, C02 EL MALETÍN completa y luego C03. Cada frase se oye entera, con el cuadro congelado donde hace falta y sin negros.
2. **OMITIR:** pruébalo en cada cinemática, en PC y en VR.
3. **Música por fase:** piano → campo de tiro → SKULLS → policía → NOVA → morir (piano).
4. **Volúmenes:** pon MÚSICA en 0 y sube VOCES en Opciones. Cierra y abre el juego: deben conservarse.
5. **Modo libre:**
   - Con Tino: compra el arma y la mejora, aléjate y no debe salir ningún aviso. Deben aparecer SOBREVIVE y el contador.
   - Las patrullas abren las puertas al llegar.
   - Muere y revisa HAS MUERTO y REINTENTAR.
6. **Drops:** mata unos 10 policías. Deben salir cargadores (luz naranja) y algún botiquín (luz verde).
7. **Indicador:** que te disparen desde los lados, desde atrás y desde arriba.
8. **Campo de tiro:** los objetivos tienen el modelo real y nadie cruza la sala.
9. **Cuerpos:** cada caída suena una vez.
10. **Textos:** ya no hay texto sobre los enemigos.
