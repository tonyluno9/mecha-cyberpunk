# T-095 Grafitis en muros y cortinas
Dueño: Alan | Rama: alan/T-095-grafitis | Modelo: Sonnet 5.5, medium
Depende de: T-081 (fachadas), T-094 (cortinas del mercado).

## Objetivo
Los muros bajos y las cortinas llevan grafitis de colores con aire mexicano cyberpunk, repartidos sobre todo en la zona pobre (Manzanas 1 y 4) y en el callejón, sin copiar marcas reales.

## Qué hacer
1. `Mundo/Editor/VestirGrafitis.cs` genera con código 6 a 8 texturas de grafiti (formas, tags de letras de bloque, calaveras estilizadas, estrellas) con alfa, en Mundo/Texturas/, y un material URP/Lit transparente `Grafiti_*`.
2. Coloca unos 40 decales (cuads finos con la textura, sin collider) en muros de edificios de la zona pobre, en el callejón de T-088 y en cortinas, a 1.2 a 2.8 m de altura, sin tapar ventanas ni letreros.
3. Semilla fija: regenerar deja lo mismo.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Tests de Mundo, docs/capturas/.

## Criterios de aceptación
- [ ] Entre 30 y 60 decales; ninguno con collider ni dentro de un edificio (prueba)
- [ ] Triángulos del barrio suben menos de 5 %
- [ ] Capturas de la zona pobre y del callejón, revisadas: docs/capturas/T-095-1..2.png
- [ ] `bash herramientas/verificar.sh todo` pasa
