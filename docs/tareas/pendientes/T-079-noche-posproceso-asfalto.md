# T-079 Noche cyberpunk: posproceso, neón con bloom y asfalto mojado
Dueño: Alan | Rama: alan/T-079-noche-posproceso-asfalto | Modelo: Sonnet 5.5, medium
Depende de: T-076 (materiales de neón HDR). Va antes que T-080 a T-083 para que sus capturas ya salgan con este ambiente.

## Objetivo
El barrio de noche tiene la iluminación cinematográfica de la referencia: el neón brilla con halo (bloom), el color está
graduado, hay niebla fría y el asfalto parece mojado y refleja las luces. De día el mismo barrio se ve creíble y no lavado.
También deja fijas cuatro vistas de referencia que usan todas las capturas de arte de la fase.

## Contexto
- Diseño: docs/diseno/estetica-hud.md (low-poly con iluminación cinematográfica: niebla, reflejos en el asfalto mojado y neón
  azul, magenta, naranja y turquesa). Revisión de la fase 1 (docs/revision-alan.md): "se vea chingón, no sencillo"; la noche de
  T-041 salió casi negra en la captura.
- URP 17.3: Volume global con un `VolumeProfile` (Bloom, Tonemapping, ColorAdjustments, Vignette). El renderer PC ya trae
  `postProcessData`. Si algo de Assets/Settings/ o de ProjectSettings/ hiciera falta (HDR, por ejemplo), NO se toca: se anota en
  docs/revision-alan.md como pedido de Core y se sigue con lo demás.
- El día y la noche los maneja `CicloDiaNoche` (Mundo/Reloj, T-041) con `ConfigCicloDia`. Las farolas y letreros se prenden con
  `LuzNocturna`.
- Las cámaras de Alan (cámara libre y la del carro) deben tener el posproceso prendido. La cámara del jugador de Tony es suya:
  se le pide en docs/revision-alan.md que la prenda (propuesta D-alan-19).

## Qué hacer
1. `Mundo/Datos/PerfilNoche.asset` y `PerfilDia.asset` (VolumeProfile creados por código): bloom con umbral cercano a 1 (solo el
   HDR brilla), tonemapping ACES, ajustes de color (contraste y saturación arriba, tinte frío de noche) y viñeta ligera.
   `CicloDiaNoche` mezcla los dos según la hora (peso de cada Volume), sin saltos en el amanecer y el anochecer.
2. Asfalto mojado: `Barrio_Asfalto` más oscuro y liso (smoothness alto), charcos (quads planos un poco más lisos y oscuros) en
   las calles y en los cruces, y sondas de reflejo horneadas con proyección de caja en los cruces y frente al clandestino y el bar
   (horneadas por el creador con `Lightmapping.BakeReflectionProbe`, texturas guardadas en `Mundo/Reflejos/`).
3. Los materiales de letreros greybox (`Barrio_Letrero_*`) y del bar (`Bar_Neon*`) pasan a emisión HDR para que el bloom los
   tome. La niebla nocturna sube un poco y es de color azul-violeta; la luz ambiente de noche sube lo justo para leer las
   fachadas.
4. `Mundo/Pruebas/VistasReferencia.cs` (+ `ConfigVistasReferencia.asset`): cuatro poses fijas que coloca la cámara libre con
   `Colocar`, para capturas comparables en toda la fase:
   - V1 Esquina: (-44, 1.75, -4), mirando al este por la calle principal (clandestino a la izquierda, bar a la derecha).
   - V2 Clandestino: (0, 1.95, -8), en la banqueta del bar mirando al norte, a la fachada del clandestino.
   - V3 Casa: (-45, 1.75, -72), mirando hacia la casa (oeste-suroeste).
   - V4 Aérea: (-60, 40, -60), mirando al centro del barrio.
   Un método estático `VistasReferencia.Ir(int vista, float hora)` (coloca la cámara y mueve el reloj) para usarlo con
   `unity-vivo.sh eval`.
5. El creador (archivo nuevo `Mundo/Editor/CreadorAmbiente.cs`) pone el Volume en Ciudad.unity y Pruebas_Alan.unity, prende el
   posproceso en la cámara libre (prefab), y se llama desde `CreadorTodo` al final, justo antes de CreadorPeleaClandestino
   (después de CreadorCarro y, cuando exista, de CreadorTrafico); actualiza el comentario de CreadorTodo.
   Las sondas solo se hornean si su textura no existe: así correr `CreadorTodo` dos veces no cambia archivos. El menú
   `CreadorAmbiente.HornearReflejos()` las rehace; las tareas que cambien el barrio (T-080 a T-083) lo corren una vez al final.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Vehiculos/ (solo prender el posproceso de CamaraCarro en el prefab y su creador),
Assets/_Proyecto/Escenas/Ciudad.unity, Assets/_Proyecto/Escenas/Pruebas_Alan.unity, Assets/_Proyecto/Tests/EditMode/Mundo/ y
Assets/_Proyecto/Tests/PlayMode/Mundo/
No tocar: Core/, Assets/Settings/, ProjectSettings/, Clandestino.unity (la jaula ya tiene su luz; si se quiere el mismo
posproceso allá, se anota para otra tarea), las carpetas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (CicloDiaNochePruebas y RelojReentradaPruebas incluidas)
- [ ] PlayMode: en Pruebas_Alan hay Volumes globales con PerfilNoche y PerfilDia; a las 23:00 el peso de noche es 1 y a las 13:00
      el de día es 1; entre 18:30 y 19:30 el peso cambia sin saltos (diferencia entre muestras de 1 minuto de juego < 0.1)
- [ ] EditMode: PerfilNoche tiene Bloom activo con umbral entre 0.8 y 1.2 y Tonemapping ACES; la cámara libre y la del carro
      tienen `renderPostProcessing` prendido
- [ ] EditMode: todo material `Barrio_Letrero_*` y `Bar_Neon*` tiene `_EMISSION` y algún canal de emisión ≥ 2
- [ ] PlayMode: hay al menos 4 sondas de reflejo con textura horneada asignada y proyección de caja
- [ ] Medido en el editor vivo (eval que renderiza la cámara a una RenderTexture de 640 × 360 y promedia la luminancia): en V1 a
      las 22:00 la luminancia media sube al menos 30 % respecto a la línea base (medida igual en alan/fase-2 antes de cambiar
      nada y anotada en la bitácora), y menos del 3 % de los píxeles queda saturado (luminancia ≥ 0.98: el bloom no lava)
- [ ] Capturas de las 4 vistas a las 22:00 (docs/capturas/T-079-1.png a -4.png) y V1 a las 13:00 (T-079-5.png)
- [ ] Consola sin errores con el reloj a x30 durante 2 minutos

## Criterios de sensación (para un humano)
- De noche se parece a la imagen de referencia: neón con halo, asfalto que brilla con los reflejos y fachadas que se leen.
- El bloom no lava la imagen ni ciega; de día no se ve todo gris.

## Pruebas automáticas
- EditMode (Tests/EditMode/Mundo/): AmbientePruebas (perfiles, materiales y cámaras).
- PlayMode (Tests/PlayMode/Mundo/): AmbienteNochePruebas (pesos por hora y sondas).

## Fuera de alcance
- Lluvia, partículas, audio de ciudad, grafitis (A-F4); posproceso de la cámara de Tony (pedido a Tony)
- Iluminación horneada (lightmaps)
