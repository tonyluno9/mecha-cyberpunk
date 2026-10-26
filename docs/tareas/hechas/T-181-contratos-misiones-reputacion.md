# T-181 Contratos de reputación, misiones y puntos de misión (Core)
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-181-contratos-misiones | Modelo: Sonnet 5.5, medium
Necesita aprobación: autorizada por Tony en el /goal
Depende de: nada

## Objetivo
En Core/ quedan, escritos antes de usarse (D-027), los contratos del bloque 5: la reputación de barrio y corporaciones (solo lectura,
para que la ciudad de Alan reaccione a tu fama), la misión en curso (solo lectura) y los puntos de misión que el mundo coloca (encargos,
destinos, botines, peleas y escondites). Tony los implementa en T-182 a T-187 con dobles en sus escenas; Alan los usa cuando quiera, sin que
nada lo espere.

## Contexto
- Diseño: concepto-historia.md ("Hay dos reputaciones que se jalan entre sí: barrio y corporaciones"; "Aceptar un contrato corporativo baja
  la reputación del barrio de forma visible"; propuesta "La ciudad reacciona a tu fama con grafitis de tu alias, gente que te saluda y anuncios
  corporativos que te difaman"), economia-misiones.md ("Las misiones del barrio salen en la calle, las corporativas por teléfono de día y las
  ilegales y robos en el bar de noche"; favores: "descuentos, información y escondites"), hoja-de-ruta.md (H7: "reputación visible en la ciudad").
- Decisiones: D-026, D-027, D-032, D-034 (Registro<T>). Propuestas nuevas: D-tony-143 (bloque), D-tony-144 (estos contratos).
- Estilo de contratos: Core/Ligas/IProgresoLigas.cs, Core/Historia/IPuntoHistoria.cs, Core/Calor/ICalor.cs. Los `///` SON el contrato;
  el revisor los lee con la mirada de Alan.
- Es tarea de Core: se integra a main en cuanto el verificador la apruebe, y se avisa en docs/revision-tony.md (sección "Para Alan").

## Qué hacer
1. `Core/Reputacion/TipoReputacion.cs` (`Barrio`, `Corporaciones`; solo se agregan valores al final) y `Core/Reputacion/IReputacion.cs`
   (solo lectura):
   - `int Minimo` (−100) e `int Maximo` (100).
   - `int Valor(TipoReputacion tipo)`: de `Minimo` a `Maximo`; empieza en 0.
   - `int Nivel(TipoReputacion tipo)`: de −2 a 2 (0 = neutral). Los umbrales son datos de Tony.
   - `string NombreNivel(TipoReputacion tipo)`: para pantallas ("JUSTICIERO", "ENEMIGO"); nunca null.
   - `event Action Cambio`: tras cualquier cambio, también al cargar una partida.
   - `event Action<TipoReputacion, int> NivelCambio`: con el nivel nuevo, solo cuando el nivel cambia; no se avisa al cargar.
   Comentario de clase: la implementa Tony (`ServicioReputacion`, Progresion/, en `ServiciosProgresion.prefab`, T-182, clave `reputacion`)
   y se busca en `Registro<IReputacion>`. Solo Tony la cambia; Alan solo lee (ejemplos: grafitis con el alias con barrio en nivel 1 o más,
   vecinos que saludan en nivel 2, anuncios que difaman con corporaciones en −1 o menos). Sin `IReputacion` registrado, actuar como 0 y nivel 0.
2. `Core/Misiones/TipoMision.cs` (`Barrio`, `Corporativa`, `Ilegal`, `Robo`) y `Core/Misiones/IMisiones.cs` (solo lectura):
   `bool HayMision`; `TipoMision Tipo` (solo vale con `HayMision`); `string Titulo` (vacío sin misión, nunca null); `event Action Cambio`
   (al aceptar, en cada paso, al terminar y al cargar); `event Action<TipoMision, bool> Terminada` (true si se cumplió; false si falló o se
   abandonó; no se avisa al cargar). Comentario de clase: la implementa Tony (`ServicioMisiones`, Progresion/, T-184, clave `misiones`); una
   sola misión a la vez; Alan puede leerla (por ejemplo, patrullas más atentas durante un robo).
3. `Core/Misiones/TipoPuntoMision.cs` (`Encargo`, `Destino`, `Botin`, `Pelea`, `Escondite`) y `Core/Misiones/IPuntoMision.cs`:
   `string Id` (único en la escena, prefijo `Mision_`, por ejemplo "Mision_Encargo_DonaLupe"), `TipoPuntoMision Tipo`, `Pose Pose` (un punto en
   el piso; +Z hacia donde se para la pelea o mira el vecino) y `void Activar(bool activo)` (Tony lo llama con true mientras el punto es parte
   del paso actual o tiene una misión que ofrecer, y con false al dejar de serlo; Alan puede prender ahí un vecino, una luz o una puerta; nunca
   debe lanzar). Comentario de clase: lo implementa quien arma la escena (Alan en Ciudad.unity; Tony con `PuntoMisionDePrueba` en sus escenas),
   se agrega a `Registro<IPuntoMision>` en `OnEnable` y se quita en `OnDisable`. Los ids que busca Tony están en sus datos de misiones (T-183) y
   en docs/revision-tony.md; si falta uno, esa misión no se ofrece y se avisa con un warning, sin error. Tony pone el interactuable (E) encima
   del punto; el punto no tiene que ser un `IInteractuable`.
4. `Core/Economia/MotivoDinero.cs`: `Mision` al final (después de `Carga`). `Core/Calor/MotivoCalor.cs`: `Mision` al final. Comentarios de
   una línea.
5. `Core/Guardado/IGuardable.cs`: en "Claves en uso" agrega `reputacion` (T-182), `favores` (T-182) y `misiones` (T-184). Solo comentarios.
6. EditMode `Tests/EditMode/Core/ContratosMisionesPruebas.cs` con dobles mínimos que cumplen los `///`: reputación recortada a [−100, 100];
   `NivelCambio` solo cuando cambia el nivel y no al "cargar"; `NombreNivel` nunca null; `Terminada` no se avisa al cargar; un doble de
   `IPuntoMision` en `Registro<IPuntoMision>` se encuentra por `Id`. Sirven de ejemplo para los dobles de Alan.

## Carpetas permitidas
Assets/_Proyecto/Core/Reputacion/ (nueva, con .meta), Assets/_Proyecto/Core/Misiones/ (nueva, con .meta), Assets/_Proyecto/Core/Economia/MotivoDinero.cs,
Assets/_Proyecto/Core/Calor/MotivoCalor.cs, Assets/_Proyecto/Core/Guardado/IGuardable.cs (solo comentarios), Assets/_Proyecto/Tests/EditMode/Core/,
docs/revision-tony.md (sección "Para Alan")
No tocar: el resto de Core/, Progresion/, UI/ y cualquier implementación (son de T-182 en adelante), ProjectSettings/, Packages/, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena, listados por nombre en docs/revision-tony.md)
- [ ] ContratosMisionesPruebas pasa (EditMode) y ReglasEnsambladosPruebas sigue en verde
- [ ] Grep: los archivos nuevos de Core no referencian nada fuera de Core y UnityEngine; `Mision` es el último valor de `MotivoDinero` y de
      `MotivoCalor` (los valores viejos conservan su número); ningún archivo de Alan cambió; todo lo que ya usaba esos enums compila sin cambios
- [ ] docs/revision-tony.md, "Para Alan": qué puede leer (reputación, misión en curso), qué puede colocar (puntos de misión con sus ids; la
      lista completa llega con T-183) y que nada de esto es obligatorio para él

## Criterios de sensación (para un humano)
- Leer los `///` de `IReputacion` e `IPuntoMision` con la mirada de Alan: ¿basta para pintar un grafiti con el alias cuando el barrio te quiere,
  o para poner a Doña Lupe en su esquina, sin preguntarle nada a Tony?

## Pruebas automáticas
- EditMode: ContratosMisionesPruebas.

## Fuera de alcance
- `ServicioReputacion` y favores (T-182), datos de misiones (T-183), `ServicioMisiones` (T-184). Grafitis, saludos, anuncios y vecinos en
  Ciudad.unity (Alan, opcional, cuando quiera).
