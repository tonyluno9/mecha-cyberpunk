# Reporte sesión 2, noche del 7-nov (docs/plan-look-ciudad.md: que el juego se vea como K01)

Trabajé de 03:00 a 07:20 en la carpeta principal. Cada tarea va en su rama `tony/T-87x-*`, con sus pruebas, merge de `origin/main`,
merge `--no-ff` a main y push. Como `main` está abierto en el worktree de mecha-s1, la integración es en HEAD separado sobre `origin/main`,
igual que el 6-nov. Plan y decisiones: docs/plan-noche-07nov-s2.md. Lo que cambió del mapa, para la sesión 1: docs/cambios-mapa-07nov.md.
Assets que faltan: docs/pedido-arte-08nov.md.

## Lo que quedó (todo en main)

| Tarea | Qué | Capturas (referencia \| juego) |
| --- | --- | --- |
| T-870 | **Calle de prueba de K01**: calle nueva sin tráfico dentro de la manzana de la casa (z = -43), de 11.5 m de fachada a fachada, con casas de 2 pisos del kit C (local con cortina y toldo, vivienda con reja y macetas, piso alto en 4 colores, pretil, tinaco sobre base de ladrillo, foco de pared, letrero de neón chico sin texto), postes C07 con luz de sodio que sale de la lámpara, halo en la niebla y transformador, cables colgantes, charcos que reflejan, carro viejo estacionado. **El look de toda la ciudad**: posproceso de noche ACES con bloom, sombras azules y luces naranjas; niebla azul de noche (0.02); contornos finos de pantalla completa (pase del renderer con profundidad y normales); cielo azul marino sin el brillo morado; ventanas cálidas al azar; reflejo planar de los charcos (solo pinta si se ve un charco). | s2c-calle-prueba-1 (antes) y s2c-calle-prueba-16 (al final) |
| T-871 | **Kit de manzanas**: 6 frentes de cuadra de 60 m (A vivienda, B locales, C mixta, D talleres, E neón, F popular), esquina de K05 (farmacia con letrero FARMACIA y cruz verde, tienda, semáforo, banqueta en L, cruce peatonal) y frente de Nova de K06 (torres T20–T23, banqueta de 4 m, bolardos, cámaras, luz fría). Prefabs en Assets/Arte/Mundo/S2c/Kit/. | s2c-kit-esquina, s2c-kit-nova, s2c-kit-frente-* |
| T-872 | **Suburbios en cuadras cortas** (ruta de la demo): calle de norte a sur (x = -110.5) que cruza la de K01 con las cuatro esquinas de K05; la calle de K01 sigue al oriente cruzando la calle de la casa hasta x = 47; ~80 casas del kit en lugar de 9 edificios greybox. Se quedan la casa, el I+D+I del Doc, el callejón de los SKULLS con su salida, el taller El Canepa, el bar y el CIC. | s2c-suburbios-k05, s2c-suburbios-k01-oriente, s2c-suburbios-* |
| T-873 | **El Mercado (K02)**: el tianguis de la avenida queda entre casas con locales del kit (fuera los edificios 10, 15, 47 y 52), con cables que cruzan y charcos. Sin focos colgando. | s2c-mercado-k02 |
| T-874 | **Neón Alto (K03)**: ~30 neones magenta y turquesa en las fachadas de la calle del casino, cables, charcos; las luces verdes del barrio pasan a magenta y turquesa (M11). | s2c-neon-alto-k03 |
| T-875 | **Nova (K06)**, lo mínimo: calle ancha de Nova en la Manzana_6 con torres, bolardos, cámaras y luz fría. Las piezas de fachada del kit ya no chocan (choca el cuerpo de la casa), así los peatones no pegan con toldos. | s2c-nova-k06 |

## Decisiones que tomé sin preguntar
- **La §8 del plan no existe** (llega a la §6): usé la §4 ("Mapa nuevo") y el mapa M11.
- **La red de calles con tráfico no se movió.** El tráfico, las patrullas, la ruta del mapa y los peatones (código de la sesión 1) están
  escritos para la rejilla de 3 × 2 manzanas de 80 m. Moverla dejaría carros y peatones pasando por dentro de las casas. Las cuadras cortas
  salen de partir las manzanas con calles nuevas sin tráfico y de cambiar lo que hay a los lados. Por eso **no hay glorieta todavía**: la
  única forma de ponerla es en un cruce con tráfico, y eso necesita que la sesión 1 adapte el grafo de calles (está en cambios-mapa).
- **El Doc, Tino y El Canepa no se movieron**: ya están cerca de la casa como pide M11 (Canepa enfrente, el Doc a ~90 m, Tino a ~95 m).
- **Los barrios quedan donde ya estaban** los lugares de la historia: Suburbios al suroeste (casa), El Mercado al centro (tianguis), Neón
  Alto al noreste (casino) y Nova al sureste (Manzana_6, que no tenía lugares). En M11 Nova está al noroeste; ahí está el CIC de Tino.
- **Niebla de noche más densa** (de 0.009 a 0.02), como en K01. Si tapa demasiado, es un número en ConfigCicloDia_Mundo.
- **Las casas son un poco más bajas que las piezas del kit** (88 %): las de K01 tienen planta baja de ~3.5 m.
- **Pruebas viejas que cambié** porque fijaban lo de antes: la niebla, el cielo, el ambiente y las nubes de noche (CieloDiurnoPruebas,
  CalculoCieloTextura), Neón Alto verde → turquesa (NeonAltoT505Pruebas), "Suburbios sigue con el edificio 41" (AssetsNuevosT540Pruebas).

## Pruebas
- **EditMode de Mundo: 311 de 311 en verde.**
- **PlayMode de Mundo: 183 de 194.** Las 11 que fallan ya fallaban en la base (cámara libre, horarios del mercado negro, el estacionamiento
  de la casa y otras). Ninguna es nueva.
- **PlayMode de Demo: 1 de 2.** `RecorridoDemoPruebas.DemoDePrincipioAFin` ya fallaba en la base.
- **PlayMode de Tráfico: 72 de 74.** Las 2 que fallan son nuevas respecto a la base, pero **no cargan la ciudad** (arman su propia escena):
  `PersecucionBarrioPruebas.Patrulla_...` ("la patrulla no lleva colliders", la patrulla ahora trae 1) y `PeatonHuyePruebas.Peaton_Alcanza...`.
  Vienen de los cambios de carros y policía de la sesión 1 (T-695, T-696), no del mundo.
- Las corridas largas de PlayMode en segundo plano las cortó la sesión por poca memoria (con dos Unity abiertos). El Unity de la prueba siguió
  solo y leí su resultado al terminar.

## Pendiente / para Tony
- **Aprobar la captura de K01** (s2c-calle-prueba-16). Lo que más falta para que sea igual: el HUD nuevo (es de la sesión 1), la cámara del
  jugador (de la sesión 1: K01 encuadra con el personaje a 1/4 de la pantalla), gente en la calle y los assets de docs/pedido-arte-08nov.md
  (base de tinaco, neones de bandera, semáforo, alcantarilla, basura, muro trasero, letreros de Nova).
- **Glorieta y plazas** (M11): necesitan que la sesión 1 cambie el grafo de calles del tráfico; está explicado en cambios-mapa.
- **Barrios 2 a 4**: quedó lo que se ve desde la calle principal de cada uno. Lo demás sigue como estaba.
- **La torre Nova** no se ve al fondo de la calle de Nova como en K06.
- Los semáforos no cambian de color (las micas son fijas: roja prendida).

## Cómo rehacer todo
Menú Mecha Cyberpunk/S2c, en orden: 0 Look, 3 Suburbios (rehace también la calle de prueba), 4 Mercado, 5 Neón Alto, 6 Nova
(y 2 Kit para los prefabs). Todo es idempotente: cada uno rehace su raíz S2c_* en Ciudad_Prologo. Nunca regenera Barrio.prefab.
Unity no quedó abierto en esta carpeta.
