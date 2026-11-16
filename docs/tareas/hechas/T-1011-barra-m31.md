# T-1011: barra de prótesis como el mockup M31 (playtest 16-nov, punto 15)

- Cada casilla: arriba "[1] PIERNAS" (tecla real), en medio el dibujo de línea de la prótesis (nivel I o II) y abajo el medidor,
  como `imageassets/referencias/menus/M31_HabilidadesProtesis.png`. Lista turquesa, activa amarilla, recargando rosa, sin implante gris.
- Íconos en `UI/Resources/Protesis/` (blancos con alfa para teñirlos): piernas, brazos y cabeza I/II recortados de
  `imageassets/ui/menus/H41_IconosProtesis.png`; láser y misiles sacados de los dibujos IMP04/IMP05. Importador: `IconosProtesisT1011`.
- La barra ya admite más habilidades que las tres fijas: las de ataque (T-1017) salen solo si tienes la prótesis.
- Pruebas: BarraProtesisT1011Pruebas (nueva, en Ciudad_Prologo), HabilidadesProtesisT992Pruebas: 12/12 (1 ignorada: capturas de T-992).
- Captura revisada: `docs/capturas/t1011/barra-m31.png`.
