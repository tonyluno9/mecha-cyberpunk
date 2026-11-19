# T-1462: la ciudad se ve bien desde arriba (antes T-1442)

Rama tony/T-1462-aereas (base integ/look-19nov-A, commit 9e40b97). Brief: docs/brief-19nov-A.md. Cupo de luces: 0.

## Problema
Edificios negros que flotan, bloques gigantes, paredes azules lisas (A18), mancha blanca en la autopista (A18), torres que flotan en la niebla,
skyline del Malecón como panel plano sobre el mar (K11) y niebla que tapa la zona entera en las aéreas.

## Paso 1 (hecho): diagnóstico
`Assets/Arte/Editor/AereasT1462.cs`, método `Diagnosticar()`. Sin Unity en el worktree: escrito sin compilar, Opus lo corre.
Correr19nov: `-pasos AereasT1462.Diagnosticar` (escena Ciudad_Prologo, no guarda).
Cámaras: A11, A13, A15, A18 como Vista19b, más rejilla 40/80/150/250 m x 4 rumbos a 45 grados sobre la mediana de cada zona
(Malecón, Cerro, Autopista, Puerto, Orillas). Marcas: A enorme/plano, B flota más de 4 m, C nombre/shader de fondo, D blanco/azul liso.
Salida: `Logs/t1462-diagnostico.txt` (ruta completa, prefab, malla, material, shader, centro, tamaño, % de pantalla y cámara) y fotos
`Logs/t1462-diag-*.png` (4 Vista19b y, por zona, 80 m rumbo N y 150 m rumbo S) con candidatos de más de 1 % pintados; leyenda de colores en el txt.
Límite: el % es del rectángulo del bounds, no descuenta oclusión.

## Paso 2 (pendiente, con el diagnóstico)
- Arreglar fondo lejano (con base, bajo o apagado por altura con componente en Mundo + prueba EditMode), skyline del Malecón como torres lejanas con ventanas y neones.
- `NieblaPorAltura` (Mundo): factor sobre la densidad de RenderSettings.fog por altura sobre el piso (calle igual; 40-80 m ~35-45 %; 150 m+ bajo), compatible con CicloDiaNoche (que fija niebla cada frame); prueba EditMode de la lógica pura; ColocarS2cLook.Foto lo aplica.
- `Validar()` repite el diagnóstico y falla si quedan sospechosos en cuadro.
- No editar T1355_Cerro ni T1357_Puerto: lo que salga de ellos se anota aquí para la sesión B.

## Para la sesión B (Cerro y Puerto)
(vacío hasta tener el diagnóstico)

## Assets que faltan
(vacío)
