# T-745 Combate y efectos: dron derribable, barriles, trazo de balas, explosiones del Particle Pack
Dueño: Tony | Rama: tony/T-745-combate-efectos | Modelo: Sonnet 5.5, medium
Depende de: T-730 (borrador "t729-wip"), T-691 (fuego), T-710 (dron dispara)

## Objetivo
Pendientes de la sesión 1 de docs/playtest-07nov.md: el dron se derriba a balazos y baja la búsqueda, los barriles explotan al dispararles, el trazo de
toda bala termina donde pega, y las explosiones y el humo usan los flipbook del Unity Particle Pack con materiales URP.

## Qué se hizo
1. Dron (Trafico): del stash "t729-wip" solo se aplicó lo de T-730 (DronMundo, GestorPersecucion, referencia a Vehiculos). Tiene esfera de impacto, vida
   (ConfigPersecucion.VidaDron), cae girando, explota en chico y llama a Enfriar(CalorAlDerribarDron). Las hélices son parte del mismo mesh: no se animan ni se separan.
2. Barriles: `BarrilExplosivo` (IRecibeDanio, IObjetoInerte). Daño por área TipoDanio.Explosion (el jugador a pie se prende con QuemaduraJugador), empuje a
   Rigidbody, llamas en el suelo 8 s, reacción en cadena. `InstaladorBarriles` se lo pone al cargar cada escena a los objetos "Barril*" o "*Tambo*" con collider
   (sin tocar escenas ni prefabs de la ciudad).
3. Trazo: `TrazadorBala` + `ReglaTrazo`: estela corta que viaja de la boca al destino y se acaba en el punto de impacto; sin impacto se desvanece al alcance
   máximo. Lo usan PistolaAPie (jugador), PistolaPandillero (pandilleros, policía a pie, aliados) y el dron.
4. Efectos: `EfectosExplosion` es la única entrada (explosión, humo, fuego, llamas de suelo). Usa prefabs del pack (Tiny/Small/BigExplosion, SmokeEffect,
   TinyFlames, MediumFlames, SparksEffect) por `CatalogoEfectos`. ExplosionCarro, el humo y fuego de DanioCarro, el dron y los barriles pasan por ella.
   `CreadorEfectosT745` pasó a URP (Particles/Unlit) los 4 materiales del pack que no lo eran (Flame02, FireFly, SandSwirls, GoopMist) y creó Trazo_URP,
   Chamusco_URP y el catálogo.

## Decisiones
- Las áreas de dueños distintos no se referencian y Core no se toca en esta tarea: la librería de efectos vive en Vehiculos/Efectos (la usan Vehiculos y
  Trafico) y el trazo está duplicado en Combate/Efectos (lo usan Personaje e IA). Pendiente de una tarea de Core: promover EfectosExplosion y TrazadorBala a un contrato de Core.
- La mancha del suelo es una partícula plana con la textura SmokePuff02 del pack (el pack no trae mancha de quemadura).
- En la explosión diminuta (dron) se quita la cúpula Shockwave del prefab: era enorme frente a su bola de fuego.
- ReglasEnsambladosPruebas: se permitió Tests.PlayMode.Personaje -> Trafico y Vehiculos (prueba de integración con el jugador real).
- No se suma calor por reventar un barril (los carros sí): queda como posible mejora.

## Pruebas
- EditMode: Combate/EfectosT745Pruebas (reglas del trazo), Vehiculos/EfectosT745Pruebas (barriles por nombre, instalación, catálogo, materiales URP sin rosa).
- PlayMode: Personaje/EfectosCombateT745Pruebas con entrada simulada (pistola con teclas y clic): derribar al dron baja la búsqueda y explota; barril baleado
  explota, daña, empuja, deja llamas y encadena; trazo termina en la pared y se apaga; sin impacto se desvanece; explosiones del pack con materiales URP; carro con humo y explosión.
- Actualizadas por el cambio: DronDisparaT710Pruebas (trazo) y ExplosionCarroPruebas (los flipbook emiten con retraso).

## Capturas (docs/capturas/)
s1e-T-745-dron-antes, dron-cayendo, dron-explota, barril-explota, trazo-pared, carro-humo, carro-explosion-humo, carro-humo-despues.
