# Ambientación de noche: estado (rama tony/ambientacion-noche)

## Paso 1: diagnóstico (antes de cambiar nada)

Revisado en los assets y con un volcado en batch de Ciudad_Prologo.unity (luces, sondas, volumes, cámaras, partículas,
materiales usados). Valores tal como estaban en tony/fase-g.

| # | Punto | Estado | Evidencia (archivo y valor) |
| --- | --- | --- | --- |
| 1 | URP para la RTX 5070 | parcial | `Assets/Settings/PC_Renderer.asset`: `m_RenderingMode: 2` (Forward+) y SSAO (`Intensity 0.4`, `Radius 0.3`). `PC_RPAsset.asset`: sombra principal 2048, adicionales 2048, `m_SoftShadowQuality: 3`, distancia 50 m, 4 cascadas, `m_ColorGradingMode: 0` (LDR). `ProjectSettings/QualitySettings.asset`: solo `Mobile` y `PC`; no hay `Ultra` ni `Medio`. |
| 2 | Noche: cielo, luna, ambiente | parcial | `Mundo/Materiales/Cielo_Procedural.mat` (Skybox/Procedural) teñido por `ConfigCicloDia_Mundo.asset` a (0.12, 0.09, 0.26), exposición 0.6: oscuro pero sin nubes. Luna `LuzNoche`: color (0.45, 0.55, 1.0), intensidad 1.1 (azulada, no tenue). Ambiente plano (0.27, 0.29, 0.58) que con `postExposure 2.2` de PerfilNoche se ve lila claro. |
| 3 | Niebla | parcial | Niebla de distancia: `CicloDiaNoche` pone Exp² densidad 0.02, color (0.12, 0.09, 0.26). Niebla baja: partículas `NieblaBaja` de `LluviaSigueJugador` con `Lluvia_Niebla.mat` (Particles/Unlit, no la tiñen los neones). No hay volumétrica ni planos de niebla. |
| 4 | Lluvia | parcial | `Mundo/Ambiente/LluviaSigueJugador.cs` (gotas 1500/s en caja de 30 m que sigue al jugador) y sonido en `AmbienteSonoro` (capa `Ambiente_Lluvia`). Faltan salpicaduras en el piso. |
| 5 | Piso mojado | parcial | `Barrio_Asfalto.mat`: Lit, `_Smoothness 0.85`, base (0.04, 0.045, 0.055), sin mapa de charcos. 50 charcos de geometría (`Barrio_Charco.mat`, 0.97). Solo 4 sondas de reflejo (2 de 50 m en la calle principal y 2 en los cruces), no cada 20 m. URP 17.3 no trae SSR. 0 decals (el renderer no tiene la feature Decal). |
| 6 | Posproceso | parcial | `Mundo/Datos/PerfilNoche.asset`: Bloom (umbral 1, intensidad 2, scatter 0.7), Tonemapping ACES, ColorAdjustments (exposición 2.2, contraste 15, saturación 25, filtro frío), Vignette 0.28. Faltan el tono dividido (sombras turquesa, luces magenta/naranja), la aberración cromática y el grano. |
| 7 | Luces y APV | parcial | 63 farolas Point (r 22 m, i 160, prendidas por `LuzNocturna`) y 57 Point de entradas e interiores. Los 39 letreros (`Barrio/LetrerosNeon`) solo tienen emisión (Neon_* 6.0), sin luz. Los 3 puestos de la calle principal no tienen luz. Las fachadas de T-250 (C01-C03, en Assets/Arte) no tienen emisión en las ventanas. APV apagado: `m_LightProbeSystem: 0`, 0 ProbeVolume, 0 LightProbeGroup. |
| 8 | Atmósfera extra | parcial | 146 cables `LineRenderer` entre postes (`VestirCalle.Cables`, comba parabólica), ninguno entre poste y fachada. No hay focos cálidos sobre la calle de los tacos. 0 sistemas de partículas de humo o vapor en los puestos. |

## Paso 2: antes

FPS medidos en un build de Windows (Builds/Medicion, ver "Cómo se midió") a 1920x1080 en ventana, D3D12, RTX 5070
Laptop, calidad `PC`, vsync apagado, a las 23:00 con lluvia:

| Punto | FPS promedio | ms | FPS del 1 % más lento |
| --- | --- | --- | --- |
| Calle principal (-30, 0, -2) mirando al este | 293.6 | 3.41 | 195.9 |
| Callejón (-53, 0, -59) mirando al oeste | 451.5 | 2.21 | 309.1 |
| Entrada del clandestino (0, 0, -1.5) mirando la puerta | 376.2 | 2.66 | 265.7 |

Capturas: docs/capturas/ambiente-antes-calle.png, ambiente-antes-callejon.png y ambiente-antes-clandestino.png.

### Cómo se midió

`Mundo/Editor/ConstruirMedicionAmbiente.Construir` (batch) agrega a Ciudad_Prologo, solo mientras construye, un objeto
con `Mundo/Pruebas/MedicionAmbiente` y construye `Builds/Medicion/MechaCyberpunk.exe`; después devuelve la escena tal
como estaba. Al correr, la herramienta espera 6 s, pausa el reloj a las 23:00, lleva al jugador a cada punto (la lluvia y
las zonas interiores lo siguen), mira con su propia cámara a 1.7 m (con el modelo del jugador oculto), calienta 4 s,
mide 8 s de `Time.unscaledDeltaTime`, guarda la captura a 1920x1080 y escribe `medicion.txt`.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe" -batchmode -quit -projectPath . \
  -executeMethod MechaCyberpunk.Mundo.Editor.ConstruirMedicionAmbiente.Construir -logFile Logs/medicion-build.log
./Builds/Medicion/MechaCyberpunk.exe -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -medicionSalida <carpeta>
```

## Estado del trabajo (interrumpido)

Pasos 3 y 4 hechos en parte, sin commit. La última corrida (`CreadorAmbienteNoche.Todo` y después la medición) se detuvo
durante el horneado del APV porque Claude Code la cortó: el sistema se quedó con muy poca memoria. Falta volver a correr
`Todo`, medir, guardar las capturas del "después", correr las 5 pruebas, generar Builds/Juego y hacer el commit.
Medición intermedia (build de medición, Ultra, 23:00, antes del último ajuste): calle 236.1 FPS (1 % bajo 130.9),
callejón 418.4, clandestino 302.8.
