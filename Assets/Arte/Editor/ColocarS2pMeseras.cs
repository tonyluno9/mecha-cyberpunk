using System.IO;
using System.IO.Compression;
using System.Linq;
using MechaCyberpunk.Casino.Meseras;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-892 (B6): 3 meseras R01 caminando en el casino. El zip trae esqueleto de Tripo y se importa como Humanoid con el flujo de los demás personajes
    /// (<see cref="CreadorVisualTripo"/>), que le presta los clips de caminar e idle. Idempotente: vive en su raíz <see cref="Raiz"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2pMeseras.Todo
    /// </summary>
    public static class ColocarS2pMeseras
    {
        public const string Raiz = "S2p_Meseras";
        public const string Nombre = "MeseraRobot";
        private const string Zip = "imageassets/modelos3d/R01.zip";
        // CreadorVisualTripo reconoce la textura de color por este patrón de nombre.
        private const string NombreTextura = "tripo_image_mesera_0_0.jpg";
        private const float RadioCuerpo = 0.3f;
        private const float AlturaCuerpo = 1.7f;
        private const float SueloDelCasino = 0.2f;

        /// <summary>Escala para que mida como la gente en Play (medida con BakeMesh: docs de la memoria de altura animada).</summary>
        public const float Escala = 1f;

        /// <summary>Rondas por los pasillos entre las mesas (x, z), a salvo de las columnas de x 67 y 74.</summary>
        public static readonly Vector2[][] Rondas =
        {
            new[] { new Vector2(66f, 47f), new Vector2(73f, 47f), new Vector2(80f, 47f) },
            new[] { new Vector2(74f, 43f), new Vector2(74f, 51f) },
            new[] { new Vector2(66.5f, 45f), new Vector2(66.5f, 51f), new Vector2(72f, 54.6f) },
        };

        private static readonly float[] Esperas = { 0f, 1.3f, 2.6f };

        [MenuItem("Mecha Cyberpunk/S2p/3 Meseras R01 en el casino")]
        public static void Todo()
        {
            Importar();
            Colocar();
        }

        public static void Importar()
        {
            string carpeta = CreadorVisualTripo.Carpeta + "/" + Nombre;
            Directory.CreateDirectory(carpeta);
            using (ZipArchive z = ZipFile.OpenRead(Zip))
            {
                foreach (ZipArchiveEntry e in z.Entries)
                {
                    string n = e.Name.ToLowerInvariant();
                    string destino = n.EndsWith(".fbx") ? CreadorVisualTripo.RutaModelo(Nombre)
                        : n.Contains("basecolor") ? carpeta + "/" + NombreTextura : null;
                    if (destino != null && !(File.Exists(destino) && new FileInfo(destino).Length == e.Length))
                    {
                        e.ExtractToFile(destino, true);
                    }
                }
            }
            AssetDatabase.Refresh();
            // Crear reescribe el .meta de los clips compartidos (Rifle_Idle, de la otra sesión): solo la primera vez.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(Nombre)) == null && CreadorVisualTripo.Crear(Nombre) == null)
            {
                throw new System.InvalidOperationException("R01 no quedó como Humanoid: revisa Logs");
            }
            AssetDatabase.SaveAssets();
        }

        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(Raiz));
            Transform raiz = new GameObject(Raiz).transform;
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualTripo.RutaModelo(Nombre));
            RuntimeAnimatorController controlador = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CreadorVisualPersonaje.RutaControlador(Nombre));
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(CreadorVisualTripo.RutaModelo(Nombre)).OfType<Avatar>().First();
            Material material = CreadorVisualTripo.AsegurarMaterialBase(Nombre);
            Physics.SyncTransforms();
            for (int i = 0; i < Rondas.Length; i++)
            {
                Vector3[] puntos = Rondas[i].Select(p => new Vector3(p.x, SueloDelCasino, p.y)).ToArray();
                Revisar(i, puntos);
                GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz);
                m.name = "Mesera_" + i;
                m.transform.position = puntos[0];
                m.transform.localScale = Vector3.one * Escala;
                foreach (Renderer r in m.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
                }
                Animator a = m.GetComponent<Animator>() ?? m.AddComponent<Animator>();
                a.avatar = avatar;
                a.runtimeAnimatorController = controlador;
                a.applyRootMotion = false;
                RondaMesera ronda = m.AddComponent<RondaMesera>();
                SerializedObject so = new SerializedObject(ronda);
                SerializedProperty sp = so.FindProperty("puntos");
                sp.arraySize = puntos.Length;
                for (int k = 0; k < puntos.Length; k++)
                {
                    sp.GetArrayElementAtIndex(k).vector3Value = puntos[k];
                }
                so.FindProperty("esperaInicial").floatValue = Esperas[i];
                so.FindProperty("animador").objectReferenceValue = a;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            ColocarS2.Guardar("s2p-meseras");
            ColocarS2cLook.Foto(new Vector3(64.5f, 1.7f, 47.5f), new Vector3(76f, 1.2f, 47f), "docs/capturas/s2p-b6-casino.png", 66f);
        }

        /// <summary>Anota en el informe si un tramo de la ronda atraviesa algún collider (mesas, columnas).</summary>
        private static void Revisar(int ronda, Vector3[] puntos)
        {
            for (int k = 0; k + 1 < puntos.Length; k++)
            {
                Vector3 pie = puntos[k] + Vector3.up * (RadioCuerpo + 0.1f);
                Vector3 cabeza = puntos[k] + Vector3.up * (AlturaCuerpo - RadioCuerpo);
                Vector3 d = puntos[k + 1] - puntos[k];
                string[] hits = Physics.CapsuleCastAll(pie, cabeza, RadioCuerpo, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => h.collider.GetComponent<CharacterController>() == null)
                    .Select(h => h.collider.name + "@" + h.point.ToString("F1")).ToArray();
                ColocarS2.Informe.AppendLine("ronda " + ronda + " tramo " + k + ": " + (hits.Length == 0 ? "libre" : "CHOCA " + string.Join(", ", hits)));
            }
        }
    }
}
