# Assets que faltan (playtest 17-nov-b)

El código ya está listo con un hueco para cada uno. Cuando llegue el asset se conecta en ese hueco; no se sustituyó nada con primitivos ni con otros modelos.

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| Camioneta de policía | 5 estrellas | `GestorPersecucion.prefabCamioneta` (hoy salen patrullas normales) |
| Nave/helicóptero que baja policías o enemigos con sogas | 5 estrellas y Nova | No se hizo nada (falta el modelo) |
| Sonido de radio de policía | Patrullas que llegan o vigilan | `ConfigPersecucion.IdSonidoRadio`, vacío. Al llegar: agregar el ID a CatalogoSonidos y a `ReglaMezclaSonidos` con sus pruebas |
| Sonido especial de llegada de Nova (más de 5 estrellas) | Aviso | `ConfigRespuestaNova.SonidoLlegada`; hoy usa `PAN_JEFE_ENTRADA` |
| Sonidos de enemigos nuevos (Nova, escudo), dron (vuelo y explosión), patrulla mejorada, otra explosión | Sonido | Sin hueco nuevo |
| Arma especial de Nova que dispare | Enemigos de Nova | No se hizo (falta el modelo) |
| Carros con cofre y puertas por separado (o carrocería rota) | Que la granada los deje "en pedazos" | `DanioCarro.LanzarPiezas` ya suelta hijos `Puerta*`, `Cofre*` y `Capo*` si existen; hoy solo salen las llantas |
| FBX con ruedas separadas para V07 y V08 | Ruedas que giren | V01a, V01b, V05 y la patrulla ya usan la rueda de V09 |
| Animaciones de NPC (platicar, gesticular, asustarse) | NPCs | No hay clips NPC_* en el proyecto; se usan `AcuerdoCompleto` y `Enojo` |
| Más NPCs de distintos tipos | Variedad | No se hizo |
| Parque con árboles (referencia K09) | Ciudad | 18-nov B: prefabs listos en `Assets/Arte/Zonas/PZ/Plazas/` (T-1341); falta colocarlos en la ciudad (sesión A o Tony) y el piso de adoquín |
| Texturas de techos y calles | Ciudad | Pedidas al chat de arte |
| Maletín del dinero (MA01 por Tripo) | Atraco | No se hizo |
| Lugares nuevos | Ciudad | Pedir referencia a Tony antes de armarlos |

## T-1337: mira del rifle de plasma (W07)

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| Pieza de mira propia en el modelo W07 (el FBX de Tripo es una sola malla; la "Mira" del banco es un corte de región) | Una mira visible en el arma | Sin hueco: hoy la mira es la línea de `MiraLaserPlasma` en `ArmasEspecialesAPie` |


## 18-nov, sesión B: modelos nuevos (nuevos-17nov)


### Modelos de zonas sin zip en imageassets/modelos-nuevos/nuevos-17nov/
| Código | Qué es | Estado |
| --- | --- | --- |
| CC03_CasaCerro | Tercera casa escalonada de la Colonia del Cerro | Solo está el .png de referencia, falta el zip de Tripo |
| CH03_EdificioColonial | Segunda variante del edificio colonial con arcos | Solo está el .png, falta el zip |
| NV01_NaveTransporte | Nave de transporte | Solo está el .png, falta el zip (no estaba en la lista de zonas) |
| V11_CamionetaPolicia | Camioneta de policía | Solo está el .png, falta el zip |

### Blender: llegó en la madrugada del 18 (ya integrado)
- Módulos de zonas (18 FBX): T-1346, en `Assets/Arte/Zonas/Modulos/`. Falta colocarlos en el mapa (sesión A o Tony).
- Carros V12–V24 y motos MT01, MT02: T-1342/T-1343 con puertas, cofre y cristal; al tráfico por zonas en T-1345.
- Cristales de los carros viejos: T-1343 (V01a, V01b, V02–V06, V09, V10). V07, V08 y C19 son motos: no llevan cristal.

### Vehículos: lo que sigue faltando
| Qué | Para qué | Hueco |
| --- | --- | --- |
| Prefab `Carro_V11` (camioneta de policía) | 5 estrellas | El FBX de Blender ya está en `Assets/Arte/Vehiculos/Piezas/V11/` con puertas, cofre y cristales; falta armarlo como carro de policía y conectarlo a `GestorPersecucion.prefabCamioneta` |
| Patrulla con su FBX de Blender | Que la patrulla también tenga cristal y cofre | `Patrulla.prefab` (Trafico) sigue con la carrocería de Tripo de V05 y la llanta de V09 (T-742/T-1331) |
| Zona de caballete del V07 en el cuerpo nuevo | Que no se vea el caballete al manejar | `ManejoMoto.mallaSinCaballete` del V07 es la malla completa |
| Luz neón abajo de V20/V21 (arrancones) | Deshuesadero | Sin hueco: hoy solo es la textura |
| Colocar los `ZonaTrafico_*` en el mapa | Que los carros salgan por zona | `Assets/_Proyecto/Trafico/Prefabs/Zonas/` (sesión A o Tony) |

### NPCs NP01–NP06
El chat de Blender les está poniendo el rig (ya no van por Mixamo). Hasta las 02:10 del 18-nov no habían llegado a imageassets/blender/.
Al llegar: importar como Humanoid con su avatar (`CreadorVisualPersonaje`/`CreadorVisualTripo`), darles las animaciones de peatón
que ya existen, mezclarlos con la gente de banqueta (`GenteDeBanqueta`, `ColocarT510`) y que los que platican queden de frente
(`PlaticarCon`). Ojo: el peatón de tráfico (`Trafico/Prefabs/Peaton.prefab`) es un solo prefab de cubos: para mezclarlos ahí hay que
cambiar `GrupoPeatones` a una lista de prefabs.

### Plazas
Los prefabs de plaza (T-1341) no llevan piso: el adoquín (T06) y la colocación en la ciudad los pone la sesión A o Tony.
