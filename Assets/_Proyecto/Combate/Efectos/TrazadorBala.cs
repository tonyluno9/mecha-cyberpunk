using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Combate.Efectos
{
    /// <summary>
    /// T-745: el trazo de todas las balas (jugador, pandilleros, policía a pie, aliados y dron). Termina en el punto donde la bala pega o, si no pegó en
    /// nada, se desvanece al llegar al alcance máximo; nunca se queda flotando (<see cref="ReglaTrazo"/>).
    /// </summary>
    public class TrazadorBala : MonoBehaviour
    {
        public const string NombreMaterial = "Trazo_URP";
        public const float GrosorTrazo = 0.04f;
        public const float AlcanceMaximo = 80f;
        private static readonly Color ColorBase = new Color(1f, 0.85f, 0.45f, 1f);
        private static readonly RaycastHit[] golpes = new RaycastHit[16];

        private sealed class Trazo
        {
            public LineRenderer Linea;
            public Vector3 Origen;
            public Vector3 Direccion;
            public float Distancia;
            public bool Pego;
            public float Inicio;
        }

        private static TrazadorBala instancia;
        private readonly List<Trazo> activos = new List<Trazo>();
        private readonly Stack<Trazo> libres = new Stack<Trazo>();
        private Material material;

        /// <summary>Cuántos trazos se ven ahora.</summary>
        public static int Activos => instancia != null ? instancia.activos.Count : 0;

        /// <summary>Cuántos trazos se han creado desde que se entró a Play.</summary>
        public static int Creados { get; private set; }

        /// <summary>Destino del último trazo (punto de impacto o final del alcance).</summary>
        public static Vector3 UltimoDestino { get; private set; }

        /// <summary>True si el último trazo terminó en un impacto.</summary>
        public static bool UltimoPego { get; private set; }

        /// <summary>El renderizador del último trazo (para pruebas y capturas).</summary>
        public static LineRenderer UltimaLinea { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            instancia = null;
            Creados = 0;
            UltimaLinea = null;
        }

        /// <summary>Un trazo de <paramref name="origen"/> a <paramref name="destino"/>. <paramref name="pego"/> dice si el destino es un impacto.</summary>
        public static void Trazar(Vector3 origen, Vector3 destino, bool pego)
        {
            Vector3 hacia = destino - origen;
            float distancia = hacia.magnitude;
            if (distancia < 0.05f)
            {
                return;
            }
            Asegurar().Agregar(origen, hacia / distancia, distancia, pego);
        }

        /// <summary>
        /// Un trazo que sigue la bala de <paramref name="origen"/> hacia <paramref name="direccion"/>: termina en lo primero que pegue
        /// (ignorando triggers y a <paramref name="ignorar"/>) o en <see cref="AlcanceMaximo"/>. Devuelve el punto final.
        /// </summary>
        public static Vector3 TrazarDireccion(Vector3 origen, Vector3 direccion, Transform ignorar = null, float alcance = AlcanceMaximo)
        {
            direccion.Normalize();
            Vector3 destino = origen + direccion * alcance;
            bool pego = false;
            int cantidad = Physics.RaycastNonAlloc(origen, direccion, golpes, alcance, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit golpe = golpes[i];
                if (golpe.distance >= mejor || (ignorar != null && golpe.collider.transform.root == ignorar.root))
                {
                    continue;
                }
                mejor = golpe.distance;
                destino = golpe.point;
                pego = true;
            }
            Trazar(origen, destino, pego);
            return destino;
        }

        private static TrazadorBala Asegurar()
        {
            if (instancia == null)
            {
                GameObject objeto = new GameObject("TrazadorBala");
                instancia = objeto.AddComponent<TrazadorBala>();
            }
            return instancia;
        }

        private Material MaterialTrazo()
        {
            if (material == null)
            {
                // El material URP (Trazo_URP, en Resources) lo crea CreadorEfectosT745; sin él se arma uno igual.
                material = Resources.Load<Material>(NombreMaterial);
                if (material == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    material = shader != null ? new Material(shader) : null;
                }
            }
            return material;
        }

        private void Agregar(Vector3 origen, Vector3 direccion, float distancia, bool pego)
        {
            Trazo trazo = libres.Count > 0 ? libres.Pop() : NuevoTrazo();
            trazo.Origen = origen;
            trazo.Direccion = direccion;
            trazo.Distancia = distancia;
            trazo.Pego = pego;
            trazo.Inicio = Time.time;
            trazo.Linea.enabled = true;
            Poner(trazo, 0f);
            activos.Add(trazo);
            Creados++;
            UltimoDestino = origen + direccion * distancia;
            UltimoPego = pego;
            UltimaLinea = trazo.Linea;
        }

        private Trazo NuevoTrazo()
        {
            GameObject objeto = new GameObject("Trazo");
            objeto.transform.SetParent(transform, false);
            LineRenderer linea = objeto.AddComponent<LineRenderer>();
            linea.positionCount = 2;
            linea.useWorldSpace = true;
            linea.startWidth = GrosorTrazo;
            linea.endWidth = GrosorTrazo * 0.5f;
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linea.receiveShadows = false;
            linea.sharedMaterial = MaterialTrazo();
            linea.enabled = false;
            return new Trazo { Linea = linea };
        }

        private static void Poner(Trazo trazo, float segundos)
        {
            ReglaTrazo.Tramo(trazo.Distancia, segundos, out float cola, out float cabeza);
            trazo.Linea.SetPosition(0, trazo.Origen + trazo.Direccion * cola);
            trazo.Linea.SetPosition(1, trazo.Origen + trazo.Direccion * cabeza);
            Color color = ColorBase;
            color.a = ReglaTrazo.Opacidad(trazo.Pego, trazo.Distancia, segundos);
            trazo.Linea.startColor = color;
            trazo.Linea.endColor = color;
        }

        private void Update()
        {
            for (int i = activos.Count - 1; i >= 0; i--)
            {
                Trazo trazo = activos[i];
                float segundos = Time.time - trazo.Inicio;
                if (ReglaTrazo.Termino(trazo.Distancia, segundos))
                {
                    trazo.Linea.enabled = false;
                    libres.Push(trazo);
                    activos.RemoveAt(i);
                    continue;
                }
                Poner(trazo, segundos);
            }
        }
    }
}
