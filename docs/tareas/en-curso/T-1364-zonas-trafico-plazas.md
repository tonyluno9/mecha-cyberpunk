# T-1364: zonas de tráfico y plazas en la ciudad

Rama tony/T-1364-zonas-trafico-plazas. Script `MechaCyberpunk.Arte.Editor.ColocarT1364ZonasTrafico.Aplicar` (raíz T1364_ZonasTrafico en Ciudad_Prologo, idempotente).

## Hecho (sin correr Unity: el script y las pruebas están escritos, falta ejecutarlos)
- 10 cajas ZonaTrafico (el componente admite una sola caja, así que una zona con varias áreas usa varias instancias con el mismo Nombre). Sin encimes: Malecón x -330..115 (Puerto desde 115.5), cubierta de autopista y 8..20 sobre Deshuesadero y -2..7.5, rampa este x 280.5..320, Avenida centro hasta x 46.5, NeonAlto desde 47, LasLomas desde z 88.5.
- Plazas: Kiosco, 2 Esquinas (U1/U2, lado ciudad), 3 Banquetas (paseo), buscadas por rejilla con comprobación de calles, colliders (OverlapBox) y altura de banqueta (raycast); losa de adoquín T06 con KitS2c.Losa porque los prefabs no traen piso. Lo que no cabe se anota "NO CUPO" en Logs/t1364-zonas-trafico.txt.
- Pruebas EditMode `ZonasTraficoT1364Pruebas` (se agregó Arte.Editor al asmdef de pruebas de Trafico).

## Pendiente
- Ejecutar Aplicar y revisar el informe (el Malecón está muy lleno: puede que el Kiosco o alguna Esquina no quepan).
- Las Lomas del M12 no existe: LasLomas cubre los corredores norte (docs/assets-faltan.md).
