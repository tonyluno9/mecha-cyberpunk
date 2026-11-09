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
    /// T-742: los carros V01a, V01b y V05 armados por piezas. Reemplaza el modelo de una sola malla (Visual_T511 / Visual) por
    /// Visual_T511 con la carrocería sin puertas ni ruedas (Carroceria) y dos pivotes de puerta en la bisagra (Puerta_Izq y Puerta_Der, cada uno con su
    /// puerta de Blender), y pone la llanta RU01 en las cuatro ruedas (Rueda_xx / Direccion / Giro / Modelo). Sin vidrio, para ver al conductor.
    /// Conserva el tamaño del carro de antes (la carrocería nueva mide lo mismo que el modelo viejo, pies en y = 0), los colliders de cubos, la física y
    /// la cámara; solo se renombran Asiento a Asiento_Conductor y PuntoSubida a Punto_Puerta. Agrega <see cref="RuedasVisualesCarro"/> y <see cref="PuertasCarro"/>.
    /// Las piezas las mete <c>ImportarS1nPiezas</c> (Assets/Arte/Vehiculos/Piezas/). Correrlo otra vez deja lo mismo.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CreadorCarrosPiezasT742.ArmarTodos
    /// </summary>
    public static class CreadorCarrosPiezasT742
    {
        public const string CarpetaPiezas = "Assets/Arte/Vehiculos/Piezas/";
        public const string RutaLlanta = CarpetaPiezas + "RU01/RU01.fbx";
        public const string NombreVisual = "Visual_T511";
        public const string NombreAsiento = "Asiento_Conductor";
        public const string NombrePuntoPuerta = "Punto_Puerta";
        public static readonly string[] Codigos = { "V01a", "V01b", "V05" };
        public static readonly string[] NombresRuedas = { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" };

        private const float SuavidadMaterial = 0.2f;
        private const float MetalicoMaterial = 0f;

        /// <summary>
        /// Lo que se ajusta a mano por carro. Las X y Z van en el marco del modelo (unidades del FBX de la carrocería, que mide cerca de 1 de largo):
        /// el frente del carro va a -X y su izquierda a -Z. Puerta: escala sobre el FBX de la puerta, X del centro, Y del borde de abajo y Z de la cara de afuera.
        /// Ruedas: radio en metros, X del eje delantero y del trasero, y Z (distancia al centro) del centro de cada llanta.
        /// </summary>
        public readonly struct Medidas
        {
            public readonly string Codigo;
            public readonly float EscalaPuerta;
            public readonly float PuertaX;
            public readonly float PuertaY;
            public readonly float PuertaZExterior;
            public readonly float RadioRueda;
            public readonly float RuedaXDelantera;
            public readonly float RuedaXTrasera;
            public readonly float RuedaZ;

            public Medidas(string codigo, float escalaPuerta, float puertaX, float puertaY, float puertaZExterior, float radioRueda,
                float ruedaXDelantera, float ruedaXTrasera, float ruedaZ)
            {
                Codigo = codigo;
                EscalaPuerta = escalaPuerta;
                PuertaX = puertaX;
                PuertaY = puertaY;
                PuertaZExterior = puertaZExterior;
                RadioRueda = radioRueda;
                RuedaXDelantera = ruedaXDelantera;
                RuedaXTrasera = ruedaXTrasera;
                RuedaZ = ruedaZ;
            }
        }

        public static readonly Medidas[] Tabla =
        {
            new Medidas("V01a", 0.277f, -0.007f, 0.04f, 0.25f, 0.34f, -0.273f, 0.299f, 0.20f),
            new Medidas("V01b", 0.245f, -0.003f, 0.06f, 0.24f, 0.34f, -0.297f, 0.299f, 0.20f),
            new Medidas("V05", 0.268f, -0.030f, 0.03f, 0.24f, 0.32f, -0.293f, 0.280f, 0.20f),
        };

        public static bool EsPorPiezas(string codigo) => Codigos.Contains(codigo);

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        [MenuItem("Mecha Cyberpunk/T-742/Armar carros por piezas")]
        public static void ArmarTodos()
        {
            StringBuilder informe = new StringBuilder();
            AssetDatabase.Refresh();
            foreach (string codigo in Codigos)
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(codigo));
                try
                {
                    informe.AppendLine(Armar(raiz, codigo));
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t742-carros.txt", informe.ToString());
            Debug.Log("[Vehiculos] T-742:\n" + informe);
        }

        /// <summary>Arma las piezas en <paramref name="raiz"/> (un carro de Vehiculos/Prefabs o la patrulla). Devuelve una línea de informe.</summary>
        public static string Armar(GameObject raiz, string codigo)
        {
            Medidas m = Tabla.First(t => t.Codigo == codigo);
            GameObject fbxCarroceria = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPiezas + codigo + "/" + codigo + "_Carroceria.fbx");
            GameObject fbxIzq = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPiezas + codigo + "/" + codigo + "_Puerta_Izq.fbx");
            GameObject fbxDer = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPiezas + codigo + "/" + codigo + "_Puerta_Der.fbx");
            GameObject fbxLlanta = AssetDatabase.LoadAssetAtPath<GameObject>(RutaLlanta);
            if (fbxCarroceria == null || fbxIzq == null || fbxDer == null || fbxLlanta == null)
            {
                return codigo + "\tFALTAN piezas en " + CarpetaPiezas + " (corre ImportarS1nPiezas.MeterTodo)";
            }
            Material materialCarro = MaterialDe(CarpetaPiezas + codigo + "/" + codigo + "_Carroceria");
            Material materialPuerta = MaterialDe(CarpetaPiezas + codigo + "/" + codigo + "_Puerta");
            Material materialLlanta = MaterialDe(CarpetaPiezas + "RU01/RU01");

            // El tamaño y el lugar del modelo de antes (o del armado anterior): la carrocería nueva queda igual.
            Transform viejo = raiz.transform.Find(NombreVisual);
            Bounds antes = viejo != null && viejo.Find("Carroceria") == null
                ? CreadorCarrosT511.LimitesExactos(viejo.gameObject)
                : default;
            Vector3 posicionPrevia = Vector3.zero;
            float escalaPrevia = 0f;
            if (viejo != null && viejo.Find("Carroceria") != null)
            {
                posicionPrevia = viejo.localPosition;
                escalaPrevia = viejo.localScale.x;
            }
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }

            GameObject visual = new GameObject(NombreVisual);
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.SetSiblingIndex(0);
            visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject carroceria = Pieza(fbxCarroceria, "Carroceria", visual.transform, materialCarro);
            if (escalaPrevia > 0f)
            {
                visual.transform.localScale = Vector3.one * escalaPrevia;
                visual.transform.localPosition = posicionPrevia;
            }
            else
            {
                Bounds nueva = CreadorCarrosT511.LimitesExactos(visual);
                float escala = antes.size.z / nueva.size.z;
                visual.transform.localScale = Vector3.one * escala;
                nueva = CreadorCarrosT511.LimitesExactos(visual);
                visual.transform.localPosition = new Vector3(antes.center.x - nueva.center.x, antes.min.y - nueva.min.y, antes.center.z - nueva.center.z);
            }
            float escalaVisual = visual.transform.localScale.x;

            Transform puertaIzq = ArmarPuerta(fbxIzq, "Puerta_Izq", -1f, m, visual.transform, materialPuerta);
            Transform puertaDer = ArmarPuerta(fbxDer, "Puerta_Der", 1f, m, visual.transform, materialPuerta);

            // Ruedas: las anclas de la suspensión (Rueda_xx) se quedan donde están; la llanta se coloca en el hueco de la carrocería.
            Transform[] direccionDelantera = new Transform[2];
            Transform[] giro = new Transform[4];
            for (int i = 0; i < NombresRuedas.Length; i++)
            {
                Transform ancla = raiz.transform.Find(NombresRuedas[i]);
                bool delantera = i < 2;
                bool izquierda = i % 2 == 0;
                Vector3 centroModelo = new Vector3(delantera ? m.RuedaXDelantera : m.RuedaXTrasera, m.RadioRueda / escalaVisual, (izquierda ? -1f : 1f) * m.RuedaZ);
                Vector3 centroLocal = ancla.InverseTransformPoint(visual.transform.TransformPoint(centroModelo));
                Transform[] cadena = ArmarRueda(fbxLlanta, ancla, centroLocal, m.RadioRueda, izquierda, materialLlanta);
                if (delantera)
                {
                    direccionDelantera[i] = cadena[0];
                }
                giro[i] = cadena[1];
            }

            Transform asiento = raiz.transform.Find("Asiento");
            if (asiento != null)
            {
                asiento.name = NombreAsiento;
            }
            Transform punto = raiz.transform.Find("PuntoSubida");
            if (punto != null)
            {
                punto.name = NombrePuntoPuerta;
            }

            Transform delanteraDI = raiz.transform.Find("Rueda_DI");
            Transform traseraTI = raiz.transform.Find("Rueda_TI");
            float distanciaEjes = Mathf.Abs(raiz.transform.InverseTransformPoint(delanteraDI.position).z - raiz.transform.InverseTransformPoint(traseraTI.position).z);
            PonerRuedas(raiz, direccionDelantera, giro, m.RadioRueda, distanciaEjes);
            PonerPuertas(raiz, puertaIzq, puertaDer);

            Bounds final = CreadorCarrosT511.LimitesExactos(visual);
            return string.Format("{0}\tcarrocería {1:F2} x {2:F2} x {3:F2} m (escala {4:F3})", codigo, final.size.x, final.size.y, final.size.z, escalaVisual);
        }

        // Una pieza del FBX como hijo sin vínculo al asset (solo conserva la malla), con su material.
        private static GameObject Pieza(GameObject fbx, string nombre, Transform padre, Material material)
        {
            GameObject copia = Object.Instantiate(fbx);
            copia.name = nombre;
            copia.transform.SetParent(padre, false);
            foreach (Renderer r in copia.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            }
            return copia;
        }

        // Pivote en la bisagra (borde delantero de la puerta) con la puerta de hijo. Marco del modelo: el frente va a -X.
        private static Transform ArmarPuerta(GameObject fbx, string nombre, float lado, Medidas m, Transform visual, Material material)
        {
            GameObject pivote = new GameObject(nombre);
            pivote.transform.SetParent(visual, false);
            GameObject modelo = Pieza(fbx, "Modelo", pivote.transform, material);
            Bounds b = LimitesEn(modelo, pivote.transform);
            float k = m.EscalaPuerta;
            modelo.transform.localScale = Vector3.one * k;
            // La bisagra queda en el borde delantero (X mínima), abajo (Y mínima) y a la mitad del grosor (Z) de la puerta.
            modelo.transform.localPosition = -new Vector3(b.min.x, b.min.y, b.center.z) * k;
            float xBisagra = m.PuertaX - b.size.x * k * 0.5f;
            float zCentro = lado * (m.PuertaZExterior - b.size.z * k * 0.5f);
            pivote.transform.localPosition = new Vector3(xBisagra, m.PuertaY, zCentro);
            return pivote.transform;
        }

        // Rueda_xx / Direccion / Giro / Modelo. La llanta queda centrada en el pivote, con la cara de la llanta hacia afuera. Devuelve {Direccion, Giro}.
        private static Transform[] ArmarRueda(GameObject fbx, Transform ancla, Vector3 centroLocal, float radio, bool izquierda, Material material)
        {
            foreach (Transform hijo in ancla.Cast<Transform>().ToList())
            {
                Object.DestroyImmediate(hijo.gameObject);
            }
            GameObject direccion = new GameObject("Direccion");
            direccion.transform.SetParent(ancla, false);
            direccion.transform.localPosition = centroLocal;
            GameObject giro = new GameObject("Giro");
            giro.transform.SetParent(direccion.transform, false);
            GameObject modelo = Pieza(fbx, "Modelo", giro.transform, material);
            Bounds b = LimitesEn(modelo, giro.transform);
            float diametro = Mathf.Max(b.size.y, b.size.z);
            float escala = 2f * radio / diametro;
            modelo.transform.localScale = Vector3.one * escala;
            modelo.transform.localRotation = Quaternion.Euler(0f, izquierda ? 180f : 0f, 0f);
            Bounds centrada = LimitesEn(modelo, giro.transform);
            modelo.transform.localPosition = -centrada.center;
            return new[] { direccion.transform, giro.transform };
        }

        // Límites exactos de las mallas de <paramref name="objeto"/> (recorriendo los vértices) en el espacio local de <paramref name="marco"/>.
        private static Bounds LimitesEn(GameObject objeto, Transform marco)
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

        private static void PonerRuedas(GameObject raiz, Transform[] direccionDelantera, Transform[] giro, float radio, float distanciaEjes)
        {
            RuedasVisualesCarro ruedas = raiz.GetComponent<RuedasVisualesCarro>();
            if (ruedas == null)
            {
                ruedas = raiz.AddComponent<RuedasVisualesCarro>();
            }
            SerializedObject so = new SerializedObject(ruedas);
            Poner(so.FindProperty("direccionDelantera"), direccionDelantera);
            Poner(so.FindProperty("giro"), giro);
            so.FindProperty("radio").floatValue = radio;
            so.FindProperty("distanciaEjes").floatValue = distanciaEjes;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PonerPuertas(GameObject raiz, Transform izquierda, Transform derecha)
        {
            PuertasCarro puertas = raiz.GetComponent<PuertasCarro>();
            if (puertas == null)
            {
                puertas = raiz.AddComponent<PuertasCarro>();
            }
            SerializedObject so = new SerializedObject(puertas);
            so.FindProperty("puertaIzquierda").objectReferenceValue = izquierda;
            so.FindProperty("puertaDerecha").objectReferenceValue = derecha;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Poner(SerializedProperty lista, Transform[] valores)
        {
            lista.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
            }
        }

        // URP Lit con la textura de color y la normal de la pieza (junto al .fbx: <nombre>_basecolor.jpg y <nombre>_normal.jpg). Se crea una vez.
        public static Material MaterialDe(string rutaSinExtension)
        {
            string ruta = rutaSinExtension + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaSinExtension + "_basecolor.jpg");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaSinExtension + "_normal.jpg");
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_MainTex", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", SuavidadMaterial);
            material.SetFloat("_Metallic", MetalicoMaterial);
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
