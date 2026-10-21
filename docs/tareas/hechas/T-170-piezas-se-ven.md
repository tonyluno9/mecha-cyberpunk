# T-170 Las piezas se ven al subirte al mecha
Dueño: Tony | Rama: tony/T-170-piezas-se-ven | Modelo: Sonnet 5.5, medium
Depende de: T-167

## Objetivo
Lo que compraste se nota al subirte: desde la cabina, los brazos tienen el color de su rama y una franja por nivel, el marco de la cabina
toma el color de la pieza de cabina, y durante el encendido una pantalla de la cabina lista las 5 piezas. Desde fuera, el cuerpo del mecha
estacionado toma el color del torso. Es greybox: el arte final de las piezas es otro trabajo.

## Contexto
- Diseño: mecha-taller.md ("Compras las mejoras en el taller y las ves al subirte al mecha"; pregunta abierta "Cómo se ve el mecha fregado
  y cómo se ve al máximo"), estetica-hud.md (neón azul, magenta, naranja y turquesa; en el mecha todo es diegético, nada flotando).
- Decisiones: D-tony-43 (alarma y cristal no se tocan), D-tony-122 (colores por rama, provisional): de serie gris; militar azul acero;
  tech turquesa; cuerpo a cuerpo naranja; fuego magenta. Nada en rojo puro (es la alarma y la furia del jefe).
- Código: `MechaJugador.prefab` (cabina: `BrazoIzquierdo`/`BrazoDerecho` con `Brazo`, `Antebrazo` y `Puno`; `PosteIzquierdo`,
  `PosteDerecho`, `VigaSuperior`; cuerpo exterior `Cuerpo`), materiales de `Mecha/Materiales/` (`Cabina_Brazo.mat`, `Cabina_Estructura.mat`),
  `UI/Cabina/PantallaKitsCabina.cs` (pantalla de cabina fuera del centro), `Mecha/Encendido/EncendidoMecha.cs`, `EquipoMecha.Cambio` (T-167).
- Deuda conocida: algunos `.mat` de la cabina cambiaban solos al correr pruebas. Aquí no se modifica ningún material en ejecución: se
  cambia `sharedMaterial` entre assets o se usa `MaterialPropertyBlock`.

## Qué hacer
1. `Mecha/Piezas/AspectoPiezas.cs` + `ConfigAspectoPiezas` (Mecha/Datos/, un material por rama y el de franja) con su creador: escucha
   `EquipoMecha.Cambio` (y aplica al empezar). Brazos de la cabina: material de la rama de su pieza y de 0 a 4 franjas encendidas en el
   antebrazo (hijos greybox nuevos creados por el creador). Marco de la cabina (postes y viga): color de la pieza de cabina. `Cuerpo`
   exterior: color del torso. Sin `EquipoMecha` (mecha `Directo`), no hace nada.
2. `UI/Cabina/PantallaPiezasCabina.cs` + config + creador: una pantalla chica de la cabina, fuera del 40 % × 40 % central de la vista, que
   lista las 5 piezas (nombre en el color de su rama) mientras dura el encendido y 5 s después; luego se apaga. No muestra dinero ni nada
   flotante.
3. Materiales nuevos en `Mecha/Materiales/` (uno por rama y el de franja), con el creador y sus `.meta`.
4. PlayMode `Tests/PlayMode/Mecha/AspectoPiezasPruebas.cs`: con brazo izquierdo de fuego II y brazo derecho militar IV, el `Brazo`
   izquierdo usa el material de fuego con 2 franjas y el derecho el militar con 4; con torso tech, `Cuerpo` usa el de tech; poner las de
   serie lo regresa todo al gris sin franjas. PlayMode en UI: al subir y encender (E, F, Espacio, Q simulados), la pantalla de piezas lista
   las 5 durante el encendido y se apaga 5 s después.

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Piezas/, Datos/, Materiales/, Editor/, Prefabs/ solo con su creador), Assets/_Proyecto/UI/ (Cabina/, Datos/, Editor/,
Prefabs/), Assets/_Proyecto/Tests/PlayMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/UI/, docs/capturas/
No tocar: Core/, Combate/ (el animador de brazos no cambia), la lógica de piezas y del taller, escenas, Assets/Arte/, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] AspectoPiezasPruebas y la de la pantalla de piezas de la cabina pasan; las pruebas de encendido, alarma y cristal siguen en verde
- [ ] Tras la suite, `git status` no muestra ningún `.mat` ni prefab modificado; el creador corrido dos veces no cambia nada (md5 igual)
- [ ] Capturas docs/capturas/T-170-1.png (cabina con brazos de dos ramas distintas y sus franjas, durante el encendido con la pantalla de
      piezas) y T-170-2.png (mecha estacionado desde fuera con torso de rama), revisadas a ojo: la pantalla no tapa el centro de la vista

## Criterios de sensación (para un humano)
- Pruebas_Economia: compra un brazo de fuego y otro militar y súbete. ¿Se nota la mejora al primer vistazo? ¿Los colores se leen como
  "ramas" distintas y no como daño o alarma? ¿Las franjas de nivel se ven sin buscarlas?
- ¿La lista de piezas durante el encendido ayuda o estorba los 5 segundos del encendido?

## Pruebas automáticas
- PlayMode: AspectoPiezasPruebas y la pantalla de piezas de la cabina.

## Fuera de alcance
- Modelos finales de las piezas (Assets/Arte/, Alan o quien diga la tarea de arte; ver D-tony-124). Piernas visibles. Cómo se ve el mecha
  fregado más allá de lo que ya hay.
