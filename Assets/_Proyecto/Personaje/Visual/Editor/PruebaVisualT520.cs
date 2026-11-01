using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>Medición de T-520: velocidad real de Caminar y Correr con movimiento de raíz, y una foto de cada visual sobre un piso.</summary>
    public static class PruebaVisualT520
    {
        public static void Medir()
        {
            var sb = new StringBuilder();
            CapturaPersonajes.EscenaDePrueba();
            foreach (string n in CreadorVisualTripo.Nombres)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(n));
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Animator a = inst.GetComponentInChildren<Animator>();
                a.applyRootMotion = true;
                foreach (string estado in new[] { "Caminar", "Correr" })
                {
                    a.SetFloat("Velocidad", estado == "Caminar" ? 1.2f : 8f);
                    a.Play(estado, 0, 0f);
                    a.Update(0f);
                    Vector3 inicio = a.transform.position;
                    float t = 0f;
                    while (t < 2f) { a.Update(1f / 60f); t += 1f / 60f; }
                    Vector3 d = a.transform.position - inicio;
                    sb.AppendLine(n + " " + estado + " velocidad real " + (new Vector3(d.x, 0, d.z).magnitude / t).ToString("0.00") + " m/s");
                }
                a.applyRootMotion = false;
                a.transform.position = Vector3.zero;
                a.Play("Idle", 0, 0f);
                a.Update(0.1f);
                SkinnedMeshRenderer smr = inst.GetComponentInChildren<SkinnedMeshRenderer>();
                sb.AppendLine(n + " idle bounds " + smr.bounds.min.y.ToString("0.00") + " a " + smr.bounds.max.y.ToString("0.00") + " escala " + smr.transform.lossyScale);
                Object.DestroyImmediate(inst);
            }
            System.IO.File.WriteAllText("Logs/medir520.txt", sb.ToString());
        }

        public static void Fotos()
        {
            CapturaPersonajes.EscenaDePrueba();
            int i = 0;
            foreach (string n in CreadorVisualTripo.Nombres)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(n));
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.position = new Vector3(i * 2.2f, 0f, 0f);
                inst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                Animator a = inst.GetComponentInChildren<Animator>();
                a.Play("Idle", 0, 0f);
                a.Update(0.05f);
                i++;
            }
            Camera c = CapturaPersonajes.ColocarCamara(new Vector3(2.2f, 0.9f, 0f), new Vector3(0f, 0.2f, -1f), 6.5f, 0.9f, 40f);
            CapturaPersonajes.Guardar(c, "Logs/fotos520.png");
        }
    
        public static void HojaAcuerdo()
        {
            CapturaPersonajes.EscenaDePrueba();
            int n = 0;
            foreach (string nombre in CreadorVisualTripo.Nombres)
            {
                AnimationClip clip = null;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(CreadorVisualTripo.RutaModelo(nombre)))
                {
                    if (o is AnimationClip c && c.name == "AcuerdoCompleto") { clip = c; }
                }
                for (int k = 0; k < 4; k++)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(nombre));
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    inst.transform.position = new Vector3(k * 1.6f, 0f, n * 1.8f);
                    Animator a = inst.GetComponentInChildren<Animator>();
                    a.enabled = false;
                    clip.SampleAnimation(a.gameObject, k * 1.2f);
                }
                n++;
            }
            Camera c2 = CapturaPersonajes.ColocarCamara(new Vector3(2.4f, 0.9f, 1.8f), new Vector3(0f, 0.6f, -1f), 9f, 2.2f, 45f);
            CapturaPersonajes.Guardar(c2, "Logs/hoja520.png");
        }
    }
}
