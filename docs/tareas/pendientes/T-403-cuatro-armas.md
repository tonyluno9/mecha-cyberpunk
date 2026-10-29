# T-403 Cuatro armas que se compran en el mercado negro y suben 3 niveles
Dueño: Tony (Mundo/ es de Alan; Tony lo autorizó) | Rama: tony/T-403-cuatro-armas | Modelo: Sonnet 5.5, medium | MODO RÁPIDO
Depende de: T-400. Mejor después de T-402 (así hay a quién dispararle en el build).

## Objetivo
Puños, pistola, escopeta y subfusil, con 1 a 4 y la rueda del ratón. En el mercado negro compras la escopeta, el subfusil y las
mejoras; cada arma sube a los niveles I, II y III, y cada nivel mejora su daño, su cargador y su recarga.

## Contexto
- Hoy: `Personaje/Combate/ArmaAPie.cs` (Punos, Pistola), `ArmasAPie.cs`, `PistolaAPie.cs`, `CargadorPistola.cs`, `DatosPistola.cs`.
- Se reutiliza el patrón de las piezas (D-tony-215): `Mecha/Piezas/DatosPieza.cs` hereda `Economia/Inventario/DatosObjeto.cs` y
  tiene `nivel` y bonos, un asset por pieza y nivel, y `CatalogoPiezas`. El inventario (`ServicioInventario`) ya se guarda.
- Tienda: `Economia/Tiendas/TiendaBasica.cs`, `Economia/Datos/DatosTienda_MercadoNegro.asset`, `UI/Prefabs/MostradorMercadoNegro.prefab`
  y `UI/Editor/CreadorMostradorTienda.cs`. El interior del mercado negro es de Alan (`Mundo/Editor/CreadorMercadoNegroInterior.cs`).
- Números en D-tony-214. Mercado negro abierto siempre: D-tony-216.

## Carpetas permitidas
Personaje/ (puede referenciar MechaCyberpunk.Economia: lo pide esta tarea), Economia/, UI/HudAPie/, UI/Tiendas/, UI/Editor/,
Mundo/Datos/DatosLugar_MercadoNegro.asset (solo con código), Progresion/Prologo/Editor/CreadorCiudadPandillas.cs,
Escenas/Ciudad_Prologo.unity (solo con código), Tests/EditMode/Personaje/, Tests/PlayMode/Personaje/, docs de Tony.
No tocar: Core/, Mecha/, Radio/.

## Qué hacer
1. `ArmaAPie`: agrega `Escopeta` y `Subfusil` al final. `Personaje/Datos/DatosArma.cs : DatosObjeto` (como `DatosPieza`): arma, nivel
   0 a 3, precio, daño por bala, perdigones, cargador, recarga (s), tiempo entre disparos (s), automática, dispersión (°) normal y
   apuntando, alcance (m). `CatalogoArmas` con `Buscar(arma, nivel)`. 16 assets con los números de D-tony-214 en `Personaje/Datos/Armas/`,
   generados por un creador. `TipoObjeto` agrega `Arma` al final.
2. `ArsenalJugador.cs` (Personaje): qué armas tienes y su nivel = la de nivel más alto del inventario. Puños y pistola nivel 0 desde el
   inicio (en T-406 la pistola se consigue en la historia; deja un campo `pistolaAlInicio`). Comprar el nivel n pide tener el n−1.
3. Generaliza `PistolaAPie` en un arma de fuego que lee `DatosArma`: escopeta = 8 perdigones en un cono; subfusil = mantener clic.
   Mismo rayo cámara→punto→boca de T-400. Visual de cubos distinto por arma en `ManoDer`. Los puños usan el daño y la recarga de su nivel.
4. Entrada: `ArmaEscopeta` (3), `ArmaSubfusil` (4) y `CambiarArma` (rueda), en el creador de la entrada. Un arma que no tienes no se elige.
5. HUD: arma, nivel ("ESCOPETA II") y balas/cargador.
6. Mercado negro: `DatosTienda_MercadoNegro` ofrece la escopeta, el subfusil y las 12 mejoras (`TipoOferta.Pieza`, sin cambiar Core); la
   pantalla de la tienda solo muestra la mejora siguiente de cada arma. `DatosLugar_MercadoNegro`: horario Siempre y sin cerrar en la demo.
   `CreadorCiudadPandillas.T403()` pone `MostradorMercadoNegro.prefab` dentro del interior del mercado negro (busca su punto de mostrador;
   si no hay, junto al muro del fondo, frente a la puerta) bajo `GTA_T403`.

## Pruebas (solo estas 2 nuevas)
- EditMode `CatalogoArmasPruebas.NivelesMejoranYBalance`: con los 16 assets reales, en cada arma el daño y el cargador no bajan y la
  recarga no sube de nivel en nivel; contra 60 de vida: pistola 0 en 3 tiros, escopeta 0 en 1 disparo a quemarropa, subfusil 0 en 5 balas.
- PlayMode `ArmasPruebas.ComprarYUsarEscopeta`: con $400, abre el mostrador con E, compra la escopeta (flechas y Enter simulados),
  sale, pulsa 3 → arma activa Escopeta; dispara a un `MunecoAPie` a 4 m → le quita ≥ 50 de vida.

## Al terminar
- Build: `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego` (ver T-400) → Builds/Juego/MechaCyberpunk.exe.
- docs/guia-para-jugar.md, sección "Juego de pandillas": 3 líneas de qué probar.
- Bitácora de una línea: docs/bitacora/<fecha>-tony-T-403.md. Fila de docs/estado-tony.md en "hecha". Merge a tony/fase-g.

## Sensación (Tony)
- ¿Cada arma se siente distinta? ¿La escopeta pega fuerte? ¿Vale la pena comprar una mejora (se nota el nivel II)? ¿Los precios?

## Fuera de alcance
- Munición que se compra (las balas de reserva son infinitas), armas en el piso (T-406 hace la de la historia), granadas.
