# T-990: avisos del Doc y de la silla en Ciudad_Prologo

**Causa (medida jugando Ciudad_Prologo con una prueba PlayMode):** la clínica I+D+I cierra de noche (`MostradorClinica.PuedeUsar` = false).
EL DOC solo ofrecía IMPLANTES si la clínica se podía usar y con vida llena; si no, decía "ESTÁS BIEN, MIJO" con `PuedeUsar=false` y el HUD H10 no
daba ayuda. Además la silla está a 0.96 m del Doc (doc x=-100.30, silla x=-101.26, z=-13.40) y `InteraccionAPie` elige el más cercano aunque no se
pueda usar: parado a 1.2 m delante de los dos, el Doc (sin ayuda) tapaba a la silla y el HUD solo mostraba `[Clic] Golpe`.
Descartado: Alcance (2.5 m) y ángulo (60°) no eran el problema (frente al Doc 1.50 m y 3.1°; junto a la silla 0.29 m).

**Decisión (coordinador):** EL DOC está presente, así que con la clínica en la escena SIEMPRE ofrece "IMPLANTES" (día o noche, vida llena o baja) y la E
abre la clínica aunque el mostrador diga "cerrado" por horario. Curar es trabajo de la silla (ya no hay "CURARTE" en el Doc cuando existe la clínica,
por eso se cambiaron las pruebas que lo esperaban).

**Arreglo:**
- `ReglaPersonajeApoyo.DocConImplantes(curar, clinicaDisponible)`: con clínica siempre IMPLANTES; sin clínica, el aviso de curar.
- `MostradorClinica.PuedeAbrirDesdeDoc` y `AbrirDesdeDoc()`: se saltan el horario pero respetan abierta/operando, jugador a pie y `ServicioImplantes`.
- `PersonajeApoyo`: usa lo anterior y la E abre con `AbrirDesdeDoc()`.
- Silla: "CURARME · Ƀcosto" herido y "CURARME" sano (default del campo y asset `ConfigSillaClinica_Base`, cambiado con SerializedObject en batch).
- Con el Doc siempre usable ya no se necesitó tocar `InteraccionAPie` (un primer arreglo allí se revirtió: ya no hay causa que lo pida).

**Pruebas:** `AvisosDocSillaT990Pruebas` (de noche, clínica cerrada): Doc -> HUD "[E] IMPLANTES" y la E abre la clínica; silla -> "[E] CURARME" y la E
te sienta (`EnSecuencia`). Cambiadas: `ReglaPersonajeApoyoPruebas.DocConImplantes_...`, `PersonajesApoyoPruebas.RecorridoProfes` (herido: Doc dice IMPLANTES,
la silla cura y cobra Ƀ50). `ImplantesDocPruebas` sin cambios (pasa).
Capturas: `docs/capturas/t990/doc.png` (de noche) y `silla.png`.
