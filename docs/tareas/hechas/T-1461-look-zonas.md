# T-1461: look de noche de las zonas nuevas

Rama `tony/T-1461-look` (sale de `integ/look-19nov-A`). Script: `Assets/Arte/Editor/LookNocheT1461.cs` (Aplicar, HornearSondas, Validar).
Shader nuevo: `Assets/Arte/Fondos/Shaders/DerrameNeon.shader`. Sin Unity en el worktree: nada de esto se ha corrido todavía.

## Qué
Que K11, K12, K15 y K18 se parezcan a las referencias: asfalto mojado que refleja sodio y neón, halos en la bruma, fachadas teñidas por los letreros,
negros profundos, niebla que deja leer 60-80 m.

## Criterios
- Faroles de sodio (1, 0.55, 0.2) con intensidad 900, alcance 28 (Cerro 18 por T-1365), cono 120/50; halo billboard y cono tenue por farol.
- Hasta +80 luces puntuales de neón (las visibles desde K11, K12, K13, K15, K18 primero) y derrames aditivos en pared y piso; las luces de neón de T-1392 suben a 16 / 11 m.
- Piso: smoothness 0.78 en asfalto y 0.5 en banqueta; charcos de 5.5 a 11 m (máscaras S2c_Charco_0..7), más cerca de faroles y neones; sondas con box projection cada 60 m, 128 px, a las 23 h, máximo 12 por zona, menos de 40 MB.
- Niebla 0.0065 (antes 0.009) y color petróleo (0.24, 0.38, 0.5); posproceso con contraste 24-28, sombras a petróleo, altas luces cálidas, bloom umbral 0.9.
- Validar escribe Logs/t1461-validar.txt y falla en voz alta. Luces totales <= 1400.

## Qué se hizo (decisiones)
- Los cambios de luces y de piso de Malecón, Autopista, Orillas y Uniones van en sus prefabs; Cerro y Puerto (sesión B) como overrides de su instancia en la escena. Sus prefabs no se tocan (Validar compara MD5).
- Materiales S2c_* y los importados de modelos no se editan: se copian como `T1461_<nombre>` en Assets/Arte/Mundo/Look/T1461/ y la zona usa la copia. No se cambió ningún material por T07_AsfaltoMojado (sería cambiar UV y tiling a ciegas); queda para la primera corrida con fotos.
- Todo lo nuevo (halos, conos, derrames, charcos, luces, sondas) vive en `T1461_Look.prefab`, un hijo por zona, ya colocado en Ciudad_Prologo. Los halos de T-1399 (HalosSodio_*) se quitan de la escena.
- Halo: se reusa el shader BrumaHalos de T-1399 (ya abre el quad hacia la cámara en el vértice), así que no hay componente de runtime ni textura radial. El cono usa HaloLuz.
- Los volúmenes por zona ya existían: se editaron sus perfiles (T1392 prioridad 11 y T1399 prioridad 12). Causa del azul plano: el perfil de T-1399 gana con contraste 3 y lift levantado.
- `LookNocheT1392`: ColorDeNeon, Excluido, EsSodio y DistanciaACalle pasaron de private a internal para reusarlas.
- La niebla (ConfigCicloDia_Mundo) la comparten los barrios viejos: cambio moderado, como T-1399. T-1442 (niebla por altura) no se toca.

## Orden de corrida
LookNocheT1392.Aplicar, LookNocheT1399.Aplicar, LookNocheT1461.Aplicar, LookNocheT1461.HornearSondas, LookNocheT1461.Validar, y luego Vista19b.Fotos en otra corrida.

## Assets que faltan
Ninguno por ahora.
