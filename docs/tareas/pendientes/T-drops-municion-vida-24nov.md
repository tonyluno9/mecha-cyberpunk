# T-drops-municion-vida-24nov · Los enemigos sueltan munición y vida
**Estado:** PENDIENTE
**Por qué:** en el modo libre la pelea dura muchos minutos; sin esto te quedas sin balas ni vida.
**Qué hacer:** al morir, cada enemigo suelta con probabilidad (valores en un ScriptableObject): munición del tipo de su arma o de tu arma equipada (policía y NOVA casi siempre, SKULLS a veces), y de vez en cuando un botiquín pequeño. Usar modelos que ya existan en el proyecto (cargadores, botiquín/consumibles); nada con primitivos. Brillo suave para verlos, se recogen al pasar cerca (en VR también al acercar la mano), desaparecen a los 30 s. Sonido de recoger. Vale para PC y VR.
**Cómo comprobar:** matar 10 policías: aparecen cargadores y algún botiquín; al recogerlos sube munición/vida.
