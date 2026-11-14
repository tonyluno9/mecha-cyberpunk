# T-995 Recorrido por la ciudad: flotantes, encimados y texturas rotas

Recorrido de Ciudad_Prologo buscando piezas que floten, estén encimadas o tengan textura rota.

## Qué se encontró (antes de arreglar)
- **Flotantes**: 0 (FlotantesS2tPruebas ya pasaba; T-859 los dejó limpios).
- **Texturas rotas**: 0. Ningún renderer activo con material nulo, shader de error o no soportado, `Default-Material`, textura con guid roto, ni material liso blanco sin `_BaseMap` en piezas del kit.
- **Encimados**: 5 reales (3 se apagaron al final, ver abajo). La primera pasada por cajas de pieza dio 285 falsos positivos (fachadas con farolas/puertas, postes cuya caja incluye el brazo y un `Halo` de luz de 7 m); se afinó a comparar malla contra malla (renderer contra renderer, >50 % del volumen del menor, ignorando halos y renderers de <0.15 m) y a no comparar estructura (fachadas, pisos, etc.) entre sí. Quedaron:
  - `T512_LuzZonas/Mercado/N01_T512` (farola) metida en `C12_PuestoTacos` (Puesto_Tacos_3).
  - `S2_Callejon/N06` dentro de `L01_Contenedor` (se deja, ver abajo) (callejón de Barrio/Manzana_4).
  - `S2_Callejon/C14_RegistroMedidores` detrás de `T502_Callejon/Piezas/N04_T502` (100 % dentro; se deja, ver abajo).
  - Dos tinacos `C09_Tinaco` encimados: `S2_Calles/Salida_4/Kit_72` (mismo padre, dos tinacos) y `S2c_CalleDorada/Casas/Casa_21` contra Casa_10 (60 %).

## Qué se arregló
- Apagadas (SetActive(false), la pieza menor de cada par): N01_T512 del tianguis, un C09_Tinaco de Kit_72 y el de Casa_21. Después: encimados 0, texturas rotas 0.
- N06, L01_Contenedor y C14_RegistroMedidores del callejón S2_Callejon se dejaron (excepción `S2_Callejon/`): hacen de escalón a los accesos a techos que prueba TechosS2Pruebas.
- Ciudad_Prologo.unity guardada con solo esos 3 cambios.

## Qué se dejó y por qué (lista `ParejasPermitidas` en el código)
- Postes y transformadores (su malla es un brazo largo; la caja está casi vacía), focos bajo un toldo (FocoPared), aire acondicionado sobre su base del tinaco (BaseTinaco), basura regada junto al bote y el banco de Tino (TallerTino, no se toca).
- Fachadas y pisos se apilan a propósito: no se comparan entre sí. Las casas de 10.2 m de fondo en Calle Dorada son intencionales (no hay callejón en ese tramo; una foto desde dentro de su cuerpo lo parecía).
- Sin arte nuevo pendiente.

## Archivos
- `Assets/Arte/Editor/RevisarCiudadT995.cs`: `Encimados()`, `TexturasRotas()`, `Informe`, `Arreglar` (idempotente, guarda la escena, informe en `Logs/t995-revision.txt`). Batch: `-executeMethod MechaCyberpunk.Arte.Editor.RevisarCiudadT995.Informe|Arreglar`.
- `Assets/_Proyecto/Tests/EditMode/Mundo/RevisarCiudadT995Pruebas.cs` (2 pruebas).
- `Assets/_Proyecto/Tests/PlayMode/Personaje/RecorridoCiudadT995Pruebas.cs`: 13 fotos a 1.7 m en docs/capturas/t995/zona-01 a zona-13 con `T995_CAPTURAS=1` (ignorada sin la variable).
- `Assets/_Proyecto/Escenas/Ciudad_Prologo.unity`.

## Criterios
- [x] Sin piezas encimadas ni duplicadas: RevisarCiudadT995Pruebas.Ciudad_SinPiezasEncimadas.
- [x] Sin texturas rotas: RevisarCiudadT995Pruebas.Ciudad_SinTexturasRotas.
- [x] Sin flotantes: FlotantesS2tPruebas 1/1 tras los cambios.
- [x] Recorrido visual: las 13 capturas se revisaron una por una (Suburbios, Calle Dorada, Mercado, Barrio, Neón Alto, Nova, casino por fuera, callejón): nada flotando, encimado ni rosa/liso. Dos tomas dieron contra una pared y se movieron de punto.
- [x] PlayMode (cada clase por separado): TechosS2Pruebas 6/6; PasarelaNovaT996Pruebas 3/3 (+1 de capturas ignorada); PandillasCiudadS2Pruebas 2/2. Juntas en una sola corrida TechosS2Pruebas falla 3/6 (Acceso_S2c_Suburbios_01/02 y parkour del contenedor) **también en origin/main sin mis cambios**: dependencia de orden previa, no de T-995.
- [x] EditMode: RevisarCiudadT995Pruebas 2/2, FlotantesS2tPruebas 1/1, SinGreyboxT506Pruebas 9/9, PuentesS2tPruebas 1/1, PiezasCalleT910Pruebas 6/6. `verificar.sh compilar` OK.
