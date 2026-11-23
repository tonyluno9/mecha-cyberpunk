using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.AI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1662 (T44): la ruta marcada en el mundo hacia el objetivo de la historia: una línea turquesa en el piso. La ruta sale de las calles del barrio
    /// (la misma del minimapa, <see cref="CalculoRutaGps"/>); si no hay, del NavMesh; y si tampoco, una recta corta que apunta al objetivo.
    /// Se recalcula cada <see cref="SegundosRecalculo"/> s y se oculta sin objetivo, en cinemáticas, en pausa o al llegar. Lo crea el <see cref="DirectorRecorrido"/>.
    /// </summary>
    public class GuiaHistoria : MonoBehaviour
    {
        public const float SegundosRecalculo = 0.5f;
        private const float MetrosMaximos = 90f;
        private const float MetrosRectaCorta = 25f;
        private const float MetrosLlegada = 4f;
        private const float AlturaLinea = 0.15f;
        private const float AnchoLinea = 0.35f;
        private const float MetrosMinimosEntrePuntos = 1.5f;
        private static readonly Color ColorLinea = new Color(0.2f, 1f, 0.9f, 0.85f);

        public static GuiaHistoria Actual { get; private set; }

        private LineRenderer linea;
        private float proximoCalculo;
        private bool visible;
        private string origenRuta = "ninguna";
        private float distancia;
        private readonly List<Vector3> puntos = new List<Vector3>();

        public bool Visible => visible;
        public float Distancia => distancia;
        public string OrigenRuta => origenRuta;
        public int Puntos => puntos.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Actual = null;

        private void Awake()
        {
            Actual = this;
            GameObject hijo = new GameObject("LineaGuia");
            hijo.transform.SetParent(transform, false);
            linea = hijo.AddComponent<LineRenderer>();
            linea.useWorldSpace = true;
            linea.alignment = LineAlignment.TransformZ;
            linea.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // la línea se ve plana desde arriba
            linea.widthMultiplier = AnchoLinea;
            linea.numCornerVertices = 3;
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linea.receiveShadows = false;
            Shader sombreador = Shader.Find("Universal Render Pipeline/Unlit");
            if (sombreador != null)
            {
                Material m = new Material(sombreador) { name = "GuiaHistoria" };
                m.SetColor("_BaseColor", ColorLinea);
                m.SetFloat("_Surface", 1f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
                linea.sharedMaterial = m;
            }
            linea.startColor = ColorLinea;
            linea.endColor = new Color(ColorLinea.r, ColorLinea.g, ColorLinea.b, 0.15f);
            linea.enabled = false;
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
            }
        }

        private void Update()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            bool puede = jugador != null && jugador.Raiz != null && objetivos != null && objetivos.HayObjetivo
                && !CinematicaEnCurso.Activa && !Pausa.Activa;
            Vector3 destino = Vector3.zero;
            if (puede)
            {
                puede = UbicacionObjetivo.Resolver(objetivos.Actual, Registro<ILugar>.Todos, out destino);
            }
            if (!puede)
            {
                Ocultar();
                return;
            }
            Vector3 pies = jugador.Raiz.position;
            distancia = Plano(pies, destino);
            if (distancia <= MetrosLlegada)
            {
                Ocultar();
                return;
            }
            if (Time.time >= proximoCalculo || puntos.Count < 2)
            {
                proximoCalculo = Time.time + SegundosRecalculo;
                Recalcular(pies, destino);
            }
            if (puntos.Count < 2)
            {
                Ocultar();
                return;
            }
            puntos[0] = new Vector3(pies.x, puntos[0].y, pies.z); // el inicio sigue al jugador cada cuadro
            linea.positionCount = puntos.Count;
            for (int i = 0; i < puntos.Count; i++)
            {
                linea.SetPosition(i, puntos[i]);
            }
            visible = true;
            linea.enabled = true;
        }

        private void Ocultar()
        {
            visible = false;
            origenRuta = "ninguna";
            if (linea != null && linea.enabled)
            {
                linea.enabled = false;
            }
            puntos.Clear();
        }

        private void Recalcular(Vector3 pies, Vector3 destino)
        {
            puntos.Clear();
            List<Vector3> ruta = CalculoRutaGps.Ruta(pies, destino);
            if (ruta != null && ruta.Count > 2)
            {
                origenRuta = "calles";
                Agregar(ruta, true);
                return;
            }
            NavMeshPath camino = new NavMeshPath();
            if (NavMesh.SamplePosition(pies, out NavMeshHit a, 5f, NavMesh.AllAreas) && NavMesh.SamplePosition(destino, out NavMeshHit b, 15f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, camino) && camino.corners.Length >= 2)
            {
                origenRuta = "navmesh";
                Agregar(new List<Vector3>(camino.corners), false);
                return;
            }
            origenRuta = "recta";
            Vector3 haciaDestino = destino - pies;
            haciaDestino.y = 0f;
            Vector3 fin = pies + haciaDestino.normalized * Mathf.Min(MetrosRectaCorta, haciaDestino.magnitude);
            Agregar(new List<Vector3> { pies, fin }, true);
        }

        // Pasa la ruta a puntos sobre el piso; recorta a MetrosMaximos desde el jugador.
        private void Agregar(List<Vector3> ruta, bool pegarAlPiso)
        {
            float acumulado = 0f;
            Vector3 anterior = default;
            for (int i = 0; i < ruta.Count; i++)
            {
                Vector3 p = ruta[i];
                if (pegarAlPiso)
                {
                    p = Suelo(p);
                }
                p.y += AlturaLinea;
                if (puntos.Count > 0)
                {
                    float d = Vector3.Distance(anterior, p);
                    if (d < MetrosMinimosEntrePuntos && i < ruta.Count - 1)
                    {
                        continue;
                    }
                    if (acumulado + d > MetrosMaximos)
                    {
                        puntos.Add(Vector3.Lerp(anterior, p, (MetrosMaximos - acumulado) / Mathf.Max(0.01f, d)));
                        return;
                    }
                    acumulado += d;
                }
                puntos.Add(p);
                anterior = p;
            }
        }

        private static Vector3 Suelo(Vector3 punto)
        {
            RaycastHit[] golpes = Physics.RaycastAll(punto + Vector3.up * 6f, Vector3.down, 14f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            bool hay = false;
            Vector3 donde = punto;
            Transform jugador = Registro<IJugador>.Actual != null ? Registro<IJugador>.Actual.Raiz : null;
            foreach (RaycastHit g in golpes)
            {
                if ((jugador != null && g.collider.transform.IsChildOf(jugador)) || g.distance >= mejor)
                {
                    continue;
                }
                mejor = g.distance;
                donde = g.point;
                hay = true;
            }
            return hay ? donde : punto;
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>Depuración: estado de la guía (visible, de dónde sale la ruta, distancia).</summary>
        public string Estado() => "visible=" + visible + " ruta=" + origenRuta + " puntos=" + puntos.Count + " distancia=" + distancia.ToString("F1") + " m";
    }
}
