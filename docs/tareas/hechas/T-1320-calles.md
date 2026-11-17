# T-1320 La calle junto a la casa (K08/K09/K10) y la calle de la armería

Origen: docs/playtest-17nov.md, punto 17 (sesión B). Hecho por Opus. Script: `Assets/Arte/Editor/ColocarT1320Calles.cs` (Aplicar, Fotos),
raíz de escena `T1320_Calles`, idempotente. No se movió la red de calles (el tráfico depende de ella).

## Hecho
- **Plaza (K09)**: el lote interior de la cuadra noreste de la casa se abre a la calle K01 quitando las cuatro casas del frente
  (S2c_CalleDorada Casa_12 a 15) con sus dos escaleras (T1101 CalleDorada_01 y _03) y la ruta de techo CalleDorada_04. Empedrado
  (textura generada), kiosco de cobre con foco, 6 árboles en jardinera, 4 bancas, puesto de tacos, de ciber y de implantes, mesas,
  4 faroles y foquitos del kiosco a los faroles. La escena de acoso del lote se queda.
- **Calle comercial (K10)**: tramo de la K01 de x -105 a -59 (sin tráfico): toldos de colores donde hay fachada y no había toldo,
  neones, 8 puestos a la orilla del arroyo alternando lados y foquitos en zigzag de fachada a fachada.
- **Calle de la armería**: letrero ARMERÍA (con tablero y luz naranja) sobre el portón del Mercado, puestos en la orilla oriente del
  callejón, foquitos (desde z = 16: el tianguis de la avenida sigue sin focos colgando, T-873), dos faroles, y el carril de T-504
  hacia la puerta libre (se apagó un tambo que lo estorbaba).
- K08 (cuadra): la calle comercial y la plaza son las cuadras "con intención"; no se hizo baldío ni taller nuevo (anotado en el reporte).
- `KitS2c.Guardar`/`Cargar` pasan a públicos (para la textura del empedrado).

## Pruebas
- EditMode `CallesT1320Pruebas` (plaza abierta y con kiosco, de la calle al kiosco no hay muros, todo lo puesto queda en su calle,
  letrero y carril libre): en EditMode Mundo 377/377.
- PlayMode techos y escaleras: TechosS2 y EscalerasTechosT1101 al día (solo cuentan accesos activos; mínimo de escaleras nuevas 12 → 10).
  `EscalerasNuevas_CaminarDosSegundosPorElTecho_NoAtraviesaElPiso` falló una vez en tanda y pasa sola (1/1): depende del orden.
- Capturas revisadas en docs/capturas/t-1320/ (antes-* y después: plaza-k09, plaza-dentro, plaza-k09-dia, calle-k10,
  calle-k10-oriente, armeria-calle, armeria-puerta, alto-plaza).

## Aprendido
- En un script de editor, la caja de un collider recién instanciado no se mueve hasta `Physics.SyncTransforms()`: medir con eso sin
  sincronizar mandaba las piezas al doble de lejos.
