# T-1472 Sonidos de armas, impactos por material y explosiones

Rama: tony/T-1472-armas-sonidos. Sin compilar ni correr pruebas en este worktree (no hay Library): lo compila y corre el orquestador.

## Tabla arma / evento -> ID

| Arma o cosa | Evento | ID | 2D / 3D |
| --- | --- | --- | --- |
| Pistola W01 | disparo | ARM_W01_DISPARO | 2D |
| Pistola | recarga | ARM_PIS_RECARGA (sin archivo nuevo) | 2D |
| Escopeta W02 | disparo | ARM_W02_DISPARO | 2D |
| Escopeta | tras cada tiro (0.45 s) | ARM_W02_BOMBA | 3D |
| Escopeta, lanzagranadas | recarga | ARM_ESC_RECARGA_CARTUCHO | 2D |
| Fusil W03 | cada bala de la ráfaga | ARM_W03_DISPARO (repite el tiro) | 2D |
| Fusil | al soltar o acabarse la ráfaga | ARM_COLA_CALLE (una vez, ReglaColaRafaga) | 2D |
| Fusil, subfusil, plasma | recarga | ARM_W03_RECARGA_CARGADOR | 2D |
| Francotirador W04 | disparo | ARM_W04_DISPARO | 2D |
| Francotirador | tras cada tiro (0.5 s) | ARM_W04_CERROJO | 3D |
| Francotirador | recarga | ARM_SNI_RECARGA (sin archivo nuevo) | 2D |
| Subfusil W05 | disparo | ARM_SUB_DISPARO (T-1471 lo suena con W01 agudo) | 2D |
| Revólver W06 | disparo | ARM_W06_DISPARO | 2D |
| Plasma W07 | carga (bucle mientras carga) | ARM_W07_CARGA | 3D, sigue a la boca |
| Plasma | al soltar el rayo | ARM_W07_DISPARO | 2D |
| Plasma | rayo continuo (bucle, se corta al acabar, soltar o cambiar de arma) | ARM_W07_RAYO | 3D, sigue a la boca |
| Lanzagranadas | disparo | ARM_LGR_DISPARO (se queda, sin archivo nuevo) | 2D |
| Toda arma de mano (no puños) | sacar | ARM_W06_SACAR | 2D |
| Puños | cambiar | ARM_CAMBIAR | 2D |
| Granada | quitar seguro y lanzar | ARM_GRANADA_SEGURO | 2D |
| Granada | rebote (mínimo 0.15 s entre rebotes) | ARM_GRANADA_REBOTE | 3D |
| Granada, lanzagranadas | explota | EXP_GRANADA (tope en cadena) | 3D |
| Prótesis láser del jugador | activar | IMP_LASER_DISPARO | 2D |
| Prótesis misiles | fijar blancos | IMP_MISILES_FIJAR (solo si hay blancos) | 2D |
| Prótesis misiles | lanzar | ARM_W02_DISPARO | 2D |
| Pandillero y policía armados | disparo (1 perdigón) / (varios) | ARM_W01_DISPARO / ARM_W02_DISPARO | 3D en la boca |
| Bala enemiga que falla con línea libre al jugador | pasa cerca | IMP_BALA_ZUMBIDO | 3D |
| Bala del dron que roza al jugador | roce | IMP_BALA_ZUMBIDO (además del ARM_BALA_ROZA silenciado) | 3D |
| Carro, barril, dron que explotan | explosión | EXP_ONDA (tope en cadena) | 3D |
| Carro que arde (humo y fuego, o ya explotado) | bucle mientras arde | FUE_LOOP_CHICO (se corta al apagarse, desactivarse o destruirse) | 3D, sigue al carro |

## Impactos de bala del jugador (un sonido por tiro, 3D en el punto)

Regla pura en `ReglaMaterialImpacto` (Personaje/Combate), por componentes y nombres (collider, raíz, material del renderer):
robot (nombre con dron, robot, mecha, guardianova) -> IMP_BALA_ROBOT; persona (IRecibeDanio no inerte) -> IMP_BALA_CABEZA si la zona es cabeza, si no IMP_BALA_CARNE;
nombre con vidrio, cristal, ventana, glass, parabrisas, window -> IMP_BALA_CRISTAL; IMontable en el collider o nombre con metal, contenedor, lamina, acero, reja, poste -> IMP_BALA_METAL; el resto -> IMP_BALA_CONCRETO.
Antes pegarle a una persona no sonaba; ahora suena IMP_BALA_CARNE o CABEZA (prueba DisparoLimpioT676 actualizada).

## Archivos

- Nuevos: Personaje/Combate/SonidosArmasT1472.cs (IDs por arma, ReglaColaRafaga, LimiteExplosionesSonido), Personaje/Combate/ReglaMaterialImpacto.cs, Vehiculos/Efectos/LimiteSonidoExplosion.cs (el mismo tope para Vehiculos y Trafico, que no pueden referenciar Personaje).
- Tocados: PistolaAPie, ArmasAPie, ArmasEspecialesAPie, ProyectilAPie, ExplosionAPie, ProtesisAtaqueAPie (Personaje); PandilleroAPie, PistolaPandillero (IA); DanioCarro, ExplosionCarro, BarrilExplosivo (Vehiculos); DronMundo (Trafico).
- Pruebas EditMode: Tests/EditMode/Personaje/SonidosArmasT1472Pruebas.cs, Tests/EditMode/Vehiculos/LimiteSonidoExplosionT1472Pruebas.cs.
- Pruebas existentes cambiadas porque los IDs cambiaron: ArmasSonidoT593Pruebas y SonidosArmasT1309Pruebas (IDs nuevos; se quitó la comparación del revólver con el clip viejo y el subfusil se salta en la prueba de clips distintos porque suena como la pistola); PlayMode DisparoLimpioT676Pruebas (W01 y el impacto de carne), RecorridoRonda2Pruebas (ARM_W02_DISPARO), ExplosionCarroPruebas (EXP_ONDA).

## Falta

- ARM_NOVA_DISPARO, ARM_NOVA_CARGA_DISPARO: no hay arma de Nova que dispare en el código (el guardia Nova solo golpea cuerpo a cuerpo); no se pidió nada.
- Recarga propia de pistola, francotirador y escopeta (se quedan con los IDs viejos), sacar propio por arma (todas usan W06_SACAR).
- Impactos de las balas de enemigos sobre el mundo (solo suena el zumbido).
- Los IDs viejos de los que quedan (ARM_PIS_RECARGA, ARM_ESC_RECARGA_CARTUCHO, ARM_SNI_RECARGA, ARM_LGR_DISPARO, ARM_BALA_ZUMBIDO del plasma, ARM_BALA_IMPACTO_METAL de los escudos) los cubre T-1471 o quedan sin archivo.
