# T-1411: pruebas del EditMode contra el look y los personajes del 19-nov

Rama tony/T-1411-pruebas-look (sale de integ/ciudad-19nov). Hecha sin Unity (sin Library): nada de esto se compiló ni corrió; falta correr `Assets/Arte/Editor/ArreglosT1411.Aplicar()` y el EditMode.

Por correr, en orden: `MechaCyberpunk.Arte.Editor.ArreglosT1411.Aplicar` (menú Mecha Cyberpunk/T1411), después EditMode completo.

## Decisión por falla

| Falla | Tipo | Qué se hizo |
| --- | --- | --- |
| AjustesT1317.Postes_TienenLaIntensidadNueva, AjustesT1331.Postes_TienenLaLuzNueva | (a) cambio a propósito T-1392 | Los faroles de las zonas están en 400 y los de los barrios en 140: la prueba acepta cualquiera de las dos y exige alcance <= 22 en los de zona. `LookNocheT1392.IntensidadSodio` y `AlcanceSodio` ahora son públicas. |
| LuzK01S2o.Postes_ElNaranjaQuedaBajoCadaPoste | (a) T-1392 | Tope de alcance 22 (era 16). |
| RendimientoCerroT1365.Luces_..._18m | (b) conflicto real | La prueba de rendimiento guarda el tope del Cerro (T-1365). Se mantiene 18 m en el Cerro: `LookNocheT1392.Zona` pone 18 m (RendimientoCerroT1365.AlcanceMaximoLuz) al sodio del Cerro, y `ArreglosT1411.LucesDelCerro` baja las luces del prefab que pasen de 18. Las otras zonas se quedan en 22 m. |
| CieloDiurno.Noche_ConservaLosValoresDeAntes | (a) T-1399 y T-1392 | Niebla de noche 0.009, color (0.34, _, 0.57), ambiente r 0.46. |
| LuzColorT507.Ambiente_DeNoche... | (a) T-1392 | Ambiente máximo 0.66: límite de la prueba 0.6 a 0.7. |
| LuzK01S2o.Noche_NieblaAzulDelgadaYCieloLiso | (a) T-1399 | Domo de nubes con opacidad 0.6 (antes 0). |
| CarrosPorPiezasT742 V01a, V01b, V05 | (a) T-1380/T-1397 | Ahora la prueba exige PuertasCarro y Puerta_I; la patrulla sigue sin puertas. |
| RuedasAlHuecoT1007 Patrulla (0.799) | (b) | `ArreglosT1411.Aplicar` corre `MedirRuedasT1007.Ajustar` (re-ajusta las llantas al 90 % del hueco). |
| CarrosS2n.Trafico_TraeVariantesDeColorCiviles (V02 Rojo) | (b) | `Aplicar` rehace las variantes (`ColocarS2nCarros.RehacerVariantes`, como T-1312), y `CrearVariante` ahora, si el carro rearmado ya no usa el material esperado, tiñe el material "Carroceria". |
| AnimacionesMixamo y ClipsBlenderPJ01 (Rifle_Idle) | (b) de esta noche | Lo causó T-1389b (commit ef22967): `CreadorVisualTripo.AnimacionesExtra` reconfiguraba los FBX de las subcarpetas de Animaciones/ y dejó Rifle_Idle.fbx.meta con el clip llamado ApuntarLarga. Se restauró ese .meta al de antes del commit y `AnimacionesExtra` ahora solo toca FBX sueltos de la carpeta raíz (en las subcarpetas no hay, así que ya no reescribe los clips compartidos). |
| NpcClipsPJ01T1004 (E15, E16, E18, E19, E20...) | (b) T-1389b | Los visuales nuevos llevaban el controlador con clips de Mixamo. `PersonajesNuevosT1389b.ConectarClipsPJ01` les pone el override de su papel (Guardias, SKULLS o NPC) con `ConectarControladorBase.ConectarModeloConClipsPJ01`; se llama al crearlos y `Aplicar` también vuelve a armar peatones, Nova y SKULLS. El perro (Generic) no cambia. |
| ReglasEnsamblados (PlayMode.Mundo -> Personaje) | Anotada, no tocada | El asmdef lo cambió T-1361 el 18-nov (commit 0ea08f2) y ya está en main, no es de esta noche. Es de Alan; falta pasar RecorridoZonasT1361Pruebas a un contrato de Core. |

No tocadas (T-1410): ArreglosPuertoT1366, TodoBarril, Carros_NingunoEstaSobreObjetos, Toldos, ArreglosCerroT1363, FlotantesS2t, RevisarCiudadT995, SinGreyboxT506, LineasS2n, PiezasCalleT910.

## Riesgos sin verificar
- Que el hueco medido de la patrulla deje las cuatro llantas entre 0.8 y 1.02 (Ajustar usa 0.9).
- Que el valor de ambiente r a las 23:00 sea 0.46 (clave 0.88 del gradiente).
- Que el .meta nuevo de ArreglosT1411.cs sea aceptado tal cual (guid hecho a mano).
