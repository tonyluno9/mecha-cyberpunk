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
