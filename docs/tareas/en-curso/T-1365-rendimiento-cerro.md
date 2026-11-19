# T-1365: rendimiento de la Colonia del Cerro (LOD, sombras, luces)

Rama tony/T-1360-barrido-cerro-puerto (sesión s2, sin commit propio). Parte de la medición de T-1362: el Cerro tiene 7400 renderers y 76.6 M triángulos LOD0; las 440 casas del kit pesan 52 M (118 mil por casa) y las 241 casas de Tripo 16 M. Meta: 50 FPS o más en todos los puntos de `RendimientoCerroT1362Pruebas`.

## Hecho
- `Assets/Arte/Editor/RendimientoCerroT1365.cs`, `Aplicar()` (batch, idempotente). Solo cambia `Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab`:
  - **Casas del kit (`Casas/Casa_N`)**: LODGroup de 2 niveles. LOD0 = todos los renderers (hasta 0.12 de alto en pantalla). LOD1 = `Cuerpo`, `AzoteaT1318`, `VentanaCalida` (los vidrios que se prenden de noche, 2 triángulos cada uno), y base y tinaco si pesan 3000 triángulos o menos; se apaga por debajo de 0.012. Fachadas, pretiles, aires, antenas, toldos, focos y letreros solo en LOD0.
  - **Casas de Tripo (`CC0x_CasaCerro`)**: LODGroup de un solo nivel, culled por debajo de 0.012. Los hijos `Extra_*` (T-1364) entran en el mismo LOD0.
  - **Detalles pesados** (`CC04_`, `CC05_`, y lo que en `Detalles` o `Carros` se llame Carro, Aire, Antena o Tinaco): LODGroup de un nivel, culled a 0.04 (carros 0.03).
  - **Sombras**: en las casas del kit solo proyecta el `Cuerpo`; en las de Tripo todo salvo lo `Extra_*` de menos de 1.5 m; en detalles y carros solo lo de 1.5 m o más. La ladera, el terreno y los postes no se tocan.
  - **Luces** (sin direccionales): sombras en tiempo real apagadas (0 con sombra, el tope es 4) y alcance máximo 18 m.
  - Escribe `Logs/t1365-rendimiento.txt`: LODGroups, renderers y triángulos por LOD, sombras apagadas, luces cambiadas y las 25 mallas más pesadas con su `lodCount`.
- `Combinar()` aparte (NO se llama desde Aplicar): junta por celda de 40 x 40 m y por material los `Cuerpo` y `AzoteaT1318` en `Combinado_T1365` (hijo de la raíz), mallas en `Assets/_Proyecto/Mundo/Datos/Zonas/T1365_Cerro_Combinado.asset`; los renderers originales quedan con `enabled = false` y siguen ahí sus colliders. Correr después de Aplicar, y solo si no alcanza.
- Prueba EditMode `Assets/_Proyecto/Tests/EditMode/Mundo/RendimientoCerroT1365Pruebas.cs`: cada casa tiene LODGroup, ningún renderer de las casas queda fuera de todo LOD, triángulos de LOD1 menos del 15 % de LOD0, luces con sombra 4 o menos y alcance hasta 18 m. Fallan hasta que se corre Aplicar.

## Cómo correr
`-executeMethod MechaCyberpunk.Arte.Editor.RendimientoCerroT1365.Aplicar` (después de T-1363 y T-1364), luego la medición `T1362_SUFIJO=despues`.

## Mesh LOD de Unity 6.3: lo que averigüé
- API real (UnityEngine.CoreModule.xml del editor): `Mesh.lodCount`, `Mesh.GetIndexCount(submesh, meshLod)`, `Mesh.GetLods`, `Mesh.lodSelectionCurve`, `Renderer.meshLodSelectionBias`, `QualitySettings.meshLodThreshold` (en el proyecto: Mobile 8, Medio 4, Ultra 1.5), `ModelImporter.generateMeshLods`.
- **La medición de T-1362 tiene un error**: busca por reflexión `GetLodSubMesh`, que no existe, así que reportó "MeshLOD no" y LOD bajo = LOD0 para todo. No prueba que no haya Mesh LOD. Además `GetIndexCount(sub)` sin nivel puede contar todos los niveles juntos (el doc dice que con -1 es "el sub-mesh entero, que puede incluir varios LOD"), así que los 76.6 M podrían estar inflados. `Logs/t1365-rendimiento.txt` trae los `lodCount` reales.
- Importadores: `generateMeshLods: 1` en C01/C02/C03 (fachadas y pisos altos de `Assets/Arte/Calle/`), C05_PretilAzotea, CC01, CC02, CC04; **`0` en C20b_PisoDosVentanas_cy** (`Assets/Arte/Calle/`). No los cambié (el encargo solo lo permitía si era imprescindible).
- Uso en ejecución con URP: no pude confirmarlo sin correr Unity. Lo que sé: el nivel se escoge por renderer según su tamaño en pantalla y `meshLodThreshold` de la calidad activa (no hay ajuste en el asset URP; el GPU Resident Drawer está apagado, `m_GPUResidentDrawerMode: 0`). No hay forma documentada de forzar el nivel más bajo; por eso en LOD1 no se meten piezas pesadas. Posible conflicto: todo el Cerro está marcado BatchingStatic; el batching estático junta mallas y podría anular el Mesh LOD en la partida (sin verificar).

## Suposiciones
- Los umbrales de LOD1 son: LOD0 en 0.12, LOD1 con corte en 0.012 (el encargo dice "hasta ~0.02" y "culled por debajo de ~0.012"; se tomó 0.012 para los dos).
- El Cuerpo queda 4 cm detrás del plano del muro de las fachadas, así que en LOD1 no hay hueco visible; se pierden puertas y ventanas en relieve (quedan las ventanas luminosas).
- Un cuarto de azotea de Tripo (`Extra_*Cuarto*`) no entra en LOD1 si pesa más de 3000 triángulos (no se puede forzar su Mesh LOD).
- Los postes (98 renderers, 13 mil triángulos cada poste) no están en el encargo y no se tocaron.
- Si una pieza trae un LODGroup interno, se apaga (`enabled = false`) para no pelear los renderers.
- Sugerencia: reducir en Blender CC01/CC02 (68 mil triángulos cada una, 241 casas) si el LOD de Unity no basta.

## Pendiente
Medición después de Aplicar: pegar aquí la tabla de `Logs/t1362-rendimiento-despues.txt` y `Logs/t1365-rendimiento.txt`. Revisar en capturas que las sombras de las casas no se vean raras.
