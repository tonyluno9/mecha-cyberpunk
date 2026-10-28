# Estado de Alan (modo automático)

Actualizado: 2026-10-27. Ramas: alan/fase-5 (integración actual, sale de main) y alan/T-093-recorrido-prologo.

**En main (5c6e986):** A-F1 a A-F4 completas, es decir T-020 a T-099 salvo T-084 y T-085 (A-F2, denegadas por el modo Auto: no se intentan por otra vía),
y T-092. Lo de Tony que Alan esperaba (T-F4, T-F5, T-F6, prefabs del prólogo) también está en main.

**T-093, T-300, T-330 y T-340 hechas** (2026-10-27, en alan/T-093-recorrido-prologo y alan/fase-5, todavía no en main): `Ciudad_Prologo.unity` con el prólogo de Tony colocado,
`Prologo_Esquina_2`, `Flashback_Plano_1` a `_6` y `Flashback.prefab` en `Flashback.unity`, y `RecorridoPrologoCiudadPruebas` (3 pruebas con entrada simulada).
Verificado: compilar OK, EditMode 914/914, PlayMode 889/898 (0 fallos; las 9 omitidas son capturas de Tony que se activan con variable de entorno).
Qué falta para el hito H6 del lado de Tony: T-136 (que apunte `DatosEscenas_Demo` a `Ciudad_Prologo.unity`). Ver docs/revision-alan.md, sección T-093.

| Tarea | Estado |
| --- | --- |
| T-074 a T-083 (A-F2) | en main |
| T-084 Tráfico mínimo de fachada, T-085 Modelo del carro viejo | pendientes: el modo Auto las denegó; no se hacen |
| T-086 a T-092, T-094 a T-099 (A-F3 y A-F4) | en main |
| T-093 Recorrido del prólogo | hecha, falta mezclar a main (aprobación de Alan) |
| T-300, T-330, T-340 (lógica pura de casino, peatones y persecución) | hechas, en alan/fase-5 |
| T-301 Tragamonedas jugable | hecha (2026-10-27), en alan/fase-5; Casino PlayMode 9/9, suite completa 898/907 sin fallos antes del ajuste visual final |
| T-302 Fichas y cajera | hecha (2026-10-27), en alan/T-302-fichas-cajera; compilar OK, EditMode 952/952, PlayMode 909/918 (9 omitidas, 0 fallos). Fichas son un servicio aparte (ServicioFichas.prefab): colócalo en una escena siempre cargada para que se guarden (clave `fichas`) |
| T-303, T-320, T-321, T-331, T-341 | pendientes (T-321 espera el mostrador de la clínica de Tony) |
| T-136 (de Tony) | bloqueada solo por el cambio de datos de escena de Tony |
| T-310, T-311 (visuales) | en otras sesiones/worktrees (mecha-cyberpunk-alan-visual y -fachadas); no tocar desde aquí |

Fecha de corte del proyecto: desde el 2026-11-12 no se empiezan tareas nuevas.

## T-320 (WIP, 2026-10-28): resultado de la corrida de PlayMode
- Corrida completa y válida (no cortada): Logs/resultados-playmode.xml, 936 pruebas, 926 pasaron, 1 falló, 9 omitidas; EditMode 952/952; compila.
- Única falla: `CerradosPruebas.Cerrados_NoTapanLaEntradaNiInvadenLaCalle`: "MercadoNegro tiene su decoración. Expected: greater than 8, But was: 8" (CerradosPruebas.cs:92).
- Causa probable: el grupo exterior de decoración del Mercado en Barrio.prefab solo tiene 8 piezas. Arreglarlo exige regenerar Barrio.prefab / Ciudad.unity, que están bloqueados hasta que main muestre el merge de tony/T-250. No se debilita la prueba para pasarla.
- Estado: T-320 queda WIP, NO integrada ni aprobada. Siguiente paso: tras el merge de T-250, regenerar el Mercado y repetir PlayMode.

## T-321 (WIP, 2026-10-28): clínica del mercado negro, código listo, sin regenerar
- Rama alan/T-321-clinica (sale de T-320). `CreadorMercadoNegroInterior` ahora arma la clínica (tabique con vano, camilla, luz fría, `Lugar_Clinica` con `IdLugar.Clinica` y horario 9 a 18, punto vacío `Clinica_Mostrador`) y `GeometriaClinica` guarda las medidas. Se quitó la pila de cajas izquierda del fondo para dejarle el rincón.
- Asset `DatosLugar_Clinica.asset` creado con batch (no toca Barrio.prefab). El prefab de Tony (`MostradorClinica`) no existe aún: el punto queda libre.
- Evidencia: compila; EditMode 957/957 (incluye `ClinicaPruebas`, 5 pruebas de datos y geometría).
- NO verificado: el creador no se ha corrido (escribe Barrio.prefab, bloqueado hasta Merge tony/T-250), así que faltan PlayMode, captura y `verificar.sh todo`. No integrada.

## T-321 (2026-10-28): clínica del mercado negro, verificada
- Se mezcló origin/main (trae T-250 y `UI/Prefabs/MostradorClinica.prefab` de Tony). El creador instancia ese prefab junto a `Clinica_Mostrador` (sin editarlo); el punto queda libre y sobre piso (y = 0.04). Se regeneró todo con `CreadorTodo.Crear`.
- Evidencia: `bash herramientas/verificar.sh todo`: compilar OK, EditMode 1134/1134, PlayMode 1015/1024 (0 fallos, 9 omitidas). Prueba nueva `Clinica_EstaRegistrada_AbreDeDia_ConMostradorDeTonyYPuntoSobrePiso`.
- Ajustes de pruebas: `LugaresPruebas` espera 5 locales con puerta + la Clínica como ILugar; `KitCallePruebas` ignora la Entrada de `Lugar_Clinica` (está dentro del cuarto, no da a la calle); se agregó `FarolPuerta` al exterior del mercado (arregla la falla de T-320 "más de 8 piezas").
- Límites: la clínica NO tiene `IPuerta` propia (se entra por el vano del tabique, sin hoja); no hay captura visual específica de la clínica (las pruebas no la capturan y no se hizo captura manual); no se probó el uso real del mostrador con entrada simulada en esta tarea (es de Tony, T-193).
