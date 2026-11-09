# T-939 Menos asaltos

Rama: tony/T-939-menos-asaltos (sale de T-938). Playtest 09-nov.

Qué dispara cada asalto o ataque en la calle (rastreo): el asalto de la historia (AsaltoDinero, ReglaAsalto, AsaltoMundo) ocurre una sola vez, al llegar al paso Asalto de la historia, y no tiene temporizador: no se tocó. Lo que sí se repite:

1. DirectorAtaques (Progresion/Prologo/Encargos): en juego libre, con una zona tuya conquistada, los SNAKES, SKULLS o NOVA atacan una zona al azar cada cierto tiempo. Espera = azar entre mínimo y máximo, dividida entre 1 + 0.35 por zona extra + 0.2 por nivel de calor, con piso de un cuarto del mínimo. Sin cambio de código del director; cambian los datos.
2. GestorRefuerzos / ReglaRefuerzos: con 3 SKULLS vencidos sale un carro de refuerzo con SKULLS armados, con enfriamiento entre carros.

Antes y después:
- DatosAtaquesZonas_Base: segundosMinimo 420 s (7 min) -> 2100 s (35 min, 5 veces); segundosMaximo 780 s (13 min) -> 3900 s (65 min). El valor por defecto del código igual.
- ReglaAtaques: el piso de la espera pasó de 1/4 a 1/2 del mínimo (FraccionPisoDelIntervalo): en el peor caso (muchas zonas y calor 3) antes bajaba a 105 s con el mínimo viejo; ahora 1050 s (17.5 min), nunca menos de 10 min.
- ReglaRefuerzos.SegundosEnfriamiento: 60 s -> 600 s (10 min; 5 veces serían 300 s, se tomó el mayor).

Pruebas: ReglaAtaquesPruebas (piso y asset nuevo), ReglaRefuerzosT714Pruebas (enfriamiento por defecto).
