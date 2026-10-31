# T-448 Tu guarida sube de nivel: armero, garaje, cama (mejora 17)
Dueño: Tony | Rama: tony/T-448-guarida | Modelo: Sonnet 5.5 | Sesión A (docs/sesion-A.md, prioridad 9)
Hecho: tres mejoras de la guarida con tres niveles cada una, que se compran con E en un poste de neón turquesa dentro de tu casa, junto a la cama (precio base ARMERO Ƀ500, GARAJE Ƀ600, CAMA Ƀ400; el nivel 2 cuesta el doble y el 3 cuatro veces):
- ARMERO: +8 / +16 / +25 % de daño a las armas de fuego a pie (`PistolaAPie` multiplica por `IGuarida.BonoDanioArmas`).
- GARAJE: con el nivel 1 aparece un carro tuyo estacionado fuera de la casa (cerca del tablero de encargos; sin alarma ni calor); nivel 2 y 3: aguanta 1.6 y 2.4 veces más daño (`DanioCarro.EscalarVida`).
- CAMA: dormir (que ya autoguarda) con el nivel 1 te deja con la vida llena; con el 2 borra la mitad del calor policial y con el 3 todo.
Contrato nuevo en Core: `IGuarida` (Core/Guarida). Código: `Progresion/Guarida/` (DatosGuarida, ReglaGuarida, ServicioGuarida guardado con la clave "guarida", GuaridaPunto) y `Vehiculos/Carro/GarajeCarro`. Escena: `CreadorCiudadPandillas.T448` (menú Mecha Cyberpunk/Pandillas/T-448 Guarida; también va en Todo) rehizo solo la raíz GTA_T448 en Ciudad_Prologo.
Pruebas: EditMode ReglaGuaridaPruebas (5); PlayMode GuaridaPruebas (1 recorrido: los puntos junto a la cama, sin dinero no se compra, ARMERO a nivel máximo, GARAJE enciende el carro y el nivel 2 le da 1.6x vida, CAMA cura y el calor baja de 40 a 20, guardar y cargar).
No cubierto: el HUD/mapa no lista las mejoras (solo el aviso al acercarse), no hay capturas de los postes ni del carro del garaje.
