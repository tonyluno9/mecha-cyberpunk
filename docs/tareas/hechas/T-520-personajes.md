# T-520 Paso B1: personajes de Tripo en el juego
Dueño: Tony | Rama: tony/T-520-personajes | Modelo: Sonnet 5.5 | docs/plan-ciudad-ui-personajes.md, paso B1
Hecho: woman, man y woman2 importados Humanoid (avatar válido de su propio esqueleto) y a 1.8 m; prefabs Visual_woman, Visual_man y Visual_woman2 (Personaje/Visual/Prefabs, con nodo Giro de 180°, Animator, VisualPersonaje, ApuntadoIK y socket de arma en la mano); conectados en lugar del muñeco de cubos a JugadorAPie (mujer por defecto, hombre si lo eliges: ModeloSegunPerfil cambia el visual y pasa las armas), Aliado (woman2 teñida con el color de tu pandilla), PandilleroArmado (man; SKULLS negro con franjas magenta, jefes 1.15x, el jefe SKULLS con brillo magenta), Peaton (woman2, 4 colores al azar), vecina Doña Lupe, Vendedor, los 3 reclutables y los 5 personajes de apoyo (DOC azul médico, INVENTOR naranja, HACKER turquesa, ESTRADA violeta, CANEPA amarillo). La pantalla de creación muestra woman/man (MUJER/HOMBRE). docs/animaciones-faltan.md lista las 10 animaciones que faltan para Mixamo y cómo dejarlas (carpeta Assets/Arte/Personajes/Animaciones/ y CreadorVisualTripo.CrearTodos).
Código: Personaje/Visual/ (ApuntadoIK, VarianteVisual, ajustes a VisualPersonaje y ReglasVisual), Visual/Editor/ (CreadorVisualTripo, ConectarVisualesT520 con `Todo()`, CapturaPersonajes, PruebaVisualT520), Personaje/Jugador/IModeloCambiable, ModeloSegunPerfil, AliadoAPie (mezcla del color), PostprocesadorAssetsT250 (escala por userData y carpeta de animaciones).
Pruebas: EditMode PersonajesT520Pruebas (16) + CreadorVisualPruebas + ImportarT500Pruebas, todo verde; PlayMode JugadorVisualPruebas 4/4 (camina, corre, apunta con IK, KO, cambio a hombre con entrada simulada), CubiertaYRodadaPruebas y VisualPersonajePruebas verdes; RecorridoDemoPruebas ver bitácora.
Capturas 1920x1080: docs/capturas/T-520-1.png (protagonista woman), -2 (man), -3 (aliado), -4 (SKULLS), -5 (jefe SKULLS), -6 (Doña Lupe), -7 (vendedor), -8 (4 peatones), -9 (DOC), -10 (INVENTOR), -11 (HACKER), -12 y -13 (apuntando pistola y arma larga).

## FPS (build de medición, 1920x1080, hora 23, calidad Media)
| Punto | FPS promedio | 1% bajo |
| --- | --- | --- |
| calle (peatones de noche) | 209.7 | 113.4 |
| tianguis (SKULLS) | 243.7 | 176.5 |
| tiroteo (pandilleros disparando) | 313.2 | 205.2 |
| suburbios | 394.3 | 270.8 |
Todo muy por encima de 60 (antes de T-520: calle 260 y suburbios 463 en T-501; los visuales con huesos cuestan unos 50 fps en la calle).

## Qué se ajustó
- Escala: PostprocesadorArte de Alan fuerza globalScale a 1; la escala (1.41, 1.37, 1.34) va en el userData del FBX y la aplica PostprocesadorAssetsT250.
- Pies sin deslizarse: se mide la velocidad real de Caminar (0.8-0.9 m/s) y Correr (2.9-3.3 m/s) con movimiento de raíz y VisualPersonaje acelera o frena el clip (0.6x a 2.2x) según la velocidad del juego.
- Animator con los nombres de parámetros del muñeco de T-400 (EnPiso, Arma, KO, Disparo, Recarga): AnimacionJugador, PandilleroAPie y AliadoAPie no cambian.
- El cuerpo de man es negro de textura: no admite colores, por eso los NPC de color usan woman/woman2.

## Bitácora
- 2026-11-01: hecho; PlayMode Personaje: BarrioAlanPruebas y RecorridoAPiePruebas ("No llegó a la entrada del Bar") fallan igual con el prefab viejo del jugador (error anterior a T-520); ApuntadoAsistido y ArmasMercado/ArmasPruebas pasan al repetir (azarosas).
