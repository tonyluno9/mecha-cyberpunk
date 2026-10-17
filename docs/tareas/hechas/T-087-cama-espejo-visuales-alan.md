# T-087 Cama y espejo (solo lo visual)
Dueño: Alan | Rama: alan/T-087-cama-espejo-visuales | Modelo: Sonnet 5.5, medium
Depende de: T-086.

## Objetivo
Cama, espejo y mesita con modelo propio y un punto marcado frente a cada uno, listos para recibir la interacción cuando `IInteractuable` llegue a main.

## Qué hacer
1. Props `Prop_Cama`, `Prop_Espejo`, `Prop_Mesita` (fuente de Blender en Assets/Arte/Fuentes~/casa/, flujo de T-076) y prefabs en Mundo/Prefabs/Kit/ con collider.
2. El espejo es un cuadro de material reflectante barato (sin cámara extra) con marco. Cada prop lleva un hijo `PuntoUso` a 0.8 m delante, mirando al prop.
3. Colocarlos en la casa de T-086 con CreadorCasa.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/Arte/, Tests de Mundo, docs/capturas/. No tocar Core/ ni Tony.

## Criterios de aceptación
- [ ] Los 3 prefabs existen con collider y `PuntoUso`; colocados en la casa sin atravesar muros
- [ ] Captura del dormitorio: docs/capturas/T-087-1.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Dormir, avanzar el reloj, reflejo real, `IInteractuable` (T-092).
