# T-202 FINTAS y RETIRADA: el rival que te estudió engaña tu guardia y esquiva tu castigo
Dueño: Tony | Rama: tony/T-202-fintas-retirada | Modelo: Sonnet 5.5, medium
Depende de: T-200, T-201 (tocan RivalBasico y el mismo creador: van una tras otra)

## Objetivo
Un rival que aprendió FINTAS alarga su aviso al azar y a veces amaga (enseña el golpe y lo guarda), así que cubrirte con buen timing ya no
basta y conviene atacar tú. Uno que aprendió RETIRADA da un paso atrás después de su golpe, así que el contragolpe tardío se queda corto y
conviene castigar rápido, atacar primero o usar una habilidad de alcance. Ninguno pega más ni corre más que antes: el aviso nunca se acorta y
el paso atrás va a su propia velocidad o menos.

## Contexto
- Diseño: combate.md ("Puedo bloquear un golpe y contraatacar con buen timing").
- Decisiones: D-012 (la preparación del rival es su aviso visible), D-tony-168 (OJOS leen `Preparando` y `SegundosParaAtacar`). Propuestas:
  D-tony-192, D-tony-196 (FINTAS: extra 0.25 / 0.4 s, amago 0.2 / 0.35 al 60 % del aviso, 0.3 s después; RETIRADA: 0.45 / 0.7, demora 0.15 s,
  hasta tu alcance + 0.75 m, velocidad × 0.8 / 0.9, a lo más su recarga), D-tony-197 e y f.
- Código: `Combate/Golpe/CicloGolpe.cs` (preparación fija por constructor), `IA/Rival/RivalBasico.cs` (`Impactar`, `Impacto`, `ciclo`, movimiento
  y `MultiplicadorVelocidad`), `IA/Rival/CerebroRival.cs` (espera entre golpes), `IA/Rival/HabilidadRival.cs` (la habilidad de los jefes usa
  `Detenido`), `IA/Adaptacion/AdaptacionRival.cs` (T-200), `IA/Adaptacion/GuardiaRival.cs` (T-201, modelo de componente con regla pura y `FijarAzar`).
- Pruebas modelo: `Tests/PlayMode/IA/GuardiaRivalPruebas.cs` (T-201), `Tests/PlayMode/Progresion/Implantes/OjosSinapsisPruebas.cs` (medir avisos
  por cuadro).

## Qué hacer
1. `CicloGolpe`: sobrecarga `IntentarGolpear(float preparacionEstaVez)` (la de siempre usa la del constructor) y `PreparacionActual`; `Progreso`
   usa la actual. CicloGolpePruebas viejas siguen en verde.
2. `RivalBasico`, solo ganchos (como los de la piedad; sin decidir nada aquí):
   - `Func<float> PreparacionExtra` (s, ≥ 0; se pide al empezar cada golpe y se suma a la de su `DatosMecha`);
   - `bool CancelarGolpe(float esperaHastaElSiguiente)`: solo en Preparando; vuelve a Listo sin impacto, brazo en reposo, y la espera del
     cerebro pasa a ese valor (`CerebroRival.FijarEspera` si hace falta); eventos `GolpeIniciado` y `GolpeCancelado`;
   - `IniciarRetirada(float distancia, float segundosMaximos, float factorVelocidad)` y `Retirandose`: camina hacia atrás mirando al objetivo,
     con su aceleración y velocidad máxima × factor (≤ 1, se limita), hasta quedar a esa distancia, pasar esos segundos o perder el control;
     mientras, no empieza golpes.
   - `ILecturaRival`: `Preparando` sigue en verdadero durante un amago (los OJOS no lo distinguen: D-tony-201 f); `SegundosParaAtacar` da −1
     durante la retirada.
3. FINTAS: `IA/Adaptacion/ReglaFintas.cs` (pura, azar inyectado): al empezar cada golpe, extra = azar × `extraAvisoMaximo[nivel − 1]`; amago si
   el azar sale menor que `probabilidadAmago[nivel − 1]` y el golpe anterior no fue amago; el amago se cancela al `fraccionAmago` × la preparación
   de serie y deja `esperaTrasAmago`; el golpe que sigue a un amago es real y su aviso nunca baja de la preparación de serie. Nivel 0: extra 0
   y nunca amago. `IA/Adaptacion/FintasRival.cs` la aplica con los ganchos y llama a `AvisarUso(Fintas)` en cada amago. Las fintas son solo del
   golpe: la habilidad de los jefes no cambia.
4. RETIRADA: `IA/Adaptacion/ReglaRetirada.cs` (pura): tras cada impacto del rival (pegue o no), si el azar sale menor que
   `probabilidadRetirada[nivel − 1]`, pide retirarse a los `demoraRetirada` s, hasta el alcance del golpe del jugador (de la `VidaMecha` de su
   objetivo) + `margenRetirada`, por a lo más la recarga del rival, con `factorVelocidadRetirada[nivel − 1]`. Nivel 0: nunca.
   `IA/Adaptacion/RetiradaRival.cs` la aplica y llama a `AvisarUso(Retirada)`. Los dos componentes tienen `FijarAzar(Func<float>)` para pruebas
   y van en `controlados`.
5. `CreadorAdaptacionRival` agrega `FintasRival` y `RetiradaRival` a los prefabs con `AdaptacionRival` (idempotente).
6. Pruebas:
   - EditMode `Tests/EditMode/IA/ReglaFintasPruebas.cs` y `ReglaRetiradaPruebas.cs` (extra en sus límites, nunca dos amagos seguidos, el aviso que
     sigue a un amago, nivel 0 neutro, distancia y tiempo de la retirada); `Tests/EditMode/Combate/` para la sobrecarga de `CicloGolpe`.
   - PlayMode `Tests/PlayMode/IA/FintasRetiradaPruebas.cs` (arena + flujo + MechaJugador, `RivalLiga1_1` y `RivalLiga4_5`, entrada simulada con el
     ratón; atajos anotados: niveles fijados en `MemoriaRivales`, `FijarAzar`, `Time.timeScale`):
     a) nivel 0 en todo: en 10 golpes la preparación medida es la del asset (±1 cuadro) y nunca se retira (igual que antes);
     b) FINTAS I con azar 1: el aviso medido es preparación + `extraAvisoMaximo` (±1 cuadro); con azar 0: amago (el brazo se echa atrás y vuelve
        sin ningún `ImpactoRecibido` en el jugador), el golpe siguiente llega con aviso ≥ la preparación de serie, y en 20 golpes nunca hay dos
        amagos seguidos;
     c) un jugador que sube la guardia a tiempo para el aviso de serie: con FINTAS I y azar natural, la fracción de bloqueos perfectos en 20
        golpes baja frente a nivel 0 (se imprimen las dos);
     d) RETIRADA con azar 0 (siempre): tras el impacto del rival, la distancia llega a alcance del jugador + margen − 0.1 m antes de que acabe su
        recarga; su velocidad nunca pasa de velocidad máxima × factor; contra TORNILLO y EL CACIQUE, un golpe de serie soltado 0.3 s después del
        impacto del rival conecta 10 de 10 y uno soltado 0.9 s después conecta 3 de 10 o menos;
     e) DON FIERRO (`RivalLiga1_5`, con EMBESTIDA) con FINTAS I y RETIRADA I: su habilidad, su aviso de 0.6 s y su enfriamiento no cambian;
     f) OjosSinapsisPruebas, HabilidadRivalPruebas, GuardiaRivalPruebas y AdaptacionRivalPruebas siguen en verde; consola sin errores.
     Si d) falla, se ajustan `factorVelocidadRetirada` (≤ 1), `demoraRetirada` o `margenRetirada` en los assets (no las reglas) y se anota.
   - Captura docs/capturas/T-202-1.png: TORNILLO dando el paso atrás después de su golpe, desde la cabina, revisada a ojo.

## Carpetas permitidas
Assets/_Proyecto/Combate/Golpe/CicloGolpe.cs, Assets/_Proyecto/IA/Adaptacion/, Assets/_Proyecto/IA/Rival/RivalBasico.cs y CerebroRival.cs,
Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_*.asset (solo si falla d, anotado), Assets/_Proyecto/IA/Editor/, Assets/_Proyecto/IA/Prefabs/ (solo con
creadores), Assets/_Proyecto/Tests/EditMode/IA/, Assets/_Proyecto/Tests/EditMode/Combate/, Assets/_Proyecto/Tests/PlayMode/IA/, docs/capturas/, docs/bitacora/
No tocar: Core/, los `DatosMecha` y `DatosRival`, `HabilidadRival` y `FuriaJefe` (su comportamiento no cambia), `MechaJugador.prefab`, UI/,
Pelea/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena) y `playmode-por-area` no marca ningún área "LIMITE"
- [ ] ReglaFintasPruebas, ReglaRetiradaPruebas y FintasRetiradaPruebas pasan 3 corridas seguidas; la bitácora imprime los avisos medidos por
      golpe, los amagos, las distancias de la retirada y los contragolpes de 0.3 s y 0.9 s que conectaron
- [ ] Las pruebas viejas de IA, Combate, Pelea y los recorridos (RecorridoHabilidadesPruebas, RecorridoImplantesPruebas) siguen en verde sin cambios
- [ ] Correr los creadores dos veces no cambia los prefabs (md5 igual); `RivalBasico.prefab`, `RivalTutorial.prefab` y `RivalPandilla.prefab` sin cambios
- [ ] Captura T-202-1 revisada a ojo

## Criterios de sensación (para un humano)
- Pruebas_Economia o Pruebas_Adaptacion (T-204), revancha contra TORNILLO con FINTAS y luego con RETIRADA: ¿un amago te hace decir "me la
  hizo" o te sientes engañado por el juego? ¿El paso atrás se ve como táctica del rival y no como un error? ¿Encuentras solo cómo ganarle?

## Pruebas automáticas
- EditMode: ReglaFintasPruebas, ReglaRetiradaPruebas y la de `CicloGolpe`. PlayMode: FintasRetiradaPruebas.

## Fuera de alcance
- La pantalla de la cabina (T-203). Esquivar misiles (D-tony-201 d). Que OJOS distinga un amago (D-tony-201 f). Fintas en la habilidad de los jefes.
