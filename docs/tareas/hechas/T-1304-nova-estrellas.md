# T-1304 · Estrellas altas = Nova (playtest 17-nov, punto 4)

- `Core/Calor/EstrellasBusqueda`: la regla de las 5 estrellas (la misma del HUD) vive en Core; con 4 o más viene Nova.
- HUD H10: con 4-5 estrellas se pintan de azul intenso (`ReglaEstrellasH10.AzulNova`).
- `IA/APie/RespuestaNova` (arranca sola si existe `IA/Resources/ConfigRespuestaNova`; solo actúa donde hay policía): con 4 estrellas aparecen
  3 guardias robot de Nova armados y 1 guardia pesado a 22-34 m del jugador, en su mismo piso si se puede, en alerta y con su punto en el
  jugador (lo persiguen); se reponen cada 25 s. Con 5 estrellas son 5 guardias y 2 pesados, y llega el jefe final E14 (una vez por búsqueda).
  Bajo 4 estrellas 6 s, se retiran (los caídos se quedan tirados).
- Los bots de Nova ya atacaban si les disparas (T-1009, `seDefiende`); los que trae la respuesta van siempre en alerta.
- Editor: `MechaCyberpunk.Arte.Editor.CrearRespuestaNovaT1304.Aplicar` arma `IA/Prefabs/GuardiaNova_Armado.prefab` y la config.
- Prueba: `RespuestaNovaT1304Pruebas` (PlayMode IA).
