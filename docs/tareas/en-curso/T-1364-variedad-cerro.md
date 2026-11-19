# T-1364: variedad en la Colonia del Cerro

Rama tony/T-1360-barrido-cerro-puerto (sesión B). Que el Cerro se parezca a K13/A13 y deje de verse como cajas parejas.
Solo cambia el prefab `Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab` (no corre el generador ni toca escenas).

## Qué hace `Assets/Arte/Editor/VariarCerroT1364.cs`
`Aplicar()` (batch: `-executeMethod MechaCyberpunk.Arte.Editor.VariarCerroT1364.Aplicar`), idempotente, semilla 13640:
1. Cuartos de azotea (28 % de las casas del kit: CC01/CC02 de Tripo, 40-60 % de la azotea, 2.4-3.0 m, con su collider, en la esquina trasera con margen de 0.85 m a bordes con vecina) y pisos extra (17 %: duplica la fachada de arriba, estira el Cuerpo y sube la azotea; se salta si el desnivel con una vecina a < 2 m pasa de 2.3 m). Casas con `Acceso_*` no se tocan.
2. Escalonado: 30 % de las casas se corren 0.3-0.45 m al frente o 0.3-0.9 m atrás con su cimiento y lo que hay en su azotea.
3. Paleta de 8 colores (rosa mexicano, verde agua, mostaza, azul cielo, terracota, blanco hueso, lila, naranja) con 16 materiales en `Assets/_Proyecto/Mundo/Materiales/T1364/` (cuerpo y fachada C03); vecinas con color distinto. C20b no se tiñe.
4. Detalle de azotea 1-3 por casa del kit: tinaco con base, aire C10, antena CY04, macetas C20d, tendedero CC04, lavadero CC05 (máx. 60 de los dos últimos), margen de 0.85 m a bordes con vecina.
5. Cables: malla nueva `Extra_Cables` (poste a poste, poste a fachadas y cruces de azotea a azotea) con `KitS2c.Cable`.

Lo que agrega lleva el prefijo `Extra_`; lo que mueve o escala se anota en `Extra_Registro` y se deshace al volver a correr.
Informe: `Logs/t1364-variar.txt`. Fotos: `FotosAntes` / `FotosDespues` -> `docs/capturas/bugs-18nov-B/t1364-{k13,a13,calle1..3}-<sufijo>.png`.

## Pruebas
`Assets/_Proyecto/Tests/EditMode/Mundo/VariarCerroT1364Pruebas.cs` (necesitan el prefab ya variado).

## Pendiente
- Correr `FotosAntes`, `Aplicar`, `FotosDespues` y las pruebas (lo hace el orquestador). Sin correr, nada de esto está verificado.
- Las casas de Tripo (CC01/CC02) no reciben cuartos, pisos ni detalles.
