# T-312 Asfalto mojado y reflejos de neón del barrio
Dueño: Alan | Rama: alan/T-312-asfalto-mojado (sale de alan/fase-5) | Modelo: Sonnet 5.5, medium
Depende de: T-079, T-098, T-099 y T-310 (ya hechas). No depende de T-084 ni de T-085 y no las toca. T-311 (fachadas) la lleva otra sesión: esta tarea no edita materiales ni scripts de fachadas.

## Objetivo
La auditoría visual (Logs/alan-auditoria-visual-salida.txt) encontró que el asfalto es negro mate de noche (V1_22, V2_22) y un azul uniforme de día (V3_12, T-094-1 y T-094-2), con solo 50 charcos rectangulares duros. Darle variación tonal, charcos con borde suave y reflejos creíbles de los neones, con pocos materiales y poca geometría, sin tocar lo que se camina.

## Qué se hace (diseño)
1. **Una textura de asfalto** (`Asfalto_Mojado.png`, 512 px, se repite cada 12 m): ruido de valores que encaja sin costura, parches más oscuros y lisos (bacheo nuevo) y grano. El **alfa es el brillo** (0.45 a 1.0): `Barrio_Asfalto` toma la suavidad del alfa del albedo (`_SmoothnessTextureChannel = 1`), así hay zonas más mojadas (reflejo largo del neón) y más secas (reflejo corto). Cero materiales nuevos y una sola muestra de textura.
2. **Charcos con borde suave** (`Charco_Mascara.png`, 128 px): `Barrio_Charco` pasa a URP/Lit translúcido (alfa, sin ZWrite, sin sombra), suavidad 0.98. Se queda en 1 material y 1 textura para todos. A los 50 charcos de T-079 (mismas posiciones y semilla) se suman 24 de cuneta, largos y estrechos pegados a la banqueta, fuera de los pasos de cebra.
3. **Líneas de borde de calle**: 14 cubos delgados sin collider (material `Final_Raya`, el mismo gris de las cebras de T-099) en las tres calles, cortados antes de cada cruce.
4. **Reflejo de neón**: sigue siendo de las cuatro sondas horneadas de T-079 (se rehornean con `HornearReflejos`). No se añaden luces ni emisión falsa: más suavidad en el suelo y charcos que sí se ven en las sondas.
5. **No cambia**: el cubo `Asfalto` (collider, tamaño, cara de arriba en y = 0), `Marcas`, cebras, grietas, alcantarillas, el cielo y la niebla de T-310, ni los materiales `Fachada_*`, `Barrio_Edif_*` y letreros (T-311 y T-310 los tienen).

Código: `Mundo/Editor/VestirAsfalto.cs` (nuevo), enganche y charcos de cuneta en `Mundo/Editor/CreadorAmbiente.cs`. Se genera con `CreadorAmbiente.Crear` (o `VestirAsfalto.Crear` suelto); nunca YAML a mano.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Tests/EditMode/Mundo/, Assets/_Proyecto/Tests/PlayMode/Mundo/, escenas de Alan (Ciudad, Pruebas_Alan), docs/capturas/, docs de Alan. Nada de Core, ProjectSettings, Packages, herramientas ni Tony.

## Criterios de aceptación y resultado medido (2026-10-27, Unity 6000.3.25f1)
Capturas `docs/capturas/T-312-V<n>_<hora>_antes.png` y `T-312-V<n>_<hora>.png` (vistas V1 a V3, 12:00 y 22:00, 1280x720). Zona de calzada medida (`Editor/MedirCaptura.cs`, x0 y0 x1 y1): V1 100 545 1000 650; V2 80 440 1200 600; V3 300 520 1180 700. Dispersión = P90 − P10 de la luminancia (la lluvia no la engaña como la desviación estándar). Medidas en `Logs/t312-medidas-*.txt` (no se suben).

- [x] Compila: `verificar.sh compilar` OK.
- [x] EditMode: 958/958 (incluye las 6 de `AsfaltoMojadoPruebas`: texturas, variación tonal y de brillo, costura, material de asfalto, material de charco, prefab).
- [x] PlayMode: 908/918 pasaron, 9 omitidas (las de siempre) y 1 fallo, `RecorridoEconomiaPruebas.Recorrido_ComprarApostar...` de Progresión («la partida real no se tocó»): otro Unity (T-311) corría PlayMode a la vez y comparte `persistentDataPath`, y el archivo de guardado cambió por él. Repetida sola, Progresión dio 114/114. Se toma como interferencia de la máquina, no de T-312.
- [x] Caminabilidad: colliders del prefab `Barrio` 361 antes y 361 después (conteo de BoxCollider/MeshCollider/etc. en el YAML); charcos y bordes sin collider (prueba); el cubo `Asfalto` conserva tamaño y cara de arriba en y = 0 (prueba); las escenas Ciudad y Pruebas_Alan no cambiaron.
- [x] Cruces: los 14 bordes terminan a ≥ 11 m del centro de cada cruce (prueba) y los charcos de cuneta están fuera de las cebras (despeje 14 m); mirado en V1.
- [x] Repetibilidad: correr `CreadorAmbiente.Crear` 3 veces dio texturas, materiales y escenas idénticos byte a byte; `Barrio.prefab` cambia solo los fileID de los objetos recreados (igual que los charcos de T-079); comparado sin fileID y ordenado, es idéntico (88 objetos Charco_, Cuneta_ y Borde).
- [x] Sin choque con T-310: franja de cielo de V1 a las 12:00 (x 420-620, y 0-230): luminancia 0.4144 en `T-310-12.png` contra 0.4126 ahora, dispersión de matiz 0.0500 contra 0.0501. No se tocó el cielo ni `ConfigCicloDia`.
- [x] Sin choque con T-311: esta rama no toca `Fachadas/`, `VestirFachadas.cs`, `Fachada_*`, `Barrio_Edif_*` ni letreros. (La mezcla real con T-311 no se probó.)
- [x] Coste en recursos: 0 materiales nuevos (solo se cambian `Barrio_Asfalto` y `Barrio_Charco`), 2 texturas (≈ 1.4 MB y 0.07 MB con mipmaps), 38 objetos nuevos (24 charcos + 14 líneas).
- [x] **Tiempo de fotograma (2026-10-27, sin otro Unity abierto; verificado con `Get-Process`):** herramienta `Tests/PlayMode/Mundo/MedirFotogramaAsfalto.cs` (explícita; 30 fotogramas de calentamiento y 400 medidos por vista, V1 a V3 a las 12:00 y 22:00, Pruebas_Alan, modo batch). Antes (prefab y 2 materiales de 7a4f92b): mediana 0.34-0.37 ms, p95 0.59-0.70 ms. Después (dos corridas): mediana 0.33-0.36 ms, p95 0.54-0.70 ms. Sin diferencia medible. Límite: es modo batch sin vsync, mide sobre todo CPU; no es el FPS de un jugador con GPU real. La primera corrida «después» dio 0.6-1.07 ms de mediana por ser la primera tras reimportar (arranque en frío) y se descartó; las otras dos la contradicen.
- [ ] Noche, dispersión de luminancia en la calzada ≥ 1.3 veces `_antes` (remedido en esta sesión con las mismas zonas: V1 0.177 → 0.308, 1.74×; V2 0.134 → 0.090, 0.67×; V3 0.257 → 0.263, 1.02×; no se hizo ningún cambio nuevo en V2): **cumple en V1** (0.177 → 0.309, 1.75×) y **no en V2** (0.134 → 0.106; en V2 pesan las rayas amarillas y la lluvia, y la primera versión daba 0.448 pero por un brillo mate que lavaba la imagen). Luminancia media de V1: 0.406 → 0.411; saturados 0 %.
- [ ] Noche, reflejo de color: la métrica «píxeles de color vivo» no sirve (V1 12.5 % → 11.2 %, V2 4.38 % → 4.39 %: la zona mide sobre todo el azul base del suelo). El reflejo de neón se ve a simple vista en V1 (franja magenta y turquesa en la calzada, abajo) y no en V2. Es juicio visual de un modelo, no de una persona.
- [ ] Día, dispersión ≥ 1.5 veces: **no se cumple** (V1 0.055 → 0.053, V2 0.048 → 0.054, V3 0.276 → 0.290). Sí sube la dispersión del matiz (V1 0.031 → 0.037, V2 0.053 → 0.065, V3 0.0096 → 0.0387): el asfalto de día ya no es un azul uniforme, pero el cambio es modesto porque de día manda el reflejo del cielo sobre un albedo de 0.04.
- [ ] Límite honesto: «se ve bien» es subjetivo; queda para revisión humana con las capturas lado a lado.

## Revisión visual a tamaño real
- **Charco negro u opaco:** ya no. Tienen borde suave y son translúcidos; de noche se ven como manchas oscuras con brillo, de día como parches más oscuros. Siguen siendo bastante oscuros: reflejan poco color porque las sondas están horneadas con el suelo viejo (no se rehornearon).
- **Repetición cada 12 m:** no es evidente en V1 a V3. En vistas aéreas (V4) no se miró.
- **Estética cyberpunk:** de noche mejora (suelo con brillos desiguales y una franja de neón reflejada en V1). Primer intento con suavidad mínima 0.45 y `_Smoothness` 0.85 dio manchas amarillas enormes de las luces del suelo; se corrigió a mínimo 0.5 y `_Smoothness` 1.0 (el alfa trae el brillo). De día el efecto es sutil.
- **Ideas no hechas:** rehornear sondas (`CreadorAmbiente.HornearReflejos`, cambia 4 EXR grandes), subir el color base del asfalto (hay que tocar `AmbientePruebas.Asfalto_EsOscuroYLiso`), más contraste de día.

## Estado
Implementada y verificada en lo medido arriba; **no cerrada**: el coste de fotograma ya está medido (sin diferencia); siguen sin cumplirse V2 de noche (0.67×) y la dispersión de día (V1 0.97×, V2 1.05×, V3 1.05×; umbral 1.5×), y falta la revisión humana. No se intentó un cambio nuevo: la textura ya refleja el neón en los charcos de V2 y subir el contraste tonal exigiría tocar el albedo base y `AmbientePruebas.Asfalto_EsOscuroYLiso`, que decide Alan. Commit WIP, sin compilar PlayMode completo en este estado (solo compilar OK; el cambio es la herramienta de medición). Rama `alan/T-312-asfalto-mojado`; sin integrar a `alan/fase-5` ni a `main`.
