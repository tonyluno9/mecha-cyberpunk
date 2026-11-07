# T-686 Botín de los enemigos
Dueño: Tony | Rama: tony/T-686-botin-enemigos | Modelo: Sonnet 5.5, medium
Depende de: T-402 (pandilleros armados), ArmaEnPiso

## Objetivo
Los enemigos sueltan su arma y su dinero al morir. El arma queda en el piso (ArmaEnPiso, se recoge con E) y el dinero se recoge al pasar. Matar y caer de los enemigos no cambia.

## Qué se hizo
- Core/Peleadores/EventosBotin.cs: aviso (posición, dinero, arma) para que IA no dependa de Personaje.
- IA: PandilleroAPie publica el botín después de caer (KO y Vencido siguen igual); el jefe suelta una escopeta, los demás una pistola. PandillaAPie ya no abona el dinero al caer.
- Personaje: ColocadorBotin (atiende el aviso), DineroEnPiso (fajo que flota, se recoge al pasar a 1.3 m, 0.6 s después de caer), DatosBotin (Resources/DatosBotin_Base con los modelos W01 y W02_Piso). No suelta una pistola si ya hay otra junto (la pistola de la historia). El botín desaparece a los 120 s.

## Criterios
- [x] Al caer suelta dinero (datos.Dinero) y arma, junto al cuerpo
- [x] El dinero se recoge al pasar y abona a la cartera; no se abona solo
- [x] El arma es un ArmaEnPiso que se recoge
- [x] El enemigo sigue cayendo en KO

## Pruebas
- EditMode: Tests/EditMode/Personaje/EventosBotinT686Pruebas.cs
- PlayMode: Tests/PlayMode/Personaje/BotinEnemigosT686Pruebas.cs; PandillaArmadaPruebas actualizada (el dinero se recoge)

## Para arte
- El fajo de dinero es un modelo provisional de 3 cubos con emisión; cambiarlo por un billete o ficha.
