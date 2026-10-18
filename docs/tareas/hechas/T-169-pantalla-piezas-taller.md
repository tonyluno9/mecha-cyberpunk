# T-169 Pantalla de piezas del taller
Dueño: Tony | Rama: tony/T-169-pantalla-piezas | Modelo: Sonnet 5.5, medium
Depende de: T-168

## Objetivo
Desde el banco del taller se abre "PIEZAS DEL MECHA": se ven las 5 ranuras con la pieza puesta (rama y nivel), se elige una ranura y se
ve qué se puede poner o comprar, cuánto cuesta y cómo cambia el mecha (vida, daño y velocidad), y con Enter se compra o se pone. Es la
parte jugable del bloque: el jugador arma su mecha mezclando ramas.

## Contexto
- Diseño: mecha-taller.md (5 piezas, 4 ramas mezclables, 4 niveles por liga), estetica-hud.md (neón azul, magenta, naranja y turquesa;
  en el mecha todo es diegético: esta pantalla es del taller, a pie).
- Decisiones: D-tony-90 (la pantalla del banco va anidada en `MechaJugador.prefab` y pide sus opciones por evento; el control vuelve un
  cuadro después de cerrar), D-tony-99 (↑ ↓ sin dar la vuelta, Enter, Esc). Propuestas nuevas: D-tony-119 y D-tony-122 (colores por rama).
- Código: `UI/Taller/PantallaBancoTaller.cs` y su creador (`UI/Editor/CreadorPantallaBancoTaller.cs`), `UI/Peleas/PantallaCartelera.cs`
  (lista con filas en gris y en naranja), `UI/Tiendas/PantallaTienda.cs`, `Mecha/Taller/TallerPiezas.cs` y `BancoTaller.PiezasPedidas`
  (T-168). Helper de mapas de acciones de T-140; `MedidorFrames`.

## Qué hacer
1. `UI/Taller/PantallaPiezasTaller.cs` + `ConfigPantallaPiezas` (UI/Datos/) + prefab con creador, anidado en `MechaJugador.prefab` como la
   del banco (lo hereda `MechaEstacionado`). Escucha `BancoTaller.PiezasPedidas` (`GetComponentInParent`) y congela al jugador mientras está abierta.
2. Dos niveles:
   - Ranuras: "PIEZAS DEL MECHA", el dinero arriba, las 5 ranuras con su pieza puesta (nombre en el color de su rama, "DE SERIE" en gris) y
     abajo VIDA, DAÑO y VELOCIDAD del mecha armado. ↑ ↓ eligen, Enter o → entran, Esc cierra.
   - Piezas de la ranura: las opciones de `TallerPiezas.Opciones(ranura)` en su orden: "PONER · TIENES n", "COMPRAR · $800", y en gris "SE
     ABRE EN LA LIGA 3"; junto a la elegida, la diferencia contra lo puesto ("VIDA +28 · DAÑO −1 · VEL +0.25", verde lo que sube y magenta lo
     que baja). Enter elige; si no se puede, dice por qué ("NO TE ALCANZA", "TALLER BLOQUEADO") y no cambia nada; si se pudo, vuelve a las
     ranuras con la pieza nueva y un aviso corto ("PUESTA: PIERNAS CUERPO A CUERPO I"; si la vieja no cupo, "+$100 · VENDISTE ..."). ← o Esc
     regresan a las ranuras.
3. Textos, colores por rama y tamaños en `ConfigPantallaPiezas_Base`.
4. En `CreadorEscenaEconomia` no hace falta nada si la pantalla viene en el prefab del mecha; comprueba que Pruebas_Economia la trae.
5. PlayMode `Tests/PlayMode/UI/PantallaPiezasPruebas.cs` en Pruebas_Economia, con entrada simulada (W hasta el banco, E, ↑ ↓, Enter, →, ←, Esc).

## Carpetas permitidas
Assets/_Proyecto/UI/ (Taller/, Datos/, Editor/, Prefabs/), Assets/_Proyecto/Mecha/Prefabs/ (solo con el creador, para anidar la pantalla),
Assets/_Proyecto/Mecha/Editor/ (solo si el anidado lo pide), Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaEconomia.cs (solo si hace
falta), Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity (solo con su creador), Assets/_Proyecto/Tests/PlayMode/UI/, docs/capturas/
No tocar: Core/, la lógica de Mecha/Taller/ (es de T-168; si falta algo, anótalo y pide la corrección), otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] PantallaPiezasPruebas a): con el mecha sin daño y $1,000 en liga 1, W hasta el banco, el HUD dice "E · Taller · piezas"; E abre
      "PIEZAS DEL MECHA" con las 5 ranuras "DE SERIE" y VIDA 100 · DAÑO 10 · VELOCIDAD 4.0
- [ ] b) ↓ ↓ ↓ hasta PIERNAS, Enter: la lista tiene las de nivel I de las 4 ramas a $500 y las de nivel II en gris "SE ABRE EN LA LIGA 2";
      sobre cuerpo a cuerpo I dice "VIDA +10 · VEL +0.25"; Enter: dinero $500, la ranura dice "PIERNAS CUERPO A CUERPO I" y VELOCIDAD 4.25
- [ ] c) con el dinero bajado a $300 por código, elegir otra de $500 dice "NO TE ALCANZA" y no cambia nada; Enter sobre una gris no hace nada
- [ ] d) en una ranura con la de serie en el inventario, "PONER · TIENES 1" la pone sin cobrar
- [ ] e) Esc desde las piezas regresa a las ranuras; Esc otra vez cierra; W mueve al jugador; la E que cerró no reabre la pantalla
- [ ] f) con renta vencida sin dinero, el banco dice lo de hoy y la pantalla no se abre
- [ ] g) consola sin errores; `MedidorFrames` sin tirones fuera de su tolerancia al abrir y cerrar
- [ ] Capturas docs/capturas/T-169-1.png (ranuras con dos ramas mezcladas) y T-169-2.png (piezas de una ranura con una bloqueada y la
      diferencia de estadísticas), revisadas a ojo: se lee a 1280×720 sin solaparse

## Criterios de sensación (para un humano)
- Pruebas_Economia, Play: arma un mecha con brazos de ramas distintas. ¿Se entiende de un vistazo qué cambia cada pieza y cuánto cuesta?
- ¿Ver las piezas de la liga siguiente en gris te da ganas de subir de liga, o estorba?
- ¿$500 por la primera pieza se siente alcanzable después de una o dos peleas?

## Pruebas automáticas
- PlayMode: PantallaPiezasPruebas.

## Fuera de alcance
- Cómo se ven las piezas en el mecha (T-170). Vista 3D girable del mecha en el taller. Ratón en la pantalla.
