# T-182 Reputación de barrio y corporaciones, y favores, como datos y servicios
Dueño: Tony | Rama: tony/T-182-reputacion-favores | Modelo: Sonnet 5.5, medium
Depende de: T-181

## Objetivo
El jugador tiene dos reputaciones (barrio y corporaciones, de −100 a 100, con 5 niveles con nombre) y un bolsillo de favores (descuento,
información y escondite), las dos cosas guardadas con la partida. Las corporaciones que te odian te persiguen más: con corporaciones en
nivel −1 o −2 el calor sube más rápido. Un doble del mundo en las escenas de Tony muestra la reputación "en la ciudad" (grafiti con tu alias y
anuncio que te difama), como ejemplo para Alan. Todavía no hay misiones (T-183 y T-184).

## Contexto
- Diseño: concepto-historia.md ("Hay dos reputaciones que se jalan entre sí"; "Te vuelves el justiciero del barrio: defiendes a los pobres de
  las corporaciones, ganas fama y ellas te odian más"; propuesta de grafitis, saludos y anuncios), economia-misiones.md ("dan reputación y
  favores: descuentos, información y escondites").
- Decisiones: D-tony-109 (el creador no pisa valores de un asset existente). Propuestas nuevas: D-tony-145 (reputación) y D-tony-146 (favores).
- Contratos: `IReputacion`, `TipoReputacion` (T-181), `IGuardable`, `ICalor`.
- Código modelo: `Progresion/Ligas/ModeloLigas.cs` y `ServicioLigas.cs` (modelo puro + servicio que registra el contrato y guarda),
  `Progresion/Calor/ServicioCalor.cs` y `ModeloCalor.cs`, `Progresion/Editor/CreadorDatosProgresion.cs` (agrega servicios a
  `ServiciosProgresion.prefab`, idempotente), `Progresion/Pruebas/` (dobles de escena).

## Qué hacer
1. `Progresion/Reputacion/DatosReputacion.cs` (ScriptableObject): `minimo` −100, `maximo` 100, `umbrales` (15 y 50, simétricos: nivel 1 desde
   15, nivel 2 desde 50, −1 desde −15, −2 desde −50), nombres por nivel para barrio (TRAIDOR, MAL VISTO, DESCONOCIDO, CONOCIDO, JUSTICIERO) y
   corporaciones (ENEMIGO, FICHADO, SIN FICHA, CONTRATISTA, SOCIO), y `multiplicadorCalor` por nivel de corporaciones (−2: 1.3, −1: 1.15,
   0 a 2: 1.0). Todo con `[Tooltip]`.
2. `Progresion/Reputacion/ModeloReputacion.cs` (puro): `Sumar(tipo, cantidad)` recortado a [mínimo, máximo]; `Nivel`, `NombreNivel`;
   eventos `Cambio` y `NivelCambio` como el contrato; `Restablecer(barrio, corporaciones)` para cargar (avisa `Cambio`, no `NivelCambio`).
   Sin tirón automático: cada misión dice cuánto mueve cada reputación (D-tony-145). `ReglaReputacion.MultiplicadorCalor(datos, nivelCorp)`.
3. `Progresion/Reputacion/ServicioReputacion.cs`: registra `IReputacion` e `IGuardable` (clave `reputacion`); `Sumar(tipo, cantidad)` es solo
   para Tony (misiones). `ServicioCalor.Sumar` multiplica la cantidad por `ReglaReputacion.MultiplicadorCalor` con la reputación registrada
   (sin `IReputacion`, ×1). `Enfriar` y `Borrar` no cambian.
4. `Progresion/Favores/TipoFavor.cs` (`Descuento`, `Informacion`, `Escondite`), `DatosFavor.cs` (id, nombre "DESCUENTO", descripción corta,
   tipo, `maximo` 3, y `valor`: la fracción de rebaja para el descuento, 0.3), `ModeloFavores.cs` (cuántos hay de cada tipo; `Dar(tipo)` respeta
   el máximo y devuelve si entró; `Gastar(tipo)` devuelve false sin favores; evento `Cambio`) y `ServicioFavores.cs` (lo registra en
   `Registro<ServicioFavores>` y como `IGuardable` con clave `favores`; un warning si carga un tipo desconocido).
5. Creador idempotente en `CreadorDatosProgresion`: `Progresion/Datos/DatosReputacion_Base.asset`, `DatosFavor_Descuento`, `DatosFavor_Informacion`
   y `DatosFavor_Escondite`, y agrega `ServicioReputacion` y `ServicioFavores` a `ServiciosProgresion.prefab` (sin pisar valores existentes).
6. `Progresion/Pruebas/ReaccionReputacionDePrueba.cs` (doble del mundo de Alan, solo para escenas de Tony): un letrero con texto que lee
   `IReputacion` y el alias del perfil: con barrio en nivel 1 o más dice "GRAFITI: <ALIAS>"; con corporaciones en −1 o menos, "ANUNCIO: <ALIAS> ES
   UN PELIGRO"; si no, vacío. Se actualiza con `Cambio`, no en cada cuadro. Va en Pruebas_Misiones (T-184 la coloca).
7. Ensamblado nuevo de PlayMode `Tests/PlayMode/Progresion/Misiones/` con asmdef `MechaCyberpunk.Tests.PlayMode.Progresion.Misiones` (referencias
   como las de `Tests/PlayMode/Progresion`), para que las pruebas del bloque no alarguen el área Progresion; `playmode-por-area` debe listarla
   como "Progresion.Misiones" (límite 900 s).
8. Pruebas:
   - EditMode `Tests/EditMode/Progresion/ReputacionPruebas.cs`: recorte a ±100; niveles en 14/15/49/50 y −14/−15/−50; nombres; `NivelCambio`
     solo al cruzar un umbral; `Restablecer` no avisa `NivelCambio`; multiplicador de calor por nivel. `FavoresPruebas.cs`: máximo 3, gastar sin
     favores da false, guardar y cargar conserva los tres conteos, texto roto no cambia nada y avisa warning.
   - PlayMode `Tests/PlayMode/Progresion/Misiones/ServiciosReputacionPruebas.cs` con `ServiciosProgresion.prefab` instanciado: `ServicioGuardado`
     guarda y carga reputación y favores (F5/F9 de `GuardadoDePrueba` o su API, con `ServicioGuardado.RutaForzada`); con corporaciones en −50,
     `ICalor.Sumar(10)` sube 13; con 0, 10; `ReaccionReputacionDePrueba` cambia su texto al subir el barrio a 15.

## Carpetas permitidas
Assets/_Proyecto/Progresion/Reputacion/ (nueva), Assets/_Proyecto/Progresion/Favores/ (nueva), Assets/_Proyecto/Progresion/Calor/ServicioCalor.cs,
Assets/_Proyecto/Progresion/Datos/ (solo con el creador), Assets/_Proyecto/Progresion/Editor/CreadorDatosProgresion.cs,
Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab (solo con el creador), Assets/_Proyecto/Progresion/Pruebas/ReaccionReputacionDePrueba.cs,
Assets/_Proyecto/Tests/EditMode/Progresion/, Assets/_Proyecto/Tests/PlayMode/Progresion/Misiones/ (nueva)
No tocar: Core/, UI/, Economia/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena)
- [ ] ReputacionPruebas y FavoresPruebas pasan con los valores del paso 8; ServiciosReputacionPruebas pasa 3 corridas seguidas
- [ ] `AREAS=Progresion.Misiones bash herramientas/verificar.sh playmode-por-area` corre el ensamblado nuevo y dice "límite 900 s"
- [ ] Correr el creador dos veces no cambia ningún asset ni el prefab (md5 igual); las pruebas de calor, guardado y ligas que ya existían siguen
      en verde (el calor sin reputación registrada sube igual que antes)

## Criterios de sensación (para un humano)
- Tony: leer los nombres de los niveles y los umbrales (D-tony-145). ¿JUSTICIERO y ENEMIGO suenan al juego? ¿Que el calor suba 30 % más con
  las corporaciones de enemigas se siente como "te odian más"?

## Pruebas automáticas
- EditMode: ReputacionPruebas, FavoresPruebas. PlayMode: ServiciosReputacionPruebas.

## Fuera de alcance
- Ganar reputación o favores con misiones (T-184), pantallas y avisos (T-186), usar los favores (T-187). Grafitis y anuncios reales en la
  ciudad (Alan, con `IReputacion`).
