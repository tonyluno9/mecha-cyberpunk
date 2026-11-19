# T-1398: drones de Nova, SKULLS, la policía y tráfico aéreo

Rama: tony/T-1398-drones-nova-skulls. Dueño: Tony. Depende de T-1397.

## Qué se hizo
- `RutaAerea` (Trafico/Drones): sigue en bucle una lista de puntos (x, z) en el espacio local del padre, a una altura, en m/s, se inclina al girar y oscila arriba y abajo. La lógica pura está en `CalculoRutaAerea` (Catmull-Rom cerrada, velocidad constante, fase, inclinación por °/s de giro con tope).
- `GiroRotores` ahora no gira los rotores si la cámara está a más de `distanciaMaxima` m (por defecto 120; 0 = sin corte; se revisa cada 0.5 s). Lógica en `CalculoGiroRotores.DentroDeAlcance`.
- `Arte/Editor/DronesZonasT1398` (Aplicar, Validar, Fotos) arma Assets/Arte/Mundo/Zonas/T1398_DronesAereos.prefab (18 drones, tope 25) y lo instancia en Ciudad_Prologo.unity. Cada dron: contenedor con RutaAerea > Dron_<código> de T-1397 (sin colliders) + 2 luces de navegación emisivas (materiales en Zonas/Materiales_T1398/) + 1 Light chica sin sombras en los de baja altura.

## Dónde quedó cada grupo (coordenadas del mundo)
| Grupo | Drones | Altura | Dónde |
| --- | --- | --- | --- |
| Nova | 2 DR02 (a, b), 2 DR04_a, 2 naves NV03 (a, b) | drones 16 a 29 m, naves 70 y 90 m | Manzana_6 y torre (x 54 a 134, z -43); naves en elipses grandes alrededor |
| SKULLS | 3 E13 | 8, 10 y 12 m | Tianguis (calle z = 0, x -40 a 40) |
| Policía | 2 DR01 (además del dron de persecución) | 25 m | uno recorre el anillo de avenidas (x +-141, z +-94), otro la avenida z = 0 |
| Ciudad | 5 DR03_a y 2 NV01 | 85 a 120 m | rutas grandes por todo el mundo (x -480 a 345, z -330 a 290), incluidas las zonas nuevas |

Los territorios salen de ColocarS2cNova, ColocarS2Pandillas y CreadorCiudadPandillasT407 (ver el comentario de `Plan()`).

## Orden de ejecución (Unity cerrado, -executeMethod)
1. MechaCyberpunk.Arte.Editor.DronesT1397.Aplicar (si Assets/Arte/Vehiculos/Drones/Prefabs no existe)
2. MechaCyberpunk.Arte.Editor.DronesZonasT1398.Aplicar
3. MechaCyberpunk.Arte.Editor.DronesZonasT1398.Validar (Logs/t1398-validar.txt)
4. MechaCyberpunk.Arte.Editor.DronesZonasT1398.Fotos (docs/capturas/t-1398/)
5. EditMode: RutaAereaT1398Pruebas (16) y DronesT1397Pruebas

## Criterios
- [ ] Todo compila (el código se escribió sin Unity).
- [ ] RutaAereaT1398Pruebas pasan.
- [ ] Validar sin FALTA: 18 drones, sin colliders, a lo más 1 Light sin sombras cada uno, luces emisivas, rotores y corte de giro.
- [ ] Fotos revisadas: escala de cada modelo y que las luces queden en las puntas.

## Pendiente
- La escala real de DR*, E13 y NV* no se midió (sin Unity): Validar imprime el tamaño; si alguno sale desproporcionado, ajustar en `Plan()` o escalar el contenedor.
- Los FBX no traen LOD (Validar lo cuenta).
- Si se vuelve a correr DronesT1397.Aplicar los prefabs Dron_* se rehacen; correr Aplicar de T-1398 después.
- Sin colliders: los drones no chocan con el jugador ni con carros (tampoco les pueden disparar).
