# T-694 Asiento del carro
Dueño: Tony | Rama: tony/T-694-asiento-carro | Modelo: Sonnet 5.5, medium
Depende de: T-622, T-656

## Objetivo
Sentado en un carro, el jugador ya no queda hundido: la cabeza queda a la altura de la ventana (a un 22 % entre la línea de la carrocería y la caja más alta) y se ve en tercera persona.

## Qué cambió
- AnimacionCarro (Personaje/Jugador): al subir mide dónde queda la cabeza con Carro_Manejar y sube el modelo (hasta 1 m, nunca baja) para que quede en esa altura, con mínimos sobre la carrocería (0.2 m) y bajo el techo (0.25 m). Sirve para todos los carros sin tocar el asiento ni la cámara de cada prefab.
- La cabeza ya no se esconde siempre: solo cuando la cámara está metida en ella (primera persona, como en la moto).
- Pruebas: Tests/PlayMode/Personaje/AsientoCarroT694Pruebas (10 carros: viejo, V01a/b, V02, V03, V04, V05 que es la patrulla, V06, V09, V10; las motos son T-695). El asmdef de pruebas de Personaje referencia Vehiculos.

## Criterios de aceptación
- [x] En cada carro la cabeza queda sobre la línea de la carrocería, bajo el techo y a media ventana (PlayMode con el jugador real subiendo por E/Subir)
- [x] Capturas de lado, de cerca y desde atrás: docs/capturas/s1t-T-694-<carro>-lado|cerca|atras.png

## Nota para arte
Los cristales de los modelos V02 a V10 vienen pintados en la textura única (oscuros y opacos), así que desde afuera no se ve a la persona aunque la cabeza esté a la altura de la ventana. Para verla hace falta un material de cristal aparte, semitransparente.
