# T-1412: últimas fallas del EditMode del 19-nov

Rama tony/T-1412-ultimas-fallas (sale de integ/ciudad-19nov). Hecha sin Unity: nada se compiló ni corrió.

Por correr, DESPUÉS de las rondas de zonas, de LookNocheT1392 y de ArreglosT1411: `MechaCyberpunk.Arte.Editor.UltimasFallasT1412.Aplicar` (Logs/t1412-aplicar.txt). Después EditMode completo.

| Falla | Causa | Arreglo |
| --- | --- | --- |
| ArreglosPuertoT1366 (filas y flota) | T-1387/T-1396 renombraban los contenedores reemplazados por PU08 a "T1387_Reemplazado_PT01_..." y la prueba solo cuenta nombres PT01_Contenedor*: faltaban filas y los de arriba quedaban sin el de abajo | `Reemplazar` ahora deja "PT01_Contenedor..._T1387Reemplazado" / "_T1396Reemplazado" (apagado); `Restaurar` de las dos rondas entiende prefijo y sufijo. `Aplicar` renombra los 135 del prefab actual |
| Postes 340 | `KitS2c.Poste` usaba 340 y 20 m | Usa `LookNocheT1392.IntensidadSodio` (400); `Aplicar` sube a 400 los postes de zona que no estén en 400 o 140 |
| Cerro 20 m | mismo `KitS2c.Poste` | En obras "Cerro" usa 18 m; `Aplicar` baja a 18 toda luz del prefab del Cerro y a 22 los postes de otras zonas |
| PhysX cleaning the mesh | las pruebas ponen MeshCollider temporal a toda malla visible (AddComponent toma la malla del MeshFilter) y alguna es degenerada | `UltimasFallasT1412.MallaCocinable` se usa como guarda en ToldosMalos, FlotantesS2t, RevisarCiudadT995, BarridoMapaT1360 y los barridos de ZonaCerro1385/1394/1406; `Aplicar` cambia MeshCollider degenerados de los prefabs de zona por BoxCollider (o los quita) |
| RevisarCiudadT995 encimados | lámparas y tinacos de las bodegas K15 están dentro de la caja de su bodega | Se agregan a `ParejasPermitidas` ("Bodegas_K15/LamparaSodio_", "Bodegas_K15/Tinaco_"); no se quitan |
| SinGreybox (HaloRojo) | los materiales HaloLuz de T1396/T1387/drones no traían textura _Ruido | Los scripts la ponen (KitS2c.TexMuro) y `Aplicar` la pone en todo HaloLuz sin textura |

Riesgos: la lista completa de las 126 mallas de SinGreybox no estaba en el log (solo 15); si quedan otras aparecerán tras correr. Qué mallas son las degeneradas se sabrá en el informe.
