# T-697 Pruebas de Vehiculos en verde
Dueño: Tony | Rama: tony/T-697-pruebas-vehiculos | Modelo: Sonnet 5.5, medium
Depende de: T-692

## Objetivo
Las 3 pruebas rojas de Vehiculos que venían de antes de la noche vuelven a pasar, sin tocar el comportamiento del carro.

## Causas (las 3 eran de la prueba, no del código)
- Bajar_ConMurosEnLosDosLados_NoPuedeBajar: desde T-657 el conductor también sale por atrás y desde el techo; la prueba solo tapaba los dos lados. Ahora tapa también atrás y el techo.
- Camara_PrimeraPersona_MiraHastaElLimiteYVuelveAlFrenteTrasLaEspera: la cámara del carro arranca en tercera persona (decisión vieja); la prueba suponía primera. Ahora pulsa C antes.
- Recorrido_DeLaCasaAlClandestino: (a) el piloto de prueba soltaba A y apretaba D en el mismo cuadro y el Input System dejaba la acción Girar sin control activo (D apretada pero lee 0); ahora suelta y aprieta en cuadros distintos. (b) Un puesto de implantes (x -23 a -21, z 3.8 a 5.6) quedó pegado a la ruta: el punto x = -24 de la ruta pasa más al sur (z 1.4).

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Vehiculos/

## Criterios de aceptación
- [x] Las 3 pruebas pasan (PlayMode, entrada simulada con InputTestFixture)
