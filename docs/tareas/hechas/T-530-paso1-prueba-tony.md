# T-530 a T-537 Paso 1 del plan de la noche 3-nov: arreglos de la prueba de Tony (docs/prueba-02nov.md, todos los [C])
Dueño: Tony | Ramas: tony/T-530-escala-barrido … tony/T-537-hud-titulo (una por tarea, merge a tony/fase-g) | Modelo: Opus 5.5 | docs/plan-noche-03nov.md, paso 1

## T-530 Escala, colisiones y barrido (ColocarT530, InspeccionT530)
- Escala: todos los personajes miden 1.8 m (medido en Play); lo grande era el mundo. Puertas de 3 x 3.2 m pasan a 1.6 x 2.4 (T-536); la cámara del hombro va a 2.6 m (antes 3.2), 1.6 de alto, 0.5 al lado. Robots de Nova (guardias) a 2.2 m.
- Colisiones: la cámara ya no entra a la pared cuando el jugador está pegado a ella (el hombro quedaba dentro del muro y el rayo salía de adentro: CamaraHombro recorta el hombro). Auditoría de mallas sólidas sin collider (Logs/t530-atravesables.txt): los edificios sí tienen; lo que salía eran brazos de farolas y pilares de la autopista elevada.
- La ciudad sigue: las 6 salidas tienen 100 m de calle con banquetas, rayas, cuadras de relleno con ventanas y una calle cruzada al fondo con un muro invisible; sin retenes, sin "ALTO · CONTROL CORPORATIVO", sin muros en las salidas; las torres de fondo de Neón Alto que caían en esas calles se recorrieron.
- Barrido: el arco del Tianguis salió del cruce (su pilar quedaba en medio de la calle: era el "poste cutre"); 7 puestos de T-512 que flotaban encima de otros bajaron al piso; 23 puestos y cajas encimados fuera (se queda el de más prioridad); hologramas y 15 proyectores de relleno fuera; las dos barreras del cruce del Tianguis fuera; cajas, tambos, huacales y mesas que flotaban, al piso; letreros planos de lugares (EL MERCADO 24 HORAS ABIERTO, etc.) apagados; los dos puentes de Neón Alto ya no atraviesan edificios (miden lo que la calle).
## T-531 Habitación, Tino y el Doc (ColocarT531)
- Habitación sin la estación del mecha ni el resto del taller (queda un banco); LA GÜERA sale a la banqueta; sin el letrero amarillo vacío; el tablero de ENCARGOS se lee desde la calle.
- Locales nuevos tallados en planta baja (8 x 6 m, frente abierto): CIC de Tino junto al MECÁNICO en la calle principal de Suburbios (x -73, z 13) con su tienda; I+D+I del Doc en el lado sur, lejos de El Mercado (x -100, z -13), con la clínica (camilla y mostrador de implantes). La clínica sigue colgando del prefab de El Mercado en la jerarquía (no se puede cambiar de padre); solo cambió de lugar.
## T-532 Tráfico, manejo, motos y patrullas (ColocarT532, CarroTrafico, TraficoCalle)
- El tráfico frena ante cualquier cosa sólida en su carril (carros estacionados, robados, patrullas, gente, puestos), cede los cruces y entra y sale por las afueras: desaparece al final de esas calles y vuelve a salir en otro final lejos y fuera de la vista.
- Freno firme (11 m/s²) y menos derrape; al soltar el acelerador se detiene más rápido (rodadura 900 N; con 1700 el piloto de la prueba ya no daba la vuelta en el cruce).
- "¡Bájate, se va a quemar!" cuando el carro en el que vas se prende (EventosMontaje.Aviso en Core, lo muestra el HUD).
- Cada carro dice su modelo en el HUD (Sedán viejo, Pickup de trabajo, Combi vieja, Patrulla, Moto…).
- Motos de la calle manejables y robables (Carro_C19, 6 en la ciudad). Tráfico civil sin V05/V06/V08 (son de la policía). La patrulla de la persecución usa el modelo V05 en lugar de los cubos.
## T-533 Calor, policía y guardias (ColocarT533, PatrullaMundo, GuardiaNova)
- Una patrulla viene desde el nivel 1 de calor (2 y 3 después), con sirena en bucle, megáfono, sale a unos 80 m y de cerca deja la calle y va directo hacia ti; al subir el calor la radio dice "RADIO · Unidades, sospechoso en <zona>". Robar una patrulla sube el calor al nivel 2.
- Seguridad de Nova: patrulla su tramo de banqueta, te vigila desde 9 m y si sacas un arma cerca o hay disparos se detiene, mira hacia allá, grita por el megáfono y llama a la policía (sube el calor una vez por alerta).
- Los SKULLS usan SkullHombre: los 5 armados de Calaveras y los 2 de puños de la calle de tu casa.
## T-534 NPC
- La gente parada te voltea a ver extrañada (ladea la cabeza) si te quedas cerca y quieto un segundo; al irte vuelve a lo suyo.
- Los peatones ya no atraviesan muros ni puestos: se dan la vuelta; y miran hacia donde caminan (a más de 60 fps no giraban).
## T-535 Luces, lluvia y asfalto (ColocarT535)
- Asfalto mojado con grano, grietas, parches, aceite y zonas lisas que brillan, con normal map (Assets/Arte/Exteriores/Fuentes~/generar_asfalto.py); también en las afueras.
- La lluvia cae en todo el alto y llega al piso, donde muere con salpicadura; no llueve bajo techo.
- Las luces de noche se apagan poco a poco una hora después de amanecer y prenden media hora antes de anochecer.
- La luz de cada farola sale del foco (punta del brazo, buscada en la malla). Cada puesto del Tianguis tiene su luz chica del color de lo que vende (32).
## T-536 Puertas (ColocarT536, PuertaLocal)
- Puertas a escala humana con jambas y dintel; se abren solas al acercarte desde la calle (adentro, con E); luz cálida en el pasillo de entrada (era la "pared negra"); la entrada E01 del Casino tenía la puerta cerrada dentro de la malla y tapaba el interior: fuera, con marco dorado de 2.6 x 2.8.
## T-537 HUD, CONTINUAR y título
- Hora del juego bajo los Biits; panel de armas como el mockup (arma naranja, casillas turquesa, fondo azul oscuro).
- CONTINUAR: no se pudo reproducir en el editor (la carga coloca bien al jugador y el HUD sigue). Se agregó CARGANDO… que tapa la pantalla durante la recarga de la escena (en el build tardaba y quedaba vacía). Revisar en el build (D-tony-349).
- Título: sin la imagen fija, detrás del menú se ve la ciudad en cámara lenta (la cámara recorre la calle principal y Neón Alto a 3.4 m).
- EL HACKER sentado: falta la animación de Mixamo (docs/animaciones-faltan.md); sigue de pie a escala.

## Pruebas
EditMode nuevas: EscalaBarridoT530 (8), LocalesT531 (4), CarrosT532 (4), LucesLluviaAsfaltoT535 (3), PuertasT536 (2), FormatoHud +1; actualizadas SinGreyboxT506, TianguisT503, PersonajesT510 (guardias patrullan, SKULLS con SkullHombre). PlayMode nuevas: CamaraPared (2), TraficoCalle +2, DanioCarro +1 (aviso de fuego), GenteMira (2), PeatonHuye +1, Puertas +1 (se abre sola); actualizadas Persecucion (patrullas por nivel), Puertas. Verdes: Personaje controlador, Vehiculos, Trafico, UI (149), puertas, lluvia, ciclo día-noche. RecorridoDemoPruebas 2/2.
Rojas que ya estaban: MercadoNegro a las 22 / Lugares (mercado 24 h), RecorridoAPie (la entrada del Bar), MontableCarro.Camara_PrimeraPersona; varias de Mundo y Personaje fallan solo si corren después de pruebas que cargan Ciudad_Prologo (contaminación; solas pasan).

## FPS (build de medición, 1920x1080, Medio, hora 23, Logs/med-paso1/medicion.txt)
| Punto | FPS | 1% bajo |
| --- | --- | --- |
| calle | 158.5 | 111.9 |
| tianguis | 165.6 | 118.8 |
| carros | 148.0 | 105.7 |
| neon_calle | 219.1 | 152.5 |
| mercado_puerta | 249.1 | 173.5 |
| vivacoin | 275.6 | 195.1 |
| suburbios | 283.8 | 193.1 |
| callejon | 295.4 | 203.9 |
| casa | 356.1 | 258.2 |

Capturas: docs/capturas/prueba2-*.png (antes | después).
