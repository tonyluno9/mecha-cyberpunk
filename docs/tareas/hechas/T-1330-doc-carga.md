# T-1330 Doc (implantes, puerta, texto) y pantalla de carga

Rama tony/T-1330-doc-carga. Viene de docs/playtest-17nov-b.md (secciones 1 y 7). Sin compilar ni correr nada: lo verifica el orquestador.

## Hecho
- Implante de misiles / "mano robótica": la hombrera de misiles (IMP05) y el brazo láser (IMP04) sí existen, pero se venden en la pestaña [TAB] PRÓTESIS DE ATAQUE de la clínica, y esa pestaña solo sale si hay una PantallaTienda en la escena. Ciudad.unity (que usa Barrio) no trae ninguna (solo Ciudad_Prologo), así que la pestaña no salía. Ahora MostradorClinica.prefab lleva su propia PantallaTienda y MostradorClinica la busca primero en sí mismo.
  Método de editor (hay que correrlo): `MechaCyberpunk.UI.Editor.TiendaEnClinicaT1330.Aplicar`.
- "ESPERA EN EL MECHA" ahora es "TIEMPO DE GOLPE Y GUARDIA (MECHA)": el efecto multiplica la preparación del golpe y el tiempo de subir la guardia del mecha (85 % = 15 % menos de espera; menos es mejor, se pinta como mejora). Cambiado en ConfigPantallaClinica.cs y su asset Base.
- Puerta del consultorio (PuertaSinHorario + HojaPuerta.MantenerAbierta): fuera siempre abierta y sin cerrarse sola; al entrar (EventosLugares.JugadorEntro Clinica) se cierra; con E se abre para salir; al salir vuelve a abrirse.
- Pantalla de carga: la foto se encaja entera (ReglaFotosCarga.TamanoEncajado, FitInParent), sin estirar ni recortar. Se quitó el zoom y el paneo (EscalaInicial/Final = 1, PaneoMaximo = 0) porque recortaban; el fundido entre fotos sigue.

## Pruebas
EditMode: FotosCargaT1330Pruebas, ClinicaT1330Pruebas (necesita haber corrido Aplicar), ReglaFotosCargaT1316Pruebas ajustada (sin zoom/paneo).
PlayMode: PuertaDocT1330Pruebas; ajustados FachadasPuertasS2pPruebas y PantallasT1316Pruebas.

## Pendiente
- Modelo de "mano robótica" aparte: no existe en el repo; se asume que es el brazo láser. Si es otra pieza, falta el arte.
- Si la pantalla de carga quiere zoom otra vez, tiene que ser dentro de los bordes negros.
