# T-1488: manejar con peso

Rama `tony/T-1488-manejar`. Estado: código y pruebas escritos, sin compilar ni correr (el worktree no tiene Library). Falta correr el método de editor y las pruebas.

## Qué cambió

- **Perfiles por tipo** (`PerfilManejo`, tabla única): compacto, sedán, camioneta, deportivo, pesado y moto. Un `ConfigCarro_<Tipo>.asset` por tipo.
- **Frenos con peso**: `CarroViejo` ya no aplica la fuerza de las llantas a la altura del centro de masa. Se aplica entre el piso y el centro de masa según `factorCabeceo` (longitudinal) y `factorBalanceo` (lateral): al frenar la carrocería cabecea, al acelerar se sienta y en curva se inclina. `CalculoPeso` tiene las cuentas.
- **Derrape**: el freno de mano multiplica el giro (`giroFrenoMano`); `CalculoManejo.CorreccionDerrape` endereza el carro hacia donde va (contravolante asistido y recuperación al soltar el freno de mano; sin ayuda con el freno de mano puesto, en reversa o a menos de 3 m/s).
- **Suspensión**: rigidez por masa para conservar la altura de reposo (hundimiento 0.15 m), amortiguamiento por relación `zeta` de 0.38 a 0.5 y rebote 1.3 veces más duro al estirarse. Más un freno a la inclinación (`amortiguacionInclinacion`) para que no oscile sin fin.
- **Cámara** (`CamaraCarro`): distancia extra 3.5 m a 90 km/h, FOV +12° a 100 km/h (se restaura al bajar) y temblor por choque. El temblor es un `CinemachineImpulseSource` en el carro (`TemblorCarro`, lo crea `CarroViejo`; lo llama `DanioCarro.OnCollisionEnter` con la rapidez relativa: 0 hasta 3 m/s, fuerza 1.5 a 18 m/s). Un `CinemachineImpulseListener` solo funciona sobre una cámara virtual de Cinemachine y esta cámara se coloca por código, así que `CamaraCarro` lee los impulsos del `CinemachineImpulseManager` (canal 1) y los suma a su pose, que es lo que hace el listener. Se agregó `Unity.Cinemachine` al asmdef de Vehiculos.
- **Sonido del motor**: `SimuladorMarchas` (4 a 6 marchas según el tipo, `marchasSonido`): el tono sube dentro de cada marcha y cae al cambiar, con un golpe de embrague de 0.18 s. Con `marchasSonido` 0 vuelve al tono lineal de T-1475.
- **Subir, bajar y robar**: el tiempo de acomodarse a la puerta ahora es de 0.2 a 0.3 s (`ReglasEntradaCarro.SegundosMinimosAcercar` de 0.08 a 0.2; robo con conductor de 0.15 a 0.25 s). Ya se interpolaba posición y giro; solo se alargó. Al bajar ya se revisa una cápsula de persona (`CarroViejo.SalidaLibre`) y se sale junto a la puerta; no se tocó.
- **Motos**: la inclinación entra con la rapidez (0 bajo 2 m/s, completa desde 7 m/s) y con tope de 90 °/s; histéresis al quitar el caballete y el visual baja suave (antes daba un saltito al pasar por 1 m/s); `antivuelcoBajaVelocidad` 3 sube el torque antivuelco de la moto parada.

## Valores por tipo

| Tipo | Masa kg | Vmax m/s | Motor m/s² | Freno m/s² | Eje m | Ángulo parado / máx ° | Agarre lat. | Resp. giro | Zeta | Cabeceo al frenar ° | Marchas |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Compacto | 900 | 15 | 7.0 | 16.7 | 2.4 | 34 / 12 | 10 | 9 | 0.38 | 4.1 | 4 |
| Sedán | 1100 | 18 | 8.0 | 18.2 | 2.7 | 32 / 10 | 11 | 8 | 0.42 | 3.9 | 5 |
| Camioneta | 2000 | 17 | 5.0 | 12.0 | 3.1 | 30 / 8 | 9 | 6.5 | 0.45 | 3.1 | 5 |
| Deportivo | 1300 | 28 | 12.0 | 21.5 | 2.6 | 30 / 8.5 | 14 | 11 | 0.38 | 3.4 | 6 |
| Pesado | 8000 | 14 | 3.0 | 7.5 | 5.0 | 24 / 6 | 6 | 4 | 0.50 | 1.2 | 6 |
| Moto | 230 | 26 | 7.8 | 27.8 | 1.4 | 40 / 16 | 11 | 10 | 0.50 | 3.0 | 5 |

La moto de reparto (V07) conserva 18 m/s y 1250 N. Los demás campos (freno de mano, rodadura, reversa, volante, balanceo, giro con freno de mano, contravolante, antivuelco, amortiguación) están en `PerfilManejo.Para`.

Modelo a tipo (`CrearPerfilesManejoT1488.TipoPorModelo`, lo que no está es sedán): V01a, V14, V21 compacto; V01b, V12 deportivo; V03, V04, V06, V15 camioneta; V16, V17, V18 pesado; V02, V05, V09, V10, V11, V13, V19, V20, V22, V23, V24 sedán; motos (MT01, MT02, C19, V07, V08) moto. V09, V10, V11, V19, V20, V22, V23 y V24 no tienen nombre ni tipo conocido: son sedán por defecto; se cambia la tabla y se vuelve a correr el método.

El tipo pesado (masa 8000) activa el sonido y claxon de pesado de T-1475 (`GrupoSonidoVehiculo.Detectar` usa masa de 2500 o más).

## Métodos de editor (en orden, Unity cerrado y en modo batch)

1. `MechaCyberpunk.Vehiculos.Editor.CrearPerfilesManejoT1488.Crear`: crea `ConfigCarro_Compacto/Sedan/Camioneta/Deportivo/Pesado.asset` (copia de `ConfigCarro_Viejo`), actualiza los de moto, asigna la config a cada `Carro_*.prefab` (los `CarroTrafico_*` heredan) y ajusta `ConfigCamaraCarro_Viejo`. Escribe `Logs/t1488-perfiles.txt`. Idempotente.

`ConfigCarro_Viejo` y `CarroViejo.prefab` no cambian de valores, así que las pruebas que los usan (IA de tráfico T-986, T-532) siguen igual. Los campos nuevos de `ConfigCarro` valen 0 o 1 por defecto (sin cabeceo ni ayuda de derrape). Los de `ConfigCamaraCarro` (FOV, temblor) sí traen valores activos por defecto.

## Pruebas

- EditMode: `Tests/EditMode/Vehiculos/ManejoPesoT1488Pruebas.cs` (peso, perfiles, derrape, suspensión simulada, marchas, temblor, FOV, moto estable). Se ajustó `CarrosPorPiezasT742Pruebas.Prefab_ConservaLaFisicaYElManejo`: ahora acepta cualquier `ConfigCarro_*`.
- PlayMode: `Tests/PlayMode/Vehiculos/PesoCarroT1488PlayPruebas.cs` (cabeceo al frenar y asentamiento en V02, moto parada vertical). La primera se ignora si no se corrió el método de editor.

## Qué probar a mano

1. Conducir un compacto, un deportivo, una camioneta y un camión (V16 a V18): se nota la diferencia de arranque, tope y giro.
2. Frenar fuerte a 60 km/h: la nariz baja y la carrocería rebota una o dos veces y se asienta.
3. Freno de mano con giro: el carro se atraviesa; soltarlo y contravolantear o no tocar nada: se endereza solo.
4. Chocar contra un muro a distintas velocidades: temblor proporcional; con un roce, nada.
5. A 100 km/h la cámara se aleja y el FOV se abre; al bajar vuelve al FOV normal.
6. Subir, bajar y robar un carro del tráfico: sin saltos, sin quedar dentro de un collider (mirar el robo con conductor).
7. Moto: parada no se cae, a baja velocidad no se inclina ni se sacude, en curva se inclina suave; al empezar a rodar no da saltito.

## Supuestos y pendientes

- Nada compilado ni corrido: hay que correr `bash herramientas/verificar.sh todo` y ajustar. Los valores de cabeceo y rebote salen de cuentas; hay que mirarlos en Unity.
- Los `.meta` de los scripts nuevos se escribieron a mano con GUID nuevo.
- Con cabeceo, los carros de la IA y del tráfico robado también cabecean (misma física).
- Animaciones que faltan: sección "Manejar, 19-nov (T-1488)" de `docs/animaciones-faltan.md`.
