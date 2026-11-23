# Confrontación por facción (T26 y T27)

Regla de oro: no atacan de primeras. Primero te ven y te miran; si te quedas cerca, UNO se acerca despacio y te empuja; la pelea de verdad empieza
cuando TÚ atacas a cualquiera del grupo. Sin voces ni frases al confrontar (T-1659 / T41).
Cada facción tiene un asset `ConfigConfrontacion` en `Assets/_Proyecto/IA/Resources/Confrontacion/` (se crean con
`MechaCyberpunk.IA.Editor.AplicarConfrontacionT1651.Aplicar`). Tony ajusta ahí los números; el código no los repite.

## Fases de SKULLS y SNAKES (maleantes de calle)

Código: `ReglaConfrontacion` (la máquina pura), `ConfrontacionGrupo` (quién se acerca y el turno de empujón), `PandillaAPie` y `PandilleroAPie` (el movimiento).
Los grupos de acoso (`GrupoAcoso`, bullying) quedan fuera: no usan confrontación.

1. **Reposo**: no te han visto. Hacen su vida.
2. **Mirando** (entras a `distanciaDeteccion`, ~20 m): todos giran el cuerpo hacia ti y te miran. Nadie se mueve. El reloj de `segundosCerca` (20 s)
   solo corre mientras estás a `distanciaCerca` (~8 m) o menos y se reinicia si sales de ahí. Si te vas más allá de `distanciaDeteccion`, vuelven a Reposo.
3. **Acercamiento** (pasaron `segundosCerca`): UNO solo (el más cercano a ti en ese momento; si cae, toma su lugar el siguiente más cercano) camina hacia ti a
   `velocidadCaminar` (~1.2 m/s) hasta `distanciaAcercamiento` (~2.2 m). Los demás siguen mirando, quietos.
4. **Empujón** (llegó): ese uno te empuja (casi sin daño) cada `segundosEntreEmpujones` y cada `empujonesParaTirar` te tira al suelo. Tras `empujonesMaximos` se aburre y se retiran.
5. **Pelea**: solo si TÚ atacas (le pegas a ese uno o a cualquiera del grupo, sacas el arma o disparas cerca). Entonces todo el grupo usa la IA de combate de siempre
   (T-1528: daño y precisión suben con el tiempo).
6. **Retirada**: si en Acercamiento o Empujón te alejas más de `distanciaDejarDeSeguir` (~14 m) se acaba todo y regresan a su lugar. No vuelven a mirarte hasta que salgas
   de `distanciaDeteccion` (mínimo `segundosRetirada`).

| Valor | SKULLS | SNAKES |
| --- | --- | --- |
| Te ven (`distanciaDeteccion`, m) | 20 | 20 |
| Cerca (`distanciaCerca`, m) | 8 | 8 |
| Segundos cerca (`segundosCerca`) | 20 | 20 |
| Dejar de seguir (m) | 14 | 14 |
| Caminar (m/s) / llegar a (m) | 1.2 / 2.2 | 1.2 / 2.2 |
| Saca el arma al mirar | no | sí (sin apuntar) |
| Empujón cada (s) / tira cada / máximo | 4 / 2 / 4 | 3.5 / 2 / 5 |

`FrasesConfrontacion` sigue existiendo (los guardias la usan), pero los maleantes de calle ya no dicen nada al confrontar.

## Otras facciones

- **Civiles**: nunca atacan; huyen (T11). Sin config propia.
- **Policía**: solo por delitos del jugador (búsqueda). Avisa con altavoz, persigue y dispara. Sin arresto (T25). Se retira cuando llega Nova (T17).
  Lo llevan Trafico y `ConductaRetiradaPolicia`; `Config_Policia` solo documenta (`SoloPorDelitos`).
- **Nova**: sin advertencia, ataca directo. Solo aparece con 5 estrellas o en la historia; cae en cápsula (T7, T17, `RespuestaNova`). `Config_Nova` solo documenta (`SinAdvertencia`).
- **Guardias de lugares** (los guardias armados que reaccionan a disparos, `reaccionaADisparos`): si sacas el arma a menos de `distanciaDeteccion` te advierten
  (frase con subtítulo, una a la vez entre todos los guardias, cada `segundosEntreFrases`; desde la tercera, "última advertencia"). Solo atacan si disparas o
  les pegas (T-1494, `ReglaPercepcionDisparo`). `Config_Guardias`, `SoloSiDisparas`.

## Probar en Play

- `MechaCyberpunk.IA.APie.ConfrontacionGrupo.DepurarConfrontacion()` (estático; también `PandillaAPie.DepurarConfrontacion()`) lista cada grupo con confrontación: fase,
  segundos en la fase, segundos cerca (x/20), distancia, empujones y el nombre del que se acerca.
- `PandillaAPie.EstadoConfrontacionMasCercano()` da solo el del grupo más cercano.
- En el log: `[Confrontacion] <grupo> <fase>` en cada cambio de fase.
- Sin los assets de Resources, SKULLS y SNAKES usan la IA anterior de T-1528 (provocar sin límite).
- Los assets deben actualizarse corriendo `AplicarConfrontacionT1651.Aplicar` (los campos viejos `segundosAdvertencia` y `distanciaQuedarse` pasan a `segundosCerca` y `distanciaCerca`).
