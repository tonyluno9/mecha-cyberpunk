# T-744 Aliados AL10 y AL11 (B6)
Dueño: Tony | Rama: tony/T-744-aliados-al10-al11 | Integrada en tony/s1-09nov
Fuente (solo lectura): imageassets/modelos3d/personajes/AL10/AL10.zip y AL11/AL11.zip (fbx de Tripo + .fbm con basecolor, normal, metallic, roughness, rm).

## Qué cambió
- Assets/Arte/Personajes/AL10 y AL11: fbx (renombrado), Texturas/ALxx_basecolor.jpg y ALxx_normal.jpg (como PJ01; las demás texturas no se usan), Materiales/ALxx_Base.mat (URP Lit con color y normal).
- Importación igual que PJ01 (T-732): Humanoid con el avatar de su propio esqueleto (78 huesos), userData "escala=": AL10 1.844 y AL11 1.839. PostprocesadorAssetsT250 los lista como personajes de Tripo.
- CreadorAliadosT744 (Personaje/Visual/Editor): reutiliza CreadorPJ01T732.AvatarDe y CreadorVisualTripo.AltoDelModelo; crea Visual_AL10 y Visual_AL11 (copias de Visual_woman2: ya traen el controlador del papel Aliados, IK de apuntado y de pies) y cambia el modelo de los prefabs de aliado.
  Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CreadorAliadosT744.Crear [-escalaAL10 x -escalaAL11 y] y luego .ConectarEscena.
- IA/Prefabs/Aliado.prefab usa AL10; Aliado_AL11.prefab (nuevo) usa AL11. Mismo AliadoAPie, pistola, CharacterController y AliadoEnCarro: el comportamiento no se tocó.
- PandillaJugador: campo prefabAliadoSegundo; los miembros impares (el 2.º, el 4.º...) usan el segundo prefab (alternan), si está vacío todos usan el primero. Ciudad_Prologo.unity lo tiene conectado (una línea).

## Decisión
Los aliados son genéricos (un solo prefab Aliado): el primero que reclutas es AL10 (hombre con gorra) y el segundo AL11 (mujer con trenzas); el tercero vuelve a AL10.

## Altura
Medida con BakeMesh en Play (EscalaJugadorT670Pruebas.AlturaAnimada): primero con la escala de reposo (1.66) midió 1.622 m animado; con 1.844 / 1.839 miden 1.791 m los dos (en la lista de T-670 con relativa 1.00).

## Animaciones
Ninguna nueva: usan el override del papel Aliados (Loco_Caminar, Loco_Correr, apuntar, disparo, subir y bajar del carro de T-706) retargeteados por Humanoid. No faltó ninguna.
Nota: el aliado no muestra arma en la mano (solo el láser de apuntado); era así con woman2.

## Pruebas
- EditMode AliadosAL10AL11T744Pruebas (7): Humanoid con avatar válido y escala guardada, visual con su material y controlador, prefabs de aliado con AL10 y AL11. Suite completa 2218/2218.
- PlayMode AliadosAL10AL11T744Pruebas (3, jugador real): 1.º AL10 y 2.º AL11 con avatar válido y 1.80 m animado, no en T-pose, te siguen caminando (Velocidad > 0.5), disparan al pandillero sin cambiar de altura.
- PlayMode AliadoCarroT706Pruebas + AliadosPruebas (5/5): subir y bajar del carro y seguir/pelear con el prefab nuevo.
- Capturas: docs/capturas/s1e-T-744-caminando.png, -parados.png, -disparando.png (aliados junto a PJ01) y las de T-706 regeneradas con AL10 (s1c-T-706-aliado-*.png). Sin T-pose, sin malla rota, material correcto.
