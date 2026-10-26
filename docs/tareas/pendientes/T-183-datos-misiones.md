# T-183 Las misiones como datos: tabla, reglas de disponibilidad, máquina de pasos y balance
Dueño: Tony | Rama: tony/T-183-datos-misiones | Modelo: Sonnet 5.5, medium
Depende de: T-181, T-182

## Objetivo
Existen las 11 misiones del bloque (3 de barrio, 3 corporativas, 2 ilegales y 3 robos) como datos, con sus pasos, su pago, lo que mueven
cada reputación, el favor y la chatarra que dan, y el calor que suman. Una regla pura dice si una misión se puede ofrecer y por qué no, y una
máquina pura lleva los pasos de la misión en curso (avanzar, cumplir, fallar, abandonar). Pruebas de balance amarran los criterios del diseño
("Una pelea paga más que una misión de barrio"; "Una misión de barrio sube la reputación del barrio y da un favor usable"; "Aceptar un contrato
corporativo baja la reputación del barrio de forma visible"). Todavía no hay nada en escena (T-184).

## Contexto
- Diseño: economia-misiones.md (complementos: contratos corporativos, misiones del barrio, ilegales y robos; dónde salen; "Vendes ... chatarra
  de robos en el mercado negro"; criterios de arriba), concepto-historia.md ("Puedes aceptar contratos corporativos por dinero, pero te cuestan
  reputación en el barrio"), ciudad.md ("De día ... salen los contratos corporativos. De noche ... aparecen las pandillas").
- Decisiones: D-tony-109, D-tony-136 (modelo de tabla provisional). Propuestas nuevas: D-tony-147 (estructura), D-tony-148 (tabla), D-tony-149
  (balance), D-tony-150 (ensamblados).
- Contratos: `TipoMision`, `TipoPuntoMision`, `IReputacion` (T-181); `DatosFavor`, `TipoFavor` (T-182); `IHistoria.Etapa` (`Libre`),
  `IRelojMundo` (`Franja`, `Dia`), `IProgresoLigas.LigaActual`.
- Código modelo: `Combate/Habilidades/ReglaHabilidad.cs` (motivo: el primero que falla), `Combate/Editor/CreadorHabilidades.cs` (creador
  idempotente con tabla), `Tests/EditMode/Mecha/BalancePiezasPruebas.cs` o la de T-179 (reglas de balance leídas de los assets).
- Premios de liga (T-158, `DatosLigas_Base`): rival 1 de la liga 1 paga $600, de la liga 2 $1,000. Chatarra: `DatosObjeto_Chatarra`, venta $50.

## Qué hacer
1. Ensamblado: `MechaCyberpunk.Progresion` gana la referencia a `MechaCyberpunk.Economia` (para la chatarra y, en T-187, el descuento; Economia
   no referencia Progresion, así que no hay ciclo; D-tony-150). ReglasEnsambladosPruebas debe seguir en verde.
2. `Progresion/Misiones/`: `OrigenMision` (`Calle`, `Telefono`, `Bar`), `FranjaMision` (`Cualquiera`, `Dia`, `Noche`), `TipoPaso` (`IrA`,
   `Recoger`, `Entregar`, `Pelear`, `Escapar`), `PasoMision` (serializable: tipo, `idPunto`, `texto` que ve el jugador, `calor` que suma al
   cumplirse, `radio` para `IrA` en m) y `DatosMision` (ScriptableObject): `id`, `titulo` (mayúsculas), `descripcion` (una frase), `tipo`
   (`TipoMision`), `origen`, `franja`, `idEncargo` (el punto `Encargo` donde se ofrece; solo `Calle`), `pasos`, `pago` ($), `chatarra`
   (`DatosObjeto` + cantidad, al cumplirse), `deltaBarrio`, `deltaCorporaciones`, `favor` (`DatosFavor`, puede ser null), `favorRequerido`
   (`TipoFavor` o ninguno; se gasta al aceptar), `nivelBarrioMinimo`, `nivelCorporacionesMinimo`, `ligaMinima`, `limiteSegundos` (0 = sin
   límite), `repetible` (vuelve al día siguiente). Todo con `[Tooltip]` y unidades.
3. `Progresion/Misiones/ReglaMisiones.cs` (pura): `MotivoMision` (`Disponible`, `FueraDeLibre`, `OtraActiva`, `FranjaEquivocada`,
   `BarrioNoTeHabla` (barrio en −2: ninguna de barrio), `CorporacionesNoLlaman` (corporaciones en −2: ninguna corporativa), `ReputacionBaja`,
   `LigaBaja`, `FaltaFavor`, `HechaHoy`, `YaHecha`, `FaltanPuntos`; el primero que falla, en ese orden) con `Decidir(datos, contexto)`. El
   contexto trae etapa, franja (sin reloj: vale cualquier franja), día, liga, niveles de reputación, favores, si hay otra activa, el último día en
   que se cumplió y si existen todos sus puntos. `Recompensa` y `Penalizacion` (al fallar o abandonar: −3 a cada reputación que la misión subía).
4. `Progresion/Misiones/MaquinaMision.cs` (pura, con `dt`): `Empezar(datos)`, `PasoActual`, `IndicePaso`, `Cumplir()` (cumple el paso actual;
   al cumplir el último, la misión termina con éxito), `Fallar(motivo)` (`Tiempo`, `KO`, `Abandono`), `Avanzar(dt)` (cuenta el límite) y eventos
   `PasoCambio`, `Terminada(exito, motivo)`. No sabe nada de escena.
5. `Progresion/Editor/CreadorMisiones.cs` (idempotente, menú y batchmode; no pisa assets existentes salvo en un paso explícito) crea en
   `Progresion/Datos/Misiones/` las 11 misiones y un `CatalogoMisiones_Base` (lista en orden):
   | Id | Título | Tipo, origen, franja | Pasos (punto) | Pago | Barrio / Corp. | Favor y otros |
   | --- | --- | --- | --- | --- | --- | --- |
   | barrio_tiendita | LA TIENDITA DE DOÑA LUPE | Barrio, Calle (Mision_Encargo_DonaLupe), cualquiera | Recoger (Mision_Botin_Farmacia), Entregar (Mision_Encargo_DonaLupe) | $50 | +8 / 0 | DESCUENTO; repetible |
   | barrio_callejon | LOS DEL CALLEJÓN | Barrio, Calle (Mision_Encargo_Taquero), noche | Pelear (Mision_Pelea_Callejon), IrA (Mision_Encargo_Taquero) | $100 | +12 / 0 | ESCONDITE; repetible |
   | barrio_luz | LUZ PARA LA VECINDAD | Barrio, Calle (Mision_Encargo_Vecindad), cualquiera, barrio ≥ 1 | Recoger (Mision_Botin_Subestacion, calor 30), Entregar (Mision_Encargo_Vecindad) | $0 | +15 / −8 | INFORMACIÓN; repetible |
   | corp_entrega | ENTREGA URGENTE | Corporativa, Teléfono, día | Recoger (Mision_Botin_Paqueteria), Entregar (Mision_Destino_TorreCorp); límite 150 s | $400 | −5 / +8 | repetible |
   | corp_cobro | COBRO DE ADEUDO | Corporativa, Teléfono, día | Recoger (Mision_Destino_Moroso, "Cobra al vecino"), Entregar (Mision_Destino_TorreCorp) | $500 | −12 / +10 | repetible |
   | corp_limpieza | LIMPIEZA DE ZONA | Corporativa, Teléfono, día, corp ≥ 1, liga ≥ 2 | Pelear (Mision_Pelea_Baldio) | $900 | −15 / +15 | repetible |
   | ilegal_carga | CARGA CALIENTE | Ilegal, Bar, noche | Entregar (Mision_Destino_Muelle, calor 40), Escapar | $350 | 0 / −5 | el paquete se recibe al aceptar; repetible |
   | ilegal_pelea | PELEA SIN JAULA | Ilegal, Bar, noche | Pelear (Mision_Pelea_Baldio), Escapar | $450 | +3 / −5 | repetible |
   | robo_bodega | BODEGA DE REFACCIONES | Robo, Bar, noche | Recoger (Mision_Botin_BodegaCorp, calor 50), Escapar | $0 | +3 / −10 | 6 chatarra; repetible |
   | robo_camion | CAMIÓN BLINDADO | Robo, Bar, noche, liga ≥ 2 | Pelear (Mision_Pelea_Camion), Recoger (Mision_Botin_Camion, calor 60), Escapar | $200 | +5 / −15 | 10 chatarra; repetible |
   | robo_soplo | EL SOPLO | Robo, Bar, noche, requiere INFORMACIÓN | Recoger (Mision_Botin_BodegaCorp, calor 15), Escapar | $0 | +3 / −8 | 12 chatarra; repetible |
   Puntos de escondite que usará T-187: Mision_Escondite_Azotea y Mision_Escondite_Lavanderia. La lista de todos los ids (con su tipo) se
   imprime con un método del creador y va a docs/revision-tony.md ("Para Alan", opcional).
6. Pruebas EditMode (`Tests/EditMode/Progresion/`; la asmdef de pruebas gana Economia si hace falta):
   - `ReglaMisionesPruebas`: con todo en orden da `Disponible`; cada motivo sale en el orden del paso 3; sin reloj vale cualquier franja; una
     repetible hecha hoy da `HechaHoy` y al día siguiente `Disponible`; penalización de −3 solo a las reputaciones que subían.
   - `MaquinaMisionPruebas`: tres pasos se cumplen en orden; `Cumplir` después de terminar no hace nada; el límite de 150 s falla en 150 s
     (no antes); abandonar avisa `Terminada(false, Abandono)` una sola vez.
   - `MisionesAssetsPruebas`: las 11 con los valores de la tabla, ids únicos, cada `idPunto` con el prefijo `Mision_` y el tipo de punto que su
     paso pide; `BalanceMisionesPruebas` (D-tony-149), leyendo los assets: a) toda misión de barrio paga menos que el rival 1 de la liga 1; b)
     ninguna misión paga (efectivo + chatarra a precio de venta) más que el rival 1 de su liga mínima; c) ninguna sube las dos reputaciones; d)
     toda misión de barrio sube el barrio y da un favor; e) toda corporativa baja el barrio al menos 5 y sube corporaciones; f) los robos pagan más
     en chatarra que en efectivo; g) toda ilegal y todo robo suma al menos 25 de calor (el nivel 1) o lleva un `Escapar`.

## Carpetas permitidas
Assets/_Proyecto/Progresion/Misiones/ (nueva), Assets/_Proyecto/Progresion/Datos/Misiones/ (nueva, solo con el creador),
Assets/_Proyecto/Progresion/Editor/CreadorMisiones.cs (nuevo), Assets/_Proyecto/Progresion/MechaCyberpunk.Progresion.asmdef (solo la referencia a
Economia), Assets/_Proyecto/Progresion/Editor/MechaCyberpunk.Progresion.Editor.asmdef (si necesita Economia), Assets/_Proyecto/Tests/EditMode/Progresion/
(y su asmdef), docs/revision-tony.md (lista de ids, "Para Alan")
No tocar: Core/, Economia/ (solo se lee), UI/, Pelea/, prefabs, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena)
- [ ] ReglaMisionesPruebas, MaquinaMisionPruebas, MisionesAssetsPruebas y BalanceMisionesPruebas pasan, con los valores de la tabla
- [ ] Correr `CreadorMisiones` dos veces no cambia ningún asset (md5 igual); ReglasEnsambladosPruebas en verde con la referencia nueva
- [ ] La bitácora imprime la tabla final (pago, chatarra en $, reputaciones, calor) y cada regla de balance con su margen

## Criterios de sensación (para un humano)
- Tony: leer la tabla (D-tony-148). ¿Las misiones de barrio se sienten de barrio (Doña Lupe, el taquero, la vecindad)? ¿Te late que las
  corporativas paguen bien y cuesten barrio, y que los robos paguen en chatarra que hay que ir a vender?

## Pruebas automáticas
- EditMode: ReglaMisionesPruebas, MaquinaMisionPruebas, MisionesAssetsPruebas, BalanceMisionesPruebas.

## Fuera de alcance
- El servicio en escena y los pasos IrA, Recoger y Entregar (T-184); Pelear y Escapar (T-185); dónde se ofrecen (T-186); usar favores (T-187).
  Hackeo (fuera del bloque).
