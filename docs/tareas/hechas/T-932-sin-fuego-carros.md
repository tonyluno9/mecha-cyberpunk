# T-932 Sin rastro de fuego en carros

Rama: tony/T-932-sin-fuego-carros (sale de T-931). Playtest 09-nov.

- Causa: el prefab TinyFlames del Particle Pack tiene sistemas hijos con emisión por distancia (rateOverDistance = 5); EfectosExplosion.Continuo solo ponía en cero la emisión por tiempo, así que un carro intacto dejaba fuego al avanzar (incluida la policía).
- Arreglo (Vehiculos/Efectos/EfectosExplosion.cs): todos los sistemas del humo y del fuego del carro quedan con rateOverTime y rateOverDistance en cero y sin ráfagas; el fuego va en espacio Local del carro (sin estela) y el humo sigue en World. DanioCarro no cambió. Los barriles (FuegoSuelo, ExplosionCarro) no se tocaron.
- Prueba PlayMode nueva Vehiculos/FuegoCarroT932Pruebas: carro intacto en marcha 2 s: 0 partículas en todos los sistemas hijos de fuego y humo; carro en Fuego: sistemas en espacio local, sale fuego y las partículas siguen al carro. Se comprobó que la primera falla si se quita el arreglo.
- Resultados: Vehiculos (FuegoCarroT932, DanioCarro, ExplosionCarro, ExplosionAreaT692) 18/18; Personaje (FuegoT691, EfectosCombateT745) 10/10; EditMode completo pasó (CalculoDanioCarro incluido).
