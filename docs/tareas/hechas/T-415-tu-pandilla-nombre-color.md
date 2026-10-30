# T-415 Tu pandilla con nombre y color: en el HUD, el mapa, los aliados y los textos
Dueño: Tony | Rama: tony/T-415-tu-pandilla | Modelo: Sonnet 5.5, medium (datos y uso; la pantalla para escogerlos es de T-413) | MODO RÁPIDO (docs/plan-noche.md, paso 2)
Depende de: T-414 (SUBURBIOS, SKULLS, SNAKES).

## Objetivo
Tu pandilla deja de llamarse "TUYA" y de ser siempre naranja: tiene un nombre (por defecto "LOS DE SUBURBIOS") y un color de 4 (NARANJA, TURQUESA,
AMARILLO, AZUL), guardados en tu perfil. Ese nombre y ese color salen en el HUD de zona, en el mapa, en tus aliados y en los avisos. La creación de
personaje (T-413) solo pondrá los dos campos para escogerlos.

## Contexto
- Perfil: `Core/Jugador/IPerfilJugador.cs` (Nombre, Alias, Apariencia, Cambio) y `Progresion/Perfil/ServicioPerfil.cs` (clave `perfil`, en JugadorAPie).
  Dobles de prueba de `IPerfilJugador`: búscalos con grep en Tests/ (CamaEspejoPruebas, CambioEscenaPruebas, RecorridoMisionesPruebas,
  PrologoNochePruebas, RecorridoPrologoPruebas, ServicioPerfilPruebas, CreacionPersonajePruebas, ContratosMechaPruebas) y en Progresion/Pruebas/.
- Hoy fijos: `UI/Mapa/FormatoTerritorio.cs` (`ColorJugador` naranja y "TUYA"), `UI/Mapa/PantallaMapa.cs` (tus manzanas, tu punto y los aliados),
  `UI/Mapa/HudTerritorio.cs`, `UI/HudAPie/HudAliados.cs` y `IA/Aliados/AliadoAPie.cs` (`colorAliado` naranja en `_BaseColor`). Textos con "tu pandilla":
  `IA/Aliados/Reclutable.cs` ("TU PANDILLA ESTÁ LLENA") y el aviso de conquista.
- Paleta (D-tony-252, corrige D-tony-246): NARANJA `#FF801A`, TURQUESA `#1AE6E6`, AMARILLO `#FFD91A`, AZUL `#3380FF`. Nunca magenta (SKULLS),
  verde ácido (SNAKES) ni rojo `#FF3333` (enemigos del minimapa). El naranja de la UI (foco, objetivo) no cambia aunque tu pandilla sea azul.
- **Cambio de Core autorizado por Tony**: `IPerfilJugador` suma `NombrePandilla` y `ColorPandilla`; `Core/Jugador/ColoresPandilla.cs` (nuevo).
  Lo leen UI e IA, que no se ven entre sí: por eso la paleta va en Core.

## Carpetas permitidas
Core/Jugador/IPerfilJugador.cs, Core/Jugador/ColoresPandilla.cs (nuevo), Progresion/Perfil/, Progresion/Pruebas/ (dobles), UI/ (Mapa/, HudAPie/),
IA/Aliados/, Progresion/Prologo/ (Pandillas/ solo los avisos con el nombre, Editor/CreadorCiudadPandillas.cs), Escenas/Ciudad_Prologo.unity (solo con código),
Tests/ (los dobles de IPerfilJugador y las 2 pruebas nuevas), docs de Tony. No tocar: Radio/, Mundo/, UI/Creacion/ (la creación vieja del mecha).

## Qué hacer
1. **Core**: `IPerfilJugador` suma `string NombrePandilla { get; }` (nunca vacío) e `int ColorPandilla { get; }` (0 a 3); `Cambio` avisa también
   cuando cambian. `ColoresPandilla` (estático): `Cantidad` (4), `Color Color(int)`, `string Nombre(int)` ("NARANJA"…), `Predeterminado` (0),
   `NombrePredeterminado` ("LOS DE SUBURBIOS") y `string Limpiar(string)`: mayúsculas, solo letras (con Ñ y acentos) y espacios, espacios dobles a uno,
   de 3 a 16 caracteres; si queda corto, el predeterminado; si es largo, se corta a 16. Un índice fuera de rango da el predeterminado.
2. **ServicioPerfil**: guarda `nombrePandilla` y `colorPandilla` dentro de `perfil` (campos opcionales: un perfil viejo sin ellos carga los
   predeterminados) y suma `FijarPandilla(string nombre, int color)` (pasa por `Limpiar`, dispara `Cambio`). Actualiza los dobles de prueba.
3. **Uso**: `FormatoTerritorio.NombreDe(IdPandilla.Jugador)` y `ColorDe(Jugador)` leen el perfil (`Registro<IPerfilJugador>`; sin perfil, los
   predeterminados). `HudTerritorio`, `PantallaMapa` (tus manzanas al color de tu pandilla, tus aliados en el mapa) y `HudAliados` se repintan con
   `Cambio`. `AliadoAPie` toma el color de tu pandilla para su ropa (en vez de `colorAliado`) y lo cambia con `Cambio`. Textos: "EL CHATO SE UNE A
   LOS DE SUBURBIOS" al reclutar y "EL TIANGUIS ES DE LOS DE SUBURBIOS" al conquistar (usa el nombre real; máximo 40 caracteres, si no cabe se corta).
4. Sin pantalla nueva: hasta T-413 la pandilla vale lo predeterminado. Las pruebas la cambian con `FijarPandilla`.

## Criterios de aceptación
- [ ] Con `FijarPandilla("los neón", 3)`, el perfil da "LOS NEÓN" y 3; el HUD de zona en tu zona dice "SUBURBIOS · LOS NEÓN · RESPETO n"; tus manzanas
      del mapa salen AZUL `#3380FF`; el CHATO reclutado tiene `_BaseColor` azul; al cambiar a AMARILLO todo se repinta sin recargar la escena.
- [ ] Guardar y cargar la partida conserva el nombre y el color; un `perfil` viejo sin esos campos carga "LOS DE SUBURBIOS" y NARANJA sin errores.
- [ ] `Limpiar`: "  los   neón " → "LOS NEÓN"; "ab" → "LOS DE SUBURBIOS"; "LA PANDILLA MÁS LARGA DEL MUNDO" → 16 caracteres; "x$%9y" → solo letras.
- [ ] Las pruebas viejas que usan dobles de `IPerfilJugador` compilan y siguen en verde (EditMode completo y PlayMode de Progresion, UI e IA).
- [ ] Captura revisada a ojo y corregida si se ve mal: docs/capturas/T-415-1.png (mapa con SUBURBIOS en azul y el CHATO azul junto a ti), en 1920x1080.

## Pruebas (solo estas 2 nuevas)
- EditMode `ColoresPandillaPruebas.LimpiarYPaleta`: los casos de `Limpiar` de arriba, los 4 colores y nombres, el índice fuera de rango, y que ningún
  color de la paleta se parece al magenta de SKULLS, al verde de SNAKES ni al rojo de enemigos (distancia RGB > 0.3; NARANJA contra el rojo da 0.32).
- PlayMode `TuPandillaPruebas.NombreYColorEnTodo` (InputTestFixture): Ciudad_Prologo en Libre con el CHATO reclutado; `FijarPandilla("LOS NEÓN", 3)`;
  W unos pasos en tu zona → el HUD dice LOS NEÓN; M abre el mapa (tus manzanas azules) y toma la captura; M lo cierra; el CHATO es azul; guarda,
  `FijarPandilla("OTROS", 0)`, carga → vuelve LOS NEÓN y AZUL.

## Al terminar
- Bitácora de una línea, fila de docs/estado-tony.md en "hecha" y merge --no-ff a tony/fase-g (el cierre del paso 2 lo hace T-418).
  Nunca force push; `git restore` a lo que Unity cambia solo.

## Sensación (Tony)
- ¿"LOS DE SUBURBIOS" te gusta de nombre por defecto? ¿Los 4 colores se distinguen bien de SKULLS y SNAKES en el mapa y en la calle de noche?

## Fuera de alcance
- La pantalla para escribir el nombre y escoger el color (T-413), el minimapa (T-409), más colores, emblemas o ropa de pandilla.
