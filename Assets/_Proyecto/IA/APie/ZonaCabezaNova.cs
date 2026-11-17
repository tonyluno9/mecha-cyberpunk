using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1323: un disparo a la cabeza de un agente de Nova no hacía daño porque la cabeza del modelo (2.05 m el pesado, 2.3 m el jefe) sobresale de la cápsula
    /// del CharacterController: la bala pasaba por encima sin tocar nada. Esta zona pone una esfera sólida en la cabeza (hueso Head del Animator humanoide), hija del
    /// agente, así que <c>GetComponentInParent&lt;IRecibeDanio&gt;</c> la manda al <see cref="PandilleroAPie"/> y ReglaZonaDanio la cuenta como cabeza (x3).
    /// </summary>
    public class ZonaCabezaNova : MonoBehaviour
    {
        /// <summary>Radio de la esfera de la cabeza (m, a escala 1).</summary>
        public const float RadioEsfera = 0.18f;
        /// <summary>El hueso Head queda en el cuello: el centro de la cabeza está esto más arriba (m, a escala 1).</summary>
        public const float SubidaCentro = 0.1f;

        public SphereCollider Esfera { get; private set; }

        private void Awake()
        {
            Animator animador = GetComponentInChildren<Animator>(true);
            if (animador == null || !animador.isHuman)
            {
                return;
            }
            Transform hueso = animador.GetBoneTransform(HumanBodyBones.Head);
            if (hueso == null)
            {
                return;
            }
            float escala = Mathf.Max(0.1f, Mathf.Abs(animador.transform.lossyScale.y));
            GameObject objeto = new GameObject("CabezaImpacto");
            objeto.transform.SetParent(hueso, false);
            objeto.transform.position = hueso.position + Vector3.up * SubidaCentro * escala;
            Esfera = objeto.AddComponent<SphereCollider>();
            Esfera.radius = RadioEsfera * escala / Mathf.Max(0.01f, Mathf.Abs(hueso.lossyScale.y));
            CharacterController cuerpo = GetComponent<CharacterController>();
            if (cuerpo != null)
            {
                Physics.IgnoreCollision(Esfera, cuerpo); // no empuja a su propio dueño
            }
        }
    }
}
