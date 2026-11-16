# T-1112: arreglos de la calle del Doc (T-1102)

## 1. LuzK01S2oPruebas.Postes_ElNaranjaQuedaBajoCadaPoste
- Causa: los 6 postes que coloca `ColocarT1102CalleDoc` (`T1102_CalleDoc/Postes/Poste_N/Luz`) salían de `KitS2c.Poste` con luz de 20 m de alcance; los postes de la ciudad llevan los valores de `ColocarS2oLuz` (alcance 9 m, intensidad 40, ángulo 75). La prueba está bien por diseño (alcance corto de los postes), no se tocó.
- Arreglo: `ColocarT1102CalleDoc.AjustarLucesPoste` aplica esos valores al colocar cada poste, y `AjustarLucesEnEscena` (menú T-1112 / `-executeMethod`) los aplicó a la escena existente sin rehacer la calle. Solo cambian esas 6 luces de Ciudad_Prologo.

## 2. DemoDePrincipioAFin se atoraba en el tramo 09 EL INVENTOR en (-101, -13)
- No era un collider ni una casa de T1102_CalleDoc: se midió con OverlapBox alrededor de (-101, 1.2, -13) y todo lo que hay son piezas del consultorio del Doc (I07, paredes del forro, muebles) que ya estaban. (-101, -13) es el interior del consultorio.
- Causa real: desde T-990 el Doc abre la clínica (IMPLANTES) con E, lo cual apaga el control del jugador (log: `control False`, velocidad 0, trabas 1 a 5 en el mismo punto). El bot terminaba el tramo 08 con la clínica abierta y no podía caminar al CIC. Antes (T-988) el Doc solo curaba.
- Arreglo (prueba, no colocador): `RecorridoDemoPruebas` cierra la clínica con Esc tras usar al Doc si el control sigue apagado.

## Pruebas
- `RecorridoDemoPruebas` (PlayMode): 3 de 3 pasan, incluida `DemoDePrincipioAFin` (Unity salió con 139 al cerrar, después de escribir el resultado; es el cierre habitual de esta corrida larga).
- EditMode Mundo: 365 total, 364 pasan. `LuzK01S2oPruebas` completa y `CalleDocT1102Pruebas` (12) pasan.
- La única falla es `NeonAltoT505Pruebas.EntradaDelCasino_TieneFocosTubosCargadoresYMoto` (cargadores CY10 apagados por T-996): ya falla en main y la arregla T-1104 (rama tony/T-1104-quitar-pasarelas, que vuelve a encender esos cargadores); no tiene que ver con esta tarea.

## Archivos
Assets/Arte/Editor/ColocarT1102CalleDoc.cs, Assets/_Proyecto/Escenas/Ciudad_Prologo.unity (6 luces), Assets/_Proyecto/Tests/PlayMode/Demo/RecorridoDemoPruebas.cs.
