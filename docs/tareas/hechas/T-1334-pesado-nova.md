# T-1334 Medir al pesado de Nova (playtest: "el enemigo con escudo es chico")

Rama: tony/T-1334-pesado-nova. Escrito sin abrir Unity (no compilado ni probado por el implementador).

## Qué se hizo y por qué
En T-1323 `ConfigRespuestaNova.escalaPesado` (1.15) se puso por estimación. Ahora hay una prueba que mide la altura real (BakeMesh con el Animator en AlwaysAnimate,
tras caer y quedar en idle) del guardia y del pesado, sin escudo/armas, y sugiere la escala.

## Pruebas
- PlayMode `MechaCyberpunk.Tests.EscalaPesadoT1334Pruebas.PesadoConEscudo_MideComoElGuardia_MasMenos4Porciento`: loguea `[T-1334] guardia=... pesado=... escalaActual=... escalaSugerida=...`
  y afirma que el pesado queda a +-4 % del guardia.

## Cómo aplicar la medida
Cambiar `escalaPesado` en `Assets/_Proyecto/IA/Resources/ConfigRespuestaNova.asset` (campo `ConfigRespuestaNova.escalaPesado`) por `escalaSugerida`. El código escala el hijo `Modelo`
(`RespuestaNova.Aparecer`). Si el log muestra que la sugerida sale rara (por ejemplo muy lejos de 1.15) la causa no es solo la escala: puede ser la animación de escudo agachada o la unidad del modelo E12.
