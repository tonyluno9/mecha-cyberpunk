# T-1103 Edificios lejanos nítidos (torres de Nova y Neón Alto)

Rama tony/T-1103-torres-nitidas. Pedido de Tony (punto 12).

## Qué estaba mal
Con la niebla ExponentialSquared 0.006 de la escena, a 150 m queda 44% del color y a 300 m 4%: las torres se volvían una mancha azul sin luces ni letrero.

## Qué se hizo
- Shader `Assets/Arte/Suburbios/Shaders/TorreNitida.shader` (URP, SRP Batcher, sombras, Forward+): usa la niebla de Unity pero con la distancia multiplicada por `_FactorNiebla` (0.3) y tope `_NieblaMaxima` (0.45); emisión de ventanas que sube con `_T501Noche`.
- Shader `TorreHaz.shader` (archivo HazTorre.shader): haz aditivo vertical. Se llama TorreHaz para que SinGreyboxT506 lo cuente como efecto.
- `Assets/Arte/Editor/ColocarT1103Torres.cs` (idempotente): materiales `*_Nitida` en `Assets/Arte/Suburbios/Materiales/T1103/` para T20-T23, NV01, NV02 y S2p_NovaCuerpo, asignados solo en T541_Torres (Torre_*) y los edificios de S2c_Nova. Raíz nueva `T1103_Luces`: letrero NOVA emisivo (textura generada `T1103_Nova.png`), haz de 100 m sin colisión ni sombras, y luces rojas de aviso (4 por torre, 14 torres, con ParpadeoNeon existente). Cero luces reales nuevas.
- No se tocó RenderSettings ni CicloDiaNoche.

## Criterios
- [x] Shader compila, sin errores ni avisos en el log; Shader.Find e isSupported (CiudadTorresT1103Pruebas).
- [x] EditMode CiudadTorresT1103Pruebas 4/4, AssetsNuevosT540Pruebas 5/5, RevisarCiudadT995Pruebas 2/2, SinGreyboxT506Pruebas 9/9; `verificar.sh compilar` OK.
- [x] Capturas en docs/capturas/t-1103/ (antes-tianguis-*, tianguis-*, casa-*, calle-nova-*, neon-alto-*, noche y luz) con CapturasTorresT1103Pruebas (T1103_CAPTURAS=1; T1103_ETAPA=antes para las de antes).

## Notas
- Las torres de horizonte de ColocarT505 están apagadas por T-541 (grupo Torres inactivo), no se tocaron; TorreHorizonte ya tiene su propia niebla.
- La torre principal de Nova es la más alta de la calle (techo 36.5 m, centro x=110); los edificios de Nova son bajos comparados con los de Neón Alto.
- Pendiente: el letrero se lee mejor de cerca que desde el tianguis (queda tapado por edificios).
