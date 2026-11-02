# T-510 Paso 1 del plan de personajes y carros: personajes nuevos
Dueño: Tony | Rama: tony/T-510-personajes-nuevos | Modelo: Sonnet 5.5 | docs/plan-personajes-carros.md, paso 1
Hecho: SkullHombre, Doctor, Inventor, Hacker, RobotHombre, GuardiaNova y Mecanico importados desde imageassets/modelos3d (zip descomprimidos en Temp/, sin los "- copia") a Assets/Arte/Personajes/<Nombre>/ con su .fbm; visual Humanoid con CreadorVisualTripo (1.8 m; SkullHombre 1.95; RobotHombre y GuardiaNova 1.85); Mecanico solo importado. Papeles: SkullHombre = jefe SKULLS del Tianguis (prefab PandilleroArmado_JefeSkulls, con luz magenta); Doctor, Inventor y Hacker = DOC, INVENTOR y EL HACKER; RobotHombre = 25 % de los peatones (PeatonVariado en el prefab Peaton) y de la gente de banqueta; GuardiaNova = 3 guardias parados en Neón Alto (T510_Guardias, sin collider, a más de 8 m de la ruta calle-Casino-EL HACKER).
man negro: CreadorVisualTripo tomaba la primera textura del .fbm, que era el mapa metálico; ahora toma el albedo (TexturaDeColor). man es base de los pandilleros: SKULLS con la textura natural más oscura y franjas, SNAKES verde y NOVA azul (copias man_<color>.png con el magenta cambiado de tono), vendedor ámbar, EL TOPO rojo. Peatones, vecina y reclutables siguen saliendo de man/woman/woman2.
Animaciones: los 7 FBX nuevos NO traen clips (tomas vacías); usan los Humanoid de woman2 (Idle, Caminar, Correr) y woman (Cubrirse). man trae angry_01, agree, run, walk; woman: afraid, agree, angry_01, run, walk; woman2: agree, run, walk. Nuevos clips en los FBX: AcuerdoCompleto (agree en bucle) y Enojo (angry en bucle). Gente de banqueta (T-508) con vida: GenteDeBanqueta (Quieto respira, Platicar asiente o discute con PlayableGraph, Caminar va y vuelve); de 40 personas: 7 caminan (3 parejas y 1 solitario, de 4.5 a 6.5 m, sin chocar ni cambiar de nivel), 33 de pie; fases y rapideces distintas; sin sombra, LODGroup y CullCompletely como T-508. Peatones: arrancan la caminata en un punto distinto del ciclo.
Mixamo: no existe Assets/Arte/Personajes/Animaciones/, no se corrió CrearTodos para conectarlos (ver docs/animaciones-faltan.md).
Código: Personaje/Visual/ (GenteDeBanqueta, PeatonVariado), Visual/Editor/ (ConectarVisualesT510 con Todo, CapturasT510, cambios en CreadorVisualTripo y ConectarVisualesT520), Arte/Editor/ (ColocarT510.Aplicar, PostprocesadorAssetsT250, asmdef de Arte.Editor ahora referencia Personaje.Visual), Ciudad_Prologo, prefab Peaton.
Pruebas: EditMode PersonajesT510Pruebas (visuales, alturas, man con color, papeles, robots 1 de cada 4, gente que camina con recorrido libre, guardias) + PersonajesT520Pruebas + Mundo + Trafico: 254/254. PlayMode GenteDeBanquetaPruebas 12/12 (camina entre sus puntos con la animación Caminar, fases distintas, respira, platica, robots 1 de cada 4, brazos abajo y no en T); PlayMode PersonajeVisual+Trafico+IA 78/80: fallan PeatonesBarrioPruebas (Rutas_NoAtraviesanNingunCollider_DelBarrio y EnSesentaSegundos...) por VolumenSuburbios, VolumenNeonAlto y CY10_CargadorCalle_T505 (colliders de zona y de props de Mundo/Alan, no del visual del peatón). RecorridoDemoPruebas ver bitácora.
Capturas 1920x1080: docs/capturas/T-510-nuevos-idle.png (los 9), -man-colores, -man-skulls, -man-nova, -skullhombre, -doctor, -inventor, -hacker, -vendedor, -guardia, -banqueta (edición: el Animator no avanza, se muestrea el clip; el doctor sale en T por ese motivo, en Play está probado por prueba).

## FPS (build de medición, 1920x1080, Medio, hora 23)
| Punto | FPS | 1% bajo |
| --- | --- | --- |
| calle | 150.0 | 96.1 |
| tianguis | 146.9 | 86.7 |
| neon_calle (Neón Alto) | 200.8 | 103.7 |
| neon | 216.4 | 101.7 |
| suburbios | 234.0 | 139.1 |
Todos muy por encima de 60 (T-508: calle 152.1, neon_calle 227.1).

## Bitácora
- 2026-11-02: hecho; capturas revisadas con Read. Si se rehace ColocarT508.Colocar hay que correr otra vez ColocarT510.Aplicar.
- 2026-11-02: RecorridoDemoPruebas 1/2: NavMesh_LugaresDeLaHistoria pasa; DemoDePrincipioAFin se queda en El Tianguis (el bot cae KO en la pelea con pistola de los SKULLS, 6 bugs anotados, el mismo fallo conocido que se arregla en el paso 6).
