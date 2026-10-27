# T-312 Asfalto mojado y reflejos de neón del barrio
Dueño: Alan | Rama: alan/T-312-asfalto-mojado (sale de alan/fase-5) | Modelo: Sonnet 5.5, medium
Depende de: T-079, T-098, T-099 y T-310 (ya hechas). No depende de T-084 ni de T-085 y no las toca. T-311 (fachadas) la lleva otra sesión: esta tarea no edita materiales ni scripts de fachadas.

## Objetivo
La auditoría visual (Logs/alan-auditoria-visual-salida.txt) encontró que el asfalto es negro mate de noche (V1_22, V2_22) y un azul uniforme de día (V3_12, T-094-1 y T-094-2), con solo 46 charcos rectangulares duros. Darle variación tonal, charcos con borde suave y reflejos creíbles de los neones, con pocos materiales y poca geometría, sin tocar lo que se camina.

## Qué se hace (diseño)
1. **Una textura de asfalto** (`Asfalto_Mojado.png`, 512 px, se repite cada 12 m): ruido de valores que encaja sin costura, parches más oscuros y lisos (bacheo nuevo) y grano. El **alfa es el brillo** (0.45 a 1.0): `Barrio_Asfalto` toma la suavidad del alfa del albedo (`_SmoothnessTextureChannel = 1`), así hay zonas más mojadas (reflejo largo del neón) y más secas (reflejo corto). Cero materiales nuevos y una sola muestra de textura.
2. **Charcos con borde suave** (`Charco_Mascara.png`, 128 px): `Barrio_Charco` pasa a URP/Lit translúcido (alfa, sin ZWrite, sin sombra), suavidad 0.98. Se queda en 1 material y 1 textura para todos. A los 46 charcos de T-079 (mismas posiciones y semilla) se suman 24 de cuneta, largos y estrechos pegados a la banqueta, fuera de los pasos de cebra.
3. **Líneas de borde de calle**: 14 cubos delgados sin collider (material `Final_Raya`, el mismo gris de las cebras de T-099) en las tres calles, cortados antes de cada cruce.
4. **Reflejo de neón**: sigue siendo de las cuatro sondas horneadas de T-079 (se rehornean con `HornearReflejos`). No se añaden luces ni emisión falsa: más suavidad en el suelo y charcos que sí se ven en las sondas.
5. **No cambia**: el cubo `Asfalto` (collider, tamaño, cara de arriba en y = 0), `Marcas`, cebras, grietas, alcantarillas, el cielo y la niebla de T-310, ni los materiales `Fachada_*`, `Barrio_Edif_*` y letreros (T-311 y T-310 los tienen).

Código: `Mundo/Editor/VestirAsfalto.cs` (nuevo), enganche y charcos de cuneta en `Mundo/Editor/CreadorAmbiente.cs`. Se genera con `CreadorAmbiente.Crear` (o `VestirAsfalto.Crear` suelto); nunca YAML a mano.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Tests/EditMode/Mundo/, Assets/_Proyecto/Tests/PlayMode/Mundo/, escenas de Alan (Ciudad, Pruebas_Alan), docs/capturas/, docs de Alan. Nada de Core, ProjectSettings, Packages, herramientas ni Tony.

## Criterios de aceptación (todos medibles; ninguno está verificado todavía)
Capturas con `VistasReferencia` (vistas V1, V2 y V3 a las 12:00 y a las 22:00), 1280x720, antes de aplicar el cambio (`docs/capturas/T-312-V<n>_<hora>_antes.png`) y después (`T-312-V<n>_<hora>.png`). "Calzada" = píxeles de la mitad inferior de la imagen que no son banqueta ni letrero (máscara fija por vista, guardada junto a las capturas).

- [ ] Compila sin errores y `bash herramientas/verificar.sh todo` pasa (EditMode y PlayMode), sin fallos nuevos.
- [ ] EditMode `AsfaltoMojadoPruebas` (6 pruebas): texturas con su tamaño, sin compresión, con mipmaps y repetibles sin costura; desviación tonal ≥ 0.05 y del brillo ≥ 0.05; brillo medio ≥ 0.6 y mínimo ≥ 0.4; albedo efectivo (color × textura) < 0.06; material de charco translúcido; 60 a 80 charcos y 14 líneas, todos sin collider.
- [ ] Caminabilidad: el cubo `Asfalto` conserva collider, tamaño (272 × 178) y cara de arriba en y = 0 (`Prefab_CharcosYBordes_SinCollider_Y_AsfaltoIntacto`), y ninguna prueba de PlayMode de recorrido (Barrio, Vehículos, Prólogo) cambia de resultado. Cantidad de colliders del prefab `Barrio` igual antes y después.
- [ ] Noche (22:00): en la calzada, desviación estándar de la luminancia ≥ 1.3 veces la de `_antes` en V1 y V2; al menos 1.5 % de los píxeles de la calzada con saturación HSV > 0.3 y luminancia > 0.15 (reflejo de color de neón; en `_antes` se mide también y debe ser menor). Luminancia media de la imagen completa entre 0.9 y 1.15 veces la de `_antes`, y píxeles saturados (≥ 0.98, `VistasReferencia.Medir`) no más de 0.5 puntos porcentuales sobre `_antes` (el bloom no lava).
- [ ] Día (12:00): en la calzada de V3, la desviación estándar de la luminancia ≥ 1.5 veces la de `_antes` y la dispersión del matiz (HSV) mayor que en `_antes`, es decir ya no es "un azul uniforme". Luminancia media de la calzada ≤ 0.35 (sigue siendo asfalto, no un espejo).
- [ ] Sin choque con T-310: franja de cielo de V1 a las 12:00 (x 420-620, y 0-230 en 640x360) igual que `T-310-12.png` (saturación media y brillo con diferencia ≤ 0.02). La noche del cielo y de la niebla no cambia (misma medición con `T-310-22.png`).
- [ ] Sin choque con T-311: `git diff alan/fase-5 --stat` de esta rama no toca `Mundo/Fachadas/`, `VestirFachadas.cs`, `Fachada_*`, `Barrio_Edif_*` ni letreros; el rebase o la mezcla con T-311 no deja conflictos en `Barrio.prefab` que no sean de regeneración (el prefab se regenera con `CreadorTodo`).
- [ ] Repetibilidad: correr `CreadorAmbiente.Crear` dos veces seguidas deja `git status` sin cambios en las texturas, materiales y `Barrio.prefab` entre la primera y la segunda corrida; `CreadorTodo.Crear` completo da los mismos 46 charcos de T-079 (mismos nombres y posiciones) más los 24 de cuneta.
- [ ] Coste razonable: materiales nuevos = 0 (solo se modifican `Barrio_Asfalto` y `Barrio_Charco`), texturas nuevas = 2 (≤ 1.6 MB juntas con mipmaps), objetos nuevos ≤ 40 (24 charcos + 14 líneas), todos en materiales ya existentes. Tiempo de fotograma en V1 y V2 con `MedidorFotogramas` no más de 5 % peor que `_antes` (misma máquina, mismo Unity abierto).
- [ ] Límite honesto: que "se vea bien" es subjetivo; queda para revisión humana con las capturas lado a lado. La repetición de la textura cada 12 m puede notarse en vistas aéreas (V4), que no se miden aquí.

## Riesgos a mirar al verificar
- El color base del asfalto (0.04) lo reinicia `CreadorBarrio`; la textura va multiplicada por él, así que la variación tonal del albedo es pequeña y el efecto viene sobre todo del brillo (alfa). Si no se nota, subir el color base exige ajustar `AmbientePruebas.Asfalto_EsOscuroYLiso`.
- Alfa-mezcla de URP/Lit con propiedades puestas por código: confirmar en Unity que el charco se ve translúcido (no negro opaco) y que el horno de sondas no se afecta.
- El ruido y el parche se calcularon sin ver el resultado: puede hacer falta afinar `AmplitudTono`, `UmbralParche` y `AmplitudBrillo` tras la primera captura.

## Estado
Preparada solo estáticamente (2026-10-27): código y pruebas escritos, **sin compilar y sin abrir Unity** (otra sesión lo ocupa). Nada está verificado; ver la lista de arriba.
