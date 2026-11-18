# T-1357: Puerto

Rama: tony/T-1357-puerto. Plano: docs/plan-zonas-18nov.md. Script: `MechaCyberpunk.Arte.Editor.ColocarT1357Puerto.Aplicar` y `.Fotos` (Assets/Arte/Editor/).
Pruebas: Assets/_Proyecto/Tests/EditMode/Mundo/PuertoT1357Pruebas.cs (acomodo puro).

## Hecho (sin correr Unity: escrito contra el código real; falta compilar y correr Aplicar)
- Acomodo puro: 25 bloques de contenedores (2 de largo x 4 filas, escalonados 1..4, pasillos 3.5-3.6 m), 4 calles de 12 m (CalleZona "Puerto") por U5 y U8, en circuito.
- Patio de concreto, muelle y bordillo, calles de asfalto con rayas, charcos, postes con cables (4 luces), reflectores y neón (6 luces), barriles C16_Tambo.
- Trepar: PuntoTrepar escalera de 2.6 m del suelo a cada bloque y entre filas; pallets junto a la fila 0. Sin visual de escalera (solo el punto).
- Mar del este por reflexión a CreadorMarT1351.CrearPlanoMar (si no existe queda Hueco_Mar_T1351).

## Pendiente
- Compilar, correr Aplicar y Fotos; revisar escalas (se ajustan por bounds: contenedor 12.19 m, grúa 42 m, barco 130 m, bodegas 32/28 m).
- Las referencias K15/A15/M12 no estaban en el worktree: el acomodo sale del plano y del encargo.
