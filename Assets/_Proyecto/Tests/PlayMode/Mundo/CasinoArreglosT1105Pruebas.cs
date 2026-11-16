using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1105: arreglos del casino (playtest de Tony). La cajera mira al salón, las sillas de las mesas miran a su mesa, SERGIO está sentado en su silla
    /// frente a su pantalla y la pared sobre el arco de la entrada se ve con el muro del casino por dentro. Con T1105_CAPTURAS=1 escribe docs/capturas/t-1105/.
    /// </summary>
    public class CasinoArreglosT1105Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RaizCasino = "T930_Casino";
        private const string RaizMesas = "S2o_Casino";
        private const float MaxGradosSilla = 20f;
        private const float MaxGradosHacker = 25f;
        private const float MaxMetrosSilla = 0.6f;
        private const float MinDotCajera = 0.9f;
        private const int FramesEspera = 90;
        private const string PrefijoMuro = "T930_Muro_";
        private const string MuroPanel = "T1105_MuroSobreArco";
        private const float EsperaSentado = 2f;
        private const float AsientoModelo = 0.62f;

        private static bool Capturas => System.Environment.GetEnvironmentVariable("T1105_CAPTURAS") == "1";

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < FramesEspera; i++)
            {
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            Scene vacia = SceneManager.CreateScene("CasinoArreglosT1105_Vacia");
            SceneManager.SetActiveScene(vacia);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path.Contains("Ciudad"))
                {
                    yield return SceneManager.UnloadSceneAsync(s);
                }
            }
        }

        /// <summary>Hacia dónde mira el cuerpo animado: de muslo izquierdo a derecho, girado 90° hacia arriba (como MeseraDeFrenteS2Pruebas).</summary>
        private static Vector3 Frente(Animator a)
        {
            Vector3 f = Vector3.Cross(a.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position, Vector3.up);
            f.y = 0f;
            return f.normalized;
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static Transform Hijo(string raiz, string nombre)
        {
            GameObject g = GameObject.Find(raiz);
            Assert.IsNotNull(g, "falta " + raiz);
            Transform t = g.transform.Cast<Transform>().FirstOrDefault(x => x.name == nombre);
            Assert.IsNotNull(t, "falta " + nombre + " en " + raiz);
            return t;
        }

        private static Animator AnimadorHacker()
        {
            GameObject sergio = GameObject.Find("Apoyo_SERGIO");
            Assert.IsNotNull(sergio, "falta Apoyo_SERGIO");
            return sergio.GetComponentInChildren<Animator>(true);
        }

        [UnityTest]
        public IEnumerator Cajera_MiraAlSalon()
        {
            Animator a = Hijo(RaizCasino, "Cajera_R01").GetComponent<Animator>();
            Vector3 frente = Frente(a);
            Assert.Greater(Vector3.Dot(frente, Vector3.back), MinDotCajera, "la cajera debe mirar al salón (-z), mira a " + frente.ToString("F2"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Sillas_CadaUnaMiraASuMesa()
        {
            GameObject raiz = GameObject.Find(RaizMesas);
            Assert.IsNotNull(raiz);
            Transform[] mesas = raiz.transform.Cast<Transform>().Where(t => t.name == "I11").ToArray();
            Transform[] sillas = raiz.transform.Cast<Transform>().Where(t => t.name == "I12").ToArray();
            Assert.AreEqual(4, mesas.Length, "cuatro mesas");
            Assert.AreEqual(12, sillas.Length, "tres sillas por mesa");
            List<string> fallas = new List<string>();
            foreach (Transform silla in sillas)
            {
                Transform mesa = mesas.OrderBy(m => Plano(m.position - silla.position).sqrMagnitude).First();
                // El frente del modelo I12 es -Z local (el respaldo está en +Z).
                float grados = Vector3.Angle(Plano(-silla.forward), Plano(mesa.position - silla.position));
                if (grados > MaxGradosSilla)
                {
                    fallas.Add("silla en " + silla.position.ToString("F2") + " mira a " + grados.ToString("F0") + "° de su mesa");
                }
            }
            Assert.IsEmpty(fallas, string.Join("\n", fallas));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hacker_SentadoEnSuSillaMirandoSuPantalla()
        {
            // Tras 2 s de Play: la pose de sentado ya se aplicó encima del Animator.
            float fin = Time.time + EsperaSentado;
            while (Time.time < fin)
            {
                yield return null;
            }
            Animator a = AnimadorHacker();
            Transform silla = Hijo(RaizCasino, "I12");
            Transform escritorio = Hijo(RaizCasino, "I02");
            Transform cadera = a.GetBoneTransform(HumanBodyBones.Hips);
            float metros = Plano(cadera.position - silla.position).magnitude;
            Assert.Less(metros, MaxMetrosSilla, "la cadera de SERGIO está a " + metros.ToString("F2") + " m de su silla");
            // El asiento I12 está a 0.62 m de su base, con la escala de la silla.
            float asiento = silla.position.y + AsientoModelo * silla.lossyScale.y;
            Assert.That(cadera.position.y, Is.InRange(asiento + 0.05f, asiento + 0.25f), "la cadera (" + cadera.position.y.ToString("F2") + ") está sobre el asiento (" + asiento.ToString("F2") + ")");
            // Muslos horizontales (rodilla a la altura de la cadera, a menos de 0.15 m) y pies cerca del piso.
            Transform rodilla = a.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Assert.Less(Mathf.Abs(rodilla.position.y - a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position.y), 0.15f, "muslos horizontales");
            float piso = silla.position.y;
            Assert.Less(a.GetBoneTransform(HumanBodyBones.LeftFoot).position.y - piso, 0.25f, "los pies están cerca del piso");
            // La silla de frente al escritorio: su frente es -Z local y el respaldo queda detrás de la espalda.
            Assert.Greater(Vector3.Dot(Plano(-silla.forward), Plano(escritorio.position - silla.position).normalized), 0.9f, "el frente de la silla apunta al escritorio");
            Assert.Greater(Vector3.Dot(Plano(silla.forward), Plano(silla.position - escritorio.position).normalized), 0.9f, "el respaldo queda del lado contrario al escritorio");
            // El frente de SERGIO es el de su Visual (el que usa PoseSentado para poner las piernas): la malla de Tripo mira hacia ahí.
            Transform visual = GameObject.Find("Apoyo_SERGIO").transform.Find("Visual");
            Vector3 hacia = Plano(escritorio.position - cadera.position);
            float grados = Vector3.Angle(Plano(visual.forward), hacia);
            Assert.Less(grados, MaxGradosHacker, "SERGIO mira a " + grados.ToString("F0") + "° de su pantalla (frente " + Plano(visual.forward).ToString("F2") + ")");
            Assert.Less(Plano(escritorio.position - silla.position).magnitude, 2f, "la silla está junto al escritorio");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Entrada_DesdeDentroSoloSeVeElMuroDelCasino()
        {
            Vector3 desde = new Vector3(72f, 1.7f, 47f);
            List<string> fallas = new List<string>();
            foreach (float y in new[] { 3.6f, 4.2f, 5.0f, 5.5f })
            {
                foreach (float z in new[] { 45.7f, 47f, 48.3f })
                {
                    Vector3 objetivo = new Vector3(60.5f, y, z);
                    RaycastHit[] golpes = Physics.RaycastAll(desde, (objetivo - desde).normalized, 30f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
                    Renderer primero = golpes.Select(h => h.collider.GetComponent<Renderer>()).FirstOrDefault(r => r != null);
                    string material = primero == null ? "nada" : primero.sharedMaterial.name;
                    if (primero == null || !(material.StartsWith(PrefijoMuro) || material == MuroPanel))
                    {
                        fallas.Add("hacia (" + y + ", " + z + ") se ve " + (primero == null ? "nada" : primero.name + " con " + material));
                    }
                }
            }
            Assert.IsEmpty(fallas, string.Join("\n", fallas));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Entrada_ElPanelQuedaTapadoPorLaFachadaDesdeLaCalle()
        {
            Transform panel = Hijo(RaizCasino, MuroPanel);
            Bounds b = panel.GetComponent<Renderer>().bounds;
            Renderer cuerpo = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).First(r => r.name == "CuerpoTecho" && r.bounds.Intersects(b));
            Assert.LessOrEqual(cuerpo.bounds.min.z, b.min.z + 0.01f, "el cuerpo de la fachada cubre el panel a lo ancho");
            Assert.GreaterOrEqual(cuerpo.bounds.max.z, b.max.z - 0.01f);
            Renderer hoja = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).First(r => r.GetComponent<MechaCyberpunk.Mundo.Lugares.HojaPuerta>() != null && Mathf.Abs(r.bounds.center.z - b.center.z) < 3f && Mathf.Abs(r.bounds.center.x - b.center.x) < 6f);
            Assert.GreaterOrEqual(b.min.y, hoja.bounds.max.y - 0.01f, "el panel empieza donde acaba la hoja de la puerta: no tapa el paso");
            Assert.GreaterOrEqual(cuerpo.bounds.max.y, b.max.y - 0.01f);
            Assert.Less(cuerpo.bounds.min.x, b.min.x, "y queda por fuera (hacia la calle) del panel");
            yield return null;
        }

        // ------------------------------------------------------------------ capturas

        [UnityTest]
        public IEnumerator Capturas_Casino()
        {
            if (!Capturas)
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1105/ con T1105_CAPTURAS=1");
            }
            System.IO.Directory.CreateDirectory("docs/capturas/t-1105");
            Transform cajera = Hijo(RaizCasino, "Cajera_R01");
            yield return Foto(new Vector3(72f, 1.7f, 53f), cajera.position + Vector3.up * 1.2f, "cajera-desde-el-salon", 55f);
            Transform sergio = GameObject.Find("Apoyo_SERGIO").transform;
            Vector3 c = sergio.position + Vector3.up * 0.9f;
            yield return Foto(c + new Vector3(0.5f, 0.6f, 2.4f), c, "sergio-de-lado", 55f);
            yield return Foto(c + new Vector3(3.2f, 0.7f, 0f), c, "sergio-por-detras", 55f);
            yield return Foto(c + new Vector3(2.2f, 0.9f, -2.4f), c, "sergio-tres-cuartos", 55f);
            yield return Foto(new Vector3(74f, 5.2f, 47f), new Vector3(74f, 0.5f, 47f), "mesas-desde-arriba", 95f);
            yield return Foto(new Vector3(70f, 2.4f, 47f), new Vector3(60.8f, 4.0f, 47f), "entrada-desde-dentro", 62f);
            yield return Foto(new Vector3(76f, 5.5f, 36f), new Vector3(63f, 1f, 40f), "esquina-de-sergio", 70f);
            yield return null;
        }

        /// <summary>La cámara renderiza sola al final del frame, después de las poses de LateUpdate (sentado, charola); la textura se lee dos frames después.</summary>
        private static IEnumerator Foto(Vector3 pos, Vector3 mira, string nombre, float fov)
        {
            GameObject go = new GameObject("CamT1105");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            yield return null;
            yield return null;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("docs/capturas/t-1105/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}
