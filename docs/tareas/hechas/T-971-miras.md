# T-971: miras (B2)

Rama: tony/T-971-miras. Sin merge.

- Mira_PuntoRojo.fbx importado en Assets/Arte/Armas/W01_Pistola/ con 4 materiales propios (Mira_Metal, Mira_Acento, Mira_Vidrio transparente, Mira_Punto emisivo con RealtimeEmissive).
- Hijo `Mira` de la `Corredera` en W01_Pistola (banco, siempre visible) y W01_Pistola_agarre (mano, con `MiraSegunNivel` en la raíz: solo se ve con la pistola en nivel III; revisa el nivel cada 0.25 s porque el arsenal no tiene evento).
- Escala 1.126 en los dos (base de la mira = 25 % del largo de la corredera, 18.9 cm; mira de 3.3 x 3.0 x 4.7 cm). Se apoya en lo más alto de la corredera bajo su huella (no en los hierros de mira, que la dejarían flotando 7 mm).
- Banco: `piezaMira` = "Mira" en la pistola. `CrearMira` y `Primitiva` fuera de `VistaHologramaArma`.
- Editor: `ColocarAgarrePistolaT736.ColocarMirasPistola` (idempotente; `Colocar` también la deja). `CapturasMiraT971.Capturar`.
- Pruebas: EditMode `MirasT971Pruebas`; PlayMode `MiraSegunNivelT971Pruebas` y 2 nuevas en `BancoTodasArmasT942Pruebas`.
- Capturas en docs/capturas/t971/.
