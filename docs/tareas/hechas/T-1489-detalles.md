# T-1489: detalles de sensación a pie

Rama: tony/T-1489-detalles. Hecho sin Unity: no está compilado ni probado todavía (se verifica al integrar).

## Qué cambió
- **Polvo al caer** (`Personaje/Jugador/EfectosPieJugador.cs`, regla en `ReglaEfectosPie.cs`): un ParticleSystem de polvo en los pies. Caída mínima 1 m (6 partículas) hasta 6 m (28). El sonido de aterrizaje ya existía (`JUG_ATERRIZAJE`, caída > 1.2 m).
- **Salpicadura en charcos**: un ParticleSystem de gotas (10 al caminar, 16 al correr) en cada paso que cae sobre un charco, más el sonido `PAS_MOJADO` si el paso no era ya mojado (el catálogo no tiene un chapoteo propio). Los charcos son quads sin collider, capa 4 (Water), nombre `Charco*`: se buscan al cargar escena y cada 20 s, y se prueba el pie contra el quad en coordenadas locales (radio 0.4 de 0.5, altura máx. 0.6 m).
- **Temblor al recibir daño**: `TemblorCamara.Preparar/Generar` (API de T-1485, canal 1) con fuerza 0.4 a 1.5 según el daño (1 a 40 puntos) y la dirección del golpe. Solo daño al jugador a pie (objetivo null) a menos de 3 m.
- **Indicador de dirección**: `IndicadorDanio` y `ReglaIndicadorDanio` ya apuntaban bien (ángulo respecto al forward de la cámara, 0 enfrente, 90 derecha) y se desvanecen en 1 s; no se tocaron, se agregaron pruebas.
- **Viñeta roja con vida baja** (`UI/HudAPie/VinietaVidaBaja.cs`, `ReglaVinietaVida.cs`): imagen UI radial (textura por código) en un canvas propio, sin tocar Volumes ni perfiles de noche. Bajo 30 % de vida: opacidad 0.2 a 0.65 (más fuerte con menos vida), pulso senoidal de 0.8 a 2 Hz con valle de 35 %; entra y sale suave. La agrega `HudAPie`.
- Enganches mínimos: `ControladorAPie` (eventos `Aterrizo`, `PasoDado` y agrega `EfectosPieJugador` en Awake) y `HudAPie` (agrega la viñeta).

## Métodos de editor
Ninguno: todo se crea por código en runtime, no hay assets nuevos.

## Qué probar
- EditMode: `DetallesT1489Pruebas` (Tests/EditMode/Personaje).
- A mano: saltar de un techo (polvo según altura), caminar y correr por los charcos de la calle (gotas y sonido), recibir balas (temblor, requiere el listener de T-1485 en la cámara; marca roja hacia la fuente), bajar de 30 % de vida (la viñeta pulsa) y curarse (se apaga).
- Si el polvo o las gotas se ven cuadrados, es el material por defecto de partículas (URP Particles/Unlit sin textura): pendiente de arte.
