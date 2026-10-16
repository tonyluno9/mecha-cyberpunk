# T-082 Letreros de neón
Dueño: Alan | Rama: alan/T-082-letreros-neon | Modelo: Sonnet 5.5, medium
Depende de: T-076 (cadena Blender a FBX y materiales), T-081 (fachadas: los letreros van encima).

## Objetivo
El barrio se llena de letreros de neón con palabras en español: de bandera sobre la banqueta, pegados a la fachada y en las
azoteas, con algunos que parpadean. Los locales que importan (bar, clandestino, casino) se reconocen de lejos por su letrero.

## Contexto
- Ya hay tres letreros del kit (T-076): letrero_tacos, letrero_farmacia y letrero_cantina. Los letreros greybox de hoy son cubos
  de color en el 45 % de los edificios (`EdificioPlano`) y uno por lugar (`Lugar`, encima de la puerta).
- Blender headless (ruta en .claude/maquina.local.md). Estilo y funciones de apoyo: `Assets/Arte/Fuentes~/kit-urbano/kit_urbano.py`
  (`_letrero_base`, `text`, materiales `Neon_*`). Los FBX nuevos pasan por el postprocesador y los materiales de T-076.
- Nombres de los locales: siguen abiertos (estetica-hud.md, preguntas abiertas). Propuesta D-alan-20: solo palabras genéricas.
- `LuzNocturna` (T-041) prende de noche lo que CreadorReloj encuentra (objetos "Farola_" y renderers con materiales
  `Barrio_Letrero*`); los letreros nuevos también deben bajar de día.

## Qué hacer
1. `Assets/Arte/Fuentes~/letreros/letreros_neon.py`: genera al menos 8 letreros nuevos con la misma estética del kit (marco
   metálico, tubos de neón, texto en relieve, soportes): HOTEL, BIRRIA 24H, CIBER, REFACCIONES, LAVANDERÍA, PELEAS (para el
   clandestino), CASINO y una flecha; dos de ellos verticales de bandera. Exporta FBX a `Assets/Arte/Mundo/Letreros/`, agrega
   sus materiales a materiales.json y sus medidas a dimensiones.json, y deja un render .png de referencia en Fuentes~/letreros/.
2. `CreadorArte` (T-076) crea sus prefabs en `Mundo/Prefabs/Kit/` sin collider.
3. Archivo nuevo `Mundo/Editor/VestirLetreros.cs`, llamado por CreadorBarrio: cambia los letreros greybox de los edificios por
   letreros del kit, con semilla fija: de bandera (perpendiculares a la fachada, a 4 m o más de alto, saliendo hasta 1.5 m),
   planos sobre la fachada y algunos en azoteas. Entre 30 y 50 letreros en todo el barrio, más en el centro que en la zona pobre.
   Bar: letrero_cantina; clandestino: PELEAS grande; casino: CASINO (cerrado: más tenue); el `LetreroLugar` de horario de cada
   local sigue visible.
4. `Mundo/Ambiente/ParpadeoNeon.cs` con su lógica pura `CalculoParpadeo` (`dt` y azar inyectado): uno de cada 6 letreros parpadea
   como neón viejo (apagones cortos e irregulares). De día los letreros bajan su emisión (con `LuzNocturna` o lo que use
   CreadorReloj, sin romper CicloDiaNochePruebas).
5. Regenera con `CreadorTodo.Crear` y al final rehornea los reflejos (`CreadorAmbiente.HornearReflejos()`, T-079).

## Carpetas permitidas
Assets/Arte/Fuentes~/letreros/, Assets/Arte/Mundo/Letreros/, Assets/Arte/Materiales/, Assets/Arte/Mundo/dimensiones.json,
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Escenas/Ciudad.unity, Assets/_Proyecto/Escenas/Pruebas_Alan.unity,
Assets/_Proyecto/Tests/EditMode/Mundo/ y Assets/_Proyecto/Tests/PlayMode/Mundo/
No tocar: Core/, Vehiculos/, Clandestino.unity, las carpetas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] El script de Blender corre headless sin errores y deja al menos 8 FBX nuevos y su render
- [ ] `bash herramientas/verificar.sh todo` pasa (ArtePruebas cuenta las piezas nuevas con sus medidas y materiales)
- [ ] EditMode `CalculoParpadeoPruebas`: con la misma semilla da la misma secuencia; la fracción de tiempo apagado en 60 s está
      entre 5 % y 30 %
- [ ] PlayMode: hay entre 30 y 50 letreros de neón y ningún letrero greybox de edificio; cada uno está a menos de 0.5 m de una
      fachada o azotea (rayo hacia atrás) y a 3 m o más de alto; los de bandera no salen más de 1.5 m de la fachada
- [ ] PlayMode: el bar, el clandestino y el casino tienen su letrero a menos de 8 m de su `Entrada`, y el `LetreroLugar` de cada
      local sigue a la vista (rayo desde la `Entrada` hacia el letrero de horario sin choques)
- [ ] PlayMode: a las 22:00 la emisión de los letreros es mayor que a las 12:00; los que parpadean cambian al menos una vez en 10 s
- [ ] Capturas de V1, V2 y V4 a las 22:00 (docs/capturas/T-082-1.png a -3.png) y un letrero de bandera de cerca (T-082-4.png)
- [ ] Consola sin errores

## Criterios de sensación (para un humano)
- De noche la calle principal es un río de neón como la imagen de referencia, y el clandestino y el bar se ubican de lejos.
- El parpadeo se siente a neón viejo, no a falla del juego.

## Pruebas automáticas
- EditMode (Tests/EditMode/Mundo/): CalculoParpadeoPruebas; ArtePruebas ampliada.
- PlayMode (Tests/PlayMode/Mundo/): LetrerosPruebas.

## Fuera de alcance
- Nombres propios de los locales (pregunta abierta); anuncios corporativos animados y pantallas gigantes (A-F4)
- Letreros con el alias del jugador (después de la demo)
