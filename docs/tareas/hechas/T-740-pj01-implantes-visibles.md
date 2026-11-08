# T-740 PJ01 implantes visibles (A9)
Dueño: Tony | Rama: tony/T-740-pj01-implantes-visibles | Integrada en tony/s1-09nov
Fuente: imageassets/blender/implantes/PJ01_Implantes.fbx (no trae LEEME ni texturas).

## Qué trae el FBX
Siete mallas ya pesadas (SkinnedMesh) a los mismos 78 huesos de PJ01, con los mismos nombres (BoneRoot, Hip, Waist, Spine01...): PJ01_Malla (el cuerpo, no se usa), IMP01_LVL1 e IMP01_LVL2 (piernas), IMP02_LVL1 e IMP02_LVL2 (brazos)
y IMP03_LVL1 e IMP03_LVL2 (visor de cabeza: ojo biónico y goggles; es el espacio Ojos). El guantelete de IMP02 viene dentro de la misma malla (no son tres objetos). Sistema nervioso no tiene pieza.
Los materiales vienen sin textura; los ids (tripo_mat_a681686b...) son los de los zips de Tripo imageassets/modelos3d/IMP0xLVLy.zip, de donde salen las texturas (Assets/Arte/Personajes/PJ01/Implantes/Texturas/IMP0x_LVLy.jpg).

## Decisión: SkinnedMesh con el esqueleto de PJ01 (no pegar a huesos)
Como vienen pesadas a los huesos de PJ01, cada pieza entra como SkinnedMeshRenderer con los huesos del modelo del jugador buscados por nombre y rootBone BoneRoot. Así siguen cualquier clip (Sin y ConImplante) y salen en el espejo como el resto del cuerpo.
- CreadorImplantesPJ01T740.Crear (editor, repetible) arma el contenedor Implantes dentro de Giro/Modelo de Visual_PJ01, con 6 hijos ocultos y el componente ImplantesVisiblesPJ01. Materiales URP con su textura en .../Implantes/Materiales.
- Escala: el FBX mide 0.98 m. Como PJ01 vive en una carpeta de personajes de Tripo, el postprocesador toma la escala de userData ("escala=1.88"). Sin ese userData las piezas salían a la mitad (cadera a 0.515 m contra 0.968 m) y quedaban DENTRO del cuerpo. Prueba EditMode de que miden igual.
- ImplantesVisiblesPJ01 lee Registro<IImplantes> (Core) en Update, igual que HudImplantes: Piernas = IMP01, Brazos = IMP02, Ojos = IMP03; se ve solo la pieza del nivel instalado (el 2 reemplaza al 1). El muñequito del HUD y HudImplantes leen el mismo servicio, así que van en sincronía (prueba).
- El espejo es un reflejo de la escena (EspejoReflejo), por lo que muestra las piezas solo; captura en docs/capturas/pj01-implantes-espejo-frente.png y -lado.png con Arte/Editor/FotosEspejoImplantesT740 (las piezas se prenden a mano en edición).

## Revisión de atravesados y suela
- Suela de IMP01 nivel 2 medida en Play (BakeMesh): y = 0.032 m contra los zapatos del cuerpo 0.037 m (a 5 mm, a ras; el cuerpo del jugador queda unos 3.7 cm sobre el piso desde antes, no se tocó).
- Capturas quieto, caminando, corriendo y apuntando, de frente y de lado, de cada nivel y de todos juntos (nivel 1 y 2): se revisaron las de IMP01 LVL2, IMP02 LVL1/LVL2 y todos LVL2. Caminando y corriendo con los clips ConImplante no atraviesan; con IMP02 nivel 2 al apuntar la hombrera y el brazalete rozan el brazo del arma (poco, en el codo). IMP02 nivel 1 corriendo se ve limpio.

## Pruebas
- EditMode ImplantesPJ01T740EditPruebas 2/2 (escala igual que PJ01, seis piezas con 78 huesos del modelo, textura, ocultas, ligadas a su espacio). EditMode total 2130/2130.
- PlayMode ImplantesVisiblesPJ01T740Pruebas: piezas por nivel y combinaciones (sistema nervioso sin pieza), HUD sincronizado, suela a ras y piezas que siguen al cuerpo; más una prueba de capturas (72 imágenes docs/capturas/pj01-implantes-*.png con CAPTURAR_S1=1).
