using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.InputSystem;
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
    /// VR40: si están los prefabs de las manos nuevas MN01 (Resources/ManosVR, los hace ImportarManosVR40), se usan esas en vez del puño de PJ01:
    /// el grip analógico mezcla Abierta→Puño, el gatillo dobla el índice, con arma la derecha va en AgarrePistola (la izquierda en el guardamanos
    /// con armas largas o con el cargador en la mano), granada en la mano que la lleva, Señalar con el menú abierto; y con la prótesis de brazos
    /// brillan en cian con pulso. Se alinean a la grip igual que el puño (centro del puño en el origen de la grip). Sin esos prefabs queda el puño
    /// de PJ01 de VR35.
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

        /// <summary>VR40: prefabs de las manos MN01 en Resources.</summary>
        public const string RecursoManoDerecha = "ManosVR/MN01_ManoVR_R";
        public const string RecursoManoIzquierda = "ManosVR/MN01_ManoVR_L";

        private AdaptadorVR adaptador;
        private readonly GameObject[] punos = new GameObject[2];
        private readonly ManoMN01VR[] mn01 = new ManoMN01VR[2];
        private GameObject prefabMN01Der;
        private GameObject prefabMN01Izq;
        private bool buscoMN01;
        private InputAction[] gripValor;
        private InputAction[] gripBoton;
        private InputAction[] gatilloValor;
        private InputAction[] gatilloBoton;
        private PistolaVR pistolaVR;
        private ArmasEspecialesVR especiales;
        private RecargaManualVR recarga;
        private AgarreImplanteVR agarre;
        private readonly Transform[] nudillos = new Transform[2];
        private readonly Vector3[] frenteLocal = new Vector3[2];
        private float siguienteRevision;
        private string firma;

        /// <summary>True si los dos puños están puestos.</summary>
        public bool Listo => punos[0] != null && punos[1] != null;

        /// <summary>VR40: true si las manos visibles son las MN01 nuevas (no el puño de PJ01).</summary>
        public bool UsaManosMN01 => mn01[0] != null && mn01[1] != null;

        /// <summary>VR40: la mano MN01 de un lado (null si no se usan).</summary>
        public ManoMN01VR ManoMN01(bool derecha) => mn01[derecha ? 1 : 0];

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
            gripValor = Acciones("VR40_Grip", "/{Grip}", InputActionType.Value);
            gripBoton = Acciones("VR40_GripBoton", "/{GripButton}", InputActionType.Button);
            gatilloValor = Acciones("VR40_Gatillo", "/{Trigger}", InputActionType.Value);
            gatilloBoton = Acciones("VR40_GatilloBoton", "/{TriggerButton}", InputActionType.Button);
        }

        private static InputAction[] Acciones(string nombre, string control, InputActionType tipo)
        {
            return new[]
            {
                new InputAction(nombre + "Izq", tipo, EntradaVR.Izq + control),
                new InputAction(nombre + "Der", tipo, EntradaVR.Der + control),
            };
        }

        private IEnumerable<InputAction> TodasLasAcciones()
        {
            foreach (InputAction[] par in new[] { gripValor, gripBoton, gatilloValor, gatilloBoton })
            {
                if (par == null)
                {
                    continue;
                }
                foreach (InputAction a in par)
                {
                    if (a != null)
                    {
                        yield return a;
                    }
                }
            }
        }

        private void OnEnable()
        {
            foreach (InputAction a in TodasLasAcciones())
            {
                a.Enable();
            }
        }

        private void OnDisable()
        {
            foreach (InputAction a in TodasLasAcciones())
            {
                a.Disable();
            }
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
            if (RevisarMN01())
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
            foreach (InputAction a in TodasLasAcciones())
            {
                a.Dispose();
            }
        }

        // ---- VR40: manos MN01 ----

        // True si se usan (o se acaban de poner) las manos MN01; false para seguir con el puño de PJ01.
        private bool RevisarMN01()
        {
            if (!buscoMN01)
            {
                buscoMN01 = true;
                prefabMN01Der = Resources.Load<GameObject>(RecursoManoDerecha);
                prefabMN01Izq = Resources.Load<GameObject>(RecursoManoIzquierda);
                if (prefabMN01Der == null || prefabMN01Izq == null)
                {
                    Debug.LogWarning("[VR] VR40: faltan los prefabs de las manos MN01 (corre ImportarManosVR40.Aplicar); quedan los puños de PJ01.", this);
                }
            }
            if (prefabMN01Der == null || prefabMN01Izq == null)
            {
                return false;
            }
            BuscarEstados();
            if (UsaManosMN01 && punos[0] != null && punos[1] != null)
            {
                OcultarControles();
                return true;
            }
            Quitar();
            ManoMN01VR izq = ConstruirMN01(adaptador.ManoIzquierda, false, prefabMN01Izq);
            ManoMN01VR der = ConstruirMN01(adaptador.ManoDerecha, true, prefabMN01Der);
            if (izq == null || der == null)
            {
                Quitar();
                prefabMN01Der = null; // no sirven: el puño de PJ01
                prefabMN01Izq = null;
                Debug.LogWarning("[VR] VR40: los prefabs de las manos MN01 no traen sus 17 huesos y poses; quedan los puños de PJ01.", this);
                return false;
            }
            mn01[0] = izq;
            mn01[1] = der;
            firma = "MN01";
            OcultarControles();
            Debug.Log("[VR] VR40: manos MN01 puestas en los controles.");
            return true;
        }

        private void BuscarEstados()
        {
            if (pistolaVR == null)
            {
                pistolaVR = GetComponent<PistolaVR>();
            }
            if (especiales == null)
            {
                especiales = GetComponent<ArmasEspecialesVR>();
            }
            if (recarga == null)
            {
                recarga = GetComponent<RecargaManualVR>();
            }
            if (agarre == null)
            {
                agarre = GetComponent<AgarreImplanteVR>();
            }
        }

        private ManoMN01VR ConstruirMN01(ManoVR mano, bool derecha, GameObject prefab)
        {
            GameObject objeto = Instantiate(prefab, mano.transform, false);
            objeto.name = NombrePuno;
            ManoMN01VR m = objeto.GetComponent<ManoMN01VR>();
            if (m == null || !m.Valida)
            {
                Destroy(objeto);
                return null;
            }
            m.PrepararMallas(mano.gameObject.layer);
            m.PonerPose(PoseManoVR.Libre, 0f, 0f);
            int lado = derecha ? 1 : 0;
            punos[lado] = objeto;
            Alinear(m, mano, derecha, lado);
            return m;
        }

        // Como el puño de PJ01: u (lado del pulgar) -> +Z de la grip, -f (muñeca->nudillos) -> +Y, y el centro del puño en el origen de la grip.
        private void Alinear(ManoMN01VR m, ManoVR mano, bool derecha, int lado)
        {
            Transform t = m.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            Vector3 escalaPadre = mano.transform.lossyScale;
            t.localScale = new Vector3(1f / Mathf.Max(1e-6f, escalaPadre.x), 1f / Mathf.Max(1e-6f, escalaPadre.y), 1f / Mathf.Max(1e-6f, escalaPadre.z));
            Transform muneca = m.Hueso(ReglaManosMN01VR.IndiceMano);
            Transform indice = m.Hueso(ReglaManosMN01VR.IndiceIndice1);
            Transform medio = m.Hueso(ReglaManosMN01VR.IndiceMedio1);
            Transform menique = m.Hueso(ReglaManosMN01VR.IndiceMenique1);
            float largo = Vector3.Distance(medio.position, muneca.position);
            Vector3 f = (medio.position - muneca.position).normalized;
            Vector3 uCrudo = indice.position - menique.position;
            Vector3 u = (uCrudo - Vector3.Project(uCrudo, f)).normalized;
            if (largo < 1e-4f || u.sqrMagnitude < 1e-6f)
            {
                nudillos[lado] = t;
                frenteLocal[lado] = Vector3.zero;
                return;
            }
            Vector3 palma = PalmaDe(f, u, derecha);
            Quaternion giro = Quaternion.LookRotation(mano.transform.forward, mano.transform.up) * Quaternion.Inverse(Quaternion.LookRotation(u, -f));
            t.rotation = giro * t.rotation;
            f = giro * f;
            palma = giro * palma;
            Vector3 baseNudillos = (indice.position + menique.position) * 0.5f;
            Vector3 centro = baseNudillos + palma * (CentroHaciaPalma * largo) - f * (CentroHaciaMuneca * largo);
            t.position += mano.transform.position - centro;
            nudillos[lado] = muneca;
            frenteLocal[lado] = muneca.InverseTransformPoint(medio.position + f * (FrenteNudillos * largo));
        }

        // Cada cuadro, después de las armas (que deciden qué hay en la mano): la pose de cada mano MN01 y el brillo de la prótesis.
        private void LateUpdate()
        {
            if (!UsaManosMN01)
            {
                return;
            }
            float mezcla = ReglaManosMN01VR.Mezcla(Time.deltaTime);
            bool armaVisible = pistolaVR != null && pistolaVR.enabled && pistolaVR.ArmaVisible;
            bool armaLarga = armaVisible && !ReglasArmaAPie.UsaPoseDePistola(pistolaVR.ArmaMostrada);
            bool granada = especiales != null && especiales.enabled && especiales.GranadaTomada;
            bool cargador = recarga != null && recarga.enabled && recarga.Estado == EstadoCargadorVR.EnMano;
            float brillo = agarre != null && agarre.TieneImplante ? ReglaManosMN01VR.Pulso(Time.time) : 0f;
            for (int lado = 0; lado < 2; lado++)
            {
                ManoMN01VR m = mn01[lado];
                if (m == null)
                {
                    continue;
                }
                bool derecha = lado == 1;
                PoseManoVR pose = ReglaManosMN01VR.Elegir(derecha, MenusVR.MenuAbierto, MenusVR.UIApuntada, armaVisible, armaLarga, granada, cargador);
                float grip = Mathf.Max(Valor(gripValor, lado), Boton(gripBoton, lado));
                float gatillo = Mathf.Max(Valor(gatilloValor, lado), Boton(gatilloBoton, lado));
                m.Aplicar(pose, grip, gatillo, mezcla);
                m.FijarBrillo(brillo);
            }
        }

        private static float Valor(InputAction[] par, int lado)
        {
            InputAction a = par != null ? par[lado] : null;
            return a != null && a.enabled ? Mathf.Clamp01(a.ReadValue<float>()) : 0f;
        }

        private static float Boton(InputAction[] par, int lado)
        {
            InputAction a = par != null ? par[lado] : null;
            return a != null && a.enabled && a.IsPressed() ? 1f : 0f;
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
                mn01[i] = null;
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
