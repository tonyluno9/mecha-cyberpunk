# Assets que faltan (playtest 17-nov-b)

El código ya está listo con un hueco para cada uno. Cuando llegue el asset se conecta en ese hueco; no se sustituyó nada con primitivos ni con otros modelos.

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| Camioneta de policía | 5 estrellas | `GestorPersecucion.prefabCamioneta` (hoy salen patrullas normales) |
| Nave/helicóptero que baja policías o enemigos con sogas | 5 estrellas y Nova | No se hizo nada (falta el modelo) |
| Sonido de radio de policía | Patrullas que llegan o vigilan | `ConfigPersecucion.IdSonidoRadio`, vacío. Al llegar: agregar el ID a CatalogoSonidos y a `ReglaMezclaSonidos` con sus pruebas |
| Sonido especial de llegada de Nova (más de 5 estrellas) | Aviso | `ConfigRespuestaNova.SonidoLlegada`; hoy usa `PAN_JEFE_ENTRADA` |
| Sonidos de enemigos nuevos (Nova, escudo), dron (vuelo y explosión), patrulla mejorada, otra explosión | Sonido | Sin hueco nuevo |
| Arma especial de Nova que dispare | Enemigos de Nova | No se hizo (falta el modelo) |
| Carros con cofre y puertas por separado (o carrocería rota) | Que la granada los deje "en pedazos" | `DanioCarro.LanzarPiezas` ya suelta hijos `Puerta*`, `Cofre*` y `Capo*` si existen; hoy solo salen las llantas |
| FBX con ruedas separadas para V07 y V08 | Ruedas que giren | V01a, V01b, V05 y la patrulla ya usan la rueda de V09 |
| Animaciones de NPC (platicar, gesticular, asustarse) | NPCs | No hay clips NPC_* en el proyecto; se usan `AcuerdoCompleto` y `Enojo` |
| Más NPCs de distintos tipos | Variedad | No se hizo |
| Parque con árboles (referencia K09) | Ciudad | El parque improvisado sigue igual, no se tocó |
| Texturas de techos y calles | Ciudad | Pedidas al chat de arte |
| Maletín del dinero (MA01 por Tripo) | Atraco | No se hizo |
| Lugares nuevos | Ciudad | Pedir referencia a Tony antes de armarlos |

## T-1337: mira del rifle de plasma (W07)

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| Pieza de mira propia en el modelo W07 (el FBX de Tripo es una sola malla; la "Mira" del banco es un corte de región) | Una mira visible en el arma | Sin hueco: hoy la mira es la línea de `MiraLaserPlasma` en `ArmasEspecialesAPie` |

## T-1357: Puerto

Cada pieza se busca por nombre; si no esta, ColocarT1357Puerto deja un Hueco_CODIGO vacio. Al 18-nov no existia ninguna en la rama.

| Codigo | Que | Origen |
| --- | --- | --- |
| PT01_Contenedor40, PT02_BitaAmarre, DS01_RejaPuas | Contenedor de 40, bita, reja | Blender (T-1353, ZonasModulos/Prefabs) |
| PU01_Grua, PU02_Barco, PU03_Bodega, PU04_Bodega, PU05_Montacargas, PU06_Pallets | Grua, carguero, bodegas, montacargas, pallets | Tripo (Assets/Arte/Zonas) |
| LO04_Caseta | Caseta de la entrada U5 | Solo si existe en Assets/Arte/Zonas |
| PluminaEntrada | Pluma de la entrada | Sin modelo, siempre hueco |
## T-1356: Autopista

El script `ColocarT1356Autopista.Aplicar` deja un `Hueco_<CODIGO>` (GameObject vacío, sin malla) cuando falta la pieza; la lista exacta de cada corrida queda en `Logs/t1356-autopista.txt`.
Al escribir la tarea no existían `Assets/Arte/ZonasModulos/Prefabs/` ni `Assets/Arte/Zonas/`, así que se esperan estos huecos hasta que lleguen las piezas.

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| AU01_AutopistaRecta (Blender) | Tablero recto z = 250 | Sin hueco: se ve el tablero generado |
| AU04_ColumnaAutopista (Blender) | Columnas cada 24 m | `Hueco_AU04` en `T1356_Autopista/Columnas` |
| AU05_Guardarrail (Blender) | Guardarraíl a los dos lados | `Hueco_AU05`; se ve el parapeto generado (con collider) |
| AU06_EntradaTunel (Blender) | Bocas del túnel | `Hueco_AU06` en `T1356_Autopista/Tunel` |
| AU01_Luminaria (Tripo) | Luminarias cada 30 m | `Hueco_AU01` en `T1356_Autopista/Luminarias` |
| T20, T21, T23 (kit de torres) | Fondo al norte | `Hueco_FondoTorres` si no están |
## T-1354: Malecón

Cada hueco es un GameObject vacío `Hueco_<CODIGO>` bajo `T1354_Malecon`; Logs/t1354-malecon.txt lista cuáles quedaron.

| Qué | Para qué | Hueco |
| --- | --- | --- |
| Prefabs de Blender de T-1353 (Assets/Arte/ZonasModulos/Prefabs): ML01_BarandalMalecon, ML02_EscaleraPlaya, ML03_MuelleRecto, ML04_MuelleEsquina, ML05_Escollera | Barandal, escaleras, muelle, escollera | `Hueco_ML01_BarandalMalecon`, etc. (solo si T-1353 no los creó) |
| Tripo (Assets/Arte/Zonas): AR03_Palmera, ML01_Farol, ML02_Lancha, ML03_PuestoMariscos, ML04_Faro, PZ01_Banca | Palmeras, faroles, lanchas, puesto, faro, bancas | `Hueco_AR03_Palmera`, etc. (esa carpeta no existe en esta rama) |
| Hieleras y bancos de plástico del puesto (K12) | Puesto de mariscos | `Hueco_Hielera`, `Hueco_BancoPlastico` (sin código de modelo) |
| Material de arena T04_Arena en Assets | Playa | Color liso `T1354_Arena` |
| Componente de runtime que gire el haz del faro | Haz giratorio (K12) | El Spot del faro queda fijo |
| CreadorMarT1351.CrearPlanoMar y CrearOrilla (T-1351) | Agua | `Hueco_Mar` y `Hueco_Orilla` |
## Colonia del Cerro (T-1355)

| Qué | Para qué | Hueco en el código |
| --- | --- | --- |
| CC01_CasaCerro, CC02_CasaCerro, CC03_CasaCerro (Tripo) | Casas del cerro | Sin hueco: mientras no estén se usan las casas del kit (KitS2c.Casa) |
| CC04_Tendedero, CC05_Lavadero (Tripo) | Azoteas | `Hueco_CC04_Tendedero`, `Hueco_CC05_Lavadero` bajo `T1355_Cerro/Huecos` |
| CE02_BarandalTubo, CE03_BarandalTuboEscalera (Blender, Assets/Arte/ZonasModulos/Prefabs) | Barandales de escaleras y plaza | `Hueco_CE02_BarandalTubo`, `Hueco_CE03_BarandalTuboEscalera` |
| CE01_EscaleraCerro (Blender) | Escaleras | Sin hueco: las escaleras se generan como malla de concreto |
| T05_ConcretoCerro.png (imageassets/calle) | Textura del terreno | No existe en el repo: Cerro_Terreno.mat usa Barrio_Concreto; si el archivo aparece, se copia al borrar el .mat y correr Aplicar |
