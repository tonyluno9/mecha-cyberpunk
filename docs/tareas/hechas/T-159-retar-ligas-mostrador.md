# T-159 Retar en las ligas desde el mostrador de peleas (lógica, sin pantalla)
Dueño: Tony | Rama: tony/T-159-retar-ligas | Modelo: Sonnet 5.5, medium
Depende de: T-154, T-158

## Objetivo
Después de la demo (etapa `Libre`), el mostrador de peleas del clandestino sabe qué peleas ofrece: el siguiente rival de tu liga, revanchas
contra los que ya venciste en tu liga (pagan la mitad), la pelea casera de siempre y, como aviso, la liga que sigue bloqueada. Al retar una,
la pelea se arma con su cartel (rival, premio, apuesta con su momio, presentador), se puede apostar como hoy y, si ganas contra el siguiente
rival, el progreso avanza; si era el campeón, subes de liga. Todo por código: la pantalla para elegir es T-160.

## Contexto
- Diseño: personaje-progresion.md (ligas; "Retas a un rival de una liga más alta: no se puede. Propuesta"; perder no es game over, puedes
  volver a retar), economia-misiones.md (las peleas son la fuente principal de dinero).
- Decisiones: D-tony-65 (EntradaArena reinicia el director), D-tony-66 (mostrador y casera; solo de noche y en `PeleaCasera` o `Libre`),
  D-tony-69 (el director del prólogo escucha `MostradorPeleas.PeleaTerminada` para la casera), D-tony-80 (apuesta). Propuestas nuevas:
  D-tony-95 (reglas del progreso y de la cartelera; revancha a la mitad) y D-tony-99 (orden de la cartelera).
- Código: `Progresion/Prologo/MostradorPeleas.cs` (Usar, AbrirApuesta, Decidir, MechaListo), `Progresion/Prologo/EntradaArena.cs`,
  `Pelea/Flujo/DirectorPelea.cs` (`PrepararCartel`, `QuitarCartel`, T-155), `Pelea/Datos/CalendarioLigas.cs` (T-158),
  `Progresion/Ligas/ServicioLigas.cs` y `LigasDePrueba.cs` (T-154), `Progresion/Prologo/Editor/CreadorMostradorPeleas.cs` y
  `CreadorEscenaEconomia.cs` (escena Pruebas_Economia, D-tony-87; agrega objetos con métodos idempotentes).
- Pruebas de ejemplo: `Tests/PlayMode/Progresion/ApuestaMostradorPruebas.cs`, `PeleaCaseraPruebas.cs`, `RecorridoEconomiaPruebas.cs`.

## Qué hacer
1. `Progresion/Ligas/ReglaCartelera.cs` (C# puro) y `OpcionCartelera` (struct, un archivo): con el progreso (liga actual, siguiente,
   campeones), los rivales por liga, los premios de ganar de cada (liga, índice), la fracción de revancha y el premio de la casera, arma la
   lista en este orden (D-tony-99): 1) `Siguiente` (si no eres campeón del clandestino); 2) `Revancha` de cada rival ya vencido de la liga
   actual, del 1 en adelante (tras el último campeón, los 5 de la liga 4); 3) `Casera`; 4) `Bloqueada`, si hay una liga más alta: su `liga` es
   la que sigue y su índice el del jefe de la liga actual (el que hay que vencer para abrirla; la pantalla lo nombra). Cada opción: tipo, liga, índice, si es jefe y premio a mostrar
   (revancha = premio × fracción, redondeado).
2. `MostradorPeleas`:
   - Campo `CalendarioLigas calendario` (opcional). `Opciones` (lista de la regla) solo si hay calendario, `ServicioLigas` registrado y la
     etapa es `Libre` o no hay `IHistoria`; si no, solo la casera. `AliasDe(opcion)` (del `DatosRival` del prefab del cartel; "???" si falta)
     y `PremioDe(opcion)`.
   - `bool Retar(OpcionCartelera opcion)`: solo si `PuedeUsar` y la opción está en `Opciones` y no es `Bloqueada`. Casera: `QuitarCartel` y
     sigue como hoy. Siguiente o Revancha: `PrepararCartel` con el cartel del calendario (fracción 1 o la de revancha) y después el mismo
     camino de hoy (pantalla de apuesta si la hay, o pelea directo). Si algo falla, quita el cartel y no cobra nada.
   - Al terminar la entrada: si la opción era `Siguiente` y ganó el jugador, `ServicioLigas.RegistrarVictoria(liga, índice)`; una revancha o
     una derrota no cambian el progreso. Después, `QuitarCartel` siempre. `PeleaTerminada` se sigue avisando como hoy y además
     `UltimaOpcion` dice qué se peleó.
   - `Usar()` sin pantalla de cartelera sigue haciendo lo de hoy (la casera): así no cambia el prólogo ni el recorrido de la economía.
   - El aviso de la apuesta muestra el rival y el premio del cartel elegido (con la fracción si es revancha).
3. `MostradorPeleas.prefab`: el creador asigna `CalendarioLigas_Base` (idempotente). Pruebas_Economia: el creador de la escena agrega
   `LigasDePrueba` (tecla L: vence al siguiente rival sin pelear) de forma idempotente y sin reordenar lo demás.
4. EditMode `Tests/EditMode/Progresion/ReglaCarteleraPruebas.cs` y PlayMode `Tests/PlayMode/Progresion/LigasMostradorPruebas.cs` (Pruebas_Economia o
   su armado en la prueba; pelea real con entrada simulada; `RecibirDanio` al rival solo después de daño real al jugador; dinero inicial por
   código; números calculados desde los assets, no escritos a mano).

## Carpetas permitidas
Assets/_Proyecto/Progresion/ (Ligas/, Prologo/, Prologo/Editor/, Prologo/Prefabs/ y la escena Escenas/Tony/Pruebas_Economia.unity solo con sus
creadores), Assets/_Proyecto/Tests/EditMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Progresion/
No tocar: Core/, Pelea/ (si `DirectorPelea` necesita algo más, se anota y se pide una tarea), IA/, UI/, Economia/, otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena) y el área Progresion no sale "LIMITE"
- [ ] ReglaCarteleraPruebas: al empezar la lista es [Siguiente 1-1, Casera, Bloqueada 2]; con liga 1 siguiente 3 es [Siguiente 1-3, Revancha
      1-1 a mitad de premio, Revancha 1-2, Casera, Bloqueada 2]; en liga 2 no aparecen revanchas de la liga 1; tras el campeón de la liga 4 no
      hay Siguiente ni Bloqueada y hay 5 revanchas
- [ ] LigasMostradorPruebas: de noche y en `Libre`, `Retar(Siguiente)` pelea contra TORNILLO; al ganar, el dinero sube el premio de su cartel y
      el siguiente pasa a 2; una revancha ganada contra TORNILLO paga la mitad y no cambia el progreso; perder contra el 2 no cambia el progreso y
      se puede retar otra vez
- [ ] LigasMostradorPruebas: con `Poner(liga 1, siguiente 5)`, apostar el máximo de la apuesta del jefe y ganarle a DON FIERRO deja liga 2,
      siguiente 1, avisa `CampeonVencido(1)` una vez y abona premio + apuesta × (1 + momio del jefe) calculado desde los assets
- [ ] `Retar` de una opción que no está en la lista (liga 3, o el rival 4 cuando el siguiente es el 2) devuelve false sin cobrar ni pelear; en la
      etapa `PeleaCasera` solo se ofrece la casera
- [ ] En `Libre`, ganar una pelea de liga no cambia la etapa de la historia ni muestra "CONTINUARÁ"; ApuestaMostradorPruebas, PeleaCaseraPruebas,
      RecorridoEconomiaPruebas y RecorridoPrologoPruebas siguen en verde sin cambios
- [ ] En Pruebas_Economia, L avanza el siguiente rival (prueba con teclado simulado); el creador de la escena corrido dos veces no cambia la escena

## Criterios de sensación (para un humano)
- Ninguno todavía (la pantalla para elegir es T-160).

## Pruebas automáticas
- EditMode: ReglaCarteleraPruebas. PlayMode: LigasMostradorPruebas.

## Fuera de alcance
- La pantalla de la cartelera (T-160). Textos de jefe y de subir de liga (T-161). Límite de peleas de liga por noche (pregunta para Tony, D-tony-103).
