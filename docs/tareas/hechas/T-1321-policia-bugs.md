# T-1321: policía, bugs de persecución, apuntar y barriles

Rama: tony/T-1321-policia-bugs. Sin compilar ni correr pruebas (lo hace el orquestador). No existe docs/playtest-17nov-b.md en main; se trabajó con el encargo.

## Causas y arreglos

1. Persecución al inicio. Causa encontrada: `PatrullaMundo.LateUpdate` ponía en búsqueda (`ReglaPrimeraEstrella.Subir`) a cualquier baja de vida de la patrulla con el jugador a menos de 80 m. Una patrulla que chocaba sola (barril, poste) al aparecer daba la primera estrella. Arreglo: `ReglaCulpaPatrulla` (Trafico/Persecucion): no cuenta en los 4 s después de aparecer ni 1.5 s después de un choque con algo que no es el jugador (`PatrullaMundo.OnCollisionEnter` lo marca). Límite: sin atacante en `DanioCarro` (Vehiculos, de Alan) no se distingue una bala del jugador de una explosión ajena; el daño por explosión de un barril lejano sigue contando si el jugador está cerca.
2. Deslizarse al apuntar. Causa: en Policia.controller el estado `ApuntarPistola` era un solo clip (pose quieta). Arreglo: `PoliciaApuntarCaminarT1321.Aplicar` lo cambia por un BlendTree 1D sobre `Velocidad` (0 = PJ01_Pistola_Apuntar, 1.4 = PJ01_Pistola_Caminar, 3.6 = PJ01_Pistola_Correr; clips ya existentes). `PandilleroAPie` ahora manda al animador la velocidad real del CharacterController (`ReglaAnimacionPolicia.VelocidadVista`). `NpcClipsPJ01T1004.CambiarClips` ya no pisa un estado que es árbol.
3. Patrullas atoradas en barriles. Causa: el barril contaba como obstáculo en todos los carriles (`ObstaculoTotal`), la velocidad deseada bajaba a 0 y el detector de atasco solo mira cuando "quiere avanzar" (`velocidadDeseada` >= mínimo), así que nunca daba reversa. Arreglo: (a) el atasco usa la velocidad deseada antes del freno por obstáculo; (b) si solo barriles tapan los carriles ya no es muro: avanza despacio y se atora/empuja/reversa; (c) choque contra un barril a >= 6 m/s lo revienta, y la explosión daña la patrulla (el daño de choque de DanioCarro ya existía).

## Métodos Aplicar
`MechaCyberpunk.IA.Editor.PoliciaApuntarCaminarT1321.Aplicar` (idempotente; modifica Assets/Arte/Personajes/Policia/Policia.controller). No hay cambios de escena.

## Pruebas
EditMode: `PatrullaCulpaT1321Pruebas` (Tests/EditMode/Trafico), `PoliciaApuntarT1321Pruebas` (Tests/EditMode/IA; la última necesita haber corrido Aplicar). Sin PlayMode: la patrulla con barril necesita escena y física; se deja al verificador.

## Decisiones
- Se tocó Trafico, IA y un guard en Personaje/Visual/Editor porque la tarea lo pide.
- No se tocó Vehiculos; atribuir daño al jugador exigiría `Atacante` en `DanioCarro`.

## Assets que faltan
Ninguno. (Opcional: un clip propio de policía apuntando-caminando; hoy se reutiliza PJ01_Pistola_Caminar.)
