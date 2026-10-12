# T-013 Vida del mecha y recibir daño
Dueño: Tony | Rama: tony/T-013-vida-recibir-danio | Modelo: Sonnet 5.5, medium
Depende de: T-001, T-011

## Objetivo
El mecha del jugador tiene vida, recibe daño por el contrato de Core, la muestra en una pantalla de la cabina,
la cabina se sacude con cada impacto y al llegar a 0 queda en KO sin control. Con esto el rival (T-021) puede pegarle
y el flujo de pelea (T-022) puede usar el prefab del jugador.

## Contexto
- Diseño: docs/diseno/combate.md y docs/diseno/estetica-hud.md (en el mecha nada flota; todo está en la cabina).
- Decisiones: D-006 (ModeloVida), D-007 (EventosPelea), D-011 (pantallas diegéticas).
- Contratos de Core: `IRecibeDanio`, `InfoDanio`, `ResultadoDanio`, `IVida`, `ModeloVida`, `IPeleador`, `LadoPelea`,
  `EventosPelea.PublicarImpacto` y `EventosPelea.ImpactoRecibido`, `DatosMecha`.
- Lee el contrato escrito en los comentarios de `IRecibeDanio` y `IPeleador` antes de implementarlos.
- Parte de MechaJugador.prefab (T-010, T-011). La pantalla va en `AnclaPantallaVida` del tablero.
- T-012 va después y agrega el bloqueo dentro de `VidaMecha`, así que deja `RecibirDanio` simple y legible.
- Ensamblados: Combate/ y UI/ solo referencian Core; no agregues referencias entre áreas.

## Qué hacer
1. `Combate/Vida/VidaMecha.cs` (MonoBehaviour, `IVida` e `IRecibeDanio`): usa un `ModeloVida` creado en `Awake`
   con `DatosMecha.VidaMaxima`, reenvía sus eventos y cumple el contrato de `IRecibeDanio`, incluido publicar
   `ImpactoRecibido` con el `IPeleador` del mismo objeto. Agrega `public void Restaurar()` y
   `public void Configurar(DatosMecha datos)` (lo usan las pruebas y el muñeco de T-012).
2. `Combate/Peleador/PeleadorMecha.cs` (MonoBehaviour, `IPeleador`): `Lado` es un campo serializado, `Nombre` sale de
   `DatosMecha.Nombre`, `Vida` es el `IVida` del mismo objeto. `ActivarControl` activa o desactiva una lista serializada
   de `Behaviour` (por ahora `ControladorMecha`; T-012 agrega los suyos). `PrepararPelea` cumple el contrato de Core.
   Al recibir el KO de su vida se quita el control solo.
3. `Mecha/Cabina/SacudidaCabina.cs`: escucha `EventosPelea.ImpactoRecibido` y, cuando el objetivo es su propio peleador,
   sacude un objeto intermedio `Sacudida` que contiene la cabina y la cámara. La intensidad crece con el daño aplicado y
   se apaga en una fracción de segundo; un impacto con 0 de daño da una sacudida mínima. La cuenta va en lógica pura
   (`CalculoSacudida`: intensidad según daño y cómo decae con `dt`) y los valores en `Mecha/Datos/ConfigSacudidaCabina.cs`
   con su asset. No muevas la cámara directamente: `ControladorMecha` ya la usa para mirar y mecerse.
4. `UI/Cabina/PantallaVidaCabina.cs`: Canvas World Space en `AnclaPantallaVida` con una barra (Image con relleno) y un
   texto "VIDA 100" (uGUI `Text`, D-010). Se actualiza con el evento `Cambio` del `IVida`, no en cada frame.
   Colores en un `Config*` de UI/Datos/.
5. `Combate/Pruebas/DaniadorDePrueba.cs`: con una `InputAction` configurada en el inspector (tecla H), aplica
   un daño configurable a un objetivo con `IRecibeDanio`. Solo va en Pruebas_Tony.unity.
6. Arma el prefab: `VidaMecha` y `PeleadorMecha` (lado Jugador, con `ControladorMecha` en la lista) en la raíz,
   la sacudida y la pantalla. Pon el `DaniadorDePrueba` en la escena apuntando al jugador.

## Carpetas permitidas
Assets/_Proyecto/Combate/, Assets/_Proyecto/Mecha/, Assets/_Proyecto/UI/, Assets/_Proyecto/Tests/EditMode/Combate/,
Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/Combate/ y Assets/_Proyecto/Escenas/Pruebas_Tony.unity
No tocar: Core/, Arena/, IA/, otras escenas ni ProjectSettings/.

## Criterios de aceptación (se comprueban jugando)
- [ ] Al dar Play veo en el tablero una barra llena y "VIDA 100"
- [ ] Cada vez que presiono H la barra y el número bajan lo que dice el DaniadorDePrueba, y la cabina se sacude
- [ ] Un daño grande sacude más que uno chico, y la sacudida se apaga sola en menos de un segundo
- [ ] Al llegar a 0 el mecha ya no camina ni gira, y la consola muestra un solo aviso de KO
- [ ] Presionar H después del KO no baja la vida de 0, no sacude y no da errores
- [ ] Si cambio `vidaMaxima` en DatosMecha_Jugador.asset a 50, el KO llega con la mitad de golpes
- [ ] No aparecen errores en la consola

## Pruebas automáticas
- EditMode (Tests/EditMode/Mecha/): `CalculoSacudida` da más intensidad a más daño, una intensidad mínima con daño 0,
  decae hasta 0 sin quedar negativa y con `dt` de 0 no cambia.
- PlayMode (Tests/PlayMode/Combate/): `VidaMecha` con un `DatosMecha` creado con `CreateInstance`: recibir daño baja la vida
  y publica `ImpactoRecibido` una vez; con daño 0 o en KO devuelve `Ignorado` y no publica; `Restaurar` vuelve al máximo.
  Corre PlayMode con el comando de CLAUDE.md cambiando `-testPlatform`.

## Fuera de alcance
- Bloqueo y golpe (T-012)
- LED rojo, cristal cuarteado y mecha más lento con vida baja
- Daño persistente entre peleas, reparaciones, kits de reparación
- Sonido de impacto

## Bloqueada
Bloqueada: depende de T-002 (herramientas de verificación), que no está verificada ni integrada en tony/fase-1. Ver docs/revision-tony.md.
