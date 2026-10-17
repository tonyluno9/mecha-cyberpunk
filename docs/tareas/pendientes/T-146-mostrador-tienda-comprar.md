# T-146 Mostrador de tienda: comprar en el mercado negro
Dueño: Tony | Rama: tony/T-146-mostrador-tienda | Modelo: Sonnet 5.5, medium
Depende de: T-142, T-143

## Objetivo
Existe un mostrador de tienda (prefab de Tony que Alan coloca en su mercado negro, D-029): con E se abre una pantalla con lo que vende el
lugar y su precio, y con Enter se compra; lo comprado entra al inventario. En este bloque el mercado negro vende kits de reparación. La
pantalla ya trae las pestañas COMPRAR y VENDER; vender es T-147.

## Contexto
- Diseño: mecha-taller.md ("kits de reparación limitados del mercado negro"), economia-misiones.md (venta en el mercado negro), ciudad.md.
- Contratos: `ITienda`, `DatosTienda`, `DatosOferta`, `ResultadoCompra`, `ILugar` (Core). `TiendaBasica` (Economia) ya implementa `ITienda`.
- Decisiones: D-036 (IdLugar), D-039 (comprar solo si alcanza: `IntentarPagar`), D-tony-49 (interactuables de Tony). Propuestas nuevas:
  D-tony-81 (los mostradores son prefabs de Tony; abierto según el `ILugar` del mismo `IdLugar`) y D-tony-86 (precio del kit).
- Horario propuesto por Alan para el mercado negro: 09:00 a 18:00 (D-alan-5), cerrado en la demo con su cierre especial; el mostrador solo
  lee `ILugar.EstaAbierto`, no decide horarios.
- Ejemplos: `Progresion/Prologo/MostradorPeleas.cs` (un `IInteractuable` con textos por estado), `UI/Historia/PantallaEspejo.cs` (pantalla que
  congela al jugador), la pantalla de apuesta de T-145. Usa el helper de mapas de acciones de T-140.

## Qué hacer
1. `UI/Tiendas/MostradorTienda.cs` (`IInteractuable`, con un `DatosTienda` y el `CatalogoObjetos`): crea su `TiendaBasica`, la registra en
   `Registro<ITienda>` mientras está activo. Texto y permiso, en orden: jugador no a pie → no se ofrece; hay un `ILugar` registrado con el
   `IdLugar` de la tienda y está cerrado → "CERRADO · abre HH:MM" (de su horario) y no se usa; si no → "E · <nombre de la tienda>". Sin `ILugar`
   de ese id (escenas de pruebas), abierto.
2. `UI/Tiendas/PantallaTienda.cs` + prefab (creador en UI/Editor/): pestañas COMPRAR y VENDER (← → cambia de pestaña; VENDER dice "PRONTO"
   hasta T-147), lista con ↑ ↓, Enter compra, Esc cierra; arriba el dinero actual. COMPRAR lista solo las ofertas de la tienda que tienen un
   `DatosObjeto` en el catálogo con esa `oferta` (en este bloque, solo el kit; las demás llegan con su bloque) con nombre, precio y "LLEVAS n/máx".
   Al comprar: si el inventario está lleno, "LLEVAS EL MÁXIMO" y no se cobra; si no alcanza, "NO TE ALCANZA"; si sí, `ITienda.Comprar` y el
   objeto entra al inventario. Congela al jugador mientras está abierta. Textos en un `ConfigPantallaTienda`.
3. `UI/Prefabs/MostradorMercadoNegro.prefab` (con creador): mostrador greybox (mesa y letrero) con `MostradorTienda` y
   `DatosTienda_MercadoNegro`. En `CreadorEscenaEconomia` (T-143): colocarlo en "Sitio_Mercado".
4. Precio del kit en `DatosOferta_KitReparacion` según D-tony-86 ($250), con el creador de datos.
5. PlayMode `Tests/PlayMode/UI/MostradorTiendaPruebas.cs` en Pruebas_Economia, con entrada simulada.

## Carpetas permitidas
Assets/_Proyecto/UI/ (Tiendas/, Datos/, Editor/, Prefabs/), Assets/_Proyecto/Economia/Datos/ y Economia/Editor/ (solo el precio del kit),
Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaEconomia.cs, Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity (solo con su
creador), Assets/_Proyecto/Tests/PlayMode/UI/, docs/capturas/
No tocar: Core/, Mecha/, Pelea/, otras escenas, todo lo de Alan (Mundo/, Casino/).

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] MostradorTiendaPruebas a): con $1,000, W hasta el mostrador, el HUD dice "E · Mercado negro"; E abre la pantalla con "KIT DE REPARACIÓN"
      a $250 y "LLEVAS 0/3"; Enter tres veces: dinero $250 y 3 kits; un cuarto Enter dice "LLEVAS EL MÁXIMO" y el dinero no cambia
- [ ] b) con $100, Enter dice "NO TE ALCANZA" y nada cambia; con dinero negativo, igual
- [ ] c) Esc cierra; con W el jugador vuelve a moverse; la E que abrió no reabre la pantalla
- [ ] d) con un `LugarDePrueba` `MercadoNegro` cerrado registrado, el aviso dice "CERRADO · abre 09:00" (o la hora de su horario) y E no abre nada
- [ ] e) mientras el mostrador está activo hay un `ITienda` en el registro y al destruirlo se quita; la oferta de munición y la del brazo no
      aparecen en la lista
- [ ] Captura docs/capturas/T-146-1.png: la pantalla de la tienda con un kit comprado

## Criterios de sensación (para un humano)
- Pruebas_Economia, Play: compra kits. ¿La pantalla se lee de un vistazo y se siente mercado negro (neón, sucio) y no menú de opciones?
- ¿$250 por kit se siente caro pero tentador?

## Pruebas automáticas
- PlayMode: MostradorTiendaPruebas.

## Fuera de alcance
- Vender (T-147), usar el kit (T-148). El edificio, la clínica y el horario del mercado negro (Alan, A-F5). Tiendas del bar.
