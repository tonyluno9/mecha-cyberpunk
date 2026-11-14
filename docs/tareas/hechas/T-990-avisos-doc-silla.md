# T-990: avisos del Doc y de la silla en Ciudad_Prologo

**Causa (medida en el juego real, prueba PlayMode con Ciudad_Prologo):** la clínica I+D+I cierra de noche. Con la clínica cerrada,
`MostradorClinica.PuedeUsar` es false, así que EL DOC (con vida llena) dice "ESTÁS BIEN, MIJO" con `PuedeUsar=false` y el HUD H10 no da
ayuda. Además la silla está a 0.96 m del Doc (doc en x=-100.30, silla en x=-101.26, z=-13.40) y `InteraccionAPie` elegía el más cercano aunque
no se pudiera usar: parado a 1.2 m delante, entre los dos (jugador en -100.10, -12.20; Doc 1.2 m, silla 1.67 m, ángulos 14.9°), el candidato era
el Doc ("ESTÁS BIEN, MIJO", PuedeUsar=False) y la silla, usable, quedaba tapada: ayudas = `[Clic] Golpe` y nada más.
Con la clínica abierta (hora 12) y parado a 1.5 m frente al Doc, ya salía "[E] IMPLANTES" (Alcance 2.5 m, ángulo 60°, no era el problema).
Descartadas: Posicion fuera de alcance/ángulo (1.50 m y 3.1° frente al Doc; 0.29 m junto a la silla) y HUD sin ayudas (sí las muestra).

**Arreglo:** `InteraccionAPie.ElegirCandidato(soloUsables)`: si el candidato es un interactuable que no se puede usar, se vuelve a elegir
ignorando los no usables; si no hay otro, se queda el original (para que siga el aviso/negación). Sin cambios en Core, Mundo ni escenas.
Decisión: el texto de la silla se deja como está en el asset ("SENTARTE · CURARTE $x" herido, "SENTARTE · REVISIÓN GRATIS" sano; cambiarlo
a "CURARME" es editar el asset de Progresion/Apoyo/Datos). El Doc sigue diciendo exactamente "IMPLANTES".

**Pruebas (PlayMode, `AvisosDocSillaT990Pruebas`):** frente al Doc el HUD contiene "[E] IMPLANTES" y la E abre la clínica; junto a la silla
contiene "[E] SENTARTE · REVISIÓN GRATIS" y la E empieza la secuencia (`EnSecuencia`); de noche, entre el Doc y la silla, gana la silla
(falla sin el arreglo, pasa con él). Capturas: `docs/capturas/t990/doc.png` y `silla.png` (CAPTURAR_T990=1).
EditMode completo 2383/2383. PlayMode filtrado 48 pruebas, 1 falla ya en base (`HudH10Pruebas.Estrellas_SinCalorSonContorno_...`).
