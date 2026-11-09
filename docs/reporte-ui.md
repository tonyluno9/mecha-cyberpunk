# Reporte de la interfaz con la guía H10

Todas las pantallas pedidas tienen aspecto propio sobre el HUD H10 (`docs/guia-estilo-ui.md`) y conservan su funcionalidad.

## Decisión de diseño

No se copiaron M20, M21, M22 ni las REF_* (reproducen las pantallas de otro juego). La composición, los marcos y la paleta son propios. De `docs/tareas-menus-09nov.md` se tomó solo la lista de funciones.

## Pantallas

| Tarea | Pantalla | Qué tiene | Captura |
| --- | --- | --- | --- |
| T-899 | Guía y menú de inicio | Logo LG24 sobre la ciudad real, panel compacto, última partida y consejo reales, aviso en coral | ui-inicio.png, ui-inicio-aviso.png |
| T-900 | Banco de Tino | Pestañas, categorías con íconos H32, cuadrícula con filo de rareza, arma en holograma 3D que gira, pieza que se separa y brilla con línea guía, antes → después, piezas H34 con costo, MEJORAR, escaneo y números que suben | ui-banco-*.png |
| T-901 | Clínica del Doc | PJ01 en holograma, líneas a las cuatro casillas, íconos H30 y H33, barrido de luz, ensamblado, compra real y silla | ui-clinica-*.png |
| T-902 | Pausa y mapa | Pausa con panel SITUACIÓN; mapa con leyenda y negocios | ui-pausa.png, ui-mapa.png |
| T-903 | Tienda del Mercado | COMPRAR y VENDER, lista con hover, detalle, precios en amarillo y coral si no alcanza | ui-tienda.png |
| T-904 | Inventario (nuevo, tecla I) | Categorías, cuadrícula con rareza y detalle; solo consulta | ui-inventario.png |
| T-905 | HUD en menús | El HUD se apaga mientras haya un menú abierto | — |

Las capturas están en `docs/capturas/`. Se revisó al menos una por pantalla y por estado principal.

## Pruebas

- Compilación sin errores. EditMode: 2256 de 2256.
- PlayMode de cada pantalla, en verde: inicio, banco, clínica y silla, pausa, mapa, tienda, inventario.
- Corrida completa del área UI (hecha en T-899 y T-900, antes de las últimas pantallas): 200 de 229, con 19 fallos, los mismos en las dos corridas y ninguno en una pantalla de esta tarea. Son de caminar, entrar con E, carro y teléfono.
- Comparación con `main` sin estos cambios, con un mismo filtro de 22 pruebas: fallan las mismas 4 en las dos (`Cic_AbreConE...`, dos de `TelefonoMensajes`, `HudH10Pruebas.Estrellas_*`). Las otras 15 no se compararon en una corrida completa de `main`, así que su origen no está confirmado.
- Ensamblado `Progresion.Implantes`: 30 de 40 antes de un ajuste. Fallan 9 de combate con pandilleros (`PandillaAPiePruebas`, `RecorridoImplantesPruebas`). El décimo era una prueba desactualizada desde T-896 y ya pasa.
- La corrida completa de PlayMode de todas las áreas no se hizo: hubo poca memoria y el sistema cortó una verificación.

## Decisiones

- Holograma de la clínica en turquesa, no rojo o naranja como decía el documento viejo; el amarillo marca el espacio elegido.
- Con cuatro armas, las categorías son una por tipo de arma y las pestañas son TODAS, MEJORABLES y AL MÁXIMO. La rareza sale del nivel real.
- Sinapsis no tiene arte propio: usa el ícono H32 de implantes.
- Velo de pausa al 93 % en vez del 82 % de la guía.
- El mapa lleva cuatro rectas alrededor de la vista, no un marco con corte.
- El inventario usa inicial de texto en categorías sin ícono (TODO y OTROS).

## Pendiente de Tony

- Revisar en el juego el texto "Precio de prueba" del Kit de reparación: viene de los datos de la tienda y parece de relleno.
- Decidir si quiere investigar los 9 fallos de pandilleros y las 15 pruebas sin comparar: tocan jugador y combate, fuera de esta tarea.

## Build

`Builds/Demo/MechaCyberpunk.exe` (1569 MB, 0 errores). Arrancó 25 s en modo sin gráficos sin excepciones en su log; no se jugó de principio a fin.
