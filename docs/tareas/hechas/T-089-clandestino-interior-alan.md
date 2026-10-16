# T-089 Clandestino por dentro con la arena
Dueño: Alan | Rama: alan/T-089-clandestino-interior | Modelo: Sonnet 5.5, medium
Depende de: T-020, T-043, T-083.

## Objetivo
Entrar al clandestino desde la calle sin carga y llegar a la jaula: vestíbulo, pasillo, gradas alrededor de la arena de T-020 y un bar chico.

## Qué hacer
1. `Mundo/Editor/CreadorClandestino.cs`: interior dentro de Lugar_Clandestino (abierto solo de noche) con gradas, luces y neón; la jaula de Arena/ queda donde está.
2. Público de bajo costo en las gradas; máximo 12 luces bajo `ZonaInterior`.
3. Los puntos de la arena (T-020) siguen publicados y sin tapar.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Arena/ (solo si hace falta), Tests de Mundo, docs/capturas/.

## Criterios de aceptación
- [ ] Entrar y llegar al borde de la jaula en < 20 s; puntos de aparición de la arena a ≥ 1 m de muros
- [ ] Rayos sin huecos; luces apagadas sin presencia
- [ ] Capturas: docs/capturas/T-089-1.png y T-089-2.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
La pelea (Pelea/ de Tony), apuestas.
