# Para la sesión A: qué colocar en el mapa (de la sesión B, 22-nov)

La sesión B no edita las escenas de la ciudad. Todo lo de abajo ya funciona sin colocar nada (usa respaldos), pero se ve y se juega
mejor si A lo pone en `Ciudad_Prologo.unity` (o en el prefab de la zona que toque). Los prefabs ya están en main.

| Qué | Prefab | Dónde | Por qué |
| --- | --- | --- | --- |
| Bancas donde el jugador se sienta (E) | `Assets/_Proyecto/Personaje/Interaccion/Prefabs/Banca_Sentable.prefab` | Sustituir las bancas PZ01 de las plazas y banquetas (o ponerla al lado). El frente de la banca es -Z, como PZ01_Banca. | T3: sentarse. Tiene dos `AsientoInteractuable` (±0.4 m). Para dejar un NPC sentado, arrastra su visual al campo `npcInicial` de un asiento. |
| Camilla del Doc | Un `IPuntoAparicion` con id `Clinica_Camilla` sobre la camilla de la clínica | Clínica del Doc, encima de la camilla, mirando hacia donde se levanta | T3: al despertar después de morir empieza acostado en la camilla y se levanta. Sin el punto, el clip se juega donde el Doc deja al jugador. |
| Punto donde lo molestan los SKULLS | `Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_Molestia.prefab` | Calle de Suburbios, a unos 20 m de la casa del jugador (`Inicio_Calle`), por donde sale caminando | T10 paso 3. Respaldo: `Inicio_Calle` + 22 m hacia adelante. |
| Ranura del muro | `Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_Ranura.prefab` | Un tramo del muro cerca de la casa, en Suburbios; el +Z del marcador mira hacia AFUERA de la ciudad | T10 pasos 5 y 6. Al cerrarse se pone una tapia de 3.2×3×0.5 m con collider. Si hay asset de grieta, ponerlo junto al marcador. |
| Pie de la torre de NOVA | `Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_Torre.prefab` | Entrada de la torre de NOVA | T10 pasos 15 y 16 (llegar a ≤35 m dispara C08). Respaldo: el `RecintoJefe` de la escena. |
| Puntos de escenas del barrio | `Assets/_Proyecto/IA/Prefabs/MarcadorEscenaBarrio.prefab` (radio 10 m, `idEscena` opcional) | Tianguis (`bully_piso_tianguis`, `vendedor_tacos`, `vendedor_elotes`), calle de Neón Alto (`bully_snakes_senora`), comercios con cortina (`bully_comerciante_cortina`), esquinas (`borracho`, `celular`, `pareja_discutiendo`, `vecinos_muro`, `vecinos_nova`) | T12 y T15: las escenas prefieren los civiles que estén dentro de un marcador. |

## Sin colocar nada
- Cápsulas de Nova (T7): caen junto al jugador donde hoy caían los refuerzos; no necesitan puntos. Si arriba hay techo a menos de 30 m usan la caída vieja.
- El jugador despierta en la calle frente a su casa (T10 paso 2) usando el `Inicio_Calle` que ya existe: revisar que quede frente a la casa en Suburbios.

## Nota
- Las cápsulas de los drones de Nova (DR01, DR02_a, DR04_a, E13) están listas (`CapsulaCaida.LanzarDron`), pero el spawn de drones de persecución
  sigue saliendo como antes: no se enganchó.

## Ronda C (22-nov noche)
| Qué | Prefab | Dónde | Por qué |
| --- | --- | --- | --- |
| Salida de los SKULLS del paso 3 | `Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_CallejonSkulls.prefab` | En el callejón o calle JUSTO a la vuelta de la casa del jugador, sobre el piso, en un hueco libre y fuera de la vista de donde despierta, con línea recta libre hacia la calle de la casa | T21 punto 3: los dos SKULLS salen de ahí y caminan hacia el jugador en línea recta (sin NavMesh). Sin el marcador se usa un punto de respaldo a 15-20 m de `Inicio_Calle`, a un lado, que puede quedar en un lugar raro. |

- T25: no toqué los prefabs de Vehiculos/Prefabs (los está cambiando T24); la patrulla buena es `Trafico/Prefabs/Patrulla.prefab` (cuerpo PA01). PatrullaMundo
  agrega DanioCarro al arrancar, así que si T24 cambia el modelo de alguna patrulla sigue recibiendo daño.

## Ronda D (22-nov noche, T30-T48)
| Qué | Prefab / archivo | Dónde | Por qué |
| --- | --- | --- | --- |
| Punto donde esperan los SKULLS | `Assets/_Proyecto/Progresion/Historia/Recorrido/Prefabs/Historia_EsperaSkulls.prefab` | A MEDIA CALLE frente a la casa del jugador, sobre el piso, a 6-10 m de `Inicio_Calle` hacia la calle (3-4 m del borde de la banqueta). Solo importa la posición. | T31: salen de `Historia_CallejonSkulls`, se paran aquí esperando y van por el jugador cuando camina. Sin el marcador usan un punto 5-10 m delante de `Inicio_Calle` (en la prueba quedaron junto a la banqueta de enfrente). |
| Expendedora cerca del camino | Una máquina `CY06_Expendedora*` o la tiendita | Entre el CIC y el Tianguis (camino de C06 a la guerra con los SKULLS) | T44: el paso "Tengo hambre…" manda a la máquina que vende más cercana; ya venden todas (T-1662). |
| Vendedores que "pregonan" | `Assets/Arte/Editor/NpcsBarriosT1542.cs` (línea ~473) | Generador de NPCs de los barrios | T40.3: si Tony los sigue viendo raros, cambiar `pregonar` por `esperar` y volver a correr el generador (toca la escena). |
| Escaleras y tubos a azoteas | `PuntoTrepar` en la escena | Donde lleven a un techo | T39: el trepar libre ya no sube a más de 2.5 m sobre la calle, pero los `PuntoTrepar` (escaleras de mano) siguen funcionando: quitar los que lleven a azoteas. |
| Drones de SKULLS | `Assets/Arte/Editor/DronesZonasT1398.cs` | Ruta de `Skulls_E13_*` | T40.5: ahora los drones vuelan por encima del edificio más alto bajo su ruta; los de SKULLS quedaron a ~88 m porque su ruta cruza una torre. Si se quieren bajos, acortar su ruta para que no pase sobre edificios. |

**Colocado por A (22-nov, merge 29e3705):** `Historia_EsperaSkulls` en (-46.5, 0, -76.5), bajo T1606_SesionB, sobre `Asfalto`, a 6.5 m de `Inicio_Calle` (-53, 0, -76.5) hacia +X; la calle mide 14 m, así que queda a media calle (7.5 m de la banqueta de la casa, 6.5 m de la de enfrente: los "3-4 m del borde" no caben a la vez con 6-10 m del inicio). Expendedora `CY06_Expendedora_T1686` (raíz T1686_SesionA) en (-64.3, 0.2, 9.5), banqueta norte de la calle z≈0, pegada a pared lisa entre una ventana y una puerta, 9 m al oriente del CIC y a la vista del camino al Tianguis; vende porque empieza con `CY06_Expendedora` (ColocadorExpendedoras). T39: los 35 `PuntoTrepar` de la escena llevan a techo (salida a 5.7-10.7 m sobre la calle); se apagaron los 32 que estaban activos, quedan 0 activos (no había ninguno solo de barda). Ojo: las pruebas PlayMode viejas EscalerasTechosT1101Pruebas y TechosS2Pruebas usan esas escaleras y ya no aplican. T40.3: los 3 vendedores del Tianguis con `NPC_Platicar_1` (Puesto_3, 6 y 9) pasaron a `NPC_Esperar` sin correr el generador. T40.5 (drones a ~88 m): no se tocó. Script `Mundo/Editor/ColocarSesionBT1686.cs`, prueba `ColocarSesionBT1686Pruebas` (3/3) y `LimpiezaT1680Pruebas` (4/4) pasan; fotos en docs/capturas/_tareas/T1686/.

## Ronda E de B (23-nov noche, docs/tareas/noche-23nov)
| Qué | Dónde | Por qué |
| --- | --- | --- |
| Abrir paso a los SKULLS del inicio | Patrulla estacionada `GTA_T1630_SoloPatrullas` en (-52.5, 0, -69.5) y expendedora `Expendedoras/Modelo` en (-55.5, 0, -71.0), entre la salida del callejón (~(-60, -62)) y `Historia_EsperaSkulls` (-46.5, -76.5) | T52: los SKULLS se atoraban en el hueco entre la máquina y el coche. Ya se desatoran por código (dejan de chocar con props chicos si no avanzan), pero se ve mejor si hay 2 m libres: mover la patrulla o la máquina. |
| Armería fuera de la historia | Letrero feo de la armería y su entrada | T59: Tino vende y mejora las armas. Quitar el letrero y cerrar la entrada (o dejarla como local cerrado). **NO borrar ni apagar el objeto `MostradorMercadoNegro`**: el COMPRAR del banco de Tino usa su lista (armas base y balas). Si se cierra el local, dejar ese objeto ACTIVO (se puede quitar solo su collider o la puerta); ya no se ofrece suelto. El `BancoArmas` y el `MostradorCIC` del CIC se quedan. |

- T68: nada obligatorio. Los horarios de locales (`HorarioLugar`) ahora ven siempre noche salvo que el jugador elija DÍA en Ajustes.
