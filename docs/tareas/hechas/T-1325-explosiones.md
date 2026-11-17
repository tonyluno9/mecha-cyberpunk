# T-1325: bomba/granada y explosiones en cadena de carros

Origen: playtest de Tony (17-nov). Nota: docs/playtest-17nov-b.md no existía en la rama; se trabajó con el encargo del orquestador. No se tocó lo que "le gustó" (explosiones, mira del lanzagranadas, muertos tirados).

## Qué se hizo
- Core: contrato nuevo `IDetonable.Detonar(float retraso)` (Core/Danio). Une Personaje y Vehiculos sin referenciarlos.
- Vehiculos: `DanioCarro` implementa `IDetonable`. Detonar destroza el carro al instante o tras un retraso. Al explotar, los carros (IDetonable) dentro de su radio detonan con `BarrilExplosivo.RetrasoCadena` (cadena). Al explotar suelta como piezas con física las llantas (Rueda/Giro/Modelo) y cualquier hijo llamado Puerta*/Cofre*/Capo*; se borran a los 12 s. El carro queda quemado, con fuego y humo existentes (llamas de chatarra).
- Personaje: `ExplosionAPie` primero detona todo `IDetonable` en el radio (una granada destroza una patrulla de un impacto) y luego reparte daño; dentro de `FraccionLetal` (0.6, nuevo campo de `ConfigArmasEspeciales`, por defecto 0.6) del radio mata a toda persona que no sea el jugador ni un objeto inerte (policías, pandilleros). El jugador conserva su daño reducido.

## Aplicar
No hay escenas ni prefabs cambiados: no hay método Aplicar.

## Pruebas
PlayMode `Tests/PlayMode/Personaje/ExplosionesT1325Pruebas.cs`: granada junto a carro (destruido, policía con daño letal, vecino explota en cadena, lejano no), llantas sueltas como piezas, EsLetal. No corridas por mí (sin Library): las corre el orquestador.

## Decisiones
- Detonación vía contrato de Core en vez de referenciar Vehiculos desde Personaje.
- Los modelos por piezas (T-742) no tienen puertas (T-972) ni cofre separado: solo las llantas salen volando; la carrocería queda quemada.
- Radio letal como fracción configurable, no número fijo.

## Assets que faltan
- Un modelo de cofre y puertas por separado (y carrocería rota) para un "en pedazos" más completo.
