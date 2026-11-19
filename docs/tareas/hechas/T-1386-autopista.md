# T-1386: Autopista elevada igual a K18 y A18

Rama: tony/T-1386-autopista. Script: `Assets/Arte/Editor/ZonaAutopistaT1386.cs` (clase `MechaCyberpunk.Arte.Editor.ZonaAutopistaT1386`).
Prefab: `Assets/Arte/Mundo/Zonas/T1356_Autopista.prefab` (todo lo nuevo bajo `T1386_Autopista`; Aplicar lo rehace).

Métodos: `Aplicar`, `Validar` (Logs/t1386-validar.txt), `Fotos` (docs/capturas/t-1386/, en corrida aparte).

## Cambios contra la referencia
- Barreras AU02 (variante _a) en los bordes del tablero y de la rampa; el guardarraíl AU05 (solo visual) se apaga y el parapeto con collider sigue cerrando el borde.
- Chatarra AU03 (_a, y _b cada tercero) bajo el tablero, z 236 a 259, fuera de la calle baja y de la rampa; quita por nombre la chatarra vieja de Bajo/Chatarra que queda debajo.
- Calle baja: la fila sur (z=210, x -290 a -190) pasa a casas de 2 y 3 pisos con letreros, más cables, semáforo y carros estacionados.
- Torre de reflectores al fondo (-330, 312) con 3 haces; apaga las torres del fondo en x < -280.
- Roca del acantilado aclarada (T1386_Roca, copia de la roca con textura).
- Cámara de K18 corregida (calle baja a 3.2 m, mirando a la boca). La rampa norte sube al este (x > -107) y no cabe en ese encuadre.
- Atorones y escalón del túnel: `Validar` lista los colliders de esos puntos y la cota de las bocas AU06; sin Unity no se pudo confirmar si es geometría.
