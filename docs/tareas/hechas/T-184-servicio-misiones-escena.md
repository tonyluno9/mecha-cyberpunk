# T-184 El servicio de misiones en escena: ir, recoger, entregar, cobrar, y la escena Pruebas_Misiones
Dueño: Tony | Rama: tony/T-184-servicio-misiones | Modelo: Sonnet 5.5, medium
Depende de: T-182, T-183

## Objetivo
Una misión aceptada se juega a pie en una escena de Tony: el objetivo con su flecha apunta al punto del paso, al llegar se cumple el `IrA`, con
E se recoge y se entrega, y al terminar se cobra el pago, se mueven las reputaciones, se recibe el favor y la chatarra. Fallar o abandonar
castiga un poco. Se guarda lo que importa (qué se hizo y cuándo). Queda armada `Pruebas_Misiones.unity`, la escena de todo el bloque.

## Contexto
- Diseño: economia-misiones.md (criterio "Una misión de barrio sube la reputación del barrio y da un favor usable").
- Decisiones: D-tony-21 (seguimiento del objetivo), D-tony-51 (reloj de prueba), D-tony-81 (mostradores con E). Propuestas: D-tony-147 (una
  misión a la vez; la misión en curso no se guarda: al cargar queda cancelada sin castigo), D-tony-151 (lo de Alan).
- Contratos: `IMisiones`, `IPuntoMision` (T-181); `IObjetivos` (lo fija `ServicioObjetivos`; en `Libre` la historia no fija objetivos),
  `IInteractuable`, `ICartera` (`MotivoDinero.Mision`), `ICalor` (`MotivoCalor.Mision`), `IGuardable`, `IJugador`.
- Código: `ReglaMisiones`, `MaquinaMision`, `DatosMision`, `CatalogoMisiones_Base` (T-183); `ServicioReputacion`, `ServicioFavores` (T-182);
  `Economia/Inventario/ServicioInventario.cs`; `Progresion/Objetivos/ServicioObjetivos.cs`; `UI/Tiendas/MostradorTienda.cs` (modelo de
  `IInteractuable` con texto y `PuedeUsar`); `Progresion/Prologo/Editor/CreadorEscenaEconomia.cs` (modelo de creador de escena con
  `CreadorEscenaPrologo`: cajas, letreros, lugares, aparición, jugador, reloj, guardado, dinero, inventario, historia libre).

## Qué hacer
1. `Progresion/Misiones/ServicioMisiones.cs` (en `ServiciosProgresion.prefab`, con `CatalogoMisiones_Base`): registra `IMisiones`, se registra
   en `Registro<ServicioMisiones>` y es `IGuardable` (clave `misiones`: por id, cuántas veces se cumplió y el último día). API de Tony:
   `Ofertas(origen, idEncargo = null)` (las misiones y su `MotivoMision`), `Motivo(datos)`, `Aceptar(datos)` (solo con `Disponible`; gasta el
   `favorRequerido`), `Abandonar()`, `Actual`, `PasoActual`, `SegundosRestantes`, `CumplirPasoExterno(tipo)` (para `Pelear` y `Escapar`, que
   llegan en T-185) y `FallarExterno(motivo)`.
2. Al empezar cada paso: fija el objetivo (`Objetivo(texto del paso, pose del punto)`) y llama `Activar(true)` al punto (y `false` al anterior).
   `IrA`: se cumple a `radio` m del punto (por la raíz de `IJugador`, a pie, en carro o en mecha). `Recoger` y `Entregar`: un `IInteractuable`
   propio en el punto (solo a pie), texto "E · RECOGER: <texto>" / "E · ENTREGAR: <texto>"; al usarlo se cumple el paso. Al cumplirse un paso
   suma su `calor` (`MotivoCalor.Mision`). Al terminar con éxito: `Abonar(pago, MotivoDinero.Mision)`, `ServicioReputacion.Sumar` de las dos,
   `ServicioFavores.Dar(favor)` (si está lleno, se pierde y se anota en el aviso), chatarra al `ServicioInventario` (recortada a su máximo) y quita
   el objetivo. Al fallar o abandonar: `Penalizacion` y quita el objetivo; los puntos se apagan. Sin `ICartera`, `ServicioInventario` o
   `ServicioReputacion`, esa parte no se hace y avisa un warning una vez.
3. Eventos de Tony para pantallas y HUD (T-186): `Aceptada(datos)`, `PasoCambio`, `Terminada(datos, exito, resumen)` donde `resumen` trae pago,
   chatarra, deltas de reputación y favor (lo que de verdad entró).
4. `Progresion/Pruebas/PuntoMisionDePrueba.cs` (implementa `IPuntoMision`; id, tipo, un cilindro de color por tipo que se enciende con
   `Activar(true)`; solo para escenas de Tony).
5. `Progresion/Prologo/Editor/CreadorEscenaMisiones.cs` (idempotente como `CreadorEscenaEconomia`; las tareas siguientes agregan lo suyo DENTRO de
   `Crear`) genera `Escenas/Tony/Pruebas_Misiones.unity`: piso de unos 90 × 60 m con calles de cajas (una "calle" en cruz y dos callejones),
   jugador a pie con `ServiciosProgresion`, reloj de prueba (tecla J: `AvanzarHasta(8)`, empieza a las 21:00), guardado (F5/F9), dinero,
   inventario, historia libre, `MechaEstacionado.prefab` en un taller con su punto de llamado, lugares de prueba `Casa`, `Bar` y `MercadoNegro`
   (con `MostradorMercadoNegro.prefab`), un `PuntoMisionDePrueba` por cada id de T-183 (y los dos escondites) a 15 m o más uno de otro, con su
   letrero, y `ReaccionReputacionDePrueba` (T-182) en un muro de la calle. La bitácora guarda un mapa con las posiciones.
6. Pruebas:
   - EditMode `Tests/EditMode/Progresion/ServicioMisionesGuardadoPruebas.cs` o similar: guardar y cargar conserva veces y último día; texto roto
     no cambia nada (warning); la misión en curso no se guarda.
   - PlayMode `Tests/PlayMode/Progresion/Misiones/MisionesAPiePruebas.cs` en Pruebas_Misiones con entrada simulada (caminar con WASD hacia el
     punto, E): `Aceptar(barrio_tiendita)` por código (atajo permitido hasta T-186); la flecha apunta a la farmacia; E recoge; el objetivo pasa a
     Doña Lupe; E entrega; dinero +$50, barrio +8, 1 DESCUENTO, `IMisiones.Terminada(Barrio, true)` una vez. Otra con `corp_entrega` (de día: J
     antes de aceptar): dejar pasar 150 s de juego falla, sin pago ni recompensa, corporaciones −3 y barrio igual. `Abandonar` a la mitad castiga igual. La misma
     misión repetible no se ofrece otra vez hasta `AvanzarHasta` al día siguiente. Consola sin errores; `MedidorFrames` sin tirones.

## Carpetas permitidas
Assets/_Proyecto/Progresion/Misiones/, Assets/_Proyecto/Progresion/Pruebas/PuntoMisionDePrueba.cs, Assets/_Proyecto/Progresion/Editor/CreadorDatosProgresion.cs,
Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab (solo con el creador), Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaMisiones.cs
(nuevo), Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity (nueva, solo con el creador), Assets/_Proyecto/Tests/EditMode/Progresion/,
Assets/_Proyecto/Tests/PlayMode/Progresion/Misiones/, docs/capturas/
No tocar: Core/, UI/, Pelea/, Economia/ (solo se usa), las otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena)
- [ ] MisionesAPiePruebas pasa 3 corridas seguidas con los valores del paso 6 (la prueba los calcula desde los assets y los imprime)
- [ ] Correr `CreadorEscenaMisiones` y `CreadorDatosProgresion` dos veces no cambia la escena ni el prefab (md5 igual); en la escena hay un punto
      por cada id de T-183 (la prueba compara con el catálogo)
- [ ] Captura docs/capturas/T-184-1.png: vista aérea de Pruebas_Misiones con los puntos y letreros, revisada a ojo

## Criterios de sensación (para un humano)
- Pruebas_Misiones, Play: con la misión de Doña Lupe aceptada por código (tecla de prueba si se agrega), ¿la flecha y el texto te llevan sin
  perderte? ¿Recoger y entregar con E se siente natural o falta algo (un vecino, un sonido)?

## Pruebas automáticas
- EditMode: guardado del servicio. PlayMode: MisionesAPiePruebas.

## Fuera de alcance
- Pelear y Escapar (T-185), aceptar desde el teléfono, el bar y la calle, y los avisos del HUD (T-186), usar los favores (T-187).
