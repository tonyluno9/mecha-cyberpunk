# T-937 Conductor robado

Rama: tony/T-937-conductor-robado (sale de T-933). Playtest 09-nov.

Causa real (medida con jugador real y la tecla E): a los 3 s, cuando el conductor ya se levantó y empieza a huir, `ConductorHuye` tiraba el rayo al piso desde 3 m arriba y daba en la cápsula del propio conductor (`PeatonMundo` la agrega desde T-663; antes el conductor no llevaba collider). Le asignaba esa altura como piso y subía 1.8 m por cuadro: y = 1063 m a los 3 s, 28 000 m a los 10 s.

Cambios:
- ConductorHuye: los rayos (al piso y de esquive) ignoran los colliders del propio conductor y toman el primer golpe ajeno.
- ConductorRobado: la cápsula del conductor está apagada mientras lo sacan del asiento (nace dentro del volumen del carro) y se enciende al quedar junto a la puerta.
- Sin teléfono (T-933), tras levantarse huye.

Prueba nueva RobarConConductorT705Pruebas.ElConductor_AlHuir_VaPegadoAlPiso_YNuncaSubePorLosAires: sin el arreglo falla (26 232 m); con él pasa. RobarConConductorT705 + TraficoRobar (jugador real, entrada simulada): 8/8.
