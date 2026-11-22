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
