# T45 / T-1683: menos carros

Pedido (docs/tareas-por-hacer.md, T45): quedarse con los básicos (sedanes, compactos, pickups, taxi) y el tuneado con neón debajo.
Fuera tráileres, camiones y la mayoría de camionetas y deportivos (un deportivo suelto está bien).

Fuentes del tipo de cada carro: imageassets/PROMPTS_ASSETS.md (nombre y descripción de cada modelo), docs/tareas/hechas/T-1488-manejar.md (tipo de manejo)
y docs/assets-expansion-mapa.md. La lista vive en código en `Vehiculos/Editor/MenosCarrosT1683.cs` (`CodigosExcluidos`, `CodigosQueSeQuedan`, `Reemplazos`)
y en `Trafico/Datos/DisenoZonasTrafico.cs` (`Excluidos`, zonas sin los excluidos).

## Carros civiles

| Código | Modelo | Tipo | Queda / fuera | Por qué | En la escena se cambia por |
| --- | --- | --- | --- | --- | --- |
| V01a | CarroViejito | Compacto viejo | Se queda | Básico (compacto) | |
| V01b | CarroPro | Deportivo (T-1488) | Fuera | Deportivo; el que se queda es V12 | V13 |
| V02 | SedanViejo | Sedán | Se queda | Básico (sedán) | |
| V03 | PickupTrabajo | Pickup | Se queda | Básico (pickup) | |
| V04 | CombiVieja | Camioneta (combi) | Se queda | La única camioneta que queda: básica de barrio, con sus 6 variantes de color en el tráfico (las prueba CarrosS2nPruebas) | |
| V09 | Taxi | Taxi | Se queda | Básico (taxi) | |
| V12 | Deportivo | Deportivo (negro con línea de neón turquesa) | Se queda | El deportivo suelto | |
| V13 | Sedan | Sedán familiar | Se queda | Básico (sedán) | |
| V14 | CompactoViejo | Compacto | Se queda | Básico (compacto) | |
| V15 | Lujo | Sedán de lujo largo | Se queda | Es sedán; no está en la lista de fuera | |
| V16 | Microbus | Microbús (7.5 m) | Fuera | Camioneta grande / transporte | V02 |
| V17 | Camion | Camión (7.5 m) | Fuera | Camión | V03 |
| V18 | Trailer | Tráiler (16 m) | Fuera | Tráiler | V14 |
| V19 | Ambulancia | Servicio | Se queda | Vehículo de servicio: no se quita | |
| V20 | Tuneado | Coupé tuneado, neón turquesa debajo | Se queda | El tuneado con neón debajo | |
| V21 | Tuneado | Hatchback tuneado, neón magenta debajo | Se queda | También es tuneado con neón debajo (los dos tuneados lo llevan) | |
| V22 | Superdeportivo | Superdeportivo | Fuera | Deportivo | V20 |
| V23 | Superdeportivo | Hiperdeportivo | Fuera | Deportivo | V21 |
| V24 | Superdeportivo | Gran turismo | Fuera | Deportivo | V09 |

V10 (pickup) y V11 (van) ya estaban fuera desde T-1554.

## No se tocan

| Código | Qué es | Por qué |
| --- | --- | --- |
| PA01 | Patrulla buena (Patrulla.prefab) | Policía; la de la foto de T-1644 |
| PA02 | Patrulla SUV (PatrullaCamioneta.prefab) | Policía |
| V05, V06, V08, V11 | Patrulla sedán, patrulla camioneta, moto de patrulla, camioneta de policía | Policía (ReglaModeloDePolicia) |
| V07, C19, MT01, MT02 | Motos | No son carros; el pedido no las toca |
| Nova | Vehículos y drones de Nova | No son civiles |

## Qué cambia

- Listas: `MenosCarrosT1683.Aplicar` quita Carro_X, CarroTrafico_X y sus variantes de los excluidos de `Trafico/Resources/ConfigCarrosZonas.asset`
  (y cualquier otra lista de datos de Trafico y Vehiculos/Datos) y de `TraficoCalle.prefabsCarro` en Ciudad_Prologo. Los prefabs se quedan en disco.
- Escena Ciudad_Prologo: cada estacionado o robable de un modelo excluido se cambia por su reemplazo, con la misma rotación y la huella en el mismo lugar,
  y luego `EstacionadosTripoT1644.Ajustar` los asienta (piso, banqueta, encimados; si alguno no cabe se apaga, no se borra).
- Explosión: `DanioCarro.Quemar` pone el material de armazón quemado en todas las mallas (carros de piezas, de Tripo y motos).
