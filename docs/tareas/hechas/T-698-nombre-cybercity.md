# T-698 Nombre CYBERCITY
Dueño: Tony | Rama: tony/T-698-nombre-cybercity | Modelo: Sonnet 5.5

## Objetivo
El juego se llama CYBERCITY en todo texto visible, el .exe y el Product Name.

## Qué se hizo
- NombreJuego: Nombre "CYBERCITY", Ejecutable "CyberCity.exe". MenuInicio y PantallaCargando toman el logo de NombreJuego.Nombre; la versión dice "DEMO · CYBERCITY". PantallaTitulo ya usaba NombreJuego. Los créditos no muestran el nombre.
- ConstruirDemo: Product Name = NombreJuego.Nombre en AplicarPantalla y PrepararBuildCyberfix; este borra también Cyberfix.exe y Cyberfix_Data.
- LogoInicio: texto provisional con glitch; si existe Resources/LogoCybercity (PNG importado como Sprite en Assets/_Proyecto/UI/Resources/LogoCybercity.png) lo usa en lugar del texto (AsignarSprite, campo logoPng).
- Sin tocar namespaces, carpetas, ensamblados ni clases *Cyberfix (nombres de código).

## Criterios
- [x] Menú y carga muestran CYBERCITY (docs/capturas/s1c-T-698-menu.png, s1c-T-698-carga.png)
- [x] InicioPruebas (11) y NombreJuegoCybercityPruebas pasan
