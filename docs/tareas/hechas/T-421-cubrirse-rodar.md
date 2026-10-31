# T-421 Cubrirse y rodar
Dueño: Tony | Rama: tony/T-421-cubrirse-rodar | Modelo: Sonnet 5.5 | MODO RÁPIDO (docs/plan-noche.md, paso 5; mejora 2 de docs/mejoras-pendientes.md)
Depende de: T-420. Tony autorizó tocar todas las áreas y Core.

## Objetivo
El jugador a pie se pega a cubierta (pared, carro, caja) con Q y queda agachado: las balas del otro lado no le hacen daño; al apuntar se asoma y puede disparar; sale moviéndose lejos o con Q.
CTRL rueda en la dirección de movimiento con cuadros de invulnerabilidad y enfriamiento. Aviso en la ayuda de controles del HUD. Las balas de los pandilleros respetan la cubierta.

## Qué hacer
1. Core: `IProtegeDeBalas` (Core/Jugador). Personaje: `DatosCubierta` (+ asset en Resources), lógica pura `ReglaCubierta` y `RodadaAPie`, `CubiertaYRodada` (lo crea ControladorAPie; teclas Q y CTRL).
2. `VidaAPie` implementa IProtegeDeBalas, ignora daño al rodar y reduce el daño de disparos cubiertos. `PistolaPandillero` no acierta si la cubierta está en medio.
3. HUD: texto de ayuda a pie con Q y CTRL. Pruebas: EditMode ReglaCubiertaPruebas, PlayMode CubiertaYRodadaPruebas.

## Carpetas permitidas
Core/, Personaje/, IA/, UI/, Tests/, docs de Tony.

## Bitácora
- Hecha: Q cubrirse, CTRL rodar. EditMode ReglaCubiertaPruebas 3/3 (+ProgresoAyudaPruebas), PlayMode CubiertaYRodadaPruebas 2/2, captura docs/capturas/T-421-1.png. ControladorAPiePruebas (4) y VidaAPiePruebas.Restaurar (1) fallan por pruebas viejas de primera persona (cámara local), no por T-421.
