# T-671 Arma sin apuntar: caminar y correr normal
Dueño: Tony | Rama: tony/T-671-arma-sin-apuntar | Modelo: Sonnet 5.5

## Objetivo
Con un arma equipada y sin apuntar el jugador camina y corre como sin arma y el arma va abajo en la mano. Las poses de arma solo al apuntar (deshace T-651).

## Qué se hizo
- VisualPersonaje.TipoArmaAnimador: el Animator solo ve el arma al apuntar o hasta 1.5 s después de disparar o recargar.
- Quitada la capa Piernas (controlador, máscara, reglas y constantes de T-651); controlador regenerado con ConstructorControladorBase.Construir.
- ApuntadoIK: la mano izquierda va al arma solo al apuntar (también el arma larga).

## Criterios
- [x] Sin apuntar con pistola: Caminar y Correr de la base, torso sin pose de arma (ArmaEnManoS1Pruebas)
- [x] Al apuntar: poses de arma y dos manos
- [x] Controlador con 2 capas (ControladorBasePruebas)
Capturas: docs/capturas/s1n-T-671-*.png
