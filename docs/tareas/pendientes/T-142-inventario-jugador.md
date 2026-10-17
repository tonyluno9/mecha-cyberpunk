# T-142 Inventario del jugador y objetos vendibles
Dueño: Tony | Rama: tony/T-142-inventario-jugador | Modelo: Sonnet 5.5, medium
Depende de: T-140

## Objetivo
El jugador tiene un inventario que se guarda: cuántos tiene de cada objeto (piezas viejas o ganadas, chatarra, kits de reparación), con un
tope por objeto y un valor de venta. Es la base de la venta de piezas (T-147), de las piezas ganadas por apuesta (T-144) y de los kits
(T-146, T-148); el bloque de piezas por ramas y niveles lo extenderá.

## Contexto
- Diseño: economia-misiones.md ("Vendes piezas viejas del mecha, piezas ganadas en peleas por apuesta y chatarra de robos en el mercado
  negro"; criterio "Puedo vender una pieza vieja en el mercado negro"), mecha-taller.md ("kits de reparación limitados del mercado negro").
- Decisiones: D-039 (dinero entero), D-tony-45 (servicios en `ServiciosEconomia.prefab`, anidado en `JugadorAPie.prefab`), D-tony-50
  (guardado por piezas `IGuardable`). Propuestas nuevas: D-tony-79 y D-tony-86 (valores).
- Ejemplos: `Economia/Cartera/ModeloCartera.cs` y `ServicioCartera.cs` (modelo puro + servicio que se registra y se guarda),
  `Economia/Editor/CreadorDatosEconomia.cs` y `CreadorServiciosEconomia.cs` (assets y prefab con código), `Economia/Pruebas/DineroDePrueba.cs`.

## Qué hacer
1. `Economia/Inventario/TipoObjeto.cs` (`Pieza`, `Chatarra`, `KitReparacion`; solo se agregan valores al final) y
   `Economia/Inventario/DatosObjeto.cs` (ScriptableObject): `id` (único), `nombre`, `tipo`, `valorVenta` ($, 0 = no se vende), `maximo`
   (cuántos puede llevar, ≥ 1) y `oferta` (opcional, `DatosOferta` de Core: la oferta de tienda que entrega este objeto al comprarse).
2. `Economia/Inventario/ModeloInventario.cs` (C# puro): `Cantidad(DatosObjeto)`, `PuedeAgregar(objeto, n)`, `Agregar(objeto, n)` (devuelve
   cuántos entraron, respeta el tope), `Quitar(objeto, n)` (false si no hay suficientes, sin cambiar nada), `Todos` (objetos con cantidad > 0,
   en orden de llegada) y `event Action Cambio`. Cantidades nunca negativas; n ≤ 0 no hace nada.
3. `Economia/Inventario/CatalogoObjetos.cs` (ScriptableObject con la lista de todos los `DatosObjeto`; busca por id) y
   `Economia/Inventario/ServicioInventario.cs`: envuelve el modelo, se registra en `Registro<ServicioInventario>` (el inventario no es contrato
   de Core, D-tony-79) y en `Registro<IGuardable>` con clave `inventario` (JSON con pares id-cantidad; un id que ya no existe en el catálogo se
   ignora con warning; texto roto no cambia nada y avisa). Va en `ServiciosEconomia.prefab` (con su creador; no cambies a mano el prefab).
4. Assets en `Economia/Datos/` con el creador: `CatalogoObjetos_Base`, `DatosObjeto_BrazoChatarra` (Pieza, venta $300, máximo 5),
   `DatosObjeto_Chatarra` (Chatarra, venta $50, máximo 20) y `DatosObjeto_KitReparacion` (KitReparacion, venta 0, máximo 3, oferta
   `DatosOferta_KitReparacion`). Valores de D-tony-86.
5. Herramienta de pruebas `Economia/Pruebas/InventarioDePrueba.cs` (como DineroDePrueba): tecla 9 agrega un Brazo de chatarra y tecla 0 un kit.
   Va en las escenas de pruebas de economía (T-143), no en el prefab del jugador.
6. EditMode `Tests/EditMode/Economia/ModeloInventarioPruebas.cs` y PlayMode `Tests/PlayMode/Economia/ServicioInventarioPruebas.cs`.

## Carpetas permitidas
Assets/_Proyecto/Economia/ (Inventario/, Datos/, Editor/, Prefabs/ solo con el creador, Pruebas/), Assets/_Proyecto/Tests/EditMode/Economia/,
Assets/_Proyecto/Tests/PlayMode/Economia/
No tocar: Core/, Mecha/, Pelea/, UI/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ModeloInventarioPruebas: tope respetado (agregar 5 kits con máximo 3 deja 3 y devuelve 3), quitar más de lo que hay no cambia nada,
      n ≤ 0 no hace nada, `Cambio` se avisa una vez por cambio real
- [ ] ServicioInventarioPruebas (PlayMode): `JugadorAPie.prefab` en escena vacía trae un solo `ServicioInventario` vacío y un `IGuardable`
      `inventario`; guardar con 2 brazos y 1 kit, vaciar y cargar devuelve 2 y 1; un id desconocido se ignora con warning; texto roto no cambia nada
- [ ] EditMode lee los assets: cada `DatosObjeto` del catálogo tiene id único y no vacío, `maximo` ≥ 1, y ningún objeto con `oferta` se vende
      por su precio de compra o más (`valorVenta` < `oferta.Precio`)

## Criterios de sensación (para un humano)
- Ninguno todavía (no hay pantalla de inventario; se ve en T-146 a T-148).

## Pruebas automáticas
- EditMode: ModeloInventarioPruebas y la de assets. PlayMode: ServicioInventarioPruebas.

## Fuera de alcance
- Comprar, vender y usar objetos (T-146 a T-148). Piezas equipables con rama y nivel (bloque de piezas). Chatarra de robos (misiones).
