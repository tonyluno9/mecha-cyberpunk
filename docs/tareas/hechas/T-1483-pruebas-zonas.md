# T-1483: pruebas de las zonas nuevas (revertir mapa 19-nov)

Ramas: tony/T-1483-pruebas-zonas. Solo se tocaron pruebas de Mundo y Trafico/PlayMode; no se corrió Unity (sin Library).
Razón de los Ignore: `T-1483: zonas nuevas quitadas del mapa (revertir mapa 19-nov)`.

## Cambios hechos

| Archivo | Clase / método | Acción | Razón |
| --- | --- | --- | --- |
| EditMode/Mundo/OrillasT1358Pruebas.cs | clase completa | Ignorada | Aplica ColocarT1358Orillas sobre Ciudad_Prologo y busca T1358_Orillas, SalidaDelAgua, tablero de T1356_Autopista en la escena. |
| PlayMode/Mundo/RecorridoZonasT1361Pruebas.cs | clase completa (6 UnityTest) | Ignorada | Carga Ciudad_Prologo y recorre Malecón, Cerro, Autopista y Puerto en la escena. |
| PlayMode/Trafico/TraficoZonasT1350Pruebas.cs | clase completa (3 UnityTest) | Ignorada | Carga Ciudad_Prologo y exige CalleZona activas de Malecon/Cerro/Autopista/Puerto, caminos a esas zonas. (Es de Trafico, pero prueba la escena, no datos de carros.) |
| EditMode/Mundo/FondoCiudadT513Pruebas.cs | Fondo_CuatroTramosConSuPintura_SinSombrasNiLuces | Regresada a 8b33987^1 | T-1358 la cambió a `GetComponentsInChildren<MeshRenderer>(true)` para aceptar tramos apagados; vuelve a `GetComponentsInChildren<MeshRenderer>()` (tramos activos). |
| EditMode/Mundo/AjustesT1317Pruebas.cs | Postes_TienenLaIntensidadNueva | Regresada | Aceptaba intensidad de sodio de zona (T-1392/T-1461); vuelve a `Assert.AreEqual(IntensidadPoste, l.intensity, 0.001f, ...)` en todos los postes. |
| EditMode/Mundo/AjustesT1331Pruebas.cs | Postes_TienenLaLuzNueva | Regresada | Saltaba los postes de sodio de zona; vuelve a exigir 140 y rango 16 en todos. |
| EditMode/Mundo/LuzK01S2oPruebas.cs | Postes_ElNaranjaQuedaBajoCadaPoste | Regresada (solo esa línea) | Alcance máximo vuelve a `16f` (antes `max(AlcanceSodio) + 0.001`). |

Diff de lo regresado (de 8b33987^1 a HEAD, lo que se deshizo):
- FondoCiudadT513: `GetComponentsInChildren<MeshRenderer>()` -> `(true)` + comentario T-1358.
- AjustesT1317: `AreEqual(IntensidadPoste, l.intensity...)` -> `deZona = Approximately(IntensidadSodio T1392 | T1461)`; `IsTrue(deZona || ...)`.
- AjustesT1331: se agregó `deZona` con `continue` tras `LessOrEqual(l.range, max(AlcanceSodio)+0.001)`.
- LuzK01S2o: `LessOrEqual(l.range, 16f ...)` -> `LessOrEqual(l.range, Mathf.Max(LookNocheT1392.AlcanceSodio, LookNocheT1461.AlcanceSodio)+0.001f ...)`.

Nota: las pruebas regresadas ya no referencian `LookNocheT1392`/`LookNocheT1461`; esas clases pueden borrarse del editor sin romper estas pruebas.

## Revisadas y que se quedan (siguen válidas)

Puras (cálculo sin escena): CalculoSalidaDelAguaPruebas, PoseQuietaPruebas, NieblaPorAlturaPruebas, MedidorFpsT1366Pruebas, RutaAereaT1398Pruebas, BarridoMapaT1360Pruebas, CerroT1355Pruebas, MaleconT1354Pruebas (incluye RellenoT530Apagable, es matemática), PuertoT1357Pruebas, AutopistaT1356Pruebas, CallesDeZonaT1350Pruebas (grafo en memoria, sin CalleZona de escena), ZonasTraficoT1364... (ver abajo), BarridoZonasT1360Pruebas (arma su propia raíz), MarT1351Pruebas (crea plano y material propios).
Validan prefab/asset (el prefab sigue en el proyecto, no abren la escena): ArreglosCerroT1363, ArreglosPuertoT1366, RendimientoCerroT1365, ReporteAT1367, VariarCerroT1364, PiezasZonasT1353, DronesT1397, PersonajesNuevosT1389b, y en PlayMode: RendimientoCerroT1362, RecorridoCerroT1361, RecorridoPuertoT1361 (cargan el prefab solo).
Otras (sonido, UI) no tocadas: Sonido*, Personaje/*Sonido*, MapaCiudadT747 y MapaPlayT747 (UI, las ve T-1481).

## Anotadas para que el orquestador las concilie (T-1482 y datos)

| Archivo | Qué comprueba | Por qué conciliar |
| --- | --- | --- |
| EditMode/Trafico/TraficoZonasT1389Pruebas.cs | Cajas ColocarT1364ZonasTrafico con Malecon/Cerro/Autopista/Puerto, ConfigCarrosZonas, prefabs ZonaTrafico_<zona>.prefab, carros nuevos salen en alguna zona | Datos de tráfico (T-1482). Si T-1482 quita zonas del config o de las cajas, fallan `Diseno_LasCuatroZonasNuevasTienenCajaYCarros`, `Diseno_CadaCarroNuevoApareceEnAlMenosUnaZona...`, `Config_CoincideConElDiseno...`, `Prefabs_ExisteUnaZonaTraficoPorZona`. |
| EditMode/Trafico/ZonasTraficoT1364Pruebas.cs | ZonaDe(punto) da Malecon/Puerto/Autopista | Pura sobre cajas; solo cambia si T-1482 saca esas cajas. |
| EditMode/Trafico/ZonasTraficoT1345Pruebas.cs | Config con >= 6 zonas, incluye Malecon y Puerto (`GreaterOrEqual` ya era flexible desde T-1389a; antes de T-1389a era `AreEqual(6)`) | Datos de tráfico (T-1482). Exige Malecon y Puerto en ConfigCarrosZonas. |
| EditMode/PersonajeVisual/PeatonesZonaT1409Pruebas.cs | Config de peatones por zona, incluye Puerto/Malecon/Cerro; el pescador no sale en el puerto | Datos de peatones (T-1482). |
| EditMode/Mundo/CieloDiurnoPruebas.cs, LuzK01S2oPruebas.cs (niebla/nubes), LuzColorT507Pruebas.cs (AmbienteMaximo 0.7) | Valores de ConfigCicloDia / ConfigAmbienteNoche cambiados en 19-nov por T-1392/T-1399/T-1461 (niebla 0.009, color 0.30/0.50, ambiente 0.46, nubes 0.6) | Dependen de los assets de config, no de la escena. NO se regresaron: solo conviene regresarlos si T-1480 también restaura esos assets a `8b33987^1`. |
| EditMode/Trafico/EstrellasT1322Pruebas.cs, UI/MapaCiudadT747Pruebas.cs | no son de zonas de escena | MapaCiudad la revisa T-1481. |
