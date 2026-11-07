# T-696 Carros y choques
Dueño: Tony | Rama: tony/T-696-carros-y-choques | Modelo: Sonnet 5.5, medium
Depende de: T-692, T-695

## Qué cambió
- Carros (ConfigCarro_Viejo, vía CalibrarCarrosT696): motor 6200 a 8800 N (8 m/s²), freno 12500 a 20000 N, freno de mano 9000 N. Reversa, giro y agarre sin cambios.
- Choques (ConfigDanioCarro): umbral 8 m/s, 1.2 por m/s y tope de 15 por choque: siempre menos que un disparo de pistola (25).
- Moto a alta velocidad (>= 11 m/s de choque): DanioCarro saca al conductor (salida forzada); JugadorAPie, con la velocidad de antes del choque, lo pasa a LanzamientoMoto (Personaje/Jugador): vuela hasta 9 m/s, cae con la caída que ya existe, pierde 10 de vida, se queda tirado y se levanta.
- Quitado el efecto de luces de alarma y el sonido de carro robado (CarroEstacionado). Se conserva la cerradura forzada y el calor.
- Aviso de fuego de T-692: ahora MUN_CHISPAS_ELECTRICAS (clip existente del catálogo; se encendió su interruptor en CatalogoSonidos.asset, un solo bool). Nada que pedirle a arte.
- Escala: carros medidos y fotografiados junto al jugador de 1.80 (s1t-T-696-escala-*.png): el techo queda a la altura del hombro o la cabeza (la furgoneta V04 la pasa), proporciones normales. No se ajustó ninguno.
- ManejoMotoPruebas ajustada: la moto ya no frena con inercia larga (T-695).

## Pruebas
EditMode ChoqueMenosQueDisparoT696; PlayMode Personaje/LanzamientoMotoT696 (moto a pared, entrada simulada) y EscalaCarrosT696 (medición y capturas).
