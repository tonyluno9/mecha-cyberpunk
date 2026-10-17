# T-152 Contrato del progreso de ligas, presentación de pelea y límite por área de Progresion
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-152-contrato-ligas | Modelo: Sonnet 5.5, medium
Depende de: nada

## Objetivo
En Core/ quedan, escritos antes de usarse (D-027), el contrato de lectura del progreso de las ligas (`IProgresoLigas`) y un evento de pelea
que dice contra quién es la pelea que empieza y si es de jefe (`InfoPelea` y `EventosPelea.PeleaPresentada`). Tony implementa y usa los dos
en este bloque; el bloque de piezas (nivel de pieza desbloqueado por liga) lee `IProgresoLigas` sin que Mecha ni Economia referencien
Progresion, y Alan puede leerlos para su clandestino (luces de la jaula en una pelea de jefe, carteles de la liga) sin implementar nada.
Además, `playmode-por-area` deja de cortar el área Progresion, que ya roza los 900 s.

## Contexto
- Diseño: personaje-progresion.md ("El clandestino tiene 4 ligas: sótano, calle, distrito y campeonato. Cada una paga más y tiene rivales
  más duros"; "Cada liga tiene 5 rivales, el último es su campeón con pelea de jefe, y vencerlo te sube de liga"; "Al vencer al campeón de
  una liga se abre la siguiente"), mecha-taller.md ("4 niveles por rama, desbloqueados con las 4 ligas").
- Decisiones: D-026, D-027, D-032, D-034 (Registro<T>). Propuestas nuevas: D-tony-93 (este contrato) y D-tony-94 (dónde viven las ligas).
- Estilo de contratos: Core/Historia/IHistoria.cs, Core/Economia/IApuestasPelea.cs, Core/Pelea/EventosPelea.cs. Los `///` SON el contrato;
  el revisor los lee con la mirada de Alan.
- Herramientas: `herramientas/verificar.sh` (modo `playmode-por-area` de T-141, línea ~90), docs/herramientas-verificacion.md. T-151 anotó que
  Progresion necesita `LIMITE_AREA=2400`.
- Es tarea de Core: se integra a main en cuanto el verificador la apruebe, y se avisa en docs/revision-tony.md (sección "Para Alan").

## Qué hacer
1. `Core/Ligas/IProgresoLigas.cs` (solo lectura). Miembros:
   - `int CantidadLigas`: cuántas ligas hay (4 en el juego).
   - `int LigaActual`: la liga más alta abierta, de 1 a `CantidadLigas` (1 = sótano). Nunca baja. Al vencer al campeón de la última sigue en
     la última.
   - `int CampeonesVencidos`: de 0 a `CantidadLigas`. Igual a `CantidadLigas` significa "campeón del clandestino".
   - `string NombreLiga(int liga)`: nombre para pantallas ("SÓTANO"); vacío si `liga` está fuera de 1..`CantidadLigas`.
   - `event Action Cambio`: tras cualquier cambio de lo anterior, también al cargar una partida.
   - `event Action<int> CampeonVencido`: al vencer por primera vez al campeón de una liga, con el número de esa liga (no se avisa al cargar
     ni en una revancha). Si es la última, el jugador es campeón del clandestino.
   Comentario de clase: la implementa Tony (`ServicioLigas`, Progresion/, en `ServiciosProgresion.prefab`, T-154, clave de guardado `ligas`) y
   se busca en `Registro<IProgresoLigas>`. Solo Tony registra victorias; Alan solo lee. El bloque de piezas toma `LigaActual` como el nivel
   máximo de pieza que se puede comprar. Sin `IProgresoLigas` registrado, quien lea debe actuar como liga 1 sin campeones.
2. `Core/Pelea/InfoPelea.cs`: `readonly struct` con `string Rival` (alias del rival), `int Liga` (1..4; 0 = pelea fuera de liga: casera,
   prólogo, emboscada) y `bool EsJefe` (pelea de jefe: el campeón de una liga o el campeón del prólogo). Constructor con los tres.
3. `Core/Pelea/EventosPelea.cs`: evento `PeleaPresentada` (`Action<InfoPelea>`) y `PublicarPresentacion(InfoPelea)`. `///`: lo publica el
   flujo de pelea de Tony una vez por pelea (también en cada revancha), justo antes de la presentación del presentador o, si no hay, del
   conteo; sirve para que el mundo acompañe (por ejemplo, Alan cambia las luces de la jaula si `EsJefe`). `LimpiarSuscriptores` lo limpia.
4. `Core/Guardado/IGuardable.cs`: en "Claves en uso" agrega `ligas` (Tony, T-154). Solo comentarios.
5. `herramientas/verificar.sh`, modo `playmode-por-area`: límite por área con una tabla en el script (Progresion 2400 s; las demás 900 s).
   `LIMITE_AREA`, si se define, sigue mandando sobre todas. La línea de cada área imprime el límite que usó. Documéntalo en
   docs/herramientas-verificacion.md.
6. Prueba EditMode `Tests/EditMode/Core/ContratosLigasPruebas.cs` con un doble mínimo de `IProgresoLigas` que cumpla los `///` (liga fuera de
   rango da nombre vacío; `CampeonVencido` no se avisa en una revancha; `Cambio` se avisa al cambiar) y una prueba de que
   `LimpiarSuscriptores` quita a los suscriptores de `PeleaPresentada`. Sirve de ejemplo para el doble de Alan.

## Carpetas permitidas
Assets/_Proyecto/Core/Ligas/ (nueva, con su .meta), Assets/_Proyecto/Core/Pelea/, Assets/_Proyecto/Core/Guardado/IGuardable.cs (solo
comentarios), Assets/_Proyecto/Tests/EditMode/Core/, herramientas/verificar.sh, docs/herramientas-verificacion.md
No tocar: el resto de Core/, Progresion/ y Pelea/ (la implementación es de T-154 y T-155), ProjectSettings/, Packages/, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en `JaulaClandestinoPruebas`, listados por nombre)
- [ ] ContratosLigasPruebas pasa (EditMode) y ReglasEnsambladosPruebas sigue en verde
- [ ] `AREAS='Core Progresion' bash herramientas/verificar.sh playmode-por-area` imprime "Progresion ... (límite 2400 s)" y "Core ... (límite
      900 s)"; con `LIMITE_AREA=20 AREAS=Progresion` sale "LIMITE 20 s" y no queda ningún Unity.exe lanzado por el script (`Get-Process Unity`)
- [ ] Grep: `IProgresoLigas.cs` e `InfoPelea.cs` no referencian nada fuera de Core; ningún archivo de Alan cambió; los suscriptores que ya
      usaban `EventosPelea` compilan sin cambios

## Criterios de sensación (para un humano)
- Leer los `///` de `IProgresoLigas` e `InfoPelea` con la mirada de Alan: ¿basta para poner un cartel "LIGA CALLE" en el clandestino o para
  cambiar las luces de la jaula en una pelea de jefe sin preguntar?

## Pruebas automáticas
- EditMode: ContratosLigasPruebas.

## Fuera de alcance
- `ServicioLigas` (T-154) y la publicación del evento desde `DirectorPelea` (T-155). Luces y carteles del clandestino (Alan, A-F5, opcional).
