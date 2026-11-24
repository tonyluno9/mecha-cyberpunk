using System.Collections;
using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-cinematicas-videos-23nov: los DatosCinematica traen sus clips en orden y C02 EL MALETÍN se ve de principio a fin, clip tras clip,
    /// sin cuadros sin imagen entre clips. Mide la duración real y la deja en el log ("[Prueba C02]").
    /// </summary>
    public class VideosCinematicas23novPruebas
    {
        private const string Carpeta = "Assets/_Proyecto/Progresion/Historia/Cinematicas/";

        private static DatosCinematica Cargar(string archivo)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<DatosCinematica>(Carpeta + archivo + ".asset");
#else
            return null;
#endif
        }

        [Test]
        public void Datos_ClipsEnOrden_C01C02C03ConVideoYC04Pendiente()
        {
            DatosCinematica c01 = Cargar("DatosCinematica_C01_EL_RECUERDO");
            DatosCinematica c02 = Cargar("DatosCinematica_C02_EL_MALETIN");
            DatosCinematica c03 = Cargar("DatosCinematica_C03_LA_DECISION");
            DatosCinematica c04 = Cargar("DatosCinematica_C04_EL_SUERO");
            Assert.AreEqual(2, c01.Clips.Count, "C01: T01 + T02");
            Assert.AreEqual(12, c02.Clips.Count, "C02: C02 + C03 + F08–F11 + F12–F15");
            StringAssert.Contains("C02_Maletin", c02.Clips[0].name);
            StringAssert.Contains("F15", c02.Clips[11].name);
            Assert.AreEqual(3, c03.Clips.Count, "C03: G01–G03");
            Assert.Greater(c03.SegundosPendientes, 0f, "C03 lleva la parte pendiente de C07");
            Assert.AreEqual(0, c04.Clips.Count, "C04 sigue con placeholder");
            Assert.AreEqual(c02.DuracionVideos, c02.DuracionEfectiva, 0.01f);
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator C02_SeVeCompleto_SinHuecosEntreClips()
        {
            DatosCinematica c02 = Cargar("DatosCinematica_C02_EL_MALETIN");
            Assert.IsNotNull(c02);
            bool termino = false;
            float inicio = Time.unscaledTime;
            CinematicaPendiente.Reproducir(c02, null, () => termino = true);
            int ultimoClip = -1;
            int huecos = 0;
            float inicioClip = 0f;
            float limite = inicio + c02.DuracionEfectiva + 30f;
            string tiempos = "";
            while (!termino && Time.unscaledTime < limite)
            {
                yield return null;
                ReglaCinematica regla = CinematicaPendiente.ReglaActual;
                if (regla == null || regla.Actual != ReglaCinematica.Fase.Contenido)
                {
                    continue;
                }
                int clip = CinematicaPendiente.ClipActual;
                if (clip != ultimoClip)
                {
                    if (ultimoClip >= 0)
                    {
                        tiempos += ultimoClip + ":" + (Time.unscaledTime - inicioClip).ToString("0.0") + "s ";
                    }
                    ultimoClip = clip;
                    inicioClip = Time.unscaledTime;
                }
                GameObject v = GameObject.Find("CanvasCinematica/Video");
                RawImage img = v != null ? v.GetComponent<RawImage>() : null;
                if (img == null || !img.enabled || img.texture == null)
                {
                    huecos++;
                }
            }
            tiempos += ultimoClip + ":" + (Time.unscaledTime - inicioClip).ToString("0.0") + "s";
            float total = Time.unscaledTime - inicio;
            Debug.Log("[Prueba C02] duración total " + total.ToString("0.0") + " s (videos " + c02.DuracionVideos.ToString("0.0")
                + " s), cuadros sin imagen en el contenido: " + huecos + ", por clip: " + tiempos);
            Assert.IsTrue(termino, "La cinemática terminó");
            Assert.AreEqual(11, ultimoClip, "Llegó al último clip");
            Assert.LessOrEqual(huecos, 1, "Sin negro entre clips");
            Assert.Less(total, c02.DuracionEfectiva + 6f, "No se trabó");
        }
    }
}
