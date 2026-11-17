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
