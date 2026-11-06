# T-677 Headshot
Dueño: Tony | Rama: tony/T-677-headshot | Modelo: Sonnet 5.5

## Objetivo
Daño por zona: la cabeza hace mucho más daño que el cuerpo.

## Decisión
- Multiplicador de la cabeza: x3 (ReglaZonaDanio.MultiplicadorCabeza). Pistola 25 -> 75.
- Cabeza = impacto a menos de 0.2 m del centro de la cabeza (hueso Head del Animator humanoide) o en el 15 % de arriba del collider. Ningún prefab tiene collider de cabeza, así que en la práctica manda el 15 % superior de la cápsula (se adapta a agachados).
- Aplica a toda bala del jugador que pegue a algo que no sea objeto inerte: pandilleros, aliados, civiles (PeatonMundo), muñecos. ImpactoBala.Cabeza avisa del headshot.

## Capturas
No hay captura útil: el tiro mata al muñeco y la prueba mide el daño y el aviso ImpactoBala.Cabeza.

## Pruebas
EditMode: ReglaZonaDanioPruebas. PlayMode: HeadshotT677Pruebas (muñeco, cabeza = x3 del pecho) y HeadshotPandilleroT677Pruebas (pandillero armado real).
