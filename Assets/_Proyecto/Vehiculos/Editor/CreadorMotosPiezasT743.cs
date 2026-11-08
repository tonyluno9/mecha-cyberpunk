using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-743: las motos V07, V08 y C19 con dos llantas RU02 que giran con la velocidad y la delantera que dobla con A y D. El cuerpo es una sola malla (la de
    /// C19 es C19CUERPO, sin ruedas; las de V07 y V08 son su modelo de siempre al que se le quitan las llantas horneadas: Tony aún no manda sus cuerpos).
    /// Se conserva todo lo demás: la malla sigue en Visual_T511/Visual (la que usa <see cref="ManejoMoto"/> para quitar el caballete y para inclinar),
    /// los puntos del conductor, los colliders, la física, la cámara y el asiento. Las llantas cuelgan de Visual_T511/Ruedas_T743, que se inclina con la moto.
    /// El manubrio y la horquilla van en la malla del cuerpo y no giran (solo la llanta). Sin puertas.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CreadorMotosPiezasT743.ArmarTodas
    /// </summary>
    public static class CreadorMotosPiezasT743
    {
        public const string NombreRuedas = "Ruedas_T743";
        public const string RutaLlanta = CreadorCarrosPiezasT742.CarpetaPiezas + "RU02/RU02.fbx";
        public static readonly string[] Codigos = { "V07", "V08", "C19" };
        private const float AnguloParado = 28f;
        private const float AnguloRapido = 10f;

        /// <summary>
        /// Medidas de cada moto en el marco M del modelo (el del FBX con su rotación de raíz aplicada: Y arriba, el eje largo de la moto en X, o en Z para C19).
        /// Las dos llantas van en la misma línea (Z de C19 / X de las otras es el largo). RadioCorte es el radio con el que se borra la llanta horneada.
        /// </summary>
        public readonly struct Medidas
        {
            public readonly string Codigo;
            public readonly string RutaFbxCuerpo;
            public readonly Vector3 CentroDelantera;
            public readonly Vector3 CentroTrasera;
            public readonly float RadioCorteDelantera;
            public readonly float RadioCorteTrasera;
            public readonly float AnchoCorte;
            public readonly bool CortarLlantas;

            public Medidas(string codigo, string rutaFbxCuerpo, Vector3 centroDelantera, Vector3 centroTrasera, float radioCorteDelantera, float radioCorteTrasera, float anchoCorte, bool cortarLlantas)
            {
                Codigo = codigo;
                RutaFbxCuerpo = rutaFbxCuerpo;
                CentroDelantera = centroDelantera;
                CentroTrasera = centroTrasera;
                RadioCorteDelantera = radioCorteDelantera;
                RadioCorteTrasera = radioCorteTrasera;
                AnchoCorte = anchoCorte;
                CortarLlantas = cortarLlantas;
            }
        }

        public static readonly Medidas[] Tabla =
        {
            new Medidas("V07", "Assets/Arte/Vehiculos/V07/V07.fbx", new Vector3(-0.340f, 0.144f, 0f), new Vector3(0.320f, 0.144f, 0f), 0.162f, 0.162f, 0.07f, true),
            new Medidas("V08", "Assets/Arte/Vehiculos/V08/V08.fbx", new Vector3(-0.347f, 0.159f, 0f), new Vector3(0.338f, 0.139f, 0f), 0.157f, 0.142f, 0.07f, true),
            new Medidas("C19", "Assets/Arte/Vehiculos/Piezas/C19/C19_Cuerpo.fbx", new Vector3(0f, 0.160f, -0.403f), new Vector3(0f, 0.160f, 0.384f), 0f, 0f, 0f, false),
        };

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.RutaPrefab(codigo);

        [MenuItem("Mecha Cyberpunk/T-743/Armar motos por piezas")]
        public static void ArmarTodas()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (string codigo in Codigos)
            {
                informe.AppendLine(ArmarPrefab(codigo));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t743-motos.txt", informe.ToString());
            Debug.Log("[Vehiculos] T-743:\n" + informe);
        }

        /// <summary>Arma una moto en su prefab de Vehiculos/Prefabs.</summary>
        public static string ArmarPrefab(string codigo)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(codigo));
            string resultado;
            try
            {
                resultado = Armar(raiz, codigo);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            return resultado;
        }

        /// <summary>Arma las piezas en <paramref name="raiz"/> (la moto de Vehiculos/Prefabs o la copia del tráfico). Devuelve una línea de informe.</summary>
        public static string Armar(GameObject raiz, string codigo)
        {
            Medidas m = Tabla.First(t => t.Codigo == codigo);
            GameObject fbxCuerpo = AssetDatabase.LoadAssetAtPath<GameObject>(m.RutaFbxCuerpo);
            GameObject fbxLlanta = AssetDatabase.LoadAssetAtPath<GameObject>(RutaLlanta);
            if (fbxCuerpo == null || fbxLlanta == null)
            {
                return codigo + "\tFALTAN piezas (corre ImportarS1nPiezas.MeterTodo)";
            }
            Transform envoltorio = raiz.transform.Find("Visual_T511");
            Transform visual = envoltorio.Find("Visual");
            MeshFilter filtro = visual.GetComponent<MeshFilter>();
            MeshRenderer dibujo = visual.GetComponent<MeshRenderer>();
            ManejoMoto manejo = raiz.GetComponent<ManejoMoto>();
            Quaternion raizFbx = fbxCuerpo.transform.localRotation;
            Mesh malla = fbxCuerpo.GetComponentInChildren<MeshFilter>().sharedMesh;

            if (codigo == "C19")
            {
                // El cuerpo nuevo viene con el largo en Z y el frente a -Z: se gira 180 grados para que mire a +Z, con el mismo largo que la moto de antes.
                dibujo.sharedMaterial = CreadorCarrosPiezasT742.MaterialDe(CreadorCarrosPiezasT742.CarpetaPiezas + "C19/C19_Cuerpo");
                visual.localRotation = Quaternion.Inverse(envoltorio.localRotation) * Quaternion.Euler(0f, 180f, 0f) * raizFbx;
                float largoMetros = 2.0f;
                visual.localScale = Vector3.one * (largoMetros / malla.bounds.size.y);
                visual.localPosition = Vector3.zero;
                Vector3 medio = Punto(visual, raizFbx, (m.CentroDelantera + m.CentroTrasera) * 0.5f);
                visual.localPosition = new Vector3(-medio.x, 0f, -medio.z);
            }
            else
            {
                malla = SinLlantas(malla, raizFbx, m, codigo);
            }
            filtro.sharedMesh = malla;

            // Caballete: la misma técnica de T-592 (la malla sin caballete se cambia mientras se maneja). Ahora la altura ya no se compensa: las llantas tocan el piso.
            Bounds zona = codigo == "C19" ? ZonaCaballeteC19() : CreadorMotosT592.ZonaCaballete(codigo);
            Matrix4x4 aPrefab = raiz.transform.worldToLocalMatrix * filtro.transform.localToWorldMatrix;
            Mesh sinCaballete = CreadorMotosT592.QuitarCaballete(malla, aPrefab, zona, out _, out int quitados);
            string rutaSin = CreadorMotosT592.CarpetaMallas + "MallaMoto_" + codigo + "_SinCaballete.asset";
            Mesh guardada = AssetDatabase.LoadAssetAtPath<Mesh>(rutaSin);
            if (sinCaballete != null)
            {
                sinCaballete.name = "MallaMoto_" + codigo + "_SinCaballete";
                if (guardada == null)
                {
                    AssetDatabase.CreateAsset(sinCaballete, rutaSin);
                    guardada = sinCaballete;
                }
                else
                {
                    EditorUtility.CopySerialized(sinCaballete, guardada);
                }
            }
            if (codigo != "C19")
            {
                // La malla sin llantas de V07 y V08 es un asset aparte (la del FBX no se toca).
                string rutaCuerpo = CreadorMotosT592.CarpetaMallas + "MallaMoto_" + codigo + "_SinLlantas.asset";
                Mesh cuerpoGuardado = AssetDatabase.LoadAssetAtPath<Mesh>(rutaCuerpo);
                if (cuerpoGuardado == null)
                {
                    AssetDatabase.CreateAsset(malla, rutaCuerpo);
                    cuerpoGuardado = malla;
                }
                else
                {
                    EditorUtility.CopySerialized(malla, cuerpoGuardado);
                }
                malla = cuerpoGuardado;
                filtro.sharedMesh = malla;
            }
            SerializedObject so = new SerializedObject(manejo);
            so.FindProperty("mallaCompleta").objectReferenceValue = malla;
            so.FindProperty("mallaSinCaballete").objectReferenceValue = sinCaballete != null ? guardada : null;
            so.FindProperty("bajarAlOcultar").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Llantas.
            Transform viejas = envoltorio.Find(NombreRuedas);
            if (viejas != null)
            {
                Object.DestroyImmediate(viejas.gameObject);
            }
            GameObject ruedas = new GameObject(NombreRuedas);
            ruedas.transform.SetParent(envoltorio, false);
            ruedas.transform.localRotation = Quaternion.Inverse(envoltorio.localRotation);
            Material materialLlanta = CreadorCarrosPiezasT742.MaterialDe(CreadorCarrosPiezasT742.CarpetaPiezas + "RU02/RU02");
            float escalaVisual = visual.lossyScale.x;
            Vector3 delantera = Punto(visual, raizFbx, m.CentroDelantera);
            Vector3 trasera = Punto(visual, raizFbx, m.CentroTrasera);
            float radioDelantera = m.CentroDelantera.y * escalaVisual;
            float radioTrasera = m.CentroTrasera.y * escalaVisual;
            Transform direccionDel = ArmarLlanta(fbxLlanta, ruedas.transform, "Rueda_Del", delantera, radioDelantera, materialLlanta, out Transform giroDel);
            ArmarLlanta(fbxLlanta, ruedas.transform, "Rueda_Tras", trasera, radioTrasera, materialLlanta, out Transform giroTras);

            RuedasVisualesCarro comp = raiz.GetComponent<RuedasVisualesCarro>();
            if (comp == null)
            {
                comp = raiz.AddComponent<RuedasVisualesCarro>();
            }
            SerializedObject sc = new SerializedObject(comp);
            sc.FindProperty("direccionDelantera").arraySize = 1;
            sc.FindProperty("direccionDelantera").GetArrayElementAtIndex(0).objectReferenceValue = direccionDel;
            sc.FindProperty("giro").arraySize = 2;
            sc.FindProperty("giro").GetArrayElementAtIndex(0).objectReferenceValue = giroDel;
            sc.FindProperty("giro").GetArrayElementAtIndex(1).objectReferenceValue = giroTras;
            sc.FindProperty("radio").floatValue = (radioDelantera + radioTrasera) * 0.5f;
            sc.FindProperty("distanciaEjes").floatValue = Mathf.Abs(Vector3.Dot(delantera - trasera, raiz.transform.forward));
            sc.FindProperty("anguloParado").floatValue = AnguloParado;
            sc.FindProperty("anguloRapido").floatValue = AnguloRapido;
            sc.ApplyModifiedPropertiesWithoutUndo();

            Bounds todo = new Bounds(raiz.transform.position, Vector3.zero);
            foreach (Renderer r in envoltorio.GetComponentsInChildren<Renderer>(true))
            {
                todo.Encapsulate(r.bounds);
            }
            return string.Format("{0}\tllantas r {1:F2} / {2:F2} m, entre ejes {3:F2} m, alto {4:F2} m, base y {5:F3}, caballete quitado: {6} triángulos",
                codigo, radioDelantera, radioTrasera, Mathf.Abs(Vector3.Dot(delantera - trasera, raiz.transform.forward)), todo.size.y, todo.min.y, quitados);
        }

        // Un punto del marco M del modelo (el del FBX con su rotación de raíz) a coordenadas del prefab (la raíz está en el origen).
        private static Vector3 Punto(Transform visual, Quaternion raizFbx, Vector3 enM)
        {
            return visual.TransformPoint(Quaternion.Inverse(raizFbx) * enM);
        }

        private static Bounds ZonaCaballeteC19()
        {
            Bounds b = new Bounds();
            b.SetMinMax(new Vector3(-0.8f, -0.05f, -0.14f), new Vector3(0.8f, 0.2f, 0.14f));
            return b;
        }

        // Copia de la malla sin los triángulos que caen por completo dentro de un cilindro por llanta (eje a lo ancho de la moto).
        private static Mesh SinLlantas(Mesh original, Quaternion raizFbx, Medidas m, string codigo)
        {
            Vector3[] v = original.vertices;
            int[] t = original.triangles;
            List<int> quedan = new List<int>();
            int quitados = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                bool borrar = true;
                for (int k = 0; k < 3 && borrar; k++)
                {
                    Vector3 p = raizFbx * v[t[i + k]];
                    borrar = DentroDelCilindro(p, m.CentroDelantera, m.RadioCorteDelantera, m.AnchoCorte)
                        || DentroDelCilindro(p, m.CentroTrasera, m.RadioCorteTrasera, m.AnchoCorte);
                }
                if (borrar)
                {
                    quitados++;
                    continue;
                }
                quedan.Add(t[i]);
                quedan.Add(t[i + 1]);
                quedan.Add(t[i + 2]);
            }
            Mesh nueva = Object.Instantiate(original);
            nueva.name = "MallaMoto_" + codigo + "_SinLlantas";
            nueva.triangles = quedan.ToArray();
            nueva.RecalculateBounds();
            Debug.Log("[Vehiculos] T-743: " + codigo + " sin llantas: " + quitados + " triángulos quitados.");
            return nueva;
        }

        // El eje de la llanta va en Z del marco M (el ancho de la moto); el centro de cada llanta tiene Z = 0.
        private static bool DentroDelCilindro(Vector3 p, Vector3 centro, float radio, float semiancho)
        {
            float dx = p.x - centro.x;
            float dy = p.y - centro.y;
            return Mathf.Abs(p.z - centro.z) <= semiancho && dx * dx + dy * dy <= radio * radio;
        }

        // Rueda / Direccion / Giro / Modelo. La llanta queda centrada en el pivote, con su eje a lo ancho de la moto (X del prefab).
        private static Transform ArmarLlanta(GameObject fbx, Transform padre, string nombre, Vector3 centroPrefab, float radio, Material material, out Transform giro)
        {
            GameObject rueda = new GameObject(nombre);
            rueda.transform.SetParent(padre, false);
            rueda.transform.localPosition = padre.InverseTransformPoint(centroPrefab);
            GameObject direccion = new GameObject("Direccion");
            direccion.transform.SetParent(rueda.transform, false);
            GameObject giroObjeto = new GameObject("Giro");
            giroObjeto.transform.SetParent(direccion.transform, false);
            GameObject modelo = Object.Instantiate(fbx);
            modelo.name = "Modelo";
            modelo.transform.SetParent(giroObjeto.transform, false);
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            }
            // El eje de la llanta es su dimensión más angosta: se gira para que quede en X.
            Bounds b = Limites(modelo, giroObjeto.transform);
            Vector3 tam = b.size;
            Quaternion deRaiz = modelo.transform.localRotation;
            if (tam.z < tam.x && tam.z < tam.y)
            {
                modelo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * deRaiz;
            }
            else if (tam.y < tam.x && tam.y < tam.z)
            {
                modelo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f) * deRaiz;
            }
            b = Limites(modelo, giroObjeto.transform);
            float diametro = Mathf.Max(b.size.y, b.size.z);
            modelo.transform.localScale = Vector3.one * (2f * radio / diametro);
            b = Limites(modelo, giroObjeto.transform);
            modelo.transform.localPosition = -b.center;
            giro = giroObjeto.transform;
            return direccion.transform;
        }

        private static Bounds Limites(GameObject objeto, Transform marco)
        {
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
                Matrix4x4 aMarco = marco.worldToLocalMatrix * f.transform.localToWorldMatrix;
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    Vector3 p = aMarco.MultiplyPoint3x4(v);
                    if (primero)
                    {
                        b = new Bounds(p, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(p);
                    }
                }
            }
            return b;
        }
    }
}
