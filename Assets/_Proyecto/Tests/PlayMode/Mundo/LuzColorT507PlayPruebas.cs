using System.Collections;
using System.Linq;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-507 en juego: de noche, parado en cada zona el posproceso mezclado trae el color de esa zona y las farolas con sombra están prendidas.</summary>
    public class LuzColorT507PlayPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float HoraNoche = 23f;
        private const float AlturaOjos = 1.6f;
        private const float SaltoMaximo = 0.35f;
        private const int SombrasMinimas = 8;
        private const int SombrasMaximas = 14;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            RelojMundo reloj = Object.FindFirstObjectByType<RelojMundo>();
            reloj.Pausar(true);
            reloj.AvanzarHasta(HoraNoche);
            yield return null;
            yield return null;
            yield return null;
        }

        // Mezcla de los Volumes igual que el motor: en orden de prioridad, cada uno mueve el valor según su cercanía (blendDistance) y su peso.
        private static Color FiltroEn(Vector3 punto)
        {
            Color resultado = Color.white;
            foreach (Volume v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).OrderBy(x => x.priority))
            {
                if (v.sharedProfile == null || !v.sharedProfile.TryGet(out ColorAdjustments ca) || !ca.colorFilter.overrideState)
                {
                    continue;
                }
                float w = v.weight;
                if (!v.isGlobal)
                {
                    Collider c = v.GetComponent<Collider>();
                    float d = Vector3.Distance(punto, c.ClosestPoint(punto));
                    w *= d <= 0f ? 1f : (v.blendDistance > 0f ? Mathf.Clamp01(1f - d / v.blendDistance) : 0f);
                }
                resultado = Color.Lerp(resultado, ca.colorFilter.value, w);
            }
            return resultado;
        }

        [UnityTest]
        public IEnumerator Suburbios_DeNocheEsCalido()
        {
            yield return null;
            Color c = FiltroEn(new Vector3(-80f, AlturaOjos, 3f));
            Assert.Greater(c.r, c.b, "Suburbios cálido: " + c);
        }

        [UnityTest]
        public IEnumerator CallejonYTianguis_DeNocheSonMagenta()
        {
            yield return null;
            foreach (Vector3 p in new[] { new Vector3(-73f, AlturaOjos, -59f), new Vector3(0f, AlturaOjos, 0f) })
            {
                Color c = FiltroEn(p);
                Assert.IsTrue(c.r > c.g && c.b > c.g, "magenta en " + p + ": " + c);
            }
        }

        [UnityTest]
        public IEnumerator NeonAlto_DeNocheEsVerde()
        {
            yield return null;
            Color c = FiltroEn(new Vector3(47f, AlturaOjos, 30f));
            Assert.IsTrue(c.g > c.r && c.g > c.b, "verde: " + c);
        }

        [UnityTest]
        public IEnumerator Transicion_AlSalirDeUnaZonaElColorCambiaSinSalto()
        {
            yield return null;
            Color dentro = FiltroEn(new Vector3(-60f, AlturaOjos, 3f));
            Color borde = FiltroEn(new Vector3(-40f, AlturaOjos, 3f));
            Assert.Less(Mathf.Abs(dentro.r - borde.r), SaltoMaximo, "sin salto brusco entre " + dentro + " y " + borde);
        }

        [UnityTest]
        public IEnumerator Farolas_DeNocheEstanPrendidasYLasCercanasTienenSombra()
        {
            yield return null;
            Light[] luces = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(l => l.type == LightType.Spot && l.transform.parent != null && l.transform.parent.name.StartsWith("Farola_")).ToArray();
            Assert.GreaterOrEqual(luces.Length, 40);
            Assert.IsTrue(luces.All(l => l.enabled), "prendidas de noche");
            int conSombra = luces.Count(l => l.shadows != LightShadows.None);
            Assert.GreaterOrEqual(conSombra, SombrasMinimas);
            Assert.LessOrEqual(conSombra, SombrasMaximas);
        }
    }
}
