# T-1363: arreglos del Cerro (barrido T-1360 y recorridos T-1361)

Solo cambia el prefab `Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab`, con `Assets/Arte/Editor/ArreglarCerroT1363.cs` (`FotosAntes()` y luego `Aplicar()`; idempotente; informe `Logs/t1363-arreglos.txt`).

## Hecho (sin correr: lo corre el orquestador)
- Cimientos: BoxCollider con los límites de su malla.
- Carros: paralelos a la calle (el modelo tiene el largo en x y estaban atravesados), a ancho/2 - 1.25 m del eje, apoyados y con la pendiente, deslizados si chocan con un poste.
- Escaleras: la escala y de los módulos usaba 2.62 m y la subida real es 2.31 m; se recalculan módulos y barandales desde el piso medido arriba y abajo, y la Rampa de cada módulo se calibra con rayos para que siga la línea de la calle. `Escaleras/CE01_EscaleraCerro_N` es el núcleo de concreto (Terreno.Escalera), no una malla vieja; está 0.5 m abajo y no duplica el piso.
- Accesos: alturaSubida, rieles y peldaños hasta la azotea real. Accesos nuevos en las casas CC01/CC02 aisladas con techo plano y calle o azotea cerca.
- Saltos: los bloqueaban `CC04_Tendedero` y `CC05_Lavadero` pegados al borde. Props a 1 m del borde que da a otra azotea, pretiles quitados entre azoteas pegadas, colliders de Detalles que estorben apagados.
- Postes: collider del tamaño del tronco (sin halo).
- Zigzag (punto 9): el escalón de 0.84 m y la pendiente de 42 % en (-415, 28.3, 21.5) parecen reales: el anillo plano de la plaza (y=28.27) se traslapa con la pierna 3 (y cerca de 27.5) entre z=14 y 40. Propuesta: terminar la pierna 3 antes del anillo o subirla a la altura de la plaza en ese tramo. No se tocó.
- Peatones: el barrido mide en ancho/2 - 0.75 = 3.75 m (borde del arroyo), no en la banqueta real (5.25 m); los carros estacionados quedan en esa línea.

## Pruebas
- EditMode `ArreglosCerroT1363Pruebas` (6 pruebas) sobre el prefab ya arreglado.
