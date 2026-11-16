# Enemigos nuevos del 16-nov (diseño de la Sesión B)

Modelos de Tripo en `imageassets/modelos3d/enemigos/` (identificados contra `imageassets/modelos-nuevos/*.png` y por el nombre de sus texturas):

| Zip original | Es | Nombre nuevo | Referencia |
| --- | --- | --- | --- |
| SIN_NOMBRE_A.zip | Jefe final de Nova (armadura blanca y azul, núcleo cian en el pecho, cañones en los brazos; texturas E14_*) | E14_JefeFinalNova.zip | E14_JefeFinalNova.png |
| SIN_NOMBRE_B.zip | Guardia pesado de Nova (texturas E12_*) | E12_GuardiaPesadoNova.zip | E12_GuardiaPesadoNova.png |
| SIN_NOMBRE_C.zip | Pandillero SNAKES (chamarra de piel de víbora con vivos verdes, tatuajes de víbora; texturas E10_*) | E10_PandilleroSnakes.zip | E10_PandilleroSnakes.png |
| E11_JefeSnakes.zip | Jefe SNAKES (gabardina verde, brazo cibernético con cuchilla) | (igual) | E11_JefeSnakes.png |

El dron E13 no llegó: no se hace.

Todos son Humanoid con los clips de PJ01 (ControladorBase + override de su papel). Todos son `PandilleroAPie` (la IA, la vida, el KO, los gritos y el mapa ya existen) con un componente de conducta encima cuando su pelea es distinta.

## Ganchos en PandilleroAPie (pocos y chicos, porque la Sesión A también toca ese archivo)
- `IConductaAPie` (IA/APie): si el objeto tiene una conducta y `Controla` es true, `Update` le pasa el turno después de `BuscarJugador()` y antes de las ramas de jefe/armado/puños. La conducta usa una API pública mínima del pandillero: `Jugador` (Transform), `JugadorAtacable`, `Mover(Vector3 velocidadPlana)` (gravedad y animador incluidos), `MirarHacia(Vector3 dir, float gradosPorSegundo)`, `DaniarJugador(float cantidad, Vector3 direccion)`, `Gritar`, `Visual`, `MostrarAviso(float progreso, Color color)` (el brillo que ya usa el jefe de Nova).
- `IFiltroDanioAPie` (IA/APie): `RecibirDanio` le pregunta antes de aplicar: devuelve la cantidad final y el `TipoBloqueo`. Sirve para el escudo del guardia pesado y el escudo de energía del jefe final.
- La lógica de cada pelea va en una clase pura `Regla*` (sin escena, con pruebas de EditMode). La conducta solo traduce la regla a movimiento y daño.

## 1. Pandillero SNAKES (E10) y la riña con los SKULLS
- Datos: `DatosPandilleroAPie_Viboras` (ya existe) con el visual nuevo E10.
- Riña: un grupo de 3 SNAKES y otro de 3 SKULLS se pelean entre ellos en la frontera de Neón Alto con el tianguis, en un lote o calle lateral fuera de la ruta de la demo.
- Conducta `ConductaRina`: mientras el jugador no se meta, cada uno busca al rival vivo más cercano de la otra pandilla (por `IdPandilla`), se acerca y le pega con su ritmo normal. La riña hace poco daño (35% de su golpe) para que dure ~1 min, y se oyen gritos.
- Si el jugador pega a uno o entra a 6 m, `Controla` pasa a false para los dos grupos: los dos bandos se le van encima, como cualquier grupo.
- Cuando un bando cae, los que quedan vuelven a su punto. La riña se rearma cuando el jugador está a más de 80 m y no la ve.

## 2. Jefe SNAKES (E11): mini jefe con cuchilla
- Vida 260; rápido (velocidad 4.2); 2 escoltas SNAKES. Vive en un lugar fijo de Neón Alto (lote o patio) fuera de la ruta. Al vencerlo, se registra como jefe vencido de la pandilla Viboras (como `JefePandilla`, `jefe:true`).
- Patrón (`ReglaJefeSnakes`):
  - Acecho: rodea al jugador a ~4 m, de lado (strafe), cambiando de sentido cada 1.5-2.5 s.
  - Tajos: a <= 2.2 m, aviso de 0.45 s (brillo verde), luego 3 tajos seguidos cada 0.25 s, de 10 de daño cada uno, en un arco frontal de 90° y radio 2.2 m. Rodar los esquiva. Después se recupera 0.9 s.
  - Estocada: de 5 a 9 m, aviso de 0.6 s (brillo verde intenso y grito "¡Sssss!"), luego embiste en línea recta a 12 m/s por 0.5 s y hace 22 de daño si pasa a <= 1.2 m. Después se recupera 1.4 s y queda castigable (x1.5 de daño recibido).
  - Alterna tajos y estocada; nunca hace dos estocadas seguidas.
  - Al 50% de vida grita "¡A mí, víboras!", llama una vez a 2 SNAKES de refuerzo (si hay `PandillaAPie`) y sus avisos bajan 25%.

## 3. Guardia pesado de Nova (E12): escudo, resistente de frente
- Vida 220; lento (velocidad 2.2); gira despacio (90°/s): rodearlo es la forma de ganarle. Va en pareja con el jefe final (escoltas) y uno en la entrada de la torre de Nova. Solo pelea si lo provocas (le pegas o le disparas) o si entras al recinto del jefe.
- Escudo (`IFiltroDanioAPie`):
  - Golpes y balas que llegan de frente se reducen 85% y devuelven `TipoBloqueo.Normal`, con chispa y sonido de metal. De frente quiere decir que el ángulo entre -Dirección y su frente es < 70°.
  - De lado o de espalda el daño entra completo.
  - El escudo absorbe hasta 150 de daño bruto. Al llegar a 0 se le cae por 5 s (aturdido 1.2 s y castigable x1.5) y luego se recupera completo.
- Ataques (`ReglaGuardiaPesado`):
  - Empujón con escudo: a <= 2 m, aviso 0.7 s, 18 de daño.
  - Cada 3 ataques, mazazo por arriba: aviso 1.1 s, 28 de daño en radio 2.5 m frente a él.
  - Se recupera 1.2 s después de cada ataque.

## 4. Jefe final de Nova (E14): pelea en 3 fases
- Lugar: el recinto al pie de la torre principal de Nova, al final de la calle de Nova (x≈125-134, z≈-43), fuera de la ruta de la demo. Es un círculo de ~14 m de radio con luz fría y 2 guardias pesados.
  - Al entrar se cierra la pelea: barrera de energía transparente en la entrada, y grito "Nadie entra a la torre de Nova."
  - Si el jugador muere o se aleja más de 40 m, el jefe vuelve a su punto con la vida llena.
- Vida 600 (la Sesión A puede estar cambiando al Capitán de Nova; este es otro enemigo). Velocidad 2.8.
- Barra de vida: no se toca el HUD. La fase se nota por gritos, por el color de su brillo y por el escudo.
- Fase 1, Asedio (100%-66%):
  - Camina hacia el jugador.
  - Puñetazo de área: a <= 3 m, aviso 0.9 s, 25 de daño en radio 3.5 m frente a él (arco 120°).
  - Cada 3 acciones, un cañonazo: un aro rojo en el piso, en la posición del jugador (radio 2.5 m), que explota a los 1.2 s con 30 de daño. Moverse o rodar lo esquiva.
- A 66%: grita "¡Escudo activado!" y prende el escudo de energía, una esfera cian translúcida alrededor del jefe.
- Fase 2, Escudo (66%-33%):
  - Escudo (`IFiltroDanioAPie`): absorbe el 100% del daño a la vida y devuelve `TipoBloqueo.Perfecto`. Tiene 140 puntos.
  - Cuando se rompe: grito "¡Sobrecarga!", queda aturdido 3 s y castigable x1.5. El escudo vuelve a los 10 s si sigue arriba de 33%.
  - Onda de choque: aviso 1.2 s (agachado, brillo cian), luego un aro que crece de 0 a 9 m en 0.8 s. Hace 20 de daño al jugador cuando el aro pasa por su distancia (banda de ±0.6 m), salvo que esté rodando (invulnerable). Lo alterna con el puñetazo de área y con dos cañonazos seguidos.
- Fase 3, Furia (<33%):
  - Sin escudo; 30% más rápido; avisos 20% más cortos.
  - Hace un combo de puñetazo de área seguido de onda de choque.
  - Lanza ráfagas de 3 cañonazos: uno en el jugador y dos a ±3 m adelante de su dirección de movimiento.
- Al vencerlo: grito final, evento `Vencido` y registro de jefe vencido de Nova. Se abre la barrera y se apagan los guardias que queden.
- Todo telegrafiado y esquivable. Nada del jugador cambia.

## Lugares (sin romper la ruta de la demo)
La ruta de la demo pasa por la casa, el Doc, el inventor, el tianguis y Neón Alto con el Hacker (ver `Tests/PlayMode/Demo/RecorridoDemoPruebas.cs`). Los grupos nuevos van a >= 30 m de su camino y de las puertas que usa. `RecorridoDemoPruebas` debe seguir pasando.
