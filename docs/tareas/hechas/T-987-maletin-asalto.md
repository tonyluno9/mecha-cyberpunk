# T-987: la camioneta de los asaltantes suelta un maletín de $10,000

Pedido de Tony (14-nov, Sesión A, punto 4). Carpetas: Assets/_Proyecto/Progresion/Prologo/Pandillas/ (AsaltoDinero, ReglaAsalto), Assets/_Proyecto/Progresion/ (prefab o
Resources del maletín si hace falta), Assets/_Proyecto/Tests/EditMode|PlayMode/Progresion/, docs/. Trafico/AsaltoMundo solo si es imprescindible.
No tocar ciudad, techos, casino ni pandillas de la otra sesión (las pandillas del barrio; AsaltoDinero es de la historia del inicio y sí entra).

## Antes
Al detener la camioneta (`Historia_Asalto_Detenido`) baja el asaltante a pelear; al vencerlo "LA VÍCTIMA TE DA $premio" (una parte de lo robado).

## Qué se pide
- Al detenerse la camioneta, los asaltantes **sueltan un maletín** junto a la puerta (visible, en el piso, con un brillo o marcador).
- Objetivo claro en el HUD: "RECOGE EL MALETÍN" con el marcador encima del maletín.
- Se recoge con **E** (contrato `IInteractuable` de Core, como las demás interacciones a pie; el jugador juega PJ01_Inter_Recoger si ya existe esa ruta).
- Da **$10,000** a la cartera (`ICartera.Abonar`) y avisa en el HUD ("+$10,000 · MALETÍN RECUPERADO" o similar).
- Al recogerlo termina el asalto y la historia sigue (Terminado). El asaltante sigue bajando y peleando, pero vencerlo ya no es necesario.
- Nunca se traba: si el maletín cae fuera del mundo o no se puede colocar, se pone en el piso junto al jugador.

## Hecho
- `ReglaAsalto`: la fase `Enfrentando` pasó a `Maletin`; `MaletinRecogido()` paga `DineroDelMaletin` ($10,000) una sola vez y termina; se quitó la recompensa por vencer al asaltante. Textos "RECOGE EL MALETÍN" / "BÁJATE Y RECOGE EL MALETÍN".
- `MaletinAsalto` (nuevo, Pandillas/): IInteractuable "Recoger maletín", modelo de primitivas con brillo y luz puntual, cae al piso por raycast.
- `AsaltoDinero`: al detenerse el carro suelta el maletín junto a la puerta (o junto al jugador si no hay piso), objetivo con marcador, E lo recoge (gesto Recoger, cartera, aviso "MALETÍN RECUPERADO · +$10,000", Terminado). Si el maletín se destruye o cae fuera, se repone junto al jugador. El asaltante sigue peleando pero ya no es necesario.
- asmdef Progresion.Prologo: referencia a MechaCyberpunk.Personaje.Visual (para GestoVisual.Recoger).
- Pruebas: EditMode ReglaAsaltoT728Pruebas (2368/2368 EditMode); PlayMode AsaltoDineroT728Pruebas (6) + HistoriaPandillasPruebas (2): 8/8. Recorridos demo (T-474, T-979) ahora recogen el maletín en vez de pelear.
- Capturas: docs/capturas/t987/ (maletin-en-el-piso, maletin-detalle, maletin-recuperado).
