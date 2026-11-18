# T-1356: Autopista elevada del borde norte

Rama: tony/T-1356-autopista (desde 9220518). Plan: docs/plan-zonas-18nov.md. Referencias: K18, A18, M12.

## Hecho (sin correr Unity: no había Library)
- `Assets/Arte/Editor/ColocarT1356Trazado.cs`: trazado puro (U6 a U9, pendiente, radio, columnas, luminarias).
- `Assets/Arte/Editor/ColocarT1356Autopista.cs`: `Aplicar()` y `Fotos()`; raíz `T1356_Autopista`.
- `Assets/_Proyecto/Tests/EditMode/Mundo/AutopistaT1356Pruebas.cs`.
- `docs/assets-faltan.md`: sección Autopista.

## Decisiones
- Túnel con radio 30 m (no 40): boca (-330,250) y salida (-360,185) mirando al sur exigen girar 90 grados en 30 m de x; con 40 m la salida quedaría 10 m corrida.
- Rampa norte: de (-107,201) a (-120,215) hay un enlace seco de 19 m (el radio de 40 se mide desde el enlace); de ahí curva R40, recta a z = 226.7 (no 232: con R40 no cabe) y S de R40 hasta (30,10,250). Pendiente 6.3 %.
- Tablero, curvas, rampas y túnel son barridos generados con MeshCollider continuo; AU01 (rectas) se coloca por bounds si existe; AU02/AU03 no se usan (radio 60 y pendiente propios). Parapeto con collider siempre; se ve solo si falta AU05.
- Luces reales: máximo 9 en luminarias + 3 en el túnel = 12. El resto, KitS2c.Halo.
- Calles: Principal, RampaNorte y Tunel (autopista:true); Este_x300 y ConectorU9 (autopista:false, banquetas:true). Zona "Autopista".

## Pendiente de verificar en Unity
Compilar, correr las pruebas, `Aplicar()` y `Fotos()`; las proporciones de las piezas (eje largo, pivote) se suponen según T-1353.
