# T-748 RecorridoDemoPruebas en verde otra vez
Dueño: Tony | Rama: tony/T-748-recorrido-demo | Integrada en tony/s1-09nov

## Síntoma (main 7c96994)
Tramo 10: la pelea con pistola contra los SKULLS del Tianguis no termina (0/7, 7 vivos); tramo 11: el jefe no muere; tramo 12: el camino a EL HACKER se traba en (-69.65, 3.88, 34.24).

## Causa (era la prueba, no el juego; no hay bug de T-742 a T-747)
T-747 cambió el tramo 09 al banco de Tino (BancoArmas). Si E no abre el banco en 4 s, la prueba llama Colocar(banco.Posicion + back, 1.2). Colocar lanzaba el rayo desde 4 m sobre el punto: dentro del CIC pega en el techo y deja al jugador en la azotea
(y=4.00 en (-73.38, 12.00); el registro [T-476] lo muestra desde la primera línea de la pelea). Desde ahí el bot estaba a 66 m del objetivo, sin NavMesh en la azotea, "Esperando vista" y sin disparar; el tramo 12 se traba en el borde del techo (y=3.88).
Los tres bugs eran uno solo.

## Arreglo
RecorridoDemoPruebas.Colocar: el rayo sale 1.5 m sobre el destino (o sobre el jugador si está más alto) en vez de 4 m sobre el punto. No se debilitó nada.

## Pruebas
RecorridoDemoPruebas.DemoDePrincipioAFin: 1/1, 240 s, 0 bugs, sin rescate (Tianguis 24 s, jefe 5 s, Neón Alto 35 s).
