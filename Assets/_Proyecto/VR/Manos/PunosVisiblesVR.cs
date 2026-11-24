using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR.Manos
{
    /// <summary>
    /// VR35: las manos de PJ01 en puño, una en cada control, en lugar de los modelos de los controles.
    /// No copia vértices (la malla de PJ01 no es legible en la build): crea un SkinnedMeshRenderer con la misma malla y materiales
    /// del cuerpo, cuyos huesos de la mano y los dedos son copias propias (cerradas en puño) y todos los demás huesos apuntan a un
    /// hueso de escala casi cero en la muñeca, así el resto del cuerpo se colapsa a un punto y no se ve.
    /// El puño se alinea a la pose grip de OpenXR (en Unity: +Z del meñique al pulgar, por el hueco del puño; -Y hacia donde pega;
    /// la palma hacia -X en la derecha y +X en la izquierda), con el centro del puño en el origen de la grip.
    /// Va en el jugador; lo agrega <see cref="ArranqueCombateVR"/>. Si el modelo cambia (ropa, implantes), se rehace.
    /// </summary>
    public class PunosVisiblesVR : MonoBehaviour
    {
        /// <summary>Nombre del hijo de cada <see cref="ManoVR"/> con el puño.</summary>
        public const string NombrePuno = "PunoVR";

        private const string NombreModeloControl = "Modelo";
        private const float SegundosEntreRevisiones = 0.5f;
        private const float EscalaColapso = 1e-4f;
        private const float MedidaCaja = 0.6f;

        // Ángulos acumulados de cada falange respecto de la dirección de la mano (°): un puño cerrado sin que los dedos crucen la palma.
        private static readonly float[] CurvaDedos = { 80f, 180f, 250f };
        private static readonly float[] CurvaPulgar = { 15f, 30f, 30f };

        // Centro del hueco del puño respecto de los nudillos (fracción del largo muñeca-nudillo): hacia la palma y hacia la muñeca.
        private const float CentroHaciaPalma = 0.28f;
        private const float CentroHaciaMuneca = 0.17f;
        private const float FrenteNudillos = 0.1f;

        private AdaptadorVR adaptador;
        private readonly GameObject[] punos = new GameObject[2];
        private readonly Transform[] nudillos = new Transform[2];
        private readonly Vector3[] frenteLocal = new Vector3[2];
        private float siguienteRevision;
        private string firma;

        /// <summary>True si los dos puños están puestos.</summary>
        public bool Listo => punos[0] != null && punos[1] != null;

        /// <summary>El objeto del puño de una mano (null si aún no está).</summary>
        public GameObject Puno(bool derecha) => punos[derecha ? 1 : 0];

        /// <summary>El frente de los nudillos del puño en el mundo (para alinear la detección de golpes). Sin puño: el origen de la mano.</summary>
        public Vector3 PuntoNudillos(bool derecha)
        {
            int i = derecha ? 1 : 0;
            if (nudillos[i] != null)
            {
                return nudillos[i].TransformPoint(frenteLocal[i]);
            }
            ManoVR mano = Mano(derecha);
            return mano != null ? mano.transform.position : transform.position;
        }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
        }

        private void Update()
        {
            if (Time.unscaledTime < siguienteRevision)
            {
                return;
            }
            siguienteRevision = Time.unscaledTime + SegundosEntreRevisiones;
            if (adaptador == null)
            {
                adaptador = GetComponent<AdaptadorVR>();
                if (adaptador == null)
                {
                    return;
                }
            }
            if (!adaptador.Armado || adaptador.ManoIzquierda == null || adaptador.ManoDerecha == null)
            {
                return;
            }
            Animator animador = BuscarAnimador();
            string nombreDer = NombreHueso(animador, HumanBodyBones.RightHand, "R_Hand");
            List<SkinnedMeshRenderer> fuentes = BuscarFuentes(nombreDer);
            string nueva = Firma(fuentes);
            if (nueva == firma && Listo)
            {
                OcultarControles();
                return;
            }
            Quitar();
            if (fuentes.Count == 0)
            {
                return;
            }
            punos[0] = Construir(adaptador.ManoIzquierda, false, fuentes, animador);
            punos[1] = Construir(adaptador.ManoDerecha, true, fuentes, animador);
            if (Listo)
            {
                firma = nueva;
                OcultarControles();
                Debug.Log("[VR] Puños de PJ01 puestos en las manos (" + fuentes.Count + " mallas).");
            }
            else
            {
                Quitar();
            }
        }

        private void OnDestroy()
        {
            Quitar();
        }

        private ManoVR Mano(bool derecha)
        {
            if (adaptador == null)
            {
                return null;
            }
            return derecha ? adaptador.ManoDerecha : adaptador.ManoIzquierda;
        }

        private void Quitar()
        {
            for (int i = 0; i < punos.Length; i++)
            {
                if (punos[i] != null)
                {
                    Destroy(punos[i]);
                }
                punos[i] = null;
                nudillos[i] = null;
            }
            firma = null;
            MostrarControles(true);
        }

        private void OcultarControles()
        {
            MostrarControles(false);
        }

        private void MostrarControles(bool mostrar)
        {
            foreach (bool derecha in new[] { false, true })
            {
                ManoVR mano = Mano(derecha);
                if (mano == null)
                {
                    continue;
                }
                Transform modelo = mano.transform.Find(NombreModeloControl);
                if (modelo != null && modelo.gameObject.activeSelf != mostrar)
                {
                    modelo.gameObject.SetActive(mostrar);
                }
            }
        }

        private bool EnRig(Transform t)
        {
            return adaptador != null && adaptador.Rig != null && t.IsChildOf(adaptador.Rig);
        }

        private Animator BuscarAnimador()
        {
            foreach (Animator a in GetComponentsInChildren<Animator>())
            {
                if (a != null && a.isHuman && a.avatar != null && !EnRig(a.transform))
                {
                    return a;
                }
            }
            return null;
        }

        private static string NombreHueso(Animator animador, HumanBodyBones hueso, string respaldo)
        {
            if (animador != null)
            {
                Transform t = animador.GetBoneTransform(hueso);
                if (t != null)
                {
                    return t.name;
                }
            }
            return respaldo;
        }

        // Las mallas con piel del cuerpo (PJ01, ropa, implantes) que tienen el hueso de la mano.
        private List<SkinnedMeshRenderer> BuscarFuentes(string nombreMano)
        {
            var lista = new List<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer r in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (r == null || !r.enabled || r.sharedMesh == null || EnRig(r.transform))
                {
                    continue;
                }
                Transform[] huesos = r.bones;
                foreach (Transform h in huesos)
                {
                    if (h != null && h.name == nombreMano)
                    {
                        lista.Add(r);
                        break;
                    }
                }
            }
            return lista;
        }

        private static string Firma(List<SkinnedMeshRenderer> fuentes)
        {
            var sb = new StringBuilder();
            foreach (SkinnedMeshRenderer r in fuentes)
            {
                sb.Append(r.GetInstanceID()).Append(':').Append(r.sharedMesh.GetInstanceID()).Append(';');
            }
            return sb.ToString();
        }

        private GameObject Construir(ManoVR mano, bool derecha, List<SkinnedMeshRenderer> fuentes, Animator animador)
        {
            string nombreMano = NombreHueso(animador, derecha ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand, derecha ? "R_Hand" : "L_Hand");
            Transform manoViva = null;
            var nombresHuesos = new HashSet<string>();
            foreach (SkinnedMeshRenderer r in fuentes)
            {
                foreach (Transform h in r.bones)
                {
                    if (h == null)
                    {
                        continue;
                    }
                    nombresHuesos.Add(h.name);
                    if (manoViva == null && h.name == nombreMano)
                    {
                        manoViva = h;
                    }
                }
            }
            if (manoViva == null)
            {
                return null;
            }

            var raiz = new GameObject(NombrePuno);
            raiz.layer = mano.gameObject.layer;
            raiz.transform.SetParent(mano.transform, false);

            // Copia de los huesos de la mano y los dedos (solo los que usan las mallas; armas y sockets no).
            var mapa = new Dictionary<string, Transform>();
            Transform copiaMano = CopiarHuesos(manoViva, raiz.transform, nombresHuesos, mapa);
            copiaMano.SetPositionAndRotation(manoViva.position, manoViva.rotation);
            Vector3 escalaPadre = raiz.transform.lossyScale;
            Vector3 escalaViva = manoViva.lossyScale;
            copiaMano.localScale = new Vector3(
                escalaViva.x / Mathf.Max(1e-6f, escalaPadre.x),
                escalaViva.y / Mathf.Max(1e-6f, escalaPadre.y),
                escalaViva.z / Mathf.Max(1e-6f, escalaPadre.z));

            var colapso = new GameObject("Colapso").transform;
            colapso.SetParent(copiaMano, false);
            colapso.localScale = Vector3.one * EscalaColapso;

            float escalaMundo = Mathf.Max(1e-6f, Mathf.Abs(copiaMano.lossyScale.x));
            foreach (SkinnedMeshRenderer fuente in fuentes)
            {
                var objeto = new GameObject("Malla_" + fuente.name);
                objeto.layer = raiz.layer;
                objeto.transform.SetParent(raiz.transform, false);
                SkinnedMeshRenderer smr = objeto.AddComponent<SkinnedMeshRenderer>();
                Transform[] originales = fuente.bones;
                var huesos = new Transform[originales.Length];
                for (int i = 0; i < originales.Length; i++)
                {
                    huesos[i] = originales[i] != null && mapa.TryGetValue(originales[i].name, out Transform copia) ? copia : colapso;
                }
                smr.sharedMesh = fuente.sharedMesh;
                smr.sharedMaterials = fuente.sharedMaterials;
                smr.bones = huesos;
                smr.rootBone = copiaMano;
                smr.updateWhenOffscreen = false;
                smr.localBounds = new Bounds(Vector3.zero, Vector3.one * (MedidaCaja / escalaMundo));
                smr.shadowCastingMode = ShadowCastingMode.Off;
                smr.receiveShadows = true;
                smr.skinnedMotionVectors = false;
            }

            Transform indice = Buscar(mapa, animador, derecha ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal, derecha, "Index1");
            Transform medio = Buscar(mapa, animador, derecha ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal, derecha, "Mid1");
            Transform menique = Buscar(mapa, animador, derecha ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal, derecha, "Pinky1");
            if (indice == null || medio == null || menique == null)
            {
                // Sin dedos no se puede orientar: se queda como viene, en la grip.
                copiaMano.SetPositionAndRotation(mano.transform.position, mano.transform.rotation);
                nudillos[derecha ? 1 : 0] = copiaMano;
                frenteLocal[derecha ? 1 : 0] = Vector3.zero;
                return raiz;
            }

            // Ejes de la mano copiada: f hacia los nudillos, u hacia el lado del pulgar, palma según la mano.
            float largo = Vector3.Distance(medio.position, copiaMano.position);
            Vector3 f = (medio.position - copiaMano.position).normalized;
            Vector3 uCrudo = indice.position - menique.position;
            Vector3 u = (uCrudo - Vector3.Project(uCrudo, f)).normalized;
            Vector3 palma = PalmaDe(f, u, derecha);

            CerrarPuno(mapa, animador, derecha, f, palma);

            // Alinear: u -> +Z de la grip, -f -> +Y de la grip.
            Quaternion giro = Quaternion.LookRotation(mano.transform.forward, mano.transform.up) * Quaternion.Inverse(Quaternion.LookRotation(u, -f));
            copiaMano.rotation = giro * copiaMano.rotation;
            f = giro * f;
            palma = giro * palma;
            Vector3 baseNudillos = (indice.position + menique.position) * 0.5f;
            Vector3 centro = baseNudillos + palma * (CentroHaciaPalma * largo) - f * (CentroHaciaMuneca * largo);
            copiaMano.position += mano.transform.position - centro;

            int lado = derecha ? 1 : 0;
            nudillos[lado] = copiaMano;
            frenteLocal[lado] = copiaMano.InverseTransformPoint(medio.position + f * (FrenteNudillos * largo));
            return raiz;
        }

        /// <summary>
        /// Lógica pura: la dirección de la palma de una mano a partir de la dirección muñeca-nudillos (f) y la del lado del pulgar (u).
        /// En la grip de OpenXR (Unity) la palma de la derecha mira a -X y la de la izquierda a +X, con X = Cross(-f, u).
        /// </summary>
        public static Vector3 PalmaDe(Vector3 f, Vector3 u, bool derecha)
        {
            Vector3 x = Vector3.Cross(-f, u).normalized;
            return derecha ? -x : x;
        }

        private static Transform CopiarHuesos(Transform original, Transform padre, HashSet<string> nombres, Dictionary<string, Transform> mapa)
        {
            var copia = new GameObject(original.name).transform;
            copia.gameObject.layer = padre.gameObject.layer;
            copia.SetParent(padre, false);
            copia.SetLocalPositionAndRotation(original.localPosition, original.localRotation);
            copia.localScale = original.localScale;
            mapa[original.name] = copia;
            for (int i = 0; i < original.childCount; i++)
            {
                Transform hijo = original.GetChild(i);
                if (nombres.Contains(hijo.name) || TieneHuesoDebajo(hijo, nombres))
                {
                    CopiarHuesos(hijo, copia, nombres, mapa);
                }
            }
            return copia;
        }

        private static bool TieneHuesoDebajo(Transform t, HashSet<string> nombres)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform hijo = t.GetChild(i);
                if (nombres.Contains(hijo.name) || TieneHuesoDebajo(hijo, nombres))
                {
                    return true;
                }
            }
            return false;
        }

        private static Transform Buscar(Dictionary<string, Transform> mapa, Animator animador, HumanBodyBones hueso, bool derecha, string sufijo)
        {
            string nombre = NombreHueso(animador, hueso, (derecha ? "R_" : "L_") + sufijo);
            return mapa.TryGetValue(nombre, out Transform t) ? t : null;
        }

        private static void CerrarPuno(Dictionary<string, Transform> mapa, Animator animador, bool derecha, Vector3 f, Vector3 palma)
        {
            Vector3 eje = Vector3.Cross(f, palma).normalized;
            string[] dedos = { "Index", "Middle", "Ring", "Little" };
            string[] sufijos = { "Index", "Mid", "Ring", "Pinky" };
            string[] falanges = { "Proximal", "Intermediate", "Distal" };
            string lado = derecha ? "Right" : "Left";
            for (int d = 0; d < dedos.Length; d++)
            {
                var cadena = new Transform[3];
                for (int k = 0; k < 3; k++)
                {
                    cadena[k] = Buscar(mapa, animador, (HumanBodyBones)System.Enum.Parse(typeof(HumanBodyBones), lado + dedos[d] + falanges[k]), derecha, sufijos[d] + (k + 1));
                }
                for (int k = 0; k < 3; k++)
                {
                    Transform seg = cadena[k];
                    if (seg == null)
                    {
                        break;
                    }
                    Vector3 dir = DireccionSegmento(cadena, k, f);
                    if (dir.sqrMagnitude < 1e-10f)
                    {
                        continue;
                    }
                    Vector3 deseada = Quaternion.AngleAxis(CurvaDedos[k], eje) * f;
                    seg.rotation = Quaternion.FromToRotation(dir.normalized, deseada) * seg.rotation;
                }
            }

            var pulgar = new Transform[3];
            for (int k = 0; k < 3; k++)
            {
                pulgar[k] = Buscar(mapa, animador, (HumanBodyBones)System.Enum.Parse(typeof(HumanBodyBones), lado + "Thumb" + falanges[k]), derecha, "Thumb" + (k + 1));
            }
            for (int k = 0; k < 3; k++)
            {
                Transform seg = pulgar[k];
                if (seg == null)
                {
                    break;
                }
                Vector3 dir = DireccionSegmento(pulgar, k, f);
                Vector3 ejePulgar = Vector3.Cross(dir, palma);
                if (ejePulgar.sqrMagnitude < 1e-10f)
                {
                    continue;
                }
                seg.rotation = Quaternion.AngleAxis(CurvaPulgar[k], ejePulgar.normalized) * seg.rotation;
            }
        }

        // Dirección actual de una falange: hacia la siguiente; la última, hacia su primer hijo o como la anterior.
        private static Vector3 DireccionSegmento(Transform[] cadena, int k, Vector3 respaldo)
        {
            Transform seg = cadena[k];
            if (k + 1 < cadena.Length && cadena[k + 1] != null)
            {
                return cadena[k + 1].position - seg.position;
            }
            if (seg.childCount > 0)
            {
                return seg.GetChild(0).position - seg.position;
            }
            if (k > 0 && cadena[k - 1] != null)
            {
                return seg.position - cadena[k - 1].position;
            }
            return respaldo;
        }
    }
}
