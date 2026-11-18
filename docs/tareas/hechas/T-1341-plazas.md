# T-1341: plazas de barrio armadas con los props nuevos

Depende de T-1340 (prefabs AR01, AR02, PZ01, PZ02, PZ03 en Assets/Arte/Zonas/). Referencia visual: imageassets/referencias/look/K09_Plaza_Barrio.png.
El piso (adoquín T06) y la colocación de las plazas en la ciudad los hace la sesión A o Tony; esta tarea solo arma los prefabs.

## Hacer
- `Assets/Arte/Editor/PlazasT1341.cs`: crea en Assets/Arte/Zonas/PZ/Plazas/ (instancias anidadas, raíz en el centro a nivel de piso, sin collider propio, grupos Arboles, Bancas, Jardineras, Kiosco):
  - `Plaza_Kiosco` (~24x24 m): kiosco al centro, 8 bancas a 6.5 m mirando al centro (ninguna frente a la escalera), 4 AR02 en las esquinas, 4 jardineras en los bordes, 4 AR01 en las entradas medias (11 m).
  - `Plaza_Esquina` (~10x10 m): 1 AR02, 2 bancas en L mirando a la esquina libre, 2 jardineras.
  - `Plaza_Banqueta` (15 m en X): 3 AR01 cada 6 m y 2 bancas mirando a la calle (-Z).
- Constantes `GiroBanca` y `GiroKiosco` (grados, por defecto 0) para corregir hacia dónde miran los modelos tras ver la captura.
- Menú Mecha Cyberpunk/T-1341/Crear plazas; informe en Logs/t1341-plazas.txt. `CrearYCapturar()` para batch.
- Capturas en docs/capturas/T-1341-<plaza>-arriba.png y -ojos.png.

## Pruebas
- EditMode `MechaCyberpunk.Tests.Mundo.PlazasT1341Pruebas`: existencia, conteo por grupo, instancias de prefab, sin primitivos, bancas mirando al centro, sin encimarse.
