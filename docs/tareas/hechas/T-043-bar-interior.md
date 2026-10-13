# T-043 Bar por dentro, sin pantalla de carga
Dueño: Alan | Rama: alan/T-043-bar-interior | Modelo: Sonnet 5.5, medium
Depende de: T-042 (puerta del bar). Paso 4: T-032 en main (`EventosLugares`, `ILugar.JugadorDentro`) y la parte B de T-040
(`IJugador` en la cámara libre).

## Objetivo
El bar tiene interior greybox dentro de la misma escena: entro por la puerta desde la calle, recorro el bar y salgo, sin
pantalla de carga ni tirón. El bar avisa cuando el jugador entra y sale, para que los sistemas de Tony (misiones ilegales de
noche, objetivos) lo sepan. Es la mitad del mundo del hito H2.

## Contexto
- Diseño: docs/diseno/ciudad.md (entras y sales en tiempo real, sin pantallas de carga; criterio: entro y salgo del bar sin
  pantalla de carga), docs/diseno/economia-misiones.md (las misiones ilegales salen en el bar de noche; el club sirve para ligar,
  de forma no explícita: aquí solo el espacio), docs/diseno/estetica-hud.md (neón, low-poly).
- Contrato: `ILugar.JugadorDentro`, `EventosLugares.PublicarEntro` y `PublicarSalio` (Core/Mundo/ en main).
  Los triggers detectan al jugador con `GetComponentInParent<IJugador>()` (comentario de `IJugador`); la cámara libre tiene
  CharacterController, así que los triggers la ven.

## Qué hacer

### Sin esperar a Tony
1. Interior del bar detrás de su puerta (con el creador de T-040), de unos 20 × 14 m y 4.5 m de alto: barra con bancos, mesas,
   un escenario pequeño o cabina de DJ, una puerta falsa a los baños, pantallas y tiras de neón. Muros gruesos con colliders,
   sin huecos; el interior no se ve desde la calle más que por la puerta.
2. Luces del interior (cálidas y de neón). `Mundo/Lugares/ZonaInterior.cs`: un trigger que cubre el interior; mientras no hay
   nadie dentro ni en el umbral, las luces del interior se apagan para ahorrar.
3. Si T-032 no está en main, `ZonaInterior` escribe en la consola "[Mundo] Entró al Bar" y "[Mundo] Salió del Bar".

### Cuando T-032 esté en main
4. `ZonaInterior` tiene el `ILugar` de su local: al entrar el jugador pone `JugadorDentro` en true y publica
   `EventosLugares.PublicarEntro(Bar)`; al salir, false y `PublicarSalio(Bar)`. Una vez por entrada y salida,
   aunque el jugador tenga varios colliders.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Tests/PlayMode/Mundo/, Assets/_Proyecto/Escenas/Ciudad.unity y
Assets/_Proyecto/Escenas/Pruebas_Alan.unity
No tocar: Core/, Arena/, las carpetas y escenas de Tony, ProjectSettings/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] PlayMode con entrada simulada a las 22:00 en Pruebas_Alan: desde la banqueta frente al bar, E y W llevan a la cámara al
      centro del interior en menos de 10 s; luego da la vuelta y sale a la calle
- [ ] En esa misma prueba: `SceneManager.sceneCount` sigue en 1, no llega ningún `sceneLoaded`, y ningún frame dura más de 100 ms
      mientras cruza la puerta (máximo de `Time.unscaledDeltaTime` medido)
- [ ] PlayMode: con T-032, `JugadorEntro(Bar)` llega una vez al entrar y `JugadorSalio(Bar)` una vez al salir, y
      `JugadorDentro` del bar es true dentro y false fuera; sin T-032, las dos líneas de la consola aparecen una vez
- [ ] PlayMode: con la puerta cerrada, 36 rayos horizontales a 1 m y a 3 m de alto desde el centro del interior chocan con un
      muro o la puerta a menos de 15 m (no hay huecos)
- [ ] PlayMode: con la cámara fuera y lejos del bar, las luces del interior están apagadas; dentro, prendidas
- [ ] PlayMode con T-032: con la cámara dentro del bar, el reloj avanzado a las 04:30 (bar cerrado) y la puerta cerrada, E en la
      puerta da `Abierta` y la cámara sale (regla de T-042)
- [ ] Capturas desde la entrada mirando hacia la barra y desde la barra mirando hacia la puerta: docs/capturas/T-043-1.png y -2.png
- [ ] Consola sin errores en 3 minutos entrando y saliendo

## Criterios de sensación (para un humano)
- Entrar al bar se siente continuo, sin cortes; el interior se siente como una cantina cyberpunk y no como una caja vacía.
- El tamaño sirve para caminar a pie entre mesas sin atorarse.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Mundo/): BarInteriorPruebas, con entrada simulada y el reloj movido con `AvanzarHasta`.

## Fuera de alcance
- Personajes, barman, misiones del bar, ligar (Tony, T-F7, y Alan, A-F3)
- Música y audio del bar (A-F4)
- Interiores de la casa y del clandestino (A-F3)
