# T-427 Ataques rivales a tus zonas
Dueño: Tony | Rama: tony/T-427-ataques-zonas | Modelo: Sonnet 5.5 | MODO RÁPIDO (docs/plan-noche.md, paso 5; mejora 7 de docs/mejoras-pendientes.md)
Tony autorizó tocar todas las áreas y Core; no hizo falta Core.

## Objetivo
En el juego libre, cada cierto tiempo una pandilla rival ataca una zona tuya: aviso, marca en el mapa, tiempo límite, oleada; premio al defender, pérdida de respeto (y de la zona) si no.

## Qué se hizo
1. DirectorAtaques (Progresion/Prologo/Encargos/) con las reglas puras en ReglaAtaques y los números en DatosAtaquesZonas_Base (espera 420 a 780 s, más seguido con más zonas y calor, 150 s para llegar, 180 s de oleada, premio 300 Ƀ y +20 respeto, castigo -45, umbral 40).
2. Reutiliza de T-426: los rivales a puños (DirectorEncargos.ColocarRival), el objetivo con marca (ServicioObjetivos) y el aviso de la pantalla de historia. El tablero de encargos no ofrece nada durante un ataque y el ataque no ocurre durante un encargo.
3. ServicioTerritorio suma AjustarRespeto y Ceder (sin cambiar Core). SUBURBIOS no se ataca; sin zonas conquistadas además de SUBURBIOS no hay ataques.
4. Creador T427 (parte de Todo()): raíz GTA_T427 con un punto de ataque en EL TIANGUIS y otro en NEÓN ALTO.

## Pruebas
EditMode ReglaAtaquesPruebas (6) y PlayMode AtaquesZonasPruebas (1: historia, sin zonas, aviso y marca, defender, abandonar, pérdida de la zona). Captura docs/capturas/T-427-1.png.
