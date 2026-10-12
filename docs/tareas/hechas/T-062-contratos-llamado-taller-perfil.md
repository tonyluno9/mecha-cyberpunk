# T-062 Contratos de llamado del mecha, taller y perfil del jugador
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-062-contratos-llamado-taller-perfil | Modelo: Sonnet 5.5, medium
Depende de: nada (T-031, T-034 y T-050 ya están en main)

## Objetivo
En Core/ existen los tres contratos nuevos que T-F4 necesita entre Tony y Alan: dónde puede llegar el mecha cuando lo llamas
(`IPuntoLlamadoMecha`, la zona permitida), dónde descansa el mecha en el taller de la casa (`ITallerMecha`) y quién es el
jugador (`IPerfilJugador`: nombre y alias, que grita el presentador). Alan los implementa o los lee en A-F3 (callejón, punto de
llamado, casa con taller) sin preguntar. El aviso de "nadie la registra todavía" de `ICalor` se actualiza.

## Contexto
- Decisiones: D-026, D-027 (quien posee el dato lo implementa), D-029, D-032. Propuesta nueva: D-tony-27 en
  docs/decisiones-pendientes-tony.md.
- Diseño: combate.md ("Llamas al mecha y llega desde tu taller en unos segundos, solo en ciertas zonas"; "Llamas al mecha en
  una zona prohibida: no llega"), personaje-progresion.md ("El presentador del clandestino dice mi alias"), mecha-taller.md
  (la reparación es en el taller, T-F5).
- Regla de la zona (D-tony-30): llamar solo funciona si el jugador está a `Radio` metros o menos (en horizontal) de la `Llegada`
  de un punto habilitado; fuera de toda zona es zona prohibida. Así Alan decide dónde se puede llamar colocando puntos, y no hay
  que dibujar polígonos.
- Lee primero, como ejemplo de estilo: Core/Jugador/IMontable.cs, Core/Jugador/IPuntoAparicion.cs, Core/Calor/ICalor.cs,
  Core/Servicios/Registro.cs. Los comentarios `///` SON el contrato; el revisor los lee con la mirada de Alan.
- Es una tarea de Core: se integra a main en cuanto el verificador la apruebe (Alan la necesita para A-F3).

## Qué hacer
1. `Core/Mecha/IPuntoLlamadoMecha.cs` (carpeta nueva Core/Mecha/):
   - `Pose Llegada`: posición en el piso y rotación (+Z hacia donde mira el mecha al llegar). El mecha baja del cielo: el sitio
     necesita cielo abierto y un círculo libre de 3 m de radio y 6 m de alto. No va en interiores ni bajo techos.
   - `float Radio`: distancia horizontal (m, mayor que 0) desde `Llegada` dentro de la cual el jugador puede llamar.
   - `bool Habilitado`: false cierra la zona por ahora (por historia o por una persecución); un punto deshabilitado no cuenta.
   - Comentario de la interfaz: la implementa Alan en el mundo y se agrega a `Registro<IPuntoLlamadoMecha>` en `OnEnable` (se
     quita en `OnDisable`). Quien llama (Tony, Mecha/) usa el punto habilitado más cercano que tenga al jugador dentro de su
     `Radio`; fuera de todos, el mecha no llega. Si dos zonas se enciman, gana la `Llegada` más cercana al jugador.
2. `Core/Mecha/ITallerMecha.cs`:
   - `Pose Estacionamiento`: base del mecha en el piso del taller (+Z hacia la salida). Necesita 2 m de radio libre y 5 m de alto.
   - `Vector3 PuntoInteraccion`: punto en el piso junto al banco de trabajo donde el jugador repara y mejora con E (lo usa T-F5;
     en T-F4 solo se lee `Estacionamiento`).
   - Comentario: lo implementa Alan en la casa (A-F3) y se agrega a `Registro<ITallerMecha>`; hay uno por escena. El mecha de
     Tony se coloca solo en `Estacionamiento` al arrancar y vuelve ahí cuando regresa; Alan no mueve el mecha. Si no hay taller
     registrado, el mecha usa como casa la pose donde arrancó.
3. `Core/Jugador/IPerfilJugador.cs`:
   - `string Nombre`, `string Alias` (nunca null ni vacíos: quien la implementa pone un valor de respaldo), `event Action Cambio`.
   - Comentario: la implementa `ServicioPerfil` (Progresion/, T-070), que va dentro de `JugadorAPie.prefab`, y se busca en
     `Registro<IPerfilJugador>`; la creación de personaje (T-F6) lo llena y el guardado (T-F5) lo conserva. Alan la lee si
     necesita el alias (carteles, grafitis). La apariencia (cara, ropa, implantes visibles) se agrega en T-F6 con otra tarea de
     Core; no la supongas.
4. `Core/Calor/ICalor.cs`: solo comentarios. Cambia "Por ahora nadie la registra ... T-F4/T-F5" por: la registra `ServicioCalor`
   (Progresion/, T-067), dentro de `ServiciosProgresion.prefab` anidado en `JugadorAPie.prefab`; mientras no esté en main usa un
   doble. Agrega que usar el mecha en la calle suma con `MotivoCalor.MechaEnCalle` (lo hace Tony).
5. Pruebas en `Tests/EditMode/Core/`: `ContratosMechaPruebas` con dobles definidos en la prueba (`PuntoLlamadoFalso`,
   `TallerFalso`, `PerfilFalso`) que se agregan y quitan de su `Registro<T>`: comprueba que se pueden implementar sin Unity vivo
   y que `Registros.LimpiarTodos()` deja los tres registros vacíos. `Registros.LimpiarTodos()` en SetUp y TearDown.

Nota para el orquestador (no es trabajo del implementador): al integrar a main, anota en docs/revision-tony.md el pedido para
Alan (D-tony-37): en A-F3, un `ITallerMecha` en la casa, un `IPuntoLlamadoMecha` junto al callejón de la emboscada (y los que
quiera en calles abiertas, ninguno en interiores) y, cuando T-064 y T-071 estén en main, colocar `Mecha/Prefabs/MechaEstacionado.prefab`
y `Pelea/Prefabs/Emboscada.prefab` sin editarlos.

## Carpetas permitidas
Assets/_Proyecto/Core/Mecha/ (nueva), Assets/_Proyecto/Core/Jugador/IPerfilJugador.cs, Assets/_Proyecto/Core/Calor/ICalor.cs
(solo comentarios) y Assets/_Proyecto/Tests/EditMode/Core/
No tocar: el resto de Core/, las áreas de Tony y de Alan, escenas, prefabs, ProjectSettings/, Packages/ ni herramientas/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa: compilar OK, EditMode y PlayMode sin fallas, con ContratosMechaPruebas contada
- [ ] ContratosMechaPruebas pasa: los tres dobles entran y salen de su `Registro<T>` y `Registros.LimpiarTodos()` los vacía
- [ ] `git diff main --stat` solo muestra archivos de las carpetas permitidas; en ICalor.cs solo cambian líneas de comentario
      (`git diff -U0 main -- Assets/_Proyecto/Core/Calor/ICalor.cs` sin líneas de código cambiadas)
- [ ] Cada miembro público de las tres interfaces nuevas tiene `///` con unidades (m, s) donde aplique
- [ ] Ningún archivo de Core/ referencia un ensamblado de área (ReglasEnsamblados sigue pasando)

## Criterios de sensación (para un humano)
- Alan (o Tony con su mirada) lee solo los `///` de Core/Mecha/ y sabe, sin preguntar, dónde poner el punto de llamado del
  callejón, cuánto espacio dejar y qué pasa si el jugador llama fuera de la zona.

## Pruebas automáticas
- EditMode (Tests/EditMode/Core/): ContratosMechaPruebas.

## Fuera de alcance
- Implementar los contratos (el mecha es T-064 y T-066, el perfil T-070, el calor T-067; los de Alan son A-F3)
- Apariencia del personaje (T-F6); bajar a la fuerza de un montable (no hace falta en T-F4: la emboscada no se puede perder)
