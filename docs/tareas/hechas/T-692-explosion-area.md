# T-692 Explosión de carro en área
Dueño: Tony | Rama: tony/T-692-explosion-area | Modelo: Sonnet 5.5, medium
Depende de: T-444, T-591

## Objetivo
La explosión de un carro daña todo lo de alrededor con caída por distancia (jugador, NPC, carros, patrullas), puede encadenar, y los carros solo explotan por daño acumulado real con aviso (humo, fuego, sonido).

## Causa de "explotar sin razón" (medida)
DanioCarro empujaba con AddExplosionForce(1400 x masa, Impulse): un cambio de velocidad de cientos de m/s. Los carros y motos salían volando y el choque resultante (relativeVelocity enorme) los destruía. Ahora el empuje es VelocityChange de 12 m/s en el centro con caída lineal.

## Qué hacer
1. Vehiculos/DanioCarro: empuje en m/s; choques con rapidez imposible (>45 m/s) no cuentan, tope de 30 % de la vida por choque, 1.5 s de gracia al nacer y 1.2 s tras un empujón de explosión; alarma (VEH_ALARMA_ROBO) al entrar en Fuego; la explosión también alcanza triggers de zona de daño; manda el mensaje AlExplotarVehiculo.
2. Patrulla: DanioCarro y una zona de daño trigger (Vehiculos/Editor/ColocarT692, aplicado a Patrulla.prefab); PatrullaMundo.Destruida deja de manejar al explotar.

## Carpetas permitidas
Vehiculos (DanioCarro, datos, editor), Trafico (PatrullaMundo y su prefab), Tests

## Criterios de aceptación
- [x] Persona/NPC cerca recibe más daño que a media distancia; lejos, nada
- [x] Carro sano al lado: recibe daño, no explota, no sale volando (altura < 3 m, < 14 m/s)
- [x] Vecino ya en Fuego: se encadena; patrulla sana recibe daño, patrulla dañada se encadena y se detiene
- [x] Impacto de 120 m/s no cuenta; carro sano quieto no explota solo
- [x] Humo, luego fuego con alarma y solo explota al llegar la vida a 0

## Pruebas automáticas
- EditMode ChoqueRealT692Pruebas; PlayMode ExplosionAreaT692Pruebas (8) y ExplosionCarroPruebas; captura docs/capturas/s1t-T-692-explosion-vecinos.png
- Vehiculos completo: 72/79 (las 3 rojas son las previas de T-688)
