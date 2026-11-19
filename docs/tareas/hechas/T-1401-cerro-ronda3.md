# T-1401 Colonia del Cerro, ronda 3

Script: Assets/Arte/Editor/ZonaCerroT1401.cs. Corre DESPUES de ZonaCerroT1394 (que corre despues de T1385). Metodos: ZonaCerroT1401.Aplicar, .Validar, .Fotos (corrida aparte).
Sin verificar en Unity todavia (escrito sin Library): el orquestador corre y devuelve errores.

- Cables: revisa todas las mallas de cables del prefab (T1355, T1385, T1394), cable por cable; quita los de mas de 40 m, flecha de mas de 3 m, NaN, bajo el piso,
  triangulos deformes (arista > 3 m o aro > 0.3 m), a menos de 1 m de K13 o colgados de un poste quitado. Malla nueva en Mallas/T1401_Cerro.asset.
- Postes: quita (guardados en T1401_Cerro/Reemplazados) los que caen en el centro del encuadre de K13 (a < 4 m, o delante < 14 m y a < 3.5 m del eje).
- Enlaces entre azoteas: escalones con M_T09_ConcretoManchado y tablones con T506_Madera_Vieja, malla a tamano real con UV en metros, collider igual a la malla.
- Azoteas visibles desde K13: tinacos (C09_Tinaco sobre TK01_BaseTinaco), antenas CY04 y tendederos CC04 donde aun no hay.
- Idempotente: Aplicar devuelve los postes y rehace T1401_Cerro. Si se vuelve a correr T1394 hay que volver a correr este script.
