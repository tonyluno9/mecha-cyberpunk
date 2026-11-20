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
    /// T-1312: los carros V02, V03, V04, V06, V09 y V10 y la moto C19 con los FBX nuevos de Blender (Assets/Arte/Vehiculos/Piezas/&lt;código&gt;/&lt;código&gt;.fbx):
    /// carrocería sin ruedas más las ruedas aparte, cada una con su pivote al centro y girando sobre su X.
    /// Carros: Visual_T511/Carroceria y, en cada ancla Rueda_xx de la suspensión, la cadena Direccion / Giro / Modelo que usa <see cref="RuedasVisualesCarro"/>.
    /// El carro nuevo mide lo mismo que el modelo de una malla de antes (largo en Z, centrado, ruedas en y = 0). Se conservan colliders, física, asiento, puntos y luces.
    /// Moto C19: el cuerpo nuevo va en Visual_T511/Visual (la malla que usa ManejoMoto) con el mismo largo de antes (2 m) y las dos ruedas del FBX en Ruedas_T743.
    /// El FBX viene con el frente a -Z del modelo (después de la rotación de raíz): se gira 180 grados en Y. Correrlo otra vez deja lo mismo.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CreadorCarrosBlenderT1312.ArmarTodos
    /// </summary>
    public static class CreadorCarrosBlenderT1312
    {
        public const string CarpetaPiezas = CreadorCarrosPiezasT742.CarpetaPiezas;
        public const string NombreVisual = CreadorCarrosPiezasT742.NombreVisual;
        public static readonly string[] Carros = { "V02", "V03", "V04", "V06", "V09", "V10" };
        public const string CodigoMoto = "C19";
        public static readonly string[] NombresRuedas = CreadorCarrosPiezasT742.NombresRuedas;

        private const float SuavidadMaterial = 0.2f;
        private const float LargoMotoMetros = 2.0f;
        private const float AnguloParadoMoto = 28f;
        private const float AnguloRapidoMoto = 10f;
        private const float BajarAlOcultar = 0f;

        public static string RutaFbx(string codigo) => CarpetaPiezas + codigo + "/" + codigo + ".fbx";

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        [MenuItem("Mecha Cyberpunk/T-1312/Armar carros con ruedas de Blender")]
        public static void ArmarTodos()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (string codigo in Carros.Append(CodigoMoto))
            {
                ConfigurarImportador(codigo);
            }
            AssetDatabase.Refresh();
            foreach (string codigo in Carros.Append(CodigoMoto))
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(codigo));
                try
                {
                    informe.AppendLine(codigo == CodigoMoto ? ArmarMoto(raiz) : ArmarCarro(raiz, codigo));
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1312-carros.txt", informe.ToString());
            Debug.Log("[Vehiculos] T-1312:\n" + informe);
        }

        // Sin materiales ni animación del FBX (los materiales son los .mat de la carpeta, con las texturas de la .fbm), sin mallas LOD.
        public static void ConfigurarImportador(string codigo)
        {
            ModelImporter importador = AssetImporter.GetAtPath(RutaFbx(codigo)) as ModelImporter;
            if (importador == null)
            {
                return;
            }
            bool cambio = false;
            if (importador.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importador.materialImportMode = ModelImporterMaterialImportMode.None;
                cambio = true;
            }
            if (importador.importAnimation)
            {
                importador.importAnimation = false;
                cambio = true;
            }
            if (importador.importCameras || importador.importLights)
            {
                importador.importCameras = false;
                importador.importLights = false;
                cambio = true;
            }
            if (cambio)
            {
                importador.SaveAndReimport();
            }
        }

        /// <summary>Arma las piezas en <paramref name="raiz"/> (el prefab Carro_Vxx). Devuelve una línea de informe.</summary>
        public static string ArmarCarro(GameObject raiz, string codigo)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(codigo));
            if (fbx == null || fbx.transform.Find("Carroceria") == null)
            {
                return codigo + "\tFALTA " + RutaFbx(codigo);
            }
            Quaternion deRaiz = fbx.transform.localRotation;
            Materiales(codigo, out Material materialCarroceria, out Material materialRueda);

            Transform viejo = raiz.transform.Find(NombreVisual);
            bool armado = viejo != null && viejo.Find("Carroceria") != null;
            Bounds antes = default;
            Vector3 posicionPrevia = Vector3.zero;
            float escalaPrevia = 0f;
            if (armado)
            {
                posicionPrevia = viejo.localPosition;
                escalaPrevia = viejo.localScale.x;
            }
            else
            {
                antes = CreadorCarrosT511.LimitesExactos(viejo.gameObject);
            }
            Object.DestroyImmediate(viejo.gameObject);

            GameObject visual = new GameObject(NombreVisual);
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.SetSiblingIndex(0);
            visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * deRaiz;
            Pieza(fbx.transform.Find("Carroceria"), "Carroceria", visual.transform, materialCarroceria);
            GameObject[] medidas = new GameObject[NombresRuedas.Length];
            for (int i = 0; i < NombresRuedas.Length; i++)
            {
                medidas[i] = Pieza(fbx.transform.Find(NombresRuedas[i]), "Medida_" + NombresRuedas[i], visual.transform, null);
            }

            Bounds nueva = CreadorCarrosT511.LimitesExactos(visual);
            if (armado && CarrosRevisadosT1507.EnMetros(codigo))
            {
                // T-1507: el FBX ya viene en metros; sin escala y con las llantas más bajas justo en el piso.
                visual.transform.localScale = Vector3.one;
                visual.transform.localPosition = new Vector3(posicionPrevia.x, -nueva.min.y, posicionPrevia.z);
            }
            else if (armado)
            {
                visual.transform.localScale = Vector3.one * escalaPrevia;
                visual.transform.localPosition = posicionPrevia;
            }
            else
            {
                visual.transform.localScale = Vector3.one * (antes.size.z / nueva.size.z);
                nueva = CreadorCarrosT511.LimitesExactos(visual);
                visual.transform.localPosition = new Vector3(antes.center.x - nueva.center.x, -nueva.min.y, antes.center.z - nueva.center.z);
            }
            float escala = visual.transform.localScale.x;

            Transform[] direccionDelantera = new Transform[2];
            Transform[] giro = new Transform[4];
            float sumaRadios = 0f;
            for (int i = 0; i < NombresRuedas.Length; i++)
            {
                Transform ancla = raiz.transform.Find(NombresRuedas[i]);
                Bounds b = CreadorCarrosT511.LimitesExactos(medidas[i]);
                sumaRadios += b.extents.y;
                foreach (Transform hijo in ancla.Cast<Transform>().ToList())
                {
                    Object.DestroyImmediate(hijo.gameObject);
                }
                Mesh malla = fbx.transform.Find(NombresRuedas[i]).GetComponent<MeshFilter>().sharedMesh;
                Transform cadenaDireccion = ArmarCadena(ancla, b.center, malla, visual.transform.rotation, escala, materialRueda, out Transform cadenaGiro);
                if (i < 2)
                {
                    direccionDelantera[i] = cadenaDireccion;
                }
                giro[i] = cadenaGiro;
            }
            foreach (GameObject m in medidas)
            {
                Object.DestroyImmediate(m);
            }

            Transform delanteraDI = raiz.transform.Find("Rueda_DI");
            Transform traseraTI = raiz.transform.Find("Rueda_TI");
            float distanciaEjes = Mathf.Abs(raiz.transform.InverseTransformPoint(delanteraDI.position).z - raiz.transform.InverseTransformPoint(traseraTI.position).z);
            RuedasVisualesCarro ruedas = raiz.GetComponent<RuedasVisualesCarro>();
            if (ruedas == null)
            {
                ruedas = raiz.AddComponent<RuedasVisualesCarro>();
            }
            SerializedObject so = new SerializedObject(ruedas);
            Poner(so.FindProperty("direccionDelantera"), direccionDelantera);
            Poner(so.FindProperty("giro"), giro);
            so.FindProperty("radio").floatValue = sumaRadios / NombresRuedas.Length;
            so.FindProperty("distanciaEjes").floatValue = distanciaEjes;
            so.ApplyModifiedPropertiesWithoutUndo();

            Bounds final = CreadorCarrosT511.LimitesExactos(visual);
            return string.Format("{0}\tcarrocería {1:F2} x {2:F2} x {3:F2} m (escala {4:F3}), radio de rueda {5:F3} m, entre ejes {6:F2} m",
                codigo, final.size.x, final.size.y, final.size.z, escala, sumaRadios / NombresRuedas.Length, distanciaEjes);
        }

        /// <summary>Arma la moto C19 en <paramref name="raiz"/> (el prefab Carro_C19). Devuelve una línea de informe.</summary>
        public static string ArmarMoto(GameObject raiz)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(CodigoMoto));
            if (fbx == null || fbx.transform.Find("Cuerpo") == null)
            {
                return CodigoMoto + "\tFALTA " + RutaFbx(CodigoMoto);
            }
            Quaternion deRaiz = fbx.transform.localRotation;
            Materiales(CodigoMoto, out Material materialCuerpo, out Material materialRueda);
            Transform envoltorio = raiz.transform.Find(NombreVisual);
            Transform visual = envoltorio.Find("Visual");
            MeshFilter filtro = visual.GetComponent<MeshFilter>();
            MeshRenderer dibujo = visual.GetComponent<MeshRenderer>();
            ManejoMoto manejo = raiz.GetComponent<ManejoMoto>();
            Mesh malla = fbx.transform.Find("Cuerpo").GetComponent<MeshFilter>().sharedMesh;
            Transform rd = fbx.transform.Find("Rueda_D");
            Transform rt = fbx.transform.Find("Rueda_T");

            // Mismo largo de antes (2 m), de frente a +Z, con el centro entre las dos ruedas sobre el origen y las ruedas en el piso.
            dibujo.sharedMaterial = materialCuerpo;
            filtro.sharedMesh = malla;
            visual.localRotation = Quaternion.Inverse(envoltorio.localRotation) * Quaternion.Euler(0f, 180f, 0f) * deRaiz;
            visual.localScale = Vector3.one * (CarrosRevisadosT1507.EnMetros(CodigoMoto) ? 1f : LargoMotoMetros / malla.bounds.size.y);
            visual.localPosition = Vector3.zero;
            Vector3 centroD = CentroEnModelo(rd);
            Vector3 centroT = CentroEnModelo(rt);
            Vector3 medio = visual.TransformPoint((centroD + centroT) * 0.5f);
            visual.localPosition = new Vector3(-medio.x, 0f, -medio.z);
            float escala = visual.lossyScale.x;
            Vector3 mundoD = visual.TransformPoint(centroD);
            Vector3 mundoT = visual.TransformPoint(centroT);
            float radioD = (rd.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y) * escala;
            float radioT = (rt.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y) * escala;

            // Caballete: la malla sin caballete se cambia mientras se maneja (misma zona que T-743).
            Bounds zona = new Bounds();
            zona.SetMinMax(new Vector3(-0.8f, -0.05f, -0.14f), new Vector3(0.8f, 0.2f, 0.14f));
            Matrix4x4 aPrefab = raiz.transform.worldToLocalMatrix * filtro.transform.localToWorldMatrix;
            Mesh sinCaballete = CreadorMotosT592.QuitarCaballete(malla, aPrefab, zona, out _, out int quitados);
            string rutaSin = CreadorMotosT592.CarpetaMallas + "MallaMoto_" + CodigoMoto + "_SinCaballete.asset";
            Mesh guardada = AssetDatabase.LoadAssetAtPath<Mesh>(rutaSin);
            if (sinCaballete != null)
            {
                sinCaballete.name = "MallaMoto_" + CodigoMoto + "_SinCaballete";
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
            SerializedObject sm = new SerializedObject(manejo);
            sm.FindProperty("mallaCompleta").objectReferenceValue = malla;
            sm.FindProperty("mallaSinCaballete").objectReferenceValue = sinCaballete != null ? guardada : null;
            sm.FindProperty("bajarAlOcultar").floatValue = BajarAlOcultar;
            sm.ApplyModifiedPropertiesWithoutUndo();

            // Ruedas.
            Transform viejas = envoltorio.Find(CreadorMotosPiezasT743.NombreRuedas);
            if (viejas != null)
            {
                Object.DestroyImmediate(viejas.gameObject);
            }
            GameObject ruedas = new GameObject(CreadorMotosPiezasT743.NombreRuedas);
            ruedas.transform.SetParent(envoltorio, false);
            ruedas.transform.localRotation = Quaternion.Inverse(envoltorio.localRotation);
            Transform direccionDel = ArmarRuedaMoto(ruedas.transform, "Rueda_Del", mundoD, rd.GetComponent<MeshFilter>().sharedMesh, visual.rotation, escala, materialRueda, out Transform giroDel);
            ArmarRuedaMoto(ruedas.transform, "Rueda_Tras", mundoT, rt.GetComponent<MeshFilter>().sharedMesh, visual.rotation, escala, materialRueda, out Transform giroTras);

            RuedasVisualesCarro comp = raiz.GetComponent<RuedasVisualesCarro>();
            if (comp == null)
            {
                comp = raiz.AddComponent<RuedasVisualesCarro>();
            }
            float entreEjes = Mathf.Abs(Vector3.Dot(mundoD - mundoT, raiz.transform.forward));
            SerializedObject sc = new SerializedObject(comp);
            Poner(sc.FindProperty("direccionDelantera"), new[] { direccionDel });
            Poner(sc.FindProperty("giro"), new[] { giroDel, giroTras });
            sc.FindProperty("radio").floatValue = (radioD + radioT) * 0.5f;
            sc.FindProperty("distanciaEjes").floatValue = entreEjes;
            sc.FindProperty("anguloParado").floatValue = AnguloParadoMoto;
            sc.FindProperty("anguloRapido").floatValue = AnguloRapidoMoto;
            sc.ApplyModifiedPropertiesWithoutUndo();
            return string.Format("{0}\tmoto: ruedas r {1:F3} / {2:F3} m, entre ejes {3:F2} m, escala {4:F3}, caballete quitado: {5} triángulos",
                CodigoMoto, radioD, radioT, entreEjes, escala, quitados);
        }

        // Centro (en el marco de la malla del FBX, sin la rotación de raíz) de la caja de la malla de una pieza.
        private static Vector3 CentroEnModelo(Transform pieza)
        {
            return pieza.localPosition + pieza.localRotation * Vector3.Scale(pieza.localScale, pieza.GetComponent<MeshFilter>().sharedMesh.bounds.center);
        }

        // Una pieza del FBX como hijo sin vínculo al asset (solo la malla y su material), en el mismo lugar del FBX.
        public static GameObject Pieza(Transform origen, string nombre, Transform padre, Material material)
        {
            GameObject pieza = new GameObject(nombre);
            pieza.transform.SetParent(padre, false);
            pieza.transform.localPosition = origen.localPosition;
            pieza.transform.localRotation = origen.localRotation;
            pieza.transform.localScale = origen.localScale;
            pieza.AddComponent<MeshFilter>().sharedMesh = origen.GetComponent<MeshFilter>().sharedMesh;
            if (material != null)
            {
                pieza.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            return pieza;
        }

        // Direccion / Giro / Modelo colgando de <paramref name="padre"/>; la llanta queda centrada en el pivote, con su malla orientada como el carro.
        // Devuelve Direccion; Giro sale en <paramref name="giro"/>.
        public static Transform ArmarCadena(Transform padre, Vector3 centroMundo, Mesh malla, Quaternion rotacionMundo, float escala, Material material, out Transform giro)
        {
            GameObject direccion = new GameObject("Direccion");
            direccion.transform.SetParent(padre, false);
            direccion.transform.position = centroMundo;
            GameObject giroObjeto = new GameObject("Giro");
            giroObjeto.transform.SetParent(direccion.transform, false);
            GameObject modelo = new GameObject("Modelo");
            modelo.transform.SetParent(giroObjeto.transform, false);
            modelo.AddComponent<MeshFilter>().sharedMesh = malla;
            modelo.AddComponent<MeshRenderer>().sharedMaterial = material;
            modelo.transform.rotation = rotacionMundo;
            modelo.transform.localScale = Vector3.one * (escala / giroObjeto.transform.lossyScale.x);
            Bounds b = LimitesEn(modelo, giroObjeto.transform);
            modelo.transform.localPosition = -b.center;
            giro = giroObjeto.transform;
            return direccion.transform;
        }

        private static Transform ArmarRuedaMoto(Transform padre, string nombre, Vector3 centroMundo, Mesh malla, Quaternion rotacionMundo, float escala, Material material, out Transform giro)
        {
            GameObject rueda = new GameObject(nombre);
            rueda.transform.SetParent(padre, false);
            rueda.transform.position = centroMundo;
            return ArmarCadena(rueda.transform, centroMundo, malla, rotacionMundo, escala, material, out giro);
        }

        public static Bounds LimitesEn(GameObject objeto, Transform marco)
        {
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
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

        public static void Poner(SerializedProperty lista, Transform[] valores)
        {
            lista.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
            }
        }

        // Materiales URP Lit con las texturas de la carpeta .fbm del FBX. Las ruedas usan su propia textura (la que lleva "tire" en el nombre) y, si el FBX no trae una, la de la carrocería.
        public static void Materiales(string codigo, out Material carroceria, out Material rueda)
        {
            string carpeta = CarpetaPiezas + codigo + "/" + codigo + ".fbm";
            string[] colores = Directory.GetFiles(carpeta, "*_basecolor.*").Where(f => !f.EndsWith(".meta")).Select(f => f.Replace('\\', '/')).ToArray();
            string colorRueda = colores.FirstOrDefault(f => Path.GetFileName(f).Contains("tire"));
            string colorCarroceria = colores.First(f => f != colorRueda);
            carroceria = MaterialDe(CarpetaPiezas + codigo + "/" + codigo + "_Blender_Carroceria.mat", colorCarroceria);
            rueda = colorRueda == null ? carroceria : MaterialDe(CarpetaPiezas + codigo + "/" + codigo + "_Blender_Rueda.mat", colorRueda);
        }

        private static Material MaterialDe(string rutaMaterial, string rutaColor)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, rutaMaterial);
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaColor);
            string rutaNormal = rutaColor.Replace("_basecolor", "_normal");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaNormal);
            if (normal != null)
            {
                TextureImporter ti = AssetImporter.GetAtPath(rutaNormal) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.NormalMap)
                {
                    ti.textureType = TextureImporterType.NormalMap;
                    ti.SaveAndReimport();
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaNormal);
                }
            }
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_MainTex", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", SuavidadMaterial);
            material.SetFloat("_Metallic", 0f);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
