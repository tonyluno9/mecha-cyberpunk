# T-921 Sin letreros con nombre de calle en los postes

- Qué: se quitaron de Ciudad_Prologo los letreros de bandera con EJE CENTRAL, AV. COYOTE, AV. NEÓN, CALLE DE LOS CABLES, CALLE DEL MERCADO... (raíz S2t_Calles completa, 115 objetos con nombre en el diff de la escena).
- Dónde: Ciudad_Prologo.unity; Assets/Arte/Editor/CallesS2t.cs ahora solo quita (Colocar ya no coloca, así no regresan si se corre el paso "9" del menú); con Encontrar() que busca por nombre y por texto (UI.Text y TextMesh).
- Prueba: EditMode CallesS2tPruebas.Ciudad_NoTieneLetrerosConNombreDeCalle (reemplaza la de T-861, que exigía los letreros): abre la escena y no debe quedar ninguno.
- Evidencia: docs/capturas/t921-{antes,despues}-cruce-eje-{coyote,neon}.png (antes se leía "EJE CENTRAL" en el poste de la esquina). PlayMode PiezasCalleT910Pruebas 3/3 (la escena carga y el semáforo ES01 sigue).
- Decisiones: no se tocaron los nombres de calle del GPS/datos ni el letrero blanco del semáforo ES01. Los textos de T-508 (CAFÉ, CANTINA...) no son nombres de calle y no se tocaron.
