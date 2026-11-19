# T-1474: sonidos de UI, HUD, tienda, interacción y consumibles

Rama `tony/T-1474-ui-interaccion`. Sin compilar ni correr pruebas en este worktree (no tiene Library): falta correrlas.

## Evento -> ID

| Evento | ID que pide el código | Dónde |
| --- | --- | --- |
| Mover selección en menús | UI_MOVER (ya sonaba) | Kit/BotonCyberfix, tienda, inventario, clínica, armas |
| Aceptar | UI_ACEPTAR (T-1471 -> UI_SELECCIONAR) | ya existía, sin cambio |
| Volver/cancelar | UI_CANCELAR (T-1471 -> UI_ATRAS) | ya existía, sin cambio |
| Sin dinero (tienda, guarida, negocios) | UI_ERROR (antes ECO_SIN_DINERO) | PantallaTienda, ServicioGuarida, ServicioNegocios |
| Botón desactivado | UI_ERROR (ya sonaba) | BotonCyberfix |
| Cambiar categoría del inventario | UI_PESTANA (antes UI_MOVER) | PantallaInventario.CambiarCategoria |
| Cambiar pestaña del teléfono | UI_PESTANA | PantallaTelefono.CambiarPestana |
| Abrir/cerrar teléfono | UI_MENU_ABRIR / UI_MENU_CERRAR | PantallaTelefono |
| Abrir/cerrar pausa | UI_MENU_ABRIR / UI_MENU_CERRAR | PantallaPausa |
| Abrir/cerrar inventario y mapa | UI_MAPA_ABRIR/CERRAR (T-1471 -> UI_MENU_*) | ya existía |
| Logo/título | UI_TITULO_INICIO (T-1471 -> UI_LOGO_INTRO) | ya existía |
| Objetivo nuevo / cumplido | UI_OBJETIVO_NUEVO / CUMPLIDO (T-1471 -> HUD_NOTIFICACION / HUD_LOGRO) | ya existía |
| Vida baja (una vez al cruzar el umbral, se rearma al recuperar) | HUD_VIDA_BAJA en 2D (antes JUG_VIDA_BAJA 3D) | VidaAPie |
| Comprar | ECO_COMPRA (T-1471 -> TIE_COMPRAR) | ya existía |
| Equipar / quitar de la rueda | TIE_EQUIPAR / TIE_DESEQUIPAR (antes UI_MOVER) | PantallaInventario.AlternarEquipo |
| Puertas | INT_PUERTA_MADERA / METAL / CRISTAL / REJA según nombre y forma (3D) | HojaPuerta + ReglaSonidoPuerta |
| Puerta del cuarto | INT_PUERTA_MADERA (antes MUN_PUERTA_CASA_*) | PuertaCuarto |
| Recoger dinero | INT_RECOGER_OBJETO (antes ECO_DINERO_RECOGER), 3D | DineroEnPiso |
| Recoger arma/munición | ECO_ARMA_RECOGER (T-1471 -> INT_RECOGER_OBJETO) | ya existía |
| Recoger maletín del asalto | INT_MALETIN_CERRAR, 3D | MaletinAsalto.Usar |
| Bebida energética | CON_LATA_ABRIR y CON_BEBER | ConsumiblesJugador + ReglaSonidoConsumible |
| Café | CON_BEBER | idem |
| Papitas | CON_PAPITAS_COMER | idem |
| Venda | CON_VENDA | idem |
| Batería de escudo (y otros) | CON_CURACION (por revisar a oído: es de estilo fantasía) | idem |

Regla de puertas: el nombre manda (reja, cristal, metal/cortina/portón, madera); si no dice nada, corrediza = cristal, con bisagra = madera, sube al dintel = metal.
Ahora suenan todas las hojas (antes solo las de bisagra); el límite de voces y distancia lo pone T-1471.

## Pruebas (EditMode, por correr)
- `ConsumiblesSonidoT1474Pruebas` (Tests/EditMode/Personaje), con ISonidos falso.
- `PuertasSonidoT1474Pruebas` (Tests/EditMode/Mundo).
- `InterfazSonidoT1474Pruebas` (Tests/EditMode/UI): constantes del inventario y IDs en el código fuente.
- SonidoUiT594 no se rompe: ColaAvisos, AvisoMensaje, PantallaSubtitulos y ServicioMensajes siguen con UI_TEXTO.
- Los .meta de los archivos nuevos los genera Unity al compilar (no hay Unity en este worktree).

## No encontró dónde ponerse (no existen en el juego)
- INT_TECLEO_HACKEO, INT_HACKEO_EXITO, INT_HACKEO_FALLO: no hay mecánica de hackeo.
- INT_INTERRUPTOR_LUZ: no hay interruptores de luz interactivos.
- INT_MALETIN_CERRAR solo en el maletín del asalto; no hay otras cajas o maletines.
- UI_PESTANA: solo inventario y teléfono tienen pestañas; mapa y pausa no.
- HUD_LOGRO por misión/logro: no existe evento de misión cumplida con sonido; solo el objetivo (UI_OBJETIVO_CUMPLIDO).
- HUD_VIDA_BAJA no se repite mientras siga bajo (solo al cruzar el umbral).
- CON_LATA_ABRIR y CON_BEBER suenan casi juntos (no hay retardo entre ambos); si se quiere, un retardo corto.
- Clínica y mejora de armas: SonidoSinDinero sigue como ECO_SIN_DINERO en sus Config*.asset (campo serializado); no se cambió el asset.
