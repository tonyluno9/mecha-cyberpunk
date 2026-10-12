using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Combate.Pruebas
{
    /// <summary>Herramienta de prueba: un cubo con vida que escribe en la consola el daño que recibe. Solo va en Pruebas_Tony.</summary>
    [RequireComponent(typeof(VidaMecha))]
    public class SacoDePrueba : MonoBehaviour
    {
        private IPeleador peleador;
        private VidaMecha vida;

        private void Awake()
        {
            peleador = GetComponent<IPeleador>();
            vida = GetComponent<VidaMecha>();
        }

        private void OnEnable()
        {
            EventosPelea.ImpactoRecibido += AlRecibirImpacto;
        }

        private void OnDisable()
        {
            EventosPelea.ImpactoRecibido -= AlRecibirImpacto;
        }

        private void AlRecibirImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (objetivo == peleador)
            {
                Debug.Log("[Combate] Saco de prueba recibió " + resultado.DanioAplicado + " de daño (vida " + vida.Actual + ")");
            }
        }
    }
}
