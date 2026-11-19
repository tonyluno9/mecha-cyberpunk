# T-1399 Niebla y cielo de noche (ronda 3)

Estado: escrita (código de editor sin compilar ni correr: sin Unity y el disco estaba lleno). La corre el orquestador en la carpeta principal.
Rama: tony/T-1399-niebla-cielo (sale de integ/ciudad-19nov). Carpetas: Assets/Arte/Editor/, Assets/Arte/Fondos/Shaders/, Assets/Arte/Mundo/Look/ (se crea al correr), docs/.

## Por qué el cielo era azul marino liso y no se veía niebla (medido en código y assets)
- Domo de nubes CieloNubes: ConfigAmbienteNoche_Mundo.opacidadNubes = 0 (apagado desde la ambientación nocturna). Las nubes con textura (CalculoCieloTextura.NubesNoche) tienen alfa 0.04 de noche (T-880).
- ColocarS2cLook.Foto no armaba CieloTexturizado ni movía el domo (en el editor no corren Start ni LateUpdate): las fotos nunca tuvieron nubes ni estrellas.
- Niebla de noche en ConfigCicloDia_Mundo: color (0.15, 0.21, 0.32), densidad 0.006 exp. al cuadrado (30 % a 100 m, 89 % a 250 m). Es casi el color del cielo y de las sombras, y ACES + PerfilNoche lo dejan en ~35 % de brillo (cielo en pantalla ~ #0f1937): existe pero no contrasta.
- El horizonte del cielo es el color de la niebla (CicloDiaNoche.AplicarCielo); cénit (0.07, 0.11, 0.22); brillo de ciudad (0.2, 0.24, 0.32) a 0.12.
- La hora de la foto (23 h, CicloDiaNoche.Aplicar) es la misma lógica del juego de noche; no era la causa. Shaders sin niebla: solo SkylineSinNiebla y FondoCiudad (fondos).

## Qué hace (orden: LookNocheT1392.Aplicar, luego LookNocheT1399.Aplicar, y en otra corrida LookNocheT1399.Fotos)
- Global: niebla de noche (0.34, 0.42, 0.57), densidad 0.009 de 21:00 a 4:30 (60 m 25 %, 100 m 55 %, 120 m 67 %, 250 m 99 %), cénit (0.04, 0.065, 0.15) solo en noche cerrada, brillo de ciudad (0.3, 0.38, 0.52) a 0.3.
- Domo: opacidadNubes 0.6, _ColorNubes (0.3, 0.35, 0.47), _ColorBrillo (0.5, 0.5, 0.6), _Cobertura 0.62.
- Por zona nueva (Malecón, Cerro, Autopista, Puerto, Uniones), en la escena bajo T1399_Bruma: plano de bruma de altura (shader BrumaAltura, quad con malla propia, opacidad por distancia 30 a 220 m, sin collider), halos cálidos billboard en los faroles de sodio (shader BrumaHalos, una malla por zona), y un Volume local (prioridad 12, contraste 3, lift (0.95, 0.98, 1.05, 0.05)).
- ColocarS2cLook.CieloCompleto (nuevo, apagado por defecto): con true la foto arma nubes, estrellas y domo como en el juego.
- Fotos: docs/capturas/t-1399/{malecon-k11,cerro-k13,puerto-k15,autopista-k18}.png y -lado.png. Informe: Logs/t1399-look.txt.

## Barrios viejos
Solo les afecta el cambio global (color y densidad de la niebla, cielo, nubes). A 60 m pasa de 13 % a 25 % de niebla, más clara y azul; es el nivel que ColocarS2cLook pedía para K01 (0.02). Bruma de altura, halos y lift quedan solo en las zonas nuevas. Si se ven lavados, bajar DensidadNoche a 0.007 en LookNocheT1399 y volver a correr Aplicar.

## Rendimiento
Sin volumetría ni lectura de profundidad: 5 planos, 5 mallas de halos, 5 volúmenes; LiftGammaGain va en el mismo uber de posproceso. Estimado muy por debajo de 0.5 ms (sin medir).

## Pendiente de verificar con Unity
Compilar; correr Aplicar y Fotos; revisar capturas (horizonte #3a4a66 a #55657f en pantalla, edificios lavados a 80 a 120 m, SK01 visible, nubes visibles); medir el costo; revisar tamaño de la escena (< 100 MB). Los valores de color son estimados sin poder medir; las constantes están arriba de LookNocheT1399.cs.
