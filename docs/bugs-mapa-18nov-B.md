# Bugs del mapa, 18-nov, sesión B

## Para la sesión A

Uniones del Puerto con otras zonas (no se tocan desde la sesión B, T-1366):

- Entrada norte, puerta U8 en x ~ 300: el carro del recorrido sale por el borde y cae en (300, -1.3, -44.6) (T-1361, `Carro_Circuito_CallesPrimeraMitad`). Falta continuar la calle y el suelo hacia la zona vecina.
- Entrada oeste, puerta U5 en (160, -215): misma unión con la zona vecina; revisar que la calzada de la otra zona empate con la del Puerto (y = 0, 12 m de ancho).

Unión de la calle base del Cerro con la ciudad en z = -120 (T-1363, T-1361): el carro se sale por el borde. La unión U3 viene del Malecón y no es de esta zona; revisar que el arroyo de la ciudad y el de Calle_Base se toquen sin escalón ni borde.

Nota: el carro "atorado" en (164.1, 0, -166) del circuito del Puerto no es un bug del mapa: la prueba une Calle_3 con Calle_4 en línea recta por el Bloque_16. No se cambia ningún `CalleZona`.

## Respuesta a la sesión A (18-nov C)

T-1367 (`ArreglarReporteAT1367`, aún sin correr; el informe medido queda en `Logs/t1367-arreglos.txt`):

1. Ladera dentro del túnel: copia nueva de la malla (`T1367_Ladera.asset`) asignada al MeshFilter y al MeshCollider de `Terreno/Ladera`. Los triángulos que tocan el prisma de la boca (x -368..-352, z 185..193, y 8.6..17.7) se quitan; los vértices a menos de 12.5 m del eje del túnel suben a y = 16.9. La prueba comprueba 0 vértices en el tubo y en la boca. El hueco de la boca llega hasta el borde de las celdas de 4 m.
2. U4: se quitan de las banquetas de Calle_Base y Calle_U4 los triángulos que caen dentro del arroyo de la otra (`T1367_Calles_Mallas.asset`); los arroyos se aplanan a y = 0 en la unión si algún vértice no lo estaba, y el arroyo encimado sube 8 mm sobre el otro. El código mide con rayos a lo largo de z = 0 y z = 3.4 antes y después (en el informe). El origen exacto del escalón en (-293, 0.1, 0) no se pudo medir sin Unity. CE03 de (-320, 1.4, 3.8): se mueve solo si queda dentro de una calle o banqueta; está a 15 m del eje de la calle base, así que probablemente no estorba ese carril: se confirma con el informe.
3. Puerto: se despeja una banda de 12 m en x (294 a 306) entre z -60 y -40 y las cajas de carro de (304.3, -56.2) y (295, -85.2), moviendo en x reja, caseta, postes, pallets y tambos. Lo que no se puede mover (bloques, bodegas, grúas) se anota en el informe como "NO se puede mover".
4. Parpadeo: el único par del barrido (Arroyo base / U4, 63 m2) se separa 8 mm. Además se separan 1.2 cm los planos y cajas coplanares de casas pegadas, se sacan al ras los FocoPared metidos más de 5 cm y se suben las CC01/CC02 con la base más de 30 cm bajo el terreno (sin dejar la esquina baja flotando más de 30 cm).
