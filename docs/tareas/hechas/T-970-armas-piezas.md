# T-970: armas con sus piezas reales de Blender (B1)

Rama: tony/T-970-armas-piezas. Sin merge.

## Qué quedó
- `W02_Piezas.fbx`, `W03_Piezas.fbx`, `W04_Piezas.fbx` (en la carpeta de cada arma): sin animación, rig, cámaras ni luces, Read/Write apagado, escala de archivo.
  `tripo_mat_*` remapeado al material del arma y `Corte_Interior` a `Armas_CorteInterior.mat` (URP/Lit, gris 0.06, metálico 0.3, smoothness 0.25).
  No se extrajeron texturas; la captura se ve igual que en Blender.
- Prefabs W02/W03/W04: `Modelo` nuevo = instancia del FBX (hijos Armazon, Canon, Bomba o Cargador, Mira). `Boca` movida al centro de la cara delantera de `Canon`.
- Banco de Tino: `ConfigMenuArmas_Base` y `CreadorMenuArmas` con Bomba/Canon (escopeta) y Cargador/Canon/Mira (fusil, francotirador). `VistaHologramaArma` ya no inventa cargador ni cañón;
  `CalculoPiezasPrimitivas` quedó solo con `FactorSobrePedestal`. `CrearMira` se queda para la pistola (T-971).
- Escopeta sin mira: `EntradaArmaMenu.TieneMira` y `NivelMaximo` (2 sin mira); `ReglaMejoraArma` recibe el máximo (parámetro con valor por defecto 3); `PantallaMejoraArmas` lo usa en
  progreso, marcas, filtros, siguiente pieza y barras. La fila de la mira queda apagada con "SIN MIRA" (ocultarla rompía la lista) y las teclas la saltan.
  Una escopeta en nivel III guardado se ve "II / 2" al máximo.

## Decisiones
- El FBX sale con el cañón sobre -Y y el arriba sobre +Z de Unity (no a +Z/+Y como decía el encargo): `Modelo` lleva rotación Euler(90, 0, 180) para dejar cañón a +Z y arriba +Y. Escala 1.
- Límites en espacio del prefab (viejo -> nuevo; Z largo, Y centro): W02 0.980 -> 0.950 m, centro Y 0.092 -> 0.026 (se movió 6.6 cm, más de 5 cm: no se ajustó); W03 0.834 -> 0.800, 0.078 -> 0.048; W04 1.079 -> 1.150, 0.086 -> 0.091.
- Boca nueva: W02 (0, 0.133, 0.664), W03 (0.007, 0.119, 0.563), W04 (0.022, 0.123, 0.880). No hay AgarreIzq en los tres prefabs.
- Los prefabs de piso no tienen colisionador y se cargan el arma anidada; no se tocaron.
- Aviso: `ImportarS1nPruebas` (EditMode, Arte) corre `ImportarS1n.MeterTodo` y regenera los prefabs W0x y `JugadorAPie.prefab`; borra este trabajo si corre. La corrida completa se hizo excluyéndola.

## Pruebas
- Editor: `ArmasPiezasT970.Importar`, `.ColocarPrefabs`, `.ConfigurarBanco`, `.CapturarModelos` (idempotentes).
- EditMode completo sin ImportarS1nPruebas: 2266 total, 2266 pasaron, 0 fallaron (incluye ArmasPiezasT970Pruebas, PoliciaS1EditPruebas).
- PlayMode BancoTodasArmasT942Pruebas, MenuArmasPruebas, ArmasNuevasS1Pruebas: 10 total, 10 pasaron.
- Capturas en docs/capturas/t970/ (banco y modelo y piso en 1280x720; mano 700x700).
