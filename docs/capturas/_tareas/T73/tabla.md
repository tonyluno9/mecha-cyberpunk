# T73: vida y daño por tipo (T-1707)

Cambio: multiplicador de cabeza x3 -> x2 (`ReglaZonaDanio.MultiplicadorCabeza`); francotirador x4 (`MultiplicadorCabezaSniper`). No se cambió ninguna vida (los .asset ya cumplían).

| Tipo | Vida | Tiros a la cabeza con pistola (25) antes (x3 = 75) | Después (x2 = 50) |
| --- | --- | --- | --- |
| Pandillero (Base, Calaveras, Víboras) | 60 | 1 | 2 |
| Policía a pie | 140 | 2 | 3 |
| Nova | 420 | 6 | 9 |
| Jefe Nova | 1200 | 16 | 24 |

Francotirador (daño al cuerpo 40/44/48/52 niveles 0-3; no mata al cuerpo a un pandillero de 60):

| Cabeza | Antes (x3) | Después (x4) | Mata pandillero (60) | Mata policía (140) |
| --- | --- | --- | --- | --- |
| Nivel 0-3 | 120-156 | 160-208 | sí, 1 tiro | sí, 1 tiro |

Nota: el revólver (55) a la cabeza hace 110 y mata al pandillero de uno (el encargo solo fija la pistola); el policía necesita 2 tiros de revólver a la cabeza.
Prueba: `DanioCabezaT73Pruebas` (EditMode).
