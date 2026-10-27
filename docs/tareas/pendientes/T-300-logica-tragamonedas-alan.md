# T-300 Lógica pura de la tragamonedas (A-F5, casino)
Dueño: Alan | Rama: alan/T-300-logica-tragamonedas | Modelo: Sonnet 5.5, medium
Depende de: nada de Tony. Solo `System.Random` inyectado.

## Objetivo
Las reglas de una tragamonedas de 3 carretes, sin Unity: símbolos con peso, tabla de pagos y retorno medible.

## Qué hacer
1. `Casino/Tragamonedas/CalculoTragamonedas.cs` (C# simple): `Girar(Random azar)` devuelve los 3 símbolos; `Pago(simbolos, apuesta)` devuelve lo ganado (3 iguales paga el multiplicador del símbolo; 2 cerezas paga 1×; nada paga 0).
2. `DatosTragamonedas` (ScriptableObject en `Casino/Datos/`): símbolos (nombre, peso, multiplicador) y apuesta. Ningún número mágico en el código.
3. `RetornoEsperado()` calcula el retorno exacto sumando probabilidades.

## Carpetas permitidas
Assets/_Proyecto/Casino/, Assets/_Proyecto/Tests/EditMode/Casino/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [ ] Retorno esperado exacto entre 0.90 y 0.97 con los datos base
- [ ] 1 millón de giros con semilla fija: retorno medido a ±0.02 del esperado
- [ ] Mismos símbolos con la misma semilla; la apuesta 0 o negativa no paga nada
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
La máquina en el mundo (T-301), fichas y cajera (T-302).
