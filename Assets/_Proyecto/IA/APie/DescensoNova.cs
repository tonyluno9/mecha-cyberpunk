using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1498: un soldado de Nova baja de la nave por una cuerda (LineRenderer): mientras baja, su IA y su controlador están apagados y se mueve en línea recta
    /// hasta el piso; al llegar los enciende y se queda como cualquier otro soldado. La cuerda se quita sola.
    /// </summary>
    public class DescensoNova : MonoBehaviour
    {
        private const float AlturaAgarre = 2f; // la cuerda lo toma por encima de la cabeza (m)
        private const float AnchoCuerda = 0.06f;

        private PandilleroAPie soldado;
        private CharacterController controlador;
        private Transform ancla;
        private Vector3 anclaLocal;
        private Vector3 anclaFija;
        private Vector3 inicio;
        private Vector3 suelo;
        private float velocidad;
        private float segundos;
        private LineRenderer cuerda;
        private CaidaNova caida;

        /// <summary>La secuencia de caída (aire, aterrizaje, levantarse); null si no hay clips configurados (para pruebas).</summary>
        public CaidaNova Caida => caida;

        /// <summary>True cuando ya tocó el piso y su IA volvió a funcionar.</summary>
        public bool Llego { get; private set; }

        /// <summary>La cuerda (para pruebas).</summary>
        public LineRenderer Cuerda => cuerda;

        /// <summary>Pone a bajar a <paramref name="soldado"/> desde <paramref name="ancla"/> (+<paramref name="anclaLocal"/>) hasta <paramref name="suelo"/>.</summary>
        public static DescensoNova Iniciar(PandilleroAPie soldado, Transform ancla, Vector3 anclaLocal, Vector3 suelo, float velocidad, Material material)
        {
            DescensoNova d = soldado.gameObject.AddComponent<DescensoNova>();
            d.soldado = soldado;
            d.controlador = soldado.GetComponent<CharacterController>();
            d.ancla = ancla;
            d.anclaLocal = anclaLocal;
            d.anclaFija = ancla.TransformPoint(anclaLocal);
            d.suelo = suelo;
            d.velocidad = velocidad;
            d.inicio = d.anclaFija - Vector3.up * AlturaAgarre;
            soldado.enabled = false;
            if (d.controlador != null)
            {
                d.controlador.enabled = false;
            }
            soldado.transform.position = d.inicio;
            d.cuerda = d.CrearCuerda(material);
            d.PintarCuerda();
            // T-1506: mientras baja juega Caida_Aire; al tocar el piso, Aterrizaje y Levantarse (null si no hay config de clips).
            d.caida = CaidaNova.Iniciar(soldado.gameObject, false);
            return d;
        }

        private LineRenderer CrearCuerda(Material material)
        {
            GameObject objeto = new GameObject("CuerdaNova");
            LineRenderer linea = objeto.AddComponent<LineRenderer>();
            linea.positionCount = 2;
            linea.useWorldSpace = true;
            linea.startWidth = AnchoCuerda;
            linea.endWidth = AnchoCuerda;
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Material usado = material;
            if (usado == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
                if (shader != null)
                {
                    usado = new Material(shader) { color = new Color(0.55f, 0.75f, 1f) };
                }
            }
            if (usado != null)
            {
                linea.sharedMaterial = usado;
            }
            linea.startColor = new Color(0.55f, 0.75f, 1f);
            linea.endColor = new Color(0.55f, 0.75f, 1f);
            return linea;
        }

        private Vector3 PuntoAncla()
        {
            if (ancla != null)
            {
                anclaFija = ancla.TransformPoint(anclaLocal);
            }
            return anclaFija;
        }

        private void PintarCuerda()
        {
            if (cuerda == null)
            {
                return;
            }
            cuerda.SetPosition(0, PuntoAncla());
            cuerda.SetPosition(1, transform.position + Vector3.up * AlturaAgarre);
        }

        private void Update()
        {
            if (Llego)
            {
                return;
            }
            segundos += Time.deltaTime;
            transform.position = ReglaNaveNova.PosicionDescenso(inicio, suelo, segundos, velocidad);
            PintarCuerda();
            if (Vector3.Distance(transform.position, suelo) < 0.01f)
            {
                Aterrizar();
            }
        }

        private void Aterrizar()
        {
            Llego = true;
            transform.position = suelo;
            if (caida != null)
            {
                caida.Aterrizar();
            }
            if (controlador != null)
            {
                controlador.enabled = true;
            }
            if (soldado != null)
            {
                soldado.enabled = true;
            }
            if (cuerda != null)
            {
                Destroy(cuerda.gameObject);
                cuerda = null;
            }
        }

        private void OnDestroy()
        {
            if (cuerda != null)
            {
                Destroy(cuerda.gameObject);
            }
        }
    }
}
