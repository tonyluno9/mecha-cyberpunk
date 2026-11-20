using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1491: dibuja los trazos de las balas del jugador (un LineRenderer fino, corto y brillante que viaja del cañón al impacto en ~0.05 s y se
    /// desvanece). Lo crea <see cref="PistolaAPie"/>. En las armas automáticas solo lleva trazo uno de cada <see cref="ReglaTrazoBala.CadaCuantos"/>
    /// disparos (la escopeta saca un trazo por perdigón). Usa un pool fijo.
    /// </summary>
    public class TrazosBala : MonoBehaviour
    {
        private const int MaxTrazos = 24;

        private struct Trazo
        {
            public LineRenderer linea;
            public Vector3 desde;
            public Vector3 direccion;
            public float total;
            public float inicio;
            public Color color;
            public bool vivo;
        }

        private readonly Trazo[] trazos = new Trazo[MaxTrazos];
        private PistolaAPie pistola;
        private Material material;
        private int siguiente;
        private int contadorDisparos;

        /// <summary>Cuántos trazos se han dibujado (para las pruebas).</summary>
        public int Dibujados { get; private set; }

        /// <summary>Cuántos trazos están vivos ahora.</summary>
        public int Vivos
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxTrazos; i++)
                {
                    if (trazos[i].vivo)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        private void Awake()
        {
            pistola = GetComponent<PistolaAPie>();
            ConfigArmasEspeciales cfg = ConfigArmasEspeciales.Actual;
            material = cfg.MaterialRayo != null ? cfg.MaterialRayo : new Material(Shader.Find("Sprites/Default"));
        }

        private void OnEnable()
        {
            if (pistola != null)
            {
                pistola.Fogonazo += AlDisparar;
                pistola.Trazo += AlTrazo;
            }
        }

        private void OnDisable()
        {
            if (pistola != null)
            {
                pistola.Fogonazo -= AlDisparar;
                pistola.Trazo -= AlTrazo;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < MaxTrazos; i++)
            {
                if (trazos[i].linea != null)
                {
                    Destroy(trazos[i].linea.gameObject);
                }
            }
        }

        // El evento Trazo de un tiro sale antes que su Fogonazo: el tiro número k lleva trazo si k es múltiplo de CadaCuantos (el primero siempre).
        private void AlDisparar(ArmaAPie arma, Vector3 boca) => contadorDisparos++;

        private void AlTrazo(ArmaAPie arma, Vector3 desde, Vector3 hasta)
        {
            if (contadorDisparos % ReglaTrazoBala.CadaCuantos(arma) != 0)
            {
                return;
            }
            Vector3 delta = hasta - desde;
            float total = delta.magnitude;
            if (total < 0.5f)
            {
                return;
            }
            Poner(arma, desde, delta / total, total);
        }

        private void Poner(ArmaAPie arma, Vector3 desde, Vector3 direccion, float total)
        {
            ref Trazo t = ref trazos[siguiente];
            siguiente = (siguiente + 1) % MaxTrazos;
            if (t.linea == null)
            {
                t.linea = Crear();
            }
            t.desde = desde;
            t.direccion = direccion;
            t.total = total;
            t.inicio = Time.time;
            t.color = ReglaTrazoBala.ColorDe(arma);
            t.vivo = true;
            t.linea.enabled = true;
            Dibujados++;
            Pintar(ref t, 0f);
        }

        private LineRenderer Crear()
        {
            LineRenderer l = new GameObject("TrazoBala").AddComponent<LineRenderer>();
            l.useWorldSpace = true;
            l.positionCount = 2;
            l.widthMultiplier = ReglaTrazoBala.Ancho;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.sharedMaterial = material;
            l.enabled = false;
            return l;
        }

        private void Update()
        {
            for (int i = 0; i < MaxTrazos; i++)
            {
                if (!trazos[i].vivo)
                {
                    continue;
                }
                float edad = Time.time - trazos[i].inicio;
                if (edad >= ReglaTrazoBala.SegundosViaje + ReglaTrazoBala.SegundosDesvanecer)
                {
                    trazos[i].vivo = false;
                    trazos[i].linea.enabled = false;
                    continue;
                }
                Pintar(ref trazos[i], edad);
            }
        }

        private static void Pintar(ref Trazo t, float edad)
        {
            ReglaTrazoBala.Extremos(t.total, edad, out float cola, out float cabeza);
            t.linea.SetPosition(0, t.desde + t.direccion * cola);
            t.linea.SetPosition(1, t.desde + t.direccion * cabeza);
            Color c = t.color;
            float op = ReglaTrazoBala.Opacidad(edad);
            t.linea.startColor = new Color(c.r, c.g, c.b, 0f);
            t.linea.endColor = new Color(c.r, c.g, c.b, op);
        }
    }
}
