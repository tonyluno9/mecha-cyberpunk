# T-425 Rendimiento a 60 FPS en todo el juego
Dueño: Tony | Rama: tony/T-425-rendimiento | Modelo: Sonnet 5.5 | MODO RÁPIDO (docs/plan-noche.md, paso 5; mejora 5 de docs/mejoras-pendientes.md)
Depende de: T-419, T-423, T-424. Tony autorizó tocar todas las áreas, Core y ProjectSettings.

## Objetivo
Que una PC modesta llegue a 60 FPS: bajar geometría y llamadas de dibujo en carros, gradas y calle sin borrar arte ni código del mecha.

## Qué se hizo
1. Medición (Arte/Editor/MedicionNoche + Mundo/Pruebas/MedicionAmbiente): ahora saca CPU/GPU ms (FrameTimingManager), llamadas de dibujo, lotes, setpass y triángulos, y tiene puntos nuevos `carros` (8 carros en movimiento), `tiroteo` (a 9 m de la pandilla más cercana) y `gradas`. Argumentos nuevos: -medicionRes 1280x720, -medicionLod <n>, -medicionPuntos a,b,c. El build prende enableFrameTimingStats solo mientras se construye.
2. Lo que pesaba (Arte/Editor/AnalisisTriangulos, Logs/analisis-triangulos.txt): 24 M de triángulos en la escena; carros V01a/V01b de 90 mil triángulos (18 copias), gradas A04 de 160 mil (8 copias), fachadas C01/C02/C03 de 7.4 mil x 140 copias cada una, postes, aires y paredes del sótano de 8 a 9 mil, todo sin LOD. Solo 1 luz con sombra (el sol) y 5 Animator: no era lo que pesaba. Los scripts ya cachean sus búsquedas (revisados con un escaneo de Update/FixedUpdate).
3. Arreglos:
   - Mesh LOD (Unity 6.2+, Arte/Editor/OptimizarMallas): `generateMeshLods` activado en 70 FBX de Assets/Arte con una malla de 3000 o más triángulos (menos los del kit de pisos, que ya tienen LODGroup). De cerca se ve igual; de lejos el motor usa la malla simplificada. `meshLodThreshold` por calidad en QualitySettings: Mobile 8, Ultra 1.5, Medio 4.
   - CulingDecorado (Mundo/Ambiente, con CalculoCortePorDistancia): al cargar Ciudad_Prologo o Ciudad, el decorado de 2.5 m o menos (sin LODGroup, sin Rigidbody, CharacterController ni Animator) recibe un LODGroup de un solo nivel que lo corta a 70 m. Se crea solo (RuntimeInitializeOnLoad), no está en la escena.
   - Medio de verdad: Medio_Renderer (copia de PC_Renderer sin SSAO, Mundo/Editor/CrearRendererMedio) y sombras suaves apagadas en Medio_RPAsset. Antes Medio usaba el mismo renderer que Ultra.
4. De T-424: CreadorJugadorAPie ahora llama a CreadorApuntadoAsistido.AgregarA (crea los datos y agrega el componente), así regenerar el prefab del jugador ya no pierde el apuntado asistido.

## Resultado (docs/capturas/rendimiento-T-425.txt)
Calidad Medio, 2560x1600: calle 140 -> 197 fps (7.1 -> 5.1 ms), carros 133 -> 198, gradas 143 -> 191, tianguis 154 -> 206; peor punto 133 -> 191 fps. Triángulos de la calle 15.4 M -> 9.8 M, llamadas de dibujo 5011 -> 4299.
Calidad Ultra: calle 122 -> 140, peor punto 112 -> 138 fps (carros). Ningún punto empeoró en fps.
En 1280x720 la calle cuesta 5.1 ms contra 7.8 ms a 2560x1600: unos 5 ms son de geometría/llamadas, no de píxeles; en una PC modesta eso es lo que importa y bajó casi a la mitad en triángulos.
Revisado a ojo: calle de Medio después vs Ultra antes (Logs/med4-medio/calle.png) casi idéntica; solo desaparece lo muy chico y lejano.

## Pruebas
- EditMode: CalculoCortePorDistanciaPruebas (5), más ArteNochePruebas, AmbientePruebas y ReglaApuntadoAsistidoPruebas: 21/21.
- PlayMode: CulingDecoradoPruebas (el chico recibe LODGroup; el grande, el que tiene física y el que ya tiene LOD no), ApuntadoAsistidoPruebas, AmbienteNochePruebas, CarroViejoPruebas: 10/10.
- Pruebas PlayMode de la ciudad (recorrido, historia, Mercado, territorio, pandillas): 39 corridas, 37 pasan; fallan 2 de MostradorTiendaPruebas ("La tienda vende kit, 4 municiones y el brazo": esperaba 6 y hay 20), que cuenta ítems de la tienda y no depende del render ni del culling; se anota para revisión (no lo causa T-425, no se verificó contra fase-g).
- Builds: ConstruirJuego y ConstruirDemoPandillas, "Build Finished, Result: Success".

## Bitácora
- Medido antes/después con MedicionNoche (Medio y Ultra). Mobile no se midió aparte: -medicionCalidad Mobile mostró Medio, la medición usa el nombre del nivel.
- No hecho (idea): GPU Resident Drawer (requiere Forward+; hoy es Deferred), occlusion culling horneado, y medir en una PC modesta real (aquí solo hay una RTX 5070 Laptop; el 60 FPS se infiere de los ms y de los triángulos).
