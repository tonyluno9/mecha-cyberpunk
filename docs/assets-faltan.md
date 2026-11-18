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
