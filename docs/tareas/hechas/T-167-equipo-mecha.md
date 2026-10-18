# T-167 Equipo del mecha: las piezas puestas cambian al mecha y se guardan
Dueño: Tony | Rama: tony/T-167-equipo-mecha | Modelo: Sonnet 5.5, medium
Depende de: T-164, T-165, T-166

## Objetivo
El mecha del jugador lleva puestas 5 piezas (al empezar, las de serie) y sus estadísticas son la suma de esas piezas: poner unas piernas
de cuerpo a cuerpo lo hace correr más y un brazo militar lo hace pegar más fuerte, en la calle y en la arena. Lo puesto se guarda con el
mecha y vuelve al cargar, con el mismo daño. Todavía no hay tienda ni pantalla: se pone por código (T-168 y T-169 lo llevan al taller).

## Contexto
- Diseño: mecha-taller.md (5 piezas; ramas mezclables; "Compras las mejoras en el taller y las ves al subirte al mecha").
- Decisiones: D-tony-42 y D-tony-59 (daño persistente solo en el mecha de la historia, `MechaPersistente` en `MechaEstacionado.prefab`),
  D-tony-50 (guardado por piezas). Propuestas nuevas: D-tony-118 (las piezas puestas se guardan DENTRO de la clave `mecha`, para que la
  vida máxima y el daño se carguen juntos sin depender del orden de registro) y D-tony-119 (al cambiar una pieza se conservan los puntos de daño).
- Código: `Mecha/Estado/MechaPersistente.cs` (clave `mecha`, `DatosGuardados` con vida y vida máxima; `ReglaVidaPersistente.AlCargar`
  escala por fracción), `Mecha/Editor/CreadorMechaPersistente.cs` (pone `MechaPersistente` solo en la variante `MechaEstacionado.prefab`),
  `VidaMecha.CambiarDatos` (T-165), `ReglaEstadisticasPiezas` y `CatalogoPiezas_Base` (T-166), `DatosMecha.CrearEnEjecucion` (T-163).

## Qué hacer
1. `Mecha/Piezas/EquipoMecha.cs` (en el mismo objeto que `MechaPersistente`; lo pone su creador solo en `MechaEstacionado.prefab`, así el
   mecha `Directo` de Pruebas_Tony y los rivales no cambian): con `CatalogoPiezas_Base`; guarda los datos de serie de su `VidaMecha` al
   despertar; `Puesta(ranura)`, `Puestas` (las 5), `Valores` (los compuestos), `Equipar(DatosPieza)` (devuelve la pieza que se quitó, o null
   si no cambió nada: misma pieza o null), `PonerDeSerie()` y `event Action Cambio`. Al cambiar: compone con `ReglaEstadisticasPiezas`, crea
   un `DatosMecha` con `CrearEnEjecucion`, lo entrega con `VidaMecha.CambiarDatos` (conserva el daño en puntos) y destruye el `DatosMecha`
   de ejecución anterior (también en `OnDestroy`). No toca el inventario ni el dinero (eso es T-168).
2. `MechaPersistente`: su `DatosGuardados` agrega los ids de las 5 piezas puestas y el daño en puntos. Al cargar, primero pone las piezas
   (un id que no existe en el catálogo deja la de serie con warning) y luego fija la vida: misma vida máxima y mismo daño que al guardar.
   Una partida vieja sin piezas carga con las de serie y la vida como hoy (por fracción), sin warnings.
3. `MechaCyberpunk.Mecha` sigue sin referenciar Progresion; nada fuera de Mecha cambia.
4. PlayMode `Tests/PlayMode/Mecha/EquipoMechaPruebas.cs` con `MechaEstacionado.prefab` montado (como las pruebas del encendido) y un
   `SacoDePrueba`, con entrada simulada:
   a) al empezar, las 5 puestas son las de serie y `VidaMecha.Datos` da los valores de `DatosMecha_Jugador`;
   b) con piernas de cuerpo a cuerpo II, W sostenida 3 s: velocidad medida 4.5 m/s (±0.05); con las de serie otra vez, 4.0;
   c) con brazo derecho militar I, un clic izquierdo al saco le quita 13 (10 + 3);
   d) con 40 de daño, torso cuerpo a cuerpo I: vida 90/130; quitarlo (poner el de serie): 60/100;
   e) guardar (ruta forzada) con brazo izquierdo de fuego II, piernas tech III y 25 de daño; en un mecha nuevo, cargar: mismas 5 piezas,
      misma vida máxima y 25 de daño; igual si el `ServicioGuardado` aparece antes o después del mecha;
   f) un texto guardado de antes de este bloque (sin piezas) carga con las de serie y la vida por fracción, sin warnings;
   g) 20 cambios de pieza seguidos no dejan `DatosMecha` de ejecución vivos de más (cuenta con `Resources.FindObjectsOfTypeAll`).

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Piezas/, Estado/, Editor/, Prefabs/ solo con su creador), Assets/_Proyecto/Tests/PlayMode/Mecha/,
Assets/_Proyecto/Tests/EditMode/Mecha/
No tocar: Core/, Combate/ (lo de T-165 ya está), Economia/, UI/, Progresion/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] EquipoMechaPruebas a) a g) pasan; las pruebas de daño persistente, banco, kits, prólogo y ligas que ya existían siguen en verde
      (`AREAS='Mecha Progresion UI Economia' bash herramientas/verificar.sh playmode-por-area`)
- [ ] `MechaJugador.prefab` (base) NO lleva `EquipoMecha`; `MechaEstacionado.prefab` lleva uno solo; el creador corrido dos veces no
      cambia los prefabs (md5 igual)
- [ ] Tras la suite, `git status` no muestra assets de piezas, `DatosMecha_*` ni materiales modificados

## Criterios de sensación (para un humano)
- Ninguno todavía (las piezas se compran y se ven en T-168 a T-170).

## Pruebas automáticas
- PlayMode: EquipoMechaPruebas.

## Fuera de alcance
- Comprar, equipar desde el inventario y bloqueo por renta (T-168), pantalla (T-169), cómo se ven (T-170). Cambiar piezas en plena pelea.
