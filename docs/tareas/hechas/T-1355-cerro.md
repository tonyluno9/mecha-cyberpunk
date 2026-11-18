# T-1355: Colonia del Cerro

Rama: tony/T-1355-cerro (desde 9220518). Plano: docs/plan-zonas-18nov.md. Referencias: K13, A13, M12.
Script: `MechaCyberpunk.Arte.Editor.ColocarT1355Cerro.Aplicar` y `.Fotos` (raíz de escena `T1355_Cerro`, idempotente).
Carpetas tocadas: Assets/Arte/Editor/ColocarT1355*.cs, Assets/_Proyecto/Mundo/{Datos/Zonas,Materiales,Texturas}, Tests/EditMode/Mundo, docs/.

## Qué hace
- Terreno: malla de altura de 4 m (no Unity Terrain), guardada en Mundo/Datos/Zonas/T1355_Cerro_Mallas.asset. Base que sube al oeste (0 en x=-305, ~40 m en x=-450, roca hasta x=-480),
  plana en U3, loma sobre el túnel y hueco de 12 x 8 m en la salida del túnel (x -366..-354, z 185..193). Piso 12 cm bajo el asfalto; plataformas bajo cada casa.
- Calles (con `CalleZona.Definir(..., "Cerro")`): base x=-305 de z=-120 a 150 (plana), U4 (-249,0,0) a (-305,0,0), zigzag de 9 m: sube al norte, vuelta de 27.5 m, sale
  en (-360,10,185) (salida del túnel U6), pasa por (-360,10,175), baja al sur, vuelta de 27.5 m, sube a una plaza redonda a ~36 m (radio 26). Pendiente máxima 8.3 %.
- Tres escaleras largas de concreto (z=120, 60, 0) entre la pierna 2 del zigzag y la calle base, con barandal CE03 (prefab o `Hueco_CE03_BarandalTuboEscalera`).
- Casas del kit (KitS2c.Casa) en 5 filas pegadas; cada tramo es una cadena de azoteas con escalones de 0.6-2.2 m (plan puro `PlanAzoteas`), y accesos PuntoTrepar a la azotea.
- Postes con cables (KitS2c.Poste/Cable), carros viejos (CarroViejo.prefab), charcos, rayas. Luces reales <= 10 (7 de postes, el resto neón); lo demás es emisivo/halo.

## Pruebas
EditMode: Assets/_Proyecto/Tests/EditMode/Mundo/CerroT1355Pruebas.cs (pendiente, radio, U4, U6, escalones, terreno). Sin correr: el agente no tiene Library.
Pendiente de correr y revisar en Unity: Aplicar, Fotos (docs/capturas/zonas/cerro-k13.png y cerro-a13.png), medidas de KitS2c.Medir en escena.
