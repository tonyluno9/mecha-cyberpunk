using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR.Ruta
{
    /// <summary>
    /// VR30: ruta minimalista al objetivo actual en VR. Chevrones holográficos pequeños (cian, aditivos, sin sombra) en el piso cada 2 m siguiendo
    /// <see cref="NavMesh.CalculatePath"/> hacia el objetivo (<see cref="ServicioObjetivos.Posicion"/>, el mismo punto de la flecha del HUD), solo los
    /// siguientes 25 m; avanzan suave y se desvanecen con la distancia (<see cref="ReglaRutaVR"/>). Se apaga al llegar (&lt;3 m), sin objetivo, fuera de VR,
    /// con <see cref="OpcionesVR.Ruta"/> en false o si el jugador no va a pie. No usa ni reactiva la línea vieja. Lo agrega <see cref="SistemaVR"/>.
    /// </summary>
    public class RutaObjetivoVR : MonoBehaviour
    {
        private const float SegundosEntreRutas = 0.4f;
        private const float MetrosBuscarNavMesh = 6f;
        private const float AlturaSobrePiso = 0.04f;
        private const float AnchoChevron = 0.42f;
        private const float LargoChevron = 0.3f;
        private const float GrosorChevron = 0.09f;
        private const float BrilloMaximo = 0.85f;
        private const int MaximoEsquinas = 64;
        private static readonly Color ColorCian = new Color(0.10f, 0.92f, 0.98f, 1f);
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColor = Shader.PropertyToID("_Color");

        private readonly List<PuntoRutaVR> puntos = new List<PuntoRutaVR>();
        private readonly List<Vector3> esquinas = new List<Vector3>(MaximoEsquinas);
        private readonly Vector3[] esquinasRuta = new Vector3[MaximoEsquinas];
        private NavMeshPath ruta;
        private float proximaRuta;
        private Vector3 metaRuta;
        private Transform raiz;
        private Mesh malla;
        private Material material;
        private MaterialPropertyBlock bloque;
        private readonly List<MeshRenderer> chevrones = new List<MeshRenderer>();
        private int visibles;

        /// <summary>Cuántos chevrones se ven ahora (pruebas).</summary>
        public int Visibles => visibles;

        private void Awake()
        {
            ruta = new NavMeshPath();
            bloque = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            if (malla != null)
            {
                Destroy(malla);
            }
            if (material != null)
            {
                Destroy(material);
            }
        }

        private void LateUpdate()
        {
            if (!Activa(out Vector3 jugador, out Vector3 meta))
            {
                Ocultar();
                return;
            }
            if (Time.time >= proximaRuta || (meta - metaRuta).sqrMagnitude > 1f)
            {
                proximaRuta = Time.time + SegundosEntreRutas;
                metaRuta = meta;
                Calcular(jugador, meta);
            }
            if (esquinas.Count < 2)
            {
                Ocultar();
                return;
            }
            // La ruta empieza en los pies de ahora (no en donde estaba al calcularla).
            esquinas[0] = jugador;
            ReglaRutaVR.Muestrear(esquinas, esquinas.Count, ReglaRutaVR.Desfase(Time.time), puntos);
            Dibujar();
        }

        private bool Activa(out Vector3 jugador, out Vector3 meta)
        {
            jugador = default;
            meta = default;
            if (!ModoVR.Activo || !OpcionesVR.Ruta || CinematicaEnCurso.Activa)
            {
                return false;
            }
            IJugador j = Registro<IJugador>.Actual;
            if (j == null || j.Raiz == null || j.Modo != ModoJugador.APie)
            {
                return false;
            }
            raiz = j.Raiz;
            ServicioObjetivos servicio = Registro<IObjetivos>.Actual as ServicioObjetivos;
            if (servicio == null || !servicio.HayObjetivo || !servicio.PosicionResuelta)
            {
                return false;
            }
            jugador = raiz.position;
            meta = servicio.Posicion;
            return !ReglaRutaVR.Llego(jugador, meta);
        }

        private void Calcular(Vector3 jugador, Vector3 meta)
        {
            esquinas.Clear();
            if (NavMesh.SamplePosition(jugador, out NavMeshHit desde, MetrosBuscarNavMesh, NavMesh.AllAreas)
                && NavMesh.SamplePosition(meta, out NavMeshHit hasta, MetrosBuscarNavMesh * 2f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(desde.position, hasta.position, NavMesh.AllAreas, ruta)
                && ruta.status != NavMeshPathStatus.PathInvalid)
            {
                int n = ruta.GetCornersNonAlloc(esquinasRuta);
                for (int i = 0; i < n; i++)
                {
                    esquinas.Add(esquinasRuta[i]);
                }
            }
            // Sin NavMesh no hay ruta (no se inventa una recta que atraviese edificios).
        }

        private void Dibujar()
        {
            Preparar();
            visibles = 0;
            for (int i = 0; i < puntos.Count; i++)
            {
                float alfa = ReglaRutaVR.Alfa(puntos[i].Metros) * BrilloMaximo;
                if (alfa <= 0.01f)
                {
                    continue;
                }
                MeshRenderer r = Chevron(visibles++);
                Transform t = r.transform;
                t.SetPositionAndRotation(puntos[i].Posicion + Vector3.up * AlturaSobrePiso, Quaternion.LookRotation(puntos[i].Direccion, Vector3.up));
                Color c = ColorCian * alfa; // aditivo: el alfa se lleva en el color
                c.a = alfa;
                bloque.SetColor(IdColorBase, c);
                bloque.SetColor(IdColor, c);
                r.SetPropertyBlock(bloque);
                if (!r.enabled)
                {
                    r.enabled = true;
                }
            }
            for (int i = visibles; i < chevrones.Count; i++)
            {
                chevrones[i].enabled = false;
            }
        }

        private void Ocultar()
        {
            visibles = 0;
            foreach (MeshRenderer r in chevrones)
            {
                if (r != null && r.enabled)
                {
                    r.enabled = false;
                }
            }
        }

        private MeshRenderer Chevron(int indice)
        {
            while (chevrones.Count <= indice)
            {
                var objeto = new GameObject("RutaVR_Chevron_" + chevrones.Count);
                objeto.transform.SetParent(transform, false);
                objeto.AddComponent<MeshFilter>().sharedMesh = malla;
                MeshRenderer r = objeto.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = LightProbeUsage.Off;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                r.enabled = false;
                chevrones.Add(r);
            }
            return chevrones[indice];
        }

        private void Preparar()
        {
            if (malla == null)
            {
                malla = CrearChevron(AnchoChevron, LargoChevron, GrosorChevron);
            }
            if (material == null)
            {
                material = CrearMaterial();
            }
        }

        /// <summary>Malla plana de un chevrón (una "V" que apunta a +Z), en el plano XZ, de cara hacia arriba.</summary>
        public static Mesh CrearChevron(float ancho, float largo, float grosor)
        {
            float m = ancho * 0.5f;
            float g = grosor;
            // Brazo izquierdo y derecho: cada uno un cuadrilátero de la orilla trasera a la punta.
            var v = new[]
            {
                new Vector3(-m, 0f, -largo * 0.5f), new Vector3(-m + g, 0f, -largo * 0.5f), new Vector3(0f, 0f, largo * 0.5f - g), new Vector3(0f, 0f, largo * 0.5f),
                new Vector3(m - g, 0f, -largo * 0.5f), new Vector3(m, 0f, -largo * 0.5f),
            };
            // Izquierdo: 0-1-2-3. Derecho: 4-5-3-2 (el material no recorta caras: se ve desde arriba igual).
            var tri = new[] { 0, 3, 1, 1, 3, 2, 4, 2, 5, 5, 2, 3 };
            var malla = new Mesh { name = "ChevronRutaVR" };
            malla.vertices = v;
            malla.triangles = tri;
            malla.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            malla.RecalculateBounds();
            return malla;
        }

        private static Material CrearMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            var m = new Material(shader) { name = "RutaVR_Chevron", hideFlags = HideFlags.DontSave };
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f); // aditivo
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor(IdColorBase, ColorCian);
            return m;
        }
    }
}
