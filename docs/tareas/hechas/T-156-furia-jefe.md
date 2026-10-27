# T-156 Pelea de jefe: el campeón entra en furia a la mitad de su vida
Dueño: Tony | Rama: tony/T-156-furia-jefe | Modelo: Sonnet 5.5, medium
Depende de: nada

## Objetivo
El campeón de cada liga no es solo un rival con más vida: cuando su vida baja a la mitad entra en furia (pega más, ataca más seguido, se mueve
más rápido y su cuerpo pulsa en rojo), así que la pelea de jefe tiene dos tiempos y se siente distinta. Es un componente que se pone en el
prefab de cada jefe (T-157), como `PiedadRival` se pone en el rival del tutorial.

## Contexto
- Diseño: personaje-progresion.md ("el último es su campeón con pelea de jefe"), combate.md (bloqueo y contraataque con buen timing).
- Decisiones: D-064 y D-tony-66 (`PiedadRival` solo en el tutorial; la casera sin piedad). Propuesta nueva: D-tony-96 (pelea de jefe).
- Ejemplos: `IA/Rival/PiedadRival.cs` (componente que cambia `MultiplicadorDanio`, `MultiplicadorEspera`, `MultiplicadorVelocidad` de
  `RivalBasico`, pinta con `MaterialPropertyBlock` y se reinicia con `PeleadorMecha.PeleaPreparada`), `IA/Rival/ReglaPiedad.cs` (regla pura),
  `IA/Datos/ConfigPiedadRival.cs`, `IA/Editor/CreadorRivalTutorial.cs`. Pruebas: `Tests/EditMode/IA/` y `Tests/PlayMode/IA/`.

## Qué hacer
1. `IA/Datos/ConfigFuriaJefe.cs` (ScriptableObject): `umbralVida` (fracción, 0.5), `multiplicadorDanio` (1.25), `multiplicadorEspera` (0.6),
   `multiplicadorVelocidad` (1.2), `colorFuria` (rojo), `frecuenciaPulso` (Hz, 3). Asset `IA/Datos/ConfigFuriaJefe_Base.asset` con su creador.
2. `IA/Rival/ReglaFuriaJefe.cs` (C# puro): con la vida actual y la máxima dice si entra en furia (una sola vez por pelea, al llegar a
   `umbralVida` o menos sin estar en KO) y se `Reinicia`.
3. `IA/Rival/FuriaJefe.cs` (requiere `RivalBasico`, `VidaMecha`, `PeleadorMecha`): al entrar en furia aplica los multiplicadores sobre
   `RivalBasico` y pulsa el cuerpo en `colorFuria`; al preparar la pelea (revancha o pelea nueva) vuelve todo a 1 y quita el color. Evento
   `FuriaIniciada`. Si el mismo objeto tiene `PiedadRival`, avisa con un error y se apaga (no van juntos: el jefe sí se puede perder).
4. Un prefab de prueba `IA/Pruebas/RivalJefeDePrueba.prefab` (variante de `RivalBasico` con `FuriaJefe` y el config base), creado con código,
   solo para pruebas y para la escena de pruebas de Tony.
5. EditMode `Tests/EditMode/IA/ReglaFuriaJefePruebas.cs` y PlayMode `Tests/PlayMode/IA/FuriaJefePruebas.cs` (jugador real con entrada simulada o
   daño real; mide la espera entre golpes y el daño por golpe antes y después de la furia).

## Carpetas permitidas
Assets/_Proyecto/IA/ (Datos/, Rival/, Editor/, Pruebas/, Materiales/), Assets/_Proyecto/Tests/EditMode/IA/, Assets/_Proyecto/Tests/PlayMode/IA/
No tocar: Core/, `RivalBasico.prefab`, `RivalTutorial.prefab`, `RivalCasero.prefab`, `RivalPandilla.prefab`, Pelea/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [x] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [x] ReglaFuriaJefePruebas: con vida máxima 150, a 76 no hay furia; a 75 entra; a 40 no vuelve a avisar; en KO no entra; tras `Reiniciar`
      puede entrar otra vez
- [x] FuriaJefePruebas (PlayMode): con el prefab de prueba, antes de la furia el golpe que recibe el jugador baja la vida lo de
      `DanioGolpe` y después 1.25 veces eso (con 0.1 de tolerancia); la espera media entre golpes medida en 6 golpes baja al menos un 30 %
      tras la furia; `FuriaIniciada` se avisa una vez; tras `PrepararPelea` los multiplicadores vuelven a 1 y el color de furia se va
- [x] Un prefab con `FuriaJefe` y `PiedadRival` registra un error y `FuriaJefe` queda apagado (prueba con un objeto armado en la prueba)
- [x] Captura docs/capturas/T-156-1.png del jefe de prueba en furia (cuerpo rojo) visto desde la cabina
- [x] RivalBasicoPruebas, PiedadRivalPruebas y las de la pandilla siguen en verde

## Criterios de sensación (para un humano)
- En Pruebas_Tony (o con el prefab de prueba en una escena de Tony): ¿se nota el cambio a la mitad (rojo, más agresivo) y obliga a pelear
  distinto (bloquear más, contraatacar con cuidado) sin volverse injusto? ¿El pulso rojo se lee sin marear?

## Pruebas automáticas
- EditMode: ReglaFuriaJefePruebas. PlayMode: FuriaJefePruebas.

## Fuera de alcance
- Los 4 jefes reales (T-157). Ataques especiales nuevos del rival, bloqueo del rival y adaptación del rival (bloque 7). Sonido de la furia.
