# Reporte 18-nov, sesión B (Opus): mundo, tiendas, pantallas, carros y armas

Trabajo de la noche del 17 al 18 de noviembre en el worktree `.sesiones/s2`, puntos 12 a 17 de la sesión B de `docs/playtest-17nov.md`.
Tony pidió detenerse a las 06:55: lo terminado está en main y lo que falta se apunta abajo. No se hizo ningún build.

## Integrado en main (merge --no-ff y push)

| Tarea | Punto | Qué quedó | Pruebas | Capturas |
| --- | --- | --- | --- | --- |
| T-1312 | 12 | Ruedas de Blender que giran en V02, V03, V04, V06, V09, V10 y la moto C19, al tamaño de su hueco. Las variantes de color del tráfico tiñen la carrocería nueva. El importador viejo ya no rearma esos carros. | RuedasBlenderT1312 7/7; EditMode Vehiculos 198/198; carros en Personaje 23/23 | docs/capturas/t-1312/ |
| T-1313 | 13 | W05 subfusil, W06 revólver, W07 rifle de plasma y W08 lanzagranadas en piezas que se desarman en el banco de Tino. El tambor gira en cada tiro y da una vuelta al sacar el arma (en la rueda). | EditMode UI+PersonajeVisual 578/578; PlayMode de armas 26/26 | docs/capturas/t-1313/ |
| T-1314 | 14 | El Mercado con cursor (clic elige, segundo clic compra). Vende solo armas y balas con nombres claros ("Celdas de plasma", "Francotirador"), sin mejoras ni artículos del mecha. | EditMode UI 324/324; PlayMode de tiendas 40/40 (+2 ignoradas) | docs/capturas/t1314/ |
| T-1315 | 15 | Inventario (I) con categorías, marca RUEDA, botón para poner o quitar de la rueda y sección de implantes. Tecla B para consumir el objeto equipado, en el HUD y en el tutorial. Las 23 expendedoras venden (antes solo 3 tenían tienda). | EditMode completo 2636/2636; PlayMode del inventario y la rueda 12/12 | docs/capturas/t-1315/ |
| T-1316 | 16 | ARRESTADO y HAS MUERTO como M33/M34: el juego se ve detrás, panel con marco y la pantalla se cubre antes de mover al jugador. Pantalla de carga tipo GTA con L01-L06 (zoom, paneo lento y fundidos). | EditMode completo 2655/2655; PantallasT1316 3/3; Arresto 2/2; HasMuerto 1/1; Inicio 14/14 | docs/capturas/t-1316/ |
| T-1317 | 17 | Los 60 barriles explotan en cadena. Postes de noche con intensidad 70, alcance 12 m y cono de 80°. Se quitaron el letrero ENCARGOS de la casa y el letrero NOVA con su haz; la torre queda como estaba. | EditMode Mundo+Vehiculos 566/566; barriles en PlayMode 14/14 | docs/capturas/t-1317/ |
| T-1318 | 17 | Azoteas con textura de concreto con grano y grietas. Pretiles bajos (malla de 0.5 m, collider de 0.4 m) que se saltan, para tirarse del techo. No había paredes invisibles: el muro era el collider de 1 m del pretil. | EditMode completo 2663/2663; techos y escaleras en PlayMode 22/22 | docs/capturas/t-1318/ |
| T-1320 | 17 | Calle junto a la casa: plaza con kiosco (K09) abierta a la calle K01 y calle comercial (K10) con toldos, puestos, foquitos y neones. Calle de la armería: letrero ARMERÍA sobre el portón, puestos ordenados, foquitos y el carril a la puerta libre. | EditMode Mundo 377/377; TechosS2 y EscalerasT1101 al día | docs/capturas/t-1320/ |

Cada tarea tiene su archivo en `docs/tareas/hechas/` con el detalle y las decisiones.

## Lo que quedó sin hacer

- **Armería por dentro (T-1319, sin empezar, sin rama)**: falta voltear el mostrador para que las armas miren al jugador y armar el interior según `docs/referencias-interiores/entrada-armas-mercado.png`. El interior lo arma `Assets/Arte/Editor/ColocarT931Armero.cs` (raíz T931_Armero; vitrinas I17 frente al muro en x = -29.8). Con girar las vitrinas 180° y revisar con captura debería bastar.
- **K08 (cuadra) a medias en T-1320**: la plaza y la calle comercial le dan intención a la cuadra, pero no se hicieron el baldío con barda ni el taller al fondo de K08.
- **EditMode completo después de T-1320**: Tony lo detuvo antes de que terminara. Sí pasaron las de Mundo (377/377) y las de PlayMode de techos.

## Avisos

- **Error mío en el merge de T-1313** (ya corregido): main estaba sacada en la carpeta principal y el merge sin checkout (`commit-tree`) usó un árbol que no traía lo último de la sesión A (b2570bd). Así, main perdió esas 6 tareas por unos minutos; se corrigió con fb1a81c, que lleva el merge real. Desde entonces se comprueba antes de cada merge que la rama contiene origin/main.
- **El mecha ya no se surte en ningún lado**: el kit de reparación y las municiones del mecha no se venden en ninguna tienda (antes solo El Mercado). RecorridoEconomiaPruebas da el kit por código.
- **Pruebas que fallan según el orden**, ya fallaban en origin/main y pasan solas:
  - RuedaArmasT991, si antes corren InventarioPruebas o ExpendedorasMarcadoresT719.
  - EscalerasNuevas_CaminarDosSegundos…, que pasa sola 1/1.
  - CiudadCarrosPruebas: el V08 estacionado se atora al robarlo, y esta sí falla también en la base.
- **Al saltar un pretil** el jugador cruza 10 cm de malla (la malla es de 0.5 m y el collider de 0.4 m).
- `herramientas/verificar.sh` no sirve en un worktree con el Unity de otra sesión abierto. En su lugar se corrieron las mismas pruebas por ensamblado y por clase.


---

## Segunda parte (madrugada del 18-nov, sesión B): modelos nuevos al juego

Opus dirigió; los scripts los escribieron subagentes Sonnet (una tarea, una rama) y Opus compiló, corrió, revisó las capturas y corrigió.
Todo se hizo en el worktree `.sesiones/s2`, con un solo Unity en modo batch (ya cerrado). No se tocó ninguna escena de la ciudad
ni se corrió CreadorTodo. Nada se hizo con primitivos. Lo que falta está en docs/assets-faltan.md.

### Qué quedó (en main)

| Tarea | Qué | Dónde |
| --- | --- | --- |
| T-1340 | 33 props de zonas de Tripo (AR, AU, CC, CH, IN, LO, ML, PU, PZ) con material URP (color, normal, metal/suavidad), escala real medida contra el jugador, pivote en la base, Mesh LOD, collider ajustado (caja, malla, o base + tronco en árboles y postes) y un prefab cada uno | `Assets/Arte/Zonas/<zona>/<Nombre>.prefab` |
| T-1341 | Plazas según K09: `Plaza_Kiosco` (kiosco, 8 bancas mirando al kiosco, 4 laureles, 4 árboles de banqueta, 4 jardineras), `Plaza_Esquina` y `Plaza_Banqueta` | `Assets/Arte/Zonas/PZ/Plazas/` |
| T-1342 | Carros nuevos V12–V24 con ruedas que giran (el tráiler V18 con 8), puertas y cofre en bisagras, interior y motor, y cristal transparente URP | `Assets/_Proyecto/Vehiculos/Prefabs/Carro_V12..V24` |
| T-1343 | Carros viejos con su FBX de Blender: cristal transparente en V01a, V01b, V02–V06, V09, V10; cofre en V01a, V01b, V05. Motos V07 y V08 con cuerpo y ruedas de Blender; motos nuevas MT01 y MT02 | Mismos prefabs `Carro_*` + `Carro_MT01/MT02` |
| T-1344 | Puertas que se abren al subir, bajar y robar el carro, y se cierran solas; el cofre se abre cuando el carro se incendia. Al explotar, puertas y cofre salen volando (DanioCarro, T-1325) | `Vehiculos/Carro/PuertasCarro.cs` |
| T-1345 | Tráfico por zonas: V12, V13, V14 y V19 en toda la ciudad; tuneados V20/V21 en el deshuesadero; superdeportivos V22–V24 en el malecón y Neón Alto; V15 en Las Lomas; microbús, camión y tráiler en avenidas y puerto | `Trafico/Resources/ConfigCarrosZonas.asset`, `Trafico/Prefabs/Zonas/ZonaTrafico_*.prefab` |
| T-1346 | Módulos de Blender que llegaron a la 01:41: autopista (recta, curva 45°, rampa, columna, guardarraíl, túnel), escalera y barandales del cerro, reja con púas, muro con luz de Las Lomas, barandal, escalera, muelles y escollera del malecón, contenedor (4 colores) y bita | `Assets/Arte/Zonas/Modulos/` |

### Lo que tienen que hacer la sesión A o Tony (no se tocaron escenas)
- Colocar en el mapa las plazas, los props y los módulos. El piso de adoquín de las plazas no va en el prefab.
- Soltar un `ZonaTrafico_<zona>` por zona (Deshuesadero, Malecon, NeonAlto, LasLomas, Avenida, Puerto) y ajustar su tamaño
  (por defecto 80×20×80 m). Sin ellos, los carros nuevos de zona no salen; los generales (V12, V13, V14, V19) sí.
- Los módulos de autopista conservan el pivote de Blender: la recta AU01 tiene el pivote al centro (12 m de ancho en X, 8 m de largo en Z),
  y la curva y la rampa en su extremo. La captura `T-1346-autopista-armada.png` las une a ojo y no casan: hay que unirlas por sus bordes.

### Qué no quedó y por qué
- **NPCs NP01–NP06:** el chat de Blender todavía no los entrega con rig (revisado por última vez a las 02:10). Qué hacer al llegar: docs/assets-faltan.md.
- **V11, la camioneta de policía:** su FBX ya está en el proyecto, pero no hay `Carro_V11` y conectarla a la persecución es otra tarea.
- **La patrulla** sigue con la carrocería de Tripo (es de Trafico, se arma desde V05 en T-742).
- **Caballete del V07:** la zona del modelo viejo toca el 6 % del cuerpo nuevo; no se recortó para no comerse parte de la moto.
- **CC03, CH03, NV01 y V11 (Tripo):** solo hay .png, sin zip.
- MT01 y MT02 no entran al tráfico todavía.

### Decisiones al revisar las capturas
- CH04 es un farol de pared con brazo, no de piso: queda a 1.3 m con caja, no a 4 m.
- Las bancas PZ01 tienen el frente hacia −Z del modelo: `GiroBanca` = 180. En la banqueta, los árboles van cada 6 m porque la jardinera del AR01 mide 3 m.
- Tripo hace muy anchos y altos los pesados: se acortaron V16 a 5.5 m, V17 a 6.5, V18 a 12 y V19 a 5.0 (antes eran 3.8 m de ancho).
- V01a, V01b y V05 ya no usan la llanta de V09 ni el ajuste al hueco de T-1007: llevan las llantas que les puso Blender. Las pruebas
  de T-742, T-1007 y T-1331 ya solo revisan la patrulla en esa parte.
- Las piezas `Interior`, `Interior_Casco` y `Motor` de los carros nuevos se copian como piezas fijas: con el cristal transparente se ve
  el interior y, con el cofre abierto, el motor.

### Pruebas
- EditMode completo antes de los ajustes: 3037 pruebas, 12 fallas, todas de la estructura vieja de V01a/V01b/V05/V07 y de las variantes
  de color. Después de corregir, las 12 clases afectadas y las nuevas: 371 pruebas con 1 falla (el caballete del V07, ajustado); la clase de motos repetida: 28/28.
- PlayMode de vehículos (PuertasCarroT1344, MontableCarro, RobarCarro, RuedasBlenderT1312, ManejoMoto, DanioCarro, ExplosionCarro, RobarModelos): 51 pruebas, 47 pasan, 4 se saltan, 0 fallan.
- PlayMode de tráfico (AparecerTraficoT704, EspacioLibreT728, ChoqueTraficoT703, CierresCiudadT986): 12 de 13. Falla
  `AparecerTraficoT704Pruebas.UnCarroQueSeReubica_SeVuelveAPegarAlSueloNuevo`, que también falla con el V10 de antes (0.198 m en
  lugar de 0.4): ya fallaba en main.
- No se corrió el PlayMode completo ni builds (memoria).

### Capturas
Hojas de resumen: `docs/capturas/reporte-18nov-B-props.png`, `-plazas.png`, `-carros-nuevos.png`, `-carros-viejos-motos.png`, `-modulos.png`.
Una por modelo: `docs/capturas/T-1340-*.png` (prop junto al jugador), `T-1341-<plaza>-arriba/-ojos.png`, `T-1342-<carro>.png` y
`-abierto.png` (puertas y cofre abiertos), `T-1343-*.png`, `T-1346-*.png`.
