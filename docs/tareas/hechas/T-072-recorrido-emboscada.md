# T-072 Recorrido de la emboscada de principio a fin (cierre de T-F4 del lado de Tony)
Dueño: Tony | Rama: tony/T-072-recorrido-emboscada | Modelo: Sonnet 5.5, medium
Depende de: T-063 a T-071

## Objetivo
Una prueba de PlayMode con entrada simulada juega la emboscada completa en Pruebas_Calle como lo haría una persona: camina al
callejón, la pandilla aparece, llama al mecha con M, espera a que baje, sube con E, hace los 3 pasos del encendido, pelea mirando
y golpeando hasta dejar a la pandilla en KO, se baja y devuelve el mecha. En todo el recorrido el jugador no puede perder. Es la
prueba de cierre de T-F4 (H4) mientras llega el callejón de Alan.

## Contexto
- Hoja de ruta, T-F4: "emboscada jugable de principio a fin; con vida baja los rivales no te matan (H4)". El mismo recorrido en el
  callejón de Alan es T-073 (espera A-F3).
- Como T-059 con su recorrido a pie: todo por teclado y ratón simulados (InputTestFixture), sin llamar métodos del juego para
  avanzar. Puedes leer estado del juego para decidir la siguiente entrada (dónde está el rival, qué paso pide el encendido).
- Lo que ya hay: Pruebas_Calle con taller, punto de llamado, emboscada y jugador (T-064 a T-071); `SensibilidadRaton` se lee de
  los config (como se corrigió en T-F3), no se escribe a mano.
- Si el recorrido destapa un error, se corrige en el área de Tony que toque, con una prueba que lo cubra, y se anota en la
  bitácora. Cambios de valores de sensación (tiempos, daño) no: se anotan para la revisión.

## Qué hacer
1. `Tests/PlayMode/Pelea/RecorridoEmboscadaPruebas.cs` (carga Pruebas_Calle, `[UnityTearDown]` que carga una escena vacía y limpia
   `Registros`, como RecorridoAPiePruebas):
   a) el jugador aparece en "Calle_Inicio" y camina con W, siguiendo la flecha del objetivo si la hay, hasta entrar al radio de
   la emboscada: aparecen los pandilleros;
   b) pulsa M dentro de la zona del punto de llamado: el mecha llega en unos 6 s;
   c) camina al mecha siguiendo la flecha y pulsa E cuando la interacción ofrece el mecha: modo Mecha;
   d) hace el encendido leyendo qué paso pide (F, mantener Espacio, Q);
   e) pelea: gira con delta de ratón proporcional al ángulo al rival en pie más cercano y hace clic seguido; termina cuando la
   emboscada da `Terminada`;
   f) baja con E (detenido) y pulsa M: el mecha regresa al taller.
   Otro caso: g) igual hasta el paso d, pero sin pelear (ni mirar ni golpear) 60 s de juego: la vida del mecha nunca llega a 0 ni
   baja del piso de piedad, y los rivales siguen en pie.
2. Mide durante a) a f): vida mínima del mecha, tiempo total, frame más largo (`Time.unscaledDeltaTime`), calor al final.
3. Capturas con el mecha en plena pelea contra la pandilla (docs/capturas/T-072-1.png) y con la pandilla en KO desde la cabina
   (T-072-2.png).

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Pelea/ y, solo para corregir errores que destape el recorrido (con su prueba), Assets/_Proyecto/Mecha/,
Assets/_Proyecto/Pelea/, Assets/_Proyecto/IA/, Assets/_Proyecto/UI/ y Assets/_Proyecto/Personaje/; docs/capturas/
No tocar: Core/, Pruebas_Tony.unity, las carpetas y escenas de Alan. Ninguna escena se edita a mano.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa con RecorridoEmboscadaPruebas contada
- [ ] RecorridoEmboscadaPruebas a) a f) pasa en menos de 120 s de juego; `Terminada` llega una vez; el jugador termina a pie y el
      mecha `EnTaller`
- [ ] En a) a f) y en g) la vida del mecha nunca llega a 0 (vida mínima medida e impresa en el log de la prueba)
- [ ] Al final de a) a f) el calor es mayor que 0 (nivel 1 o más)
- [ ] Ningún frame dura más de 100 ms y la consola no tiene errores en todo el recorrido
- [ ] Capturas T-072-1 y T-072-2 revisadas

Nota para el orquestador (no es trabajo del implementador): en docs/revision-tony.md, deja la prueba humana de H4 del lado de Tony:
Pruebas_Calle, Play, jugar la emboscada sin ayuda de nadie.

## Criterios de sensación (para un humano)
- Jugar la emboscada en Pruebas_Calle de principio a fin se siente como un momento de la historia: tensión al aparecer la
  pandilla, alivio al ver bajar el mecha, el encendido como ritual y una pelea que se gana con esfuerzo, sin sentir que el juego te
  la regala.
- ¿Cuánto dura? (meta: 1 a 2 minutos para la demo de 15).

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Pelea/): RecorridoEmboscadaPruebas.

## Fuera de alcance
- La emboscada en el barrio de Alan (T-073); llegar en carro al callejón (H3, Alan); el director del prólogo (T-F6)
