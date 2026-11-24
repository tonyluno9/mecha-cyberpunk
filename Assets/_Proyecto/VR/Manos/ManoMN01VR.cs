using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR.Manos
{
    /// <summary>
    /// VR40: una mano MN01 (prefab MN01_ManoVR_R / _L en Resources/ManosVR, hecho por ImportarManosVR40). Trae sus 17 huesos y las 9 poses ya
    /// muestreadas de los clips del FBX; cada cuadro <see cref="Aplicar"/> pone la pose base (<see cref="PoseManoVR"/>), el grip mezcla
    /// Abierta→Puño y el gatillo dobla solo el índice, todo con una mezcla corta. Con la prótesis de brazos, <see cref="FijarBrillo"/> cambia a una
    /// copia del material de prótesis (emisión cian enmascarada por el mapa metálico) con pulso. No usa Animator ni toca el FBX ni sus texturas.
    /// </summary>
    public class ManoMN01VR : MonoBehaviour
    {
        private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private bool derecha;
        [SerializeField] private Transform[] huesos = new Transform[0];
        [SerializeField] private PoseManoMN01[] poses = new PoseManoMN01[0];
        [SerializeField] private Renderer[] mallas = new Renderer[0];
        [SerializeField] private Material materialNormal;
        [SerializeField] private Material materialProtesis;
        [Tooltip("T87: en VR solo se ve la mano con el guante; el antebrazo se encoge al puño del guante (sin tocar el FBX).")]
        [SerializeField] private bool ocultarAntebrazo = true;

        private PoseManoMN01 abierta;
        private PoseManoMN01 puno;
        private PoseManoMN01 gatilloPose;
        private Material copiaProtesis;
        private bool brillando;
        private bool listaPoses;
        private bool antebrazoColapsado;
        private Vector3 antebrazoPosOriginal;
        private Vector3 antebrazoEscalaOriginal;
        private Vector3 manoPosOriginal;
        private Vector3 manoEscalaOriginal;

        public bool Derecha => derecha;
        public PoseManoVR Pose { get; private set; }
        public float Grip { get; private set; }
        public float Gatillo { get; private set; }
        public bool Brillando => brillando;
        public int CantidadHuesos => huesos != null ? huesos.Length : 0;
        public int CantidadPoses => poses != null ? poses.Length : 0;
        /// <summary>T87: true si el antebrazo está oculto ahora mismo (encogido al punto de corte).</summary>
        public bool AntebrazoOculto => antebrazoColapsado;

        /// <summary>Un hueso por su índice en <see cref="ReglaManosMN01VR.Huesos"/> (null si falta).</summary>
        public Transform Hueso(int indice) => huesos != null && indice >= 0 && indice < huesos.Length ? huesos[indice] : null;

        /// <summary>True si tiene los 17 huesos y al menos Abierta, Puño y Gatillo.</summary>
        public bool Valida
        {
            get
            {
                PrepararPoses();
                return listaPoses;
            }
        }

        /// <summary>Lo llama el script de editor al hacer el prefab.</summary>
        public void Configurar(bool esDerecha, Transform[] losHuesos, PoseManoMN01[] lasPoses, Renderer[] lasMallas, Material normal, Material protesis)
        {
            derecha = esDerecha;
            huesos = losHuesos;
            poses = lasPoses;
            mallas = lasMallas;
            materialNormal = normal;
            materialProtesis = protesis;
            listaPoses = false;
        }

        /// <summary>La pose muestreada de un clip (null si no está).</summary>
        public PoseManoMN01 PoseDe(string clip)
        {
            if (poses == null)
            {
                return null;
            }
            foreach (PoseManoMN01 p in poses)
            {
                if (p != null && p.Nombre == clip && p.Rotaciones != null && p.Rotaciones.Length == CantidadHuesos)
                {
                    return p;
                }
            }
            return null;
        }

        private void PrepararPoses()
        {
            if (listaPoses)
            {
                return;
            }
            abierta = PoseDe(ReglaManosMN01VR.ClipAbierta);
            puno = PoseDe(ReglaManosMN01VR.ClipPuno);
            gatilloPose = PoseDe(ReglaManosMN01VR.ClipGatillo);
            listaPoses = huesos != null && huesos.Length == ReglaManosMN01VR.CantidadHuesos && abierta != null && puno != null && gatilloPose != null;
            if (listaPoses)
            {
                foreach (Transform h in huesos)
                {
                    listaPoses &= h != null;
                }
            }
        }

        /// <summary>Pone una pose de golpe (sin mezcla): para alinear la mano al control y para las pruebas.</summary>
        public void PonerPose(PoseManoVR pose, float grip, float gatillo)
        {
            Aplicar(pose, grip, gatillo, 1f);
        }

        /// <summary>T87: muestra u oculta el antebrazo; se aplica en el siguiente <see cref="Aplicar"/> (o ya, si la mano tiene pose).</summary>
        public void FijarOcultarAntebrazo(bool ocultar)
        {
            ocultarAntebrazo = ocultar;
            if (!ocultar)
            {
                RestaurarAntebrazo();
            }
            else if (listaPoses)
            {
                ColapsarAntebrazo();
            }
        }

        /// <summary>
        /// Lleva los huesos hacia la pose objetivo: la base de <paramref name="pose"/>, el grip (0..1) y el gatillo (0..1); <paramref name="mezcla"/>
        /// es la fracción del camino de este cuadro (<see cref="ReglaManosMN01VR.Mezcla"/>; 1 = de golpe).
        /// </summary>
        public void Aplicar(PoseManoVR pose, float grip, float gatillo, float mezcla)
        {
            PrepararPoses();
            if (!listaPoses)
            {
                return;
            }
            Pose = pose;
            Grip = Mathf.Clamp01(grip);
            Gatillo = Mathf.Clamp01(gatillo);
            PoseManoMN01 basePose = PoseDe(ReglaManosMN01VR.ClipDe(pose)) ?? abierta;
            bool conPosiciones = basePose.Posiciones != null && basePose.Posiciones.Length == huesos.Length;
            RestaurarAntebrazo(); // la mezcla parte de las posiciones de la pose, no de las del antebrazo encogido
            for (int i = 0; i < huesos.Length; i++)
            {
                Quaternion objetivo = ReglaManosMN01VR.Rotacion(i, pose, Grip, Gatillo, abierta.Rotaciones[i], puno.Rotaciones[i], basePose.Rotaciones[i],
                    gatilloPose.Rotaciones[i]);
                Transform h = huesos[i];
                h.localRotation = mezcla >= 1f ? objetivo : Quaternion.Slerp(h.localRotation, objetivo, mezcla);
                if (conPosiciones)
                {
                    h.localPosition = mezcla >= 1f ? basePose.Posiciones[i] : Vector3.Lerp(h.localPosition, basePose.Posiciones[i], mezcla);
                }
            }
            if (ocultarAntebrazo)
            {
                ColapsarAntebrazo();
            }
        }

        // T87: el antebrazo es la raíz del rig y la mano su único hijo. Se encoge el antebrazo (escala casi 0) con su origen a unos cm detrás de la
        // muñeca: todo lo pesado a él se junta en ese punto y cierra el puño del guante; la mano se recoloca y se contra-escala y queda igual en el
        // mundo. Así no hace falta leer la malla (el FBX no es Read/Write) y funciona igual en el editor y en el Quest.
        private void ColapsarAntebrazo()
        {
            Transform antebrazo = Hueso(ReglaManosMN01VR.IndiceAntebrazo);
            Transform mano = Hueso(ReglaManosMN01VR.IndiceMano);
            if (antebrazoColapsado || antebrazo == null || mano == null || mano.parent != antebrazo)
            {
                return;
            }
            antebrazoPosOriginal = antebrazo.localPosition;
            antebrazoEscalaOriginal = antebrazo.localScale;
            manoPosOriginal = mano.localPosition;
            manoEscalaOriginal = mano.localScale;
            float escala = ReglaManosMN01VR.EscalaAntebrazoOculto;
            ReglaManosMN01VR.ColapsoAntebrazo(antebrazoPosOriginal, antebrazo.localRotation, Vector3.Scale(antebrazoEscalaOriginal, manoPosOriginal),
                ReglaManosMN01VR.RetrocesoCorteAntebrazo, escala, out Vector3 nuevaAntebrazo, out Vector3 nuevaMano);
            antebrazo.localPosition = nuevaAntebrazo;
            antebrazo.localScale = antebrazoEscalaOriginal * escala;
            mano.localPosition = new Vector3(nuevaMano.x / antebrazoEscalaOriginal.x, nuevaMano.y / antebrazoEscalaOriginal.y, nuevaMano.z / antebrazoEscalaOriginal.z);
            mano.localScale = manoEscalaOriginal / escala;
            antebrazoColapsado = true;
        }

        private void RestaurarAntebrazo()
        {
            if (!antebrazoColapsado)
            {
                return;
            }
            antebrazoColapsado = false;
            Transform antebrazo = Hueso(ReglaManosMN01VR.IndiceAntebrazo);
            Transform mano = Hueso(ReglaManosMN01VR.IndiceMano);
            if (antebrazo == null || mano == null)
            {
                return;
            }
            antebrazo.localPosition = antebrazoPosOriginal;
            antebrazo.localScale = antebrazoEscalaOriginal;
            mano.localPosition = manoPosOriginal;
            mano.localScale = manoEscalaOriginal;
        }

        /// <summary>
        /// La prótesis de brazos: con <paramref name="brillo"/> &gt; 0 la mano usa una copia del material de prótesis con la emisión cian por ese
        /// brillo (el pulso lo da quien llama); con 0 vuelve al material normal.
        /// </summary>
        public void FijarBrillo(float brillo)
        {
            bool quiere = brillo > 0f && materialProtesis != null;
            if (quiere && copiaProtesis == null)
            {
                copiaProtesis = new Material(materialProtesis) { name = materialProtesis.name + "_Pulso" };
                copiaProtesis.EnableKeyword("_EMISSION");
                copiaProtesis.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (quiere != brillando)
            {
                brillando = quiere;
                Material m = quiere ? copiaProtesis : materialNormal;
                if (m != null)
                {
                    foreach (Renderer r in mallas)
                    {
                        if (r != null)
                        {
                            r.sharedMaterial = m;
                        }
                    }
                }
            }
            if (quiere)
            {
                copiaProtesis.SetColor(IdEmision, ReglaManosMN01VR.ColorProtesis * brillo);
            }
        }

        /// <summary>Apaga las sombras y deja las mallas en la capa de la mano (lo hace quien la pone en el control).</summary>
        public void PrepararMallas(int capa)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = capa;
            }
            foreach (Renderer r in mallas)
            {
                if (r == null)
                {
                    continue;
                }
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = true;
                if (r is SkinnedMeshRenderer s)
                {
                    s.updateWhenOffscreen = true; // la mano va pegada a la cara: que no se recorte por bounds viejos
                    s.skinnedMotionVectors = false;
                }
            }
        }

        private void OnDestroy()
        {
            if (copiaProtesis != null)
            {
                Destroy(copiaProtesis);
            }
        }
    }
}
