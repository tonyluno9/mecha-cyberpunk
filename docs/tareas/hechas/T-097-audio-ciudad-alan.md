# T-097 Audio ambiental de la ciudad
Dueño: Alan | Rama: alan/T-097-audio-ciudad | Modelo: Sonnet 5.5, medium
Depende de: T-041 (reloj), T-042 (lugares).

## Objetivo
La ciudad suena: zumbido de neón, lluvia, tráfico lejano y música apagada de cada local, con volumen según dónde está el jugador.

## Qué hacer
1. No hay audios finales en el repo: `Mundo/Editor/CreadorAudio.cs` genera clips provisionales por código (ruido filtrado de lluvia, zumbido de 100 Hz, tráfico) en Assets/Arte/Audio/ y deja anotado que se cambian por finales.
2. `Mundo/Ambiente/AmbienteSonoro.cs`: capas con `AudioSource` que mezclan por franja del reloj y por `ZonaInterior.JugadorDentro` (adentro: música del local y ambiente amortiguado).
3. Volúmenes en `ConfigAudioCiudad` (Datos/), nada de números en el código.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/Arte/Audio/, Tests de Mundo.

## Criterios de aceptación
- [ ] Al entrar al bar sube la música y baja la lluvia, y al salir al revés (prueba con los volúmenes de las capas)
- [ ] Sin errores de consola; `bash herramientas/verificar.sh todo` pasa
- [ ] Límite honesto en la revisión: lo audible lo comprueba una persona
