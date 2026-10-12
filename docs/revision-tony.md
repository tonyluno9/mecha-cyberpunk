# Revisión de Tony (turno de día)

## Para probar de noche / al despertar
- Pruebas_Tony.unity, Play: WASD, ratón, Escape y clic. Observa si el mecha se siente pesado (T-010).
- T-011, Pruebas_Tony, Play: gira rápido el ratón y mira arriba y abajo; ¿parpadea o se recorta algo? ¿Tinte del cristal, neón turquesa/magenta y brazos marrones se ven bien?
- T-012, Pruebas_Tony, Play: mira de frente (el atacante se pone naranja al avisar), clic izq golpea el saco y clic der guarda. Prueba Bloqueo perfecto: clic der justo en los últimos ~0.2 s del aviso. ¿Se sienten bien el peso y la recarga?
- El piso no tiene muros: se puede caer por el borde.

## Tareas
### T-001: hecha (sin revisar). Verificada antes: 5/5, EditMode 18/18. Sin merge a main.
### T-002: hecha (sin revisar)
Verificada 5/5: verificar.sh todo (compilar, EditMode 18/18, PlayMode 3/3), editor vivo y captura real (docs/capturas/T-002-1.png). Merge a main no hecho (el clasificador lo denegó antes); está en tony/fase-1. `.gitattributes` (*.sh eol=lf) cae fuera de las carpetas permitidas: decide si se acepta.
### T-010: hecha (sin revisar)
Verificada 9/9. EditMode 29/29, PlayMode 14/14, 125 s de Play sin errores. Capturas T-010-1/2.png. Detalles en docs/bitacora/2026-10-11-tony-T-010.md.
### T-011: hecha (sin revisar)
Verificada 7/8 automático; 1 criterio de juicio humano (parpadeo al girar). EditMode 29/29, PlayMode 24/24. Capturas T-011-1/2.png. Bitácora: docs/bitacora/2026-10-11-tony-T-011.md.
### T-013: hecha (sin revisar)
Verificada con EditMode 36/36, PlayMode 48/48 y captura T-013-1.png. A probar a mano: sensación de la sacudida (ConfigSacudidaCabina_Base.asset), legibilidad de la pantalla de vida, que H no haga nada tras el KO. CabinaPruebas (T-011) se tocó solo para la ruta Sacudida/Cabina.
Verificador: 7/7 criterios cumplen (sensación de la sacudida a mano). Cambios fuera de lo permitido, para decidir: una línea en Tests/PlayMode/Mecha/CabinaPruebas.cs (ruta Sacudida/Cabina) y el asmdef MechaCyberpunk.Mecha.Editor ahora referencia Combate y UI (solo editor).
### T-012: hecha (sin revisar)
Verificada 9/10 automático; bloqueo perfecto solo por prueba. Una ronda de corrección (aviso naranja invisible, ya visible: T-012-5.png). Conocido: tras Escape, el clic que recaptura el cursor también golpea (requiere tocar Mecha/ControladorMecha).

## Resultado final del turno de día
- Todas las tareas de Tony hechas e integradas en tony/fase-1 (con push). Ninguna bloqueada.
- Pruebas en tony/fase-1: compilar OK, EditMode 51/51, PlayMode 64/64.
- main no se tocó (merge de T-001/T-002 a main sigue sin hacerse; el clasificador lo negó antes).
- Para la noche: revisar T-001, T-002, T-010, T-011, T-013, T-012; decidir el cambio de .gitattributes y los cambios fuera de carpetas de T-013.
