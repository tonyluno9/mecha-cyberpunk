# REVISIÓN CON TONY — 20-nov (personajes, animaciones y vehículos)

Esto es solo diagnóstico. **No cambié ninguna animación ni ningún FBX del juego.** Todo lo que se ve aquí sale de los FBX que Unity usa hoy (`Assets/Arte/Personajes` y `Assets/Arte/Vehiculos`), con la escala real que tienen en el juego: escala de importación × escala del prefab `Visual_*` × escala en la escena.

## Cómo abrir la revisión

| Archivo | Qué hay |
|---|---|
| `revision_PJ01_animaciones.blend` | Se abre en la colección **REV_PJ01**: PJ01 con todos sus clips (nuevos `G_*`/`K_*`, viejos `Loco_*`/`Mixamo_*`, NPC `N_*`, robots `R_E*`), piso de ajedrez de 25 cm y escaleras de 18×28 cm (`_EscSubir`, `_EscBajar`). Para ver un clip: Dope Sheet → Action Editor → elige la acción. |
| `revision_personajes.blend` | 44 personajes, cada uno en `GRUPO_<grupo>/REV_<código>`, junto a la figura de 1.80 m (con regla cada 10 cm) y una copia de PJ01. Están a la altura que tienen **en el juego**. Las líneas rojas marcan 1.60, 1.80 y 1.90 m. |
| `revision_vehiculos.blend` | 50 vehículos, cada uno en `REVISION_VEHICULOS/REV_<código>`, junto a la figura de 1.80 m. Los `*_viejo` son los FBX de una sola malla que todavía se usan en algunas escenas. |
| `capturas/` | `P00_tallas_*` (alturas), `P01_deformacion_*` (rig en 4 poses), `A*`/`B*`/`C*`/`D*` (clips de PJ01), `EN_*` (clips de NPC), `ER_*` (clips de robots), `V_*` (vehículos en 6 vistas). |
| `_metricas_pj01.json`, `_medidas.json`, `_alturas_juego.json`, `_rigs_unity.json`, `_vehiculos.json`, `_escaleras_pen.json`, `_postura_pj01.json` | Los datos crudos de cada medición. |

**Cómo se midió**
- **m/s:** desplazamiento de la cadera de un ciclo dividido entre su duración, a 30 fps.
- **Loop:** diferencia de rotación de cada hueso entre el primer y el último frame (0° = cierra perfecto).
- **Patinaje:** cuánto se arrastra el pie (en horizontal) mientras está apoyado.
- **Piso:** el punto más bajo de la malla (negativo = atraviesa el piso).
- **Altura de los personajes:** se midió en T-pose; parado y animado mide entre 1 y 2 % menos.

---

## RESUMEN — lo más grave

1. **La carrera nueva de PJ01 se ve rara, y es la que más se ve en el juego.**
   - Con WASD el jugador va a 4.5 m/s (`DatosPersonaje.velocidadCaminar = 4.5`), así que casi siempre se ve **PJ01_Trote**; con Shift se ve **PJ01_Sprint** a 7.25 m/s. El caminar de 1.6 m/s casi nunca aparece.
   - Trote, Sprint y los trotes laterales y hacia atrás tienen la **cadera hundida** (92 % de la altura de pie, contra 96 % del Loco_Correr viejo) y las **rodillas muy dobladas** (hasta 151°).
   - El torso **va casi derecho**: 9° de inclinación hacia adelante, contra 17° del viejo.
   - Las **manos van altas, junto al pecho y la barbilla** (pose de "T-rex"). En los trotes laterales los brazos casi no se mueven (7 cm de braceo).
   - Resultado: parece que corre sentado. El **Loco_Correr viejo (Mixamo) se ve más natural** (hojas A04 a A06).
2. **Las alturas no cuadran entre personajes.**
   - **Doc** mide 1.96 m y **Tino** (Inventor) 1.96 m, contra 1.84 m de PJ01. **GuardiaNova** mide **2.46 m**.
   - Los civiles NP07, NP08, NP10, NP13 y NP14 miden entre **1.51 y 1.59 m**.
   - Los robots Nova siguen chicos: E15 mide 2.37 m (objetivo 2.6), E16 entre 2.93 y 3.02 m (objetivo 3.5), E18 1.93 m (objetivo 2.2). La versión a escala real del 19-nov (`personajes/escala_real/`) **nunca se importó a Unity**.
3. **Robots "rotos" (son de configuración, no de la malla).**
   - `Visual_E16_RobotPesadoNova_b` usa **Override_Guardias**, no Override_E16. Por eso el E16_b no tiene cañón, escudo ni giros.
   - La locomoción lateral y hacia atrás de todos los Nova es la de PJ01 vieja, y el **Loco_CaminarDer viejo gira el cuerpo unos 90°** y patina 38 cm.
   - El E16 "corre" con su clip de caminar.
   - Los clips propios E15_Cubrirse, E15_Muerte, E16_Muerte y E18_Muerte **no están en ningún Animator**; solo los usa `ConfigClipsNovaNpc`.
   - Con los brazos arriba, las hombreras de E14, E16 y GuardiaNova atraviesan la cabeza.
4. **Escenas con carros viejos de una sola malla.**
   - `T1356_Autopista` (58 instancias), `T1355_Cerro` (16), `Barrio` (17) y `Ciudad_Prologo` (4) siguen usando `V01a_CarroViejito.prefab`, `V01b_CarroPro.prefab` y `V02.prefab` a `V10.prefab`.
   - Esos prefabs cargan los FBX viejos de 1 m sin piezas, escalados ×4.3 a 5.1 en el prefab: sin ruedas que giren, sin puertas y sin cristales.
   - Las versiones nuevas son `Carro_*.prefab`, que usan `Piezas/`.
5. **Patrullas PA01 y PA02.** Tienen **ventanas tapadas**: la carrocería tiene caras justo detrás del 100 % del vidrio, así que no se ve el interior. Además **no tienen cofre ni motor** separados. Lo mismo pasa con el V22.
6. **Vehículos grandes sobredimensionados.**
   - **V16** microbús: 3.5 m de ancho y 4.1 m de alto.
   - **V17** camión: 4.0 m de ancho y 4.5 m de alto.
   - **V18** tráiler: 4.4 m de ancho y 6.0 m de alto.
   - **V19** ambulancia: 3.2 m de ancho y 3.7 m de alto.
   - Al lado de la figura de 1.80 m se ven gigantes.
7. **El salto nuevo.**
   - `Salto_Aire` es un ciclo de 17 frames que "pedalea" las piernas en el aire.
   - `Salto_Aterrizaje` empieza con las piernas derechas y los brazos al frente, y eso no coincide con la última pose del aire: hay un salto de pose al aterrizar.
   - El final de `Salto_Despegue` sube los dos brazos rectos sobre la cabeza.
   - **No existe "saltar corriendo"**: se usa el mismo salto en el lugar.
8. **"Provocar" no existe en el juego.**
   - El slot `Skull_Burla`/`Nova_Flexing` está reemplazado por `PJ01_Punos_Guardia` en **todos** los overrides.
   - `Jefe_Burla`, `Jefe_Ataque`, `Nova_Punch`, `Nova_Barrido`, `Skull_Idle` y `Skull_Caminar` están importados pero solo los usan scripts de prueba.

**Arreglos técnicos aplicados sin preguntar: ninguno en los assets del juego.**
- Todo lo que encontré o depende de Unity (escala de import o del prefab, overrides, prefabs en escenas), o cambiar el FBX movería cosas ya acomodadas en los prefabs (pivotes de drones, escala de carros).
- Dejo cada arreglo propuesto con su número exacto para aplicarlo cuando Tony lo apruebe.
- Solo en los archivos de revisión (no en los del juego):
  - les quité la animación a nivel de objeto a `E17_PerroRobotNova_a`, `man`, `woman` y `woman2`, porque los sacaba de lugar;
  - desconecté texturas que no existen en disco para que no se vieran en magenta.

---

## 1. PJ01 — animaciones (archivo `revision_PJ01_animaciones.blend`)

**Qué clip usa el juego en cada slot** (`Override_Jugador`):

| Slot del Animator | Clip | Origen |
|---|---|---|
| Loco_Caminar | PJ01_Caminar | nuevo |
| Loco_Correr | PJ01_Sprint | nuevo |
| PJ01_Caminar_SinImplante | PJ01_Trote | nuevo |
| PJ01_Loco_CaminarAtras/Der/Izq | PJ01_Trote_Atras/Derecha/Izquierda | nuevos |
| Loco_CaminarAtras/Der/Izq | PJ01_Loco_CaminarAtras/Der/Izq | **viejos** |
| SaltoInicio/Aire/Aterrizaje | PJ01_Salto_Despegue/Aire/Aterrizaje | nuevos |
| Pistola_ApuntarAtras/Lateral | PJ01_CaminarApuntando_* | nuevos |
| Pistola_Caminar/Correr, Rifle_* | — | Mixamo de siempre |
| Escaleras | PJ01_Escaleras_Subir/Bajar | nuevos |

`AnimacionJugador` mezcla por velocidad: caminar 1.6 → trote 4.5 → sprint 7.25.

### Métricas por clip

| Clip (acción) | Hoja | Frames | m/s | Loop | Patina (I/D) | Piso mín. | Qué se ve mal | Cómo arreglarlo (propuesta) |
|---|---|---|---|---|---|---|---|---|
| **Caminar** nuevo (G_Caminar) | A01 | 1-31 | 1.60 | 0° ok | 2.2 / 1.3 cm | −1.4 cm | Correcto de pies, pero rígido. Brazos casi pegados al cuerpo y sin swing de frente; tronco sin balanceo; pasos largos con piernas muy rectas. | Bajar el paso a ~0.70 m, subir el swing de brazos a 25–30° y agregar 3–4° de giro de pelvis y hombros contrarios. |
| Caminar viejo (Loco_Caminar, Mixamo) | A02 | 1-32 | 1.62 | 0° | 2.0 / 4.0 cm | −1.2 cm | Más vivo y con peso: rebote, braceo natural, puño cerrado. Las rodillas abren un poco ("vaquero") de frente. | **El viejo se ve más natural.** Opción rápida: regresarlo al slot Loco_Caminar. |
| Caminar_SinImplante (viejo, usado por los Skulls) | A03 | 1-32 | 1.62 | 0° | 4.1 / 5.2 cm | −1.2 cm | Bien. Patina un poco. | — |
| **Trote** nuevo (G_Trote, slot de WASD) | A04 | 1-21 | 4.50 | 0° | 2.1 / 2.1 cm | −2.2 cm | Corre "sentado": cadera al 92.5 %, rodilla de hasta 129°, torso a 9° (poco), manos 13 cm bajo el pecho y por delante (pose de T-rex), 9 frames en el aire. | Subir la cadera al ~96 %, inclinar el torso 14–17°, bajar los codos (manos a la altura de la cintura en el swing atrás), más patada de talón atrás y menos rodilla al frente. **O usar Loco_Correr viejo (4.31 m/s).** |
| Correr viejo (Loco_Correr) | A05 | 1-22 | 4.31 | 0° | 2.6 / 2.8 cm | −2.5 cm | **Más natural:** inclinación de 17°, braceo de 46 cm. | Usarlo de referencia o regresarlo. |
| **Sprint** nuevo (G_Sprint) | A06 | 1-19 | 7.25 | 0° | **6.9 / 6.9 cm** | −1.2 cm | Igual que el trote pero más exagerado: cadera al 90 %, rodilla de 151°, manos abiertas frente a la cara y paso tipo zancada. Patina 7 cm. | Mismos ajustes que el trote; además cerrar las manos y corregir el patinaje (fijar el apoyo del pie 1 frame más). |
| Trote atrás (G_TroteAtras) | A07 | 1-21 | 4.00 | 0° | 4.8 / 4.8 cm | −0.3 cm | 4 m/s hacia atrás es demasiado rápido (una persona trota hacia atrás a 2–2.5 m/s). Agachado y con los brazos inmóviles arriba. | Bajar el juego a ~2.5 m/s hacia atrás y rehacer el clip a esa velocidad, más erguido. |
| Trote derecha/izquierda (G_TroteDer/Izq) | A08–A09 | 1-21 | 4.00 | 0° | **7.0 / 7.2 cm** | −0.6 cm | Pasos cruzados agachados, brazos congelados (7 cm de braceo). Patina 7 cm. Parece que brinca. | Strafe con pasos laterales (sin cruzar) a ~3 m/s, con hombros sueltos. |
| Caminar atrás viejo (Loco_CaminarAtras, en juego) | A10 | 1-37 | 1.03 | 0° | **41 / 83 cm** | −0.4 cm | El pie derecho se arrastra hasta 83 cm: el apoyo no está fijo (retarget viejo). | Reemplazarlo por un caminar atrás nuevo o volver a retargetear el Mixamo con apoyos fijos. |
| Caminar derecha viejo (Loco_CaminarDer, en juego) | A11 | 1-32 | 1.57 | 0° | **38 / 35 cm** | −0.1 cm | **El cuerpo gira unos 90°** mientras camina (se ve de espaldas desde el lado) y patina 35–38 cm. | Reemplazarlo. Lo usan también todos los NPC, los Nova y los Skulls. |
| Caminar izquierda viejo (Loco_CaminarIzq) | A12 | 1-32 | 1.57 | 0° | 4.1 / 1.7 cm | −0.3 cm | Aceptable. | — |
| Caminar apuntando adelante, atrás, der., izq. | A13–A16 | 27 / 23 | 2.00 | 0° | 3 / 2; **atrás 8.6**; 1.6 cm | −1 cm | Pistola a la altura del pecho, no de los ojos. En CaminarApuntando_Atras patina 8.6 cm el pie derecho. Brazos sin movimiento (correcto para apuntar). | Subir las manos unos 15 cm (línea de mira) y fijar el apoyo derecho en el clip de atrás. |
| Pistola_Caminar / Pistola_Correr (Mixamo) | A17–A18 | 25 / 23 | **2.43 / 2.87** | 0° | 3 / 2 cm | −3 cm | El clip de correr con pistola avanza solo **2.87 m/s**; si el jugador corre a 4.5–7.5 con pistola, los pies patinan mucho en el juego. Cadera al 86 %. | Escalar el clip en Unity (speed 1.6) o hacer un trote con pistola a 4.5. |
| **Escaleras subir** (G_EscUp) | B01 | 1-37 | 0.47 hor. + 0.30 vert. | 0° | 2.5 / 2.1 cm | **0 penetración** con peldaño de 18×28 cm | Coincide perfecto con escalones de 18 cm de alto y 28 cm de huella. Se ve tieso: torso derecho, brazos muertos, sin inclinarse al subir. | Inclinar el torso 8–10° y agregar algo de braceo. **Revisar en Unity que las escaleras del mapa midan 18×28 cm**; si no, el pie flota o se hunde. |
| **Escaleras bajar** (G_EscDown) | B02 | 1-35 | 0.49 hor. − 0.32 vert. | 0° | 0 / 1.7 cm | 0 penetración | Bien colocado; tieso igual que subir. | Igual que subir. |
| **Salto despegue** (K_SaltoDespegue) | C01 | 1-16 | en el lugar | — | 2 cm | −1.7 cm | Buena anticipación (agacharse con los brazos atrás), pero en los frames 13–14 los **dos brazos suben rectos sobre la cabeza** (como un tiro de básquet). Se ve exagerado. | Brazos a la altura de los hombros al despegar. |
| **Salto aire** (K_SaltoAire, loop) | C02 | 1-17 | — | 0° | — | — | Las piernas **pedalean** en un ciclo de 0.56 s. En un salto de 0.6–0.8 s se ve como correr en el aire. | Pose casi fija (una rodilla arriba y la otra extendida) con un movimiento leve, no un ciclo. |
| **Salto aterrizaje** (K_SaltoAterrizaje) | C03 | 1-23 | — | — | 2.7 / 1.4 cm | −0.3 cm | El frame 1 está de pie con las piernas rectas, pero el Aire termina con las piernas abiertas: **brinco de pose** al tocar el piso. La flexión llega tarde. | Que el frame 1 sea la pose del Aire y que la flexión empiece en el frame 2–3. |
| Aterrizaje fuerte (K_AterrizajeFuerte) | C04 | 1-45 | — | — | 5.9 / 8.3 cm | −3.1 cm | Bien (sentadilla profunda), pero los pies se mueven 6–8 cm. | Fijar los pies. |
| Salto viejo (Loco_SaltoInicio/Aire/Aterrizaje) | C05–C07 | 12 / 12 / 16 | — | — | 9.8 / 8.8 cm | −1.4 cm | **Más natural** en el despegue (braceo al frente), pero sus pies patinan unos 9 cm. | — |
| Saltar corriendo | — | — | — | — | — | — | **NO EXISTE.** El juego usa el salto en el lugar también al correr. | Hace falta un `Salto_Correr` (despegue con un pie) y un aterrizaje en carrera. Referencia: Parkour_SaltarBarda (C08) se ve bien a 4.2 m/s. |
| Golpe 1 y 2, Daño, Muerte, Cobertura (K_*) | D04–D12 | 15–91 | — | — | 0–9 cm | Muerte frente **−11.8 cm**, Golpe2 −6.2 cm | Golpes con la guardia muy pegada a la cara. Muerte frente: los brazos y la cara atraviesan el piso 12 cm. Daño: sin problema. | Subir la cadera 10 cm en el final de Muerte_Frente. |

**Comparación pedida (viejos contra nuevos).**
- **Caminar:** el nuevo tiene mejor contacto de pies, pero el **viejo Loco_Caminar se ve más natural** (más peso y braceo).
- **Correr:** el **viejo Loco_Correr es claramente más natural** que el Trote y el Sprint nuevos.
- **Salto:** el despegue viejo se ve mejor; el aterrizaje nuevo tiene mejor peso, pero hay que arreglar la transición.

**Faltan para el juego (PJ01):**
- salto corriendo y aterrizaje en carrera;
- caminar con pistola a 4.5 m/s;
- desplazarse en cobertura (lateral);
- empujar o abrir a golpe;
- sentarse y levantarse;
- caminar lento real si se quiere usar 1.6 m/s.

---

## 2. Personajes (archivo `revision_personajes.blend`, colecciones `REV_<código>`)

**Cómo leer la tabla**
- **Altura en juego:** FBX × globalScale × escala del prefab × escala en la escena.
- **Rig:**
  - Todos son Humanoid en Unity, con todos los huesos obligatorios mapeados, menos el perro E17, que es Generic (correcto).
  - En los rigs AccuRIG de 58 huesos (PJ01, Policía, Doc, Hacker, Inventor, Mecánico, RobotHombre, SkullHombre, man, woman y woman2) el **Neck no está mapeado**. Es opcional en Unity, pero la cabeza y el cuello quedan más rígidos.
  - Ningún personaje tiene vértices sin peso ni normales invertidas (≤0.04 %).
- **Deformación:** hojas `P01_deformacion_<código>` en 4 poses (brazos abajo, brazos arriba con codos, sentadilla y giro cruzando el brazo). Hombros, codos y rodillas deforman bien en todos; solo se marcan los problemas.
- **Animaciones:** cada uno usa los clips de su override (ver la sección 3). No tienen clips propios, salvo PJ01, los Nova, el perro y los NPC.

Prioridad para el demo: PJ01, policía, SKULLS, Nova, Doc y NPC.

| Código | Altura en juego | Objetivo | Estado | Problemas | Arreglo propuesto | Técnico ya corregido | Abrir |
|---|---|---|---|---|---|---|---|
| **PJ01** | **1.84 m** | 1.60–1.90 | BIEN (modelo) / animaciones mal | Rig bien. Carrera nueva rara (ver la sección 1). | Ver la sección 1. | — | REV_PJ01 / revision_PJ01_animaciones |
| **Policía (C01)** | 1.86 | 1.60–1.90 | BIEN | `Policia.controller` propio con 16 clips de PJ01 (viejos). Le faltan cobertura, muerte de espaldas, daños, escaleras y arrestar. | Pasarlo a un override (Override_Guardias) para que herede todo. | — | REV_Policia |
| **SkullHombre** | 1.73 | 1.60–1.90 | BIEN | Provocar usa Punos_Guardia (placeholder). | Conectar Skull_Burla o Jefe_Burla. | — | REV_SkullHombre |
| man (PandilleroArmado, Skulls) | 1.76 | 1.60–1.90 | BIEN | El FBX trae animación a nivel de objeto y 4 clips embebidos. | Quitar los clips embebidos del import. | — | REV_man |
| **E19 Pandillero Skulls a_2 / b** | **1.60 / 1.58** | 1.60–1.90 | ARREGLO CHICO | 24–26 cm más bajos que PJ01; se ven chicos. | globalScale ×1.10 (0.897→0.986 y 0.890→0.979) → 1.75 m. | — | REV_E19_* |
| E20 Matón Skulls a / b | 1.70 / 1.68 | 1.60–1.90 | ARREGLO CHICO | Un matón debería ser ≥ PJ01. | globalScale ×1.07 → 1.80 m. | — | REV_E20_* |
| E10 Pandillero Snakes | 1.62 | 1.60–1.90 | ARREGLO CHICO | Bajo. | globalScale ×1.08 → 1.75 m. | — | REV_E10_PandilleroSnakes |
| E11 Jefe Snakes | 1.78 | 1.60–1.90 | BIEN | Gabardina: en la sentadilla la falda se deforma mucho. Override_Jefe usa clips de PJ01; los Jefe_* no se usan. | Conectar los Jefe_*. | — | REV_E11_JefeSnakes |
| **GuardiaNova** | **2.46** | ~1.95 | ARREGLO CHICO | 62 cm más alto que PJ01 (prefab ×1.654). Las hombreras atraviesan la cabeza con los brazos arriba. | Escala del modelo en `Visual_GuardiaNova` de 1.654 → 1.31. | — | REV_GuardiaNova |
| E12 Guardia Pesado Nova | 1.77 | (pesado ≥ 1.9) | ARREGLO CHICO | Más bajo que PJ01 siendo "pesado". El escudo atraviesa la cabeza con los brazos arriba. | globalScale ×1.12 → 1.98 m. | — | REV_E12_GuardiaPesadoNova |
| E14 Jefe Final Nova | 1.89 | (jefe ≥ 2.2) | ARREGLO CHICO | Para jefe final es casi del tamaño de PJ01. Las hombreras atraviesan la cabeza. | globalScale ×1.2 → 2.27 m (a decidir). | — | REV_E14_JefeFinalNova |
| **E15 Centinela a / b** | **2.37 / 2.38** | **2.6** | ARREGLO CHICO | Escala vieja: no se importó la versión del 19-nov. No tiene locomoción lateral propia. E15_Cubrirse y E15_Muerte no están en el Animator. | Importar `personajes/escala_real/E15_CentinelaNova.fbx` o globalScale ×1.10. Meter E15_Cubrirse y E15_Muerte en Override_E15. | — | REV_E15_* |
| **E16 Robot Pesado a / b** | **2.93 / 3.02** | **3.5** | **ARREGLO GRANDE** (config) | Chico (−15 %). **E16_b usa Override_Guardias** (sin cañón, escudo ni giros). Corre con el clip de caminar. Las hombreras atraviesan la cabeza. E16_Muerte no se usa. | Importar escala_real o globalScale ×1.18. Cambiar `Visual_E16_RobotPesadoNova_b` a Override_E16. Meter E16_Muerte. | — | REV_E16_* |
| **E18 Élite** | **1.93** | **2.2** | ARREGLO CHICO | −12 %. E18_Muerte y E18_Caida_Levantarse no están en el Animator. | escala_real o globalScale ×1.14. | — | REV_E18_EliteNova_a |
| E17 Perro (a / esc) | largo **1.30**, alto 1.04 | largo 1.2 | ARREGLO CHICO | 8 % largo. El `_a` trae animación a nivel de objeto (mueve y escala el objeto raíz) y no se usa; el juego usa `_esc`. | globalScale de `_esc` ×0.92. | — | REV_E17_* |
| **Doc (Doctor)** | **1.96** | 1.60–1.90 | ARREGLO CHICO | **12 cm más alto que PJ01** (prefab ×1.677). Es lo que vio Tony. | Escala del modelo en `Visual_Doctor` de 1.677 → 1.52 (1.78 m). | — | REV_Doctor |
| **Tino (Inventor)** | **1.96** | ~1.70 | ARREGLO CHICO | En Ciudad_Prologo la instancia tiene escala 1.67 (encima de la del prefab, 1.495). | Escala en la escena 1.67 → 1.45 (1.70 m). Verificar en Unity. | — | REV_Inventor |
| Hacker | 1.80 | 1.60–1.90 | BIEN | Su idle es NPC_Hacker_Tecleando. | — | — | REV_Hacker |
| El Canepa (Mecánico) | 1.80 | 1.60–1.90 | BIEN | `Mecanico.controller` propio con pocos clips (14). | Pasarlo a un override. | — | REV_Mecanico |
| AL10 / AL11 | 1.80 / 1.80 | 1.60–1.90 | BIEN | Prefabs `Visual_AL10` y `Visual_AL11` no usados en escenas. | — | — | REV_AL10/11 |
| R01 Mesera Robot | 1.65 | 1.60–1.90 | BIEN | La charola atraviesa el brazo con los brazos arriba (pieza rígida). Prefab no usado en escenas. | — | — | REV_MeseraRobot |
| woman / woman2 | 1.80 / 1.73 | 1.60–1.90 (mujer ~1.65) | ARREGLO CHICO | woman mide lo mismo que PJ01. Los FBX traen animación de objeto y clips embebidos. | woman: prefab 1.277 → 1.17. | — | REV_woman/woman2 |
| RobotHombre | 1.80 | 1.60–1.90 | BIEN | — | — | — | REV_RobotHombre |
| **NP07 Pescador a / b** | **1.56 / 1.56** | 1.60–1.90 | ARREGLO CHICO | Chico. | globalScale ×1.12 → 1.75. | — | REV_NP07_* |
| **NP08 Turista a / b** | **1.57 / 1.59** | 1.60–1.90 | ARREGLO CHICO | Chico. | ×1.11 / ×1.10. | — | REV_NP08_* |
| NP09 Corredora a / b | 1.62 / 1.63 | 1.60–1.90 | BIEN | — | (opcional ×1.02 → 1.65) | — | REV_NP09_* |
| **NP10 Elotero** | **1.51** | 1.60–1.90 | ARREGLO CHICO | Muy chico. | ×1.16. | — | REV_NP10_Elotero_a |
| NP11 Estibador a / b | 1.61 / 1.67 | 1.60–1.90 | ARREGLO CHICO (a) | a, bajo para estibador. | a ×1.09. | — | REV_NP11_* |
| NP12 Abuela | 1.42 | (~1.52) | ARREGLO CHICO | Muy chica. La falda se deforma mucho en la sentadilla (normal en faldas). | ×1.07. | — | REV_NP12_Abuela_a |
| **NP13 Repartidor a / b** | **1.51 / 1.51** | 1.60–1.90 | ARREGLO CHICO | Muy chicos. | ×1.16. | — | REV_NP13_* |
| NP14 Oficinista a / b | 1.59 / 1.60 | 1.60–1.90 | ARREGLO CHICO (a) | a, bajo. | a ×1.10; b ×1.03. | — | REV_NP14_* |
| NP01–NP06 | — | — | **NO ESTÁN EN UNITY** | Solo existen los zips en `modelos-nuevos/nuevos-17nov`. | Importarlos. | — | — |
| AL12 | — | — | **NO ENCONTRADO** | No está en ninguna carpeta. | — | — | — |

*"globalScale" es el Scale Factor del import del FBX en Unity: multiplica el valor actual por el factor.*

---

## 3. Animaciones que usa cada grupo y las que faltan

| Grupo (override) | Locomoción que usa | Faltan para el juego |
|---|---|---|
| Jugador | Ver la sección 1. | Salto corriendo, desplazarse en cobertura, sentarse y empujar. |
| Policía (`Policia.controller`) | PJ01_Caminar_SinImplante, PJ01_Correr_SinImplante, Pistola_* | Cobertura, muerte de espaldas, daño de espalda y fuerte, escaleras, arrestar o esposar, hablar por radio. |
| SKULLS / SNAKES / Jefe | Caminar_SinImplante y Correr_SinImplante viejos; lateral y atrás viejos (CaminarDer gira el cuerpo). | **Provocar** (el slot existe pero tiene Punos_Guardia), **empujar**, intimidar, recargarse en pared o sentarse, huir. Los Jefe_* existen pero no están conectados. |
| Nova E15 / E16 / E18 / Guardias | Clips propios para caminar y disparar; lateral y atrás de PJ01 viejo. | Caminar lateral y atrás propios; cubrirse (E15_Cubrirse existe, sin conectar); muerte propia (existe, sin conectar); provocar (Nova_Flexing tiene Punos_Guardia); E16 correr. |
| NPC (Hombre, Mujer, Mayor, Celular) | NPC_Caminar_* y NPC_Huir_Corriendo (generador nuevo: mismo estilo "sentado" que el trote). | Sentarse (NPC_Sentado_Banca solo por script), platicar (por script), reacción a disparo propia (usa NPC_Balazo), gritar o señalar. |
| Perro E17 | E17_Caminar, Correr, Ladrar, Morder, Muerte y SaltoAtaque. | Olfatear o idle, reacción a daño. |

Hojas de clips de NPC: `EN_*`. Hojas de los clips de robots, mostrados en PJ01 porque son Humanoid: `ER_*`.
- El N_Caminar_Hombre y el N_Caminar_Mujer son iguales al caminar nuevo de PJ01 (brazos rígidos).
- N_Huir tiene la misma postura "sentada" del trote.
- R_E15Caminar y R_E15Correr van agachados con los brazos al pecho (puede ser intencional para un centinela).
- R_E15Muerte (−15.7 cm), R_E16Muerte (−19.2 cm) y R_E18Aterrizaje (−13.9 cm) **atraviesan el piso**.

---

## 4. Vehículos (archivo `revision_vehiculos.blend`, colecciones `REV_<código>`, hojas `V_<código>.png`)

**Medidas y revisiones**
- **Medidas:** en metros, con espejos y torreta. **Ancho:** un auto real mide unos 1.8 m de carrocería y 2.0–2.1 m con espejos.
- **Ruedas:** en todos están separadas, con el pivote centrado en el eje (0–1 cm), todas tocan el piso al mismo nivel (desnivel ≤0.3 cm) y son redondas.
- **Puertas:** en todos los pivotes están en la bisagra, a 0 cm.
- **Choque al abrir:** caras que se cruzan con la carrocería con la puerta abierta a 65° (≈0–10 es bien).
- **Vidrio tapado:** % del cristal que tiene una cara de la carrocería pegada detrás (100 % = ventana sin hueco).
- **Normales:** ninguno tiene invertidas (≤0.4 %).

Orden: patrullas, tráfico y motos, drones, viejos.

| Código | L × A × H (m) | Estado | Problemas | Arreglo propuesto | Técnico ya corregido | Abrir |
|---|---|---|---|---|---|---|
| **PA01** patrulla sedán | 4.80 × 2.39 × 2.22 | **ARREGLO CHICO** | **Vidrio tapado al 100 %** (no se ve el interior). **Sin cofre ni motor.** Ancho de 2.39. | Borrar las caras de la carrocería detrás de los 5 cristales; separar el cofre con pivote en la bisagra y agregar motor. | — | REV_PA01 |
| **PA02** patrulla camioneta | 5.00 × 2.62 × 2.24 | **ARREGLO CHICO** | Vidrio tapado al 100 %; sin cofre ni motor; **ancho de 2.62** (demasiado). | Igual que PA01; revisar el ancho (los espejos sobresalen mucho). | — | REV_PA02 |
| V11 camioneta policía | 4.40 × 2.16 × 2.32 | ARREGLO CHICO | Corta para pickup (4.4) y alta; ruedas enormes (estilo caricatura); 2 piezas flotando. | Aceptable como estilo; revisar las piezas flotando. | — | REV_V11 |
| V05 patrulla | 4.43 × 2.20 × 2.11 | ARREGLO CHICO | **Las puertas chocan al abrir** (62 y 88 caras); sin interior. | Recortar el hueco de las puertas en la carrocería; agregar interior. | — | REV_V05 |
| V06 patrulla | 4.50 × 2.54 × 2.30 | ARREGLO CHICO | Ancho y alto (2.54 × 2.30); chocan un poco las puertas y el cofre (12–16). | Es proporción de caricatura: no se arregla con escala uniforme. Si molesta, ×0.85. | — | REV_V06 |
| V01a viejito | 4.41 × 2.46 × 2.04 | ARREGLO CHICO | Puertas chocan (37 y 41); sin interior; ancho. | Recortar los huecos de las puertas; interior. | — | REV_V01a |
| V01b pro | 4.41 × 2.30 × 1.60 | ARREGLO CHICO | Puertas chocan (57 y 58); sin interior. | Igual. | — | REV_V01b |
| V02 | 4.33 × 2.32 × 2.26 | ARREGLO CHICO | Puertas chocan (57 y 45); alto. | Recortar los huecos de las puertas. | — | REV_V02 |
| V03 pickup | 4.50 × 2.53 × 2.10 | BIEN | Ancho (2.53, por la caja); puertas y cofre sin choque. | — | — | REV_V03 |
| V04 van | 4.50 × 2.30 × 2.50 | ARREGLO CHICO | Es van: alto bien. 24 % del vidrio tapado; puertas chocan un poco (24). | Abrir los huecos de las ventanas. | — | REV_V04 |
| V09 taxi | 4.41 × 2.17 × 1.98 | ARREGLO CHICO | Puertas chocan (43 y 53). | Recortar los huecos. | — | REV_V09 |
| V10 pickup | 4.41 × 2.18 × 1.86 | BIEN | 2 piezas flotando en la carrocería. | Revisar. | — | REV_V10 |
| V12 deportivo | 4.40 × 2.28 × 1.53 | BIEN | — | — | — | REV_V12 |
| V13 sedán | 4.70 × 2.44 × 1.81 | BIEN | Ancho. | — | — | REV_V13 |
| V14 compacto | 3.80 × 2.31 × 1.96 | BIEN (estilo) | Muy "chato" y ancho (caricatura). | — | — | REV_V14 |
| V15 lujo | 5.10 × 2.21 × 1.45 | BIEN | Puerta izquierda choca un poco (18). | — | — | REV_V15 |
| **V16 microbús** | 7.00 × **3.54 × 4.05** | ARREGLO CHICO | **Gigante**: 40 % sobrado de ancho y alto. | Escala uniforme ×0.70 → 4.9 × 2.5 × 2.8. | — | REV_V16 |
| **V17 camión** | 8.00 × **4.02 × 4.45** | ARREGLO CHICO | Sobredimensionado. | ×0.80 → 6.4 × 3.2 × 3.6. | — | REV_V17 |
| **V18 tráiler** | 16.0 × **4.37 × 5.99** | ARREGLO CHICO | Sobredimensionado. | ×0.70 → 11.2 × 3.1 × 4.2. | — | REV_V18 |
| **V19 ambulancia** | 6.00 × **3.22 × 3.66** | ARREGLO CHICO | Sobredimensionada. | ×0.78 → 4.7 × 2.5 × 2.9. | — | REV_V19 |
| V20 tuneado | 4.50 × 2.33 × 1.60 | BIEN | — | — | — | REV_V20 |
| V21 tuneado | 4.20 × 2.55 × 1.95 | BIEN (estilo) | Ancho por las salpicaderas. | — | — | REV_V21 |
| **V22** superdeportivo | 4.60 × 2.54 × 1.38 | ARREGLO CHICO | **Vidrio tapado al 100 %**; sin cofre ni motor. | Igual que PA01. | — | REV_V22 |
| V23 superdeportivo | 4.60 × 2.12 × 1.24 | ARREGLO CHICO | **Las puertas chocan al abrir** (61 y 60). | Recortar los huecos o abrir tipo tijera. | — | REV_V23 |
| V24 superdeportivo | 4.70 × 2.25 × 1.45 | BIEN | — | — | — | REV_V24 |
| V07 moto reparto | 2.00 × 0.87 × 1.35 | BIEN | — | — | — | REV_V07 |
| V08 moto policía | 2.00 × 0.88 × 1.30 | BIEN | — | — | — | REV_V08 |
| MT01 deportiva | 2.05 × 0.81 × 1.20 | BIEN | — | — | — | REV_MT01 |
| MT02 chopper | 2.40 × 1.01 × 1.21 | BIEN | — | — | — | REV_MT02 |
| C19 moto | 2.11 × 0.73 × 1.13 | BIEN | — | — | — | REV_C19 |
| DR01 dron | 0.62 × 0.80 × 0.33 | ARREGLO CHICO | Pivote de los rotores a 2.7 cm del centro (tambaleo al girar). Raíz con el pivote abajo (16 cm). | Centrar los pivotes de los rotores en el eje. | — | REV_DR01 |
| DR02 a / b | ~1.6 × 1.80 × 0.7 | ARREGLO CHICO | Rotores a 6.1–6.2 cm del eje. | Centrar los pivotes de los rotores. | — | REV_DR02_* |
| DR03 a | 2.40 × 2.08 × 1.17 | ARREGLO CHICO | Rotores a 6.2 cm. | Igual. | — | REV_DR03_a |
| DR04 a | 0.73 × 0.70 × 0.58 | ARREGLO CHICO | Rotor a 4.9 cm. | Igual. | — | REV_DR04_a |
| E13 dron | 1.02 × 1.10 × 0.42 | ARREGLO CHICO | Rotores a 3.8 cm. | Igual. | — | REV_E13 |
| NV01 nave | 12.0 × 12.0 × 4.6 | ARREGLO CHICO | Rotores a 7.5 cm; raíz a 2.3 m del centro. | Centrar los rotores. | — | REV_NV01 |
| NV03 a / b | 8.2 / 6.4 × 9.0 × 3.4 / 3.0 | BIEN | Rotores a 1.4–3.1 cm (aceptable). | — | — | REV_NV03_* |
| **V01a / V01b / V02–V10 \_viejo** | FBX de ~1 m, escalados ×4.3 a 5.1 en el prefab | **REHACER (reemplazar)** | Malla única: sin ruedas, puertas ni cristales. **Siguen en escenas:** T1356_Autopista (58), T1355_Cerro (16), Barrio (17), Ciudad_Prologo (4). | En esas escenas cambiar `Vxx.prefab` y `V01*_Carro*.prefab` por `Carro_Vxx.prefab`. | — | REV_*_viejo |

**Nota de los pivotes de los rotores:** la medida es la distancia entre el pivote y el centro de la caja de la hélice. Si la hélice es asimétrica, puede no ser un error real. Hay que confirmarlo girándola en Unity antes de tocarlo.

---

## 5. Sugerencia de orden para arreglar (después de revisar con Tony)

1. **Unity (minutos y sin riesgo):**
   - escalas de Doc, Tino, GuardiaNova y los NPC (tablas de arriba);
   - `Visual_E16_RobotPesadoNova_b` → Override_E16;
   - cambiar los carros viejos en las escenas por `Carro_*`.
2. **Decidir el caminar y el correr de PJ01:**
   - **(a)** regresar Loco_Caminar y Loco_Correr viejos y bajar la velocidad a la de esos clips; o
   - **(b)** rehacer Trote, Sprint y laterales con las correcciones de la tabla (cadera, inclinación, brazos).
   - Y decidir si WASD debe caminar (1.6–2.0) en vez de trotar (4.5).
3. **Salto:** arreglar el pedaleo del Aire y la transición al Aterrizaje; crear Salto_Correr.
4. **Robots:**
   - importar escala_real;
   - conectar los clips propios (Cubrirse y Muerte);
   - lateral y atrás propios.
5. **Vehículos:**
   - abrir las ventanas de PA01, PA02 y V22;
   - recortar los huecos de las puertas que chocan;
   - escalar V16 a V19;
   - pivotes de los rotores.
