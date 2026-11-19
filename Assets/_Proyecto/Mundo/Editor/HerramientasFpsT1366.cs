using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Mundo.Pruebas;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1366: diagnóstico sin renderizar de las vistas de <see cref="VistasFpsT1366"/> con la escena abierta en el editor.
    /// Menú Mecha Cyberpunk/Mundo/FPS T-1366: Diagnosticar (escribe Logs/fps-diagnostico.txt) e Iniciar medición en Play.
    /// </summary>
    public static class HerramientasFpsT1366
    {
        public const string RutaDiagnostico = "Logs/fps-diagnostico.txt";
        private const float CampoVision = 70f;
        private const float Aspecto = 16f / 9f;
        private const float PlanoCerca = 0.3f;
        private const float PlanoLejos = 600f;
        private const int ObjetosPorVista = 15;

        /// <summary>Los puntos de vista (los de la clase compartida, para ajustarlos en un solo lugar).</summary>
        public static IReadOnlyList<VistaFpsT1366> PuntosDeVista() => VistasFpsT1366.Todas;

        [MenuItem("Mecha Cyberpunk/Mundo/FPS T-1366/Iniciar medición en Play")]
        public static void IniciarMedicionEnPlay() => MedidorFpsZonas.IniciarEnEditor();

        [MenuItem("Mecha Cyberpunk/Mundo/FPS T-1366/Diagnosticar")]
        public static void Diagnosticar()
        {
            Physics.SyncTransforms();
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Light[] luces = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Dictionary<Mesh, int> cache = new Dictionary<Mesh, int>();
            List<Diagnostico> diagnosticos = new List<Diagnostico>();

            foreach (VistaFpsT1366 vista in VistasFpsT1366.Todas)
            {
                VistasFpsT1366.Resolver(vista, VistasFpsT1366.AlturaConFisica, out Vector3 posicion, out Vector3 mirar);
                Quaternion giro = Quaternion.LookRotation(mirar - posicion, Vector3.up);
                Matrix4x4 vistaMatriz = Matrix4x4.TRS(posicion, giro, new Vector3(1f, 1f, -1f)).inverse;
                Matrix4x4 proyeccion = Matrix4x4.Perspective(CampoVision, Aspecto, PlanoCerca, PlanoLejos);
                Plane[] planos = GeometryUtility.CalculateFrustumPlanes(proyeccion * vistaMatriz);

                Diagnostico d = new Diagnostico { Etiqueta = vista.Etiqueta };
                HashSet<Material> materiales = new HashSet<Material>();
                List<KeyValuePair<string, long>> objetos = new List<KeyValuePair<string, long>>();
                foreach (Renderer r in renderers)
                {
                    if (!r.enabled || !GeometryUtility.TestPlanesAABB(planos, r.bounds)) continue;
                    d.Renderers++;
                    long tris = Triangulos(r, cache);
                    d.Triangulos += tris;
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m != null) materiales.Add(m);
                    }
                    objetos.Add(new KeyValuePair<string, long>(Ruta(r.transform), tris));
                }
                foreach (Light l in luces)
                {
                    if (!l.enabled || l.type == LightType.Directional) continue;
                    Bounds caja = new Bounds(l.transform.position, Vector3.one * (l.range * 2f));
                    if (GeometryUtility.TestPlanesAABB(planos, caja)) d.Luces++;
                }
                d.Materiales = materiales.Count;
                objetos.Sort((a, b) => b.Value.CompareTo(a.Value));
                d.Top = objetos.GetRange(0, Mathf.Min(ObjetosPorVista, objetos.Count));
                diagnosticos.Add(d);
            }

            diagnosticos.Sort((a, b) => b.Triangulos.CompareTo(a.Triangulos));
            StringBuilder sb = new StringBuilder("Diagnóstico de vistas (de peor a mejor por triángulos en el frustum; sin oclusión)\n");
            foreach (Diagnostico d in diagnosticos)
            {
                sb.Append('\n').Append(d.Etiqueta).Append(": ").Append(d.Renderers).Append(" renderers, ")
                  .Append(d.Triangulos).Append(" tris, ").Append(d.Materiales).Append(" materiales, ")
                  .Append(d.Luces).Append(" luces\n");
                foreach (KeyValuePair<string, long> o in d.Top)
                {
                    sb.Append("    ").Append(o.Value).Append(" tris  ").Append(o.Key).Append('\n');
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RutaDiagnostico));
            File.WriteAllText(RutaDiagnostico, sb.ToString());
            Debug.Log("[Mundo] Diagnóstico FPS escrito en " + RutaDiagnostico);
        }

        private static long Triangulos(Renderer renderer, Dictionary<Mesh, int> cache)
        {
            Mesh malla = null;
            if (renderer is SkinnedMeshRenderer piel)
            {
                malla = piel.sharedMesh;
            }
            else if (renderer.TryGetComponent(out MeshFilter filtro))
            {
                malla = filtro.sharedMesh;
            }
            if (malla == null) return 0;
            if (!cache.TryGetValue(malla, out int cuenta))
            {
                for (int i = 0; i < malla.subMeshCount; i++) cuenta += (int)(malla.GetIndexCount(i) / 3);
                cache[malla] = cuenta;
            }
            return cuenta;
        }

        private static string Ruta(Transform t)
        {
            string ruta = t.name;
            int tope = 4;
            while (t.parent != null && tope-- > 0)
            {
                t = t.parent;
                ruta = t.name + "/" + ruta;
            }
            return ruta;
        }

        private class Diagnostico
        {
            public string Etiqueta;
            public int Renderers;
            public long Triangulos;
            public int Materiales;
            public int Luces;
            public List<KeyValuePair<string, long>> Top;
        }
    }
}
