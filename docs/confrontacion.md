# Confrontación por facción (T26 y T27)

Regla de oro: nadie te cae encima de golpe. Primero advierten, luego empujan, y la pelea de verdad (golpes, armas) empieza cuando TÚ atacas.
Cada facción tiene un asset `ConfigConfrontacion` en `Assets/_Proyecto/IA/Resources/Confrontacion/` (se crean con
`MechaCyberpunk.IA.Editor.AplicarConfrontacionT1651.Aplicar`). Tony ajusta ahí los números; el código no los repite.

## Fases de SKULLS y SNAKES (maleantes de calle)

Código: `ReglaConfrontacion` (la máquina), `ConfrontacionGrupo` (turnos de voz y empujón por grupo), `PandillaAPie` y `PandilleroAPie` (el movimiento).

1. **Reposo**: no te han detectado. Hacen su vida (patrulla, plática).
2. **Advertencia** (te acercas a `distanciaDeteccion`): durante `segundosAdvertencia` se acercan un poco (hasta `distanciaAcercamiento`), provocan y gritan.
   Una frase cada `segundosEntreFrases`, una sola voz a la vez en el grupo (la dice el más cercano a ti), con subtítulo. Una cosa a la vez:
   o caminan o hacen la burla; mientras la hacen se quedan quietos (`segundosGesto`). En la última frase suena SKU_ULTIMO_AVISO.
3. **Empujón** (pasó la advertencia y sigues a `distanciaQuedarse` o menos): solo empujan (casi sin daño) y cada `empujonesParaTirar` te tiran al
   suelo (`AtropelloJugador.Derribar`). Uno a la vez, cada `segundosEntreEmpujones`. Tras `empujonesMaximos` se aburren y se retiran. No sacan golpes ni disparan.
4. **Pelea**: solo si TÚ atacas (les pegas, sacas el arma o disparas cerca). Entonces todo el grupo usa la IA de combate de siempre (T-1528: daño y
   precisión suben con el tiempo).
5. **Retirada**: si te alejas más de `distanciaDejarDeSeguir`, o se terminó la advertencia y estás lejos, o se aburrieron, vuelven a su lugar. No te
   advierten otra vez hasta que salgas de `distanciaDeteccion` (mínimo `segundosRetirada`).

Frases: `FrasesConfrontacion` (español de barrio). IDs de voz: SKU_PROVOCAR, SKU_INSULTO, SKU_ULTIMO_AVISO. Los SNAKES no tienen voces SNK en el catálogo: solo
subtítulo y burbuja sobre la cabeza.

| Valor | SKULLS | SNAKES |
| --- | --- | --- |
| Detección (m) | 12 | 14 |
| Advertencia (s) | 20 | 14 |
| Quedarse (m) | 10 | 11 |
| Dejar de seguir (m) | 22 | 24 |
| Frase cada (s) | 5 | 4.5 |
| Saca el arma al advertir | no | sí (sin apuntar) |
| Empujón cada (s) / tira cada / máximo | 4 / 2 / 4 | 3.5 / 2 / 5 |

## Otras facciones

- **Civiles**: nunca atacan; huyen (T11). Sin config propia.
- **Policía**: solo por delitos del jugador (búsqueda). Avisa con altavoz, persigue y dispara. Sin arresto (T25). Se retira cuando llega Nova (T17).
  Lo llevan Trafico y `ConductaRetiradaPolicia`; `Config_Policia` solo documenta (`SoloPorDelitos`).
- **Nova**: sin advertencia, ataca directo. Solo aparece con 5 estrellas o en la historia; cae en cápsula (T7, T17, `RespuestaNova`). `Config_Nova` solo documenta (`SinAdvertencia`).
- **Guardias de lugares** (los guardias armados que reaccionan a disparos, `reaccionaADisparos`): si sacas el arma a menos de `distanciaDeteccion` te advierten
  (frase con subtítulo, una a la vez entre todos los guardias, cada `segundosEntreFrases`; desde la tercera, "última advertencia"). Solo atacan si disparas o
  les pegas (T-1494, `ReglaPercepcionDisparo`). `Config_Guardias`, `SoloSiDisparas`.

## Probar en Play

- `MechaCyberpunk.IA.APie.PandillaAPie.EstadoConfrontacionMasCercano()` devuelve fase, tiempo, distancia, empujones y orador del grupo más cercano.
- En el log: `[Confrontacion] <grupo> <fase>` en cada cambio de fase y `[Confrontacion] guardia ... advierte`.
- Sin los assets de Resources, SKULLS y SNAKES usan la IA anterior de T-1528 (provocar sin límite).
