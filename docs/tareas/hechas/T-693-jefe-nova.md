# T-693 Jefe de Nova
Dueño: Tony | Rama: tony/T-693-jefe-nova | Modelo: Sonnet 5.5, medium
Depende de: T-404, T-673

## Objetivo
El Capitán Nova golpea de verdad con un patrón claro y se puede esquivar rodando agachado.

## Qué hacer
1. IA/APie/ReglaJefeNova (pura): Acecho, telegrafía el golpe (0.8 s, pausa y brillo), golpe de área corta (radio 2.8 m, arco de 160 grados, 22 de daño), recuperación (1.8 s, recibe 1.5x), cada 3 golpes y desde 4 a 14 m una carga (telegrafía 1.1 s con brillo rojo, 11 m/s durante 0.7 s, 35 de daño, recuperación 2.6 s).
2. PandilleroAPie: modo jefeCuerpoACuerpo (ActualizarJefeNova, GolpeDeArea, brillo con luz y tinte). Datos nuevos en DatosPandilleroAPie.
3. IA/Editor/AjustarJefeNovaT693 pone el asset DatosPandilleroAPie_JefeNova en cuerpo a cuerpo (sin escopeta). Si se vuelve a correr CreadorCiudadPandillas hay que volver a correr este método.

## Carpetas permitidas
IA, Tests

## Criterios de aceptación
- [x] Telegrafía con pausa y brillo antes de pegar; no pega durante la pausa
- [x] Pega 22 con el golpe de área y 35 con la carga si te quedas quieto
- [x] Se recupera y recibe 1.5x; cada 3 golpes carga
- [x] Agachado, Ctrl + A + Espacio (rodar) esquiva el golpe y la carga

## Pruebas automáticas
- EditMode ReglaJefeNovaPruebas; PlayMode JefeNovaT693Pruebas (4, entrada simulada); capturas docs/capturas/s1t-T-693-jefe-telegrafia.png y s1t-T-693-jefe-carga.png
