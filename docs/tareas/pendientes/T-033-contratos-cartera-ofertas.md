# T-033 Contratos de cartera y ofertas
Dueño: Tony (tarea de Core, D-032, con su implementación en Economia/) | Rama: tony/T-033-contratos-cartera-ofertas | Modelo: Sonnet 5.5, medium
Depende de: T-030 (carpeta Economia/ y su ensamblado de pruebas), T-032 (`IdLugar`)

## Objetivo
En Core/ existen los contratos del dinero del jugador (`ICartera`) y de lo que se vende en cada lugar (`DatosOferta`,
`DatosTienda`, `ITienda`), y en Economia/ su primera implementación en lógica pura con datos de ejemplo. Así el casino, el
mercado negro, la clínica y el bar de Alan pueden mostrar precios y cobrar con la cartera de Tony sin referenciar Economia/.

## Contexto
- Diseño: docs/diseno/economia-misiones.md (las peleas son la fuente principal de dinero; si reparar cuesta más que el premio
  quedas en negativo y es intencional; se vende en el mercado negro), docs/diseno/personaje-progresion.md (implantes en la
  clínica; el doctor cobra por levantarte), docs/diseno/mecha-taller.md (kits de reparación del mercado negro).
- Precios: son pregunta abierta del diseño. Los de esta tarea son de prueba y se dicen así en el asset.
- Decisiones: D-005 (las clases de datos compartidas viven en Core, los assets en la carpeta del dueño), D-027.
  Propuestas: D-tony-6 (Core solo lleva contratos; los modelos van en el área de Tony) y D-tony-7 (dinero en enteros y deuda
  permitida) en docs/decisiones-pendientes-tony.md.
- Quién implementa: `ICartera` e `ITienda` las implementa Tony (Economia/). Alan las consume desde su mundo (A-F5) buscando
  en `Registro<ICartera>` y `Registro<ITienda>`; en sus pruebas usa dobles propios. La parte de Core se integra a main en
  cuanto el verificador la apruebe (la tarea entera va junta).

## Qué hacer
1. Core, en `Core/Economia/`, un tipo por archivo y con comentarios `///` de contrato:
   - `MotivoDinero`: `enum { Premio, Apuesta, Reparacion, Renta, Compra, Venta, Casino, Clinica, Otro }`.
   - `ICartera`: `int Dinero`; `event Action<int, MotivoDinero> Cambio` (diferencia con signo y motivo);
     `bool PuedePagar(int cantidad)`; `bool IntentarPagar(int cantidad, MotivoDinero motivo)` (solo cobra si alcanza; con
     cantidad <= 0 devuelve false y no cambia nada); `void Abonar(int cantidad, MotivoDinero motivo)` (ignora <= 0);
     `void CargarDeuda(int cantidad, MotivoDinero motivo)` (resta aunque quede negativo; para reparaciones y el doctor).
   - `TipoOferta`: `enum { Pieza, Implante, KitReparacion, Municion, Servicio, Consumible }`.
   - `DatosOferta` (ScriptableObject, `[CreateAssetMenu]`): `id` (texto único), `nombre`, `descripcion`, `precio` (int, `[Min(0)]`),
     `tipo`. Propiedades de solo lectura.
   - `DatosTienda` (ScriptableObject): `IdLugar lugar`, `nombre`, lista de `DatosOferta`. En `OnValidate` avisa con
     `Debug.LogWarning` si hay ofertas nulas o ids repetidos.
   - `ResultadoCompra`: `enum { Comprado, SinDinero, NoDisponible }`.
   - `ITienda`: `IdLugar Lugar`, `string Nombre`, `IReadOnlyList<DatosOferta> Ofertas`, `bool EstaDisponible(DatosOferta oferta)`,
     `ResultadoCompra Comprar(DatosOferta oferta, ICartera cartera)`, `event Action<DatosOferta> Comprado`.
     Contrato: el mundo de Alan decide si el lugar está abierto antes de mostrar la tienda; la tienda solo cobra y avisa.
2. Economia (lógica pura, sin MonoBehaviour):
   - `Economia/Cartera/ModeloCartera.cs : ICartera`, con dinero inicial en el constructor.
   - `Economia/Tiendas/TiendaBasica.cs : ITienda`, construida con un `DatosTienda`. `Comprar` devuelve `NoDisponible` si la
     oferta es null o no está en la tienda, `SinDinero` si `IntentarPagar` falla, y `Comprado` (avisando `Comprado` una vez)
     si cobró. Usa `MotivoDinero.Compra`, o `Clinica` si la tienda es de la clínica.
3. Datos de ejemplo en `Economia/Datos/`, creados con un creador de editor (`Economia/Editor/CreadorDatosEconomia.cs`, asmdef
   `MechaCyberpunk.Economia.Editor` solo para Editor, con MenuItem "Mecha Cyberpunk/Crear T-033"):
   - Mercado negro: `DatosOferta_KitReparacion` (300, KitReparacion), `DatosOferta_MunicionMisil` (500, Municion),
     `DatosOferta_BrazoChatarra` (1200, Pieza) en `DatosTienda_MercadoNegro`.
   - Clínica: `DatosOferta_ImplanteOjos` (5000, Implante) en `DatosTienda_Clinica`.
   - Bar: `DatosOferta_Bebida` (50, Consumible) en `DatosTienda_Bar`.
   - En la descripción de cada oferta: "Precio de prueba".

## Carpetas permitidas
Assets/_Proyecto/Core/Economia/, Assets/_Proyecto/Economia/, Assets/_Proyecto/Tests/EditMode/Core/ y
Assets/_Proyecto/Tests/EditMode/Economia/
No tocar: el resto de Core/, las demás áreas, escenas, ProjectSettings/, Packages/ ni herramientas/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa con las pruebas nuevas contadas, y `verificar.sh ensamblados` ya lista
      MechaCyberpunk.Tests.EditMode.Economia
- [ ] ModeloCarteraPruebas pasa: `IntentarPagar` con dinero suficiente resta y avisa una vez con diferencia negativa; sin
      dinero suficiente devuelve false, no cambia y no avisa; cantidades <= 0 no hacen nada; `Abonar` suma y avisa;
      `CargarDeuda` puede dejar el dinero negativo; `PuedePagar` coincide con `IntentarPagar`
- [ ] TiendaBasicaPruebas pasa: comprar con dinero da `Comprado`, resta el precio y avisa; sin dinero da `SinDinero` y no
      cobra; una oferta de otra tienda o null da `NoDisponible`; la tienda de la clínica cobra con motivo `Clinica`
- [ ] Los 5 assets de oferta y los 3 de tienda existen con los valores del paso 3 (prueba EditMode que los carga con
      `AssetDatabase` y compara precio, tipo y `IdLugar`)
- [ ] ReglasEnsamblados sigue pasando: Core no referencia Economia y Economia solo referencia Core
- [ ] `git diff main --stat` solo toca las carpetas permitidas

## Criterios de sensación (para un humano)
- Alan, o Tony leyendo como Alan: con `ITienda` y `DatosOferta` podría hacer el mostrador del mercado negro sin preguntar.

## Pruebas automáticas
- EditMode (Tests/EditMode/Economia/): ModeloCarteraPruebas, TiendaBasicaPruebas y la de los assets.

## Fuera de alcance
- Componente de escena de la cartera, guardado, premios de pelea, renta, reparaciones (T-F5)
- Inventario: lo comprado todavía no se entrega a ningún lado
- Fichas del casino (son de Alan, A-F5) y apuestas (T-F7)
