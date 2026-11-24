using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// V12 / VR15: la pantalla grande de "cine" en VR. Lo que en PC mueve la cámara (el recorrido de Menu_Calles, cinemáticas, el video del
    /// logo, la pantalla de carga) en VR se dibuja en una textura y se ve aquí: una sola pantalla CURVA (un trozo de cilindro con la cabeza en
    /// su eje, <see cref="Grados"/>° de ancho a <see cref="Radio"/> m, alto según el aspecto), quieta en el mundo frente a la cabeza.
    /// La cabeza del jugador nunca se mueve por una animación: solo su cabeza real mueve la vista.
    /// <list type="bullet">
    /// <item>Fondo: varios dueños pueden pedirlo a la vez; se ve la textura del de mayor prioridad (a igual prioridad, el último).</item>
    /// <item>Encima (VR15): una capa con alfa premultiplicado compuesta sobre el fondo, como los Canvas Overlay sobre la cámara en PC
    /// (Inicio: el menú, el video del logo y la carga, ver <see cref="InicioCineVR"/>). Sin fondo pedido, el fondo es negro.</item>
    /// </list>
    /// Se dibuja sin profundidad (ZTest Always: las paredes no la tapan) y con orden de dibujo bajo: los paneles de menú y los fundidos
    /// (más cerca y con más orden) siguen encima de ella.
    /// </summary>
    public class PantallaCineVR : MonoBehaviour
    {
        /// <summary>VR15: radio de la pantalla curva (m): la distancia de la cabeza a cualquier punto de ella.</summary>
        public const float Radio = 3.5f;
        /// <summary>VR15: ancho de la pantalla (°): casi toda la vista del visor (Quest 3 ≈ 110°).</summary>
        public const float Grados = 105f;
        /// <summary>Distancia de la pantalla a la cabeza (m) = <see cref="Radio"/>.</summary>
        public const float Distancia = Radio;
        /// <summary>Cuánto sube el centro de la pantalla sobre los ojos (m): centrada en la vista.</summary>
        public const float SubidaCentro = 0f;
        /// <summary>Aspecto si una textura no lo dice (ancho/alto).</summary>
        public const float AspectoPorDefecto = 16f / 9f;
        /// <summary>Orden de dibujo del fondo y de la capa de encima (los Canvas del mundo y los fundidos usan órdenes mayores).</summary>
        public const int OrdenFondo = -50;
        public const int OrdenEncima = -49;
        private const int Segmentos = 64;
        private const int Cola = 3000;
        private const float DiferenciaAspecto = 0.01f;
        // Shader premultiplicado (el del HUD de casco: UI/Default dejó la textura premultiplicada; con UI/Default encima lo translúcido
        // saldría más oscuro). Si no está, UI/Default.
        private const string ShaderEncima = "Hidden/MechaCyberpunk/HudCascoVR";

        private sealed class Pedido
        {
            public object Dueno;
            public Texture Textura;
            public float Aspecto;
            public int Prioridad;
            public int Orden;
        }

        private static PantallaCineVR actual;
        private static int contador;
        private readonly List<Pedido> pedidos = new List<Pedido>();

        private MeshRenderer fondo;
        private MeshRenderer encima;
        private MeshFilter filtroFondo;
        private MeshFilter filtroEncima;
        private Material materialFondo;
        private Material materialEncima;
        private Mesh malla;
        private float aspectoMalla = -1f;
        private float alto;
        private object duenoEncima;
        private Texture texturaEncima;
        private Texture texturaFondo;
        private bool visible;
        private Transform cabezaColocada;

        /// <summary>La pantalla (null si nadie la ha pedido).</summary>
        public static PantallaCineVR Actual => actual;
        /// <summary>True si hay algo en la pantalla.</summary>
        public static bool Visible => actual != null && actual.visible;
        /// <summary>La textura del fondo que se ve (null si nada; negro si solo hay capa de encima).</summary>
        public static Texture TexturaActual => Visible ? actual.texturaFondo : null;
        /// <summary>VR15: la capa compuesta encima del fondo (null si no hay).</summary>
        public static Texture TexturaEncima => Visible ? actual.texturaEncima : null;
        /// <summary>Centro de la pantalla en el mundo (el punto de la curva frente a la cabeza).</summary>
        public static Vector3 Centro => actual != null ? actual.transform.TransformPoint(Vector3.forward * Radio) : Vector3.zero;
        /// <summary>VR15: el eje de la curva (donde estaba la cabeza al colocarla), mirando al centro.</summary>
        public static Transform Raiz => actual != null ? actual.transform : null;
        /// <summary>Ancho visible en metros, medido por la curva (para las pruebas).</summary>
        public static float AnchoMundo => actual != null ? Radio * Grados * Mathf.Deg2Rad : 0f;
        /// <summary>VR15: alto de la pantalla (m), según el aspecto de lo que se ve.</summary>
        public static float AltoMundo => actual != null ? actual.alto : 0f;

        /// <summary>Muestra <paramref name="textura"/> en la pantalla grande por <paramref name="dueno"/>. aspecto = ancho/alto (≤0: el de la textura).</summary>
        public static void Mostrar(object dueno, Texture textura, float aspecto = 0f, int prioridad = 0)
        {
            if (dueno == null || textura == null)
            {
                return;
            }
            PantallaCineVR p = Asegurar();
            Pedido pedido = p.pedidos.Find(x => ReferenceEquals(x.Dueno, dueno));
            if (pedido == null)
            {
                pedido = new Pedido { Dueno = dueno };
                p.pedidos.Add(pedido);
            }
            pedido.Textura = textura;
            pedido.Aspecto = aspecto > 0f ? aspecto : (textura.height > 0 ? (float)textura.width / textura.height : AspectoPorDefecto);
            pedido.Prioridad = prioridad;
            pedido.Orden = ++contador;
            p.Aplicar();
        }

        /// <summary>Quita lo que puso <paramref name="dueno"/>. Si no queda nada, la pantalla se apaga.</summary>
        public static void Ocultar(object dueno)
        {
            if (actual == null || dueno == null)
            {
                return;
            }
            actual.pedidos.RemoveAll(x => ReferenceEquals(x.Dueno, dueno));
            actual.Aplicar();
        }

        /// <summary>True si <paramref name="dueno"/> tiene algo pedido en el fondo.</summary>
        public static bool Tiene(object dueno) => actual != null && actual.pedidos.Exists(x => ReferenceEquals(x.Dueno, dueno));

        /// <summary>
        /// VR15: pone una capa con alfa premultiplicado encima de lo que se vea (una sola a la vez: la del último que la pide).
        /// Es lo que en PC son los Canvas sobre la cámara: el menú sobre las calles, la carga, el video del logo.
        /// </summary>
        public static void MostrarEncima(object dueno, Texture textura)
        {
            if (dueno == null || textura == null)
            {
                return;
            }
            PantallaCineVR p = Asegurar();
            p.duenoEncima = dueno;
            p.texturaEncima = textura;
            p.Aplicar();
        }

        /// <summary>VR15: quita la capa de encima si es de <paramref name="dueno"/>.</summary>
        public static void OcultarEncima(object dueno)
        {
            if (actual == null || dueno == null || !ReferenceEquals(actual.duenoEncima, dueno))
            {
                return;
            }
            actual.duenoEncima = null;
            actual.texturaEncima = null;
            actual.Aplicar();
        }

        /// <summary>VR15: true si la capa de encima es de <paramref name="dueno"/>.</summary>
        public static bool TieneEncima(object dueno) => actual != null && dueno != null && ReferenceEquals(actual.duenoEncima, dueno);

        /// <summary>Vuelve a poner la pantalla frente a la cabeza actual (al cambiar de rig o al recentrar).</summary>
        public static void Recolocar()
        {
            if (actual != null)
            {
                actual.cabezaColocada = null;
                actual.Colocar();
            }
        }

        /// <summary>
        /// VR15: dónde toca un rayo del mundo (el láser) la pantalla: UV 0..1 (0,0 abajo a la izquierda, vista desde la cabeza), el punto
        /// y la distancia. False si no se ve o el rayo no cae dentro de sus bordes.
        /// </summary>
        public static bool Tocar(Ray rayoMundo, out Vector2 uv, out Vector3 punto, out float distancia)
        {
            uv = Vector2.zero;
            punto = Vector3.zero;
            distancia = 0f;
            if (!Visible)
            {
                return false;
            }
            Transform t = actual.transform;
            var local = new Ray(t.InverseTransformPoint(rayoMundo.origin), t.InverseTransformDirection(rayoMundo.direction));
            if (!MallaCurvaVR.Tocar(local, Radio, Grados, actual.alto, out uv, out float distanciaLocal))
            {
                return false;
            }
            punto = t.TransformPoint(local.origin + local.direction * distanciaLocal);
            distancia = Vector3.Distance(rayoMundo.origin, punto);
            return true;
        }

        /// <summary>VR15: el punto del mundo de la pantalla en estas UV (para apuntar a algo que se ve en ella).</summary>
        public static Vector3 PuntoEnUV(Vector2 uv)
        {
            if (actual == null)
            {
                return Vector3.zero;
            }
            float angulo = (uv.x - 0.5f) * Grados * Mathf.Deg2Rad;
            var local = new Vector3(Mathf.Sin(angulo) * Radio, (uv.y - 0.5f) * actual.alto, Mathf.Cos(angulo) * Radio);
            return actual.transform.TransformPoint(local);
        }

        private static PantallaCineVR Asegurar()
        {
            if (actual != null)
            {
                return actual;
            }
            var objeto = new GameObject("PantallaCineVR");
            DontDestroyOnLoad(objeto);
            actual = objeto.AddComponent<PantallaCineVR>();
            actual.Construir();
            return actual;
        }

        private void Construir()
        {
            materialFondo = MallaCurvaVR.Material(null, true, Cola);
            materialFondo.name = "PantallaCineVR_Fondo";
            Shader premultiplicado = Shader.Find(ShaderEncima);
            if (premultiplicado != null && premultiplicado.isSupported)
            {
                materialEncima = new Material(premultiplicado) { name = "PantallaCineVR_Encima", renderQueue = Cola };
            }
            else
            {
                Debug.LogWarning("[VR] No está el shader " + ShaderEncima + ": la capa de encima de la pantalla grande usa UI/Default (lo translúcido sale algo más oscuro).");
                materialEncima = MallaCurvaVR.Material(null, true, Cola);
                materialEncima.name = "PantallaCineVR_Encima";
            }
            fondo = NuevaCapa("Fondo", materialFondo, OrdenFondo, out filtroFondo);
            encima = NuevaCapa("Encima", materialEncima, OrdenEncima, out filtroEncima);
            ArmarMalla(AspectoPorDefecto);
            fondo.enabled = false;
            encima.enabled = false;
        }

        private MeshRenderer NuevaCapa(string nombre, Material material, int orden, out MeshFilter filtro)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform, false);
            filtro = objeto.AddComponent<MeshFilter>();
            MeshRenderer r = objeto.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = orden;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            r.allowOcclusionWhenDynamic = false;
            return r;
        }

        // El ancho (°) es fijo; el alto sale del aspecto: largo del arco / aspecto.
        private void ArmarMalla(float aspecto)
        {
            aspecto = Mathf.Max(0.1f, aspecto);
            if (malla != null && Mathf.Abs(aspecto - aspectoMalla) < DiferenciaAspecto)
            {
                return;
            }
            if (malla != null)
            {
                Destroy(malla);
            }
            aspectoMalla = aspecto;
            alto = Radio * Grados * Mathf.Deg2Rad / aspecto;
            malla = MallaCurvaVR.Crear(Radio, Grados, alto, Segmentos);
            malla.name = "PantallaCineVR";
            filtroFondo.sharedMesh = malla;
            filtroEncima.sharedMesh = malla;
        }

        private void Aplicar()
        {
            pedidos.RemoveAll(x => x.Textura == null || x.Dueno == null || (x.Dueno is Object o && o == null));
            if (duenoEncima is Object de && de == null)
            {
                duenoEncima = null;
                texturaEncima = null;
            }
            Pedido mejor = null;
            foreach (Pedido p in pedidos)
            {
                if (mejor == null || p.Prioridad > mejor.Prioridad || (p.Prioridad == mejor.Prioridad && p.Orden > mejor.Orden))
                {
                    mejor = p;
                }
            }
            bool ver = mejor != null || texturaEncima != null;
            if (ver && !visible)
            {
                cabezaColocada = null; // al aparecer se pone frente a donde mira
            }
            visible = ver;
            fondo.enabled = ver;
            encima.enabled = ver && texturaEncima != null;
            if (!ver)
            {
                texturaFondo = null;
                materialFondo.mainTexture = null;
                materialEncima.mainTexture = null;
                return;
            }
            texturaFondo = mejor != null ? mejor.Textura : Texture2D.blackTexture;
            float aspecto = mejor != null ? mejor.Aspecto
                : (texturaEncima.height > 0 ? (float)texturaEncima.width / texturaEncima.height : AspectoPorDefecto);
            ArmarMalla(aspecto);
            materialFondo.mainTexture = texturaFondo;
            materialEncima.mainTexture = texturaEncima;
            Colocar();
        }

        private void LateUpdate()
        {
            if (!visible)
            {
                return;
            }
            // Un dueño destruido (escena descargada) ya no pide nada.
            bool muerto = duenoEncima is Object de && de == null;
            foreach (Pedido p in pedidos)
            {
                if (p.Dueno is Object o && o == null)
                {
                    muerto = true;
                }
            }
            if (muerto)
            {
                Aplicar();
            }
            Colocar();
        }

        // Quieta en el mundo: solo se coloca al aparecer o cuando cambia la cabeza (rig de menús → rig del jugador).
        // El eje de la curva va en los ojos: toda la pantalla queda a la misma distancia.
        private void Colocar()
        {
            Camera cabeza = SistemaVR.CamaraVR;
            if (cabeza == null || cabezaColocada == cabeza.transform)
            {
                return;
            }
            cabezaColocada = cabeza.transform;
            // No cuelga del rig: el rig de menús se destruye al cargar y se la llevaría (vive en DontDestroyOnLoad).
            Transform raiz = RaizDe(cabeza.transform);
            Vector3 frente = cabeza.transform.forward;
            frente.y = 0f;
            if (frente.sqrMagnitude < 1e-4f)
            {
                frente = raiz != null ? raiz.forward : Vector3.forward;
                frente.y = 0f;
            }
            if (frente.sqrMagnitude < 1e-4f)
            {
                frente = Vector3.forward;
            }
            frente.Normalize();
            transform.SetPositionAndRotation(cabeza.transform.position + Vector3.up * SubidaCentro, Quaternion.LookRotation(frente, Vector3.up));
        }

        private static Transform RaizDe(Transform cabeza)
        {
            Unity.XR.CoreUtils.XROrigin origen = cabeza.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            return origen != null ? origen.transform : cabeza.root;
        }

        private void OnDestroy()
        {
            if (actual == this)
            {
                actual = null;
            }
            if (malla != null)
            {
                Destroy(malla);
            }
            if (materialFondo != null)
            {
                Destroy(materialFondo);
            }
            if (materialEncima != null)
            {
                Destroy(materialEncima);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            actual = null;
            contador = 0;
        }
    }
}
