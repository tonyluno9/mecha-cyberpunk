# T-711 Balas enemigas en la ciudad real
Dueño: Tony (S1) | Rama: tony/T-711-balas-enemigas | Modelo: Sonnet 5.5
Depende de: T-676, T-709, T-710

## Objetivo
Comprobar en la escena Ciudad real (Ciudad_Prologo, no una escena vacía) que las balas de los SKULLS, los policías y el dron no atraviesan paredes, carros, cajas ni las casas nuevas del kit; y separar a los 2 policías que baja la patrulla (cada uno a una esquina distinta).

## Qué se hizo
- Prueba BalasCiudadT711Pruebas (PlayMode IA) con la escena real: (1) 10 casas del kit, 3 edificios, 10 carros y la caja del callejón: la línea de tiro de PistolaPandillero queda bloqueada con el obstáculo y libre sin él, y Disparar no hace daño; (2) el callejón de los SKULLS (el volumen y la pared sacados de la escena): SKULL, policía y dron con las casas nuevas en medio no disparan ni dañan, y con línea libre sí; (3) igual con el carro más grande de la ciudad. Un vigilante independiente revisa, cada vez que alguien dispara, que no haya nada sólido entre su boca y el pecho del jugador.
- Resultado: no se encontró ninguna bala que atraviese. Los carros de la ciudad tienen colliders bajos (carrocería 0.4 a 0.74 m, cabina hasta 1.37 m): un tiro a la altura del pecho cruza la cabina, y a las puntas del carro pasa por encima del cofre; es lo que ve la bala, no cambia.
- Policías: PandilleroAPie.BuscarCubierta ya no manda a todos al mismo punto. PuestosDeCubierta ordena los puestos (policías: las dos esquinas, la de su lado primero; los demás: centro y lados cada 1.3 m) y nadie toma un puesto a menos de 1.3 m de otro. El de la esquina asoma por su propio lado. La cubierta se evalúa con el pecho agachado (0.8 m).

## Criterios
- [x] Las balas de SKULLS, policías y dron chocan con casas del kit, carros y paredes en la Ciudad real
- [x] Con el obstáculo en medio no disparan ni dañan; con línea libre sí
- [x] Los 2 policías de la patrulla van a esquinas distintas (puestos a 2.2 m entre sí en la prueba)
- [x] Capturas docs/capturas/s1c-T-711-*.png (policias, callejon, carro)

## Pruebas
- EditMode IA: PuestosDeCubiertaT711Pruebas (5)
- PlayMode Progresion: BalasCiudadT711Pruebas (3, en Progresion porque carga la ciudad y deja objetos entre escenas), PoliciasT709Pruebas (3, con la regla de esquinas)

## Fuera de alcance
- Cambiar los colliders de los carros (son de Vehiculos)
