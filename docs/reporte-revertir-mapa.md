# Reporte: mapa de vuelta a los 4 barrios (19-nov, noche)

Pedido de Tony: quitar la expansión del mapa del 18 y 19 de noviembre y dejar la ciudad como estaba. Quedan solo los 4 barrios
(Suburbios, El Mercado, Neón Alto y Nova) con sus bordes originales. Todo lo demás se queda.

Referencia de "cómo estaba": la escena del commit `8b33987^1`, antes del merge de las zonas.

Tareas, todas hechas por subagentes Sonnet en su rama e integradas en `integ/revertir-mapa`:

| Tarea | Qué |
| --- | --- |
| T-1480 | Escena sin las zonas, con las afueras y los fondos como antes |
| T-1481 | Mapa grande, minimapa y HUD sin las zonas |
| T-1482 | Tráfico, peatones y drones dentro de los 4 barrios |
| T-1483 | Pruebas de las zonas marcadas como ignoradas |

## Qué se quitó

### De la escena `Ciudad_Prologo.unity`
Lo quita el script de editor `MechaCyberpunk.Arte.Editor.RevertirMapaT1480.Aplicar`. Es idempotente.

- **Las cuatro zonas:** `T1354_Malecon`, `T1355_Cerro`, `T1356_Autopista` (con el túnel y el ramal) y `T1357_Puerto`.
- **Lo que se agregó para las zonas:**
  - `T1388_Uniones` y `T1365_Uniones`: uniones y rampas de los barrios hacia las zonas.
  - `T1358_Orillas`: límites nuevos del mundo, muros en el mar y `SalidaDelAgua`.
  - `T1461_Look`: faroles, neones y charcos de las zonas.
  - `T1392_Volumenes`: volúmenes de noche de las zonas.
  - `T1399_Bruma`: bruma de las zonas.
  - `T1462_Aereas`: skyline SK01 sobre el mar, `OcultarPorAltura` y torres de fondo de la autopista.
- **Zonas de tráfico de las zonas:** `ZonaTrafico_Malecon`, `_Cerro`, `_Puerto`, `_Autopista` y `_Autopista_RampaEste`.
- **Plazas del Malecón:** `T1364_ZonasTrafico/Plazas` (T-1341).

### Del mapa grande, el minimapa y el HUD
Volvieron a como estaban en `8b33987^1` los archivos que cambió T-1352 y la imagen del mapa:
- `BarriosMapa`, `Minimapa`, `MapaImagen` (cuadro de -200 a 200 por -140 a 140), `PantallaMapa` (zoom máximo 3x) y `CalculoCallesMapa`.
- `CapturadorMapaCiudad`, `EstiloM10` (ya sin el mar azul) y `HudH10` (el nombre de zona solo dice los 4 barrios).
- `MapaCiudad.png`.

### Pruebas
Las clases que solo prueban las zonas en la escena quedaron con
`[Ignore("T-148x: zonas nuevas quitadas del mapa (revertir mapa 19-nov)")]`:
- **Clases completas:** `OrillasT1358Pruebas`, `RecorridoZonasT1361Pruebas` y `TraficoZonasT1350Pruebas`.
- **Métodos sueltos:**
  - 4 de `TraficoZonasT1389Pruebas`.
  - 3 de `ZonasTraficoT1364Pruebas`.
  - 2 de `PeatonesZonaT1409Pruebas`.

Las pruebas puras (cálculos) y las que revisan un prefab de zona como asset se quedan, porque esos prefabs siguen en el proyecto.

## Qué se regresó a como estaba

### Afueras
Se volvieron a encender los 66 objetos que la expansión apagó o movió. Se encontraron por su fileID exacto; ninguno hizo falta buscarlo por ruta.
- **`T530_Afueras/Salida_0..5`:** los edificios de fondo, sus ventanas y los muros `LimiteInvisible` de los 6 finales.
- **`S2_Calles/Salida_*`:** Kit_12, 29, 47, 50, 62, 79 y 97.
- **`T513_Fondo`:** los 4 paneles de fondo, encendidos y en su lugar (posición 0, rotación 0, escala 1).

La autopista decorativa vieja del norte (z = 103, T-098) nunca se tocó y sigue igual.

### Azoteas
T-1410 había bajado o apagado 29 tinacos, aires, antenas y pisos C20b mientras estaban las zonas. Sin las zonas quedaban flotando,
así que volvieron a sus valores de antes de T-1410 (`ad662b8^`).

### Resultado
- Las raíces de la escena son las mismas que en `8b33987^1`, más `T1398_DronesAereos` (drones) y `T1364_ZonasTrafico`. Esta última solo tiene ahora las 6 zonas de tráfico de los barrios.
- La comparación objeto por objeto de T530_Afueras, T513_Fondo y S2_Calles contra `8b33987^1` no da ninguna diferencia.

## Qué se quedó

### Gameplay y contenido
No se tocó nada que no sea del mapa: sonidos nuevos, carros nuevos (ruedas, puertas y cristal), motos, tráfico, NPCs y enemigos nuevos, drones,
armas, implantes, policía, menús, HUD, arreglos de bugs y la luz y el look de los barrios (niebla global, CicloDiaNoche, perfiles).

### Carros que solo salían en las zonas
Ahora salen en los barrios (`DisenoZonasTrafico` y `ConfigCarrosZonas.asset`):

| Carro | Antes | Ahora |
| --- | --- | --- |
| MT01, MT02 (motos) | Malecón, Autopista y Cerro | Extras generales (toda la ciudad) |
| V20, V21 | Deshuesadero y Autopista | Deshuesadero y Extras |
| V22, V23, V24, V12 | Malecón y Autopista | Neón Alto (V12 también en Extras) |
| V15 | Malecón y Autopista | Las Lomas |
| V09, V10, V16, V17, V18 | Malecón, Puerto, Cerro y Autopista | Avenida |
| V13, V14, V19 | Autopista y Cerro | Avenida o Extras |
| V01a, V01b, V02, V03, V04 | Cerro y Puerto | Lista general del tráfico (ya estaban ahí) |

### Peatones de las zonas
Ahora salen en los barrios (`ConfigPeatonesZona_Base.asset`):
- Avenida (mercado): pescador y turista.
- Neón Alto: turista y corredor.
- Las Lomas: abuela, elotero y corredor.
- Deshuesadero: estibador.
- Las cuatro zonas: repartidor y oficinista.
- Fuera de esas zonas sale la lista general, con las 14 variantes.

### Drones
Las rutas del grupo Ciudad y las naves de Nova quedan dentro de x -240..240 y z -180..180. Antes llegaban a -480..345 y -330..290.
Siguen siendo los mismos 19 drones.

### Archivos de las zonas
Los prefabs, modelos y scripts de las zonas siguen en el proyecto, fuera de la escena:
- `Assets/Arte/Mundo/Zonas`, `Assets/Arte/Zonas` y `Assets/Arte/ZonasModulos`.
- Los prefabs `ZonaTrafico_Malecon/Puerto/Autopista/Cerro`.

Nada de eso está referenciado desde la escena ni desde `Resources`, así que no entra al build. `ZonasTraficoT1482.QuitarPrefabsViejos`,
que borraría esos prefabs, no se corrió.

## Revisión de que no quedara nada roto
- **`RevertirMapaT1480.Validar`:** sin raíces de las zonas, 0 scripts faltantes y 0 referencias a objetos borrados. Fuera del rectángulo de los barrios
  solo quedan las afueras viejas y la jaula del clandestino (x 400), que ya estaban ahí antes.
- **Nombres en la escena:** no queda nada de mar, faro, SK01, agua, muelle, Malecón, Cerro, Puerto ni Autopista.
- **Calles, bardas, tráfico y peatones:** pasan las pruebas de PlayMode de cierres de calles, tráfico, aparición de tráfico y peatones (abajo).
  Sin `CalleZona` en la escena, el grafo de calles, la policía y los peatones vuelven solos al comportamiento de antes.
- **Misiones y objetivos:** ninguno mandaba a las zonas. `Mision_Destino_Muelle` es del prólogo viejo.
- **Fotos antes/después:** en `docs/capturas/t-1480/` (hojas `comparacion-*`: a la izquierda antes, a la derecha ahora). Las aéreas de toda la ciudad, del sur, norte, este y oeste
  salen iguales a las de `8b33987^1`; lo único extra son los drones. Las calles de las salidas 0 y 1 quedan cerradas por su edificio de fondo, como antes.
  En las salidas 2 a 5 la cámara quedó pegada a una pared (coordenadas mal puestas), pero antes y ahora también salen idénticas.
  Script: `MechaCyberpunk.Arte.Editor.FotosT1480.FotosAhora` y `FotosAntes`.

## Pruebas que corrieron
- **EditMode** de Mundo, UI, Tráfico y PersonajeVisual: 1733 pruebas, 1719 pasan, 13 ignoradas y 1 falla.
  - La falla era `FlotantesS2tPruebas` (las azoteas de T-1410), ya corregida.
  - Después del arreglo, `FlotantesS2tPruebas`, `RevisarCiudadT995Pruebas` (sin piezas encimadas) y `RevertirMapaT1480Pruebas` pasan las 8.
- **PlayMode** por clase: `MapaPlayT747Pruebas` 3/3, `CierresCiudadT986Pruebas` 1/1, `TraficoCallePruebas` 8/8, `PeatonesBarrioPruebas` 7/7
  y `AparecerTraficoT704Pruebas` 5/5.
- No se corrió el PlayMode completo, a propósito.

## Pendiente para una persona
- Jugar un rato en la ciudad: manejar hasta cada una de las 6 salidas y ver que los carros y peatones se ven bien en los barrios.
- Dos cosas no las cubre ninguna prueba automática y conviene revisarlas a ojo:
  - Las 4 pruebas de postes (`AjustesT1317`, `AjustesT1331` y `LuzK01S2o`) se regresaron a su expectativa de `8b33987^1`, y en esta corrida pasaron.
  - Las pruebas de valores de cielo y niebla (`CieloDiurnoPruebas` y otras) siguen esperando el look nuevo, que se quedó.
