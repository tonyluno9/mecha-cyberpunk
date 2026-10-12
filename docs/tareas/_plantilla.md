# T-012 Golpe y bloqueo
Dueño: Tony | Rama: tony/T-012-golpe-bloqueo | Modelo: Sonnet 5.5, medium
Depende de: T-001, T-011

## Objetivo
Qué existe al terminar y por qué importa para el juego, en una o dos frases.

## Contexto
Archivos relevantes, decisiones de diseño que aplican, enlace a docs/diseno/combate.md.

## Qué hacer
1. ...
2. ...

## Carpetas permitidas
Assets/_Proyecto/Combate/ y Assets/_Proyecto/Tests/EditMode/Combate/
No tocar: Core/, Arena/, IA/, ni escenas que no sean Pruebas_Tony.unity

## Criterios de aceptación (se comprueban jugando)
- [ ] Con clic izquierdo, el brazo derecho lanza un golpe visible
- [ ] Manteniendo clic derecho, los brazos se cruzan y el daño recibido baja
- [ ] No aparecen errores en la consola durante una pelea completa

## Pruebas automáticas
- EditMode: el bloqueo reduce el daño; el golpe no se repite antes de su recarga

## Fuera de alcance
- Contraataque, sonido, partículas
