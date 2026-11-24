# TAREA: Manos de VR del protagonista (MN01) en Unity

Origen: chat de Blender, 24-nov, rama tony/blender-24nov. Esta tarea es solo de Unity.

## Reglas
- Paso 0: cierra Unity y Unity Hub, pero solo el proyecto mecha-cyberpunk (NO el de la carpeta wtVR). Confirma que no queda Unity.exe con mecha-cyberpunk y borra Temp/UnityLockfile; repítelo si vuelve a aparecer.
- NO modifiques los FBX ni las texturas que entregó Blender: todo se ajusta en Unity (import settings, prefabs, scripts).
- No corras CreadorTodo. Nunca hagas force push.

## Archivos
- Assets/Arte/Personajes/PJ01/ManosVR/MN01_ManosVR_R.fbx (mano derecha: malla + rig + 9 clips)
- Assets/Arte/Personajes/PJ01/ManosVR/MN01_ManosVR_L.fbx (mano izquierda: espejo con su propio rig + 9 clips)
- Assets/Arte/Personajes/PJ01/ManosVR/Texturas/MN01_BaseColor.jpg (sRGB)
- Assets/Arte/Personajes/PJ01/ManosVR/Texturas/MN01_Normal.jpg (Texture Type = Normal map)
- Assets/Arte/Personajes/PJ01/ManosVR/Texturas/MN01_Metallic.jpg (lineal, sRGB apagado)
- Assets/Arte/Personajes/PJ01/ManosVR/Texturas/MN01_Roughness.jpg (lineal; en URP Lit usar smoothness = 1 − roughness: invertirla en el material o con Shader Graph)
- Fuente en Blender: imageassets/blender/manos_vr/MN01_ManosVR.blend
- Capturas: docs/capturas/manos-vr-24nov-abrircerrar-f01.png, -f07.png y -f13.png
Las dos manos comparten un solo material: crear MN01_ManosVR.mat (URP Lit) con las 4 texturas.

## Escala, pivote y orientación
- Escala real: una mano adulta de 1.84 m, 19 cm de la muñeca a la punta del dedo medio. Import Scale Factor = 1 (ya viene en metros).
- Pivote (origen del objeto raíz) = la MUÑECA. Así cuadra con la pose del controlador XR; solo hay que afinar un offset de agarre.
- Orientación en Unity: +Z hacia adelante, en la dirección de los dedos; +Y arriba, con la palma hacia abajo (−Y). El antebrazo sale hacia −Z (≈ 28 cm, para la vista en primera persona).
- Si al importar sale girada 90° en X, activar "Bake Axis Conversion" en el importador del FBX; no tocar el FBX.

## Rig (Animation Type = Generic; NO Humanoid, porque es un rig de solo mano)
Huesos de la mano derecha (en la izquierda, los mismos con _L):
- Forearm_R → Hand_R
- Thumb1_R, Thumb2_R, Thumb3_R
- Index1_R, Index2_R, Index3_R
- Middle1_R, Middle2_R, Middle3_R
- Ring1_R, Ring2_R, Ring3_R
- Little1_R, Little2_R, Little3_R
Los dedos 1 cuelgan de Hand_R. Los dedos se cierran hacia la palma girando sobre su eje X local; el pulgar usa X y Z.

## Clips (los mismos 9 en los dos FBX; las poses son de 1 frame)
| Clip | Uso |
|---|---|
| Mano_Abierta | reposo / grip = 0 |
| Mano_Puno | grip = 1 sin arma |
| Mano_Gatillo | agarre cerrado con el índice apretando el gatillo |
| Mano_AgarrePistola | agarre de pistola con el índice estirado sobre el gatillo |
| Mano_AgarreArmaLarga_L | izquierda en el guardamanos del rifle (en el FBX de la derecha = abierta) |
| Mano_Cargador_L | izquierda sosteniendo un cargador (en el FBX de la derecha = abierta) |
| Mano_Granada | agarre redondo |
| Mano_Senalar | índice estirado para menús y UI |
| Mano_AbrirCerrar | prueba en loop: abre y cierra en 1 s (25 frames a 24 fps) |
Marcar Loop Time solo en Mano_AbrirCerrar.

## Cómo se usan (Animator por mano, con capas o Blend Trees)
1. Sin arma: Blend Tree 1D con el parámetro Grip (valor del grip del controlador XR, 0–1): Mano_Abierta (0) → Mano_Puno (1).
2. Gatillo: capa aditiva o con máscara solo de Index1-3. El parámetro Trigger (0–1) mezcla el índice de la pose base → Mano_Gatillo. Con arma, la base es Mano_AgarrePistola (índice estirado) y el gatillo lo dobla.
3. Pose por tipo de arma (parámetro int TipoArma o un estado por arma):
   - pistola/revólver: derecha AgarrePistola + Gatillo; izquierda Abierta/Grip o de apoyo.
   - rifle/escopeta/sniper: derecha AgarrePistola + Gatillo; izquierda AgarreArmaLarga_L sobre el guardamanos.
   - recargar: izquierda Cargador_L mientras sostiene el cargador.
   - granada: Granada en la mano que la lleva.
   - menús y UI: Senalar en la mano que apunta, con el raycast saliendo de la punta de Index3.
4. Prótesis de brazos con brillo: cuando el PJ01 tenga implante o prótesis activa, aplicar a MN01_ManosVR.mat (o a una variante MN01_ManosVR_Protesis.mat) emisión cian en el antebrazo y el guante, con un pulso suave. El modelo no trae máscara de emisión: hacerla con Shader Graph (emisión × fresnel, o una máscara pintada en Unity). No modificar las texturas de Blender.
5. Las manos se hacen hijas de los anchors de los controladores XR, con un offset local de agarre para cada arma.

## Cómo comprobarlo
- En una escena de prueba: las dos manos en el origen, con Mano_AbrirCerrar en loop. Se abren y cierran sin torcerse ni romper el guante, y se ven igual que en las capturas.
- Con un controlador XR o simulado: el grip mezcla Abierta→Puño y el trigger dobla solo el índice.
- Cada clip de pose aplicado a las dos manos: la izquierda es espejo exacto de la derecha.
- Tamaño: la mano mide unos 19 cm de la muñeca a la punta junto a un cubo de 0.2 m, y la muñeca queda en el pivote del controlador.
- Pruebas EditMode: los 2 FBX existen, cada uno tiene 17 huesos con los nombres de arriba y los 9 clips.
- Captura en VR o en Game view en docs/capturas/.
