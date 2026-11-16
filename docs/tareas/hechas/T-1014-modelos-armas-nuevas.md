# T-1014: las armas nuevas se ven en todas partes (modelos, mano, piso, hologramas y banco)

Sigue el camino de W02-W04 (`ImportarS1n` y `ColocarS1nArmas`) con `Assets/Arte/Editor/ColocarT1014Armas.cs`
(`Todo()` hace todo; `Colocar()` solo los modelos y el jugador). Es idempotente.

## Qué quedó
- Modelos Tripo importados a `Assets/Arte/Armas/W05_Subfusil/`, `W06_Revolver/`, `W07_RiflePlasma/`, `W08_Lanzagranadas/` (`ImportarS1n.ArmasNuevas`; los .zip
  están en `imageassets/modelos3d/armas_nuevas/`). Material URP Lit con color, normal y una máscara metálico (R) y suavidad (A) hecha de `metallic` y `roughness`
  (`Wxx_mascara.png`).
- Prefabs `Wxx_Nombre.prefab` (cañón a +Z, empuñadura en el origen, hijo `Boca`) y `Wxx_Nombre_Piso.prefab` (con `ArmaEnPiso`).
- Granada: `imageassets/blender/granada/W09_Granada.py` (Blender por script) genera `W09_Granada.fbx` (piña con anillo y palanca, 0.107 m de alto, 160 vértices);
  `W09_Granada.prefab`, `_Piso.prefab` y `ConfigArmasEspeciales.prefabGranada` (sin colliders; también es el proyectil del lanzagranadas).
- `JugadorAPie.prefab`: objetos `Subfusil`, `Revolver`, `Lanzagranadas`, `Granada`, `RiflePlasma` en el socket de la mano derecha (modelo, `Boca` con `Light`),
  y `ArmasAPie.visualesNuevas` en el orden del enum.
- Agarres (`AgarresArmas`): revólver y granada como la pistola; subfusil, lanzagranadas y rifle de plasma como las armas largas, cada uno con su mano izquierda.
- Animación: `ReglasArmaAPie.UsaPoseDePistola` (pistola, revólver, granada) decide la pose en `AnimacionJugador`; esas armas también salen con el evento de sacar
  (`ArmasAPie`) y `AgarrePistolaPJ01` apunta la que esté en la mano. Subfusil, lanzagranadas y rifle de plasma usan la pose de armas largas.
- Tambor: la malla del revólver y la del lanzagranadas se parten por región en `Wxx_Cuerpo.asset` y `Wxx_Tambor.asset`; el prefab tiene los objetos `Cuerpo` y
  `Tambor` con el origen del tambor en su eje. `TamborArma` lo gira alrededor del cañón.
- Hologramas H05-H09 (`imageassets/ui/hologramas/generar_t1014.py`, desde el arte conceptual y el render de la granada), `ImportarS2nHologramas` (9 archivos),
  `PiezasUI` (`armas` y `armasHolograma` de 10), iconos `Cat_<Arma>.png` del inventario, y 5 entradas nuevas en `ConfigMenuArmas_Base` (`CreadorMenuArmas`), sin
  nombres de pieza (los modelos no traen cargador, cañón ni mira separados: el holograma se ve igual).

## Medidas (en el .fbx de Tripo: largo 0.98 m, cañón a -X, Y arriba; render de lado en Blender con rejilla de 0.1 m)
| Arma | Escala | Largo | Empuñadura (X, Y) | Boca (X, Y) | Mano izquierda en el arma (x, y, z) | Tambor (x de, x a, eje Y, radio) |
| --- | --- | --- | --- | --- | --- | --- |
| W05 Subfusil | 0.61 | 0.60 m | (0.31, 0.25) | (-0.49, 0.417) | (-0.038, -0.024, 0.30) | no tiene |
| W06 Revólver | 0.36 | 0.35 m | (0.35, 0.26) | (-0.49, 0.395) | como la pistola | 0.04, 0.265, 0.388, 0.083 |
| W07 Rifle de plasma | 1.02 | 1.00 m | (0.16, 0.157) | (-0.49, 0.30) | (-0.038, -0.007, 0.286) | no tiene |
| W08 Lanzagranadas | 0.82 | 0.80 m | (0.153, 0.155) | (-0.49, 0.294) | (-0.038, -0.053, 0.371) | -0.175, -0.015, 0.21, 0.12 |
| W09 Granada | 1.0 | 0.107 m | centro del cuerpo | luz a (0, 0.04, 0.15) | como la pistola | no tiene |

La granada va 9 cm arriba y 4 cm adelante de la palma para que se vea sobre el puño.

## Pruebas
- Nueva `ArmasNuevasT1014Pruebas` (PlayMode): en la mano, en el piso (E) y el tambor. Capturas con `CAPTURAR_S1=1` en `docs/capturas/t1014/`.
- Ajustadas: `CatalogoArmasPruebas` (40 armas, 39 ofertas de armas, 53 ofertas, subfusil presente, "el sniper pega más" solo contra armas de bala),
  `ArmasNuevasS1EditPruebas` (9 entradas del banco con modelo), `ArmasMercadoPruebas` (el Mercado lista el subfusil), `HudCyberfixPruebas` (iconos de armas = tamaño
  del enum) y `ArmasEspecialesT1013Pruebas` (`Preparar` pone el Animator en AlwaysAnimate: con modelo, el rayo y las granadas salen de la boca real y, sin esto, el
  personaje no se anima fuera de cámara y el rifle queda de lado).
- Ya fallaba en la base (T-1013): `CanonAlPuntoDeMiraT681Pruebas.CuatroArmas_AlApuntarYDisparar...` (4.43° contra 4.0° de tolerancia, igual con y sin T-1014).

## Notas
- La tira de casillas del HUD a pie sigue con 5 casillas (puños y las 4 armas); el arma activa siempre muestra su holograma.
- Un tambor de 6 recámaras a 60° se ve igual de lado: las capturas del tambor son a 0°, 30° y 60°.
