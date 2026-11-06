# T-673 Agacharse
Dueño: Tony | Rama: tony/T-673-agacharse | Modelo: Sonnet 5.5

## Objetivo
CTRL agacha y levanta (alterna). Agachado camina lento; con SHIFT camina agachado más rápido; con ESPACIO rueda. Sin agacharse no se rueda. Agachado sirve para cubrirse de enemigos tras una cobertura baja.

## Qué se hizo
- CubiertaYRodada: CTRL alterna (techo impide levantarse), cápsula 1.0 m agachado, factores de velocidad (DatosCubierta: factorAgachado 0.35, factorAgachadoCorriendo 0.45), ESPACIO rueda solo agachado (ControladorAPie.SaltoBloqueado); la rodada ya no está en CTRL. Q cubrirse sigue igual.
- AnimacionJugador: quieto agachado usa la pose de Cobertura_Entrar/Mantener; caminando, AgachadoCaminar (Rifle_AgachadoCaminar). Disparar agachado usa las animaciones de asomarse de la cobertura.
- IA: los pandilleros apuntan al pecho según la cápsula del jugador (ReglaPandilleroAPie.AlturaPechoSegunCuerpo): agachado una cobertura baja tapa la vista.
- Textos de ayuda: "CTRL agacharse (+ESPACIO rueda)".

## Faltan animaciones
- Agachado quieto (idle) y rodada propia: se usan la pose de Cobertura_Entrar y el giro procedural del modelo.
