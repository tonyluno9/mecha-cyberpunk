# Progreso: revertir el mapa a los 4 barrios (19-nov, noche)

Pedido de Tony: quitar del mapa la expansión (Malecón, Colonia del Cerro, Autopista elevada con túnel, Puerto) y dejar
la ciudad con los 4 barrios (Suburbios, El Mercado, Neón Alto, Nova) y sus bordes originales. Todo lo demás se queda.
Merge --no-ff a main y push autorizados por Tony. Una sola instancia de Unity; nada de PlayMode completo; no correr CreadorTodo.

## Dónde se trabaja
- Integración: `.sesiones/s2` (tiene Library), rama `integ/revertir-mapa` (sale de main 1389cab).
- Tareas: worktrees `.sesiones/rm-T-1480`, `rm-T-1481`, `rm-T-1482`, ramas `tony/T-148x-*`. Los subagentes no compilan:
  se integran en `integ/revertir-mapa` y se compila y prueba en s2.
- Base "antes de la expansión": `8b33987^1` (escena `Assets/_Proyecto/Escenas/Ciudad_Prologo.unity`).

## Lo que se encontró (inventario de la escena, antes vs ahora)
Raíces nuevas en la escena desde la expansión:
- Quitar: T1354_Malecon, T1355_Cerro, T1356_Autopista, T1357_Puerto (prefabs), T1388_Uniones, T1461_Look (prefabs),
  T1358_Orillas (límites nuevos y SalidaDelAgua), T1365_Uniones, T1392_Volumenes (volúmenes por zona), T1399_Bruma,
  T1462_Aereas.
- Dentro de la raíz `Zonas`: quitar ZonaTrafico_Malecon, _Cerro, _Puerto, _Autopista, _Autopista_RampaEste.
  Se quedan ZonaTrafico_Avenida_Centro, _LasLomas_Oeste/_Este, _NeonAlto, _NeonAlto_CorredorEste, _Deshuesadero.
- Se queda: T1398_DronesAereos (drones), revisando que sus rutas no salgan de los 4 barrios.
Objetos viejos que la expansión apagó o movió (todos estaban encendidos antes):
- T530_Afueras/Salida_0..5: Edificio, Ventanas (y un MeshRenderer de Salida_4/Ventanas), LimiteInvisible.
- S2_Calles/Salida_0..5: Kit_12, Kit_29, Kit_47, Kit_50, Kit_62, Kit_79, Kit_97.
- T513_Fondo: Tramo_1, Tramo_2, Tramo_3 apagados; Tramo_0..3 movidos (antes: posición 0, rotación 0, escala 1).
Cambios que NO son del mapa y se quedan: pandilleros, tinacos/aires/antenas (C09, C10, C20b, CY04), CicloDiaNoche y
niebla global, Barrio.prefab (sonido de puertas, persecución), PisoAdoquin, Plaza_Banqueta_1/3.

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1480 | Escena: script de editor que quita las raíces y zonas de tráfico de las zonas y vuelve a encender afueras y fondos | integrada; Aplicar corrido (11 raíces fuera, 66 objetos regresados por id) |
| T-1481 | Mapa grande y minimapa sin zonas ni nombres | integrada (regresa a 8b33987^1 lo de T-1352 y el png); falta compilar y probar |
| T-1482 | Tráfico, peatones, drones y objetivos solo en los 4 barrios | integrada; métodos ZonasTraficoT1482.Aplicar, PeatonesBarriosT1482.Aplicar, DronesZonasT1398.SoloPrefab. NO correr QuitarPrefabsViejos (los prefabs de zonas se quedan) |
| T-1483 | Pruebas de las zonas nuevas: ignoradas con razón | integrada (3 clases ignoradas, 4 pruebas regresadas a 8b33987^1) |
| Final | Correr T-1480 en s2, EditMode por clase, PlayMode por clases, capturas antes/después, reporte, builds | pendiente |

## Bitácora
- 19-nov: inventario hecho, rama de integración creada.
- T-1481 integrada en integ/revertir-mapa (5139687).
- T-1480, T-1482, T-1483 integradas (487ea5c). Siguiente: validar, pruebas por clase, capturas, reporte, builds.
