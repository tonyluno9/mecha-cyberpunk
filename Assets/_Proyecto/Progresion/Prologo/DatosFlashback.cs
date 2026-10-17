using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>Un plano del flashback (T-131): qué punto de historia pone la cámara, cuánto dura y qué dice el subtítulo.</summary>
    [Serializable]
    public struct PlanoFlashback
    {
        [Tooltip("Id del IPuntoHistoria que da la pose de la cámara, por ejemplo Flashback_Plano_1.")]
        public string idPunto;
        [Tooltip("Segundos con el plano a la vista, sin contar los fundidos (s).")]
        public float segundos;
        [TextArea(1, 3)] [Tooltip("Subtítulo del plano.")]
        public string subtitulo;
    }

    /// <summary>Los planos, el fundido entre ellos y el aviso de saltar del flashback (T-131). Los textos son propuesta de Tony (D-tony-68).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del flashback", fileName = "DatosFlashback_Nuevo")]
    public class DatosFlashback : ScriptableObject
    {
        [Tooltip("Planos en orden.")]
        [SerializeField] private List<PlanoFlashback> planos = new List<PlanoFlashback>();
        [Tooltip("Segundos de cada fundido: uno al entrar y otro al salir de cada plano (s).")]
        [Min(0f)] [SerializeField] private float segundosFundido = 0.5f;
        [Tooltip("Aviso pequeño para saltar.")]
        [SerializeField] private string textoSalto = "Mantén ESPACIO para saltar";
        [Tooltip("Segundos que hay que mantener ESPACIO para saltar (s).")]
        [Min(0.1f)] [SerializeField] private float segundosParaSaltar = 1f;

        public IReadOnlyList<PlanoFlashback> Planos => planos;
        public float SegundosFundido => segundosFundido;
        public string TextoSalto => textoSalto;
        public float SegundosParaSaltar => segundosParaSaltar;

        /// <summary>Duración total: planos más dos fundidos por plano (s).</summary>
        public float SegundosTotales
        {
            get
            {
                float total = 0f;
                foreach (PlanoFlashback plano in planos) total += plano.segundos + 2f * segundosFundido;
                return total;
            }
        }

        /// <summary>Para crear los datos desde código (editor y pruebas).</summary>
        public void Fijar(IEnumerable<PlanoFlashback> nuevosPlanos, float fundido, string salto, float segundosSaltar)
        {
            planos = new List<PlanoFlashback>(nuevosPlanos);
            segundosFundido = fundido;
            textoSalto = salto;
            segundosParaSaltar = segundosSaltar;
        }
    }
}
