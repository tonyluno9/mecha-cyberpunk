# T-190 Contratos de los implantes (Core)
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-190-contratos-implantes | Modelo: Sonnet 5.5, medium
Necesita aprobación: autorizada por Tony en el /goal
Depende de: nada

## Objetivo
En Core/ quedan, escritos antes de usarse (D-027), los contratos del bloque 6: qué implantes trae el jugador en sus 4 espacios y qué
efectos suman (solo lectura). Así el jugador a pie (Personaje), el mecha (Mecha), las pantallas (UI) y el cuerpo que se ve en el espejo
leen lo mismo sin referenciar Progresion, y la ciudad de Alan puede reaccionar al cromo si quiere, sin que nada lo espere.

## Contexto
- Diseño: personaje-progresion.md ("4 espacios de implante: Ojos: ves la vida y el próximo ataque del rival. Brazos: más daño a pie.
  Piernas: corres y saltas más. Sistema nervioso: reaccionas más rápido en el mecha"; "Los implantes se instalan en una clínica dentro del
  mercado negro"; criterio "Un implante instalado se ve en mi apariencia y cambia lo que dice que cambia").
- Decisiones: D-011 (la vida del rival se esperaba al implante de ojos), D-026, D-027, D-032, D-034 (`Registro<T>`), D-tony-63 (apariencia).
  Propuestas nuevas: D-tony-163 (bloque), D-tony-164 (estos contratos), D-tony-165 (tabla de implantes, para los ejemplos de los `///`).
- Estilo de contratos: Core/Reputacion/IReputacion.cs y Core/Misiones/IMisiones.cs (T-181), Core/Datos/ValoresMecha.cs (struct de datos
  simples). Los `///` SON el contrato; el revisor los lee con la mirada de Alan.
- Ya existen y NO cambian: `MotivoDinero.Clinica`, `IdLugar.Clinica`, `TipoOferta.Implante`, `AparienciaJugador.Implante` (el implante
  cosmético que se elige al crear el personaje; los implantes instalados son otra cosa y se suman encima).
- Es tarea de Core: se integra a main en cuanto el verificador la apruebe, y se avisa en docs/revision-tony.md (sección "Para Alan").

## Qué hacer
1. `Core/Implantes/EspacioImplante.cs`: `Ojos`, `Brazos`, `Piernas`, `SistemaNervioso` (solo se agregan valores al final).
2. `Core/Implantes/EfectosImplantes.cs`: `[Serializable] struct` con campos públicos y `///` en cada uno (unidad y valor neutro):
   - `multiplicadorGolpeAPie` (daño del golpe a pie; 1 = sin cambio), `multiplicadorCorrer` (velocidad al correr a pie; 1),
     `multiplicadorSalto` (altura del salto a pie; 1),
   - `multiplicadorReaccionMecha` (multiplica la preparación del golpe y el tiempo de subir la guardia del mecha del jugador y divide la
     ventana del bloqueo perfecto; 1 = sin cambio, menor que 1 es más rápido),
   - `verVidaRival` (bool), `vidaRivalEnNumeros` (bool), `anticipacionAtaque` (s; 0 = no ve el próximo ataque).
   - `static EfectosImplantes Ninguno` (todo neutro) y `static EfectosImplantes Combinar(EfectosImplantes a, EfectosImplantes b)` (los
     multiplicadores se multiplican, los bool con OR, la anticipación con el máximo). No hace más cálculos ni conoce precios.
3. `Core/Implantes/IImplantes.cs` (solo lectura):
   - `int Nivel(EspacioImplante espacio)`: 0 = nada instalado; 1 o 2 (los niveles son datos de Tony).
   - `string Nombre(EspacioImplante espacio)`: para pantallas ("OJOS DE LECHUZA"); vacío sin implante, nunca null.
   - `int Cantidad`: cuántos espacios tienen algo.
   - `EfectosImplantes Efectos`: la combinación de todo lo instalado; `Ninguno` sin implantes.
   - `event Action Cambio`: tras cualquier cambio, también al cargar una partida.
   - `event Action<EspacioImplante, int> Instalado`: con el nivel nuevo, solo al instalar de verdad; no se avisa al cargar.
   Comentario de clase: la implementa Tony (`ServicioImplantes`, Progresion/, en `ServiciosProgresion.prefab`, T-191, clave `implantes`) y se
   busca en `Registro<IImplantes>`. Solo Tony instala (en su clínica, T-193). Alan solo lee (ejemplos: un vecino que dice "ese trae cromo"
   con 3 o más implantes; el letrero de la clínica que se prende con un implante nuevo). Sin `IImplantes` registrado, actuar como
   `Cantidad` 0 y `Efectos` = `Ninguno` (así el prólogo y las escenas viejas no cambian).
4. `Core/Guardado/IGuardable.cs`: en "Claves en uso" agrega `implantes` (Tony, T-191). Solo comentarios.
5. `Core/Jugador/IPuntoAparicion.cs`: en el `///` de `Id`, agrega como ejemplo "Clinica_Camilla" (donde despiertas cuando el doctor te
   levanta, T-196). Solo comentarios.
6. EditMode `Tests/EditMode/Core/ContratosImplantesPruebas.cs` con un doble mínimo que cumple los `///`: `Ninguno` es neutro; `Combinar`
   multiplica, hace OR y toma el máximo, y combinar con `Ninguno` no cambia nada; `Nombre` nunca null; `Instalado` no se avisa al "cargar"
   pero `Cambio` sí; sin nada registrado, quien lee obtiene `Ninguno`. Sirven de ejemplo para un doble de Alan.

## Carpetas permitidas
Assets/_Proyecto/Core/Implantes/ (nueva, con .meta), Assets/_Proyecto/Core/Guardado/IGuardable.cs (solo comentarios),
Assets/_Proyecto/Core/Jugador/IPuntoAparicion.cs (solo comentarios), Assets/_Proyecto/Tests/EditMode/Core/, docs/revision-tony.md (sección "Para Alan")
No tocar: el resto de Core/, Progresion/, Personaje/, Mecha/, UI/ y cualquier implementación (son de T-191 en adelante), ProjectSettings/,
Packages/, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena, listados por nombre en docs/revision-tony.md)
- [ ] ContratosImplantesPruebas pasa (EditMode) y ReglasEnsambladosPruebas sigue en verde
- [ ] Grep: los archivos nuevos de Core no referencian nada fuera de Core, System y UnityEngine; ningún enum existente cambió de valores;
      ningún archivo de Alan cambió
- [ ] docs/revision-tony.md, "Para Alan": qué puede leer (`IImplantes`), el id "Clinica_Camilla" que conviene poner en su clínica (llega con
      T-196), y que nada de esto es obligatorio para él

## Criterios de sensación (para un humano)
- Leer los `///` de `IImplantes` y `EfectosImplantes` con la mirada de Alan: ¿basta para que un vecino reaccione a tu cromo sin preguntarle
  nada a Tony? ¿Se entiende que el implante cosmético de la creación no es un implante instalado?

## Pruebas automáticas
- EditMode: ContratosImplantesPruebas.

## Fuera de alcance
- `ServicioImplantes` y los datos (T-191), efectos a pie y cuerpo (T-192), clínica (T-193), ojos y sistema nervioso (T-194), el doctor
  (T-196). Lectura del rival para los ojos: no va en Core (D-tony-168). Hackeo.
