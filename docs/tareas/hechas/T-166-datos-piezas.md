# T-166 Piezas del mecha: ranuras, ramas, niveles y suma de estadísticas
Dueño: Tony | Rama: tony/T-166-datos-piezas | Modelo: Sonnet 5.5, medium
Depende de: T-163

## Objetivo
Existen las piezas del mecha como datos: 5 ranuras (brazo izquierdo, brazo derecho, torso, piernas y cabina), 4 ramas que se mezclan
(militar, tech, cuerpo a cuerpo y fuego) y 4 niveles, más las 5 piezas de serie con las que sale el mecha. Una regla pura suma las 5
piezas puestas sobre los datos de serie y da los valores del mecha. Las piezas son objetos del inventario (se guardan, se ganan en
apuestas y se venden en el mercado negro). Todavía no se ponen en el mecha (T-167) ni se compran (T-168).

## Contexto
- Diseño: mecha-taller.md ("5 piezas ... 4 ramas ... se mezclan libremente"; trade-offs: cuerpo a cuerpo más vida y velocidad, menos
  daño; militar y fuego más daño, más frágiles; tech vida y daño medios; "4 niveles por rama, desbloqueados con las 4 ligas"; criterios
  "Puedo tener un brazo de fuego y el otro militar" y "Un mecha de cuerpo a cuerpo corre más y aguanta más que uno militar, y el militar
  pega más fuerte").
- Decisiones: D-tony-79 (el inventario es de Economia y "el bloque de piezas extenderá `DatosObjeto`"), D-tony-82 (venta fija). Propuestas
  nuevas: D-tony-115 (qué mueve cada ranura), D-tony-116 (ramas), D-tony-117 (niveles y piezas de serie), D-tony-118 (dónde viven) y
  D-tony-120 (tabla de bonos y precios, provisional).
- Contrato: `Core/Datos/ValoresMecha.cs` y `DatosMecha.Valores` (T-163).
- Ejemplos: `Economia/Inventario/DatosObjeto.cs`, `CatalogoObjetos.cs`, `Economia/Editor/CreadorInventario.cs` (crea `CatalogoObjetos_Base`),
  `IA/Editor/CreadorRivalesLiga.cs` (muchos assets desde una tabla; el creador no pisa valores de un asset que ya existe, D-tony-109).

## Qué hacer
1. `Mecha/Piezas/RanuraPieza.cs` (`BrazoIzquierdo`, `BrazoDerecho`, `Torso`, `Piernas`, `Cabina`) y `Mecha/Piezas/RamaPieza.cs`
   (`DeSerie`, `Militar`, `Tech`, `CuerpoACuerpo`, `Fuego`). Solo se agregan valores al final.
2. `Mecha/Piezas/DatosPieza.cs`: hereda de `DatosObjeto` (tipo siempre `Pieza`) y agrega `ranura`, `rama`, `nivel` (0 = de serie, 1 a 4),
   `precio` ($ en el taller; 0 = no se compra), `bonoVida` (puntos), `bonoDanio` (puntos por golpe), `bonoVelocidad` (m/s) y `bonoRecarga`
   (s; negativo = golpea más seguido). Si `DatosObjeto` no deja a una clase hija fijar sus campos, agrégale un método protegido (Economia es
   de Tony); no cambies sus campos serializados.
3. `Mecha/Piezas/ReglaEstadisticasPiezas.cs` (pura): `Componer(ValoresMecha deSerie, IReadOnlyList<DatosPieza> puestas)`: vida, daño y
   velocidad suman los bonos; aceleración, frenado y giro se multiplican por la misma razón que la velocidad (nueva / de serie); recarga suma
   sus bonos y nunca baja de 0.3 s; lo demás queda como el de serie. Pieza null o repetida en una ranura: se ignora con warning.
4. `Mecha/Piezas/CatalogoPiezas.cs` (ScriptableObject): todas las piezas; `DeSerie(ranura)`, `De(ranura)`, `Buscar(id)`.
5. Creador `Mecha/Editor/CreadorPiezas.cs` (idempotente; no pisa valores de un asset existente): en `Mecha/Datos/Piezas/` las 5 de serie y
   las 80 de rama (5 ranuras × 4 ramas × 4 niveles) con la tabla de abajo, `CatalogoPiezas_Base`, y agrega las 85 a `CatalogoObjetos_Base`
   (Economia/Datos) para que el inventario las conozca. Si `CreadorInventario` reescribe la lista del catálogo, ajústalo para que conserve
   lo que ya tiene. Ids `pieza_<ranura>_<rama>_<nivel>` (por ejemplo `pieza_brazoder_militar_2`); nombres en mayúsculas con número romano
   ("BRAZO DER. MILITAR II", "TORSO DE SERIE"). Máximo en el inventario 2 de cada una; valor de venta la mitad del precio; las de serie
   no se compran y se venden en $100.

   Bonos POR NIVEL (se multiplican por el nivel; provisional, D-tony-120):
   | Ranura | Militar | Tech | Cuerpo a cuerpo | Fuego |
   | --- | --- | --- | --- | --- |
   | Torso | vida +14 | vida +22 | vida +30 | vida +12 |
   | Piernas | vida +4, vel +0.08 | vida +6, vel +0.15 | vida +10, vel +0.25 | vida +3, vel +0.12 |
   | Cada brazo | daño +3, vida +2 | daño +2, vida +4 | daño +1, vida +5 | daño +2.5, vida +2, recarga −0.03 s |
   | Cabina | vida +4 | vida +6 | vida +8 | vida +3 |
   Precio por nivel: I $500, II $800, III $1,200, IV $1,800 (igual en las 4 ramas). Las de serie no dan bonos.
6. EditMode `Tests/EditMode/Mecha/ReglaEstadisticasPiezasPruebas.cs` (cuentas con piezas creadas en la prueba) y
   `Tests/EditMode/Mecha/PiezasAssetsPruebas.cs` (lee los assets).

## Carpetas permitidas
Assets/_Proyecto/Mecha/Piezas/ (nueva), Assets/_Proyecto/Mecha/Datos/Piezas/ (nueva), Assets/_Proyecto/Mecha/Editor/,
Assets/_Proyecto/Economia/Inventario/DatosObjeto.cs (solo un método protegido), Assets/_Proyecto/Economia/Editor/CreadorInventario.cs,
Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset (solo con los creadores), Assets/_Proyecto/Tests/EditMode/Mecha/,
Assets/_Proyecto/Tests/EditMode/Economia/
No tocar: Core/, Combate/, prefabs, escenas, IA/, Pelea/, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ReglaEstadisticasPiezasPruebas: las 5 de serie dan exactamente los valores de `DatosMecha_Jugador`; torso cuerpo a cuerpo II suma
      60 de vida; piernas tech I suben la velocidad a 4.15 y la aceleración en la misma razón; brazo izquierdo de fuego III y brazo derecho
      militar III juntos (con las demás de serie) dejan el daño en 26.5 (10 + 7.5 + 9) y la recarga en 0.71 s (se pueden mezclar ramas);
      piezas de prueba con −0.5 s de recarga no la bajan de 0.3 s; una pieza en la ranura equivocada o repetida se ignora con warning
- [ ] PiezasAssetsPruebas: hay 85 piezas con id único, 1 de serie por ranura y 16 por ranura con rama; todas están en `CatalogoObjetos_Base`
      y en `CatalogoPiezas_Base`; con las 5 piezas del mismo nivel, cuerpo a cuerpo tiene más vida y más velocidad que militar, militar más
      daño que cuerpo a cuerpo, y tech queda entre los dos en vida y en daño (niveles 1 a 4); el precio sube con el nivel y el valor de
      venta siempre es menor que el precio
- [ ] Las pruebas de inventario y venta que ya existían (ServicioInventario, Venta, Tienda) siguen en verde
- [ ] Correr `CreadorPiezas` y `CreadorInventario` dos veces, en los dos órdenes, no cambia ningún asset (md5 igual)

## Criterios de sensación (para un humano)
- Tony: leer la tabla de bonos y precios (D-tony-120). ¿Las ramas se sienten distintas en el papel? ¿Fuego (golpe más seguido) contra
  militar (golpe más pesado) te convence, o quieres otra diferencia?

## Pruebas automáticas
- EditMode: ReglaEstadisticasPiezasPruebas, PiezasAssetsPruebas.

## Fuera de alcance
- Poner piezas en el mecha (T-167), comprarlas (T-168), verlas (T-170), piezas en juego (T-171), balance contra las ligas (T-172).
  Armas o habilidades por pieza y el cooldown más corto de tech (bloque 4). Pieza destruida en pelea (propuesta del diseño, no se hace).
