# T-041 Reloj del mundo y ciclo de día y noche
Dueño: Alan | Rama: alan/T-041-reloj-dia-noche | Modelo: Sonnet 5.5, medium
Depende de: T-040 (barrio, farolas, Ciudad.unity y Pruebas_Alan.unity). Parte B: T-032 en main (`IRelojMundo`, `FranjaDia`).

## Objetivo
El mundo tiene hora: un día dura 20 minutos reales, 12 de noche y 8 de día, y la luz, el ambiente, la niebla y las farolas
cambian con él. Cuando llega el contrato de Core, el reloj se publica como `IRelojMundo` para que los locales (T-042) y los
sistemas de Tony (renta, dormir, objetivos) usen la misma hora.

## Contexto
- Diseño: docs/diseno/ciudad.md (un día dura 20 minutos reales: 12 de noche y 8 de día; de día abre el mercado negro; de noche
  el clandestino, el bar y el casino), docs/diseno/estetica-hud.md (niebla y neón; la noche es la imagen de referencia).
- Propuesta: D-alan-4 (amanecer 07:00, anochecer 19:00; 12 horas de día en 8 minutos y 12 de noche en 12; hora inicial 21:00) en
  docs/decisiones-pendientes-alan.md. Esos valores van en un ScriptableObject, no en el código.
- Contrato (parte B): lee los comentarios de `Core/Mundo/IRelojMundo.cs` en main, sobre todo las reglas de `HoraEnteraCambio`
  y de `AvanzarHasta`. Tu implementación las cumple al pie de la letra.
- La luz y el ambiente se cambian en la escena en tiempo de ejecución (`RenderSettings`, la luz direccional), nunca en
  ProjectSettings/.
- Cómo saber si llegó T-032: `git show main:Assets/_Proyecto/Core/Mundo/IRelojMundo.cs`.

## Qué hacer

### Parte A: sin esperar a Tony
1. `Mundo/Datos/ConfigRelojMundo.cs` y su asset: hora de amanecer (7), de anochecer (19), minutos reales de día (8) y de noche (12),
   hora inicial (21), día inicial (1).
2. Lógica pura `Mundo/Reloj/CalculoReloj.cs`: avanza con segundos reales (`dt`), con la velocidad de la franja en que está
   (si un `dt` cruza el amanecer o el anochecer, cada parte avanza a su velocidad); lleva la hora y el día; reporta qué horas
   enteras pasaron (en orden), si cambió la franja y si cambió el día; `AvanzarHasta(hora)` y pausa con las reglas del contrato
   de T-032 (si todavía no está en main, con las reglas copiadas en esta tarea: salta hasta la próxima vez que el reloj marque
   esa hora, nada si es la actual, un aviso de hora final, uno de día por medianoche cruzada y uno de franja solo si cambió).
   Sin los tipos de Core usa `bool EsDeNoche`; no crees tu propio `FranjaDia`.
3. `Mundo/Reloj/RelojMundo.cs` (MonoBehaviour): usa `CalculoReloj` con `Time.deltaTime` y expone hora, día, si es de noche,
   pausa, `AvanzarHasta` y eventos de hora entera, franja y día.
4. `Mundo/Reloj/CicloDiaNoche.cs` con `Mundo/Datos/ConfigCicloDia.cs` y su asset: por hora del día, color e intensidad de la luz
   direccional (sol de día, luna fría de noche), su rotación, color del ambiente y color y densidad de la niebla
   (`Gradient` y `AnimationCurve` en el config).
5. `Mundo/Reloj/LuzNocturna.cs`: prende su `Light` (y su material emisivo, si tiene) de noche y la apaga de día. Ponla en las
   farolas del barrio y en los letreros de neón que ya existan.
6. `Mundo/Pruebas/ControlRelojDePrueba.cs` con acciones en el `.inputactions` de la cámara libre: N salta a las 22:00, M salta a
   las 12:00, T alterna la velocidad ×30. Escribe en la consola la hora nueva.
7. En Ciudad.unity y Pruebas_Alan.unity: un `RelojMundo` y un `CicloDiaNoche`; el control de prueba solo en Pruebas_Alan.

### Parte B: cuando T-032 esté en main
8. `RelojMundo` implementa `IRelojMundo` (con `FranjaDia`), se agrega a `Registro<IRelojMundo>` en `OnEnable` y publica los
   eventos del contrato. `CicloDiaNoche` y `LuzNocturna` pueden escuchar al reloj por el contrato o directo (es la misma área).

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Tests/EditMode/Mundo/, Assets/_Proyecto/Tests/PlayMode/Mundo/,
Assets/_Proyecto/Escenas/Ciudad.unity y Assets/_Proyecto/Escenas/Pruebas_Alan.unity
No tocar: Core/, Arena/, las carpetas y escenas de Tony, ProjectSettings/ (tampoco Lighting global del proyecto).

## Criterios de aceptación (los comprueba el verificador)
Parte A:
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode CalculoReloj: un día completo dura 1200 s reales; de 07:00 a 19:00 tarda 480 s y de 19:00 a 07:00 tarda 720 s
      (±0.01 s); desde 21:00, 60 s reales llevan a 22:00; un `dt` que cruza el anochecer avanza cada parte a su velocidad;
      cruzar las 00:00 suma un día una vez; un `dt` de 3 horas reporta las 3 horas enteras en orden; `AvanzarHasta(8)` desde las
      21:00 deja el día siguiente a las 08:00 con un aviso de día, uno de franja y uno de hora; `AvanzarHasta` a la hora actual no
      cambia nada; en pausa no avanza
- [ ] PlayMode: con el reloj en 12:00 la luz direccional es más intensa que en 00:00, y el color del ambiente cambia
- [ ] PlayMode: a las 22:00 todas las `LuzNocturna` del barrio están prendidas y a las 12:00 todas apagadas
- [ ] PlayMode con entrada simulada en Pruebas_Alan: N deja el reloj en 22:00 y M en 12:00; T multiplica la velocidad por 30
- [ ] Capturas desde el mismo punto de Pruebas_Alan a las 12:00 y a las 23:00: docs/capturas/T-041-1.png y -2.png; de noche se
      ve más oscuro, con niebla y farolas prendidas
- [ ] Consola sin errores en 3 minutos con velocidad ×30 (más de un día completo)
Parte B:
- [ ] PlayMode: `Registro<IRelojMundo>.Actual` es el reloj de la escena; `FranjaCambio`, `DiaCambio` y `HoraEnteraCambio` llegan
      con las reglas del contrato (una prueba con `AvanzarHasta` y otra avanzando con `dt`)
- [ ] Si T-032 no estaba en main al terminar la parte A, la tarea queda "hecha" con la parte B anotada en docs/revision-alan.md

## Criterios de sensación (para un humano)
- La noche se siente cyberpunk (fría, con niebla y farolas) y el día no lava el barrio; con ×30, el amanecer y el anochecer
  no son un salto brusco.

## Pruebas automáticas
- EditMode (Tests/EditMode/Mundo/): CalculoRelojPruebas, con los casos de arriba.
- PlayMode (Tests/PlayMode/Mundo/): CicloDiaNochePruebas y ControlRelojPruebas (entrada simulada).

## Fuera de alcance
- Horarios de los locales y letreros (T-042)
- Dormir, renta y lo que pase cada día (Tony, T-F5)
- Lluvia, neón final y audio de la ciudad (A-F4); sol que proyecta sombras realistas o cielo procedural
