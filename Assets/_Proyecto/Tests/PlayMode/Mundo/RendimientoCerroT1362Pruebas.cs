using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1362: mide el rendimiento de la Colonia del Cerro y del Puerto (cada zona sola, en una escena vacía) desde varios puntos de vista.
    /// Escribe Logs/t1362-rendimiento-sufijo.txt y .json y una captura por punto. Solo informa; falla si un punto da menos de 20 FPS.
    /// El sufijo sale de -sufijo en la línea de comandos o de la variable T1362_SUFIJO (por defecto "antes").
    /// </summary>
    public class RendimientoCerroT1362Pruebas
    {
        private const string RutaCerro = "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab";
        private const string RutaPuerto = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        private const string SufijoPorDefecto = "antes";
        private const string CarpetaCapturas = "docs/capturas/bugs-18nov-B/";
        private const int FramesCalentamiento = 60;
        private const int FramesMedidos = 180;
        private const float FpsMinimo = 20f;
        private const float FovGrados = 60f;
        private const float PlanoCercano = 0.05f;
        private const float PlanoLejano = 1000f;
        private const float AlturaOjos = 1.7f;
        private const int CantidadCulpables = 30;
        private const float MsPorSegundo = 1000f;
        private const int AnchoMedicion = 1920;
        private const int AltoMedicion = 1080;
        private const int BitsProfundidad = 24;

        [Serializable]
        private class Punto
        {
            public string zona;
            public int n;
            public string descripcion;
            public Vector3 posicion;
            public Vector3 mira;
            public float msPromedio;
            public float msP95;
            public float msDeltaTime;
            public float fps;
            public float gpuMsPromedio;
            public long batches;
            public long setPass;
            public long triangulos;
            public long vertices;
            public long sombras;
        }

        [Serializable]
        private class Culpable
        {
            public string malla;
            public string raiz;
            public long triangulos;
        }

        [Serializable]
        private class Informe
        {
            public string sufijo;
            public string fecha;
            public List<Punto> puntos = new List<Punto>();
            public string puntoPeor;
            public List<Culpable> culpables = new List<Culpable>();
            public List<string> renderersPorTipo = new List<string>();
        }

        private struct Vista
        {
            public string descripcion;
            public Vector3 posicion;
            public Vector3 mira;
        }

        [UnityTest]
        [Timeout(3600000)]
        public IEnumerator MideCerroYPuerto()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            string sufijo = LeerSufijo();
            Informe informe = new Informe { sufijo = sufijo, fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm") };

            GameObject luz = new GameObject("Luz_T1362");
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 0.35f;
            l.color = new Color(0.55f, 0.6f, 0.9f);
            l.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.14f);

            GameObject camObj = new GameObject("Camara_T1362");
            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = FovGrados;
            cam.nearClipPlane = PlanoCercano;
            cam.farClipPlane = PlanoLejano;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
            camObj.AddComponent<UniversalAdditionalCameraData>();

            string[] rutas = { RutaCerro, RutaPuerto };
            string[] zonas = { "cerro", "puerto" };
            Punto peor = null;

            for (int z = 0; z < rutas.Length; z++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutas[z]);
                Assert.IsNotNull(prefab, "Falta el prefab " + rutas[z]);
                GameObject zona = UnityEngine.Object.Instantiate(prefab);
                zona.transform.position = Vector3.zero;
                Physics.SyncTransforms();
                yield return null;

                List<Vista> vistas = z == 0 ? VistasCerro(zona.transform) : VistasPuerto(zona.transform);
                for (int i = 0; i < vistas.Count; i++)
                {
                    Vista v = vistas[i];
                    camObj.transform.position = v.posicion;
                    camObj.transform.rotation = Quaternion.LookRotation((v.mira - v.posicion).normalized, Vector3.up);
                    Punto p = new Punto { zona = zonas[z], n = i + 1, descripcion = v.descripcion, posicion = v.posicion, mira = v.mira };
                    yield return Medir(p, cam);
                    informe.puntos.Add(p);
                    if (peor == null || p.msPromedio > peor.msPromedio)
                    {
                        // Los culpables se calculan con la cámara todavía en el punto, justo tras medirlo.
                        peor = p;
                        informe.puntoPeor = p.zona + "-" + p.n + " (" + p.descripcion + ")";
                        informe.culpables = ListarCulpables(cam, zona.transform, informe.renderersPorTipo);
                    }
                    CapturaPruebas.Guardar(cam, CarpetaCapturas + "fps-" + p.zona + "-" + p.n + "-" + sufijo + ".png");
                }
                UnityEngine.Object.Destroy(zona);
                yield return null;
            }

            Escribir(informe, sufijo);
            float minimo = float.MaxValue;
            string nombre = "";
            foreach (Punto p in informe.puntos)
            {
                if (p.fps < minimo)
                {
                    minimo = p.fps;
                    nombre = p.zona + "-" + p.n;
                }
            }
            UnityEngine.Object.Destroy(camObj);
            UnityEngine.Object.Destroy(luz);
            Assert.GreaterOrEqual(minimo, FpsMinimo, "Punto con menos de " + FpsMinimo + " FPS: " + nombre + " (" + minimo.ToString("0.0") + "). Ver Logs/t1362-rendimiento-" + sufijo + ".txt");
        }

        private static string LeerSufijo()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-sufijo")
                {
                    return args[i + 1];
                }
            }
            string env = Environment.GetEnvironmentVariable("T1362_SUFIJO");
            return string.IsNullOrEmpty(env) ? SufijoPorDefecto : env;
        }

        private static ProfilerRecorder Grabar(string nombre)
        {
            return ProfilerRecorder.StartNew(ProfilerCategory.Render, nombre);
        }

        private static long Promedio(List<long> muestras)
        {
            if (muestras.Count == 0)
            {
                return -1;
            }
            long suma = 0;
            foreach (long m in muestras)
            {
                suma += m;
            }
            return suma / muestras.Count;
        }

        private static IEnumerator Medir(Punto p, Camera cam)
        {
            // En batchmode la cámara no dibuja sola (no hay Game view): se dibuja a mano en una RenderTexture y se sincroniza con la GPU.
            RenderTexture rt = new RenderTexture(AnchoMedicion, AltoMedicion, BitsProfundidad, RenderTextureFormat.ARGB32);
            Texture2D sync = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            cam.targetTexture = rt;
            cam.enabled = false;
            for (int i = 0; i < FramesCalentamiento; i++)
            {
                yield return null;
                DibujarYSincronizar(cam, rt, sync);
            }
            ProfilerRecorder batches = Grabar("Batches Count");
            ProfilerRecorder setPass = Grabar("SetPass Calls Count");
            ProfilerRecorder tris = Grabar("Triangles Count");
            ProfilerRecorder verts = Grabar("Vertices Count");
            ProfilerRecorder sombras = Grabar("Shadow Casters Count");
            List<long> mb = new List<long>(), ms = new List<long>(), mt = new List<long>(), mv = new List<long>(), mh = new List<long>();
            List<float> tiempos = new List<float>(FramesMedidos);
            float sumaDelta = 0f;
            System.Diagnostics.Stopwatch reloj = new System.Diagnostics.Stopwatch();
            for (int i = 0; i < FramesMedidos; i++)
            {
                yield return null;
                sumaDelta += Time.unscaledDeltaTime * MsPorSegundo;
                reloj.Restart();
                DibujarYSincronizar(cam, rt, sync);
                reloj.Stop();
                tiempos.Add((float)reloj.Elapsed.TotalMilliseconds);
                if (batches.Valid) { mb.Add(batches.LastValue); }
                if (setPass.Valid) { ms.Add(setPass.LastValue); }
                if (tris.Valid) { mt.Add(tris.LastValue); }
                if (verts.Valid) { mv.Add(verts.LastValue); }
                if (sombras.Valid) { mh.Add(sombras.LastValue); }
            }
            p.batches = Promedio(mb);
            p.setPass = Promedio(ms);
            p.triangulos = Promedio(mt);
            p.vertices = Promedio(mv);
            p.sombras = Promedio(mh);
            batches.Dispose(); setPass.Dispose(); tris.Dispose(); verts.Dispose(); sombras.Dispose();
            p.gpuMsPromedio = -1f;
            p.msDeltaTime = sumaDelta / FramesMedidos;
            float suma = 0f;
            foreach (float t in tiempos)
            {
                suma += t;
            }
            p.msPromedio = suma / tiempos.Count;
            tiempos.Sort();
            p.msP95 = tiempos[Mathf.Min(tiempos.Count - 1, (int)(tiempos.Count * 0.95f))];
            p.fps = MsPorSegundo / Mathf.Max(0.0001f, p.msPromedio);
            cam.targetTexture = null;
            rt.Release();
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(sync);
        }

        private static void DibujarYSincronizar(Camera cam, RenderTexture rt, Texture2D sync)
        {
            cam.Render();
            RenderTexture previa = RenderTexture.active;
            RenderTexture.active = rt;
            sync.ReadPixels(new Rect(0f, 0f, 1f, 1f), 0, 0);
            sync.Apply(false);
            RenderTexture.active = previa;
        }

        private static List<Vector3> Lineas(Transform raiz, string calle)
        {
            List<Vector3> r = new List<Vector3>();
            Transform c = BuscarDescendiente(raiz, calle);
            if (c == null)
            {
                return r;
            }
            foreach (Transform t in c)
            {
                if (t.name == "Linea")
                {
                    r.Add(t.position);
                }
            }
            return r;
        }

        private static Transform BuscarDescendiente(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            return null;
        }

        private static List<Vista> VistasCerro(Transform raiz)
        {
            Transform ladera = BuscarDescendiente(raiz, "Ladera");
            Vector3 centroLadera = ladera != null ? ladera.GetComponent<Renderer>().bounds.center : raiz.position;
            List<Vector3> zig = Lineas(raiz, "Calle_Zigzag");
            List<Vector3> baseCalle = Lineas(raiz, "Calle_Base");
            List<Vector3> u4 = Lineas(raiz, "Calle_U4");
            Assert.Greater(zig.Count, 3, "Calle_Zigzag sin líneas");
            Assert.Greater(baseCalle.Count, 3, "Calle_Base sin líneas");
            Vector3 ojos = Vector3.up * AlturaOjos;
            List<Vista> v = new List<Vista>();
            Vector3 b0 = baseCalle[baseCalle.Count / 2];
            v.Add(new Vista { descripcion = "a pie en la calle base, mirando a la ladera", posicion = b0 + ojos, mira = centroLadera });
            Vector3 b1 = baseCalle[baseCalle.Count / 8];
            v.Add(new Vista { descripcion = "calle base al sur, mirando a la ladera", posicion = b1 + ojos, mira = centroLadera });
            Vector3 u = u4.Count > 0 ? u4[u4.Count / 2] : b0;
            v.Add(new Vista { descripcion = "desde la calle U4 mirando a la ladera entera", posicion = u + Vector3.up * 3f, mira = centroLadera + Vector3.up * 10f });
            Vector3 z1 = zig[zig.Count / 4];
            v.Add(new Vista { descripcion = "zigzag, primer cuarto, mirando a la ladera", posicion = z1 + ojos, mira = centroLadera });
            Vector3 z2 = zig[zig.Count / 2];
            v.Add(new Vista { descripcion = "a media pierna del zigzag, mirando a la ladera", posicion = z2 + ojos, mira = centroLadera });
            Vector3 alta = zig[0];
            foreach (Vector3 p in zig)
            {
                if (p.y > alta.y)
                {
                    alta = p;
                }
            }
            v.Add(new Vista { descripcion = "plaza de arriba, mirando al resto de la ladera", posicion = alta + ojos, mira = centroLadera });
            Vector3 azotea = zig[0];
            float mejorY = float.MinValue;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "AzoteaT1318" && r.bounds.center.y > mejorY)
                {
                    mejorY = r.bounds.center.y;
                    azotea = r.bounds.center;
                }
            }
            v.Add(new Vista { descripcion = "azotea alta mirando al resto", posicion = azotea + ojos, mira = b0 });
            v.Add(new Vista { descripcion = "calle base elevada 12 m, panorámica de toda la ladera", posicion = b0 + new Vector3(30f, 12f, 0f), mira = centroLadera });
            return v;
        }

        private static List<Vista> VistasPuerto(Transform raiz)
        {
            Vector3 ojos = Vector3.up * AlturaOjos;
            Transform patio = BuscarDescendiente(raiz, "Patio");
            Bounds b = patio != null ? patio.GetComponent<Renderer>().bounds : new Bounds(raiz.position, Vector3.one);
            float piso = b.max.y;
            Vector3 centro = new Vector3(b.center.x, piso, b.center.z);
            List<Vista> v = new List<Vista>();
            v.Add(new Vista { descripcion = "a pie en el centro del patio, mirando al este", posicion = centro + ojos, mira = centro + ojos + Vector3.right * 50f });
            v.Add(new Vista { descripcion = "esquina oeste del patio, mirando al centro", posicion = new Vector3(b.min.x + 5f, piso, b.center.z) + ojos, mira = centro + Vector3.up * 3f });
            v.Add(new Vista { descripcion = "muelle sur mirando al patio", posicion = new Vector3(b.center.x, piso, b.min.z + 3f) + ojos, mira = centro + Vector3.up * 3f });
            v.Add(new Vista { descripcion = "este del patio, 8 m de altura, panorámica", posicion = new Vector3(b.max.x - 3f, piso + 8f, b.center.z), mira = new Vector3(b.min.x, piso, b.center.z) });
            return v;
        }

        private static string NombreSinNumeros(string n)
        {
            n = n.Replace("(Clone)", "");
            StringBuilder sb = new StringBuilder();
            foreach (char ch in n)
            {
                if (!char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString().Trim('_', ' ');
        }

        /// <summary>El objeto "prop": hijo de la carpeta (Casas, Escaleras...) que hay bajo la raíz de la zona.</summary>
        private static Transform RaizDeProp(Transform raiz, Transform t)
        {
            Transform actual = t;
            while (actual.parent != null && actual.parent != raiz && actual.parent.parent != raiz)
            {
                actual = actual.parent;
            }
            return actual;
        }

        /// <summary>Estima si el renderer se dibuja: en un LODGroup, solo si el LOD que toca por tamaño en pantalla lo incluye.</summary>
        private static bool VisiblePorLod(Camera cam, Renderer r)
        {
            LODGroup grupo = r.GetComponentInParent<LODGroup>();
            if (grupo == null)
            {
                return true;
            }
            float distancia = Mathf.Max(PlanoCercano, Vector3.Distance(cam.transform.position, grupo.transform.TransformPoint(grupo.localReferencePoint)));
            Vector3 e = grupo.transform.lossyScale;
            float tamano = grupo.size * Mathf.Max(e.x, Mathf.Max(e.y, e.z));
            float relativa = tamano / (2f * distancia * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            LOD[] lods = grupo.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                if (relativa >= lods[i].screenRelativeTransitionHeight)
                {
                    foreach (Renderer x in lods[i].renderers)
                    {
                        if (x == r)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            return false;
        }

        private static long Triangulos(Mesh m)
        {
            long suma = 0;
            for (int i = 0; i < m.subMeshCount; i++)
            {
                suma += m.GetIndexCount(i) / 3;
            }
            return suma;
        }

        private static List<Culpable> ListarCulpables(Camera cam, Transform raiz, List<string> porTipo)
        {
            Plane[] planos = GeometryUtility.CalculateFrustumPlanes(cam);
            List<Culpable> todos = new List<Culpable>();
            Dictionary<string, int> cuentas = new Dictionary<string, int>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>())
            {
                string tipo = NombreSinNumeros(RaizDeProp(raiz, r.transform).name);
                cuentas[tipo] = cuentas.TryGetValue(tipo, out int n) ? n + 1 : 1;
                if (!r.enabled || !GeometryUtility.TestPlanesAABB(planos, r.bounds))
                {
                    continue;
                }
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    continue;
                }
                // En batch isVisible no se actualiza: el LOD se estima por distancia y tamaño en pantalla.
                if (!VisiblePorLod(cam, r))
                {
                    continue;
                }
                todos.Add(new Culpable { malla = mf.sharedMesh.name, raiz = RaizDeProp(raiz, r.transform).name, triangulos = Triangulos(mf.sharedMesh) });
            }
            todos.Sort((a, b) => b.triangulos.CompareTo(a.triangulos));
            if (todos.Count > CantidadCulpables)
            {
                todos.RemoveRange(CantidadCulpables, todos.Count - CantidadCulpables);
            }
            porTipo.Clear();
            List<KeyValuePair<string, int>> lista = new List<KeyValuePair<string, int>>(cuentas);
            lista.Sort((a, b) => b.Value.CompareTo(a.Value));
            foreach (KeyValuePair<string, int> kv in lista)
            {
                porTipo.Add(kv.Key + "\t" + kv.Value);
            }
            return todos;
        }

        private static void Escribir(Informe informe, string sufijo)
        {
            string logs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            Directory.CreateDirectory(logs);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1362 rendimiento (" + sufijo + ") " + informe.fecha);
            sb.AppendLine("GPU: " + SystemInfo.graphicsDeviceName + "  |  resolución de medición " + AnchoMedicion + "x" + AltoMedicion + " (RenderTexture ARGB32)");
            sb.AppendLine("ms = Stopwatch de Render + sincronización con la GPU, promedio y p95 de " + FramesMedidos + " frames; gpuMs = -1 (no se mide); deltaTimeMs = Time.unscaledDeltaTime; contador -1 = el recorder no existe, 0 = existe pero no contó.");
            sb.AppendLine("zona-n\tms\tp95\tFPS\tgpuMs\tbatches\tsetpass\ttriangulos\tvertices\tsombras\tdeltaTimeMs\tdescripcion");
            foreach (Punto p in informe.puntos)
            {
                sb.AppendLine(p.zona + "-" + p.n + "\t" + p.msPromedio.ToString("0.0") + "\t" + p.msP95.ToString("0.0") + "\t" + p.fps.ToString("0.0") + "\t" + p.gpuMsPromedio.ToString("0.0")
                    + "\t" + p.batches + "\t" + p.setPass + "\t" + p.triangulos + "\t" + p.vertices + "\t" + p.sombras + "\t" + p.msDeltaTime.ToString("0.0") + "\t" + p.descripcion);
            }
            sb.AppendLine();
            sb.AppendLine("Punto peor: " + informe.puntoPeor);
            sb.AppendLine("== 30 renderers visibles con más triángulos (LOD0 de la malla) ==");
            foreach (Culpable c in informe.culpables)
            {
                sb.AppendLine(c.triangulos + "\t" + c.malla + "\t" + c.raiz);
            }
            sb.AppendLine();
            sb.AppendLine("== Renderers por tipo de prop en la zona del punto peor (nombre y cantidad) ==");
            foreach (string linea in informe.renderersPorTipo)
            {
                sb.AppendLine(linea);
            }
            File.WriteAllText(Path.Combine(logs, "t1362-rendimiento-" + sufijo + ".txt"), sb.ToString());
            File.WriteAllText(Path.Combine(logs, "t1362-rendimiento-" + sufijo + ".json"), JsonUtility.ToJson(informe, true));
        }
    }
}
