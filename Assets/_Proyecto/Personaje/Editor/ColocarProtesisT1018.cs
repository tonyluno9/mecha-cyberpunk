using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using MechaCyberpunk.Personaje.Habilidades;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1018: mete los modelos de Tripo de las prótesis de ataque (IMP04 brazo láser, IMP05 hombrera de misiles) a Assets/Arte/Protesis/, con material URP Lit,
    /// arma su prefab (raíz = el hueso; "Modelo" con la pose en el hueso; "Punta" = de donde sale el rayo o los misiles) y lo asigna al campo <c>modelo</c> de cada
    /// <see cref="DatosProtesisAtaque"/> del catálogo. La pose en el hueso se mide con el esqueleto de PJ01 (<see cref="Calibrar"/>).
    /// Los .zip se buscan en imageassets/modelos3d/protesis/ del proyecto y, si no, de la carpeta principal (hermana mecha-cyberpunk). Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.ColocarProtesisT1018.Colocar
    /// </summary>
    public static class ColocarProtesisT1018
    {
        public const string CarpetaArte = "Assets/Arte/Protesis/";
        public const string RutaCatalogo = "Assets/_Proyecto/Personaje/Datos/Resources/CatalogoProtesisAtaque.asset";
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        public const string NombreModelo = "Modelo";
        public const string NombrePunta = "Punta";
        private static readonly string[] CarpetasZip = { "imageassets/modelos3d/protesis/", "../mecha-cyberpunk/imageassets/modelos3d/protesis/" };

        private const float MetalicoMaterial = 0.5f;
        private const float SuavidadMaterial = 0.5f;

        public enum ModoColocacion
        {
            /// <summary>A lo largo del hueso hacia la mano (el brazo láser).</summary>
            Antebrazo,
            /// <summary>Derecho en el cuerpo, sobre un punto de un hueso (la hombrera).</summary>
            Hombro,
        }

        public readonly struct Prototesis
        {
            public readonly string Codigo;
            public readonly string Nombre;
            public readonly HabilidadProtesis Habilidad;
            public readonly ModoColocacion Modo;
            /// <summary>Hueso al que se pega (sigue su animación).</summary>
            public readonly HumanBodyBones Hueso;
            /// <summary>Antebrazo: el hueso hacia el que apunta (la mano). Hombro: el hueso sobre el que se coloca (la parte alta del brazo).</summary>
            public readonly HumanBodyBones HuesoHacia;
            /// <summary>Largo (m) que mide el modelo en su dimensión mayor.</summary>
            public readonly float Largo;
            /// <summary>Antebrazo: eje del modelo (en su marco) que va hacia la mano, y eje del modelo que mira hacia afuera del cuerpo.</summary>
            public readonly Vector3 EjeALaPunta;
            public readonly Vector3 EjeAfuera;
            /// <summary>Desplazamiento (m) del centro del modelo en el marco del cuerpo (x derecha, y arriba, z adelante).</summary>
            public readonly Vector3 Ajuste;
            /// <summary>Hombro: giro (°) del modelo sobre el eje vertical del cuerpo para que el depósito quede atrás y hacia adentro.</summary>
            public readonly float GiroHombro;

            public Prototesis(string codigo, string nombre, HabilidadProtesis habilidad, ModoColocacion modo, HumanBodyBones hueso, HumanBodyBones huesoHacia, float largo,
                Vector3 ejeALaPunta, Vector3 ejeAfuera, Vector3 ajuste, float giroHombro = 0f)
            {
                Codigo = codigo;
                Nombre = nombre;
                Habilidad = habilidad;
                Modo = modo;
                Hueso = hueso;
                HuesoHacia = huesoHacia;
                Largo = largo;
                EjeALaPunta = ejeALaPunta;
                EjeAfuera = ejeAfuera;
                Ajuste = ajuste;
                GiroHombro = giroHombro;
            }

            public string Carpeta => CarpetaArte + Codigo + "_" + Nombre + "/";
            public string RutaFbx => Carpeta + Codigo + "_" + Nombre + ".fbx";
            public string RutaMaterial => Carpeta + Codigo + "_" + Nombre + ".mat";
            public string RutaPrefab => Carpeta + Codigo + "_" + Nombre + ".prefab";
            public string Zip => Codigo + "_" + Nombre + ".zip";
        }

        // IMP04 viene como un brazo colgando (el cañón arriba, la mano de dedos abajo en -Y, el dorso de la mano hacia -Z).
        // IMP05 viene derecha: la media cúpula del hombro y, a su lado en +X y detrás en -Z, el depósito de misiles.
        public static readonly Prototesis[] Todas =
        {
            new Prototesis("IMP04", "BrazoLaser", HabilidadProtesis.Laser, ModoColocacion.Antebrazo, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 0.46f,
                Vector3.down, Vector3.back, Vector3.zero),
            new Prototesis("IMP05", "HombreraMisiles", HabilidadProtesis.Misiles, ModoColocacion.Hombro, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, 0.3f,
                Vector3.up, Vector3.right, new Vector3(0.04f, 0.1f, -0.02f), 90f),
        };

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-1018/Colocar prótesis en el cuerpo")]
        public static void Colocar()
        {
            Informe.Clear();
            foreach (Prototesis p in Todas)
            {
                Importar(p);
            }
            AssetDatabase.Refresh();
            foreach (Prototesis p in Todas)
            {
                Material material = CrearMaterial(p);
                GameObject prefab = ArmarPrefab(p, material);
                Asignar(p, prefab);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1018-protesis.txt", Informe.ToString());
            Debug.Log("[Personaje] T-1018:\n" + Informe);
        }

        private static string BuscarZip(string nombre)
        {
            return CarpetasZip.Select(c => c + nombre).FirstOrDefault(File.Exists);
        }

        // Desempaca el .zip de Tripo: el .fbx y las cuatro imágenes (color _0_0, normal _0_3, metálico, rugosidad) con el nombre de la convención de armas.
        private static void Importar(Prototesis p)
        {
            string zip = BuscarZip(p.Zip);
            if (zip == null)
            {
                Informe.AppendLine("falta " + p.Zip);
                return;
            }
            Directory.CreateDirectory(p.Carpeta + "Texturas");
            using (ZipArchive archivo = ZipFile.OpenRead(zip))
            {
                foreach (ZipArchiveEntry e in archivo.Entries.Where(x => x.Length > 0))
                {
                    string nombre = e.Name.ToLowerInvariant();
                    string destino = null;
                    if (nombre.EndsWith(".fbx")) { destino = p.RutaFbx; }
                    else if (nombre.EndsWith("_0_0.jpg")) { destino = p.Carpeta + "Texturas/" + p.Codigo + "_basecolor.jpg"; }
                    else if (nombre.EndsWith("_0_3.jpg")) { destino = p.Carpeta + "Texturas/" + p.Codigo + "_normal.jpg"; }
                    else if (nombre.EndsWith("_metallic.jpg")) { destino = p.Carpeta + "Texturas/" + p.Codigo + "_metallic.jpg"; }
                    else if (nombre.EndsWith("_roughness.jpg")) { destino = p.Carpeta + "Texturas/" + p.Codigo + "_roughness.jpg"; }
                    if (destino == null) { continue; }
                    if (File.Exists(destino) && new FileInfo(destino).Length == e.Length) { continue; }
                    e.ExtractToFile(destino, true);
                    Informe.AppendLine("copiado " + destino);
                }
            }
            AssetDatabase.ImportAsset(p.RutaFbx, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();
            var importador = (ModelImporter)AssetImporter.GetAtPath(p.RutaFbx);
            if (importador != null && (importador.materialImportMode != ModelImporterMaterialImportMode.None || importador.importAnimation))
            {
                importador.materialImportMode = ModelImporterMaterialImportMode.None;
                importador.importAnimation = false;
                importador.SaveAndReimport();
            }
            string normal = p.Carpeta + "Texturas/" + p.Codigo + "_normal.jpg";
            var texImportador = AssetImporter.GetAtPath(normal) as TextureImporter;
            if (texImportador != null && texImportador.textureType != TextureImporterType.NormalMap)
            {
                texImportador.textureType = TextureImporterType.NormalMap;
                texImportador.SaveAndReimport();
            }
        }

        private static Material CrearMaterial(Prototesis p)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(p.RutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, p.RutaMaterial);
            }
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>(p.Carpeta + "Texturas/" + p.Codigo + "_basecolor.jpg");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(p.Carpeta + "Texturas/" + p.Codigo + "_normal.jpg");
            material.SetTexture("_BaseMap", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", MetalicoMaterial);
            material.SetFloat("_Smoothness", SuavidadMaterial);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Límites de las mallas de <paramref name="objeto"/> en el espacio local de <paramref name="marco"/>.</summary>
        public static Bounds LimitesEn(GameObject objeto, Transform marco)
        {
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null) { continue; }
                Matrix4x4 aMarco = marco.worldToLocalMatrix * f.transform.localToWorldMatrix;
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    Vector3 q = aMarco.MultiplyPoint3x4(v);
                    if (primero) { b = new Bounds(q, Vector3.zero); primero = false; }
                    else { b.Encapsulate(q); }
                }
            }
            return b;
        }

        private static GameObject ArmarPrefab(Prototesis p, Material material)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(p.RutaFbx);
            if (fbx == null)
            {
                Informe.AppendLine("sin fbx " + p.RutaFbx);
                return null;
            }
            var raiz = new GameObject(p.Codigo + "_" + p.Nombre);
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            modelo.name = NombreModelo;
            modelo.transform.SetParent(raiz.transform, false);
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var punta = new GameObject(NombrePunta);
            punta.transform.SetParent(raiz.transform, false);
            Calibrar(p, modelo.transform, punta.transform);
            GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, p.RutaPrefab);
            Object.DestroyImmediate(raiz);
            return guardado;
        }

        /// <summary>
        /// Pone <paramref name="modelo"/> y <paramref name="punta"/> (hijos de una raíz que irá pegada al hueso con pose identidad) en su lugar sobre el cuerpo de PJ01.
        /// Se carga el jugador, se pide el hueso del Animator Humanoid y la pose se calcula en el cuerpo y se pasa al espacio del hueso (con su escala):
        /// - Antebrazo: el eje del modelo que va hacia la mano (EjeALaPunta) sigue el hueso hacia la mano y EjeAfuera mira hacia afuera del cuerpo.
        /// - Hombro: el modelo queda derecho en el cuerpo (su +X a la derecha del cuerpo, su +Z adelante), sobre el punto del hueso HuesoHacia.
        /// </summary>
        private static void Calibrar(Prototesis p, Transform modelo, Transform punta)
        {
            GameObject jugador = PrefabUtility.LoadPrefabContents(RutaJugador);
            try
            {
                Animator animador = jugador.GetComponentInChildren<Animator>(true);
                Transform hueso = animador.GetBoneTransform(p.Hueso);
                Transform hacia = animador.GetBoneTransform(p.HuesoHacia);
                Transform cuerpo = animador.transform;
                Bounds b = LimitesEn(modelo.gameObject, modelo);
                float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                float escala = p.Largo / (mayor * hueso.lossyScale.x);
                Quaternion giro;
                Vector3 centro;
                Vector3 puntaModelo;
                if (p.Modo == ModoColocacion.Antebrazo)
                {
                    Vector3 alaMano = (hacia.position - hueso.position).normalized;
                    float lado = Mathf.Sign(Vector3.Dot(hueso.position - cuerpo.position, cuerpo.right));
                    Vector3 afuera = Vector3.ProjectOnPlane(cuerpo.right * lado, alaMano).normalized;
                    Quaternion enMundo = Quaternion.LookRotation(alaMano, afuera) * Quaternion.Inverse(Quaternion.LookRotation(p.EjeALaPunta, p.EjeAfuera));
                    giro = Quaternion.Inverse(hueso.rotation) * enMundo;
                    centro = Vector3.Lerp(hueso.position, hacia.position, 0.5f);
                    puntaModelo = b.center + Vector3.Scale(p.EjeALaPunta, b.size) * 0.5f;
                }
                else
                {
                    giro = Quaternion.Inverse(hueso.rotation) * cuerpo.rotation * Quaternion.Euler(0f, p.GiroHombro, 0f);
                    centro = hacia.position;
                    puntaModelo = b.center + Vector3.up * (b.size.y * 0.5f);
                }
                centro += cuerpo.TransformVector(p.Ajuste);
                modelo.localScale = Vector3.one * escala;
                modelo.localRotation = giro;
                modelo.localPosition = Vector3.zero;
                Vector3 centroModelo = modelo.TransformPoint(b.center);
                modelo.localPosition = hueso.InverseTransformPoint(centro) - centroModelo;
                punta.localPosition = modelo.TransformPoint(puntaModelo);
                Informe.AppendLine(p.Codigo + ": tamaño " + b.size.ToString("F3") + " escala " + escala.ToString("F3") + " hueso " + p.Hueso
                    + " modelo en el hueso " + modelo.localPosition.ToString("F3") + " punta " + punta.localPosition.ToString("F3"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(jugador);
            }
        }

        private static void Asignar(Prototesis p, GameObject prefab)
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoProtesisAtaque>(RutaCatalogo);
            if (catalogo == null || prefab == null) { return; }
            DatosProtesisAtaque datos = catalogo.De(p.Habilidad);
            if (datos == null) { Informe.AppendLine("el catálogo no tiene " + p.Habilidad); return; }
            var so = new SerializedObject(datos);
            so.FindProperty("modelo").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            Informe.AppendLine("asignado " + prefab.name + " a " + datos.name);
        }
    }
}
