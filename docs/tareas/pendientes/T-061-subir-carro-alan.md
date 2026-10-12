# T-061 Subir al carro de Alan (H3, lado de Tony)
Dueño: Tony | Rama: tony/T-061-subir-carro-alan | Modelo: Sonnet 5.5, medium
Depende de: T-058, T-060
**Espera A-F2.** Motivo: necesita en main el carro viejo de Alan implementando `IMontable` (A-F2) y colocado en Pruebas_Alan.unity.
El orquestador la deja en docs/tareas/bloqueadas/ hasta que esté en main.
Cómo saber si llegó: `git grep -l "IMontable" main -- Assets/_Proyecto/Vehiculos/` encuentra al menos un archivo.

## Objetivo
El jugador a pie de Tony se sube al carro de Alan con E, va dentro mientras Alan maneja el carro, y se baja con E, sin cambiar
nada del carro. Es el lado de Tony del hito H3; la vuelta de casa al clandestino en carro la cierra Alan con su prueba.

## Contexto
- Hoja de ruta: H3 (T-F3 y A-F2). Contrato: Core/Jugador/IMontable.cs (T-050). Lado del jugador ya hecho y probado con
  CarroDePrueba (T-058).
- Lee la tarea de Alan del carro (docs/tareas/hechas/) para saber qué teclas maneja el carro y cuál es su `PuedeBajar`.
- Los tests de Tony no referencian Vehiculos/: el carro se encuentra en `Registro<IMontable>` por `Modo == Carro`.

## Qué hacer
1. `Tests/PlayMode/Personaje/CarroAlanPruebas.cs`: carga Pruebas_Alan.unity, instancia `JugadorAPie.prefab`, apaga los otros
   `IJugador`, coloca al jugador a 1.5 m del `PuntoSubida` del carro mirándolo y, con entrada simulada: E sube; con las teclas de
   manejo de Alan avanza unos segundos; se detiene; E baja.
2. Ajustes del lado del jugador, si hacen falta, en Personaje/ (se anotan). Lo que falte del lado del carro se anota para
   docs/revision-tony.md y la tarea queda bloqueada con la evidencia.

## Carpetas permitidas
Assets/_Proyecto/Tests/PlayMode/Personaje/ y, solo para ajustes del lado del jugador, Assets/_Proyecto/Personaje/
No tocar: Vehiculos/, Mundo/ y las escenas de Alan, Core/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa con CarroAlanPruebas contada
- [ ] CarroAlanPruebas: tras E, `Modo` es Carro, `IJugador.Camara` es la del carro y hay un solo `AudioListener` habilitado;
      manejando, la `Raiz` del jugador sigue al `Asiento` (a menos de 0.3 m); detenido, E baja con `Modo` APie, el jugador queda
      en el piso fuera del collider del carro y camina con W
- [ ] Con el carro en marcha (si el carro de Alan lo permite en la prueba), E no baja y el HUD dice "Frena para bajar"
- [ ] `git status` después de las pruebas no muestra cambios en archivos de Alan
- [ ] Captura manejando el carro de Alan con el HUD del objetivo: docs/capturas/T-061-1.png
- [ ] Consola sin errores

## Criterios de sensación (para un humano)
- Subir y bajar del carro de Alan se siente igual de inmediato que con el carro de prueba.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Personaje/): CarroAlanPruebas.

## Fuera de alcance
- Manejo, cámara y física del carro (Alan); la prueba de cierre de H3 de casa al clandestino (Alan, A-F2)
