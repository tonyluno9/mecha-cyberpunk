# T-419 Assets 3D de la noche (39 modelos): importar y colocar
Dueño: Tony | Rama: tony/T-419-assets-noche | MODO RÁPIDO (docs/plan-noche.md, paso 4). Tony autorizó tocar Mundo/, Arena/, Vehiculos/,
Casino/, Trafico/ y Arte/ esta noche.

## Objetivo
Importar los 39 modelos nuevos con el flujo de T-250 y colocarlos en el juego. Parte 1 (esta): todo menos el kit de pisos. Parte 4b
(misma rama): kit de pisos sobre las fachadas del Barrio.

## Origen
`C:\Users\Angel Perez\Desktop\videojuego\assets_noche\<Código_Nombre>\` (FBX + .fbm con la basecolor), fuera del repo. Es dato externo:
solo se copia. CY04_AntenaAzotea trae nombre `tripo_convert_...`: se renombra al copiar.

## Qué hacer
1. Capturas antes (noche): calle principal, callejón, El Tianguis, Neón Alto, casa y taller: `docs/capturas/noche-<lugar>-antes.png`.
2. Importar los 39 a `Assets/Arte/<Zona>/<Código>/` con prefab (raíz con BoxCollider, hijo Visual, pivote en la base, material URP Lit, frente -Z).
3. Colocar (parte 1):
   - C01/C02/C03 _cy reemplazan a las C01/C02/C03 de T-250 (misma medida y posición).
   - CY01–CY10 por las calles, más densos en El Tianguis y Neón Alto; CY09 junto a I+D+I; CY08 en Neón Alto; CY04 en azoteas.
   - CY07b: visual del dron de vigilancia de la policía (Trafico/).
   - E01 entrada de VivaCoin, E02 portón de El Mercado, E03 pantalla gigante en Neón Alto, A08 marcador del clandestino,
     A11a/b portal y escalera del sótano en la entrada del clandestino.
   - T06, T07, T08 en el taller; H06, H07, H08, H09, H11 en la casa (quitar el cubo greybox junto a la cama).
   - C13 puesto ciber, C16 tambos y C17 huacales en El Tianguis; C18 barreras en orillas de zona; C19 moto estacionada.
   - Parte 4b: C20a, C20b, C20d, C21, C22, C23 (kit de pisos) se importan ahora y se colocan después.
4. Nada tapa puertas, mostradores, rutas de la historia ni NPCs: correr las pruebas PlayMode de la historia, el recorrido y El Mercado.
5. Nunca borrar el arte de T-250 (si se regenera Barrio.prefab: ColocarT250.Colocar y luego la colocación nueva).
6. Modelos de más de 30 mil triángulos: LOD o menos copias. Filas nuevas en docs/assets-importados.md.
7. FPS de noche en la calle principal: 60 obligatorio.
8. Capturas después revisadas a ojo: nada flotando, enterrado, de espaldas a la calle ni gigante.

## Carpetas permitidas
Assets/Arte/, Mundo/ (Barrio.prefab, Editor/ para la medición), Arena/, Casino/, Trafico/Prefabs/Dron.prefab, Escenas/Ciudad_Prologo.unity
(solo con código), Tests/PlayMode/ de las áreas tocadas, docs/assets-importados.md, docs/capturas/, docs de Tony.
- 2026-10-30 (4b): kit de pisos apilado con ColocarNoche.ColocarPisos (también lo llama Colocar), 98 fachadas, LODGroup por fachada; FPS de noche mínimo 121.9 en la calle principal; capturas noche-*-despues.png; pruebas ArteNochePruebas 6/6.
- 2026-10-30: PlayMode HistoriaDemo/HistoriaPandillas/Territorio/ArmasMercado pasan; MercadoNegroInterior (2) y RecorridoPrologoCiudad (3) fallan igual sin los cambios de T-419 (Barrio.prefab de b45f780): vienen de antes, no son del kit de pisos.
