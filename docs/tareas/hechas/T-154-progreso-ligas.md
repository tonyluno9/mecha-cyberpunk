# T-154 Progreso de las ligas: datos, modelo y servicio que se guarda
Dueño: Tony | Rama: tony/T-154-progreso-ligas | Modelo: Sonnet 5.5, medium
Depende de: T-152

## Objetivo
La partida sabe en qué liga va el jugador, cuál es el siguiente rival que puede retar, a quiénes ya venció y cuántos campeones lleva, y lo
conserva al guardar y cargar. Vencer al quinto rival (el campeón) abre la siguiente liga. Todo sin pantallas ni peleas todavía: la cartelera
del mostrador (T-159, T-160) y el bloque de piezas leen este progreso.

## Contexto
- Diseño: personaje-progresion.md (4 ligas de 5 rivales, el último es el campeón; vencerlo te sube de liga; "Retas a un rival de una liga más
  alta: no se puede. Propuesta"; "Perder una pelea no es game over ... puedes volver a retar").
- Contrato: `Core/Ligas/IProgresoLigas.cs` (T-152): léelo primero, sus `///` mandan.
- Decisiones: D-tony-50 (guardado por piezas), D-tony-79 (servicios en prefabs de servicios). Propuestas nuevas: D-tony-94 (dónde viven las
  ligas) y D-tony-95 (reglas del progreso).
- Ejemplos: `Progresion/Historia/ModeloHistoria.cs` y `ServicioHistoria.cs` (modelo puro + servicio que se registra y se guarda por clave),
  `Progresion/Editor/CreadorDatosProgresion.cs` (agrega servicios a `ServiciosProgresion.prefab` de forma idempotente),
  `Progresion/Pruebas/HistoriaLibreDePrueba.cs` y `Economia/Pruebas/DineroDePrueba.cs` (dobles con teclas de prueba).

## Qué hacer
1. `Progresion/Datos/DatosLigas.cs` (ScriptableObject): `nombres` (un nombre por liga, en orden), `rivalesPorLiga` (≥ 2; el último es el
   campeón) y `fraccionPremioRevancha` (0 a 1; la usa la cartelera en T-159). `CantidadLigas` = cantidad de nombres. Asset
   `Progresion/Datos/DatosLigas_Base.asset`: "SÓTANO", "CALLE", "DISTRITO", "CAMPEONATO"; 5 rivales; 0.5. Con su creador (idempotente: no
   pisa valores de un asset existente).
2. `Progresion/Ligas/ModeloLigas.cs` (C# puro): se construye con la cantidad de ligas y de rivales por liga. Estado: `LigaActual` (1..N),
   `Siguiente` (índice 1..rivales del siguiente rival por vencer en la liga actual; 0 si ya es campeón del clandestino), `CampeonesVencidos`.
   Consultas: `EsJefe(indice)`, `Vencido(liga, indice)`, `PuedeRetar(liga, indice)` (vencido, o el siguiente de la liga actual; nunca una liga
   más alta ni un rival salteado). `RegistrarVictoria(liga, indice)` devuelve qué pasó: nada (revancha o no permitido, sin cambios), avanzó,
   campeón vencido (abre la siguiente liga: `LigaActual + 1`, `Siguiente = 1`) o campeón del clandestino (última liga). `Poner(liga,
   siguiente, campeones)` con validación (para cargar y para pruebas), `Reiniciar()`. Eventos `Cambio` y `CampeonVencido(int liga)` (este solo
   en la primera victoria sobre un campeón, nunca al `Poner`).
3. `Progresion/Ligas/ServicioLigas.cs`: implementa `IProgresoLigas` (nombres de `DatosLigas`) e `IGuardable` (clave `ligas`; guarda liga,
   siguiente y campeones; al cargar valida y, si no entiende, avisa con warning y no cambia nada; al cargar avisa `Cambio`, nunca
   `CampeonVencido`). Se registra en `Registro<IProgresoLigas>`, `Registro<IGuardable>` y en `Registro<ServicioLigas>` (para Tony: expone
   `Siguiente`, `Vencido`, `PuedeRetar`, `RegistrarVictoria`, `Poner`, `Reiniciar` y `Datos`). Va en `ServiciosProgresion.prefab` con su creador.
4. `Progresion/Pruebas/LigasDePrueba.cs` (doble de teclas, como `DineroDePrueba`): una `InputAction` en el inspector (tecla L por defecto en el
   prefab o el objeto que lo cree; revisa antes que L no choque con otra tecla de prueba ni del jugador a pie) que registra la victoria del
   siguiente rival sin pelear, para saltar al jefe en las pruebas a mano. No se coloca en ninguna escena en esta tarea (lo hace T-159).
5. EditMode `Tests/EditMode/Progresion/ModeloLigasPruebas.cs` y PlayMode `Tests/PlayMode/Progresion/ServicioLigasPruebas.cs` (registro,
   guardar y cargar con `ServicioGuardado` y ruta forzada, texto roto no cambia nada).

## Carpetas permitidas
Assets/_Proyecto/Progresion/ (Datos/, Ligas/ nueva, Editor/, Pruebas/, Prefabs/ solo con el creador), Assets/_Proyecto/Tests/EditMode/Progresion/,
Assets/_Proyecto/Tests/PlayMode/Progresion/
No tocar: Core/, Progresion/Prologo/, Pelea/, IA/, UI/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ModeloLigasPruebas: empieza en liga 1, siguiente 1, 0 campeones; vencer del 1 al 4 avanza; vencer al 5 de la liga 1 deja liga 2,
      siguiente 1, 1 campeón y avisa `CampeonVencido(1)` una vez; volver a vencer al campeón de la liga 1 no avisa ni cambia nada (revancha);
      retar al 3 cuando el siguiente es el 2, o a la liga 3 estando en la 2, da `PuedeRetar` false y `RegistrarVictoria` no cambia nada; al
      vencer al campeón de la liga 4 queda liga 4, siguiente 0, 4 campeones y el resultado es "campeón del clandestino"; `Poner` con valores
      fuera de rango no cambia nada
- [ ] ServicioLigasPruebas (PlayMode): con `ServiciosProgresion.prefab` instanciado, `Registro<IProgresoLigas>.Actual` es el servicio y
      `NombreLiga(2)` es "CALLE"; tras vencer hasta liga 2 siguiente 3, guardar, `Reiniciar` y cargar, vuelve a liga 2 siguiente 3 y avisa
      `Cambio` pero no `CampeonVencido`; un texto roto deja todo igual con un warning y sin excepciones; la partida real del humano no se toca
- [ ] El creador corrido dos veces no cambia `ServiciosProgresion.prefab` ni `DatosLigas_Base.asset` (md5 igual)

## Criterios de sensación (para un humano)
- Ninguno todavía (la cartelera llega en T-159 y T-160).

## Pruebas automáticas
- EditMode: ModeloLigasPruebas. PlayMode: ServicioLigasPruebas.

## Fuera de alcance
- Rivales, premios y momios por liga (T-157, T-158). Retar desde el mostrador (T-159). Avisos de subir de liga (T-161). Niveles de pieza (bloque 3).
