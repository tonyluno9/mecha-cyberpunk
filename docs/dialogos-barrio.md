# Diálogos del barrio (T-1624: T12 y T15)

Todas las líneas que dicen los NPCs en las escenas del barrio. Hoy salen solo con subtítulo. Para ponerles voz:
graba el archivo, déjalo en `Assets/Arte/Audio/19_Voces/Barrio/` y asígnalo en el campo **Audio** de la línea, dentro del
asset de la escena (`Assets/_Proyecto/IA/Resources/Barrio/EscenaBarrio_<id>.asset`). Con audio, el subtítulo dura lo que el clip.
El número del archivo es la posición de la línea dentro de la escena (las que solo hacen un gesto no llevan voz, pero cuentan en el número).

Todos los ID de escena se pueden forzar en Play con `MechaCyberpunk.IA.Barrio.DirectorEscenasBarrio.ForzarEscena("<id>")`.

## Bullying (los pandilleros abusan de la gente)

### bully_piso_tianguis (SKULLS, un vendedor del tianguis)
| # | Quién | Texto | Gesto | Archivo esperado |
| --- | --- | --- | --- | --- |
| 1 | SKULL | ¿Y la cuota de esta semana, jefe? | | bully_piso_tianguis_01.wav |
| 2 | VENDEDOR | Ya les pagué ayer... | suplica (manos arriba) | bully_piso_tianguis_02.wav |
| 3 | SKULL | Pues hoy pagas doble. | empuja al vendedor | bully_piso_tianguis_03.wav |
| 4 | SKULL | (sin voz) | patea la mercancía | |
| 5 | VENDEDOR | (sin voz) | cae al suelo | |
| 6 | SKULL | (risas) | se burla | usa la voz SKU_RISA_BURLA del catálogo |

### bully_celular (SKULLS, un joven)
| # | Quién | Texto | Gesto | Archivo esperado |
| --- | --- | --- | --- | --- |
| 1 | SKULL | Préstame tu cel, carnal... gracias. | le quita el celular | bully_celular_01.wav |
| 2 | SKULL | (sin voz) | empuja | |
| 3 | JOVEN | (sin voz) | cae al suelo | |
| 4 | SKULL | (risas) | se burla | usa SKU_RISA_BURLA |

### bully_snakes_senora (SNAKES, una señora con su mandado, Neón Alto)
| # | Quién | Texto | Gesto | Archivo esperado |
| --- | --- | --- | --- | --- |
| 1 | SNAKE | Esta calle es nuestra, doña. | | bully_snakes_senora_01.wav |
| 2 | SEÑORA | Ya no traigo nada, muchachos... (línea añadida) | suplica | bully_snakes_senora_02.wav |
| 3 | SNAKE | Aquí se paga por pasar. | se burla | bully_snakes_senora_03.wav |

### bully_repartidor (SKULLS, un repartidor)
| # | Quién | Texto | Gesto | Archivo esperado |
| --- | --- | --- | --- | --- |
| 1 | SKULL | ¡Ora, repartidor, aquí no se pasa gratis! (línea añadida) | empuja | bully_repartidor_01.wav |
| 2 | REPARTIDOR | (sin voz) | cae | |
| 3 | SKULL | (sin voz) | patea la caja | |
| 4 | SKULL | (risas) | se burla | usa SKU_RISA_BURLA |

### bully_abuelo (SKULLS, un señor mayor)
| # | Quién | Texto | Gesto | Archivo esperado |
| --- | --- | --- | --- | --- |
| 1 | SKULL | Ándale, abuelo, más rápido. | empuja | bully_abuelo_01.wav |
| 2 | ABUELO | (sin voz) | cae | |
| 3 | SKULL | (risas) | se burla; se van riendo | usa SKU_RISA_BURLA |

### bully_comerciante_cortina (SKULLS, un comerciante que cierra su cortina)
| # | Quién | Texto | Gesto | Archivo esperado |
| --- | --- | --- | --- | --- |
| 1 | SKULL | Mañana venimos por lo de la protección. | | bully_comerciante_cortina_01.wav |
| 2 | COMERCIANTE | Sí, sí... aquí los espero. (línea añadida) | suplica | bully_comerciante_cortina_02.wav |
| 3 | SKULL | (sin voz) | se burla | |

Las líneas marcadas "añadida" no estaban en T15: las puse para que la víctima responda; cámbialas si no te gustan.

## Escenas normales del barrio

| Escena | Quién | Texto | Archivo esperado |
| --- | --- | --- | --- |
| vendedor_tacos | VENDEDOR | ¡Lleve sus tacos! | vendedor_tacos_01.wav |
| vendedor_elotes | VENDEDOR | ¡Elotes, esquites! | vendedor_elotes_01.wav |
| vecinos_muro | VECINA | Dicen que nadie ha salido del muro en años... | vecinos_muro_01.wav |
| vecinos_muro | VECINO | Mejor ni preguntes, comadre. (añadida) | vecinos_muro_02.wav |
| vecinos_nova | VECINO | NOVA dice que todo es por nuestra seguridad. (añadida) | vecinos_nova_01.wav |
| vecinos_nova | VECINA | Pues yo no me siento más segura, la verdad. (añadida) | vecinos_nova_02.wav |
| pareja_discutiendo | ÉL | ¡Siempre dices lo mismo! | pareja_discutiendo_01.wav |
| pareja_discutiendo | ELLA | ¡Es que nunca me escuchas! | pareja_discutiendo_02.wav |
| celular | HOMBRE | Sí, ya voy para allá... no, no me esperes. (hace el gesto de hablar por teléfono) | celular_01.wav |
| borracho | BORRACHO | ¡Yo invito la... la ronda! ¡Salud! (se burla/gesticula) | borracho_01.wav |

## Cómo funciona (resumen)

- Cada 30 a 70 s (después de los primeros 30 s) puede salir una escena a 20-40 m del jugador. Nunca dos a la vez, no repite la misma
  seguida, y no sale en misión protegida ni en cinemática. Cada pandilla espera 1 a 3 min entre bullyings.
- Los subtítulos solo se ven si el que habla está a 15 m o menos y a la vista; se apagan en Opciones, fila SUBTÍTULOS.
- Si el jugador golpea o dispara a los pandilleros, o saca un arma cerca, dejan a la víctima y van contra él. Si no hace nada,
  se ríen y regresan a su lugar; la víctima huye.
