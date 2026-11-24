using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR.Ruta
{
    /// <summary>
    /// VR41: la ruta al objetivo en VR vuelve a ser la línea turquesa de antes en el piso (la de GuiaHistoria, T-1662, que T-51 quitó en PC; en PC
    /// sigue sin línea). La ruta sale de las calles del barrio (<see cref="CalculoRutaGps"/>, la del mapa); si no hay, del NavMesh; y si tampoco, una
    /// recta corta que apunta al objetivo (<see cref="ServicioObjetivos.Posicion"/>, el punto de la flecha del HUD). Se recalcula cada medio segundo,
    /// el inicio sigue los pies cada cuadro y se oculta al llegar, sin objetivo, en cinemáticas o en pausa, fuera de VR, con
    /// <see cref="OpcionesVR.Ruta"/> en false (RUTA: NO) o si el jugador no va a pie. Los chevrones de VR30 se quitaron. Lo agrega <see cref="SistemaVR"/>.
    /// </summary>
    public class RutaObjetivoVR : MonoBehaviour
    {
        public const float SegundosRecalculo = 0.5f;
        private const float AlturaLinea = 0.15f;
        private const float AnchoLinea = 0.35f;
        private const float MetrosBuscarNavMesh = 5f;
        private const float MetrosBuscarNavMeshDestino = 15f;
        private const float AlturaRayoSuelo = 6f;
        private const float LargoRayoSuelo = 14f;
        private static readonly Color ColorLinea = new Color(0.2f, 1f, 0.9f, 0.85f);
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        private LineRenderer linea;
        private Material material;
        private NavMeshPath camino;
        private float proximoCalculo;
        private Vector3 metaCalculada;
        private string origenRuta = "ninguna";
        private readonly List<Vector3> crudos = new List<Vector3>();
        private readonly List<Vector3> puntos = new List<Vector3>();

        /// <summary>True mientras la línea se ve.</summary>
        public bool Visible => linea != null && linea.enabled;

        /// <summary>Cuántos puntos tiene la línea ahora.</summary>
        public int Puntos => Visible ? puntos.Count : 0;

        /// <summary>De dónde salió la ruta: calles, navmesh, recta o ninguna.</summary>
        public string OrigenRuta => origenRuta;

        /// <summary>La línea (pruebas).</summary>
        public LineRenderer Linea => linea;

        private void Awake()
        {
            camino = new NavMeshPath();
            GameObject hijo = new GameObject("LineaRutaVR");
            hijo.transform.SetParent(transform, false);
            linea = hijo.AddComponent<LineRenderer>();
            linea.useWorldSpace = true;
            linea.alignment = LineAlignment.TransformZ;
            linea.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // plana sobre el piso, como la de antes
            linea.widthMultiplier = AnchoLinea;
            linea.numCornerVertices = 3;
            linea.shadowCastingMode = ShadowCastingMode.Off;
            linea.receiveShadows = false;
            linea.lightProbeUsage = LightProbeUsage.Off;
            linea.reflectionProbeUsage = ReflectionProbeUsage.Off;
            material = CrearMaterial();
            if (material != null)
            {
                linea.sharedMaterial = material;
            }
            linea.startColor = ColorLinea;
            linea.endColor = new Color(ColorLinea.r, ColorLinea.g, ColorLinea.b, 0.15f);
            linea.enabled = false;
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        private void LateUpdate()
        {
            if (!Activa(out Vector3 pies, out Vector3 meta))
            {
                Ocultar();
                return;
            }
            if (Time.time >= proximoCalculo || puntos.Count < 2 || (meta - metaCalculada).sqrMagnitude > 1f)
            {
                proximoCalculo = Time.time + SegundosRecalculo;
                metaCalculada = meta;
                Recalcular(pies, meta);
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
            if (!linea.enabled)
            {
                linea.enabled = true;
            }
        }

        private bool Activa(out Vector3 pies, out Vector3 meta)
        {
            pies = default;
            meta = default;
            if (!ModoVR.Activo || !OpcionesVR.Ruta || CinematicaEnCurso.Activa || Pausa.Activa)
            {
                return false;
            }
            IJugador j = Registro<IJugador>.Actual;
            if (j == null || j.Raiz == null || j.Modo != ModoJugador.APie)
            {
                return false;
            }
            ServicioObjetivos servicio = Registro<IObjetivos>.Actual as ServicioObjetivos;
            if (servicio == null || !servicio.HayObjetivo || !servicio.PosicionResuelta)
            {
                return false;
            }
            pies = j.Raiz.position;
            meta = servicio.Posicion;
            return !ReglaRutaVR.Llego(pies, meta);
        }

        private void Ocultar()
        {
            origenRuta = "ninguna";
            puntos.Clear();
            if (linea != null && linea.enabled)
            {
                linea.enabled = false;
            }
        }

        private void Recalcular(Vector3 pies, Vector3 meta)
        {
            crudos.Clear();
            List<Vector3> calles = CalculoRutaGps.Ruta(pies, meta);
            if (calles != null && calles.Count > 2)
            {
                origenRuta = "calles";
                foreach (Vector3 p in calles)
                {
                    crudos.Add(Suelo(p) + Vector3.up * AlturaLinea);
                }
            }
            else if (NavMesh.SamplePosition(pies, out NavMeshHit a, MetrosBuscarNavMesh, NavMesh.AllAreas)
                && NavMesh.SamplePosition(meta, out NavMeshHit b, MetrosBuscarNavMeshDestino, NavMesh.AllAreas)
                && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, camino) && camino.corners.Length >= 2)
            {
                origenRuta = "navmesh";
                foreach (Vector3 p in camino.corners)
                {
                    crudos.Add(p + Vector3.up * AlturaLinea);
                }
            }
            else
            {
                origenRuta = "recta";
                crudos.Add(Suelo(pies) + Vector3.up * AlturaLinea);
                crudos.Add(Suelo(ReglaRutaVR.FinRectaCorta(pies, meta)) + Vector3.up * AlturaLinea);
            }
            ReglaRutaVR.Recortar(crudos, puntos);
        }

        // El piso bajo un punto (sin contar al jugador); si no hay, el mismo punto.
        private static Vector3 Suelo(Vector3 punto)
        {
            RaycastHit[] golpes = Physics.RaycastAll(punto + Vector3.up * AlturaRayoSuelo, Vector3.down, LargoRayoSuelo, ~0, QueryTriggerInteraction.Ignore);
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

        private static Material CrearMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                return null;
            }
            var m = new Material(shader) { name = "RutaVR_Linea", hideFlags = HideFlags.DontSave };
            m.SetColor(IdColorBase, ColorLinea);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }
    }
}
