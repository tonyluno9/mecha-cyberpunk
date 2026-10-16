# T-102 Cartera del jugador y datos de la economía
Dueño: Tony | Rama: tony/T-102-cartera-jugador | Modelo: Sonnet 5.5, medium
Depende de: T-100

## Objetivo
El jugador tiene dinero de verdad: `ServicioCartera` registra `ICartera` (y se guarda como `IGuardable`), empieza con el dinero
inicial de la demo y vive en `ServiciosEconomia.prefab`, anidado en `JugadorAPie.prefab`. Los números de la economía de la demo
(dinero inicial, renta, costo de reparación) están en un solo asset, y la regla de cuánto cuesta reparar es una función pura que
usan el banco del taller y el HUD.

## Contexto
- Diseño: economia-misiones.md (las peleas son la fuente principal de dinero; quedar en negativo por reparar es intencional;
  precios y renta son preguntas abiertas), mecha-taller.md (reparación completa en el taller, con costo según el daño).
- Decisiones: D-038 (los modelos viven en Economia/), D-039 (dinero en enteros; `CargarDeuda` puede dejarlo en negativo),
  D-055 (un solo prefab del jugador: los servicios van anidados en `JugadorAPie.prefab`). Propuestas: D-tony-44 (valores),
  D-tony-45 (dónde vive la cartera), D-tony-46 (reparación completa), D-tony-50 (guardado por piezas).
- Contratos: Core/Economia/ICartera.cs, Core/Guardado/IGuardable.cs y `MotivoDinero.Carga` (T-100).
- Código existente: Economia/Cartera/ModeloCartera.cs (lógica pura, sin registrar), Economia/Editor/CreadorDatosEconomia.cs
  (precios de prueba de T-033), Personaje/Editor/CreadorJugadorAPie.cs (anida `ServiciosProgresion.prefab`; anidar el nuevo
  prefab igual), Progresion/Calor/ServicioCalor.cs (ejemplo de servicio que envuelve un modelo y se registra).

## Qué hacer
1. `Economia/Datos/DatosEconomia.cs` (ScriptableObject) y `DatosEconomia_Base.asset`, con tooltips y unidades: dinero inicial
   500 ($), costo de reparación por punto de vida 10 ($/punto), costo mínimo de una reparación 50 ($), renta 500 ($) y días entre
   rentas 7. Valores provisionales (D-tony-44).
2. `Economia/Reparacion/ReglaReparacion.cs` (estática, pura): `int Costo(float danio, int costoPorPunto, int costoMinimo)`:
   0 si `danio` < 0.5; si no, el mayor entre `costoMinimo` y `ceil(danio × costoPorPunto)`.
3. `Economia/FormatoDinero.cs` (estática, pura): `string Texto(int dinero)` → "$500", "$1,250", "-$150", "$0" (separador de miles
   con coma, sin decimales). La usarán el banco del taller (Mecha/) y el HUD (UI/).
4. `ModeloCartera`: agrega `Restablecer(int dinero)`, que fija el dinero y avisa `Cambio(diferencia, MotivoDinero.Carga)` solo si
   cambió.
5. `Economia/Cartera/ServicioCartera.cs : MonoBehaviour, ICartera, IGuardable` (clave `cartera`): envuelve `ModeloCartera` con el
   dinero inicial de `DatosEconomia`, reenvía `Cambio`, se agrega a `Registro<ICartera>` y `Registro<IGuardable>` en `OnEnable` y se
   quita en `OnDisable`. `Guardar` → JSON con el dinero; `Cargar` → `Restablecer`; texto que no se entiende → warning y nada cambia.
   Expón `DatosEconomia Datos` para quien necesite los precios. Sin `DatosEconomia`: error en consola y se apaga (como ServicioCalor).
6. `Economia/Pruebas/DineroDePrueba.cs`: tecla 7 abona 500 y tecla 8 carga 500 de deuda (`MotivoDinero.Otro`) a la cartera
   registrada. Solo va en escenas de pruebas.
7. Creador `Economia/Editor/CreadorServiciosEconomia.cs` (con `MenuItem`, idempotente): crea `DatosEconomia_Base.asset` y
   `Economia/Prefabs/ServiciosEconomia.prefab` con `ServicioCartera`. Luego `CreadorJugadorAPie` anida `ServiciosEconomia.prefab` en
   `JugadorAPie.prefab` como ya anida `ServiciosProgresion.prefab` (agrega la referencia a `MechaCyberpunk.Economia` en el asmdef de
   Personaje/Editor). `CreadorEscenaCalle` pone un `DineroDePrueba` en Pruebas_Calle (agrega la referencia a Economia en el asmdef
   de Mecha/Editor). Regenera los prefabs y Pruebas_Calle con sus creadores.
8. Pruebas:
   - EditMode `Tests/EditMode/Economia/ReglaReparacionPruebas.cs`: daño 0 → 0; daño 0.4 → 0; daño 3 con mínimo 50 → 50; daño 65
     → 650; daño 64.2 → 642; daño 64.25 → 643 (redondea hacia arriba).
   - EditMode `FormatoDineroPruebas`: 0, 500, 1250, 1000000, -150 y -1250 con los textos de arriba.
   - EditMode `ModeloCarteraPruebas`: `Restablecer` avisa con `Carga` y la diferencia correcta, y no avisa si el dinero es igual.
   - EditMode `DatosEconomiaAssetsPruebas`: `DatosEconomia_Base` existe con los 5 valores de arriba.
   - PlayMode `Tests/PlayMode/Economia/ServicioCarteraPruebas.cs`: con `JugadorAPie.prefab` solo en una escena vacía hay
     exactamente una `ICartera` registrada, con 500, y un `IGuardable` con clave `cartera`; `Guardar` con 1234 y `Cargar` tras
     cambiar el dinero lo devuelve a 1234 y avisa con `Carga`; `Cargar("basura")` no cambia nada y avisa un warning (LogAssert);
     al destruir el jugador los dos registros quedan sin la cartera; en Pruebas_Calle, la tecla 7 sube 500 y la 8 baja 500 (entrada
     simulada).

## Carpetas permitidas
Assets/_Proyecto/Economia/, Assets/_Proyecto/Personaje/Editor/ (y JugadorAPie.prefab regenerado por su creador),
Assets/_Proyecto/Mecha/Editor/CreadorEscenaCalle.cs y su asmdef, Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity (con su creador),
Assets/_Proyecto/Tests/EditMode/Economia/, Assets/_Proyecto/Tests/PlayMode/Economia/
No tocar: Core/, Pruebas_Tony.unity, el HUD (dinero en pantalla es T-110), las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] ReglaReparacionPruebas, FormatoDineroPruebas, ModeloCarteraPruebas, DatosEconomiaAssetsPruebas y ServicioCarteraPruebas pasan
- [ ] Con `JugadorAPie.prefab` solo en una escena vacía hay exactamente una `ICartera` registrada con 500 (prueba automática)
- [ ] `ServiciosEconomia.prefab` está anidado en `JugadorAPie.prefab` (una sola instancia)
- [ ] Ningún asmdef de Tony referencia un área de Alan (ReglasEnsamblados pasa)
- [ ] Consola sin errores en las pruebas

## Criterios de sensación (para un humano)
- Ninguno en esta tarea: el dinero se ve en pantalla en T-110.

## Pruebas automáticas
- EditMode: ReglaReparacionPruebas, FormatoDineroPruebas, ModeloCarteraPruebas, DatosEconomiaAssetsPruebas.
- PlayMode: ServicioCarteraPruebas.

## Fuera de alcance
- Mostrar el dinero (T-110), premios (T-106), renta (T-107), reparar (T-109), el archivo de guardado (T-111)
- Apuestas, venta de piezas y kits de reparación (T-F7)
