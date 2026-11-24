using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// V12: la pantalla grande de "cine" en VR. Lo que en PC mueve la cámara (el recorrido de Menu_Calles, cinemáticas, el video del logo,
    /// la pantalla de carga) en VR se dibuja en una textura y se ve aquí, quieto en el mundo, a <see cref="Distancia"/> frente a la cabeza.
    /// La cabeza del jugador nunca se mueve por una animación: solo su cabeza real mueve la vista.
    /// Varios dueños pueden pedirla a la vez: se ve la textura del de mayor prioridad (a igual prioridad, el último).
    /// Es un Canvas World Space con un RawImage (el shader de UI siempre está en el build y va bien en estéreo).
    /// </summary>
    public class PantallaCineVR : MonoBehaviour
    {
        /// <summary>Distancia de la pantalla a la cabeza (m).</summary>
        public const float Distancia = 4f;
        /// <summary>Ancho de la pantalla (m): ~64° de vista a 4 m.</summary>
        public const float Ancho = 5f;
        /// <summary>Cuánto sube el centro de la pantalla sobre los ojos (m) en el juego (cinemáticas).</summary>
        public const float SubidaCentro = 0.35f;
        /// <summary>En Inicio (rig de menús) sube más: su borde de abajo queda arriba del menú (centro 0.6 m bajo los ojos a 1.5 m).</summary>
        public const float SubidaCentroInicio = 1.05f;

        /// <summary>La subida que toca ahora.</summary>
        public static float SubidaActual => MenusVR.Actual != null && MenusVR.Actual.RaizRigMenus != null ? SubidaCentroInicio : SubidaCentro;
        private const float PixelesPorMetro = 400f;
        private const int OrdenLienzo = -50;

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
        private static Material materialEncima;
        private readonly List<Pedido> pedidos = new List<Pedido>();

        private Canvas lienzo;
        private RectTransform marco;
        private RawImage imagen;
        private AspectRatioFitter ajuste;
        private Transform cabezaColocada;

        /// <summary>La pantalla (null si nadie la ha pedido).</summary>
        public static PantallaCineVR Actual => actual;
        /// <summary>True si hay algo en la pantalla.</summary>
        public static bool Visible => actual != null && actual.lienzo != null && actual.lienzo.enabled;
        /// <summary>La textura que se ve (null si nada).</summary>
        public static Texture TexturaActual => Visible ? actual.imagen.texture : null;
        /// <summary>Centro de la pantalla en el mundo.</summary>
        public static Vector3 Centro => actual != null ? actual.transform.position : Vector3.zero;
        /// <summary>Ancho visible en metros (para las pruebas).</summary>
        public static float AnchoMundo => actual != null ? actual.marco.rect.width * actual.transform.lossyScale.x : 0f;

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
            pedido.Aspecto = aspecto > 0f ? aspecto : (textura.height > 0 ? (float)textura.width / textura.height : 16f / 9f);
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

        /// <summary>True si <paramref name="dueno"/> tiene algo pedido.</summary>
        public static bool Tiene(object dueno) => actual != null && actual.pedidos.Exists(x => ReferenceEquals(x.Dueno, dueno));

        /// <summary>Vuelve a poner la pantalla frente a la cabeza actual (al cambiar de rig o al recentrar).</summary>
        public static void Recolocar()
        {
            if (actual != null)
            {
                actual.cabezaColocada = null;
                actual.Colocar();
            }
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
            lienzo = gameObject.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.WorldSpace;
            lienzo.sortingOrder = OrdenLienzo;
            // Es World Space desde el principio: MenusVR no la convierte ni la mueve.
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(Ancho * PixelesPorMetro, Ancho * PixelesPorMetro);
            transform.localScale = Vector3.one / PixelesPorMetro;

            marco = NuevoHijo("Marco");
            marco.anchorMin = marco.anchorMax = new Vector2(0.5f, 0.5f);
            var fondo = marco.gameObject.AddComponent<Image>();
            fondo.color = Color.black;
            fondo.raycastTarget = false;
            fondo.material = MaterialEncima();

            var objetoImagen = new GameObject("Imagen", typeof(RectTransform));
            objetoImagen.transform.SetParent(marco, false);
            var rectImagen = (RectTransform)objetoImagen.transform;
            rectImagen.anchorMin = Vector2.zero;
            rectImagen.anchorMax = Vector2.one;
            rectImagen.offsetMin = rectImagen.offsetMax = Vector2.zero;
            imagen = objetoImagen.AddComponent<RawImage>();
            imagen.raycastTarget = false;
            imagen.material = MaterialEncima();
            ajuste = marco.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            marco.sizeDelta = new Vector2(Ancho * PixelesPorMetro, Ancho * PixelesPorMetro * 9f / 16f);
            lienzo.enabled = false;
        }

        // V12-B: la pantalla no la tapan las paredes (la clínica, cuartos chicos: a 4 m casi siempre hay una). El material de UI con ZTest Always
        // la dibuja encima del mundo; los menús (orden mayor) y los fundidos siguen encima de ella.
        private static Material MaterialEncima()
        {
            if (materialEncima == null)
            {
                materialEncima = new Material(Canvas.GetDefaultCanvasMaterial()) { name = "PantallaCineVR_Encima" };
                materialEncima.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            return materialEncima;
        }

        private RectTransform NuevoHijo(string nombre)
        {
            var o = new GameObject(nombre, typeof(RectTransform));
            o.transform.SetParent(transform, false);
            return (RectTransform)o.transform;
        }

        private void Aplicar()
        {
            Pedido mejor = null;
            foreach (Pedido p in pedidos)
            {
                if (p.Textura == null)
                {
                    continue;
                }
                if (mejor == null || p.Prioridad > mejor.Prioridad || (p.Prioridad == mejor.Prioridad && p.Orden > mejor.Orden))
                {
                    mejor = p;
                }
            }
            pedidos.RemoveAll(x => x.Textura == null || x.Dueno == null || (x.Dueno is Object o && o == null));
            bool ver = mejor != null;
            if (ver && !lienzo.enabled)
            {
                cabezaColocada = null; // al aparecer se pone frente a donde mira
            }
            lienzo.enabled = ver;
            if (ver)
            {
                imagen.texture = mejor.Textura;
                ajuste.aspectRatio = mejor.Aspecto;
                marco.sizeDelta = new Vector2(Ancho * PixelesPorMetro, Ancho * PixelesPorMetro / Mathf.Max(0.1f, mejor.Aspecto));
                Colocar();
            }
            else
            {
                imagen.texture = null;
            }
        }

        private void LateUpdate()
        {
            if (lienzo == null || !lienzo.enabled)
            {
                return;
            }
            // Un dueño destruido (escena descargada) ya no pide nada.
            int antes = pedidos.Count;
            pedidos.RemoveAll(x => x.Dueno is Object o && o == null);
            if (pedidos.Count != antes)
            {
                Aplicar();
            }
            Colocar();
        }

        // Quieta en el mundo: solo se coloca al aparecer o cuando cambia la cabeza (rig de menús → rig del jugador).
        private void Colocar()
        {
            Camera cabeza = SistemaVR.CamaraVR;
            if (cabeza == null)
            {
                return;
            }
            if (cabezaColocada == cabeza.transform)
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
            frente.Normalize();
            transform.position = cabeza.transform.position + frente * Distancia + Vector3.up * SubidaActual;
            transform.rotation = Quaternion.LookRotation(frente, Vector3.up);
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
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            actual = null;
            contador = 0;
            materialEncima = null;
        }
    }
}
