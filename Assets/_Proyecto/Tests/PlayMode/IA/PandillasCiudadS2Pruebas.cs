using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// S2 pandillas (14-nov): en Ciudad_Prologo hay 5 grupos de 3 a 5 SKULLS con su civil, cada uno sobre suelo, y se toma una foto de cada uno
    /// acosando a unos 6 m en docs/capturas/s2-14nov/pandilla-n.png (la cámara renderiza sola al final del frame; WaitForEndOfFrame no corre en batchmode).
    /// </summary>
    public class PandillasCiudadS2Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string CarpetaFotos = "docs/capturas/s2-14nov";
        private const int GruposEsperados = 5;
        private const float DistanciaFoto = 6f;
        private const float AlturaFoto = 2.2f;
        private const float AlturaMira = 1.1f;
        private const float AlturaOjos = 1.5f;
        private const float TolerSuelo = 0.5f;
        private const float RadioMarco = 0.3f;
        private const int Ancho = 1280;
        private const int Alto = 720;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        // La ciudad no se queda cargada para las pruebas que siguen (los grupos de acoso están activos y otras pruebas destruyen pandilleros).
        [UnityTearDown]
        public IEnumerator Descargar()
        {
            Scene ciudad = SceneManager.GetSceneByPath(RutaEscena);
            if (ciudad.IsValid() && ciudad.isLoaded)
            {
                Scene vacia = SceneManager.CreateScene("VaciaS2");
                SceneManager.SetActiveScene(vacia);
                yield return SceneManager.UnloadSceneAsync(ciudad);
            }
        }

        [UnityTest]
        public IEnumerator Ciudad_TieneCincoGruposDeTresACincoSkullsConCivilSobreSuelo()
        {
            GrupoAcoso[] grupos = Object.FindObjectsByType<GrupoAcoso>(FindObjectsSortMode.None).OrderBy(g => g.name).ToArray();
            Assert.AreEqual(GruposEsperados, grupos.Length, "Los grupos de S2_PandillasAcoso");
            Assert.AreEqual(new[] { 3, 3, 4, 4, 5 }, grupos.Select(g => g.GetComponent<PandillaAPie>().Pandilleros.Length).OrderBy(n => n).ToArray(), "Tamaños 3, 3, 4, 4, 5");
            foreach (GrupoAcoso g in grupos)
            {
                PandillaAPie pandilla = g.GetComponent<PandillaAPie>();
                Assert.IsNotNull(g.Civil, g.name + " debe traer su civil");
                Assert.AreEqual(pandilla.Pandilleros.Length, pandilla.Presentes, g.name + ": salen todos (de día y de noche)");
                Assert.IsTrue(g.Civil.gameObject.activeInHierarchy, g.name + ": civil activo");
                foreach (Transform t in pandilla.Pandilleros.Select(p => p.transform).Concat(new[] { g.Civil.transform }))
                {
                    Assert.IsTrue(Physics.Raycast(t.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit h, 2f, ~0, QueryTriggerInteraction.Ignore),
                        g.name + "/" + t.name + " sin suelo debajo");
                    Assert.LessOrEqual(Mathf.Abs(h.point.y - t.position.y), TolerSuelo, g.name + "/" + t.name + " flota o está hundido");
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_CadaGrupoAcosaYSeFotografiaA6Metros()
        {
            Directory.CreateDirectory(CarpetaFotos);
            GrupoAcoso[] grupos = Object.FindObjectsByType<GrupoAcoso>(FindObjectsSortMode.None).OrderBy(g => g.name).ToArray();
            Assert.AreEqual(GruposEsperados, grupos.Length);
            // Dejamos pasar unos segundos para que cada grupo haga sus gestos.
            for (int i = 0; i < grupos.Length; i++)
            {
                GrupoAcoso g = grupos[i];
                Assert.AreEqual(ReglaAcoso.Fase.Acosando, g.Fase, g.name + " debe estar acosando");
                Vector3 civil = g.Civil.transform.position;
                Vector3 mira = civil + Vector3.up * AlturaMira;
                Vector3 camara = ElegirCamara(g, mira);
                GameObject go = new GameObject("CamPandilla");
                Camera cam = go.AddComponent<Camera>();
                cam.fieldOfView = 55f;
                cam.nearClipPlane = 0.05f;
                cam.depth = 100f;
                go.transform.position = camara;
                go.transform.LookAt(mira);
                RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
                cam.targetTexture = rt;
                yield return null;
                yield return null;
                cam.enabled = false;
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(CarpetaFotos + "/pandilla-" + (i + 1) + ".png", tex.EncodeToPNG());
                cam.targetTexture = null;
                Object.Destroy(tex);
                Object.Destroy(rt);
                Object.Destroy(go);
            }
        }

        // La cámara a ~6 m del civil, mirándolo, en el ángulo que no se tapa con paredes (prefiere desde donde se ven los pandilleros de frente).
        private static Vector3 ElegirCamara(GrupoAcoso g, Vector3 mira)
        {
            Vector3 civil = g.Civil.transform.position;
            Vector3 mejor = civil + Vector3.up * AlturaFoto;
            float mejorLibre = -1f;
            foreach (float distancia in new[] { DistanciaFoto, 5f, 4f, 3f })
            {
                for (float a = 0f; a < 360f; a += 30f)
                {
                    Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                    Vector3 pos = civil + dir * distancia + Vector3.up * AlturaFoto;
                    if (Physics.CheckSphere(pos, RadioMarco, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    Vector3 hacia = mira - pos;
                    bool tapado = Physics.Raycast(pos, hacia.normalized, out RaycastHit h, hacia.magnitude, ~0, QueryTriggerInteraction.Ignore)
                        && !h.transform.IsChildOf(g.transform);
                    if (tapado)
                    {
                        continue;
                    }
                    // Que no esté justo detrás de un pandillero (ver más caras que espaldas).
                    float libre = g.GetComponent<PandillaAPie>().Pandilleros.Min(p => Vector3.Distance(p.transform.position, pos));
                    if (distancia >= DistanciaFoto - 0.01f && libre > mejorLibre)
                    {
                        mejorLibre = libre;
                        mejor = pos;
                    }
                    else if (mejorLibre < 0f)
                    {
                        return pos;
                    }
                }
                if (mejorLibre >= 0f)
                {
                    return mejor;
                }
            }
            return mejor;
        }
    }
}
