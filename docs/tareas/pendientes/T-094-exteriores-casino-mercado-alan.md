# T-094 Exteriores cerrados del casino y el mercado negro
Dueño: Alan | Rama: alan/T-094-exteriores-casino-mercado | Modelo: Sonnet 5.5, medium
Depende de: T-042 (puertas y horarios), T-081 (fachadas), T-082 (letreros). Sin dependencias de Tony.

## Objetivo
El casino (Manzana_3) y el mercado negro (Manzana_2) se ven como lugares reales pero cerrados en la demo: casino con marquesina y puerta de vidrio oscura con cadena, mercado con cortina metálica baja y candados, y un cartel de "CERRADO" en cada uno, sin interior.

## Qué hacer
1. `Mundo/Editor/VestirCerrados.cs`: por cada lugar cerrado (los que tienen `DatosLugar` con cerrado = true) agrega, delante de la fachada y sin tapar la `Entrada`: casino, marquesina de 6 × 1.5 m con tiras de neón apagado/tenue, dos columnas decorativas y alfombra roja gastada; mercado, cortina metálica (cubos finos con rayas) cubriendo la puerta, dos candados y un letrero de tela "VOLVEMOS PRONTO".
2. Un cartel "CERRADO" (uGUI Text World Space o cubo emisivo tenue) en cada puerta, con el horario real del lugar.
3. Se regenera con `CreadorTodo.Crear` (añadir el paso en el orden correcto) y se puede repetir sin cambiar nada.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Tests de Mundo, docs/capturas/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [ ] La puerta de cada lugar cerrado sigue sin abrir (`ResultadoPuerta` de cerrado) a cualquier hora
- [ ] Ninguna pieza nueva bloquea `Entrada` ni los puntos de aparición, ni se mete en la calle más de 1.5 m
- [ ] Capturas de día y de noche de cada fachada, revisadas: docs/capturas/T-094-1..4.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Interior de estos lugares (A-F5), grafitis (T-095).
