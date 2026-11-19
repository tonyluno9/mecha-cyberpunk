# T-1416 Cerro, ronda 7: K13 con la ciudad a un lado

Script: Assets/Arte/Editor/ZonaCerroT1416.cs (raíz del prefab: T1416_Cerro). Sin verificar en Unity (escrito sin Library).
Orden: ZonaCerroT1416.Aplicar, ZonaCerroT1416.Validar (Logs/t1416-validar.txt), ZonaCerroT1416.Fotos (corrida aparte; docs/capturas/t-1416/cerro-k13.png; la búsqueda queda en Logs/t1416-busqueda.txt). Solo la búsqueda: ZonaCerroT1416.BuscarCamara.
- Tramo elegido: pierna 2 del zigzag (x = -360, sube al sur 5 %). Mirando al sur desde z = 152 la ladera baja al este y la ciudad cae a la IZQUIERDA (espejo de la referencia); casas del oeste y la calle quedan a la derecha. Las pierna 3 (sube al norte) se descartó: la ciudad queda a 75 grados del eje y fuera del cuadro.
- Paseo escalonado de concreto en el borde este (x de -354 a -352), 3 tramos entre las escaleras CE01 (z 150-124, 116-64, 56-20), peldaños de 3 m con alza de 0.15 m, al ras de la banqueta, collider igual a la malla, faldón hasta el terreno. Barandal de tubo (módulo CE02_BarandalTubo o tubo propio) del lado de la ciudad.
- Casas del lado este apagadas (reversible, T1416_Cerro/Apagados): las de la huella del paseo y las que cortan rayos de la cámara a la ciudad entre 6 y 60 m.
- 2 postes con sodio sobre el paseo, cables (a más de 4.5 m de la cámara), 2 carros viejos, 4 neones de bandera pegados a las fachadas del oeste (por rayo).
- Cámara K13b: K13bDesde y K13bHacia (constantes). Fotos() busca alrededor (x, z, rumbo, cabeceo, lado) con puntaje: ciudad iluminada (emisión o luz, a más de 80 m y abajo de la cámara) sin tapar en el lado de la ciudad, más casas y escalera en el otro; si encuentra mejor, usa esa y la anota en el log para copiarla a las constantes.
