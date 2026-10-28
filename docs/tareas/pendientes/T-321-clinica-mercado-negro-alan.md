# T-321 Clínica dentro del mercado negro (A-F5)
Dueño: Alan | Rama: alan/T-321-clinica | Modelo: Sonnet 5.5, medium
Depende de: T-320. **Espera a Tony para el mostrador:** el doctor y los implantes son de Tony (bloque de implantes); hoy solo existen `Economia/Datos/DatosTienda_Clinica.asset` e `IdLugar.Clinica`.

## Objetivo
Un cuarto de clínica al fondo del mercado negro, con su propio `ILugar` (`IdLugar.Clinica`), listo para que Tony coloque su mostrador.

## Qué hacer
1. En `CreadorMercadoNegroInterior`: cuarto con camilla, luz fría y un punto vacío `Clinica_Mostrador` donde va el prefab de Tony.
2. `LugarMundo` con `IdLugar.Clinica`, horario de día y su puerta.
3. Si Tony ya publicó el prefab (`git ls-files | grep -i MostradorClinica`), colocarlo; si no, solo este paso queda bloqueado y se anota.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Tests de Mundo, docs/capturas/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [ ] El `ILugar` Clinica se registra y abre de día; `Clinica_Mostrador` está libre y sobre piso
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Implantes y el doctor (Tony).
