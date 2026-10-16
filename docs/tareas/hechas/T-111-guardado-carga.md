# T-111 Guardar y cargar la partida
Dueño: Tony | Rama: tony/T-111-guardado-carga | Modelo: Sonnet 5.5, medium
Depende de: T-100, T-102, T-103, T-107

## Objetivo
La partida se guarda en un archivo y se carga: dinero, daño del mecha, renta y adeudo, nombre y alias, y el día y la hora del
reloj vuelven a como estaban. `ServicioGuardado` junta todo lo que implementa `IGuardable` sin conocer a nadie, así Alan agrega su
reloj (y lo que el mundo necesite después) sin tocar el código de Tony.

## Contexto
- Diseño: la hoja de ruta pide para T-F5 "guardado y carga" y para H5 "guardar y cargar conserva todo"; dónde se guarda solo
  (dormir) y el menú de cargar son del prólogo (T-F6).
- Decisiones: D-034, D-038. Propuesta: D-tony-50 (formato, qué se guarda y qué no).
- Contrato: Core/Guardado/IGuardable.cs (T-100): clave única, `Guardar`, `Cargar` que no simula el tiempo ni lanza excepciones.
- Piezas que ya existen: `ServicioCartera` (`cartera`, T-102), `MechaPersistente` (`mecha`, T-103), `ServicioRenta` (`renta`, T-107),
  `RelojDePrueba` (`reloj`, T-107). Falta el perfil: Progresion/Perfil/ServicioPerfil.cs (`Fijar` normaliza con `ReglaAlias`).
- Código: Progresion/Editor/CreadorDatosProgresion.cs (arma `ServiciosProgresion.prefab`, anidado en `JugadorAPie.prefab`),
  Mecha/Editor/CreadorEscenaCalle.cs.

## Qué hacer
1. `Progresion/Guardado/ArchivoPartida.cs` (pura, `[Serializable]`): `version` (1), fecha de guardado (texto ISO) y lista de piezas
   `{clave, datos}`. Funciones puras para pasar de piezas a JSON y de JSON a piezas; JSON inválido, versión desconocida o más nueva
   → resultado "no se pudo" sin excepción.
2. `Progresion/Guardado/ServicioGuardado.cs` (MonoBehaviour, en `ServiciosProgresion.prefab`):
   - `RutaArchivo`: por defecto `Application.persistentDataPath/partida.json`; `FijarRuta(string)` para pruebas.
   - `bool Guardar()`: pide `Guardar()` a cada `IGuardable` registrado (si dos comparten clave, solo el primero y un warning) y
     escribe el archivo de forma atómica (primero `.tmp`, luego reemplaza); avisa `event Action Guardada`. Si falla la escritura,
     false, error en consola y el archivo anterior queda intacto.
   - `bool Cargar()`: lee y valida el archivo; si no existe o no se entiende, false, warning y nada cambia. Si está bien, llama
     `Cargar(datos)` de cada registrado con su clave y guarda las piezas que nadie reclamó para dárselas a quien se registre después
     (por `Registro<IGuardable>.Agregado`); avisa `event Action Cargada`. Las claves que no vienen en el archivo no se tocan.
   - `bool HayPartida` y `void Borrar()`.
   - No guarda ni carga solo al arrancar (eso lo decide el menú de T-F6).
3. `ServicioPerfil` implementa `IGuardable` (clave `perfil`): guarda nombre y alias; `Cargar` usa `Fijar`.
4. `Progresion/Pruebas/GuardadoDePrueba.cs`: F5 guarda y F9 carga con el `ServicioGuardado` de la escena, y escribe en consola qué
   pasó. `CreadorEscenaCalle` lo pone en Pruebas_Calle. `CreadorDatosProgresion` agrega `ServicioGuardado` a
   `ServiciosProgresion.prefab` (idempotente). Regenera con sus creadores.
5. Pruebas:
   - EditMode `Tests/EditMode/Progresion/ArchivoPartidaPruebas.cs`: ida y vuelta de piezas (con comillas, acentos y saltos de línea
     en los datos), JSON roto, versión 2 y lista vacía.
   - PlayMode `Tests/PlayMode/Progresion/ServicioGuardadoPruebas.cs` (ruta en `Application.temporaryCachePath`, se borra en el
     TearDown): con dobles `IGuardable` → guardar, cambiarlos, cargar y quedan como al guardar; pieza que se registra después de
     cargar recibe su dato; clave repetida → warning y se guarda solo la primera; archivo roto → `Cargar` false, warning y nada
     cambia; tras `Guardar` no queda ningún `.tmp`; `Borrar` deja `HayPartida` en false.
   - PlayMode `Tests/PlayMode/Progresion/PartidaCompletaPruebas.cs` (Pruebas_Calle, entrada simulada para F5 y F9): con dinero 1234,
     mecha en 40, perfil "Ana"/"LA CHISPA", adeudo 500 con próximo cobro 15 y el reloj de prueba en el día 9 a las 22:30, F5;
     cambiar todo (dinero, reparar, otro perfil, J dos veces); F9 → todo vuelve a los valores guardados (vida ±0.01, hora ±0.01 h),
     sin cobros de renta al cargar y sin errores.

## Carpetas permitidas
Assets/_Proyecto/Progresion/ (Guardado/ nueva, Perfil/, Pruebas/, Editor/, Prefabs/), Assets/_Proyecto/Mecha/Editor/CreadorEscenaCalle.cs,
Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity (con su creador), Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab (solo si
su creador lo regenera), Assets/_Proyecto/Tests/EditMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Progresion/
No tocar: Core/, Economia/ y Mecha/ (sus piezas ya existen; si una falla al cargar, anótalo en el resumen y no la arregles aquí),
Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] ArchivoPartidaPruebas, ServicioGuardadoPruebas y PartidaCompletaPruebas pasan
- [ ] Guardar y cargar en Pruebas_Calle devuelve dinero, vida del mecha, renta, perfil, día y hora (prueba automática)
- [ ] Un archivo roto no cambia nada ni lanza excepciones (prueba automática)
- [ ] Con `JugadorAPie.prefab` solo en una escena vacía hay exactamente un `ServicioGuardado`
- [ ] Consola sin errores en las pruebas (los warnings esperados se comprueban con LogAssert)

## Criterios de sensación (para un humano)
- Ninguno en esta tarea: cuándo se guarda solo y el menú de cargar son de T-F6.

## Pruebas automáticas
- EditMode: ArchivoPartidaPruebas. PlayMode: ServicioGuardadoPruebas, PartidaCompletaPruebas.

## Fuera de alcance
- Guardar al dormir y el menú "continuar / nueva partida" (T-F6); varias ranuras de guardado; posición del jugador, calor, objetivos
  y estado del llamado (no se guardan, D-tony-50); cifrado o protección contra trampas
