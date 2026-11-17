# T-1324: armas, balas, francotirador y rayo

Rama `tony/T-1324-armas`. Escrita sin compilar ni correr Unity (regla de la noche): el orquestador compila y corre las pruebas.

## Qué se hizo
1. **Sin balas no se apunta**: `ControladorAPie.ApuntarBloqueado` (lo pone `PistolaAPie`, regla pura `ReglaApuntarConBalas`). Con el cargador vacío y apuntar pulsado, empieza la recarga sola (si hay reserva). El rayo de plasma ya en marcha no se corta.
2. **Francotirador**: ya era hitscan (sin caída) y tenía zoom + `MiraSniper`. Se cierra el zoom (FOV 15 a 8), el precio sube a 2200 (oferta y arma) y con el zoom la ayuda de apuntado ya no empuja la inclinación (mira libre arriba y abajo).
3. **Policías que no mueren (causa)**: `PistolaAPie.BuscarImpacto` usaba `RaycastNonAlloc` con buffer de 16; con 150 m de alcance en calles con muchos colliders el buffer no traía el más cercano y la bala pasaba de largo. Ahora `ReglaImpactoBala.BuscarPrimero` avanza con rayos simples (siempre el más cercano) saltando al jugador y a su carro. Hipótesis a confirmar en vivo: policías dentro de la patrulla quedan tapados por el collider del carro (no se tocó).
4. **Rayo de plasma**: daño 400/470/550/650 por segundo (antes 120 a 200); atraviesa hasta 3 paredes u objetos (`ConfigArmasEspeciales.paredesQueAtraviesa`) además de toda la gente; objetos x3; a una persona le hace su vida x20: un toque la mata (`ReglaDanioRayo`). Aplica a toda persona con `IVida`, no solo a policías.

## Aplicar
`MechaCyberpunk.Personaje.Editor.AjustarArmasT1324.Aplicar` (idempotente): fovSniper, precio del sniper (DatosArma y DatosOferta) y daño del plasma. `CreadorArmas` ya lleva los mismos números.

## Pruebas
EditMode `MechaCyberpunk.Tests.Personaje.ArmasT1324EditPruebas` (8): reglas de apuntar, daño del rayo, penetración e impacto con más de 16 colliders. Sin PlayMode con entrada simulada: no se pudo correr nada para validarla esta noche.

## Decisiones
- Apuntar bloqueado también aplica a granada y lanzagranadas (cargador vacío tras lanzar).
- Mata-gente genérico por `IVida` porque Personaje no referencia IA.

## Assets que faltan
Ninguno. Mira de francotirador: se usa la existente (`MiraSniper`, solo está en la escena T-416; revisar que esté en las demás).
