# T-012 Golpe y bloqueo
Dueño: Tony | Rama: tony/T-012-golpe-bloqueo | Modelo: Sonnet 5.5, medium
Depende de: T-001, T-011, T-013

## Objetivo
El jugador golpea con el brazo derecho y bloquea cruzando los brazos, con una ventana de tiempo que premia subir la
guardia justo antes del impacto. Es la mitad del jugador en la meta de la fase 1: golpear a un rival que te golpea.

## Contexto
- Diseño: docs/diseno/combate.md ("los brazos son de precisión: golpes, bloqueos y contraataques";
  "puedo bloquear un golpe y contraatacar con buen timing"; el contraataque queda para otra fase).
- Decisiones: D-009 (ignorar el propio `transform.root`), D-012 (reglas del golpe y del bloqueo).
- Contratos de Core: `IRecibeDanio`, `InfoDanio`, `ResultadoDanio`, `TipoBloqueo`, `IPeleador`, `DatosMecha`
  (DanioGolpe, AlcanceGolpe, PreparacionGolpe, RecargaGolpe, TiempoSubirGuardia, ReduccionBloqueo, VentanaBloqueoPerfecto).
- Entrada: acciones Golpear y Bloquear de Mecha/Entrada/ControlesMecha.inputactions (T-010).
- Brazos: `BrazoIzquierdo`, `BrazoDerecho` y `Puno` del prefab (T-011). Vida: `VidaMecha` y `PeleadorMecha` (T-013).

## Qué hacer
1. Lógica pura en `Combate/Golpe/CicloGolpe.cs`: estados Listo, Preparando, Recuperando. `IntentarGolpear()` solo
   funciona en Listo. `Avanzar(dt)` devuelve si en este frame ocurrió el impacto, que pasa una sola vez al terminar
   `PreparacionGolpe`; después espera `RecargaGolpe` y vuelve a Listo. Expone el progreso (0 a 1) para animar.
2. Lógica pura en `Combate/Bloqueo/CalculoBloqueo.cs`: recibe el daño, si la guardia está arriba, cuántos segundos lleva
   arriba y los valores de `DatosMecha`, y devuelve el daño final y el `TipoBloqueo`:
   guardia abajo o todavía subiendo: daño completo, Ninguno; arriba hace menos de `VentanaBloqueoPerfecto`: 0, Perfecto;
   arriba desde antes: daño × (1 − `ReduccionBloqueo`), Normal.
3. `Combate/Bloqueo/GuardiaMecha.cs`: mientras se mantiene Bloquear, la guardia sube en `TiempoSubirGuardia` y se queda
   arriba; al soltar baja. Expone si está arriba y desde cuándo. No sube mientras un golpe está en Preparando.
4. `Combate/Golpe/GolpeMecha.cs`: con Golpear usa `CicloGolpe`; no golpea si la guardia está arriba o subiendo.
   En el impacto busca hacia adelante hasta `AlcanceGolpe` (un SphereCast desde el centro del mecha), ignora los
   colliders de su propio `transform.root`, y al primer `IRecibeDanio` (con `GetComponentInParent`) le manda un `InfoDanio`
   con su `IPeleador` como atacante.
5. `Combate/Brazos/AnimadorBrazos.cs`: mueve los pivotes de los brazos por código, sin Animator: el brazo derecho se echa
   un poco atrás en la preparación, sale al frente en el impacto y regresa en la recarga; con la guardia, los dos brazos se
   cruzan frente al cristal. Las poses y velocidades van en `Combate/Datos/ConfigBrazos.cs` con su asset.
6. Cambia `VidaMecha.RecibirDanio` para que, si el objeto tiene `GuardiaMecha`, aplique `CalculoBloqueo` y devuelva el
   `TipoBloqueo` en el resultado. Una línea en la consola dice "Bloqueo normal" o "Bloqueo perfecto".
7. Agrega `GolpeMecha` y `GuardiaMecha` a la lista de control de `PeleadorMecha` en el prefab.
8. Herramientas de prueba en `Combate/Pruebas/`, solo en Pruebas_Tony.unity:
   - Un `SacoDePrueba`: un cubo de unos 4 m con `VidaMecha` y `PeleadorMecha` (lado Rival) y un
     `DatosMecha_SacoPrueba.asset`. Escribe en la consola el daño que recibe.
   - `AtacanteDePrueba.cs`: un cubo frente al jugador que cada cierto tiempo (campo serializado) se pinta de naranja
     durante su `PreparacionGolpe` y luego golpea al jugador si está dentro de su `AlcanceGolpe`.
     Usa `DatosMecha_AtacantePrueba.asset` con `preparacionGolpe` de 0.6 s para que se pueda reaccionar.

## Carpetas permitidas
Assets/_Proyecto/Combate/, Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab, Assets/_Proyecto/Tests/EditMode/Combate/,
Assets/_Proyecto/Tests/PlayMode/Combate/ y Assets/_Proyecto/Escenas/Pruebas_Tony.unity
No tocar: Core/, Arena/, IA/, UI/, otras escenas ni ProjectSettings/.

## Criterios de aceptación (se comprueban jugando)
- [ ] Con clic izquierdo, el brazo derecho se echa atrás, sale al frente y regresa
- [ ] Si hago clic muchas veces seguidas, los golpes salen a su ritmo de recarga y no se enciman
- [ ] Al golpear el saco a menos de 3 m, la consola muestra 10 de daño por golpe; desde más lejos no le hago nada
- [ ] Mis golpes nunca bajan mi propia vida
- [ ] Manteniendo clic derecho los brazos se cruzan; mientras, el clic izquierdo no golpea
- [ ] Contra el atacante de prueba sin guardia, mi barra baja el daño completo
- [ ] Con la guardia arriba desde antes de que el cubo se ponga naranja, la barra baja menos y la consola dice "Bloqueo normal"
- [ ] Si subo la guardia en la última parte del aviso naranja, justo antes del golpe, la barra no baja y la consola dice "Bloqueo perfecto"
- [ ] Durante el KO no puedo golpear ni bloquear
- [ ] No aparecen errores en la consola en 3 minutos contra el atacante y el saco

## Pruebas automáticas
EditMode, en Tests/EditMode/Combate/:
- CicloGolpe: el impacto ocurre una sola vez y justo después de la preparación; no se puede golpear antes de terminar la
  recarga; después de la recarga se puede otra vez; un `dt` grande no produce dos impactos.
- CalculoBloqueo: guardia abajo da daño completo; guardia subiendo da daño completo; dentro de la ventana da 0 y Perfecto;
  fuera de la ventana da el daño reducido y Normal; los bordes exactos de la ventana dan siempre el mismo resultado.

## Fuera de alcance
- Contraataque, golpe del brazo izquierdo, combos
- Misiles, habilidades, munición
- Sonido, partículas, vibración de cámara al golpear

## Bloqueada
Bloqueada: depende de T-002 (herramientas de verificación), que no está verificada ni integrada en tony/fase-1. Ver docs/revision-tony.md.
