# T-1309: Sonidos por arma, chispeo del plasma y rayo más realista

Origen: docs/playtest-17nov.md punto 9. Rama `tony/T-1309-sonidos-armas` (sale de `tony/T-1308-recarga-animada`).

## Cómo se asignan los sonidos aquí
Las armas no guardan el clip en `DatosArma`: piden un ID por código (`PistolaAPie.IdDisparo`) y el `CatalogoSonidos`
(`Progresion/Datos/CatalogoSonidos.asset`) lo traduce a clips, con un interruptor por ID. Por eso la asignación se hace en el catálogo.

## Un disparo distinto por arma
| Arma | ID | Clip |
| --- | --- | --- |
| Pistola | ARM_PIS_DISPARO | sin cambio |
| Escopeta | ARM_ESC_DISPARO | sin cambio |
| Fusil | ARM_FUS_DISPARO | sin cambio |
| Sniper | ARM_SNI_DISPARO | ahora solo ARM_SNI_DISPARO_02 |
| Subfusil | ARM_SUB_DISPARO | nuevo ARM_SUB_DISPARO_01.wav (se quitó el alias que lo mandaba al fusil) |
| Revólver | ARM_REV_DISPARO | copia exacta de ARM_SNI_DISPARO_01 (el sonido que le gusta al jugador no cambia) |
| Lanzagranadas | ARM_LGR_DISPARO | nuevo ARM_LGR_DISPARO_01.wav |
| Rifle de plasma | ARM_PLA_DISPARO | nuevo ARM_PLA_DISPARO_01.wav (antes usaba el del sniper) |

Los WAV nuevos son procedurales (numpy, sin internet) y están en Assets/Arte/Audio/ARM/ SIN .meta: Unity los importa solo
(ImportadorAudio los deja en mono/Vorbis) y crea los .meta al abrir el proyecto.

## Paso manual obligatorio (editor)
`MechaCyberpunk.Personaje.Editor.SonidosArmasT1309.Aplicar` (también en el menú Mecha Cyberpunk/T-1309 Asignar sonidos de armas):
importa los WAV, los asigna al catálogo y prende sus interruptores. Después hay que commitear los .meta nuevos y el catálogo.
Hasta entonces las pruebas de catálogo salen como Ignoradas y las armas nuevas no suenan (el ID no tiene clip).

## Chispeo al cargar el plasma
`ArmasEspecialesAPie`: al empezar a cargar arranca el bucle `ARM_PLA_CARGA` (sigue a la boca); se corta al empezar el rayo, al soltar,
al cancelar o al cambiar de arma (`ApagarPlasma`). Antes sonaba un solo bip (ARM_LASER_AVISO, que sigue para guardias y pandilleros).
Política de mezcla en `ReglaMezclaSonidos` (volumen 0.5).

## Rayo de plasma
Nuevo `EfectoRayoPlasma` (reemplaza el LineRenderer simple): núcleo blanco + halo cian más ancho, textura de gradiente generada por código,
material URP Particles/Unlit aditivo (el PlasmaRayo.mat existente, copiado; flags RealtimeEmissive), ancho que pulsa, zigzag leve con puntos
cada ~1.6 m (`ReglaZigzagRayo`, pura), fundido de 0.15 s al terminar y chispas (ParticleSystem) cuando el rayo pega en pared u objeto.

## Drones
Los drones están en Trafico/ (Alan: `DronMundo`); en IA/ no hay dron de Tony. El zumbido de motor queda pendiente para la otra área
(un AudioSource 3D en bucle o ReproducirBucle con un ID nuevo, p. ej. CAL_DRON_MOTOR).

## Pruebas
- EditMode `Tests/EditMode/Personaje/SonidosArmasT1309Pruebas.cs`: IDs distintos, clips distintos en el catálogo, zigzag, textura.
- PlayMode `Tests/PlayMode/Personaje/ChispeoPlasmaT1309Pruebas.cs`: el chispeo suena durante la carga y se corta.

## Sin verificar
No se compiló ni se corrió nada (sin Unity en el worktree). Revisar a ojo y de oído los WAV generados; el aspecto del rayo.
