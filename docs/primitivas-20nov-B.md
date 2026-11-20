# Primitivas de Unity en el build (T-1523)

Generado por `PrimitivasT1523.Diagnostico` (Mundo/Editor). Lista completa en `Logs/t1523-primitivas.txt`.

## Por que T-1501, T-1502 y T-1512 no se vieron en el build

- T-1502 y T-1512 solo cambiaban cubos llamados `Edificio` dentro de las afueras (`T530_Afueras`). `CalculoEdificiosT1502.Clasificar` devuelve `Ninguno` para cilindros, esferas, props y piezas chicas, y los cubos `Edificio` de barrio con kit, las torres, los muros y los locales con interior solo se anotaban. Ese era el 39 de "39 cubos cambiados por ED", y de esos T-1512 apago 17 por encimarse.
- En la escena la mayoria de lo "basico" que queda son decales planos (Quad: lineas de calle, charcos, ventanas con luz, pantallas), luces (focos, lentes), rotulos y piezas de interiores; los edificios de barrio son piezas del kit de fachadas (C20, C21, ... modelos reales pero cuadrados) y no cubos.
- Ninguna de las tareas miraba las otras escenas del build. `Inicio.unity` arma su fondo de ciudad por codigo con cajas (`UI/Inicio/FondoCiudad3D.cs`, malla `Inicio_Caja`): es el "edificios de cubos" que se ve en el menu. Ver la tabla de abajo.
- Los scripts de runtime con `CreatePrimitive` crean efectos, marcadores y pickups, no la ciudad (lista abajo); el maletin del asalto y los fajos de dinero si son formas basicas visibles.
- Los render de los cubos se apagan, no se borran: eso no revive en el build. Si algo reaparecio, es de otra categoria de esta lista.

## Antes / despues (Ciudad_Prologo, MeshRenderer visibles con malla incorporada o caja procedural)

"Antes" es la primera corrida de `Diagnostico` o `Aplicar` (`Logs/t1523-antes.txt`; borrarlo reinicia la linea base). "Despues" es esta corrida.

| Categoria | Que se hace | Antes | Despues |
| --- | --- | --- | --- |
| Edificio | modelo ED/NV/P/LO/CC segun barrio | 39 | 11 |
| EdificioConKit | se apaga el render si el kit lo cubre | 8 | 0 |
| Prop | prop real equivalente o se apaga | 798 | 1 |
| DecalPlano | se deja (linea, charco, ventana, pantalla) | 992 | 992 |
| LuzRotulo | se deja (foco, lente, neon, rotulo) | 283 | 283 |
| LosaPlana | se deja (piso, banqueta, calle) | 263 | 263 |
| MuroPerimetral | T-1524 | - | 0 |
| MuroOtro | se anota (muro largo) | 25 | 25 |
| EstructuraInterior | se deja (paredes de interiores y locales) | 100 | 597 |
| PendienteArte | esperan TR01/TR02 (assets-faltan.md) | 432 | 432 |
| **Total visibles** | | 2940 | 2604 |
| **Por cambiar (Edificio + EdificioConKit + Prop)** | objetivo 0 | 845 | 12 |
| Flotantes o muy altos | asentar o apagar | - | 0 |

## Por barrio y tamano (solo las que faltan por cambiar)

| Barrio | <0.5 m | 0.5-2 m | 2-6 m | 6-15 m | >15 m | Total |
| --- | --- | --- | --- | --- | --- | --- |
| Mercado | 0 | 0 | 0 | 2 | 2 | 4 |
| NeonAlto | 0 | 0 | 0 | 1 | 0 | 1 |
| Suburbios | 0 | 0 | 0 | 0 | 5 | 5 |
| Tianguis | 0 | 0 | 0 | 1 | 1 | 2 |

## Zonas (raiz de la jerarquia) con mas primitivas por cambiar

- `Barrio`: 8
- `T530_Afueras`: 4

## Otras escenas del build (solo cuenta; no se cambian)

- `Assets/_Proyecto/Escenas/Flashback.unity`: 19 MeshRenderer visibles con malla incorporada
- `Assets/_Proyecto/Escenas/Inicio.unity`: 0 MeshRenderer visibles con malla incorporada
- `Assets/_Proyecto/Escenas/Menu_Calles.unity`: 2537 MeshRenderer visibles con malla incorporada
- `Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity`: 17 MeshRenderer visibles con malla incorporada
- `Inicio.unity` ademas crea cajas por codigo al arrancar (`FondoCiudad3D`): ese fondo es una decision de arte del menu, no se toco.

## Geometria creada por codigo en runtime

Busqueda de `CreatePrimitive` y `new Mesh(` en `Assets/_Proyecto` sin `Editor/`, `Tests/` ni `Pruebas/`. Ninguna crea edificios de la ciudad; las que si se ven en juego como objeto solido son el maletin del asalto (`MaletinAsalto`, existe `Assets/Arte/Objetos/MA01_Maletin.prefab`), los fajos y municion en el piso (`DineroEnPiso`, `MunicionEnPiso`) y el fondo del menu.

- `Assets/_Proyecto/Combate/Habilidades/DestelloCono.cs:92 Mesh malla = new Mesh { name = "DestelloCono" };`
- `Assets/_Proyecto/Combate/Habilidades/EscudoVisual.cs:12 GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);`
- `Assets/_Proyecto/Combate/Habilidades/ProyectilHabilidad.cs:38 GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);`
- `Assets/_Proyecto/IA/APie/ConstructorRecintoJefe.cs:49 GameObject barrera = GameObject.CreatePrimitive(PrimitiveType.Cube);`
- `Assets/_Proyecto/IA/APie/EscudoEnergia.cs:96 GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);`
- `Assets/_Proyecto/Mecha/Llamado/LlamadoMecha.cs:382 marca = GameObject.CreatePrimitive(PrimitiveType.Cylinder);`
- `Assets/_Proyecto/Mundo/Reloj/CieloTexturizado.cs:75 GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);`
- `Assets/_Proyecto/Personaje/Combate/ArmasEspecialesAPie.cs:102 GameObject bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);`
- `Assets/_Proyecto/Personaje/Combate/DineroEnPiso.cs:81 GameObject billete = GameObject.CreatePrimitive(PrimitiveType.Cube);`
- `Assets/_Proyecto/Personaje/Combate/HuecosBala.cs:282 GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);`
- `Assets/_Proyecto/Personaje/Combate/MunicionEnPiso.cs:105 GameObject bala = GameObject.CreatePrimitive(PrimitiveType.Capsule);`
- `Assets/_Proyecto/Personaje/Combate/PistolaAPie.cs:765 GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Cube);`
- `Assets/_Proyecto/Personaje/Combate/ProyectilAPie.cs:51 objeto = GameObject.CreatePrimitive(PrimitiveType.Sphere);`
- `Assets/_Proyecto/Personaje/Combate/SensacionDisparo.cs:295 GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);`
- `Assets/_Proyecto/Personaje/Habilidades/MisilAPie.cs:30 GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Capsule);`
- `Assets/_Proyecto/Personaje/Jugador/AnillosSuelo.cs:161 Mesh cuadro = new Mesh { name = "OndaPisadaCuadro" };`
- `Assets/_Proyecto/Progresion/Prologo/Pandillas/DirectorPandillas.cs:738 GameObject pieza = GameObject.CreatePrimitive(tipo);`
- `Assets/_Proyecto/UI/Armas/VistaHologramaArma.cs:501 GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);`
- `Assets/_Proyecto/UI/Armas/VistaHologramaArma.cs:530 GameObject disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);`
- `Assets/_Proyecto/UI/Clinica/VistaHologramaCuerpo.cs:426 GameObject disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);`
- `Assets/_Proyecto/UI/Inicio/FondoCiudad3D.cs:331 Mesh m = new Mesh { name = "Inicio_Caja" };`
- `Assets/_Proyecto/UI/Inicio/FondoCiudad3D.cs:442 Mesh m = Registrar(new Mesh { name = "Inicio_Halo" });`

## Flotantes o sobre 60 m (primeros 60)

