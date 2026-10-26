# T-178 Los jefes de las ligas usan una habilidad
Dueño: Tony | Rama: tony/T-178-jefes-habilidades | Modelo: Sonnet 5.5, medium
Depende de: T-174, T-175 (puede ir en paralelo con T-176 y T-177)

## Objetivo
Los 4 jefes de las ligas (DON FIERRO, LA MATRACA, EL ALCALDE y EL CACIQUE) usan una habilidad de su liga con enfriamiento y un aviso
visible antes de lanzarla, así que el jugador aprende a bloquear, esquivar o aguantar con escudo, y la pelea de jefe se siente distinta a las
otras. Los demás rivales, el tutorial, la pandilla y la pelea casera no cambian.

## Contexto
- Diseño: combate.md (bloquear y contraatacar con buen timing; "El rival se adapta a tu forma de pelear" es propuesta del bloque 8, no de este),
  personaje-progresion.md (ligas y jefes).
- Decisiones: D-tony-97 (los 20 rivales), D-tony-96 y D-tony-108 (jefe con furia a la mitad de la vida, `FuriaJefe`), D-tony-109 (el creador
  no pisa valores de un asset que ya existe). Propuesta nueva: D-tony-139 (qué habilidad usa cada jefe, enfriamiento ×1.5, aviso 0.6 s, sin
  límite de munición).
- Código: `IA/Rival/RivalBasico.cs` (preparación del golpe con brazo atrás y color de aviso; `ControlActivo`), `IA/Rival/FuriaJefe.cs`
  (`EnFuria`), `IA/Rival/PiedadRival.cs` (no la llevan los rivales de liga), `IA/Editor/CreadorRivalesLiga.cs` (el quinto de cada liga es el
  jefe; prefabs `IA/Prefabs/Ligas/RivalLiga{L}_5.prefab`), `LanzadorHabilidades` y `ReglaHabilidad` (T-174, T-175).
- Pruebas modelo: `Tests/PlayMode/IA/FuriaJefePruebas.cs`, `RivalesLigaPruebas.cs`.

## Qué hacer
1. `IA/Datos/DatosHabilidadRival.cs` (ScriptableObject; los valores de pelea del rival que no caben en `DatosRival` de Core, que no puede
   referenciar Combate): `habilidad`, `nivel`, `factorEnfriamiento` (1.5), `factorEnfriamientoEnFuria` (0.7, se multiplica encima),
   `segundosAviso` (0.6), `distanciaMinima` y `distanciaMaxima` para usarla (m), con `[Tooltip]` y `[Min]`.
2. `IA/Rival/ReglaHabilidadRival.cs` (pura, con `dt`): decide cuándo lanzar (enfriamiento cumplido, a distancia, con control, sin estar
   aturdido, sin estar preparando un golpe) y lleva el aviso → lanzamiento. El primer uso llega después de un enfriamiento completo desde que
   empieza la pelea (no al sonar la campana).
3. `IA/Rival/HabilidadRival.cs` (MonoBehaviour junto a `RivalBasico` y `LanzadorHabilidades`): durante el aviso el cuerpo brilla con el color
   de la rama de su habilidad (no rojo: rojo es la furia) y el rival se detiene; luego lanza mirando al jugador. Se reinicia en cada pelea
   (`PeleaIniciada`); con KO, sin control o aturdido corta el aviso sin lanzar. No gasta munición (D-tony-139).
4. Creador (paso explícito "habilidades de los jefes" en `CreadorRivalesLiga`, o un creador nuevo en `IA/Editor/`; idempotente) que crea
   `IA/Datos/Ligas/DatosHabilidadRival_Jefe1..4.asset` y pone `LanzadorHabilidades` + `HabilidadRival` en los 4 prefabs de jefe:
   | Jefe | Habilidad | Nivel |
   | --- | --- | --- |
   | DON FIERRO (Sótano) | EMBESTIDA | I |
   | LA MATRACA (Calle) | DESCARGA | II |
   | EL ALCALDE (Distrito) | MISIL | III |
   | EL CACIQUE (Campeonato) | LANZALLAMAS | IV |
5. PlayMode `Tests/PlayMode/IA/HabilidadRivalPruebas.cs` (un jefe real de los prefabs y un mecha jugador de prueba con control):
   a) DON FIERRO a 5 m: tras 7 × 1.5 = 10.5 s (±0.2) desde `PeleaIniciada` avisa (color de rama, se detiene 0.6 s) y embiste; el jugador pierde
      14 si no bloquea; el siguiente uso no llega antes de otros 10.5 s;
   b) en furia, el enfriamiento medido es 10.5 × 0.7 = 7.35 s (±0.2);
   c) LA MATRACA: su DESCARGA deja aturdido al jugador 1.0 s (no golpea ni sube la guardia; las teclas no hacen nada) y luego vuelve todo;
   d) EL ALCALDE: el MISIL III contra la guardia arriba entra reducido como cualquier golpe bloqueado (30 × (1 − reducción del bloqueo));
   e) si el jefe queda aturdido o en KO durante el aviso, no lanza;
   f) TORNILLO (rival 1 de la liga 1) y el rival del tutorial no tienen `HabilidadRival` y pelean igual que antes;
   g) al empezar una pelea nueva contra el mismo jefe (revancha), el primer uso vuelve a tardar un enfriamiento completo.

## Carpetas permitidas
Assets/_Proyecto/IA/ (Datos/, Datos/Ligas/ solo con el creador, Rival/, Editor/, Prefabs/Ligas/ solo con el creador),
Assets/_Proyecto/Tests/PlayMode/IA/, Assets/_Proyecto/Tests/EditMode/IA/
No tocar: Core/, Combate/ (si falta algo de T-175, se anota y se pide una corrección), Mecha/, Pelea/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] HabilidadRivalPruebas a) a g) pasan; EditMode de `ReglaHabilidadRival` (aviso, enfriamiento con furia, primer uso) pasa; FuriaJefePruebas,
      RivalesLigaPruebas, PiedadRivalPruebas, PandillaPruebas y las de Pelea y Progresion siguen en verde
      (`AREAS='IA Pelea Progresion' bash herramientas/verificar.sh playmode-por-area`)
- [ ] Solo los 4 prefabs de jefe cambiaron (`git diff --stat` en IA/Prefabs); el creador corrido dos veces no cambia nada (md5 igual)
- [ ] Captura docs/capturas/T-178-1.png: desde la cabina, EL ALCALDE en su aviso (color de rama) justo antes de lanzar el misil, revisada a ojo

## Criterios de sensación (para un humano)
- Pruebas_Economia, `L` hasta cada liga: ¿el aviso de cada jefe se ve a tiempo para reaccionar? ¿DON FIERRO embistiendo sorprende sin ser
  injusto? ¿La descarga de LA MATRACA se siente como un castigo por pelear pegado, o como algo que no puedes evitar?

## Pruebas automáticas
- EditMode: ReglaHabilidadRivalPruebas. PlayMode: HabilidadRivalPruebas.

## Fuera de alcance
- Rivales normales con habilidades y rivales que reaccionan a las del jugador (bloque 8, adaptación del rival). Balance de los jefes con su
  habilidad (T-179).
