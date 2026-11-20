# Primitivas de Unity en el build (T-1523)

Este archivo lo reescribe `PrimitivasT1523.Diagnostico` (Mundo/Editor) con los numeros reales; esta version la dejo antes de poder correr Unity.
Lista completa por objeto en `Logs/t1523-primitivas.txt`.

## Por que T-1501, T-1502 y T-1512 no se vieron en el build

- T-1502 y T-1512 solo cambiaban cubos llamados `Edificio` dentro de las afueras (`T530_Afueras`). `CalculoEdificiosT1502.Clasificar` devuelve `Ninguno` para cilindros, esferas, props y piezas chicas, y los cubos `Edificio` de barrio con kit, las torres, los muros y los locales con interior solo se anotaban. De los 39 "cubos cambiados por ED", T-1512 apago 17 por encimarse.
- Lectura de solo lectura del YAML de `Ciudad_Prologo.unity` (sin Unity): 4199 MeshFilter con malla incorporada (2400 cubos, 1092 quads, 484 esferas, 191 cilindros); 1689 siguen con render activo y activos en la jerarquia, sin contar lo que viene dentro de prefabs instanciados. La mayoria son decales planos (lineas blancas y amarillas, charcos, ventanas con luz, pantallas), focos y lentes, tragamonedas del casino (T930_Casino y T546_Interiores) y piezas de interiores. Quedan pocos cubos `Edificio` (12) de las afueras y de S2n_Calles.
- Los edificios de barrio son piezas del kit de fachadas (C20, C21, C01...), modelos reales pero cuadrados; no cuentan como primitivas.
- Ninguna tarea miraba las otras escenas del build (Inicio, Menu_Calles, CreacionPersonaje, Flashback). `Inicio.unity` arma su fondo de ciudad por codigo con cajas (`UI/Inicio/FondoCiudad3D.cs`, malla `Inicio_Caja`): es el "edificios de cubos" del menu.
- Geometria de runtime con `CreatePrimitive` o `new Mesh`: `FondoCiudad3D` (menu), `MaletinAsalto` (existe `Assets/Arte/Objetos/MA01_Maletin.prefab`), `DineroEnPiso`, `MunicionEnPiso`, `VarianteVisual` (franjas), `ConstructorRecintoJefe` (barrera de energia), `LlamadoMecha` (marca), `HuecosBala`, `SensacionDisparo`, `PistolaAPie` (marcas), proyectiles y escudos (efectos), `CieloTexturizado` (domo), `VistaHologramaArma` y `VistaHologramaCuerpo` (UI). Ninguna crea la ciudad. No se cambiaron en esta tarea: son efectos, pickups y UI.
- Los render de los cubos se apagan, no se borran: eso no revive en el build.

## Antes / despues

| Categoria | Antes | Despues |
| --- | --- | --- |
| Edificio (cubos de edificio o casa) | | |
| EdificioConKit | | |
| Prop (cajas, postes, barriles, mesas) | | |
| DecalPlano (se deja) | | |
| LuzRotulo (se deja) | | |
| LosaPlana (se deja) | | |
| MuroPerimetral (T-1524) | | |
| MuroOtro | | |
| EstructuraInterior (se deja) | | |
| PendienteArte (tragamonedas, TR01/TR02) | | |
| **Por cambiar (Edificio + EdificioConKit + Prop)** | | |
| Flotantes o sobre 60 m | | |

Orden: `PrimitivasT1523.Diagnostico` (linea base en `Logs/t1523-antes.txt`), `PrimitivasT1523.Aplicar`, `PrimitivasT1523.Diagnostico`.

## Segunda vuelta (runtime y menu)

- `FondoCiudad3D` (Inicio): edificios, carros, vehiculos aereos, antenas y pasos elevados salen de prefabs serializados (`ModelosFondoInicioT1523.Aplicar` los llena; `CreadorEscenaInicio` tambien). Cada bloque se escala al hueco y, si es muy alto, se apilan copias del modelo. Se quitan colliders, scripts y luces del modelo. Si una lista queda vacia, esa pieza usa la caja de antes. Se dejan como cajas emisivas los anuncios de neon, las tiras de neon, faros y pilotos, las rayas de la calle y la losa de la calle (luz y rotulo, no edificios). La baliza roja ya no es cubo, solo su halo.
- `Menu_Calles` y `FondoCallesInicio` no crean primitivas: usan fotos y las calles reales de la ciudad.
- `MaletinAsalto`: ya usaba `Resources/MaletinAsalto` (T-1499, MA01); se quito el respaldo de cubos.
- `DineroEnPiso` y `MunicionEnPiso`: no hay modelos de billetes, fajos, cargadores ni cajas de balas en Assets/Arte (solo el material W01_Cargador). Usan prefabs de `Personaje/Resources` que crea `ModelosPickupsT1523.Aplicar` (dinero = maletin MA01, municion = CB03_CajasMetal_a, ambos a ~0.3 m). Si falta el recurso queda la primitiva como respaldo.
- `VarianteVisual`: las franjas eran cubos pegados a los huesos; ya no se crean (queda el brillo de la luz puntual y el material de la variante).
- `ConstructorRecintoJefe`: la barrera de energia es un cubo translucido con `Mat_BarreraEnergiaNova` que se apaga al terminar; se deja (efecto, no edificio).
