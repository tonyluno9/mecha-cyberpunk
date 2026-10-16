# T-120 Contratos de historia, apariencia y puntos de historia
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-120-contratos-historia | Modelo: Sonnet 5.5, medium
Depende de: nada (T-062 y T-100 ya están hechas; para integrar a main, T-100 tiene que estar en main primero)

## Objetivo
En Core/ están los contratos que T-F6 necesita entre Tony y Alan: en qué etapa del prólogo va la partida (`IHistoria`, para
que el mundo de Alan reaccione: carro cerca del clandestino, retenes, casino cerrado), la apariencia del jugador
(`AparienciaJugador` en `IPerfilJugador`, prometida en T-062) y los puntos con nombre que pone el mundo para la historia
(`IPuntoHistoria`: los planos de cámara del flashback y los pasos de la ruta del prólogo). Además, la prueba de ensamblados
acepta subensamblados de un área (`MechaCyberpunk.Progresion.Prologo`), que el director del prólogo necesita porque está
encima de todas las áreas de Tony.

## Contexto
- Decisiones: D-027 (quien posee el dato lo implementa), D-032, D-034 (Registro<T>), D-053 (objetivos intermedios en las
  esquinas), D-059 (la apariencia llega en T-F6 con otra tarea de Core). Propuestas nuevas: D-tony-57, D-tony-62 y D-tony-72 en
  docs/decisiones-pendientes-tony.md.
- Diseño: demo.md (orden del prólogo), personaje-progresion.md (nombre, alias y apariencia: cara, ropa e implantes visibles; la
  apariencia se ve en el espejo y en las cinemáticas), concepto-historia.md (el prólogo es el futuro; el flashback de los papás).
- Lee primero, como ejemplo de estilo: Core/Jugador/IPerfilJugador.cs, Core/Jugador/IPuntoAparicion.cs, Core/Guardado/IGuardable.cs,
  Core/Mecha/ITallerMecha.cs. Los comentarios `///` SON el contrato; el revisor los lee con la mirada de Alan.
- Es tarea de Core: el orquestador la integra a main en cuanto el verificador la apruebe. T-100 sigue en la rama local
  `tony/core-pendiente-push-main` sin subir a main (el push fue denegado): si al integrar T-100 aún no está en main, deja T-120 lista
  en una rama sobre `tony/core-pendiente-push-main` y anótalo en docs/revision-tony.md para que Tony haga los dos push.

## Qué hacer
1. `Core/Historia/EtapaPrologo.cs` (carpeta nueva), enum con estos valores en este orden y un `///` por valor que diga qué pasa
   en el mundo: `Ninguna` (sin partida), `CreacionPersonaje`, `PeleaTutorial` (de noche, el jugador ya está en su mecha en la
   arena del clandestino), `SalidaClandestino` (a pie, saliendo del clandestino hacia su carro), `Emboscada` (la pandilla del
   callejón), `RegresoACasa` (su carro está cerca del clandestino; maneja a casa), `Dormir` (en casa, debe ir a la cama),
   `Flashback` (escena del flashback), `Despertar` (paso corto: Tony pone el estado inicial de la historia), `PeleaCasera`
   (anochecer del presente; el jugador va al clandestino a su primera pelea casera), `Continuara` (pantalla de fin de la demo),
   `Libre` (después de la demo: juego libre). Nunca cambia el orden; se agregan valores solo al final.
2. `Core/Historia/IHistoria.cs`: `EtapaPrologo Etapa { get; }` y `event Action<EtapaPrologo> EtapaCambio` (una vez por cambio, con
   la etapa nueva; también al cargar una partida si cambió). Comentario: la implementa Tony (`ServicioHistoria`, Progresion/,
   dentro de `ServiciosProgresion.prefab` anidado en `JugadorAPie.prefab`, T-124) y la avanza el director del prólogo (T-132);
   se busca en `Registro<IHistoria>`. Alan solo la lee para que su mundo acompañe (ejemplos: en `RegresoACasa` y después, el
   carro del jugador estacionado cerca del clandestino; en `PeleaTutorial` y `PeleaCasera` el clandestino abierto; casino y
   mercado negro cerrados en toda la demo). Sin `IHistoria` registrada, el mundo se comporta como en `Libre`. Es `IGuardable` con
   clave `historia`.
3. `Core/Jugador/AparienciaJugador.cs`: `readonly struct` con `int Cara`, `int Ropa`, `int Implante` (0 = sin implante visible),
   constructor con los tres, `static AparienciaJugador Predeterminada` (0, 0, 0) e igualdad por valor (`IEquatable`, `==`, `!=`,
   `GetHashCode`). Comentario: son índices al catálogo de Tony (`CatalogoApariencia`, Personaje/, T-123); nunca son negativos y
   Alan no los interpreta: para mostrar al jugador coloca `Personaje/Prefabs/CuerpoJugador.prefab` (Tony), que se pinta solo con
   el perfil registrado.
4. `Core/Jugador/IPerfilJugador.cs`: agrega `AparienciaJugador Apariencia { get; }` (nunca inválida: quien implementa pone
   `Predeterminada` de respaldo) y cambia los comentarios: `Cambio` avisa también cuando cambia la apariencia; quita "La apariencia
   ... se agrega en T-F6 con otra tarea de Core; no la supongas". Para que compile: en `Progresion/Perfil/ServicioPerfil.cs` agrega
   SOLO `public AparienciaJugador Apariencia => AparienciaJugador.Predeterminada;` (T-123 la implementa de verdad) y en el doble
   `PerfilFalso` de `Tests/EditMode/Core/ContratosMechaPruebas.cs` lo mismo.
5. `Core/Historia/IPuntoHistoria.cs`: `string Id { get; }` (único en la escena; prefijos `Flashback_` para planos de cámara y
   `Prologo_` para la ruta del prólogo, por ejemplo "Flashback_Plano_1", "Prologo_Carro", "Prologo_Esquina_1"), `Pose Pose { get; }`
   (para un plano: posición y rotación de la cámara; para la ruta: punto en el piso) y `void Activar(bool activo)` (Tony lo llama
   con true cuando empieza el plano que usa este punto y con false cuando termina; Alan puede prender ahí una animación o luces;
   para puntos de ruta no se llama; nunca debe lanzar). Comentario: la implementa Alan en sus escenas (Flashback.unity y Ciudad.unity)
   y se agrega a `Registro<IPuntoHistoria>` en `OnEnable` (se quita en `OnDisable`). Los ids que Tony busca están en sus datos
   (`DatosFlashback` y `DatosPrologo`, T-131 y T-132) y en docs/revision-tony.md; si falta uno, Tony usa un respaldo y avisa con
   warning, sin error.
6. `Core/Guardado/IGuardable.cs`: en "Claves en uso" agrega `historia` (Tony).
7. `Tests/EditMode/Core/ReglasEnsambladosPruebas.cs`: un subensamblado `MechaCyberpunk.<Área>.<Sub>` (y su `.Editor`, y los de
   pruebas `MechaCyberpunk.Tests.<Modo>.<Área>.<Sub>`) tiene el dueño de `<Área>`; un `<Área>` que no está en la tabla sigue siendo
   desconocido. Casos nuevos: `MechaCyberpunk.Progresion.Prologo` que referencia Mecha, Pelea y UI no da error;
   `MechaCyberpunk.Progresion.Prologo` que referencia `MechaCyberpunk.Mundo` da 1 error que nombra a los dos;
   `MechaCyberpunk.Progresion.Prologo.Editor` es de Tony; `MechaCyberpunk.Inventada.Sub` da "nombre desconocido". El ciclo de "falta
   el asmdef" no cambia.
8. Pruebas en `Tests/EditMode/Core/ContratosHistoriaPruebas.cs`, con dobles definidos en la prueba (`HistoriaFalsa`,
   `PuntoHistoriaFalso`): entran y salen de su `Registro<T>`; `Registros.LimpiarTodos()` deja los dos vacíos; `EtapaPrologo.Ninguna`
   vale 0 y `Libre` es el último valor; `AparienciaJugador` compara por valor (dos iguales son `==`, distintas no) y `Predeterminada`
   es (0, 0, 0). `Registros.LimpiarTodos()` en SetUp y TearDown.

Nota para el orquestador (no es trabajo del implementador): al integrar a main, anota en docs/revision-tony.md el pedido a Alan de
D-tony-72 (qué prefabs de Tony coloca en Ciudad y Flashback, qué ids de `IPuntoAparicion` e `IPuntoHistoria` pone y que puede leer
`IHistoria`).

## Carpetas permitidas
Assets/_Proyecto/Core/Historia/ (nueva), Assets/_Proyecto/Core/Jugador/AparienciaJugador.cs (nuevo), Assets/_Proyecto/Core/Jugador/IPerfilJugador.cs,
solo el comentario de claves en Core/Guardado/IGuardable.cs, la propiedad `Apariencia` en Assets/_Proyecto/Progresion/Perfil/ServicioPerfil.cs,
Assets/_Proyecto/Tests/EditMode/Core/
No tocar: el resto de Core/ y de las áreas de Tony, las áreas de Alan, escenas, prefabs, ProjectSettings/, Packages/, herramientas/, CLAUDE.md.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa: compilar OK, EditMode y PlayMode sin fallas nuevas, con ContratosHistoriaPruebas contada
- [ ] ContratosHistoriaPruebas y los casos nuevos de ReglasEnsambladosPruebas pasan (buscados por nombre en el XML)
- [ ] `git diff tony/fase-5 --stat` solo muestra archivos de las carpetas permitidas; en IGuardable.cs solo cambian líneas de comentario y en
      ServicioPerfil.cs solo se agrega la propiedad `Apariencia`
- [ ] Cada miembro público de `IHistoria`, `IPuntoHistoria`, `AparienciaJugador` y cada valor de `EtapaPrologo` tiene `///`
- [ ] Ningún archivo de Core/ referencia un ensamblado de área (ReglasEnsamblados.ProyectoReal sigue pasando)

## Criterios de sensación (para un humano)
- Alan (o Tony con su mirada) lee solo los `///` de IHistoria, EtapaPrologo e IPuntoHistoria y sabe, sin preguntar, qué tiene que
  poner en Flashback.unity y en Ciudad.unity y qué puede hacer su mundo en cada etapa.

## Pruebas automáticas
- EditMode (Tests/EditMode/Core/): ContratosHistoriaPruebas y los casos nuevos de ReglasEnsambladosPruebas.

## Fuera de alcance
- Implementar los contratos (ServicioHistoria T-124, apariencia T-123, puntos de prueba T-127 y T-131; los de Alan son suyos)
- Escenas en Build Settings (T-137, bloqueada) y cualquier cambio a CLAUDE.md (si Tony quiere mencionar el subensamblado, lo hace él)
