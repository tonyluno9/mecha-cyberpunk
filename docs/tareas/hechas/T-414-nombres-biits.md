# T-414 Nombres nuevos del equipo y moneda Biits en todo lo que ve el jugador
Dueño: Tony | Rama: tony/T-414-nombres-biits | Modelo: Sonnet 5.5, medium (renombrado mecánico) | MODO RÁPIDO (docs/plan-noche.md, paso 2)
Depende de: T-407 (nombre CYBERFIX, GestorSonidos y la escena con `GTA_T407`). Es la primera del paso 2: T-415 a T-418 dependen de ella.

## Objetivo
Lo que el jugador lee usa los nombres del documento del juego (29-oct): SUBURBIOS, SKULLS, SNAKES, EL MERCADO, I+D+I, EL FOX, VIVACOIN,
MEGALOPOLIS, y el dinero son Biits con su propio símbolo (Ƀ). Los nombres internos no cambian.

## Tabla de nombres (la fuente de verdad; cópiala también a docs/diseno/pandillas.md)
| Antes | Ahora (en pantalla) | Dónde está hoy |
| --- | --- | --- |
| LA FOSA (tu zona) | SUBURBIOS | `Progresion/Datos/Territorio/DatosZona_fosa.asset`, `CreadorCiudadPandillas.T404` (línea `CrearZona("fosa", "LA FOSA"…)`) |
| LOS CALAVERAS | SKULLS (en frases: "LOS SKULLS"), magenta `#FF1ACC` | `UI/Mapa/FormatoTerritorio.NombreDe`, `DirectorPandillas` (TextoJefe y subtítulo "—Los Calaveras se creen dueños…"), `IA/Datos/DatosPandilleroAPie_Calaveras.asset` ("CALAVERA") y `_JefeCalaveras` ("JEFE CALAVERA"), `IA/Editor/CreadorPandilleroArmado.cs`, `CreadorCiudadPandillas.T404` (`nombresJefe`) |
| LAS VÍBORAS | SNAKES ("LOS SNAKES"), verde ácido `#73FF1A` | `FormatoTerritorio.NombreDe`, `DatosPandilleroAPie_Viboras` y `_JefeViboras`, `CreadorPandilleroArmado`, `nombresJefe` |
| Neotek | NOVA | Hoy no sale en ningún texto; la usa la historia (T-418) |
| La ciudad | MEGALOPOLIS | Título del mapa (nuevo, aquí) y el letrero de inicio (T-418) |
| Mercado negro | EL MERCADO ("VE AL MERCADO" en frases) | `Mundo/Datos/DatosLugar_MercadoNegro.asset` y `Mundo/Editor/CreadorLugares.cs` (letrero de la puerta), `Economia/Datos/DatosTienda_MercadoNegro.asset` y `Economia/Editor/CreadorDatosEconomia.cs` (título de la tienda), `UI/Editor/CreadorMostradorTienda.TextoLetrero`, `DirectorPandillas` ("EN EL MERCADO NEGRO VENDEN ESCOPETAS"), `UI/Datos/ConfigPantallaMisiones.cs` y su asset, `UI/Misiones/FormatoMisiones.cs`, y lo que T-407 haya puesto bajo `GTA_T407` |
| Clínica | I+D+I | `Mundo/Datos/DatosLugar_Clinica.asset` y `Mundo/Editor/CreadorMercadoNegroInterior.cs` (l. 311), `CreadorDatosEconomia` (tienda "Clínica"), `UI/Datos/ConfigPantallaClinica.cs` y su asset ("CLÍNICA DEL DOC", "CLÍNICA", "CLÍNICA · CERRADA"), `UI/Editor/CreadorMostradorClinica.cs`, `UI/Datos/ConfigHudAPie` (motivo "CLÍNICA") |
| Bar El Foco / Cantina El Chip | EL FOX | `Mundo/Datos/DatosLugar_Bar.asset` ("Bar El Foco") y `CreadorLugares.cs`; el modelo `Letrero_Cantina` (palabra horneada, `Mundo/Editor/VestirLetreros.cs` l. 163) |
| Casino (Casino Aurora) | VIVACOIN | `Mundo/Datos/DatosLugar_Casino.asset` y `CreadorLugares.cs`; el modelo `Letrero_Casino` (`VestirLetreros.cs` l. 164); `ConfigHudAPie` (motivo "CASINO") |
| Taller del Inventor | CIC | Hoy no existe; lo pone T-417 |
| Moneda ($) | Biits, símbolo **Ƀ** delante ("Ƀ340", "-Ƀ150", "+Ƀ40 · PANDILLERO") | `Economia/FormatoDinero.cs` (lo usan HUD, tienda, clínica, continuará, misiones), `IA/Aliados/Reclutable.cs` ("TE FALTA DINERO: $", "· $"), y los Tooltip que dicen "con $" |
EL TIANGUIS, NEÓN ALTO, DOÑA LUPE, EL CHATO, LA GÜERA y EL TOPO no cambian.

## Contexto
- Solo cambia el **texto visible** (D-tony-249): los ids (`fosa`, `tianguis`, `neon`), los enums (`IdPandilla.Calaveras`, `IdLugar.MercadoNegro`,
  `IdLugar.Clinica`…), los nombres de objetos (`Lugar_MercadoNegro`, `Clinica_Camilla`) y de assets se quedan: así no se rompen datos, guardados,
  pruebas que buscan objetos ni el código de Alan.
- Los letreros de las puertas los escribe `Mundo/Lugares/LetreroLugar.cs` con `DatosLugar.Nombre`: basta cambiar el asset (por código, con
  `SerializedObject`) y la constante del creador. **No regeneres Barrio.prefab** (tiene el arte de T-250).
- Los modelos `Letrero_Cantina` y `Letrero_Casino` traen la palabra vieja en la malla: se apagan en Ciudad_Prologo con un override de la instancia
  del barrio (no se borran) y en su lugar va un letrero de neón hecho por código ("EL FOX" cian, "VIVACOIN" naranja) bajo `GTA_T414` (D-tony-251).
- Moneda: la usan áreas que no se ven entre sí (IA no ve Economia), por eso el símbolo va en Core (D-tony-250). Los precios no cambian (1 $ = Ƀ1).
- Las escenas de prueba del mecha (`CreadorEscenaMisiones`, `CreadorEscenaEconomia`, `CreadorEscenaImplantes`, Pelea/, Mecha/) no se tocan: no están en
  los builds de CYBERFIX. Sus pruebas sí se ajustan si esperan "$".
- **Cambio de Core autorizado por Tony**: `Core/Economia/Moneda.cs` (nuevo) y los comentarios de `Core/Territorio/IdPandilla.cs` (SKULLS, SNAKES).

## Carpetas permitidas
Core/Economia/Moneda.cs (nuevo), Core/Territorio/IdPandilla.cs (solo comentarios), Economia/ (FormatoDinero.cs, Editor/CreadorDatosEconomia.cs, Datos/),
UI/ (Mapa/, HudAPie/, Tiendas/, Clinica/, Historia/, Misiones/, Datos/, Editor/), IA/ (Aliados/Reclutable.cs, Editor/CreadorPandilleroArmado.cs, Datos/),
Progresion/Prologo/ (Pandillas/, Editor/CreadorCiudadPandillas.cs), Progresion/Datos/Territorio/, Mundo/Editor/CreadorLugares.cs y CreadorMercadoNegroInterior.cs
(solo las constantes de nombre), Mundo/Datos/DatosLugar_*.asset (por código), Escenas/Ciudad_Prologo.unity (solo con código), Tests/ (solo para cambiar
"$" y los nombres viejos en lo que esperan las pruebas, y las 2 pruebas nuevas), Tests/PlayMode/UI/CapturaUI.cs (nuevo), docs/diseno/pandillas.md,
docs/diseno/ui-cyberfix.md, docs de Tony. No tocar: Radio/, Barrio.prefab, el arte de T-250, ProjectSettings/.

## Qué hacer
1. **Moneda** `Core/Economia/Moneda.cs` (estático): `const string Nombre = "BIITS"`, `Simbolo` y `Texto(int)` ("Ƀ1,250", "-Ƀ150", sin decimales,
   cultura invariante). `Economia/FormatoDinero.Texto` llama a `Moneda.Texto`. Antes de nada comprueba que la fuente integrada (`LegacyRuntime.ttf`)
   dibuja `Ƀ` (U+0243): `font.RequestCharactersInTexture("Ƀ")` y `font.HasCharacter('Ƀ')`, y míralo en la captura. Si no lo dibuja, prueba `₿` (U+20BF);
   si tampoco, `Texto` escribe "340 BIITS". Anota cuál quedó en D-tony-250. `Reclutable` usa `Moneda.Texto`. Cambia "con $" por "con Ƀ" en los Tooltip de las carpetas permitidas.
2. **Textos y datos** de la tabla: constantes de código, valores por defecto de los Config y sus assets (por código, con `SerializedObject`, en un
   método `CreadorCiudadPandillas.T414()` que se suma a `Todo()` y que también rehace los DatosZona, DatosLugar, DatosTienda e IA/Datos tocados).
   `FormatoTerritorio.NombreDe`: SKULLS y SNAKES ("TUYA" se queda hasta T-415). Motivos de dinero de `ConfigHudAPie`: VIVACOIN e I+D+I.
   Mapa (`PantallaMapa`): título "MEGALOPOLIS" arriba al centro (texto 28, turquesa). Si T-407 dejó letreros con "MERCADO NEGRO" bajo `GTA_T407`,
   cambia su constante en `T407()` y vuelve a correrlo.
3. **Letreros en el mundo**: `T414()` apaga en la escena las instancias de `Letrero_Cantina` y `Letrero_Casino` (override, sin borrar) y pone bajo
   `GTA_T414` dos letreros de neón con texto 3D (canvas en World Space con `Text` y material emisivo, o `TextMesh`), del tamaño del modelo que tapan:
   "EL FOX" en Lugar_Bar y "VIVACOIN" en Lugar_Casino. El letrero de la puerta del mercado ya dirá "El Mercado" y el de la clínica "I+D+I".
4. **Grep de comprobación** (y pégalo en la bitácora): en UI/, Progresion/Prologo/, Progresion/Territorio/, IA/, Economia/ y Personaje/, sin contar
   comentarios, `Tooltip`, `Debug.Log*`, `MenuItem`, Tests/ ni los creadores de escenas de prueba del mecha (CreadorEscenaMisiones, CreadorEscenaEconomia,
   CreadorEscenaImplantes y los locales de prueba de CreadorJugadorAPie), no queda ningún literal con LA FOSA, CALAVERA, VÍBORA, MERCADO NEGRO, Mercado negro, CLÍNICA, Clínica, El Foco, Casino Aurora
   ni `"$` / `" $`. Las pruebas viejas que esperaban esos textos se cambian al texto nuevo (con `Moneda.Texto(…)` en vez del literal cuando se pueda).
5. **Ayudante de capturas** `Tests/PlayMode/UI/CapturaUI.cs` (lo pedía T-408; nace aquí para que todo el paso 2 lo use): pone los canvas Overlay en
   Screen Space Camera, renderiza a 1920x1080 y a 2560x1440, guarda el PNG y deja todo como estaba. Las pruebas de otras carpetas lo usan agregando a su
   asmdef la referencia a `MechaCyberpunk.Tests.PlayMode.UI`.
6. Docs: la tabla de nombres en docs/diseno/pandillas.md (arriba) y SKULLS/SNAKES en vez de LOS CALAVERAS/LAS VÍBORAS en docs/diseno/ui-cyberfix.md.

## Criterios de aceptación
- [ ] `Moneda.Texto(1250)` = "Ƀ1,250" y `Moneda.Texto(-150)` = "-Ƀ150" (o la variante que quedó en D-tony-250); el HUD de dinero y la tienda la usan.
- [ ] El grep del paso 4 sale vacío (pegado en la bitácora).
- [ ] En Ciudad_Prologo: el letrero de la puerta del mercado dice "El Mercado", el de la clínica "I+D+I", el del bar "El Fox" y el del casino "VivaCoin";
      los modelos `Letrero_Cantina` y `Letrero_Casino` están apagados y siguen en Barrio.prefab (md5 de Barrio.prefab igual antes y después).
- [ ] El HUD de zona dice "SUBURBIOS · TUYA · RESPETO n" en tu zona y "EL TIANGUIS · SKULLS · RESPETO n" en el Tianguis; el mapa (M) dice MEGALOPOLIS
      y SUBURBIOS; la tienda del mercado se titula EL MERCADO y sus precios llevan Ƀ.
- [ ] EditMode completo en verde; PlayMode de UI, Progresion, Personaje e IA en verde (las de Mecha y Pelea solo se ajustan en el texto esperado).
- [ ] Capturas revisadas a ojo y corregidas si se ven mal: docs/capturas/T-414-1.png (calle con el letrero de El Fox y el HUD con Ƀ) y T-414-2.png
      (tienda de El Mercado), en 1920x1080.

## Pruebas (solo estas 2 nuevas)
- EditMode `NombresCyberfixPruebas.DatosYMoneda`: carga los DatosZona, DatosLugar (Bar, Casino, MercadoNegro, Clinica), DatosTienda_MercadoNegro,
  ConfigPantallaTienda_Base, ConfigPantallaClinica_Base, ConfigHudAPie_Base y los DatosPandilleroAPie de IA/Datos, y ninguno de sus textos contiene un
  nombre viejo; `Moneda.Texto` da "Ƀ0", "Ƀ340", "Ƀ1,250" y "-Ƀ150"; `FormatoTerritorio.NombreDe` da SKULLS y SNAKES.
- PlayMode `RecorridoNombresPruebas.RecorridoNombres` (InputTestFixture, en Tests/PlayMode/UI): Ciudad_Prologo con Ƀ400; el jugador aparece frente a la
  puerta de El Mercado (lee el letrero), E abre la tienda (título EL MERCADO, precio "Ƀ350" en la escopeta), Esc la cierra; lo pasa a la calle del
  Tianguis y W camina: el HUD de zona dice SKULLS; M abre el mapa (MEGALOPOLIS) y M lo cierra; frente a El Fox toma la captura 1. Toma la captura 2 en la tienda.

## Al terminar
- Bitácora de una línea en docs/bitacora/<fecha>-tony-T-414.md (con el grep y el símbolo elegido), fila de docs/estado-tony.md en "hecha" y merge
  --no-ff a tony/fase-g. El cierre del paso 2 (main, builds y guía) lo hace T-418. Nunca force push; `git restore` a lo que Unity cambia solo y no
  subas `Assets/InitTestScene*`.

## Sensación (Tony)
- ¿Se lee bien Ƀ en el HUD y en la tienda o prefieres otro símbolo? ¿"SKULLS" y "SNAKES" sin artículo suenan bien en el HUD? ¿Los letreros de El Fox
  y VivaCoin tapan bien a los modelos viejos?

## Fuera de alcance
- Tu pandilla con nombre y color (T-415), los profes y el CIC (T-417), la historia con Nova (T-418), los modelos nuevos de letreros (paso 4: E01, E02)
  y renombrar ids, enums o assets.
