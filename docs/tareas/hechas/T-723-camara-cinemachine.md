# T-723 Cámara con Cinemachine
Dueño: Tony (S1) | Rama: tony/T-723-camara-cinemachine | Modelo: Sonnet 5.5
Depende de: T-699 (cámara H10), T-682 (apuntar centrado), T-687 (apuntar desde el carro). Es tarea de Core para el paquete (Tony autorizó instalar Cinemachine).

## Objetivo
La cámara del jugador es de Cinemachine y se parece a CAM01_APie.png, CAM02_APieCerca.png y CAM03_Carro.png: a pie detrás y un poco arriba del hombro con el personaje al ~40 % del alto, en carro y moto detrás y arriba con el vehículo abajo. No atraviesa paredes y se acerca en cuartos y bajo techos.

## Qué se hizo
- `Packages/manifest.json`: `com.unity.cinemachine` 3.1.7 (trae `com.unity.splines`). Los asmdef de Personaje y de sus pruebas PlayMode referencian `Unity.Cinemachine`.
- `CamaraHombro` (Personaje/Movimiento) ya no coloca la cámara: al despertar pone un `CinemachineBrain` en la cámara del jugador y crea la vcam `VcamAPie` (hija del jugador) con `CinemachineThirdPersonFollow` (con su colisión contra muros) y `CinemachineDeoccluder`. Cada cuadro solo le da el giro y la inclinación (punto `PuntoCamara`), baja el punto de giro bajo techos y manda el FOV (apuntar y zoom del sniper). Es la única cámara del jugador a pie: con la cámara apagada (carro) o sin control (menús, cinemáticas) el Brain se apaga y no pelea con quien mueva la cámara.
- Los colliders del jugador llevan la etiqueta `Player` (no hay capas libres) para que Cinemachine los ignore.
- Valores (`ConfigCamaraHombro_Base`): punto de giro 1.55 m + brazo vertical 0.15 m, distancia 3.5 m, FOV 60, centrada (lateral 0, T-699/T-682). Apuntando: mismo encuadre y solo zoom (FOV 40). El zoom no cambia caminando ni corriendo. Al mirar hacia abajo la cámara se acerca hasta 40 % (CAM02).
- Carro y moto: `CamaraCarro` (Vehiculos) mira un punto `alturaMira` (1.0 m) sobre su foco, así el vehículo queda abajo como CAM03. Es el único cambio en Vehiculos (código y `ConfigCamaraCarro_Viejo`); su manejo, órbita y colisión no cambian. Esta cámara no se pasó a Cinemachine (decisión: la del carro ya tiene colisión propia y se probó en T-687).
- `AjustarCamaraT723` (editor) fija los valores; reemplaza a `AjustarCamaraH10T699`.

## Criterios
- [x] Cinemachine 3.x instalado en Packages/manifest.json
- [x] A pie: detrás y un poco arriba del hombro, personaje ~42 % del alto, centrado (s1d-T-723-calle, comparacion-calle)
- [x] En carro y moto: detrás y arriba, vehículo abajo (s1d-T-723-carro, moto, comparacion-carro)
- [x] El zoom solo cambia al apuntar
- [x] No atraviesa paredes; en el cuarto de la casa se acerca y se ve al personaje (s1d-T-723-cuarto)
- [x] Una sola cámara: CamaraHombro adaptado, sin dos cámaras peleando

## Pruebas
- EditMode Personaje: CamaraCinemachineT723EditPruebas
- PlayMode Personaje: CamaraCinemachineT723Pruebas (estructura, calle, zoom con W/Shift/clic derecho, pared, cuarto chico, sin control), CamaraEscenaT723Pruebas (Ciudad_Prologo: calle, cerca, apuntando, cuarto, carro y moto), CamaraParedPruebas y CamaraApuntarT682Pruebas actualizadas.
- `CamaraH10T699Pruebas` se retiró (medía los valores de T-699, ahora sustituidos).
