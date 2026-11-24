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
    /// T-cinematicas-narracion-24nov: con la narración (cuadro congelado y 1 s de pausa) y el botón OMITIR.
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
        public void Datos_ClipsEnOrden_C01aC05ConVideoYSinPartePendiente()
        {
            DatosCinematica c01 = Cargar("DatosCinematica_C01_EL_RECUERDO");
            DatosCinematica c02 = Cargar("DatosCinematica_C02_EL_MALETIN");
            DatosCinematica c03 = Cargar("DatosCinematica_C03_LA_DECISION");
            DatosCinematica c04 = Cargar("DatosCinematica_C04_EL_SUERO");
            DatosCinematica c05 = Cargar("DatosCinematica_C05_EL_INVENTOR");
            Assert.AreEqual(2, c01.Clips.Count, "C01: videos 02–03");
            Assert.AreEqual(12, c02.Clips.Count, "C02: videos 04–15");
            StringAssert.StartsWith("04_C02_Maletin", c02.Clips[0].name);
            StringAssert.StartsWith("15_", c02.Clips[11].name);
            Assert.AreEqual(6, c03.Clips.Count, "C03: videos 16–21 (G01–G06, ya con C07)");
            StringAssert.StartsWith("21_C07", c03.Clips[5].name);
            Assert.AreEqual(0f, c03.SegundosPendientes, "C03 ya no lleva placeholder de C07");
            Assert.AreEqual(3, c04.Clips.Count, "C04 EL SUERO: videos 22–24");
            Assert.AreEqual(0f, c04.SegundosPendientes);
            Assert.AreEqual(3, c05.Clips.Count, "Tino (primera mejora): videos 25–27");
            StringAssert.StartsWith("25_", c05.Clips[0].name);
        }

        /// <summary>T-cinematicas-narracion-24nov: cada clip de C02 y C03 tiene su par video + narración (la clave NAR_ está en el nombre del video).</summary>
        [Test]
        public void Datos_CadaClipConSuNarracion_C02C03ConVozYLasDemasSinVoz()
        {
            foreach (string archivo in new[] { "DatosCinematica_C02_EL_MALETIN", "DatosCinematica_C03_LA_DECISION" })
            {
                DatosCinematica d = Cargar(archivo);
                for (int i = 0; i < d.Clips.Count; i++)
                {
                    AudioClip voz = d.Narracion(i);
                    Assert.IsNotNull(voz, archivo + ": el clip " + d.Clips[i].name + " tiene narración");
                    StringAssert.StartsWith("NAR_", voz.name);
                    string clave = voz.name.Substring(4);
                    string resto = d.Clips[i].name.Substring(d.Clips[i].name.IndexOf('_') + 1);
                    Assert.IsTrue(resto == clave || resto.StartsWith(clave + "_"), d.Clips[i].name + " va con " + voz.name);
                    Assert.AreEqual(1f, d.Pausa(i), 0.001f, "1 s de pausa tras la frase");
                    Assert.AreEqual(Mathf.Max((float)d.Clips[i].length, voz.length) + 1f, d.TramoClip(i), 0.05f, "Tramo = máx(video, voz) + pausa");
                }
            }
            foreach (string archivo in new[] { "DatosCinematica_C01_EL_RECUERDO", "DatosCinematica_C04_EL_SUERO", "DatosCinematica_C05_EL_INVENTOR" })
            {
                DatosCinematica d = Cargar(archivo);
                for (int i = 0; i < d.Clips.Count; i++)
                {
                    Assert.IsNull(d.Narracion(i), archivo + ": sin narración");
                    Assert.AreEqual(0f, d.Pausa(i), "Sin narración no hay pausa: se une sin negro");
                }
                Assert.AreEqual(d.DuracionVideos, d.DuracionEfectiva, 0.01f);
            }
            DatosCinematica c02 = Cargar("DatosCinematica_C02_EL_MALETIN");
            DatosCinematica c03 = Cargar("DatosCinematica_C03_LA_DECISION");
            Debug.Log("[Prueba narración] C02 " + c02.DuracionEfectiva.ToString("0.0") + " s, C03 " + c03.DuracionEfectiva.ToString("0.0") + " s con la regla");
            Assert.AreEqual(61.2f, c02.DuracionEfectiva, 1.5f, "C02 con la regla (tabla de la tarea)");
            Assert.AreEqual(28.9f, c03.DuracionEfectiva, 1.5f, "C03 con la regla (tabla de la tarea)");
        }

        /// <summary>
        /// T-omitir-cinematicas-24nov: el botón OMITIR sale abajo a la derecha, pulsarlo salta toda la cinemática (con la narración sonando),
        /// la voz se apaga con fundido y se llaman enNegro y alTerminar (el juego queda en el estado de después).
        /// </summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator Omitir_ConNarracion_SaltaTodoYLlamaLosAvisos()
        {
            DatosCinematica c03 = Cargar("DatosCinematica_C03_LA_DECISION");
            Assert.IsNotNull(c03);
            bool enNegro = false;
            bool termino = false;
            CinematicaPendiente.Reproducir(c03, () => enNegro = true, () => termino = true);
            float limite = Time.unscaledTime + 10f;
            while (Time.unscaledTime < limite && !CinematicaPendiente.NarracionSonando)
            {
                yield return null;
            }
            Assert.IsTrue(CinematicaPendiente.NarracionSonando, "La frase del primer clip suena con su video");
            Assert.IsTrue(MechaCyberpunk.Core.DuckingNarracion.Activo, "Mientras suena la frase se avisa a la música");
            RectTransform boton = CinematicaPendiente.BotonOmitir;
            Assert.IsNotNull(boton, "Hay botón OMITIR");
            Assert.AreEqual(new Vector2(1f, 0f), boton.anchorMin, "Abajo a la derecha");
            MechaCyberpunk.UI.Kit.BotonCyberfix b = boton.GetComponent<MechaCyberpunk.UI.Kit.BotonCyberfix>();
            Assert.IsNotNull(b);
            StringAssert.Contains(CinematicaPendiente.TextoOmitir, b.Etiqueta);
            AudioSource voz = CinematicaPendiente.FuenteNarracion;
            b.AlPulsar.Invoke();
            limite = Time.unscaledTime + 3f;
            while (!termino && Time.unscaledTime < limite)
            {
                yield return null;
            }
            Assert.IsTrue(enNegro, "Se llamó enNegro");
            Assert.IsTrue(termino, "Saltó toda la cinemática (no solo el clip)");
            Assert.IsFalse(CinematicaPendiente.EnCurso);
            Assert.IsFalse(MechaCyberpunk.Core.DuckingNarracion.Activo, "La música vuelve");
            Assert.IsTrue(voz == null || !voz.isPlaying, "La voz ya no suena");
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
            Assert.Greater(total, c02.DuracionConNarracion - 1f, "Nada se aceleró: cada frase completa y su pausa");
        }
    }
}
