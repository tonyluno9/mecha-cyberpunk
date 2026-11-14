using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-976: guarda la pose final de cadera, pies y cabeza de cada cuadro (corre después de PoseMoto y AnimacionCarro).</summary>
    [DefaultExecutionOrder(10000)]
    public class RegistroHuesosT976 : MonoBehaviour
    {
        public Transform[] Huesos;
        public readonly List<Vector3[]> Cuadros = new List<Vector3[]>();
        public readonly List<string> Notas = new List<string>();
        public System.Func<string> Nota;

        private void LateUpdate()
        {
            if (Huesos == null)
            {
                return;
            }
            var p = new Vector3[Huesos.Length];
            for (int i = 0; i < Huesos.Length; i++)
            {
                p[i] = Huesos[i].position;
            }
            Cuadros.Add(p);
            Notas.Add(Nota != null ? Nota() : "");
        }
    }
}
