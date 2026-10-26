# T-174 Habilidades y munición como datos
Dueño: Tony | Rama: tony/T-174-datos-habilidades | Modelo: Sonnet 5.5, medium
Depende de: T-166 (piezas como datos, ya en main)

## Objetivo
Existen las 8 habilidades especiales como datos (una por rama en el brazo izquierdo y otra en el torso), las 4 municiones que se compran
en el mercado negro, y las piezas de rama saben qué habilidad traen y cuánto acortan los enfriamientos (tech). Una regla pura decide si
una habilidad se puede usar y calcula su potencia y su enfriamiento. Todavía no se lanza nada (T-175) ni se usa desde la cabina (T-176).

## Contexto
- Diseño: combate.md ("Las habilidades especiales, como misiles, tienen cooldown y munición limitada que compras"; "Te quedas sin munición
  en plena pelea: solo te quedan los brazos"; criterio "Un misil no se puede volver a usar hasta que pase su cooldown, y cuando se acaba la
  munición no dispara"), mecha-taller.md ("Tech/eléctrica: ... habilidades con cooldown más corto (escudos, descargas, drones de apoyo)";
  pregunta abierta "Qué arma o habilidad tiene cada pieza por rama y nivel").
- Decisiones: D-tony-116 (el "cooldown más corto" de tech llega en este bloque como dato de `DatosPieza`), D-tony-129 (ids de piezas),
  D-tony-109/D-tony-110 (los creadores no pisan assets existentes: los cambios a assets viejos van en un paso explícito). Propuestas nuevas:
  D-tony-134 (no hace falta Core), D-tony-135 (qué ranura lleva habilidad), D-tony-136 (tabla), D-tony-137 (munición), D-tony-138 (reglas de uso).
- Código: `Mecha/Piezas/DatosPieza.cs`, `ReglaEstadisticasPiezas.cs`, `Mecha/Editor/CreadorPiezas.cs`; `Economia/Inventario/DatosObjeto.cs`,
  `TipoObjeto.cs`, `Economia/Editor/CreadorInventario.cs`, `CreadorDatosEconomia.cs` (ya crea `DatosOferta_MunicionMisil`, precio de prueba
  $500, en `DatosTienda_MercadoNegro`); `Mecha/Kits/ReglaKitReparacion.cs` (modelo de una regla de "motivo").
- Ensamblados: Combate solo referencia Core; Mecha referencia Combate y Economia. Por eso la habilidad (Combate) no conoce su munición
  (Economia): la pieza (Mecha) une las dos.

## Qué hacer
1. `Combate/Habilidades/TipoEfectoHabilidad.cs` (`Proyectil`, `Cono`, `Escudo`, `Embestida`; solo se agregan valores al final) y
   `Combate/Habilidades/DatosHabilidad.cs` (ScriptableObject): `id`, `nombre` (mayúsculas, "MISIL"), `efecto`, `danio` (puntos, nivel I),
   `enfriamiento` (s), y los parámetros por efecto con `[Tooltip]` y unidades: proyectil (`velocidad` m/s, `alcance` m, `radioExplosion` m,
   `cantidad`, `dispersion` °), cono (`alcance` m, `angulo` °, `aturdir` s, `empuje` m, `rompeGuardia`), escudo (`reduccion` 0 a 1,
   `duracion` s), embestida (`distancia` m, `duracionEmbestida` s, `empuje` m), y `colorRama` (para los efectos y las pantallas, D-tony-122).
2. `Combate/Habilidades/ReglaHabilidad.cs` (pura): `MotivoHabilidad` (`Puede`, `SinHabilidad`, `FueraDePelea`, `SinControl`, `Aturdido`,
   `GuardiaArriba`, `Enfriando`, `SinMunicion`; el primero que falla, en ese orden) con `Decidir(...)`; `Potencia(nivel)` = 1 + 0.25 × (nivel − 1)
   (I 1.0, II 1.25, III 1.5, IV 1.75; multiplica el daño y la duración del escudo); `Enfriamiento(baseSegundos, reduccion)` con la reducción
   limitada a [0, 0.4]; `Avanzar(restante, dt)` sin bajar de 0.
3. `DatosPieza` agrega `habilidad` (DatosHabilidad, null en las de serie), `municion` (DatosObjeto, null si no hay habilidad) y
   `bonoEnfriamiento` (fracción; solo tech: 0.03 × nivel). `ReglaEstadisticasPiezas.ReduccionEnfriamiento(puestas)` suma los bonos (tope 0.4).
   No cambies los campos que ya existen.
4. `TipoObjeto.Municion` al final. Creador de Economia: 4 municiones en `Economia/Datos/` (`DatosObjeto_Municion<Nombre>` con su
   `DatosOferta_Municion<Nombre>`, tipo `TipoOferta.Municion`), en `CatalogoObjetos_Base` y en `DatosTienda_MercadoNegro`:
   | Munición | Rama | Precio | Máximo |
   | --- | --- | --- | --- |
   | MISILES (`DatosOferta_MunicionMisil`, ya existe: ajustar a $80 en un paso explícito) | Militar | $80 | 6 |
   | COMBUSTIBLE | Fuego | $60 | 6 |
   | CELDAS | Tech | $40 | 8 |
   | CARGAS | Cuerpo a cuerpo | $50 | 6 |
   La munición no se vende (`valorVenta` 0 y el mercado negro no la compra). Actualiza `DatosEconomiaAssetsPruebas` (esperaba $500).
5. `Combate/Editor/CreadorHabilidades.cs` (idempotente) crea en `Combate/Datos/Habilidades/` las 8 de la tabla (nivel I; provisional, D-tony-136):
   | Rama | Brazo izq. (tecla 1) | Torso (tecla 2) |
   | --- | --- | --- |
   | Militar | MISIL: proyectil 20 m/s, alcance 25 m, daño 20, explosión 1.2 m, enfriamiento 8 s | SALVA: 3 proyectiles, dispersión 6°, daño 8 c/u, explosión 0.8 m, 10 s |
   | Fuego | LANZALLAMAS: cono 4 m, 60°, daño 14, 6 s | ONDA DE CALOR: cono 3 m, 360°, daño 15, empuje 1.5 m, 9 s |
   | Tech | DESCARGA: cono 3.5 m, 30°, daño 6, aturde 1.0 s, 5 s | ESCUDO: −50 % de daño recibido, 3 s, 8 s |
   | Cuerpo a cuerpo | MARTILLO: cono 2.8 m, 40°, daño 16, rompe guardia, 6 s | EMBESTIDA: 5 m en 0.3 s, daño 14, empuje 2 m, 7 s |
6. `CreadorPiezas` gana un paso explícito "ajustar habilidades" (menú y método de batchmode) que pone `habilidad` y `municion` a las 32 piezas
   de brazo izquierdo y torso de rama (la de su rama y su ranura) y `bonoEnfriamiento` a las 20 tech; las de serie, brazo derecho, piernas y
   cabina quedan sin habilidad. Correrlo dos veces no cambia nada.
7. EditMode: `Tests/EditMode/Combate/ReglaHabilidadPruebas.cs` (orden de motivos, potencia, tope de reducción, enfriamiento que no baja de 0),
   `Tests/EditMode/Combate/HabilidadesAssetsPruebas.cs` (las 8 con los valores de la tabla y su efecto) y casos nuevos en
   `PiezasAssetsPruebas` (Mecha) y `DatosEconomiaAssetsPruebas` (Economia).

## Carpetas permitidas
Assets/_Proyecto/Combate/Habilidades/ (nueva), Assets/_Proyecto/Combate/Datos/Habilidades/ (nueva), Assets/_Proyecto/Combate/Editor/,
Assets/_Proyecto/Mecha/Piezas/, Assets/_Proyecto/Mecha/Datos/Piezas/ (solo con el creador), Assets/_Proyecto/Mecha/Editor/CreadorPiezas.cs,
Assets/_Proyecto/Economia/Inventario/TipoObjeto.cs, Assets/_Proyecto/Economia/Editor/, Assets/_Proyecto/Economia/Datos/ (solo con los creadores),
Assets/_Proyecto/Tests/EditMode/Combate/, Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/Tests/EditMode/Economia/
No tocar: Core/, prefabs, escenas, IA/, UI/, Pelea/, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ReglaHabilidadPruebas: con todo en orden da `Puede`; cada motivo sale en el orden del paso 2 cuando falla; potencia de nivel III = 1.5;
      enfriamiento de 8 s con 5 piezas tech IV (reducción pedida 0.6) = 4.8 s (tope 0.4); `Avanzar(0.1, 0.5)` = 0
- [ ] HabilidadesAssetsPruebas: 8 habilidades con id único y los valores de la tabla; PiezasAssetsPruebas: 32 piezas con habilidad (las 16
      de brazo izquierdo y las 16 de torso de rama), cada una con la munición de su rama; las 20 tech con `bonoEnfriamiento` = 0.03 × nivel; las
      demás sin habilidad ni bono
- [ ] Las 4 municiones están en `CatalogoObjetos_Base` y en `DatosTienda_MercadoNegro` con el precio y el máximo de la tabla
- [ ] Correr `CreadorHabilidades`, `CreadorPiezas` (con su paso de ajuste), `CreadorInventario` y `CreadorDatosEconomia` dos veces, en cualquier
      orden, no cambia ningún asset (md5 igual); las pruebas de piezas, inventario, venta y tienda que ya existían siguen en verde

## Criterios de sensación (para un humano)
- Tony: leer la tabla de habilidades y munición (D-tony-136 y D-tony-137). ¿Cada rama tiene una habilidad que se le parece? ¿Te late que el
  brazo izquierdo sea el arma y el torso el sistema?

## Pruebas automáticas
- EditMode: ReglaHabilidadPruebas, HabilidadesAssetsPruebas, PiezasAssetsPruebas, DatosEconomiaAssetsPruebas.

## Fuera de alcance
- Lanzar los efectos (T-175), usarlas desde la cabina (T-176), pantallas (T-177), rivales con habilidad (T-178), balance (T-179).
  Drones de apoyo (mecha-taller.md los nombra para tech; no entran en este bloque, D-tony-142).
