# T-volumen-ajustes-24nov: volumen en el menú de ajustes

## Paso 0 (obligatorio)
Cerrar Unity y Unity Hub, confirmar que no queda ningún Unity.exe, borrar `Temp/UnityLockfile` y repetir si el candado vuelve a aparecer.

## Qué hacer
En el menú de ajustes (desde el menú principal y desde la pausa), agregar deslizadores con el estilo de la UI del juego:
- **General**
- **Música**
- **Efectos** (armas, impactos, carros, ambiente, UI)
- **Voces** (personajes, peatones y narración)

Usar un AudioMixer con grupos Música, Efectos y Voces (crearlos si no existen y enrutar ahí todas las fuentes, incluidas las nuevas de 19_Voces y 20_Musica). Los valores se guardan entre partidas y se aplican al arrancar. Al mover el deslizador de efectos o de voces suena una muestra (UI_MOVER / una voz corta). Funciona igual en PC y en VR.

## Cómo comprobarlo
Que el usuario baje la música a 0 y suban las voces: se nota en el juego, y al cerrar y abrir el juego se conserva.
