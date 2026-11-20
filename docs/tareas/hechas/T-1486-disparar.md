# T-1486: disparar a pie con peso

Rama tony/T-1486-disparar. Sin Unity en el worktree: nada de esto se compiló ni se corrió aquí; se verifica al integrar.

## Qué cambió
- `ReglaRetrocesoArma` (nueva, pura): el arma sube rápido atrás (20 % del tiempo) y regresa suave; la patada de cámara cae cuadrática y con duración propia por arma. `ReglaFloracionDispersion`: la dispersión crece por tiro seguido, se recupera por segundo y cuenta la mitad apuntando. `SonidosMarcaImpacto`: IDs de la marca.
- `AjusteSensacion` (en `DatosSensacion`) gana: `retrocesoArma`, `duracionRetrocesoArma`, `recuperacion`, `impulso`, `casquillos`, `floracion`, `topeFloracion`, `recuperacionFloracion`. El arreglo cubre las 10 armas (antes 5; las nuevas usaban un valor genérico).
- `SensacionDisparo`: pone un `CinemachineImpulseSource` en el jugador y lo usa con `TemblorCamara.Preparar/Generar` (canal 1, dirección abajo con desvío lateral); expulsa casquillos con un sistema de partículas (el pool, tope 32, con colisión en el piso).
- `PistolaAPie`: el retroceso visual sale de los datos por arma; floración en el cono; si la mira queda detrás del cañón la bala sigue a la cámara (antes salía hacia atrás).
- `Hitmarker`: al matar sale una cruz recta roja además de las diagonales (otra forma y color) y suena distinto.

## Tabla por arma (valores de fábrica y de AjustarSensacionT1486)
| Arma | Patada ° | Retroceso m / s | Recuperación s | Impulso | Casquillo | Floración ° (tope, °/s) |
| --- | --- | --- | --- | --- | --- | --- |
| Puños | 0.9 | 0 | general | 0 | 0 | 0 |
| Pistola | 1.0 | 0.07 / 0.12 | general | 0.25 | 1 | 0.3 (1.5, 6) |
| Escopeta | 3.2 | 0.16 / 0.26 | 0.30 | 0.6 | 1 | 0 |
| Fusil | 0.6 | 0.045 / 0.07 | 0.10 | 0.3 | 1 | 0.15 (2.5, 5) |
| Sniper | 4.5 | 0.20 / 0.40 | 0.45 | 0.6 | 1 | 0 |
| Subfusil | 0.5 | 0.035 / 0.06 | 0.09 | 0.22 | 1 | 0.12 (2.0, 5) |
| Revólver | 2.6 | 0.14 / 0.20 | 0.28 | 0.5 | 0 | 0 |
| Lanzagranadas | 3.0 | 0.15 / 0.30 | 0.30 | 0.6 | 0 | 0 |
| Granada | 0 | 0 | general | 0 | 0 | 0 |
| Rifle de plasma | 0.7 | 0.05 / 0.10 | 0.12 | 0.3 | 0 | 0 |

## Método de editor a correr
`-executeMethod MechaCyberpunk.Personaje.Editor.AjustarSensacionT1486.Ajustar` (menú Mecha Cyberpunk > Ajustar T-1486 peso del disparo). Escribe las 10 armas en `Resources/DatosSensacion.asset` y conserva el destello de T-676. Sin correrlo, las 5 armas viejas tienen valores por defecto del campo nuevo (retroceso 0.08 m, impulso 0.25, casquillo 1) y las 5 nuevas solo 5 entradas faltantes caen al genérico.

## Revisado y sin cambio
- Bala: ya sale del cañón (luz de boca) al punto del rayo central de la cámara (`PistolaAPie.Disparar`); el arma la alinea `ApuntadoIK`. No se tocó.
- Enemigo (`PandilleroAPie.RecibirDanio` y `AlLlegarAKO`): ya hace `visual.Herir` (flinch) al recibir y `visual.Caido`/`RegistrarGolpe` al morir; no se cambió IA. Falta confirmar en juego que no atraviesa el piso (lo cubre el visual, no se probó aquí).
- Sonido del disparo: `GestorSonidos` ya da volumen 0.8-0.9 por arma, tono ±2.5 % y volumen ±5 % por tiro (`ReglaMezclaSonidos`). Si se quiere más variación, es un cambio en Progresion/Sonido (fuera de esta tarea).
- Agarres/capas del animator (`AgarresArmas`, `AgarrePistolaPJ01`): no se editaron; no hay Unity para medir si el arma atraviesa el cuerpo.

## Qué probar (en Unity)
1. Correr el método de editor y las pruebas EditMode `DisparoPesoT1486Pruebas`.
2. Disparar cada arma: se nota la diferencia de patada, retroceso del arma y temblor (requiere el listener de T-1485); los casquillos caen dorados al piso.
3. Ráfaga de fusil o subfusil: la dispersión se abre y se cierra al soltar.
4. Pegar a un enemigo: marca blanca de diagonales y tic; matarlo: cruz roja y sonido de logro.
5. Disparar con el blanco pegado al arma: la bala no sale hacia atrás.

## Pendientes y supuestos
- Compila contra `TemblorCamara` de T-1485 (namespace `MechaCyberpunk.Personaje.Movimiento`); si falta en integ, no compila.
- Los casquillos pueden rebotar en el collider del propio jugador; si estorba, bajar `colision.bounce` o quitar la colisión.
- Pruebas PlayMode nuevas no se escribieron (no hay editor para correrlas); la existente `SensacionDisparoPruebas` debería seguir pasando (la patada y la marca no cambiaron de comportamiento).
- Retroceso solo posicional: la elevación del cañón (rotación del arma) no se hizo por depender del IK de la mano.

## Animaciones y sonidos que faltan
Ver `docs/animaciones-faltan.md`, sección T-1486.
