# T-1415: Autopista K18, ramal curvo (ronda 7)

Rama: tony/T-1415-k18-curva. Escrito sin Unity: no compilado ni corrido.
Script: `MechaCyberpunk.Arte.Editor.ZonaAutopistaT1415`. Orden, cada una en su corrida: Aplicar, Validar, BuscarCamara (con el prefab ya aplicado), Fotos.
Ramal elevado en S (radio 60) que sale del principal en x cerca de -205 (en arista de la rejilla de barreras), sube de 9.98 a 12.5 m y entra a una segunda boca AU06 en un peñasco nuevo en (-316, 12.5, 281).
3 columnas AU04 gruesas (3.2 m) cada 26 m, faroles AU01 cada 30 m, barreras AU02 y parapetos con collider. Cerrado: reja DS01 y letreros OBRA pasada la bifurcación; sin tráfico.
Toca el principal solo para abrir su parapeto/barreras norte en la salida (parapeto norte rehecho con el mismo corte) y quita por nombre casas, postes, chatarra y torres del fondo que estorban (todo en Logs/t1415-aplicar.txt).
