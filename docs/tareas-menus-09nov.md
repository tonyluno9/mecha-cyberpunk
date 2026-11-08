# Tareas: menús con el look nuevo (8-nov) — inicio, mejora de armas, clínica de implantes

Referencias de estilo (de un juego comercial; tomar el estilo, no copiar nada): `imageassets/referencias/menus/REF_*.png`.
Mockups propios a seguir: `imageassets/referencias/menus/M20_MenuInicio.png`, `M21_MejoraArmas.png`, `M22_ClinicaImplantes.png`.
Gráficos: `imageassets/logo/LG20–LG23` (Tony elige uno; mientras, LG20), `imageassets/ui/menus/H30–H32`, hologramas H01–H04.
Paleta: rojo coral para líneas y títulos, cian para lo activo, fondo casi negro rojizo, colores de rareza. Letra Rajdhani (ya está).
Animaciones con DOTween. **Paso 0 en cada tarea:** cerrar Unity y Unity Hub, sin Unity.exe, borrar Temp/UnityLockfile (repetir si vuelve).

## C1. Menú de inicio (como M20)
- Panel vertical rojo translúcido a la izquierda con el logo y CONTINUAR / NUEVA PARTIDA / CARGAR / OPCIONES / CRÉDITOS / SALIR.
- De fondo, la ciudad real del juego: la cámara pasa lento por varias calles (K01, tianguis, Neón Alto, Nova), con corte cada ~8 s.
- Aviso tipo recuadro (ej. "La partida guardada está dañada") con el mismo estilo. Versión abajo.

## C2. Banco de Tino: mejora de armas (como M21)
- Pestañas, categorías, cuadrícula de armas con borde de rareza, panel de estadísticas antes → después, piezas con costo, botón MEJORAR.
- Al centro el MODELO 3D real del arma con shader de holograma cian que se gira con el mouse. Al elegir una pieza: la pieza se separa
  (vista explotada) y brilla; al mejorar, animación de escaneo que recorre el arma, la pieza vuelve a su lugar con un destello y los
  números suben animados.

## C3. Clínica del Doc: implantes (como M22)
- Al centro PJ01 (con PJ01_Implantes.fbx) como holograma rojo/naranja con líneas de luz, girando lento; líneas que salen a las casillas
  de CABEZA/OJOS, BRAZOS, PIERNAS, ESCUDO con los íconos H30.
- Al pasar sobre un implante, se ve en el holograma (la malla del implante aparece con un barrido de luz de abajo hacia arriba); al
  instalarlo, animación de ensamblado y el implante queda puesto. Detalle con nombre, nivel, descripción y costo.
- Debe quedar conectado a la compra real de implantes y a la silla del Doc.

## C4. Mismo estilo en lo demás
- Pausa, mapa grande (M), inventario/objetos y la tienda del Mercado con el mismo look (paneles, esquinas cortadas, rojo y cian).
