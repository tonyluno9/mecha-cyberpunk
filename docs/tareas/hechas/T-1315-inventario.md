# T-1315 Inventario: equipar en la rueda, implantes, tecla de consumir, expendedoras

Punto 15 de docs/playtest-17nov.md. Rama tony/T-1315-inventario. Solo carpetas de UI/ y sus pruebas.

## Qué se hizo
- **Rueda equipable** (`UI/Rueda/EquipoRueda.cs`, lógica pura): por omisión todo lo que traes va en la rueda; lo que quitas en el inventario queda
  como "quitado" y `RuedaArmasObjetos` ya no lo muestra (`EstaEquipada`). Los puños siempre salen. Tope = `ReglaRuedaArmas.Casillas`
  (las casillas que ya tiene la rueda). Dura la sesión: no se guarda con la partida (el inventario no usa IGuardable para esto; anotado).
- **Inventario (I)**: las armas de fuego y los consumibles llevan la marca "RUEDA" en su casilla y un botón abajo del detalle
  (PONER EN LA RUEDA / QUITAR DE LA RUEDA / LA RUEDA ESTÁ LLENA). Se cambia con clic (cursor libre), Enter o F. El encabezado de la cuadrícula
  dice "1-16 DE n" cuando hay más casillas de las que caben (16 visibles, se baja con las flechas).
- **Implantes**: la categoría ya existía; ahora cada implante lleva su descripción (la frase de `DatosImplante`, o lo que hace su espacio) y el
  botón dice SOLO SE CONSULTA. No se tocó la barra de prótesis ni el combate.
- **Tecla de consumir**: no existía (desde T-991 los objetos solo se usaban desde la rueda). Se agregó la acción `ConsumirEquipado`
  (`UI/HudAPie/AccionConsumirEquipado.cs`, tecla B, la acción del Input System es la que da el nombre de la tecla). `HudConsumibles` muestra
  "[B] BEBIDA ENERGÉTICA x2" arriba de las casillas de objetos y B consume el primer objeto equipado que sirve ahora. El aviso del tutorial de
  Consumibles dice "... · B USA EL OBJETO EQUIPADO" con la tecla real (`ReglaTutorial.TeclaConsumir`, `TutorialLigero.TeclaReal`).
- **Expendedoras**: ver abajo.

## Por qué fallaban las expendedoras
Ciudad_Prologo solo tenía 3 `Expendedora` vivas (las que pone `ColocadorExpendedoras` desde DatosExpendedoras_Ciudad). Las demás máquinas
(`CY06_Expendedora_T503` del tianguis, las de las fachadas y los kits de Mundo: 20 en la escena) eran solo el modelo, sin componente ni tienda:
al apuntarles no pasaba nada. Ahora el colocador (`BuscarDecoradas`) agrega `Expendedora` (con la misma tienda y pantalla) a todo objeto cuyo nombre
empiece con `CY06_Expendedora` y que no tenga ya una, y un collider si le faltara. No se edita ninguna escena (son de Alan). Resultado medido:
23 expendedoras vivas, todas con tienda, oferta no vacía, `PuedeUsar` y collider.

## Decisiones
- La tecla B está en un `InputAction` compartido de UI (no en ControlesAPie.inputactions, que es de Personaje): se puede recambiar con override.
- Lo equipado no se guarda con la partida (solo la sesión). Si se quiere guardar, hace falta un IGuardable en Progresion/.
- Sin cambios en Personaje/.

## Pruebas
- EditMode `EquipoRuedaT1315Pruebas` (7) + ReglaRuedaArmas, ReglaInventarioUI y TutorialTeclasRealesT718: 35/35.
- PlayMode `InventarioRuedaT1315Pruebas` (4): I abre, Enter quita el fusil y la rueda (Tab) ya no lo muestra, F lo vuelve a poner; implante con su
  descripción; HUD con "[B] ..." y B consume; las 23 expendedoras de la ciudad dejan comprar.
- Capturas en docs/capturas/t-1315/ (inventario-armas, inventario-implantes, hud-consumir).

## Resultados finales
- EditMode completo: 2636 total, 2636 pasaron, 0 fallaron.
- PlayMode `InventarioRuedaT1315Pruebas` + `InventarioPruebas` (T-904): 6/6. Solas, RuedaArmasT991 pasa (excepto lo que ya fallaba).
- En un solo proceso con HudAPiePruebas y HudH10Pruebas por delante, fallan lo mismo con y sin este cambio (medido en la base): HudAPie Arma_ConEntradaSimulada,
  HudH10 (Arma_Balas, Estrellas, Objetos_TeclaReal), RecorridoNombres y TiendaCic; esa contaminación también hace fallar a las pruebas que corren
  después (RuedaArmasT991 y las de T-1315) aunque pasan solas.
- Revisión (Opus): RuedaArmasT991 + InventarioRuedaT1315 juntas 12/12 (+1 ignorada); ExpendedorasMarcadoresT719 sola 1/1. Si en el mismo proceso corren antes InventarioPruebas o ExpendedorasMarcadoresT719, fallan 6 de RuedaArmasT991 también en origin/main (contaminación de entrada entre clases, ya existía): correr en tandas.
