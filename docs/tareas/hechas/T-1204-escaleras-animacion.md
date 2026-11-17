# T-1204: animaciones para subir y bajar escaleras de mano

Rama `tony/T-1204-escaleras-animacion`. Pedido del 17-nov, punto 5, con el ajuste de Tony: usar los clips PJ01_Escalera_* hechos en Blender sobre PJ01.

## Qué se hizo
- `ConfigAnimacionEscalera` (Personaje/Datos/Resources): clips entrar, subir, bajar, salir arriba y salir abajo, y metros por ciclo (0.9 m).
- `ImportarAnimacionesEscalera` (editor; menú Mecha Cyberpunk > T-1204):
  - Copia `imageassets/blender/pj01_clips/PJ01_Escalera_{Entrar,Subir,Bajar,SalirArriba,SalirAbajo}.fbx` a `Assets/Arte/Personajes/PJ01/Clips/`.
  - Los importa con `ImportadorClipsPJ01`, como los demás clips de PJ01 (Humanoid, avatar de PJ01, escala 1.8367). Subir y Bajar en bucle.
  - Acepta también `imageassets/animaciones/Escalera_Subir.fbx` y `Escalera_Bajar.fbx` de Mixamo; si existen los de PJ01, tienen prioridad.
  - Al abrir Unity importa solo los que falten (no en batchmode).
- `AnimacionEscalera` (en el jugador; la agrega `TreparAPie`) juega los clips con un PlayableGraph sobre el Animator del jugador:
  - Entrar al engancharse desde el piso.
  - Subir y Bajar con el tiempo atado a los metros trepados.
  - SalirArriba durante el paso al techo, que ahora dura lo que el clip.
  - SalirAbajo al soltarse en el piso.
- `TreparAPie`: estado de bajar. Desde el techo, en la salida de una escalera (o junto a su cima), mirando a la calle y con W, se engancha arriba y baja mientras sostengas W; al soltar W, W sube y S baja.
- `docs/pedido-animaciones.md`: qué clip se juega cuándo y cómo cambiarlos.

## Evidencia
- PlayMode, corrido por clases:
  - `EscalerasTechosT1101Pruebas`: `Escalera_ConClipsDePJ01_JuegaCadaTramo` (los 5 clips en su tramo), `Techo_BajarPorLaEscalera_DesdeArriba` (3 escaleras), `Escaleras_Todas_LlevanAUnTechoConPiso` (30), `Techos_CaminarDespuesDeSubir_NoRegresaNiEmpuja` (30 de 30) y las demás pasan.
  - `TreparAPie` y `TechosS2`: 11/11.
- EditMode completo: 2581/2581. `ClipsPJ01JugadorT985` ya no cuenta los PJ01_Escalera_* entre los clips del controlador (no van ahí), y una prueba nueva revisa que los 5 estén en la config.
- Capturas revisadas en `docs/capturas/t1204/`: subiendo, saliendo-arriba (cruza el pretil), bajando y saliendo-abajo.

## Notas
- `Escaleras_Todas_LlevanAUnTechoConPiso` sube a 30 techos y con el clip de salir arriba tarda más de 3 min: se le dio un límite de 15 min.
- Para revisar jugando: cómo se ve SalirArriba sobre el pretil.
