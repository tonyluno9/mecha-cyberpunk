using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>T-1602: capturas de comprobación. Pone un "NPC" junto a un poste real de la escena y dispara con ReglaImpactoBala.BuscarPrimero.</summary>
    public static class CapturasHitboxesT1602
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "docs/capturas/_tareas/T4";
        private const float Distancia = 9f;
        private const float AlturaPecho = 1.3f;

        public static void Capturar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Type regla = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                .First(t => t.FullName == "MechaCyberpunk.Personaje.Combate.ReglaImpactoBala");
            MethodInfo buscar = regla.GetMethods().First(m => m.Name == "BuscarPrimero" && m.GetParameters().Length == 6);

            StringBuilder sb = new StringBuilder();
            Transform poste = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name == "Poste_Luz" && t.GetComponent<CapsuleCollider>() != null)
                .OrderBy(t => t.position.x).First();
            CapsuleCollider cap = poste.GetComponent<CapsuleCollider>();
            sb.AppendLine("Poste: " + poste.name + " en " + poste.position + " radio=" + cap.radius + " altura=" + cap.height + " capa=" + poste.gameObject.layer);

            Vector3 hacia = new Vector3(1f, 0f, 0f);
            Vector3 npcPos = poste.position + hacia * (cap.radius + 0.45f + 0.05f);
            GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NPC_Prueba";
            npc.transform.position = npcPos + Vector3.up * 0.9f;
            npc.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            npc.GetComponent<Renderer>().sharedMaterial = Material(Color.red);
            Physics.SyncTransforms();

            Vector3 objetivo = npcPos + Vector3.up * AlturaPecho;
            Vector3 lado = Vector3.Cross(Vector3.up, hacia).normalized;
            Vector3 origenA = objetivo + (hacia + lado * 0.6f).normalized * Distancia + Vector3.up * 0.2f;
            Disparo(buscar, origenA, objetivo, npc.transform, "A_npc_pegado_al_poste", sb, out bool pegaA);

            // B: una pared en medio.
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.name = "Pared_Prueba";
            Vector3 dir = (objetivo - origenA).normalized;
            pared.transform.position = origenA + dir * (Distancia * 0.5f);
            pared.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
            pared.transform.localScale = new Vector3(5f, 6f, 0.3f);
            pared.GetComponent<Renderer>().sharedMaterial = Material(new Color(0.4f, 0.4f, 0.45f));
            Physics.SyncTransforms();
            Disparo(buscar, origenA, objetivo, npc.transform, "B_pared_en_medio", sb, out bool pegaB);

            // C: la misma pared en la capa NoDetieneBalas (como una reja): la bala la atraviesa.
            pared.layer = LayerMask.NameToLayer("NoDetieneBalas");
            Physics.SyncTransforms();
            Disparo(buscar, origenA, objetivo, npc.transform, "C_reja_NoDetieneBalas_en_medio", sb, out bool pegaC);

            sb.AppendLine("Resultado: A pega=" + pegaA + " (esperado True), B pega=" + pegaB + " (esperado False), C pega=" + pegaC + " (esperado True)");
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Carpeta + "/resultado.txt", sb.ToString());

            pared.SetActive(false);
            Foto(origenA, objetivo, "A_npc_pegado_al_poste.png", true);
            pared.SetActive(true);
            pared.layer = 0;
            Foto(origenA, objetivo, "B_pared_en_medio.png", false);
            Debug.Log("[Mundo] T-1602 capturas: " + sb);
        }

        private static void Disparo(MethodInfo buscar, Vector3 origen, Vector3 objetivo, Transform npc, string nombre, StringBuilder sb, out bool pegaNpc)
        {
            object[] args = { origen, (objetivo - origen).normalized, 60f, null, null, default(RaycastHit) };
            bool hay = (bool)buscar.Invoke(null, args);
            RaycastHit h = (RaycastHit)args[5];
            pegaNpc = hay && h.collider.transform == npc;
            sb.AppendLine(nombre + ": hay=" + hay + " golpea=" + (hay ? h.collider.name + " (capa " + LayerMask.LayerToName(h.collider.gameObject.layer) + ") a " + h.distance.ToString("F2") + " m" : "nada"));
        }

        private static Material Material(Color c)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.color = c;
            return m;
        }

        private static void Foto(Vector3 origen, Vector3 objetivo, string archivo, bool linea)
        {
            GameObject lineaObj = null;
            if (linea)
            {
                lineaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.DestroyImmediate(lineaObj.GetComponent<Collider>());
                lineaObj.transform.position = (origen + objetivo) * 0.5f;
                lineaObj.transform.rotation = Quaternion.LookRotation(objetivo - origen);
                lineaObj.transform.localScale = new Vector3(0.04f, 0.04f, (objetivo - origen).magnitude);
                lineaObj.GetComponent<Renderer>().sharedMaterial = Material(Color.green);
            }
            GameObject camObj = new GameObject("CamCaptura");
            Camera cam = camObj.AddComponent<Camera>();
            Vector3 pos = origen + Vector3.up * 0.3f + (origen - objetivo).normalized * 1.5f;
            camObj.transform.position = pos;
            camObj.transform.rotation = Quaternion.LookRotation(objetivo - pos + Vector3.up * 0.5f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Carpeta + "/" + archivo, tex.EncodeToPNG());
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(camObj);
            if (lineaObj != null)
            {
                UnityEngine.Object.DestroyImmediate(lineaObj);
            }
        }
    }
}
