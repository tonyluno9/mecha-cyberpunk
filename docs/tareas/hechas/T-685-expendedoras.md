# T-685 Máquinas expendedoras y consumibles
Dueño: Tony | Rama: tony/T-685-expendedoras | Modelo: Sonnet 5.5, medium
Depende de: T-684 (escudo), T-142 (inventario), T-147 (pantalla de tienda)

## Objetivo
Máquinas expendedoras en la calle donde se compran objetos con E y un menú pequeño. Los objetos van al inventario (que se guarda con la partida) y se usan con una tecla.

## Qué se hizo
- Objetos (DatosConsumible, nuevo TipoObjeto.Consumible, en CatalogoObjetos_Base):
  bebida energética $60 (cura 25 y 5 s sin recibir daño), papitas $15 (curan 15), venda $90 (cura 70 repartidos en 4 s), café $40 (x1.35 de velocidad 20 s), batería de escudo $75 (recarga 40 de escudo).
- Teclas (a pie, con control y vivo): Tab elige el siguiente objeto que lleva, F usa el elegido, 6/7/8/9/0 usan directo bebida, papitas, venda, café y batería. No chocan con las teclas del jugador (1-5 armas, E, R, T, Ctrl, Shift, Espacio). Una cura no se gasta con la vida llena.
- Expendedora (UI/Tiendas): IInteractuable que abre la PantallaTienda existente (Enter compra, Esc sale). Se coloca en tiempo de ejecución por ColocadorExpendedoras desde UI/Datos/Resources/DatosExpendedoras_Ciudad.asset (lista de posiciones por escena): la escena Ciudad_Prologo no se editó.
- Posiciones medidas en banqueta libre (y = 0.2), frente a la casa (-55.5, -71), entrada del tianguis (-36, -8) y calle de Nova (64, -8.5). Modelo: Assets/Arte/Calle/Prefabs/CY06_Expendedora.prefab.
- HUD: línea "[TAB] OBJETO xN · F USAR" abajo a la izquierda y avisos de efecto activo.
- Por la parte del escudo: ControladorAPie.FactorExtraVelocidad (el café no pisa la cubierta) y VidaAPie.Curar / DarInvulnerabilidad.

## Criterios de aceptación
- [x] E en la máquina abre el menú; Enter compra y cobra; Esc sale y devuelve el control
- [x] Bebida: cura y 5 s sin recibir daño; papitas curan poco; venda cura más pero tarda; café acelera; batería recarga escudo
- [x] Los objetos se guardan con el inventario
- [x] Máquinas en la calle frente a la casa, el tianguis y Nova, sin encimarse ni flotar (capturas s1t-T-685-calle-*.png)

## Pruebas
- EditMode: Tests/EditMode/Personaje/ConsumiblesT685Pruebas.cs
- PlayMode: Tests/PlayMode/Personaje/ConsumiblesT685Pruebas.cs y ExpendedoraT685Pruebas.cs

## Para arte
- Las máquinas usan el modelo CY06 que ya había; los íconos de los objetos del menú son solo texto (sin ícono 2D).
