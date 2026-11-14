# T-999: emboscada desde un techo en la ciudad real

Rama: tony/T-999-emboscada-techo. Dueño: Tony.

## Criterios
- [x] Prueba en Ciudad_Prologo con disparo real del jugador (pistola, clic derecho + clic, cámara apuntada con Empujar) desde la azotea vecina del grupo Acoso_5_Azotea: 2 s sin ser visto; el daño entra con el grupo acosando, sorpresa +0.01 s, alerta a +1.62 s (retraso 1.6 s, tolerancia 0.2 s).
- [x] Desde un techo junto a un grupo de suelo (Acceso_S2c_CalleDorada_04, techo y=6.25, junto a Acoso_3_Lote, 16.1 m, elegido con la regla real ReglaAcoso.Evaluar: nadie lo ve ni oye): 2 s sin ser visto; tiro real; alerta a +2.61 s (retraso desde arriba 2.6 s, desnivel 6.02 m).
- [x] Control a ras de suelo, de frente a 10 m sin obstáculos (Acoso_1_Plaza): alerta a 0.13 s (<= 0.5 s).
- [x] La azotea vecina se cruza al techo del grupo: hueco medido 0.75 m (<= 2.5 m). Había un pretil (C05_PretilAzotea, 0.9 m alto y 0.36 m grueso, sin apoyo para agarrarse) que lo impedía: el jugador se quedaba pegado y saltando sin subir. Arreglo: ColocarS2Pandillas.AbrirPasoEnAzotea (idempotente, también en el menú "Abrir paso en la azotea de la pandilla") apaga las 2 piezas de pretil en la línea a espaldas del grupo (~2 m de paso). Cruce a pie con entrada simulada: llega en 2.8 s.
- [x] TechosS2Pruebas: UnitySetUp que descarga la ciudad previa, MapasDeAccionesDePrueba.Reiniciar/AntesDelTearDown/DespuesDelTearDown y descarga de la ciudad en el TearDown. PandillasCiudadS2Pruebas ya descarga la ciudad; GrupoAcosoPruebas no carga escenas (grupos armados en el piso de prueba), no necesitan cambio.
- [x] Captura docs/capturas/t999/emboscada.png (T999_CAPTURAS=1) revisada: se ve al grupo desde la azotea vecina.

## Evidencia
- PlayMode "EmboscadaTechoT999Pruebas|TechosS2Pruebas|PandillasCiudadS2Pruebas|PasarelaNovaT996Pruebas|TreparAPiePruebas" en una corrida: 22 total, 20 pasan, 0 fallan, 2 saltadas (las capturas).
- PlayMode GrupoAcosoPruebas: 8/8. EditMode ReglaAcosoPruebas|FlotantesS2tPruebas|RevisarCiudadT995Pruebas: 25/25. verificar.sh compilar: OK.

## Archivos
Tests/PlayMode/Personaje/EmboscadaTechoT999Pruebas.cs, TechosS2Pruebas.cs, Arte/Editor/ColocarS2Pandillas.cs, Escenas/Ciudad_Prologo.unity (2 overrides m_IsActive de pretiles), docs/capturas/t999/.

## Pendiente
El techo vecino (-0.4 m, solo ~2.5 m de ancho) no se verificó si tiene acceso propio desde la calle; el cruce se prueba desde él.
