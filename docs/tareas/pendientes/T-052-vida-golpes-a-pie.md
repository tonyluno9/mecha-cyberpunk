# T-052 Vida a pie y golpes simples
Dueño: Tony | Rama: tony/T-052-vida-golpes-a-pie | Modelo: Sonnet 5.5, medium
Depende de: T-051 (jugador a pie y escena). Mejor después de T-050 (comentario de daño sin `IPeleador`).

## Objetivo
El jugador a pie tiene vida y puede recibir daño, y pega golpes simples con clic izquierdo a un muñeco de prueba de tamaño
humano. Es la mitad del combate a pie básico de combate.md y lo que muestra la barra de vida del HUD (T-056).

## Contexto
- Diseño: docs/diseno/combate.md ("a pie solo hay combate básico, con pistola y golpes simples; las peleas importantes son en
  mecha"), docs/diseno/personaje-progresion.md (implante de brazos: más daño a pie, por eso el daño es dato).
- Decisiones: D-006 (`ModeloVida` en Core), D-007 (quien recibe daño publica `ImpactoRecibido`), D-009 (para no pegarse a sí
  mismo, comparar `transform.root`), D-012 (golpe en tres momentos). Propuestas: D-tony-15 (daño a pie, sin `IPeleador`),
  D-tony-17 (Personaje referencia Combate), D-tony-24 (KO a pie) en docs/decisiones-pendientes-tony.md.
- Reutiliza, sin copiar: `Combate/Golpe/CicloGolpe.cs` (preparación, impacto y recarga) y `Combate/Golpe/BarridoGolpe.cs`
  (esfera hacia adelante que ignora el propio root). Para eso `MechaCyberpunk.Personaje` pasa a referenciar
  `MechaCyberpunk.Combate` (mismo dueño, esta tarea lo pide). No edites Combate/.
- El jugador a pie NO implementa `IPeleador`: `DirectorPelea` y `RivalBasico` buscan "el IPeleador de lado Jugador" entre todos
  los MonoBehaviour y lo confundirían con el mecha. Por eso publica `EventosPelea.PublicarImpacto(null, info, resultado)`.
- Patrón para el muñeco (solo leer): Combate/Pruebas/SacoDePrueba.cs y DaniadorDePrueba.cs.

## Qué hacer
1. `DatosPersonaje`: agrega daño del golpe (15 puntos), alcance (1.6 m), radio de la esfera (0.3 m), preparación (0.12 s) y
   recarga (0.45 s). Actualiza `DatosPersonaje_Base.asset` con el creador o con código de editor.
2. `Personaje/Vida/VidaAPie.cs : MonoBehaviour, IVida, IRecibeDanio` en la raíz del jugador: usa `ModeloVida` con la vida de
   `DatosPersonaje`; cumple el contrato de `IRecibeDanio` (ignora si está en KO o la cantidad es <= 0; si no, aplica y publica
   el impacto con objetivo null). `Restaurar()` deja la vida completa.
3. KO a pie (solo lo mínimo de esta fase): al llegar a 0, `ControladorAPie.ActivarControl(false)`, la cámara baja a 0.4 m del
   piso y se ladea, y no puede atacar. `Restaurar()` devuelve el control y la cámara. Lo que pasa después (clínica) es de T-F5.
4. `Personaje/Combate/GolpeAPie.cs`: con Atacar (clic izquierdo) y el arma "puños" en la mano, usa `CicloGolpe`; en el impacto
   usa `BarridoGolpe.Buscar` desde la cámara hacia donde mira y aplica `InfoDanio` con `TipoDanio.Golpe` y atacante null.
   Visual greybox: un cubo "puño" hijo de `Manos` que avanza durante la preparación y regresa en la recarga.
   Deja una propiedad pública `ArmaActiva` o equivalente preparada para que T-053 alterne puños y pistola (por ahora siempre puños).
5. `Personaje/Pruebas/MunecoAPie.cs`: muñeco de 1.8 m con `CapsuleCollider`, `IVida` e `IRecibeDanio` (ModeloVida, 100 puntos),
   parpadea al recibir daño y se restaura solo 3 s después del KO. Prefab `Personaje/Pruebas/MunecoAPie.prefab`.
6. `Personaje/Pruebas/RevividorDePrueba.cs`: con una `InputAction` del inspector (tecla K) llama `Restaurar()` del jugador.
   Solo para escenas de prueba.
7. Creador: agrega `VidaAPie` y `GolpeAPie` al prefab; en Pruebas_APie pon dos muñecos frente al jugador y un
   `DaniadorDePrueba` (Combate/Pruebas, tecla H) apuntando al jugador, y el `RevividorDePrueba`. Regenera prefab y escena.

## Carpetas permitidas
Assets/_Proyecto/Personaje/ (incluido su asmdef), Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity,
Assets/_Proyecto/Tests/EditMode/Personaje/ y Assets/_Proyecto/Tests/PlayMode/Personaje/
No tocar: Core/, Combate/ (solo usarlo), Mecha/, las carpetas y escenas de Alan, Pruebas_Tony.unity.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (las pruebas viejas de Combate, IA y Pelea siguen pasando)
- [ ] PlayMode con entrada simulada: frente a un muñeco a 1.2 m, un clic izquierdo le quita exactamente el daño del golpe,
      y el daño llega después de la preparación (no en el mismo frame del clic)
- [ ] PlayMode con entrada simulada: un segundo clic durante la recarga no pega; después de la recarga sí
- [ ] PlayMode con entrada simulada: con el muñeco a 3 m no hay daño; con el jugador pegado a un muro no se hace daño a sí mismo
- [ ] PlayMode: `RecibirDanio` al jugador baja su vida, avisa `IVida.Cambio` y publica `ImpactoRecibido` con objetivo null y el
      resultado aplicado; en KO devuelve `Ignorado` y no publica nada; cantidad <= 0 también se ignora
- [ ] PlayMode con entrada simulada: en KO, W 2 s no lo mueve y el clic no pega; después de `Restaurar()`, W lo mueve otra vez
- [ ] EditMode por reflexión: ningún tipo del ensamblado MechaCyberpunk.Personaje implementa `IPeleador` (así `DirectorPelea`
      y `RivalBasico` nunca confunden a la persona con el mecha)
- [ ] Captura del puño a media preparación frente a un muñeco: docs/capturas/T-052-1.png
- [ ] Consola sin errores en Pruebas_APie pegándole a los muñecos y recibiendo daño con H hasta el KO y revivir con K

## Criterios de sensación (para un humano)
- El golpe se siente rápido y se entiende cuándo conecta (el muñeco parpadea en el momento justo).
- El KO a pie se entiende (la vista cae al piso) sin parecer un error.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Personaje/): VidaAPiePruebas y GolpeAPiePruebas con InputTestFixture; muñeco y piso creados en la prueba.
- EditMode (Tests/EditMode/Personaje/): PersonajeSinPeleadorPruebas (la reflexión de arriba). No hace falta otra lógica pura
  nueva (CicloGolpe y ModeloVida ya tienen sus pruebas); si separas alguna cuenta, pruébala.

## Fuera de alcance
- Pistola (T-053); HUD de vida (T-056)
- Enemigos a pie que peleen solos, bloqueo a pie, patadas, combos, animaciones de cuerpo
- Morir en la calle y despertar en la clínica (T-F5/T-F6)
