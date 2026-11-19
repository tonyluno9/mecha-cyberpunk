# T-1440 Cerro A13: ladera en terrazas

Script: Assets/Arte/Editor/ZonaCerroT1440.cs (raíz del prefab: T1440_Ladera). Sin verificar en Unity (escrito sin Library).
Orden: ZonaCerroT1440.Aplicar (después de T1385 ... T1416 y de AereasT1414, antes de T-1441), ZonaCerroT1440.Validar (Logs/t1440-validar.txt), ZonaCerroT1440.Fotos (corrida aparte; docs/capturas/_vista19b/cerro-a13-t1440.png y cerro-k13-t1440.png).
- Pisos extra (terrazas): sobre las casas de la ladera (x < -358) se apilan de 1 a 3 pisos (KitS2c.Casa de un piso, C03_Fachada) con retiro de 2.2 m al frente y 1.5 m por piso; la cantidad crece hacia el oeste (cuesta arriba) con ruido por manzana. Cada piso lleva peldaños de concreto (alza 0.2 m) sobre la terraza o, si no caben, una escalera de servicio PuntoTrepar. No se mueve ningún collider viejo: la azotea de abajo queda como terraza y el parkour sigue (de la terraza al piso nuevo hay peldaños o escalera).
- Escaleras largas entre la pierna 3 (x = -409) y la pierna 2 (x = -366): peldaños reales (alza menor de 0.2 m), barandal CE03, lámparas emisivas y muros CC08 a los lados. Solo en los pasillos z donde el terreno no entierra la escalera; las casas del pasillo se apagan (reversible, T1440_Ladera/Apagados).
- Postes de luz a lo largo del zigzag cada ~22 m alternando lados: 8 con Light real (los más cercanos a A13 y K13b), el resto con cabeza emisiva naranja y charco de luz falso (malla aditiva), cables entre ellos.
- Ciudad brillante abajo: 110 paneles de ventanas emisivas (T1363_VentanasLejanas) con pantallas magenta, turquesa y ámbar, a 110-520 m y bajo el Cerro, sin colliders.
- Cámara A13b: T1440_Ladera/CamaraA13 (+ Mira); K13b es la de T1416.
