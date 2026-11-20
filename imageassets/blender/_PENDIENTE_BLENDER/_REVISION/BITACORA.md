# Bitácora de tareas Blender — CyberCity (9-nov)

## 01 · Puertas V01A, V01B y V05 — HECHO
**Qué hice**
- Importé las carrocerías de Tripo (V01CARROZA = V01A, V01BCARROZA, V05CARROZA) y las 6 puertas actuales.
- Medí el hueco de la puerta delantera de cada carrocería (rayos desde el costado, cuadrícula de 5 mm): borde delantero, poste B, estribo y techo.
- Ajusté cada puerta a su hueco (escala por ejes, 3 mm de traslape), al ras de la lámina (0–5 mm). V01B_Derecha: el reflejo anterior estaba al revés; ahora va girado 180° para que su frente quede al frente del carro.
- Oriente: frente del carro hacia +Z de Unity (−Y en Blender). Pivote de cada puerta en la bisagra (borde delantero, a media altura del panel). Transforms aplicados, metros, Y arriba, texturas embebidas.
- La puerta izquierda abre girando negativo en Y de Unity hacia afuera; la derecha, positivo (en el .blend de revisión están animadas abriendo 65°).

**Archivos exportados** (`imageassets\blender\puertas\`, reemplazando los anteriores)
- V01A_Puerta_Izquierda.fbx, V01A_Puerta_Derecha.fbx, V01B_Puerta_Izquierda.fbx, V01B_Puerta_Derecha.fbx, V05_Puerta_Izquierda.fbx, V05_Puerta_Derecha.fbx
- Extra: V01A_Ensamble.fbx, V01B_Ensamble.fbx, V05_Ensamble.fbx (carrocería + 2 puertas ya en su lugar, mismo frente +Z) para copiar posiciones.
- Posición de cada bisagra respecto al origen de su carrocería (escala Tripo, carro de 0.98 m):
  - V01A_Izquierda: pivote Unity (relativo al carro) = (-0.2135, 0.1089, 0.134)
  - V01A_Derecha: pivote Unity (relativo al carro) = (0.2184, 0.1089, 0.134)
  - V01B_Izquierda: pivote Unity (relativo al carro) = (-0.175, 0.1016, 0.114)
  - V01B_Derecha: pivote Unity (relativo al carro) = (0.175, 0.1016, 0.114)
  - V05_Izquierda: pivote Unity (relativo al carro) = (-0.1934, 0.1012, 0.149)
  - V05_Derecha: pivote Unity (relativo al carro) = (0.1935, 0.1012, 0.149)
- Fuentes descomprimidas en `imageassets\blender\puertas\_fuentes\`.

**Bien**: las 6 puertas llenan su hueco, el marco de la ventana sigue al poste A, quedan al ras y abren sin chocar.
**Pendiente / ojo**: la carrocería y las puertas están a escala Tripo (0.98 m); en Unity deben usar el mismo factor de escala. Si en Unity la carrocería sigue con el frente en −X (como sale de Tripo), usar la de `<Código>_Ensamble.fbx` o girarla −90° en Y. La llanta RU01 viene a otra escala (1 m de alto); solo fue referencia.
**Revisión**: `_REVISION\01_Puertas.blend` (cámara Cam_Revision, cuadros 1–80: puertas abren y cierran). Vistas: 01_vista_frente.png, 01_vista_lado.png, 01_vista_puertas_abiertas.png.

## 02 · Clips PJ01: carro y moto — HECHO
**Qué hice**
- Carro_Entrar, Carro_Salir y Carro_Manejar: pasados de Mixamo a PJ01 (retarget por rotaciones mundiales; probé la herramienta contra Loco_Caminar ya aprobado: 0.0 mm de diferencia).
- Encadené los tres para que la posición sentado sea la misma: Entrar termina y Salir empieza exactamente en la cadera de Manejar (diferencia 0 mm). Entrar va girado −100° y Salir −12° para que entre/salga por la puerta del conductor (lado izquierdo, +X en Blender / −X en Unity) mirando al frente del carro.
- Salir: la cadera sube suave del asiento (cuadros 45–66) para que los pies no se metan en el piso (antes bajaban 11 cm, ahora máx. 5 cm al apoyar).
- Moto (no había clips de Mixamo): armé la pose con IK propio de 2 huesos (el IK de Blender no sirve en este rig porque los huesos no terminan donde empieza el hijo). Moto C19 a escala ×1.8 (asiento a 0.81 m): cadera sobre el asiento, torso inclinado 50°, manos en los puños, rodillas abiertas, pies planos en los estribos.
  - Moto_Manejar: bucle de 60 cuadros con dirección ±3° y rebote de 6 mm.
  - Moto_Subir: 54 cuadros, sube por la izquierda: agarra el manubrio, pasa la pierna derecha por encima del asiento (delante de la caja trasera) y se sienta.
  - Moto_Bajar: el mismo movimiento al revés.

**Archivos exportados** (`imageassets\blender\pj01_clips\`)
- PJ01_Carro_Entrar.fbx (165 cuadros), PJ01_Carro_Manejar.fbx (151), PJ01_Carro_Salir.fbx (175)
- PJ01_Moto_Subir.fbx (54), PJ01_Moto_Manejar.fbx (60, bucle), PJ01_Moto_Bajar.fbx (54)

**Bien**: sentado sin flotar; manos en volante/puños; pies en el piso del carro y en los estribos; continuidad Entrar→Manejar→Salir y Subir→Manejar→Bajar.
**Ojo / no perfecto**:
- El carro de referencia lo escalé ×4.3 (V01A Tripo de 0.98 m → 4.2 m de largo). En Unity, PJ01 sentado: origen del personaje a (−0.4, 0, −0.265) m respecto al origen del carro (frente +Z), con el carro a ×4.3. Si el carro en Unity tiene otra escala, mover el punto de asiento.
- Moto C19 a ×1.8 (1.77 m sin llantas). PJ01 en moto: origen del personaje en el origen de la moto. Si la moto en Unity tiene otra escala, el asiento cambia.
- Muslos: rozan 5–8 cm el costado del tanque de la moto (abrazando la moto); en juego casi no se nota.
- Los clips de moto son hechos a mano con IK (no existen en Mixamo); si se consigue un clip de moto de Mixamo/otra fuente, se puede reemplazar.
**Revisión**: `_REVISION\02_Carro_Moto.blend` (cámara Cam_Revision; moto: Subir 1–54, Manejar 55–114, Bajar 115–166; carro: Entrar 1–165, Manejar 166–316, Salir 317–489). Vistas: 02_vista_frente.png, 02_vista_lado.png, 02_vista_subir_moto.png.

## 03 · Salto de PJ01 — HECHO
**Qué hice**
- Pasé Loco_SaltoInicio, Loco_SaltoAire, Loco_SaltoAterrizaje y Loco_Caida de Mixamo a PJ01 y los rehíce para juego:
  - Todos **en el lugar** (sin subir/avanzar la raíz): el salto lo hace el CharacterController. Cadera en el aire a 0.95 m (3 cm sobre la de pie).
  - **SaltoInicio**: 12 cuadros (0.4 s; antes 26). Sentadilla reducida al 70 % (baja 20 cm, antes 38) con los pies pegados al piso; los 3 últimos cuadros ya en el aire con las piernas estiradas.
  - **SaltoAire**: bucle de 22 cuadros, encogido reducido al 45 % (antes rodillas al pecho), cabeza al frente; últimos 5 cuadros mezclados con el primero para que el bucle no brinque.
  - **SaltoAterrizaje**: 16 cuadros (0.53 s; antes 33). Empieza 2 cm sobre el piso, contacto en el cuadro 2, amortigua 16 cm y se para. Pies sin atravesar el piso.
  - **Caida**: el clip original era caer de 2 m + rodar + levantarse (129 cuadros). Tomé solo la parte en el aire y la hice bucle de 22 cuadros en el lugar.

**Archivos exportados** (`imageassets\blender\pj01_clips\`)
- PJ01_Loco_SaltoInicio.fbx (12), PJ01_Loco_SaltoAire.fbx (22, bucle), PJ01_Loco_SaltoAterrizaje.fbx (16), PJ01_Loco_Caida.fbx (22, bucle)

**Bien**: impulso corto, aire sin piernas raras, aterrizaje que amortigua, pies nunca bajo el piso.
**Ojo**: en Unity poner Loop Time en SaltoAire y Caida; transiciones cortas (0.05–0.1 s). Si se quiere el clip completo de “caer y rodar”, el original sigue en la carpeta 03.
**Revisión**: `_REVISION\03_Salto.blend` (cámara Cam_Revision; izquierda: Inicio 1–12, Aire 13–56 con arco de altura solo de muestra, Aterrizaje 57–72; derecha: Caida en bucle). Vistas: 03_vista_frente.png, 03_vista_lado.png, 03_vista_aterrizaje.png.

## 07 · Armas en piezas (W02, W03, W04) — HECHO
**Qué hice**
- Importé los zips de Tripo (cada arma venía en una sola malla).
- Corté cada arma con planos rectos (bisect) y separé las piezas por caras; los cortes quedan cerrados (tapas planas, sin huecos) con un material oscuro `Corte_Interior` para que la tapa no estire la textura.
  - **W02 escopeta**: Armazon (cajón, empuñadura, culata, riel), Canon (cañón + tapa de boca), Bomba (guardamanos de bomba).
  - **W03 fusil**: Armazon, Canon (guardamanos + freno de boca), Cargador (curvo), Mira (punto rojo).
  - **W04 francotirador**: Armazon, Canon (cañón + silenciador + bípode), Cargador, Mira (telescópica con anillas).
- Pivote de cada arma en la empuñadura (donde va la mano), cañón hacia +Z de Unity, Y arriba, metros. Objeto raíz `W02`/`W03`/`W04` con las piezas como hijos, todas con transform en cero (se separan moviendo los hijos).
- Escala real: W02 0.95 m, W03 0.80 m, W04 1.15 m (Tripo las daba de 0.98 m todas).

**Archivos exportados** (`imageassets\blender\armas\`): W02.fbx, W03.fbx, W04.fbx (texturas embebidas). Fuentes descomprimidas en `armas\_fuentes\`.

**Bien**: piezas con nombres exactos (Armazon, Canon, Cargador, Mira, Bomba), cortes limpios y cerrados, ninguna pieza flotando cuando están armadas.
**No se pudo / ojo**:
- W02 (escopeta de bomba) no tiene cargador desmontable ni mira en el modelo: lleva Armazon, Canon y Bomba. Para la mejora "Mira" se usa el modelo de la tarea 08.
- Las tapas de corte usan material liso oscuro (no textura); en Unity aparecerá un 2.º material en cada pieza.
- El W03 trae unas aristas abiertas de origen (de Tripo) alrededor de la mira y del cargador; no se ven desde fuera.
**Revisión**: `_REVISION\07_Armas.blend` (cámara Cam_Revision; cuadros 1–90: armadas → piezas separadas → armadas). Vistas: 07_vista_frente.png, 07_vista_lado.png, 07_vista_armadas.png.

## 04 · Clips PJ01: golpes y muertes — HECHO
**Qué hice**
- Pasé los 9 clips de Mixamo a PJ01 y los ajusté:
  - **Puños**: Guardia (72 cuadros, bucle), GolpeDer (49), GolpeIzq (44), Fuerte (56); golpes 20–25 % más rápidos. Revisado que el puño llega al frente (−Y Blender / +Z Unity): 0.62–0.72 m delante de la cadera; GolpeIzq es al abdomen (0.75 m de alto).
  - **Muertes** más instantáneas: velocidad ×1.7 y sin el tramo de espera inicial. Adelante 42 cuadros (1.4 s; antes 104), Atrás 39 (antes 92), Desmayo 47 (antes 169).
  - **Levantarse**: BocaAbajo 157 cuadros, BocaArriba 110 (quité ~3.5 s de estar tirado al inicio).
  - Piso: corregí la altura cuadro por cuadro. Los originales atravesaban el piso hasta 20 cm (Levantarse boca abajo) y 13 cm (muerte adelante); ahora quedan apoyados (máx. 1 cm).

**Archivos exportados** (`imageassets\blender\pj01_clips\`)
- PJ01_Punos_Guardia.fbx, PJ01_Punos_GolpeDer.fbx, PJ01_Punos_GolpeIzq.fbx, PJ01_Punos_Fuerte.fbx
- PJ01_Muerte_Adelante.fbx, PJ01_Muerte_Atras.fbx, PJ01_Muerte_Desmayo.fbx
- PJ01_Dano_Levantarse_BocaAbajo.fbx, PJ01_Dano_Levantarse_BocaArriba.fbx

**Bien**: puños al frente, muertes rápidas sin atravesar el piso, levantarse apoyado.
**Ojo**: las muertes y levantarse traen desplazamiento de raíz (avanza/retrocede al caer, como en Mixamo); en Unity usar Root Motion o "Bake Into Pose" en XZ según convenga. Si la muerte aún se siente lenta, se puede subir a ×2.
**Revisión**: `_REVISION\04_Golpes_Muertes.blend` (cámara Cam_Revision; izquierda puños 1–221, centro muertes 1–128, derecha levantarse 1–267). Vistas: 04_vista_frente.png, 04_vista_lado.png, 04_vista_golpe.png.

## 05 · Clips PJ01: locomoción base — HECHO
**Qué hice**
- Pasé Loco_Idle (236 cuadros), Loco_CaminarAtras (37), Loco_CaminarDer (32), Loco_CaminarIzq (32), Loco_GirarDer (46), Loco_GirarIzq (46) y Loco_Frenar (28) a PJ01.
- Pies: los pasos laterales metían el pie hasta 3.8 cm en el piso; corregido (0 cm).
- Implantes: medí choques de brazos contra IMP01 (piernas) y de los brazaletes IMP02 contra piernas. Todos chocaban (Idle muchísimo: brazos colgando sobre el exoesqueleto). Hice versiones **_ConImplante** con hombros arriba y brazos separados (igual que caminar/correr): 10°/12° en Idle, Atrás, GirarIzq y Frenar; 10°/16° en GirarDer; 12°/20° en los pasos laterales. Choques después: 0–17 vértices en todo el clip (no se notan).

**Archivos exportados** (`imageassets\blender\pj01_clips\`)
- PJ01_Loco_Idle.fbx + PJ01_Loco_Idle_ConImplante.fbx
- PJ01_Loco_CaminarAtras.fbx + _ConImplante
- PJ01_Loco_CaminarDer.fbx + _ConImplante, PJ01_Loco_CaminarIzq.fbx + _ConImplante
- PJ01_Loco_GirarDer.fbx + _ConImplante, PJ01_Loco_GirarIzq.fbx + _ConImplante
- PJ01_Loco_Frenar.fbx + _ConImplante

**Bien**: pies en el piso, brazos libres del exoesqueleto en las versiones _ConImplante.
**Ojo**: igual que caminar/correr, el Animator debe usar la versión _ConImplante cuando PJ01 lleve IMP01 o IMP02.
**Revisión**: `_REVISION\05_Locomocion.blend` (cámara Cam_Revision; izquierda sin implantes, derecha con IMP01_LVL2 + IMP02_LVL1; los 7 clips seguidos, cuadros 1–451). Vistas: 05_vista_frente.png, 05_vista_lado.png, 05_vista_implantes.png.

## 10 · Charola de la mesera R01 — HECHO
**Qué hice**
- Importé R01 (esqueleto mixamorig de Tripo, una sola malla). La charola estaba pesada a 17 huesos (dedos, mano y antebrazo): al mover los dedos se doblaba hasta 12.7 cm.
- Opción elegida: **arreglar los pesos** (no separar objeto) para que en Unity siga siendo un solo SkinnedMesh con el mismo esqueleto. Los 1 790 vértices de la charola quedaron a peso 1.0 en `mixamorig:LeftHand`.
- Prueba: doblé dedos 35°, muñeca y antebrazo; la charola se mueve como sólido (error 0.0 mm). Los dedos se siguen moviendo normal debajo de ella.

**Archivo exportado**: `imageassets\blender\R01\R01.fbx` (esqueleto + malla `R01_Cuerpo`, texturas embebidas, sin animaciones, Y arriba, metros, escala Tripo 0.98 m). Fuente en `R01\_fuente\`.

**Bien**: la charola ya no se deforma con ninguna pose de mano/dedos.
**Ojo**: R01 viene en pose T (brazo izquierdo estirado con la charola). Con animaciones de Mixamo de "llevar charola" la charola seguirá la mano; si una animación cierra el puño, los dedos pueden asomar por encima de la charola (se ven en 10_vista_charola.png). En Unity escalar como los otros NPC (Scale Factor ≈ 1.3–1.8 según la altura deseada).
**Revisión**: `_REVISION\10_Charola.blend` (cámara Cam_Revision; cuadros 1–60: mano y dedos se mueven, la charola no se dobla). Vistas: 10_vista_frente.png, 10_vista_lado.png, 10_vista_charola.png.

## 06 · Clips PJ01: arma larga, interacciones y cobertura — HECHO
**Qué hice**
- Pasé 16 clips de Mixamo a PJ01 y corregí pies/piso:
  - Rifle_Idle (258), Rifle_Apuntar (94), Rifle_Caminar (40), Rifle_Correr (21), Rifle_ApuntarCaminar (42), Rifle_ApuntarCorrer (23), Rifle_DisparoCaminar (99), Rifle_Recargar (100)
  - Cobertura_Entrar (35), Cobertura_Disparo (69), Cobertura_Disparo2 (81)
  - Inter_AbrirPuerta (192), Inter_Boton (98), Inter_Recoger (104), Inter_Telefono (1118), Inter_VerTelefono (100)
- Agarre: puse el fusil W03 (de la tarea 07) en la mano derecha (empuñadura en la palma, cañón hacia la mano izquierda) y lo revisé en los 8 Rifle_*. La mano izquierda queda sobre el guardamanos: a 0–3 cm del eje del cañón (Idle/Caminar/Apuntar/Disparo) y hasta 6 cm al correr; en Recargar la mano izquierda se va al cargador (normal).
- W03 respecto al hueso R_Hand (Blender, sin escala): posición (0.003, 0.068, 0.003) m, rotación (1.6, -169.3, -0.5)°. En Unity: hijo de R_Hand y ajustar a ojo con esos valores como punto de partida.

**Archivos exportados** (`imageassets\blender\pj01_clips\`): PJ01_Rifle_Idle.fbx, PJ01_Rifle_Apuntar.fbx, PJ01_Rifle_Caminar.fbx, PJ01_Rifle_Correr.fbx, PJ01_Rifle_ApuntarCaminar.fbx, PJ01_Rifle_ApuntarCorrer.fbx, PJ01_Rifle_DisparoCaminar.fbx, PJ01_Rifle_Recargar.fbx, PJ01_Cobertura_Entrar.fbx, PJ01_Cobertura_Disparo.fbx, PJ01_Cobertura_Disparo2.fbx, PJ01_Inter_AbrirPuerta.fbx, PJ01_Inter_Boton.fbx, PJ01_Inter_Recoger.fbx, PJ01_Inter_Telefono.fbx, PJ01_Inter_VerTelefono.fbx

**Bien**: agarre del fusil coherente en todos los Rifle_*, pies en el piso.
**Ojo / no perfecto**:
- Los Cobertura_* de Mixamo son con pistola/manos juntas, no con arma larga (la mano izquierda no va al guardamanos). Sirven para pistola; para fusil en cobertura haría falta otro clip.
- Inter_Telefono dura 37 s (1118 cuadros) como el original; si se quiere corto, recortarlo en Unity con los eventos de clip.
- No hice versiones _ConImplante de estos (en Rifle_* los brazos van al frente y no chocan con IMP01).
**Revisión**: `_REVISION\06_Rifle_Inter_Cobertura.blend` (cámara Cam_Revision; izquierda Rifle_* con el W03 en la mano, centro Inter_*, derecha Cobertura_*). Vistas: 06_vista_frente.png, 06_vista_lado.png, 06_vista_rifle.png.

## 08 · Miras de las armas — HECHO
**Qué hice**
- **Mira_PuntoRojo** (pistola): modelada nueva, tipo mini-réflex de pistola: base 2.9 × 4.2 cm, capucha delantera, vidrio inclinado y punto rojo emisivo. Bordes biselados.
- **Mira_Holografica** (escopeta/fusil): modelada nueva (el punto rojo del W03 de Tripo salía roto al separarlo: cascarón abierto). Base de riel en color arena, cuerpo trasero, ventana rectangular con vidrio y retícula circular + punto emisivos. 4.0 × 7.0 × 4.9 cm.
- **Mira_Telescopica** (francotirador): la del W04 (separada en la tarea 07, con su textura de Tripo y anillas), limpia de piezas sueltas. 41 cm de largo.
- Las tres: pivote en el centro de la base de montaje (abajo), frente hacia +Z de Unity, Y arriba, metros.
- Materiales de las nuevas: Mira_Metal (gris metal), Mira_Acento (arena, como las armas), Mira_Vidrio (turquesa transparente), Mira_Punto (rojo emisivo).

**Archivos exportados** (`imageassets\blender\armas\`): Mira_PuntoRojo.fbx, Mira_Holografica.fbx, Mira_Telescopica.fbx

**Bien**: tamaño real y estilo de colores de las armas; la holográfica montada sobre el W02 (en el riel, a 20 cm de la empuñadura) se ve en proporción.
**Ojo**: las dos nuevas son de geometría sencilla (sin textura pintada como las de Tripo). Si se quieren con el mismo detalle que las armas, pedirlas al chat de arte para Tripo y aquí solo se ajustan. El vidrio usa transparencia: en Unity poner el material en modo Transparent.
**Revisión**: `_REVISION\08_Miras.blend` (cámara Cam_Revision). Vistas: 08_vista_frente.png, 08_vista_lado.png, 08_vista_montada.png.

## 09 · Puertas de los otros carros (V02, V03, V04, V06, V09, V10) — NO SE PUDO
**Por qué**: las carrocerías sin puertas de Tripo todavía no existen. En `imageassets\modelos-nuevos\piezas\` solo están las imágenes (V02_Carroceria.png … V10_Carroceria.png) para generarlas; no hay modelos 3D. Los zips de esta carpeta son los carros completos (con las puertas pegadas a la carrocería), que no se pueden abrir sin el hueco.
**Qué dejé listo**: descomprimí los 6 modelos actuales en `imageassets\blender\puertas\_fuentes_09\` y los acomodé en `_REVISION\09_Puertas_otros_carros.blend` como referencia. Cuando lleguen las carrocerías, se hace igual que la tarea 01 (mismo procedimiento ya probado: medir hueco, ajustar puerta, pivote en bisagra, exportar `<Código>_Puerta_Izquierda/Derecha.fbx` + `<Código>_Ensamble.fbx`).
**Vistas**: 09_vista_frente.png, 09_vista_lado.png.

---
# RESUMEN FINAL (9-nov)
**Hecho**
- 01 Puertas V01A/V01B/V05: 6 puertas ajustadas a su hueco, pivote en bisagra, + 3 ensambles. → `imageassets\blender\puertas\`
- 02 Carro y moto: Carro_Entrar/Manejar/Salir encadenados + Moto_Subir/Manejar/Bajar nuevos con la C19. → `pj01_clips\`
- 03 Salto: Inicio (12 cuadros), Aire (bucle), Aterrizaje (16), Caída (bucle), todos en el lugar. → `pj01_clips\`
- 07 Armas en piezas: W02, W03, W04 con Armazon/Canon/Cargador/Mira/Bomba, cortes cerrados, pivote en empuñadura. → `imageassets\blender\armas\`
- 04 Golpes y muertes: 4 puños, 3 muertes rápidas (×1.7), 2 levantarse, sin atravesar el piso. → `pj01_clips\`
- 05 Locomoción base: 7 clips + 7 versiones _ConImplante sin choques con IMP01/IMP02. → `pj01_clips\`
- 10 Charola R01: charola rígida en la mano (0 mm de deformación). → `imageassets\blender\R01\R01.fbx`
- 06 Rifle/interacciones/cobertura: 16 clips, agarre revisado con el W03. → `pj01_clips\`
- 08 Miras: Mira_PuntoRojo, Mira_Holografica (nuevas), Mira_Telescopica (del W04). → `imageassets\blender\armas\`

**A medias / con observaciones**
- 02: clips de moto hechos a mano con IK (no hay de Mixamo); posiciones de asiento dependen de la escala del carro (×4.3) y la moto (×1.8) en Unity.
- 06: los Cobertura_* de Mixamo son de pistola/manos juntas, no de arma larga.
- 08: las miras nuevas son de geometría simple sin textura pintada.

**No se pudo**
- 09: faltan las carrocerías sin puertas de V02, V03, V04, V06, V09 y V10 (solo hay imágenes de referencia).

**Archivos de trabajo**
- Respaldo del maestro: `imageassets\blender\pistola\W01_Pistola_agarre_respaldo_2026-11-09.blend`
- El maestro `W01_Pistola_agarre.blend` ahora tiene todas las acciones nuevas de PJ01 (y las RAW_ de origen del salto/golpes con usuario falso) y la escena a 30 fps.
- Herramientas reutilizables (retarget Mixamo→PJ01, piso, IK, exportación): `imageassets\blender\_herramientas\cc_tools.py`.
- Clips de PJ01 totales en `pj01_clips\`: 80 en total (31 anteriores + 49 nuevos de hoy).

### 01 · Ajuste (revisión) — V01B
- Pedido: los marcos de la puerta sobresalían o quedaban cortos al cerrar.
- Hecho en V01B (prueba): pegué el contorno de cada puerta al contorno del hueco (solo se mueve la orilla; el centro del panel queda igual). Antes: sobresalía hasta 1.4 cm y quedaban huecos de 2.9 cm (escala Tripo). Después: huecos ≤ 0.5 cm; la izquierda aún sobresale 1 cm en el borde delantero bajo. El espejo quedó un poco más angosto.
- Reexportado: puertas\V01B_Puerta_Izquierda.fbx, V01B_Puerta_Derecha.fbx, V01B_Ensamble.fbx. Mallas anteriores guardadas en el .blend como *_antes.
- Vistas antes/después: 01_V01B_antes_izq.png, 01_V01B_despues_izq.png, 01_V01B_antes_der.png, 01_V01B_despues_der.png.
- Pendiente: aplicar lo mismo a V01A y V05 si se aprueba.

### 01 · DECISIÓN (9-nov, revisión con Tony)
- **Los carros V01A, V01B y V05 se van al juego SIN PUERTAS por ahora.** El ajuste de las puertas de Tripo al hueco de la carrocería no quedó bien (los marcos no coinciden con el carro).
- Los FBX de puertas en `imageassets\blender\puertas\` se quedan guardados pero NO se usan. En Unity: usar solo la carrocería (`<Código>_Carroceria`), sin `_Puerta_Izquierda/Derecha`.
- Si más adelante se quieren puertas: pedirlas a Tripo generadas con la imagen del hueco de cada carro, o recortarlas de la propia carrocería.


### 02 — Ajuste (revisión): postura sentado en el carro
- Pedido: se sentaba muy alto, pies atravesaban el piso, cabeza el techo, manos en el aire.
- Hecho: cadera 15 cm más abajo y 5 cm hacia adelante (queda sobre el cojín), torso inclinado 13° hacia el volante con la cabeza nivelada, piernas estiradas con IK hasta el piso del carro (pies apoyados bajo el tablero), manos con IK justo en el aro del volante (10 y 2) conservando el pequeño giro de volante del clip original.
- Medido en todo el clip: punto más bajo 0.098 (piso 0.095, ya no atraviesa); punta de la cabeza 1.27 (techo 1.30).
- Carro_Entrar: la cadera y piernas se acomodan a la nueva postura entre f100–128 y las manos van al volante entre f136–156 (la entrada aprobada no cambia antes de f100).
- Carro_Salir: suelta el volante f4–16 y regresa a la postura original f8–40, antes de levantarse.
- Re-exportados (reemplazados): pj01_clips\PJ01_Carro_Entrar.fbx, PJ01_Carro_Manejar.fbx, PJ01_Carro_Salir.fbx. Maestro actualizado (versiones anteriores guardadas como *_ANTERIOR).
- Vistas nuevas: 02_vista_carro_lado.png, 02_vista_carro_entrando.png, 02_vista_carro_atras.png. Puertas del V01A ocultas en este archivo (decisión: carros sin puertas).
- Mejorable: brazos casi rectos porque el volante del V01A queda lejos del asiento; si se quiere más relajado habría que acercar el volante en el modelo del carro.


### 02 — Aprobado por el usuario (carro). PENDIENTE: resolver/ajustar los finales de la moto (Moto_Bajar y la transición al terminar de manejar).


### 03 — Ajuste (revisión): salto más rápido
- Pedido: pasaba mucho tiempo en el aire.
- Hecho: Loco_SaltoAire acelerado de 22 a 15 cuadros (bucle de 0.47 s; mismo movimiento, más rápido). En el archivo de muestra el aire dura ahora 17 cuadros (0.57 s, antes 1.5 s) y sube 0.5 m (antes 0.75).
- Re-exportado (reemplazado): pj01_clips\PJ01_Loco_SaltoAire.fbx. Maestro actualizado (anterior como Loco_SaltoAire_ANTERIOR).
- Ojo en Unity: lo que tarda en el aire lo decide el CharacterController (fuerza del salto y gravedad), no el clip. Para que se sienta igual: altura ~0.5 m y gravedad ~-12 (≈0.57 s en el aire, igual que la muestra); si se quiere aún más rápido, subir la gravedad.
- Vistas actualizadas: 03_vista_frente.png, 03_vista_lado.png, 03_vista_aterrizaje.png.


### 03 — Ajuste 2 (revisión): aire con movimiento y salto más rápido
- Pedido: en el aire se veía congelado; subida y caída un poco más rápidas.
- Hecho: Loco_SaltoAire rehecho como movimiento de 12 cuadros (0.4 s), NO bucle: sale de la pose final de SaltoInicio, encoge las rodillas (una más que la otra) y abre los brazos, y luego estira las piernas hacia abajo hasta la pose inicial de SaltoAterrizaje.
- Muestra: en el aire 14 cuadros (0.47 s, antes 0.57) y 0.45 m de altura.
- Re-exportado (reemplazado): pj01_clips\PJ01_Loco_SaltoAire.fbx. Maestro actualizado (la primera versión sigue como Loco_SaltoAire_ANTERIOR).
- CAMBIA la indicación para Unity: SaltoAire SIN Loop Time (se queda en la última pose si la caída es larga; para caídas largas usar Loco_Caida, que sí es bucle). Gravedad sugerida ~-16 con altura ~0.45 m.
- Vistas actualizadas: 03_vista_frente.png y 03_vista_lado.png (rodillas arriba), 03_vista_aterrizaje.png (estirando para aterrizar).


### 03 — Aprobado por el usuario (salto con movimiento en el aire).


### 04 — Aprobado por el usuario (golpes y muertes, sin cambios).


### 05 — Aprobado por el usuario (locomoción base, sin cambios).


### 06 — Aprobado por el usuario (rifle, interacciones y cobertura, sin cambios).


### 07 — Ajuste (revisión): mira completa del fusil W03
- Pedido: la pieza Mira del W03 no tomaba toda la mira; también separar su base.
- Hecho: la pieza Mira ahora es la mira completa (cuerpo con el vidrio rojo + ocular trasero) junto con su base atornillada y las dos trabas sobre el riel. Corte recto a ras del riel (z del cajón), tapas cerradas con Corte_Interior en las dos piezas; el cajón queda limpio arriba y el poste de mira trasera se queda en el Armazon. Unas astillas sueltas del poste que estaban en la Mira regresaron al Armazon.
- Re-exportado (reemplazado): armas\W03.fbx (mismos nombres de piezas; Canon y Cargador idénticos a antes).
- Vistas nuevas: 07_vista_W03_separado.png, 07_vista_W03_armado.png.
- Nota: el fusil que se ve en la mano en 06_Rifle_Inter_Cobertura.blend es la versión anterior (solo de muestra; los clips no cambian).


### 07 — Aprobado por el usuario (con la mira completa del W03).


### 08 — Decisión del usuario (revisión)
- Escopeta W02: SIN mira (se descarta la mejora de mira para la escopeta).
- Pistola: usa Mira_PuntoRojo — aprobada.
- Fusil W03 y francotirador W04: usan su propia mira (pieza Mira de cada arma) — aprobado.
- Mira_Holografica.fbx queda sin uso (no se borró; está en armas\ por si se necesita después).


### 09 — Cancelada por decisión del usuario: los carros van sin puertas, así que no se necesitan las puertas de los otros carros.


### 10 — Aprobado por el usuario (charola R01, sin cambios).

---
# RESUMEN DE LA REVISIÓN (9-nov)
- 01 Puertas: decisión — los carros V01A, V01B y V05 van al juego SIN puertas.
- 02 Carro/Moto: ajustada la postura en el carro (más abajo, piernas estiradas, manos en el volante); re-exportados Carro_Entrar/Manejar/Salir. Aprobado.
- 03 Salto: aire con movimiento (encoge rodillas, abre brazos) y salto más rápido; re-exportado Loco_SaltoAire (ya NO es bucle). Aprobado.
- 04 Golpes y muertes: aprobado sin cambios.
- 05 Locomoción base: aprobado sin cambios.
- 06 Rifle, interacciones y cobertura: aprobado sin cambios.
- 07 Armas en piezas: mira completa del W03 con su base; re-exportado armas\W03.fbx. Aprobado.
- 08 Miras: pistola con Mira_PuntoRojo; fusil y francotirador con su propia mira; escopeta SIN mira. Mira_Holografica sin uso.
- 09 Puertas de otros carros: cancelada (carros sin puertas).
- 10 Charola R01: aprobado sin cambios.

**PENDIENTE**
- Ajustar los finales de la moto (Moto_Bajar y la transición al terminar de manejar).

