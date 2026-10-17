# T-099 Iluminación final y comparación con la referencia
Dueño: Alan | Rama: alan/T-099-iluminacion-final | Modelo: Sonnet 5.5, medium
Depende de: T-094 a T-098.

## Objetivo
Ajustar la luz y el posproceso del barrio completo hasta que se parezca a la imagen de referencia (docs/diseno/estetica-hud.md y paquete-original), con una comparación honesta.

## Qué hacer
1. Con `VistasReferencia` toma las vistas de referencia a las 22:00 y a las 12:00; ajusta luna, ambiente, niebla, bloom y charcos en los Config existentes (sin números nuevos en el código).
2. Escribe en la revisión qué se parece y qué no a la referencia, con las capturas lado a lado.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Tests de Mundo, docs/capturas/.

## Criterios de aceptación
- [ ] Capturas antes y después: docs/capturas/T-099-1..4.png
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] Límite honesto: si parecerse a la referencia es subjetivo, queda para revisión humana
