# Bitácora de tareas Blender — CyberCity ronda 2 (16-nov)

## 01 · Caminar más natural — HECHO
**Qué hice**
- Base: el caminar actual (Loco_Caminar_Ajustado, que es el mismo que el PJ01_Caminar_* exportado antes). Le agregué encima, cuadro a cuadro y en bucle:
  - Cadera: se mece 2.5 cm hacia la pierna de apoyo, se inclina ±3.5° y gira ±7° con cada paso; sube 1.5 cm (rodillas menos dobladas, menos "agachado").
  - Hombros: giran al revés que la cadera (±9°) y el pecho va 3° más erguido.
  - Brazos más relajados: balanceo 50 % menor, codos un poco doblados, brazos más pegados y la mano que va adelante ya no cruza al centro ni se estira hacia el frente.
  - Cabeza estable (cancela 75 % del vaivén del torso).
  - Pies: fijados con IK a la misma posición exacta del caminar anterior (error 0.4 cm), así que el largo de paso y la velocidad son idénticos y no patinan.
- ConImplante: mismo caminar con hombros +10° y brazos 14° más abiertos. Choques con IMP01_LVL2 / IMP02: 0 vértices en todo el ciclo (el SinImplante sí chocaba 67 con IMP01_LVL2).

**Exportados** (`imageassets\blender\pj01_clips\`, reemplazan): PJ01_Caminar_SinImplante.fbx, PJ01_Caminar_ConImplante.fbx

**Datos para Unity**
- Bucle de 31 cuadros (1.03 s) + cuadro 32 = cuadro 1. Loop Time ON.
- Trae avance de raíz: 1.673 m por ciclo → **velocidad 1.62 m/s** (con Scale Factor 1.8367, personaje de 1.8 m). Si se usa sin Root Motion, mover el CharacterController a 1.62 m/s para que los pies no patinen.
- 2 pasos por ciclo (0.84 m por paso).

**Revisión**: `_REVISION\01_Caminar.blend` (izquierda sin implante, derecha con IMP01_LVL2 + IMP02_LVL1; 4 ciclos, la cámara los sigue). Vistas: 01_vista_lado.png, 01_vista_paso.png, 01_vista_frente.png.
**Mejorable**: es el mismo caminar de Mixamo retocado, no uno nuevo; si Tony quiere un estilo muy distinto (más "swag"), lo ideal es bajar de Mixamo un "Walking" con más actitud y aplicarle este mismo arreglo.

## 02 · Idle (parado) — HECHO
**Qué hice**
- Nuevo bucle de **150 cuadros (5.0 s)** + cuadro 151 = cuadro 1, sacado del idle de Mixamo con un cruce de 1 s al final para que cierre sin salto.
- Encima le puse:
  - Peso en la pierna derecha: cadera 4 cm hacia ese lado, cadera inclinada 3° (la izquierda baja y esa rodilla se afloja), torso compensado; el peso se mueve un poco y regresa.
  - Postura menos agachada: cadera 2 cm más alta (piernas casi rectas), pecho 4° más erguido, pies un poco más juntos (37 cm) y puntas 8° hacia afuera.
  - Respiración: 2 respiraciones por bucle (pecho y hombros suben y bajan).
  - Mirada: voltea ~9° a un lado, luego ~5° al otro y regresa al frente, con un poco de arriba/abajo.
  - Brazos relajados colgando al costado (antes las manos quedaban flotando delante de los muslos).
- ConImplante: igual con hombros +10° y brazos 12° más abiertos. Choques con implantes: 0 (el normal chocaría miles de vértices con el exo, por eso existen los dos).

**Exportados** (reemplazan): `pj01_clips\PJ01_Loco_Idle.fbx`, `pj01_clips\PJ01_Loco_Idle_ConImplante.fbx`

**Datos para Unity**: 151 cuadros a 30 fps (5.0 s), Loop Time ON, en el lugar (sin avance). Pies a ras del piso todo el bucle.
**Revisión**: `_REVISION\02_Idle.blend` (izquierda sin implante, derecha con implantes; 2 vueltas del bucle). Vistas: 02_vista_frente.png, 02_vista_lado.png.
**Mejorable**: los cambios de postura son suaves; si se quiere un idle "con actitud" (tronarse el cuello, mirar el reloj), pedir 1-2 idles extra de Mixamo y alternarlos en Unity.

## 03 · Escaleras de mano — HECHO (animado a mano: no hay clips de escalera de Mixamo en imageassets\animaciones)
**Qué hice**
- Escalera de referencia: peldaños cada 0.30 m, 0.45 m de ancho, el plano de los peldaños **0.33 m delante de la raíz** del personaje (raíz = piso al pie de la escalera). Pared a 0.45 m.
- Todo con IK de brazos y piernas sobre los peldaños: manos agarran el peldaño (dedos cerrados como en el volante), puntas de los pies apoyadas en el peldaño, mano derecha + pie izquierdo y luego mano izquierda + pie derecho, cuerpo separado de la escalera, cabeza mirando un poco arriba.
- PJ01_Escalera_Entrar: desde el idle se agarra (mano der. al peldaño de 1.5 m, izq. al de 1.8 m), sube pie izq. al peldaño 1 y pie der. al 2. Termina exactamente en la pose del cuadro 1 de Subir.
- PJ01_Escalera_Subir (bucle): sube 2 peldaños por ciclo. **En el lugar** (la cadera no sube; los apoyos "bajan").
- PJ01_Escalera_Bajar (bucle): Subir al revés.
- PJ01_Escalera_SalirArriba: del cuadro 1 de Subir pasa manos al borde del techo, sube los pies 2 peldaños, mete el cuerpo entre los largueros y pisa el techo; termina en la pose del Idle de pie sobre el techo.
- PJ01_Escalera_SalirAbajo: Entrar al revés (baja los pies al piso y se suelta; termina en Idle).

**Exportados** (`pj01_clips\`): PJ01_Escalera_Entrar.fbx, PJ01_Escalera_Subir.fbx, PJ01_Escalera_Bajar.fbx, PJ01_Escalera_SalirArriba.fbx, PJ01_Escalera_SalirAbajo.fbx

**Datos para Unity**
| Clip | Cuadros | Duración | Movimiento |
| --- | --- | --- | --- |
| Escalera_Entrar | 45 | 1.50 s | raíz quieta (la cadera sube 0.17 m dentro del clip) |
| Escalera_Subir | 41 (bucle de 40) | 1.33 s | **sube 0.60 m por ciclo = 0.45 m/s** (mover el CharacterController hacia arriba a esa velocidad) |
| Escalera_Bajar | 41 (bucle de 40) | 1.33 s | baja 0.60 m por ciclo = 0.45 m/s |
| Escalera_SalirArriba | 78 | 2.60 s | al terminar el personaje está **2.10 m más arriba y 1.00 m más adelante** que la raíz al empezar (sin Root Motion: mover la raíz al final, o activar Root Motion en este clip) |
| Escalera_SalirAbajo | 45 | 1.50 s | raíz quieta, termina en Idle |
- Para que manos y pies caigan en los peldaños, empezar Subir/Bajar con la raíz a una altura múltiplo de 0.30 m sobre el primer peldaño (0.30 m del piso) y con la escalera a 0.33 m al frente.
- SalirArriba asume el techo 2.10 m sobre la raíz al empezar (= 7 peldaños) y la orilla del techo 0.45 m al frente; los largueros deben seguir arriba del techo o el cuerpo los cruza.

**Revisión**: `_REVISION\03_Escaleras.blend` (escalera derecha: Entrar → Subir ×3 → SalirArriba; escalera izquierda: Bajar ×3 → SalirAbajo). Vistas: 03_vista_subir.png, 03_vista_techo.png, 03_vista_entrar.png.
**Mejorable**: está animado a mano (IK), se ve funcional pero algo rígido; con un "Climbing Ladder" de Mixamo quedaría más natural y se le aplicaría la misma medida de peldaños.

## 04 · Parkour — HECHO (animado a mano con IK + clips existentes; no hay clips de parkour de Mixamo en imageassets\animaciones)
**Qué hice**
- PJ01_Parkour_SaltarBarda: sale corriendo (cuadros del Correr), apoya la mano izquierda arriba de la barda y pasa las piernas encogidas por el lado derecho (salto "de velocidad"), cae y sigue corriendo (termina en el cuadro 22 del Correr = inicio del bucle). Revisado cuadro a cuadro: ninguna parte del cuerpo atraviesa la barda (solo las yemas de la mano izquierda rozan la orilla al agarrarla).
- PJ01_Parkour_TreparBorde: desde el Idle se agacha, salta, se cuelga con las dos manos del borde, apoya los pies en la pared, se impulsa, sube un pie y luego el otro, y termina de pie en el Idle encima.
- PJ01_Parkour_RodarCaida: toca el piso (amortigua del SaltoAterrizaje), rueda (Loco_Rodar) y sale corriendo (Loco_Correr); corrección de piso cuadro a cuadro.

**Exportados** (`pj01_clips\`): PJ01_Parkour_SaltarBarda.fbx, PJ01_Parkour_TreparBorde.fbx, PJ01_Parkour_RodarCaida.fbx

**Datos para Unity** (todos traen el avance en la cadera; usar Root Motion o mover la raíz igual)
| Clip | Cuadros | Duración | Altura | Avance |
| --- | --- | --- | --- | --- |
| Parkour_SaltarBarda | 33 | 1.10 s | barda de **1.0 m**, cara frontal a **1.68 m** delante de la raíz al empezar, 0.2 m de grueso | **4.46 m** (a velocidad de correr, ~4.3 m/s) |
| Parkour_TreparBorde | 70 | 2.33 s | borde a **2.0 m**, pared a **0.45 m** delante de la raíz | termina **2.0 m arriba y 0.95 m adelante** |
| Parkour_RodarCaida | 39 | 1.30 s | empieza al tocar el piso (la caída previa es Loco_Caida; vale para cualquier altura) | **5.5 m** (rueda ~1.9 m y sale corriendo) |
- SaltarBarda y RodarCaida empiezan/terminan en poses del Correr: transición corta (0.1 s) desde/hacia Loco_Correr.
- TreparBorde empieza y termina en Idle.

**Revisión**: `_REVISION\04_Parkour.blend` (izquierda: barda; centro: trepar; derecha: cae desde 3 m y rueda). Vistas: 04_vista_barda.png, 04_vista_trepar.png, 04_vista_rodar.png.
**Mejorable**: SaltarBarda y TreparBorde están animados a mano (IK); funcionan sin atravesar nada pero se ven más rígidos que un clip real. Con "Jump Over Obstacle"/"Climbing Up Wall" de Mixamo quedarían más naturales.

## 05 · Bajar de la moto — HECHO
**Qué hice**
- Partí del Moto_Bajar anterior (guardado como Moto_Bajar_ANTERIOR en el maestro) y le rehíce el final:
  - El cuadro 1 es idéntico al de Moto_Manejar (diferencia 0.0°), así que no hay salto al dejar de manejar.
  - La pierna derecha pasa por encima del asiento y baja por fuera de la moto: la llevé 13 cm más hacia afuera y con la punta derecha (antes la punta del pie se metía en el costado de la moto). Revisado cuadro por cuadro contra la malla de la C19: nada atraviesa (solo las yemas de la mano derecha rozan el asiento al apoyarse).
  - De los cuadros 40 a 64 se acomoda en el Idle nuevo (pies a su lugar con IK, sin patinar ni hundirse); el último cuadro es exactamente el cuadro 1 de PJ01_Loco_Idle.
- Clip: 64 cuadros (2.13 s; antes 54).

**Exportado** (reemplaza): `pj01_clips\PJ01_Moto_Bajar.fbx`

**Datos para Unity**
- Transición Manejar → Bajar sin mezcla (0 s) o muy corta; Bajar → Loco_Idle sin mezcla.
- Avance: termina de pie **0.70 m a la izquierda** del centro de la moto y 0.08 m más adelante (la cadera se desplaza; con Root Motion se mueve sola la raíz, sin Root Motion mover la raíz 0.70 m a la izquierda al terminar).

**Revisión**: `_REVISION\05_Moto_Bajar.blend` (Subir → Manejar → Bajar → Idle seguidos, con la moto C19). Vistas: 05_vista_pierna.png, 05_vista_atras.png, 05_vista_final.png.
**Mejorable**: el movimiento de pasar la pierna sigue siendo el del clip original; si se quiere más ágil (bajarse de un brinco), se acorta.

## 06 · Cobertura con arma larga — HECHO (armado a mano con IK sobre Rifle_Idle / Rifle_Apuntar)
**Qué hice**
- El fusil W03 (versión nueva con la mira completa) en la mano derecha con el agarre de la ronda anterior; la mano izquierda va con IK al guardamanos en todos los cuadros.
- PJ01_CoberturaRifle_Entrar: de Rifle_Idle de pie se arrodilla (rodilla derecha al piso) detrás de la cobertura en 20 cuadros.
- PJ01_CoberturaRifle_Idle: bucle de 60 cuadros, rodilla al piso, torso inclinado 20°, cabeza agachada, fusil cruzado apuntando abajo/al costado (no toca la cobertura). Respira y voltea un poco.
- PJ01_CoberturaRifle_Disparo: se asoma **por el lado derecho** de la cobertura (se levanta a medio agachar y da un paso a la derecha 0.58 m), apunta, 3 disparos con retroceso (cuadros 16, 20 y 24) y regresa. Empieza y termina igual que el Idle.
- Revisado cuadro por cuadro contra una cobertura de prueba: ni el cuerpo ni el fusil la atraviesan; nada se hunde en el piso.
- Asomarse por arriba no se pudo: con el personaje arrodillado la cabeza llega a 1.28 m y el fusil al apuntar de pie queda a 1.04 m del piso, así que una cobertura que lo tape completo no deja disparar por encima. Por eso es por el lado.

**Exportados** (`pj01_clips\`): PJ01_CoberturaRifle_Entrar.fbx, PJ01_CoberturaRifle_Idle.fbx, PJ01_CoberturaRifle_Disparo.fbx

**Datos para Unity**
| Clip | Cuadros | Duración | Notas |
| --- | --- | --- | --- |
| CoberturaRifle_Entrar | 20 | 0.67 s | de Rifle_Idle a Idle de cobertura, en el lugar |
| CoberturaRifle_Idle | 61 (bucle 60) | 2.0 s | Loop Time ON |
| CoberturaRifle_Disparo | 40 | 1.33 s | disparos en los cuadros 16, 20, 24 (eventos para el sonido/fogonazo); la cadera sale 0.58 m a la derecha y regresa (no mover la raíz) |
- Cobertura de referencia: **1.3 m de alto** (o más), cara frontal **0.5 m delante** de la raíz, y la orilla derecha de la cobertura **0.25 m a la derecha** de la raíz (el personaje se pega a la orilla derecha para asomarse).
- Para salir de la cobertura: CoberturaRifle_Entrar al revés (velocidad -1) o transición de 0.3 s a Rifle_Idle.

**Revisión**: `_REVISION\06_Cobertura_Rifle.blend` (Entrar → Idle → Disparo → Idle → Disparo → Idle, con el W03 y una cobertura de 1.3 m). Vistas: 06_vista_cubierto.png, 06_vista_disparo.png, 06_vista_frente.png.
**Mejorable**: es una pose armada a mano; para asomarse a la izquierda haría falta el espejo (cambiar de mano el fusil), no lo hice.

## 07 · Robar un carro — HECHO (combinado de clips existentes + IK)
**Qué hice**
- PJ01_Conductor_SerSacado (80 cuadros): sentado manejando (pose nueva del carro), voltea hacia la puerta y se inclina hacia ella (cuadros 1–9), lo sacan del asiento (el Carro_Salir acelerado ×3, cuadros 10–40) y lo avientan: cae al piso hacia adelante (Muerte_Adelante, cuadros 41–80) y queda tirado.
- PJ01_Carro_SacarConductor (80 cuadros): parte de la pose inicial de Carro_Entrar (de pie junto al hueco de la puerta), se mete por el hueco y lo agarra (mano izquierda al hombro, derecha al cuello de la camisa, con IK que sigue al conductor cuadro a cuadro, error 1–4 cm), lo jala hacia afuera dando pasos hacia atrás sin meter los pies al carro, lo avienta (cuadro 46), suelta y regresa a la pose del cuadro 1 de Carro_Entrar → queda listo para entrar.
- Los carros van sin puerta: la animación usa el hueco (no hay manija).

**Exportados** (`pj01_clips\`): PJ01_Carro_SacarConductor.fbx, PJ01_Conductor_SerSacado.fbx

**Datos para Unity (sincronizar)**
- Los dos clips duran **80 cuadros (2.67 s)** y empiezan en el **mismo cuadro**. Las dos raíces van en el mismo punto: el mismo origen que se usa para Carro_Entrar/Carro_Manejar (asiento del conductor).
- Tiempos: agarra en el cuadro 10 (0.33 s); el conductor sale del asiento 10–40 (0.33–1.33 s); lo avienta en el 46 (1.53 s); el conductor toca el piso ~60 (2.0 s); el jugador queda listo en el 80 → seguir con Carro_Entrar.
- El conductor termina tirado a **1.46 m** hacia afuera (lado de la puerta) y **1.19 m** hacia el frente del carro respecto a la raíz; con Root Motion la raíz lo sigue sola. Para que se levante después: Dano_Levantarse_BocaAbajo.
- Usar Carro_Entrar justo al terminar (su cuadro 1 = último cuadro de SacarConductor).

**Revisión**: `_REVISION\07_Robar_Carro.blend` (V01A sin puertas; conductor y jugador juntos; el jugador sigue con Carro_Entrar). Vistas: 07_vista_agarra.png, 07_vista_jala.png, 07_vista_avienta.png.
**Mejorable**: al principio la mano derecha queda a ~15 cm del cuello del conductor (no alcanza bien por el hueco); el jalón se ve funcional pero rígido. Con un "Car Jack" de Mixamo (si se consigue) se vería más real.

## 08 · Prótesis de ataque en PJ01 (IMP04 brazo láser, IMP05 hombrera de misiles) — HECHO
**Qué hice**
- Descomprimí los zips de Tripo en `implantes\_fuentes_ronda2\`.
- **IMP04_BrazoLaser**: el modelo de Tripo traía antebrazo + mano robótica completa. Le corté la mano (corte recto en la muñeca, tapa cerrada con Corte_Interior) y dejé solo el brazalete del antebrazo con el emisor arriba, junto a la muñeca. Escalado al antebrazo de PJ01 (largo ×0.30, ancho ×0.42, alto ×0.36) y rígido al hueso **R_Forearm** (peso 1.0). Así no choca con la mano de PJ01 ni con las armas.
- **IMP05_HombreraMisiles**: el arnés con las dos hombreras (×0.70) sobre los hombros, rígido a **Spine02** (peso 1.0; no se mueve con los brazos). Corté el lanzador del arnés (corte recto en el soporte, tapas cerradas) y lo moví **más abajo, más atrás y hacia el hombro izquierdo**: queda apoyado sobre la hombrera izquierda, 8 cm más abajo que en el original y lejos de la cabeza. Con la cámara al hombro derecho no tapa la vista (ver 08_vista_camara_hombro.png).
- Puntos de salida como Empties dentro del FBX: `IMP04_Salida` (hijo de R_Forearm, en la cara frontal del emisor) e `IMP05_Salida` (hijo de Spine02, en la cara frontal del lanzador).

**Exportado**: `imageassets\blender\implantes\PJ01_Implantes_Ataque.fbx` (mismo armature PJ01 de 78 huesos + mallas IMP04_BrazoLaser e IMP05_HombreraMisiles + los 2 Empties; texturas en `PJ01_Implantes_Ataque.fbm\`). No toqué PJ01_Implantes.fbx. Verificado reimportándolo: las mallas quedan en su lugar sobre el esqueleto.

**Datos para Unity** (Scale Factor 1.8367 como los demás; posiciones en metros de juego, personaje de 1.8 m)
- Rayo: sale de `IMP04_Salida`; respecto al hueso R_Forearm: **0.165 m a lo largo del hueso** (hacia la mano), 0.093 m hacia el dorso, -0.02 m lateral. Dirección: a lo largo del antebrazo hacia la mano.
- Misiles: salen de `IMP05_Salida`; respecto a Spine02: 0.255 m a lo largo del hueso (hacia arriba), 0.072 m y -0.155 m en los otros ejes (sobre el hombro izquierdo, al frente del lanzador). Dirección: hacia el frente y un poco hacia arriba.
- Lo más fácil en Unity: usar directamente los transforms de los Empties `IMP04_Salida` / `IMP05_Salida` (ya vienen colgados de su hueso).

**Revisión**: `_REVISION\08_Protesis_Ataque.blend` (Idle → golpe → apuntar → Idle con las dos prótesis). Vistas: 08_vista_frente.png, 08_vista_atras.png, 08_vista_camara_hombro.png.
**Mejorable**: el brazalete es la parte de antebrazo del modelo (es abierto, se ve el brazo entre los aros); el lanzador va al hombro izquierdo — si lo quieren en el derecho habría que bajarlo detrás de la espalda y taparía menos, pero quedaría detrás de la hombrera.

## 09 · Ruedas de los otros carros (V02, V03, V04, V06, V09, V10) — HECHO
**Qué hice** (archivo de trabajo: `imageassets\blender\carros\carros_trabajo.blend`; fuentes descomprimidas en `carros\_fuentes\`)
- Enderecé los carros que Tripo entregó girados (V02 62.5°, V03 51.2°, V04 y V10 90°) para que todos queden con el **frente hacia -Y de Blender = +Z de Unity**, centrados y apoyados en el piso (z = 0).
- **V02, V03, V04, V06**: las ruedas eran piezas sueltas dentro de la malla → las separé tal cual (sin cortar ni deformar), con sus tapones incluidos. Cada rueda con su pivote al centro.
- **V09 y V10**: las ruedas venían soldadas a la carrocería → borré las ruedas (las caras dentro del cilindro de cada rueda) y puse la **RU01** escalada al hueco (radio 0.137, ancho 0.095), con pivote al centro.
- Nombres: Carroceria, Rueda_DI, Rueda_DD, Rueda_TI, Rueda_TD (D = delantera, T = trasera; I = izquierda del conductor = +X de Blender), colgados de un vacío raíz con el código del carro. Sin puertas (como se decidió).

**Exportados**: `imageassets\blender\carros\V02.fbx`, `V03.fbx`, `V04.fbx`, `V06.fbx`, `V09.fbx`, `V10.fbx` (texturas junto al FBX en las carpetas .fbm).

**Datos para Unity**
- Escala: la de Tripo (≈1 m de largo); escalar el carro completo como al V01A (×4.3 si se quieren del mismo tamaño).
- Para que giren: rotar cada Rueda_* sobre su eje X local (el pivote ya está al centro). Radios (escala Tripo): V02 0.087, V03 0.085, V04 0.089, V06 0.092, V09/V10 0.137.

**Revisión**: `_REVISION\09_Ruedas_Carros.blend` (los 6 carros con las ruedas girando). Vistas: 09_vista_frente.png, 09_vista_lado.png.
**Mejorable**: en V09 y V10 la RU01 es una llanta todo terreno (taqueada) y se ve más grande/gruesa que las demás; si se quiere el look original, pedir a Tripo una versión de cada carro con las ruedas separadas.

## 10 · Moto C19 con llantas — HECHO
**Qué hice**
- C19 (cuerpo) con el frente hacia -Y (Unity +Z). Puse dos RU02 giradas 90° y escaladas a sus ejes: **Rueda_D** en la horquilla (radio 0.20) y **Rueda_T** en el basculante (radio 0.151), las dos tocando el piso y centradas entre las barras de la horquilla. Pivote al centro de cada llanta.
- Objetos: Cuerpo, Rueda_D, Rueda_T (hijos del vacío C19).

**Exportado**: `imageassets\blender\carros\C19.fbx`
**Datos para Unity**: escala de Tripo (≈1 m). La Moto_C19 que se usó para los clips de PJ01 estaba a ×1.8: escalar igual para que los clips de moto coincidan. Girar las llantas sobre su X local.
**Revisión**: `_REVISION\10_Moto_C19.blend` (llantas girando). Vistas: 10_vista_moto.png, 10_vista_lado.png.
**Mejorable**: la llanta delantera queda un poco más grande que la trasera porque así lo pide la altura de los ejes del cuerpo.

## 11 · Piezas de las armas nuevas (W05–W08) — HECHO
**Qué hice** (archivo de trabajo: `imageassets\blender\armas\armas_ronda2.blend`; fuentes en `armas\_fuentes_ronda2\`)
- Igual que W02–W04: cortes rectos (planos), piezas separadas, cortes cerrados con tapas planas y material `Corte_Interior` (oscuro). Ninguna pieza deformada.
- Orientación como las demás: cañón hacia **-Y de Blender = +Z de Unity**, Y arriba, metros, pivote del arma en la empuñadura (donde va la mano). Piezas como hijos del vacío raíz (W05…W08) con transform en cero.
- **W05 subfusil** (0.60 m): Armazon, Canon (guardamanos con la empuñadura delantera y la boca), Cargador, Mira (punto rojo).
- **W06 revólver** (0.30 m): Armazon, Canon, **Tambor** con su pivote en el eje → se puede girar sobre su Y local (eje paralelo al cañón).
- **W07 rifle de plasma** (0.90 m): Armazon, Canon (cañón con las bobinas y la barra inferior), **Celda** (la celda de energía cian bajo el cañón), Mira.
- **W08 lanzagranadas** (0.75 m): Armazon (con el asa de arriba), Canon (con la empuñadura delantera), **Tambor** con pivote en su eje (gira sobre su Y local).

**Exportados**: `imageassets\blender\armas\W05.fbx`, `W06.fbx`, `W07.fbx`, `W08.fbx` (texturas embebidas).

**Datos para Unity**: igual que W02–W04 (hijo de R_Hand y ajustar a ojo partiendo de los valores del W03). Tambores: rotar sobre su eje Y local (en Unity, el eje que apunta al frente del arma).
**No perfecto**:
- W07 no trae una mira de verdad en el modelo: la pieza "Mira" es solo el alza trasera (4 × 1.4 cm). Si se quiere mira, usar Mira_PuntoRojo o pedir una a Tripo.
- En las piezas aparece un 2.º material (Corte_Interior) en las caras del corte.
**Revisión**: `_REVISION\11_Piezas_Armas_Nuevas.blend` (cuadros 1–70: armadas → piezas separadas → armadas; 70–100: giran los tambores). Vistas: 11_vista_armadas.png, 11_vista_piezas.png, 11_vista_lado.png.

---
# RESUMEN FINAL · Ronda 2 (16-nov)

**Respaldo del maestro**: `pistola\W01_Pistola_agarre_respaldo_2026-11-16.blend` (las acciones viejas reemplazadas quedaron como *_ANTERIOR dentro del maestro).

## ✅ Hecho
- **01 Caminar**: PJ01_Caminar_SinImplante / _ConImplante (32 cuadros, 1.62 m/s).
- **02 Idle**: PJ01_Loco_Idle / _ConImplante (5 s, en bucle).
- **03 Escaleras**: Entrar, Subir, Bajar, SalirArriba, SalirAbajo (0.45 m/s, en el sitio).
- **04 Parkour**: SaltarBarda, TreparBorde, RodarCaida.
- **05 Moto bajar**: PJ01_Moto_Bajar (empieza en Manejar, termina en Idle). Resuelve lo de los finales de la moto que quedó pendiente de la ronda 1.
- **06 Cobertura con rifle**: Entrar, Idle, Disparo (se asoma por la derecha).
- **07 Robar carro**: PJ01_Carro_SacarConductor + PJ01_Conductor_SerSacado (sincronizados, 80 cuadros).
- **08 Prótesis de ataque**: implantes\PJ01_Implantes_Ataque.fbx (brazo láser + hombrera de misiles, con vacíos de salida).
- **09 Ruedas de carros**: carros\V02, V03, V04, V06, V09, V10.fbx (carrocería + 4 ruedas separadas).
- **10 Moto C19**: carros\C19.fbx (cuerpo + rueda delantera + trasera).
- **11 Armas nuevas**: armas\W05–W08.fbx, separadas en piezas.

## ⚠️ A medias (funciona, pero conviene revisar)
- **Clips hechos a mano**: escaleras, parkour, cobertura y robo de carro. No hay clips de Mixamo para eso en `imageassets\animaciones`, así que salen de poses con IK. Se ven limpios, pero si aparecen clips de Mixamo equivalentes conviene cambiarlos.
- **W07**: el modelo no trae una mira real. La pieza "Mira" es solo el alza trasera.
- **07 Robar carro**: el carro de la escena de revisión es un bloque de referencia. En Unity hay que alinear la puerta (o el hueco, porque los carros van sin puertas) con el inicio del clip.
- **Datos de Unity**: los ajustes finos (offsets de manos y armas, altura de la barda o del borde) quedan por calibrar en el juego con los valores anotados en cada tarea.

## ❌ No se pudo
- Nada bloqueante: todas las tareas 01–11 tienen sus archivos exportados.
- No se tocó la carpeta Assets de Unity ni se borraron originales.

### 01 · Ajuste en revisión (16-nov): manos (3.er intento, desde cero)
- **Pedido**: los dedos se veían separados y rotos, y luego "se sacuden".
- **Causas**:
  1. El rig tiene 4 dedos + pulgar (`L/R_Mid*` es el medio). En el 1.er intento se quedó suelto.
  2. La muñeca del clip de Mixamo da tirones (hasta 8° por cuadro, cambiando de dirección), y eso sacudía toda la mano.
- **Qué hice**:
  1. Regresé la mano a cero: borré la animación de los dedos (pose base del rig) y dejé la muñeca y los twist del antebrazo fijos en su pose promedio del ciclo. Ya no hay tirones: la mano sigue al antebrazo sin temblar.
  2. Desde ahí armé una mano relajada fija para los 5 dedos: curvatura suave que crece del índice al meñique y el pulgar al lado del índice (al menos 2 cm entre huesos para que no lo atraviese).
- **Verificado**:
  - Los 32 cuadros del ciclo, cada 4 cuadros, desde el lado y de frente, en las dos manos.
  - Los dedos no cambian contra la palma en ningún cuadro (variación 0).
  - Ningún dedo entra en la pierna ni en el bolsillo: la mínima separación es 3.2 cm, en el cuadro 56.
- **Re-exportado** (mismo nombre): `pj01_clips\PJ01_Caminar_SinImplante.fbx`, `pj01_clips\PJ01_Caminar_ConImplante.fbx`.
- **Previews actualizados**: 01_vista_lado, 01_vista_paso, 01_vista_frente, 01_vista_manos y 01_vista_manos_cuadros (hoja con todos los cuadros revisados).
- **Herramienta** para reusar en otros clips: `_herramientas\cc_hands.py` + `mano_relajada.json`.

### 02 · Ajuste en revisión (17-nov): manos
- **Qué hice**:
  - Puse la misma mano relajada de la tarea 01 en `Loco_Idle` y `Loco_Idle_ConImplante`: los 5 dedos juntos, curvatura suave y el pulgar junto al índice. La muñeca del idle ya era suave (menos de 0.6° por cuadro), así que la dejé como estaba.
  - Al revisar cuadro por cuadro vi que en `Loco_Idle` (sin implante) los dedos se metían en el bolsillo cargo del pantalón, sobre todo la mano izquierda. Abrí un poco los brazos desde el hombro: izquierdo 10°, derecho 4°. Es una rotación fija en todo el clip, así que el bucle no cambia.
- **Verificado** en los 150 cuadros (cada 3):
  - Ningún vértice de los dedos queda dentro de la pierna ni del bolsillo.
  - La versión con implante no chocaba (los dedos quedan a 11 cm o más del exoesqueleto), así que sus brazos no los toqué.
- **Re-exportado** (mismo nombre): `pj01_clips\PJ01_Loco_Idle.fbx`, `pj01_clips\PJ01_Loco_Idle_ConImplante.fbx`.
- **Previews actualizados**: 02_vista_frente, 02_vista_lado y 02_vista_manos_cuadros.

### 02 · Ajuste en revisión (17-nov): idle más lento y relajado, cabeza quieta
- **Pedido**: la mitad de lento, mucho más relajado y sin movimiento de cabeza.
- **Qué hice** (`_herramientas\cc_idle.py` → `relax_idle`):
  - **Más lento**: el bucle pasó de 151 a **301 cuadros (10 s)**, con el mismo movimiento a la mitad de velocidad.
  - **Más relajado**: todo el cuerpo (cadera, columna, hombros, brazos y muñecas) se mueve con la mitad de amplitud alrededor de su pose promedio. La respiración y el cambio de peso siguen, pero más suaves: la cadera se mueve 1.6 cm de lado a lado y como mucho 0.6 mm por cuadro.
  - **Cabeza quieta**: la cabeza queda fija mirando al frente (0° de variación). Ya no voltea ni cabecea.
  - Los pies se vuelven a fijar con IK a sus posiciones originales, así que no patinan. Se mantienen las manos relajadas y los brazos abiertos de la versión sin implante.
- **Verificado**:
  - El bucle cierra igual (el cuadro 301 es igual al 1).
  - Revisé uno de cada 5 cuadros: ningún dedo entra en la pierna ni en el bolsillo.
- **Respaldo** de la versión anterior dentro del maestro: `Loco_Idle_v16nov`, `Loco_Idle_ConImplante_v16nov`.
- **Re-exportado** (mismo nombre): `pj01_clips\PJ01_Loco_Idle.fbx`, `pj01_clips\PJ01_Loco_Idle_ConImplante.fbx`.
- **Para Unity**: el clip ahora dura 10 s (301 cuadros a 30 fps), con Loop Time activado.

### 03 · Ajuste en revisión (17-nov): brazos y manos de las escaleras
- **Problema encontrado** al revisar cuadro por cuadro con la vara de las manos: en los clips hechos a mano el IK del brazo se "volteaba".
  - Entrar / SalirAbajo: la mano subía pasando junto al hombro y el codo saltaba hasta 22 cm en un cuadro, con giros de 93–134°.
  - Subir / Bajar: el brazo giraba sobre sí mismo 116° en un solo cuadro.
- **Qué hice** (`_herramientas\cc_hands.py`):
  - **Entrar**: rehice el primer alcance de cada mano. La mano ahora sube adelante en arco (no pegada al hombro) y el codo cambia de dirección suave entre el inicio y el agarre.
  - **Subir y SalirArriba**: suavicé la rotación de la cadena del brazo y volví a poner las manos con IK exactamente en los mismos peldaños (desvío de 0.6 mm o menos).
  - **Bajar y SalirAbajo** se regeneraron como el reverso exacto de Subir y Entrar.
  - **Manos**: Entrar empieza y SalirArriba/SalirAbajo terminan con la misma mano relajada del caminar y el idle, así que ya no "brincan" los dedos al pasar de un clip a otro.
- **Resultado**:
  - Ningún salto. El máximo es Entrar 23–28° por cuadro y Subir 25–33° por cuadro (en la mano), y es la velocidad normal del alcance, que sube y baja suave.
  - Los clips encadenan sin salto: Entrar→Subir, Subir→Subir (bucle), Subir→SalirArriba y Bajar→SalirAbajo, todos con 0° de diferencia.
- **Respaldo** dentro del maestro: `Escalera_*_v16nov`.
- **Re-exportado** (mismo nombre): `PJ01_Escalera_Entrar`, `_Subir`, `_Bajar`, `_SalirArriba` y `_SalirAbajo` (`pj01_clips\`). Los datos para Unity no cambian.

- **03 · Estado (17-nov)**: el usuario lo dejó para revisar después (pendiente).

### 01 · Ajuste en revisión (17-nov): pies menos abiertos (solo el zapato)
- **Pedido**: girar solo el zapato hacia adentro ("15%" → lo tomé como 15°), sin mover nada más.
- **Qué hice**: regresé piernas, cadera y todo lo demás a como estaban, y giré **solo el pie y la punta** 15° hacia adentro sobre el tobillo.
- **Resultado**:
  - En el apoyo la punta pasó de unos 22° hacia afuera a unos 7°.
  - Rodillas y piernas, igual que antes.
  - Al despegar, la punta patina un poco más que antes: 9 y 15 mm por cuadro, contra 7 y 12 mm.
- **Re-exportado** (mismo nombre): `pj01_clips\PJ01_Caminar_SinImplante.fbx`, `pj01_clips\PJ01_Caminar_ConImplante.fbx`.
- **Previews**: 01_vista_lado, 01_vista_frente y 01_vista_pies.

### 04 · Ajuste en revisión (17-nov): brazos y manos del parkour
- **Problema encontrado** (misma revisión cuadro por cuadro que en caminar y escaleras): el IK de los brazos se volteaba.
  - El codo giraba hasta 179° en un cuadro (SaltarBarda) y la muñeca hasta 161° (TreparBorde).
  - Los dedos quedaban con la mano vieja de Mixamo.
- **Qué hice** (`_herramientas\cc_hands.py` → `clean_arms`):
  - Rehice los brazos cuadro por cuadro, partiendo siempre del cuadro anterior para que el giro sea continuo.
  - Las manos que tocan algo (borde, piso) se quedan exactamente donde estaban. Las manos libres siguen un camino suavizado.
  - El codo apunta a una dirección suave y el antebrazo gira con la mano para que la muñeca no se tuerza.
  - **TreparBorde**: el salto hacia el borde (cuadros 4–14) se rehízo como en la escalera: las manos suben por delante en arco, sin pasar pegadas al hombro.
  - **Dedos**: donde la mano está libre usa la mano relajada; los agarres y empujes se conservan.
- **Resultado** (mayor giro por cuadro, medido contra el hueso padre):
  - SaltarBarda: de 179° a 20°.
  - RodarCaida: de 80° a 29°.
  - TreparBorde: de 161° a 45°. Los picos que quedan están en los cuadros 37–41, cuando el cuerpo pasa por encima de las manos apoyadas.
- **Respaldo** dentro del maestro: `Parkour_*_v16nov`.
- **Re-exportado** (mismo nombre): `PJ01_Parkour_SaltarBarda`, `_TreparBorde` y `_RodarCaida` (`pj01_clips\`). Los datos para Unity no cambian.

- **04 · Estado (17-nov)**: se mostró con los brazos corregidos; el usuario siguió a la 05 sin pedir cambios.
### 05 · Ajuste en revisión (17-nov): manos al bajar de la moto
- **Revisado** cuadro por cuadro con la misma vara: los brazos no tienen saltos (máx. 23° por cuadro, medido contra el hueso padre).
- **Corregido**: al soltar el manubrio los dedos quedaban en la mano vieja de Mixamo y "brincaban" al pasar al idle. Ahora pasan suave a la mano relajada; el agarre del manubrio se conserva.
- En la escena de revisión, el idle del final se cambió por el idle nuevo (10 s, cabeza quieta).
- **Respaldo** dentro del maestro: `Moto_Bajar_v16nov`.
- **Re-exportado** (mismo nombre): `pj01_clips\PJ01_Moto_Bajar.fbx`. Los datos para Unity no cambian.
- **Previews**: 05_vista_pierna, 05_vista_atras, 05_vista_final y uno nuevo, 05_vista_manos.

- **05 · Corrección (17-nov)**: en la escena de revisión, al terminar de bajar el personaje "regresaba" encima de la moto. No era el clip: el idle de la escena empezaba en el origen en lugar de donde termina la bajada. Le puse a la escena el desplazamiento del final (0.73 m a la izquierda y 0.07 m hacia atrás), así que ahora se queda parado al lado. Lo más cerca que queda de la moto es a 2 cm.
  - **Para Unity**: hay que aplicar el root motion de `PJ01_Moto_Bajar` (o mover al personaje 0.70 m a su izquierda al terminar el clip) **antes** de pasar al idle. Si no, va a aparecer encima de la moto, igual que pasaba aquí.

- **05 · Ajuste (17-nov): postura en la moto y manos en el manubrio**
  - **Pedido**: el personaje se veía muy jorobado al manejar y las manos no estaban bien sobre el manubrio.
  - **Antes**:
    - La espalda iba curva: la columna se doblaba cada vez más hacia arriba (−15°, 12°, 38° y 54°) y la inclinación total era de 33°.
    - Las manos agarraban unos 10 cm por fuera de los puños y 4 cm abajo del tubo, con los nudillos cruzados respecto al manubrio.
  - **Qué hice** (`_herramientas\cc_ride.py`):
    - **Espalda**: la puse recta (pelvis 20°, columna 24°/26°, cuello 30°). La inclinación total quedó en 26° y la cabeza levanta un poco la vista al frente.
    - **Manos**: medí los puños reales del manubrio de la C19 (x ±0.285, y −0.44, z 1.005). Coloqué cada mano para que los dedos envuelvan el puño, con los nudillos a lo largo del tubo; quedaron a 0 cm del puño.
    - **Brazos**: quedan con el codo un poco doblado (alcance al 96–98 %).
    - **Pies**: no se mueven.
  - **Clips afectados**:
    - `Moto_Manejar`: entero.
    - `Moto_Subir` y `Moto_Bajar`: el cambio entra y sale suave según qué tan cerca están las manos del manubrio.
    - Las uniones entre los tres clips cambian 2 mm o menos de un cuadro al otro.
  - **Revisado**: los brazos giran 27° por cuadro o menos y no hay saltos.
  - **Respaldo** dentro del maestro: `Moto_Subir_v17nov`, `Moto_Manejar_v17nov`, `Moto_Bajar_v17nov`.
  - **Re-exportado** (mismo nombre): `PJ01_Moto_Subir`, `PJ01_Moto_Manejar` y `PJ01_Moto_Bajar` (`pj01_clips\`).
  - **Previews**: 05_vista_manejar (nuevo), 05_vista_manos, 05_vista_pierna, 05_vista_atras y 05_vista_final.

- **05 · Estado (17-nov)**: el usuario aprobó la nueva postura en la moto ("mejor").

### 06 · Ajuste en revisión (17-nov): mano de apoyo en el guardamanos
- **Revisado** con la misma vara que en las anteriores:
  - Brazos: no tienen saltos (máx. 27° por cuadro). Solo había un tirón de la muñeca izquierda de 42° en el cuadro 2 de `CoberturaRifle_Entrar`.
  - Mano izquierda (de apoyo): no agarraba el arma. Quedaba unos 5 cm por debajo del guardamanos, con los dedos abiertos y las puntas metidas en él.
- **Qué hice** (`_herramientas\cc_grip.py`):
  - Subí la mano izquierda 3 cm, la adelanté 3 cm y la pegué al guardamanos, con la palma por debajo.
  - Cerré la separación de los dedos y los curvé hasta tocar el costado del guardamanos (a menos de 2 cm, sin entrar), con el pulgar por el otro lado.
  - La mano queda fija al arma en todos los cuadros de los tres clips (error de IK de 0 mm). Con eso desaparece también el tirón del cuadro 2.
- **Respaldo** dentro del maestro: `CoberturaRifle_*_v17nov`.
- **Re-exportado** (mismo nombre): `PJ01_CoberturaRifle_Entrar`, `_Idle` y `_Disparo` (`pj01_clips\`). Los datos para Unity no cambian.
- **Previews**: 06_vista_cubierto, 06_vista_disparo, 06_vista_frente y uno nuevo, 06_vista_manos.

- **06 · Ajuste (17-nov): pie de la rodilla apoyada**
  - **Pedido**: al agacharse, el zapato quedaba plano en el piso, cuando debería apoyar solo la punta con la suela al aire.
  - **Detalle**: el pie mal era el de la rodilla en el suelo, que es el **derecho** del personaje (desde la cámara se ve a la izquierda). La espinilla iba acostada en el piso, el zapato plano detrás y la punta enterrada 6 cm. El pie izquierdo, el de la rodilla arriba, ya estaba bien plano.
  - **Qué hice** (`_herramientas\cc_kneel.py`):
    - La rodilla se queda en el piso. La espinilla se levanta un poco, con el tobillo a 25 cm.
    - El pie se inclina 58° con la punta apoyada (bola del pie a 3.5 cm) y el talón arriba. Los dedos quedan doblados planos sobre el piso.
    - El cambio entra y sale según qué tan baja está la rodilla, así que al levantarse para disparar vuelve suave al pie plano (18° por cuadro o menos).
  - **Re-exportado** (mismo nombre): `PJ01_CoberturaRifle_Entrar`, `_Idle` y `_Disparo`.
  - **Previews**: 06_vista_cubierto y 06_vista_frente actualizadas, y una nueva: 06_vista_pies.

- **06 · Estado (17-nov)**: aprobado por el usuario ("está bien").

### 07 · Ajuste en revisión (17-nov): brazos y manos al robar el carro
- **Revisado** con la misma vara:
  - El que roba (`Carro_SacarConductor`): las dos muñecas daban un tirón de unos 92° en el cuadro 4. Además agarraba al conductor con la mano abierta de Mixamo (los dedos nunca se cerraban).
  - El conductor: los brazos están bien (máx. 49° por cuadro mientras lo jalan), pero terminaba tirado en el piso con la mano vieja.
  - Revisé que el conductor no atraviese el carro al caer: no lo atraviesa (queda a 26 cm o más).
- **Qué hice**:
  - **Brazos del que roba**: los rehice cuadro por cuadro (`clean_arms`). Ya no hay tirón; lo máximo es 54° por cuadro en el alcance rápido de los cuadros 3–8. Las manos siguen sobre el conductor durante el jalón (variación de 4 cm o menos).
  - **Manos del que roba**: puño de agarre durante el jalón (entra en los cuadros 4–10 y sale en los 46–52). El resto del tiempo, mano relajada.
  - **Conductor**: al quedar en el piso, sus manos pasan a la mano relajada (cuadros 60–75). El agarre del volante al inicio se conserva.
  - **`Carro_Entrar`**: empieza con la mano relajada para empatar con el final de `Carro_SacarConductor` (0° de diferencia en la unión).
- **Respaldo** dentro del maestro: `Carro_SacarConductor_v16nov`, `Conductor_SerSacado_v16nov`, `Carro_Entrar_v16nov`.
- **Re-exportado** (mismo nombre, reemplazados): `PJ01_Carro_SacarConductor`, `PJ01_Conductor_SerSacado` y `PJ01_Carro_Entrar` (`pj01_clips\`). Los datos para Unity no cambian.
- **Previews**: 07_vista_agarra, 07_vista_jala, 07_vista_avienta y uno nuevo, 07_vista_manos.

- **07 · Ajuste (17-nov): subirse al carro sin esperar**
  - **Pedido**: después de aventar al conductor, en el cuadro 69 ya debe estar subiéndose al carro.
  - **Qué hice**: hasta el cuadro 46 (agarre, jalón y aventón) no cambió nada. De ahí al final comprimí el clip: lo que antes tomaba de 46 a 80 ahora va de 46 a 69, 1.48 veces más rápido. Termina en la misma pose, que empata con el inicio de `Carro_Entrar`.
  - **Ahora**:
    - `PJ01_Carro_SacarConductor` dura **69 cuadros** (antes 80). En la escena de revisión, `Carro_Entrar` empieza en el cuadro 69.
    - La unión es suave (5 cm por cuadro o menos) y los brazos no tienen saltos (máx. 55° por cuadro).
  - **Para Unity**: al terminar `PJ01_Carro_SacarConductor` (cuadro 69) se pasa directo a `PJ01_Carro_Entrar`. `PJ01_Conductor_SerSacado` **no cambia**: sigue durando 80 cuadros, y el conductor termina de caer por su cuenta mientras el otro ya se sube.
  - **Respaldo** dentro del maestro: `Carro_SacarConductor_v17nov` (la versión de 80 cuadros).
  - **Re-exportado**: `pj01_clips\PJ01_Carro_SacarConductor.fbx`.
  - **Previews**: 07_vista_avienta actualizada y una nueva, 07_vista_sube (cuadro 78).

- **07 · Estado (17-nov)**: el usuario siguió a la 08 sin pedir más cambios.

- **08 · Revisión (17-nov)**: en la escena puse el idle nuevo (10 s, cabeza quieta). Revisé choques en idle, golpe y apuntar (689 cuadros, uno de cada 12): el pod de misiles nunca toca la cabeza, pero llega a quedar a 1.4 cm del pelo en el golpe y al apuntar (cuadros 217, 337 y 601). El brazo láser no atraviesa el cuerpo. No re-exporté nada.

- **08 · Ajuste (17-nov): base del lanzamisiles bien apoyada en el hombro**
  - **Pedido**: la base del lanzamisiles no estaba bien apoyada en el hombro.
  - **Antes**: el soporte del pod quedaba detrás del borde de la hombrera izquierda, colgando sobre la orilla trasera (3–9 cm sobre la superficie, y en parte sobre nada).
  - **Qué hice** (sin deformar; solo moví la pieza): moví el pod completo con su soporte 12 cm hacia adelante, 1.5 cm hacia afuera y 3.2 cm hacia arriba, para que la base quede sentada encima del domo de la hombrera, tocándola o a 3 cm o menos. También moví el vacío de salida `IMP05_Salida` lo mismo.
  - **Revisado**: lo más cerca que queda el pod de la cabeza es a 2.7 cm, en el golpe y al apuntar (antes 1.4 cm). Ajuste guardado en `_herramientas\imp05_pod_ajuste.json`.
  - **Re-exportado**: `implantes\PJ01_Implantes_Ataque.fbx` (armadura, IMP04, IMP05 y los 2 vacíos, texturas embebidas). Lo verifiqué reimportándolo.
  - **Previews**: 08_vista_frente, 08_vista_atras y 08_vista_camara_hombro actualizadas, y una nueva: 08_vista_base.

### 08 · Ajuste en revisión (17-nov): prótesis láser como "súper puño"
- **IMP04_BrazoLaser** ahora es el modelo completo de Tripo (antebrazo + **mano robótica**), escala **uniforme** (sin deformar), rígido a R_Forearm (peso 1.0). Va desde el codo hasta más allá de la punta de los dedos (~0.55 m de juego) y sustituye a la mano. Corrido 3.5 cm hacia el dorso para que en el idle no se meta en el muslo.
- `IMP04_Salida` movido a la lente del emisor: respecto a R_Forearm 0.253 a lo largo del hueso (hacia la mano), 0.053 hacia el dorso, 0 lateral (unidades del armature; ×1.8367 = m de juego).
- **Mano derecha oculta**: en la escena de revisión, restricción `Ocultar_Mano` (Limit Scale 0.001) en el hueso R_Hand. **Unity**: con la prótesis equipada, en LateUpdate poner `R_Hand.localScale = Vector3.zero` (oculta la mano y los dedos; el antebrazo humano queda dentro de la prótesis).
- Con el puño grande el golpe y el apuntado se metían en la cara y en la mano izquierda, así que hice clips propios de prótesis (los originales no se tocaron):
  - `pj01_clips\PJ01_Protesis_Golpe.fbx` (de Punos_GolpeDer): hombro 10° y codo 16° más abiertos en la guardia para que el puño no entre en la cara; en la extensión el codo no pasa de recto.
  - `pj01_clips\PJ01_Protesis_Apuntar.fbx` (de Pistola_Apuntar): apunta con un brazo; el izquierdo baja relajado en 7 cuadros (ya no agarra la mano).
- Revisado cuadro a cuadro (1–689): la prótesis no atraviesa cuerpo, cabeza, muslos ni mano izquierda.
- Re-exportado `implantes\PJ01_Implantes_Ataque.fbx` (verificado reimportando: IMP04 21774 vértices, IMP05 20925, los 2 Empties en su hueso). Maestro actualizado (malla anterior guardada como `IMP04_BrazoLaser_mesh_v17nov`).
- Vistas: 08_vista_protesis.png (idle, guardia, golpe, apuntar), 08_vista_frente.png.

### 08 · Ajuste en revisión (17-nov, 2): base de la prótesis alineada con el brazo
- La base (aro del codo) estaba inclinada ~16° y corrida hacia el dorso, así que el brazo no parecía entrar en ella. Giré y moví IMP04 (rígido, sin deformar) para que el eje del tubo coincida con el antebrazo: ahora el brazo entra por el centro del aro y la mano robótica queda al final.
- `IMP04_Salida` respecto a R_Forearm: 0.241 a lo largo del hueso, 0.083 hacia el dorso, 0 lateral (unidades del armature).
- Al quitar el corrimiento, la prótesis volvía a rozar el muslo en el idle. Hice `pj01_clips\PJ01_Protesis_Idle.fbx` (Loco_Idle con el brazo derecho 4° más abierto; el original no se tocó). Unity: con la prótesis equipada usar Protesis_Idle, Protesis_Golpe y Protesis_Apuntar.
- Reajusté Protesis_Golpe y Protesis_Apuntar a la nueva posición (2–5° en hombro/codo donde rozaba cara o pecho). Revisado cuadro a cuadro 1–689: sin choques.
- Re-exportado `implantes\PJ01_Implantes_Ataque.fbx` (verificado reimportando) y actualizado el maestro (3 acciones Protesis_*).
- Vistas actualizadas: 08_vista_protesis.png, 08_vista_frente.png.

# PEDIDO 17-nov noche (CYBERCITY)

## A · Llantas (y cofre/puertas) de V01a, V01b, V05, V07, V08 y V11 — HECHO
Archivo de trabajo: `imageassets\blender\carros\carros_trabajo.blend` (fuentes en `carros\_fuentes\`; script `_herramientas\cc_build_17nov.py`).
Mismo formato que V02–V10: raíz vacía con el código, frente hacia -Y de Blender = +Z de Unity, piso en z = 0, escala de Tripo (≈1 m; ×4.3 para tamaño real como los demás). Cada pieza con su pivote; las ruedas giran sobre su X local.

| FBX (`carros\`) | Piezas | Notas |
|---|---|---|
| V01A.fbx | Carroceria, Cofre, Rueda_DI/DD/TI/TD | RU01 r 0.088 al hueco (centros y -0.266 / 0.296, x ±0.228) |
| V01B.fbx | Carroceria, Cofre, Rueda_* | RU01 r 0.092 (y -0.276 / 0.294, x ±0.207) |
| V05.fbx (patrulla sedán) | Carroceria, Cofre, Rueda_* | RU01 r 0.086 (y -0.274 / 0.264, x ±0.201) |
| V07.fbx (moto reparto) | Cuerpo, Rueda_D, Rueda_T | ruedas originales separadas (r 0.145) |
| V08.fbx (moto patrulla) | Cuerpo, Rueda_D, Rueda_T | ruedas originales (venían sueltas): D r 0.157, T r 0.140 |
| V11.fbx (camioneta policía) | Carroceria, Cofre, Puerta_I, Puerta_D, Rueda_* | RU01 r 0.13 (y -0.276 / 0.22, x ±0.17) |

- **"La patrulla" = V05**: `modelos3d\V05.zip` resultó ser el mismo modelo que V06 (misma malla, textura idéntica), así que usé la carrocería de patrulla sedán `piezas\V05CARROZA.zip`.
- **V11**: la llanta original está fundida con la salpicadera (no hay hueco entre ellas), no se puede separar limpia; la borré y puse RU01 (como en V09/V10). Las salpicaderas se quedan en la carrocería.
- **V01A, V01B, V05**: son carrocerías sin ruedas; la RU01 se ajustó al hueco medido con rayos y la carrocería se subió para que las llantas toquen el piso.
- **Cofre** (V01A, V01B, V05, V11): recortado de la propia carrocería con cortes rectos, con cara de abajo para que se vea al abrirlo; debajo queda una tapa oscura (material Corte_Interior) como compartimiento del motor. Pivote en la bisagra (borde de atrás, junto al parabrisas): abrir girando **negativo en X local** de Blender (≈ -45°). Bisagras: V01A (0, -0.118, 0.296), V01B (0, -0.128, 0.263), V05 (0, -0.162, 0.28), V11 (0, -0.207, 0.301).
- **Puertas**: V11 trae Puerta_I y Puerta_D (delanteras) recortadas de su carrocería, pivote en la bisagra delantera (±0.23, -0.158, 0.254); abren girando en Z local: Puerta_I negativo, Puerta_D positivo (≈65°). Detrás hay un panel oscuro. V01A/V01B/V05 siguen **sin puertas** (sus carrocerías traen el hueco abierto, decisión del 9-nov). V07/V08 son motos.
- Verificado reimportando los 6 FBX: nombres, pivotes y piezas en su lugar.

## B · Piezas modulares para zonas (rejilla de 4 m) — HECHO
Archivo de trabajo: `imageassets\blender\zonas\zonas_trabajo.blend` (generadas por código: `_herramientas\cc_zonas.py` y `cc_zonas_piezas.py`).
Exportadas una por FBX en `imageassets\blender\zonas\` · **metros reales (Scale Factor 1 en Unity)** · low poly (24–600 caras) · UV en caja a 1 m (para texturas que repiten) · pivote en la base.
Convención: el lado "de afuera" (mar, calle, vacío) mira a **-Y de Blender = +Z de Unity**; los tramos lineales corren a lo largo de X (4 m, de -2 a +2), así que se encadenan cada 4 m.
Materiales con nombre fijo para cambiarlos en Unity: Concreto, ConcretoClaro, Asfalto, PinturaBlanca, Madera, MaderaOscura, Roca, Metal, Hierro, Contenedor, Malla, Fondo, Luz (emisivo).

| FBX | Medidas | Notas |
|---|---|---|
| ML01_BarandalMalecon | 4 × 0.4 × 1.08 m | balaustrada de concreto: zócalo, 9 balaústres, pasamanos; medio pilar en cada extremo (al juntar dos forman un pilar) |
| ML02_EscaleraPlaya | 4 × 4 m, baja 2 m | pivote en la arena (z 0); arriba (borde +Y) a 2 m = nivel del malecón; 10 escalones de 3 m de ancho entre muros |
| ML03_MuelleRecto | 3 m de ancho × 4 m | pivote en el nivel del agua; tablones a 1.2 m; pilotes cada 2 m bajan a -2 m; corre a lo largo de Y |
| ML04_MuelleEsquina | celda 4 × 4 | dobla de -Y a +X (une un tramo recto que llega por -Y con otro que sale por +X; girarla para las otras esquinas) |
| ML05_Escollera | ≈4 × 4 m, 2 m de alto | rocas low poly; la parte baja hacia el mar (-Y); se encimas un poco con la siguiente al encadenar |
| CE01_EscaleraCerro | 2.3 m de ancho × 4 m, sube 2 m | 12 escalones con bordillo; la siguiente va en (0, +4, +2); losa por abajo |
| CE02_BarandalTubo | 4 m, 1.05 m | tubo con postes cada 2 m |
| CE03_BarandalTuboEscalera | 4 m, inclinado 2 m | **extra**: el mismo barandal con la pendiente de CE01 (ponerlo a ±1.2 m en X de la escalera) |
| AU01_AutopistaRecta | 12 × 8 m, 2.1 m | losa cajón de 1.2 m (asfalto arriba en z 1.2), parapetos, líneas; pivote abajo al centro |
| AU02_AutopistaCurva45 | radio 24 m al eje | pivote al inicio (borde de entrada); termina en (7.03, 16.97) girada 45°; **dos curvas = vuelta de 90° que cae en la rejilla (24, 24)** |
| AU03_AutopistaRampa | 12 × 48 m | sube del piso (asfalto z 0) a 7.2 m = altura del asfalto de AU01 sobre AU04; muros de contención y estribo al final |
| AU04_ColumnaAutopista | 6 m de alto | columna redonda + cabezal de 9.8 m; la losa AU01/AU02 va encima (su pivote en z 6) |
| AU05_Guardarrail | 4 m, 0.75 m | lámina W con postes cada 2 m |
| AU06_EntradaTunel | portal 16 × 11 m | hueco de 10 m de ancho × 9 m; tubo de 14 m hacia +Y con fondo negro y tira de luz; pivote en el piso al centro del portal |
| PT01_Contenedor40 | 12.19 × 2.44 × 2.59 m | medida real de 40 pies; **un solo material "Contenedor"** (cambiar el color en Unity); puertas en el extremo +X |
| PT02_BitaAmarre | 0.6 × 0.6 × 0.62 m | bita de hierro |
| DS01_RejaPuas | 4 m, 2.4 m (+brazos) | postes cada 2 m, malla con textura `T_Malla.png` (alfa: usar Cutout en Unity), 3 hilos de púas y concertina hacia afuera (-Y) |
| LM01_MuroLuz | 4 × 0.45 × 3.12 m | muro blanco con remate; tira de luz (material Luz, emisivo) al pie y bajo el remate, del lado calle (-Y) |

Verificado reimportando las 18: medidas y pivotes correctos. Vistas: `_REVISION\17N_vista_zonas.png`, `17N_vista_carros.png`, `17N_vista_V11_puertas.png`.

### Resumen pedido 17-nov noche
- **Hecho**: A (6 vehículos con llantas separadas; cofre en 4; puertas en V11) y B (16 piezas pedidas + 1 extra, CE03).
- **A medias**: V11 usa RU01 en vez de su llanta original (venía fundida con la salpicadera). V01A/V01B/V05 siguen sin puertas (hueco abierto, decisión del 9-nov). Las piezas de zonas van con colores lisos; las texturas T04/T05/T06 del pedido de arte se pueden poner encima (UV a 1 m).
- **No se pudo**: nada.


## 18-nov — A) Vehiculos nuevos V12–V24, MT01, MT02  (carros\carros_nuevos.blend, script _herramientas\cc_veh.py)
- Fuentes: modelos-nuevos\nuevos-17nov\*.zip (sin _v1), descomprimidos en carros\_fuentes\<codigo>\. Escala real (V12-V15,V20-V24 sedanes 3.8–5.1 m; V16 microbus 7 m; V17 camion 8 m; V18 trailer con caja 16 m; V19 ambulancia 6 m; motos 2.05/2.4 m). Frente -Y Blender (= +Z Unity), pivote en el piso al centro.
- Llantas: en los coches la llanta Tripo viene soldada a la salpicadera (gap test sin hueco limpio) -> se borra la llanta original y se pone RU01 (como V09/V10/V11), ancho/radio medido a mano de vistas ortogonales a 1 cm/px. Posicion X simetrizada por eje. V18 tiene 4 ejes: Rueda_DI/DD, Rueda_T1x, T2x, T3x.
- Motos: llantas originales separadas (franja |x|<=0.105 m MT01, 0.15 m MT02) + islas sueltas absorbidas a la llanta. Sin cofre/puertas (no aplica).
- Cofre: pivote en la bisagra trasera (abre con rotacion -X local). Debajo: Motor = bahia oscura con paredes + bloque, tapa de valvulas, bujias, admision, filtro, bateria y radiador (Motor_Bahia/Motor/Motor_Metal). Ancho del cofre limitado para no comerse las salpicaderas.
- Puertas delanteras: pivote en la bisagra delantera; Puerta_I abre con -Z, Puerta_D con +Z. Cada puerta lleva de hijo su Cristal_Puerta_I/D.
- Cristales: caras de ventana detectadas por color de textura (azulado/oscuro) dentro de una "cerca" por coche, crecidas a los reflejos, borradas de la carroceria y puestas en Cristal_Parabrisas / Cristal_Medallon / Cristal_Laterales con material "Cristal" (alpha 0.28, rough 0.04, azulado).
- Interior: Interior_Casco (cara interna oscura de la cabina) + Interior (piso, tablero, volante, asientos).
- Exportados: carros\V12..V24.fbx, MT01.fbx, MT02.fbx (raiz vacia con el codigo en el origen, hijos sin prefijo, texturas embebidas, 5–11 MB c/u). Verificado reimportando V13.
- Pendiente/observaciones: en coches oscuros (V12, V20, V22, V24) la deteccion de cristal deja bordes dentados en algunas ventanas; V16 tiene morro corto, cofre chico; V22/V23 (deportivos de motor central) el cofre delantero muestra la bahia con las llantas a los lados; MT01 no lleva Cristal (su parabrisas es pequeno y va pintado oscuro).


## 18-nov — B) Cristales en los carros viejos + C) motor  (carros\carros_trabajo.blend, scripts cc_old.py / cc_veh.py)
- Los carros viejos estan a escala Tripo (~1 m de largo). Para trabajarlos se escalaron x4.5 de forma rigida y uniforme (malla + posiciones, sin deformar), se procesaron en metros y se regresaron a 1/4.5. Medidas originales verificadas despues de regresar. NO se cambio su escala final (V01A..V11 siguen ~1 m como antes; los nuevos V12..V24 si estan en metros reales). Si en Unity quieren todo homogeneo, V01A..V11 van con escala x4.5.
- V02, V03, V04, V06, V09, V10, V11 (ventanas pintadas): caras de ventana detectadas por color dentro de una cerca por carro (sin tocar portaequipaje, caja de pickup ni torreta), borradas de la carroceria y puestas en Cristal_Parabrisas/Medallon/Laterales (material Cristal). V11: el color de la ventana es casi el del cuerpo azul marino; se uso "oscuro y poco saturado" (s<0.21, v<0.34). Las puertas de V11 llevan Cristal_Puerta_I/D como hijo.
- Interior simple oscuro (Interior_Casco + Interior con piso, tablero, volante y asientos) en V02, V03, V04, V06, V09, V10, V11.
- V01A, V01B, V05 (carrozas): las ventanas son huecos abiertos y ya traen interior. Se pusieron vidrios rellenando los huecos cerrados (parabrisas, medallon y ventanas laterales cerradas) con un barrido de rayos; los huecos de puerta abiertos (sin puerta) se dejan sin vidrio a proposito. V01B no tiene medallon abierto.
- C) Motor: la placa plana gris Corte_Interior de V01A, V01B, V05 y V11 se quito y se puso Motor (bahia oscura con paredes + bloque, tapa, bujias, admision, filtro de aire, bateria, radiador).
- Re-exportados: carros\V01A, V01B, V02, V03, V04, V05, V06, V09, V10, V11 .fbx (hijos sin prefijo, texturas embebidas).


## 18-nov — D) Piezas de zonas con textura  (zonas\zonas_trabajo.blend, cc_zonas.py / cc_zonas_piezas.py; respaldo *_v17nov.py)
- Texturas nuevas en zonas\texturas\ (repetibles, sin costura): T_ConcretoManchado (concreto desgastado con manchas de agua, escurrimientos, grietas, moho), T_MaderaVieja y T_MaderaVieja_Oscura (veta gastada), T_MetalOxidado (galvanizado con oxido), T_HierroOxidado (hierro negro con oxido), T_Roca. Copiadas de imageassets\calle: T03_Asfalto, T04_Arena, T05_ConcretoCerro, T06_Adoquin.
- Asignacion: barandal malecon, escaleras, autopista (recta/curva/rampa), columna, tunel y muro = concreto manchado; escalera del cerro = T05_ConcretoCerro; carriles = T03_Asfalto; muelle = madera vieja (pilotes/largueros en la oscura); bita = hierro oxidado; reja, guardarrail y barandal de tubo = metal oxidado (la malla sigue con T_Malla); escollera = roca. Contenedor sin cambio (un solo material para cambiarle el color).
- UV: proyeccion de caja con la escala metida en las UV (concreto 2 m por repeticion, asfalto 4 m, madera/metal 1 m, roca 1.5 m), asi se ve igual en Unity sin tocar el tiling. Imagen conectada directo a Base Color (sin nodos intermedios) para que el FBX la embeba.
- Muelle (ML03 recto, ML04 esquina): ahora 4 x 4 m (ocupa toda la celda), cubierta a 0.60 m sobre el pivote (antes 1.20), tablones de 22 cm con separacion de 3.5 cm y un poco disparejos, 4 largueros, 2 filas de 4 pilotes que bajan a -3.0 m (al agua) con cruces de refuerzo, guardacanto bajo. ML04 cierra por -X y +Y y trae una bita chica. Pivote igual que antes (centro, z=0 = nivel del agua).
- Bita PT02: 0.60 m exactos (base 0.6 x 0.6 con pernos, cuerpo y cabeza de 0.5 m). Verificado reimportando el FBX.
- Medidas, rejilla de 4 m y pivotes de las demas piezas sin cambio. Re-exportadas las 18 FBX con texturas embebidas.
- Ojo: al reimportar AU01-AU03 en Blender, el material PinturaBlanca aparece ligado a la imagen del asfalto (rareza del importador/exportador FBX). Si en Unity las lineas salen grises, quitarle la textura al material PinturaBlanca.
- Capturas: 18N_A_vehiculos_V12-V18.png, 18N_A_vehiculos_V19-V24.png, 18N_A_V16_V22_final.png, 18N_A_motos.png, 18N_B_cristal_interior_V02-V11.png, 18N_B_cristal_carrozas_V01A_V01B_V05.png, 18N_C_motor_V01A_V11.png, 18N_D_zonas_texturas.png, 18N_D_muelle_con_agua.png, 18N_D_texturas.png.



## 19-nov — T-1381) Rig de los personajes nuevos de Tripo  (personajes\*.fbx, scripts _herramientas\t1381_*.py)
- Esqueleto: el mismo de E10_PandilleroSnakes / GuardiaNova (Mixamo, `mixamorig:Hips`... 52 huesos con dedos y puntas, T-pose, pose de pies a -Y). Tomado de `Assets/Arte/Personajes/E10_PandilleroSnakes/E10_PandilleroSnakes.fbx` y de `GuardiaNova.fbx`; los Humanoid de Unity se mapean solos y les sirven las animaciones de `Animaciones/` (Loco_*, etc.).
- Hallazgo: los zips de Tripo ya traen el esqueleto mixamorig con pesos para casi todos. Solo NO lo traen E15_b, E17_a, E18_a y E19_a. Los demas: se importaron, se normalizaron y se probaron.
- Normalizacion (t1381_lib.normalizar): escala uniforme aplicada a la altura real, pies en z=0, centrado en X, frente -Y (comprobado con dedo vs tobillo del esqueleto), sin rotacion ni escala residual en el objeto. Alturas: E15 1.85, E16 2.45, E18 1.90, E19 1.78, E20 1.88, NP07 1.72, NP08 1.75, NP09 1.78, NP10 1.65, NP11 1.82, NP12 1.55, NP13 1.72, NP14 1.75, E17 0.90 (con torreta).
- E15_b, E18_a: sin rig. Se uso el esqueleto de E15_a como plantilla, ajustado a la malla con marcas (z de entrepierna, altura de brazos, ancho de brazos y pies), y los pesos se pasaron por Data Transfer (vertice mas cercano interpolado) desde E15_a. E19_a: es la MISMA malla que E20_a (27 748 vertices, medidas identicas), se rigeo con el esqueleto y pesos de E20_a.
- E16_b: el modelo venia con los brazos colgando (minigun + escudo, 62 grados bajo la horizontal). Para que sirva como Humanoid se llevo a T-pose (posar brazos, aplicar modificador, aplicar pose como reposo). Los brazos de la T-pose se ven deformados (minigun fino), pero con las animaciones de caminar se ve bien. Los pesos de E16_b solo tienen 50 grupos (faltan 2 huesos del original).
- Prueba: pose de prueba (brazos abajo, paso largo, sentadilla) + 3 cuadros del Loco_Caminar.fbx retargeteado por rotaciones en mundo. Hojas: _REVISION\T1381_<Codigo>_<a|b>_pruebas.png.
- E17_PerroRobotNova_a: no hay animaciones de cuadrupedo en el proyecto. Rig generico de 19 huesos: Raiz > Cadera > Columna > Cuello > Cabeza; Cadera > Cola > Cola2; patas Pata_DI_1/2/3 (delantera izq.), Pata_DD_*, Pata_TI_*, Pata_TD_* (izquierda = +X de Blender). Cabeza hacia -Y, 0.90 m de alto, ~1.1 m de largo. Pesos por cercania al segmento del hueso (max 4). Clips dentro del FBX: Perro_Idle (60 cuadros, 30 fps, bucle, respira, cola y cabeza) y Perro_Caminar (18 cuadros, bucle, trote con diagonales; unos 0.8 m/s). En Unity: animationType Generic.
- Variantes sin zip (solo .png, sin rigear): E17_b, E18_b, NP10_b, NP12_b. E19_a_2 se rigeo tambien (extra del zip).
- Exportados: personajes\<Codigo>_<a|b>.fbx (armature + malla, texturas embebidas, escala 1, Y arriba, sin animacion salvo E17). Verificado reimportando: 52 huesos, -Y, alturas y pies en 0.
- Pendiente/observaciones: en la T-pose del E16 y E20/E19 los brazos llevan piezas robot que rozan el torso al bajarlos; los dedos usan los pesos de Tripo sin retocar.
