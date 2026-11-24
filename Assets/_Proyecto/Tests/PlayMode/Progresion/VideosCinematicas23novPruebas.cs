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

        private static string NombreToma(DatosCinematica d, int i)
        {
            return d.EsFoto(i) ? d.Foto(i).name : d.Clips[i].name;
        }

        /// <summary>T97: C01 sigue en video (campo y despertar); C02–C05 son imágenes fijas en el orden de las tomas.</summary>
        [Test]
        public void Datos_TomasEnOrden_C01EnVideoYElRestoImagenesFijas()
        {
            DatosCinematica c01 = Cargar("DatosCinematica_C01_EL_RECUERDO");
            DatosCinematica c02 = Cargar("DatosCinematica_C02_EL_MALETIN");
            DatosCinematica c03 = Cargar("DatosCinematica_C03_LA_DECISION");
            DatosCinematica c04 = Cargar("DatosCinematica_C04_EL_SUERO");
            DatosCinematica c05 = Cargar("DatosCinematica_C05_EL_INVENTOR");
            Assert.AreEqual(2, c01.Clips.Count, "C01: videos 02–03");
            Assert.AreEqual(0, c01.CantidadFotos, "C01 sigue en video");
            Assert.IsNotNull(c01.Clips[0]);
            Assert.IsNotNull(c01.Clips[1]);
            Assert.AreEqual(17, c02.Clips.Count, "C02: F01–F15 (con F12B y F12C)");
            Assert.AreEqual(17, c02.CantidadFotos);
            StringAssert.StartsWith("04_C02_F01", NombreToma(c02, 0));
            StringAssert.StartsWith("15_", NombreToma(c02, 16));
            Assert.AreEqual(6, c03.CantidadFotos, "C03: G01–G06");
            StringAssert.StartsWith("21_C07", NombreToma(c03, 5));
            Assert.AreEqual(0f, c03.SegundosPendientes);
            Assert.AreEqual(3, c04.CantidadFotos, "C04 EL SUERO: I01–I03");
            Assert.AreEqual(0f, c04.SegundosPendientes);
            Assert.AreEqual(3, c05.CantidadFotos, "Tino (primera mejora): J01–J03");
            StringAssert.StartsWith("25_", NombreToma(c05, 0));
        }

        /// <summary>T97: toma con narración = voz + 1 s; sin narración, imagen fija 3 s y C01 (video) sin voz.</summary>
        [Test]
        public void Datos_TramosDeLasTomas_NarracionMasPausaOTresSegundos()
        {
            foreach (string archivo in new[] { "DatosCinematica_C02_EL_MALETIN", "DatosCinematica_C03_LA_DECISION",
                "DatosCinematica_C04_EL_SUERO", "DatosCinematica_C05_EL_INVENTOR" })
            {
                DatosCinematica d = Cargar(archivo);
                for (int i = 0; i < d.Clips.Count; i++)
                {
                    Assert.IsTrue(d.EsFoto(i), archivo + ": toma " + i + " es imagen fija");
                    AudioClip voz = d.Narracion(i);
                    float esperado = voz != null ? voz.length + d.Pausa(i) : 3f;
                    Assert.AreEqual(esperado, d.TramoClip(i), 0.05f, archivo + " " + NombreToma(d, i));
                }
            }
            DatosCinematica c01 = Cargar("DatosCinematica_C01_EL_RECUERDO");
            for (int i = 0; i < c01.Clips.Count; i++)
            {
                Assert.IsNull(c01.Narracion(i), "C01 sin narración");
            }
            Assert.AreEqual(c01.DuracionVideos, c01.DuracionEfectiva, 0.01f);
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
            Assert.AreEqual(c02.Clips.Count - 1, ultimoClip, "Llegó a la última toma");
            Assert.LessOrEqual(huecos, 1, "Sin negro entre clips");
            Assert.Less(total, c02.DuracionEfectiva + 6f, "No se trabó");
            Assert.Greater(total, c02.DuracionConNarracion - 1f, "Nada se aceleró: cada frase completa y su pausa");
        }
    }
}
