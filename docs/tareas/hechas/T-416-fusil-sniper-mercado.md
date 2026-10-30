# T-416 Cinco armas en El Mercado: el subfusil pasa a fusil de asalto y llega el sniper con zoom
Dueño: Tony | Rama: tony/T-416-fusil-sniper | Modelo: Sonnet 5.5, medium (datos y combate; Opus si algo falla 2 veces) | MODO RÁPIDO (docs/plan-noche.md, paso 2)
Depende de: T-414 (El Mercado y Biits).

## Objetivo
Tienes 5 armas: PUÑOS (1), PISTOLA (2), ESCOPETA (3), FUSIL de asalto (4, antes el subfusil) y SNIPER (5, nuevo). El sniper hace zoom fuerte con clic
derecho, tiene pocas balas y mucho daño. Las 5 se mejoran en 3 niveles en El Mercado, donde también se compran (menos los puños).

## Contexto
- Armas: `Personaje/Combate/ArmaAPie.cs` (enum: Punos, Pistola, Escopeta, Subfusil), `ArmasAPie.cs` (teclas 1 a 4 y la rueda, visuales por arma),
  `PistolaAPie.cs` (disparo, dispersión con `controlador.Apuntando`, luz de boca por arma), `Personaje/Jugador/ArsenalJugador.cs`. Datos:
  `Personaje/Datos/DatosArma.cs` y `Personaje/Datos/Armas/DatosArma_<clave>_<nivel>.asset`, hechos por `Personaje/Editor/CreadorArmas.cs` (tabla
  `Familias`, que también hace las ofertas y las mete a `Economia/Datos/DatosTienda_MercadoNegro.asset` y a `CatalogoObjetos_Base`). Las teclas están
  en las acciones de ControlesAPie que arma `Personaje/Editor/CreadorJugadorAPie.cs`.
- Cámara: `Personaje/Movimiento/CamaraHombro.cs` con `ConfigCamaraHombro` (FOV 60, apuntando 48); sensibilidad en `ControladorAPie`/`MiradaAPie`.
- HUD del arma: `UI/HudAPie/FormatoHud.cs` (línea 47 decide "ESCOPETA"/"SUBFUSIL"/"PISTOLA").
- Sonidos (T-407): el fusil sigue con los IDs de la LISTA 1 `ARM_SUB_DISPARO` y `ARM_SUB_RECARGA` (no se renombran: el equipo ya entrega con esos
  nombres); el sniper usa tres IDs nuevos `ARM_SNI_DISPARO`, `ARM_SNI_RECARGA` y `ARM_SNI_ZOOM`, con provisional generado por código (D-tony-254).
- El enum se guarda como número: `Subfusil` se renombra `Fusil` con el **mismo valor** y `Sniper` va **al final**. Los ids de objeto cambian de
  `arma_subfusil_N` a `arma_fusil_N` (las partidas viejas ya no se ven desde D-tony-233). Decisiones: D-tony-253 y D-tony-254.
- La mira del sniper es uGUI hecha por código (como `FormatoTerritorio.CrearCanvas`), porque el kit de UI llega en T-408; T-408 la pasa al kit.

## Carpetas permitidas
Personaje/ (Combate/, Jugador/, Movimiento/CamaraHombro.cs y MiradaAPie.cs, Datos/, Editor/CreadorArmas.cs y CreadorJugadorAPie.cs), Economia/Datos/
(ofertas y tienda, por código), UI/HudAPie/ (FormatoHud y la mira nueva `MiraSniper.cs`), Progresion/Sonido/ y Assets/Arte/Audio/Provisional/ARM/
(solo los 3 IDs del sniper), Progresion/Prologo/Editor/CreadorCiudadPandillas.cs, Escenas/Ciudad_Prologo.unity (solo con código), Tests/EditMode/Personaje/,
Tests/PlayMode/Personaje/ (y ajustar `CatalogoArmasPruebas`, `ArmasPruebas` y `FormatoHudPruebas`), docs/sonidos-cyberfix.md, docs de Tony.
No tocar: Radio/, Mundo/, IA/ (los pandilleros siguen con pistola y el jefe con escopeta).

## Qué hacer
1. **Datos** (tabla `Familias` de CreadorArmas; niveles base, I, II y III):
   | Arma | Daño | Balas | Cargador | Recarga (s) | Entre tiros (s) | Alcance | Dispersión (sin / apuntando) | Precio base · I · II · III |
   | --- | --- | --- | --- | --- | --- | --- | --- | --- |
   | Puños | 20 · 26 · 32 · 40 | 1 | 1 | 0.45 → 0.3 | 0.45 | 2 m | — | no se vende · 100 · 200 · 350 |
   | Pistola | 25 · 30 · 36 · 44 | 1 | 8 · 10 · 12 · 15 | 1.4 → 0.8 | 0.25 | 60 m | igual que hoy | 150 · 150 · 300 · 500 |
   | Escopeta | igual que hoy (8 perdigones) | 8 | 4 · 5 · 6 · 8 | 2.4 → 1.4 | 0.9 | 20 m | 6 / 4 | 350 · 250 · 450 · 700 |
   | FUSIL | 15 · 18 · 21 · 25 | 1 | 30 · 30 · 35 · 40 | 2.0 · 1.8 · 1.6 · 1.3 | 0.11, automático | 70 m | 2.5 / 1.2 | 700 · 350 · 600 · 850 |
   | SNIPER | 70 · 85 · 100 · 120 | 1 | 4 · 5 · 6 · 7 | 2.8 · 2.5 · 2.2 · 1.9 | 1.2 | 150 m | 8 / 0 (solo es preciso con zoom) | 900 · 400 · 650 · 900 |
   Nombres visibles: "Fusil" y "Sniper" (con " I", " II", " III"). La pistola base se vende para quien no la tenga (en la historia se recoge gratis);
   si ya la tienes, la tienda dice "LLEVAS EL MÁXIMO". El Mercado queda con 4 armas y 15 mejoras (19 ofertas), en ese orden: armas y luego mejoras por arma.
2. **Enum y teclas**: `Subfusil` → `Fusil` (mismo valor), `Sniper` al final; la acción y el campo `armaSubfusil` pasan a `armaFusil`, y se suma
   `armaSniper` (tecla 5) en el creador de los controles; la rueda recorre las 5 en orden y salta las que no tienes. Visual del sniper con primitivas
   (cañón largo, mira y culata) y su luz de boca, como el del fusil.
3. **Zoom del sniper**: con el sniper en la mano, clic derecho lleva el FOV de 60 a **15** en 0.15 s (con las demás armas sigue a 48); la sensibilidad
   del ratón baja a x0.35 mientras dura el zoom; suena ARM_SNI_ZOOM. Sale `MiraSniper` (canvas orden 16): pantalla negra con un círculo transparente
   del 80 % del alto, cruz fina turquesa y la distancia al punto apuntado abajo ("82 m", texto 22). Al soltar o al cambiar de arma, todo vuelve.
   Cada tiro con zoom da una sacudida de 1.5° hacia arriba que vuelve en 0.3 s.
4. **HUD**: `FormatoHud` dice "FUSIL II 28/30" y "SNIPER 3/4 · R" cuando toca. Conecta los 3 IDs del sniper (`Sonidos.Reproducir…`), genera sus
   provisionales con `GeneradorSonidosProvisionales` (disparo grave y largo, recarga de cerrojo, clic corto del zoom) y súmalos a docs/sonidos-cyberfix.md.
5. `CreadorArmas.Crear()` rehace los 20 DatosArma, las 19 ofertas, el catálogo y la tienda, y quita los assets viejos `*_subfusil_*` (con
   `AssetDatabase.DeleteAsset`). Vuelve a generar el prefab del jugador con su creador y córrelo desde `T416()` en `CreadorCiudadPandillas` (sumado a `Todo()`).

## Criterios de aceptación
- [ ] El Mercado lista PISTOLA, ESCOPETA, FUSIL y SNIPER y 15 mejoras con su precio en Ƀ; no queda ningún asset ni texto "subfusil" visible (grep).
- [ ] Comprado el sniper, 5 lo saca; con clic derecho el FOV de la cámara es 15 ± 0.5 en menos de 0.3 s y la mira está prendida; al soltar vuelve a 60.
- [ ] Un tiro con zoom tumba a un pandillero de 60 de vida a 80 m; el cargador del sniper base es 4 y el de SNIPER I es 5; sin zoom, a 80 m falla la
      mayoría (dispersión 8°: de 10 tiros simulados con semilla fija pegan 3 o menos).
- [ ] El fusil dispara solo mientras mantienes el clic (≥ 8 tiros por segundo) y sale como "FUSIL" en el HUD.
- [ ] Las pruebas viejas de armas (`CatalogoArmasPruebas`, `ArmasPruebas`, `FormatoHudPruebas`) ajustadas a 5 armas y en verde.
- [ ] Capturas revisadas a ojo y corregidas si se ven mal: docs/capturas/T-416-1.png (mira del sniper apuntando a un pandillero de noche) y T-416-2.png
      (El Mercado con la lista de armas), en 1920x1080.

## Pruebas (solo estas 2 nuevas)
- EditMode `CatalogoArmasPruebas.CincoArmasTresNiveles`: hay 20 DatosArma (5 x 4 niveles) que suben daño o cargador o bajan recarga en cada nivel;
  19 ofertas en DatosTienda_MercadoNegro; el sniper tiene el mayor daño por bala y el menor cargador; los puños no se venden.
- PlayMode `ArmasMercadoPruebas.RecorridoFusilYSniper` (InputTestFixture): Ciudad_Prologo con Ƀ3000 frente a El Mercado; E abre la tienda, ↓ y Enter
  compran FUSIL y SNIPER y SNIPER I; Esc cierra; un muñeco a 80 m; 5 saca el sniper, clic derecho hace zoom (FOV 15, mira prendida, captura 1),
  clic izquierdo lo tumba; R recarga a 5 balas; 4 saca el fusil y mantener el clic 1 s gasta 8 o más balas. Captura 2 en la tienda.

## Al terminar
- Bitácora de una línea, fila de docs/estado-tony.md en "hecha" y merge --no-ff a tony/fase-g (el cierre del paso 2 lo hace T-418).
  Nunca force push; `git restore` a lo que Unity cambia solo.

## Sensación (Tony)
- ¿El sniper se siente poderoso? ¿El zoom marea o es justo? ¿El fusil se siente distinto del viejo subfusil? ¿Los precios cuadran con lo que ganas
  por pandillero (Ƀ40)?

## Fuera de alcance
- Munición de reserva, balas que se compran, tiros a la cabeza, armas para los pandilleros y el modelo 3D de las armas (llega con el arte).
