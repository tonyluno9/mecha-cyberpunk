# T-191 Los implantes como datos: tabla, servicio, guardado y balance
Dueño: Tony | Rama: tony/T-191-datos-implantes | Modelo: Sonnet 5.5, medium
Depende de: T-190

## Objetivo
Existen los 8 implantes (4 espacios, 2 niveles) como assets que Tony edita, un servicio que sabe qué tienes instalado, cobra al instalar,
cumple `IImplantes` y se guarda con la partida, y una prueba de balance que vigila que los precios tengan sentido contra los premios de
las ligas. Todavía no hay clínica ni efectos: este es el cerebro que usan T-192 a T-197.

## Contexto
- Diseño: personaje-progresion.md (los 4 espacios y su efecto), economia-misiones.md ("una pelea paga más que una misión de barrio").
- Decisiones: D-tony-85 (con dinero negativo no se compra), D-tony-98 (premios por liga), D-tony-117 (desbloqueo por liga, "SE ABRE EN LA
  LIGA n"). Propuestas: D-tony-165 (tabla), D-tony-166 (reglas de la clínica), D-tony-171 (reglas de balance), D-tony-172 (dónde vive el código).
- Contratos: `IImplantes`, `EspacioImplante`, `EfectosImplantes` (T-190); `ICartera` (`IntentarPagar`, `MotivoDinero.Clinica`),
  `IProgresoLigas.LigaActual` (sin servicio, liga 1), `IGuardable`.
- Modelos: `Progresion/Reputacion/` y `Progresion/Favores/` (T-182: servicio + modelo puro + datos + `CreadorDatosProgresion`),
  `Mecha/Piezas/DatosPieza.cs` y `ReglaTallerPiezas.cs` (nivel, liga mínima, estados de una opción), `Tests/EditMode/Progresion/BalanceMisionesPruebas.cs`
  (prueba de balance que imprime su tabla).
- `Economia/Datos/DatosOferta_ImplanteOjos.asset` y `DatosTienda_Clinica.asset` son datos de prueba de T-033: no se usan ni se borran (los
  revisa `DatosEconomiaAssetsPruebas`).

## Qué hacer
1. `Progresion/Implantes/DatosImplante.cs` (ScriptableObject): id (`implante_<espacio>_<nivel>`), nombre en pantalla, espacio, nivel (1 o 2),
   precio, liga mínima, descripción corta para la clínica ("VES LA VIDA DEL RIVAL Y SU PRÓXIMO ATAQUE") y sus `EfectosImplantes`.
   `Progresion/Implantes/CatalogoImplantes.cs` (lista; buscar por id y por espacio y nivel).
2. Assets en `Progresion/Datos/Implantes/` con un creador idempotente (en `Progresion/Editor/`, como `CreadorMisiones`): los 8 de D-tony-165
   y `CatalogoImplantes_Base`.
3. `Progresion/Implantes/ReglaImplantes.cs` (lógica pura): para un espacio, el nivel instalado, la liga y el dinero, la opción que se ofrece y
   su estado: `Instalar` (con su costo: el precio, o la diferencia si ya tienes el nivel 1 y compras el 2), `SinDinero`, `LigaBaja` (con la
   liga que lo abre), `YaLoMejor`. `Combinar` de todo lo instalado (con `EfectosImplantes.Combinar`).
4. `Progresion/Implantes/ServicioImplantes.cs` en `ServiciosProgresion.prefab` (con `CatalogoImplantes_Base`; el prefab solo con el creador):
   registra `IImplantes` y `Registro<ServicioImplantes>`; es `IGuardable` (clave `implantes`: el id instalado por espacio). API de Tony:
   `Opcion(espacio)`, `Instalar(DatosImplante)` → `ResultadoImplante` (`Instalado`, `SinDinero`, `LigaBaja`, `YaInstalado`, `NoMejora`),
   `Instalado(espacio)` (el `DatosImplante` o null), `QuitarTodos()` (solo para pruebas y herramientas). Cobra con `IntentarPagar(costo,
   MotivoDinero.Clinica)`: no fiado. No sabe de horarios ni de la clínica (eso es de T-193). Sin `ICartera` registrada no instala y avisa un
   warning una vez. Una partida vieja sin la clave carga sin implantes; un id desconocido se ignora con warning.
5. Pruebas:
   - EditMode `Tests/EditMode/Progresion/ReglaImplantesPruebas.cs`: costo de la diferencia; `LigaBaja` con la liga que lo abre; `SinDinero`
     con dinero negativo o corto; `YaLoMejor` con el nivel 2; `Combinar` de 0, 1 y 4 implantes.
   - EditMode `Tests/EditMode/Progresion/ServicioImplantesGuardadoPruebas.cs`: guardar y cargar conserva lo instalado; `Instalado` no se avisa
     al cargar y `Cambio` sí; texto roto no cambia nada (warning); partida sin la clave = sin implantes.
   - EditMode `Tests/EditMode/Progresion/BalanceImplantesPruebas.cs`: las reglas de D-tony-171 leídas desde los assets (premios desde
     `DatosLigas`/`DatosApuestaPelea`/lo que use `BalanceMisionesPruebas`, golpe y salto desde `DatosPersonaje_Base`, vida del pandillero
     desde el valor provisional anotado en D-tony-169 hasta que exista su asset en T-195); imprime la tabla de los 8 con precio, liga y efecto.
     Si una regla falla, se ajusta la tabla de implantes (no las reglas ni los premios) y se anota en la bitácora.

## Carpetas permitidas
Assets/_Proyecto/Progresion/Implantes/ (nueva), Assets/_Proyecto/Progresion/Datos/Implantes/ (nueva, solo con el creador),
Assets/_Proyecto/Progresion/Editor/, Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab (solo con el creador),
Assets/_Proyecto/Tests/EditMode/Progresion/
No tocar: Core/, Personaje/, Mecha/, UI/, Economia/ (solo se usa), escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena)
- [ ] ReglaImplantesPruebas, ServicioImplantesGuardadoPruebas y BalanceImplantesPruebas pasan; la tabla impresa coincide con D-tony-165 (o
      con el ajuste anotado en la bitácora)
- [ ] Correr el creador dos veces no cambia los assets ni `ServiciosProgresion.prefab` (md5 igual)
- [ ] Las pruebas del prólogo y de misiones siguen en verde (el servicio sin implantes no cambia nada): RecorridoPrologoPruebas,
      RecorridoMisionesPruebas

## Criterios de sensación (para un humano)
- Leer la tabla impresa: ¿los nombres suenan a la calle de la ciudad? ¿Juntar para el primer implante (unas 3 o 4 peleas de la liga 1) se
  siente como una meta y no como un muro?

## Pruebas automáticas
- EditMode: ReglaImplantesPruebas, ServicioImplantesGuardadoPruebas, BalanceImplantesPruebas.

## Fuera de alcance
- La clínica y su pantalla (T-193), efectos a pie y cuerpo (T-192), efectos en el mecha (T-194), quitar o vender implantes (no existe:
  D-tony-166). Hackeo.
