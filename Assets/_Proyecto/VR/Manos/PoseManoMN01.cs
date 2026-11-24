using System;
using UnityEngine;

namespace MechaCyberpunk.VR.Manos
{
    /// <summary>
    /// VR40: una pose de la mano MN01 ya muestreada de su clip (1 frame) por el script de editor ImportarManosVR40: la rotación y la posición
    /// local de cada uno de los 17 huesos, en el orden de <see cref="ReglaManosMN01VR.Huesos"/>. Así en runtime no hace falta Animator.
    /// </summary>
    [Serializable]
    public class PoseManoMN01
    {
        public string Nombre;
        public Quaternion[] Rotaciones;
        public Vector3[] Posiciones;
    }
}
