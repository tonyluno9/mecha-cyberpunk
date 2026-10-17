using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Herramienta de pruebas: al arrancar deja la historia en Libre (la demo ya pasó) si estaba en Ninguna; con otra etapa no hace nada.
    /// Solo va en escenas de pruebas que no arrancan el prólogo (Pruebas_Economia, T-143).
    /// </summary>
    public class HistoriaLibreDePrueba : MonoBehaviour
    {
        private void Start()
        {
            ServicioHistoria historia = Registro<IHistoria>.Actual as ServicioHistoria;
            if (historia != null && historia.Etapa == EtapaPrologo.Ninguna)
            {
                historia.Avanzar(EtapaPrologo.Libre);
            }
        }
    }
}
