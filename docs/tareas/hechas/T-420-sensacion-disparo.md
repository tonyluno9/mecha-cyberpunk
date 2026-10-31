# T-420 Sensación de golpe y disparo
Dueño: Tony | Rama: tony/T-420-sensacion-disparo | Modelo: Sonnet 5.5 | MODO RÁPIDO (docs/plan-noche.md, paso 5; mejora 1 de docs/mejoras-pendientes.md)
Depende de: T-416 (armas y zoom del sniper).

## Objetivo
Pegar y disparar se siente: patada de cámara por arma (más en escopeta y sniper), destello del cañón, chispas en pared o carro, salpicadura roja en persona,
marca en la retícula al pegar (distinta si mata) y sacudón de cámara al conectar un puñetazo.

## Qué hacer
1. `Personaje/Datos/DatosSensacion.cs` (+ asset en Resources y material, por `Personaje/Editor/CreadorSensacion.cs`): todos los parámetros.
2. `Personaje/Combate/SensacionDisparo.cs` (la crea PistolaAPie), eventos `Fogonazo` y `BalaImpacto` en PistolaAPie y `Conecto` en GolpeAPie; CamaraHombro suma la patada.
3. `UI/HudAPie/Hitmarker.cs` (lo crea HudAPie). El zoom del sniper no cambia: su sacudida propia reemplaza la patada.
4. Pruebas: EditMode DatosSensacionPruebas, PlayMode SensacionDisparoPruebas.

## Carpetas permitidas
Personaje/, UI/HudAPie/, Tests/, docs de Tony.

## Bitácora
- Hecha: EditMode DatosSensacionPruebas 1/1, PlayMode SensacionDisparoPruebas 1/1, captura docs/capturas/T-420-1.png (marca en la retícula + destello). GolpeAPiePruebas y HudAPiePruebas tienen 5 fallas que ya existían sin T-420.
