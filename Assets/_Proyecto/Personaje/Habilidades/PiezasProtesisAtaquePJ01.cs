using UnityEngine;

namespace MechaCyberpunk.Personaje.Habilidades
{
    /// <summary>
    /// T-1311: las piezas de PJ01_Implantes_Ataque.fbx dentro del cuerpo de PJ01: el brazo láser (IMP04, pesado a R_Forearm) y la hombrera de misiles (IMP05, pesada a Spine02),
    /// más los puntos de salida del rayo y de los misiles (los vacíos IMP04_Salida e IMP05_Salida del fbx, colgados de su hueso). Las piezas vienen apagadas;
    /// <see cref="ProtesisVisiblesAPie"/> las enciende cuando el jugador tiene la prótesis. Lo arma ColocarProtesisAtaqueT1311 (editor).
    /// </summary>
    public class PiezasProtesisAtaquePJ01 : MonoBehaviour
    {
        [SerializeField] private GameObject laser;
        [SerializeField] private GameObject misiles;
        [SerializeField] private Transform salidaLaser;
        [SerializeField] private Transform salidaMisiles;

        /// <summary>La pieza del brazo láser (apagada hasta tener la prótesis).</summary>
        public GameObject Laser => laser;

        /// <summary>La pieza de la hombrera de misiles (apagada hasta tener la prótesis).</summary>
        public GameObject Misiles => misiles;

        /// <summary>De donde sale el rayo (en el hueso del antebrazo derecho).</summary>
        public Transform SalidaLaser => salidaLaser;

        /// <summary>De donde salen los misiles (en el hueso Spine02).</summary>
        public Transform SalidaMisiles => salidaMisiles;

        /// <summary>Para el creador de editor: asigna las piezas y los puntos de salida.</summary>
        public void Configurar(GameObject piezaLaser, GameObject piezaMisiles, Transform puntoLaser, Transform puntoMisiles)
        {
            laser = piezaLaser;
            misiles = piezaMisiles;
            salidaLaser = puntoLaser;
            salidaMisiles = puntoMisiles;
        }
    }
}
