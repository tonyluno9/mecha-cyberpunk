# T-1414 Vistas aéreas (A13 Cerro, A15 Puerto, A18 Autopista)

Script: Assets/Arte/Editor/AereasT1414.cs (sin compilar en el worktree: la corrida de Unity la hace el orquestador).
Métodos: AereasT1414.Aplicar (o AplicarCerro/AplicarPuerto/AplicarAutopista), AereasT1414.Validar, AereasT1414.Fotos y AereasT1414.Diagnostico (estas dos en corridas aparte).
Cada prefab de zona recibe una raíz propia T1414_<Zona> (con CamaraA/Mira, Faroles y, en Cerro y Autopista, Azoteas, Props, Ventanas y Neones). Aplicar la rehace.
Pendiente de evidencia: Logs/t1414-aplicar.txt, t1414-validar.txt, t1414-torres.txt y docs/capturas/t-1414/*.png.
