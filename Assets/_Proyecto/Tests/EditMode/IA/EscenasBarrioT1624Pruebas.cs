using System.Collections.Generic;
using MechaCyberpunk.IA.Barrio;
using MechaCyberpunk.UI.Voces;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1624: lógica pura de las escenas del barrio (frecuencia, no repetir, nunca dos a la vez) y de los subtítulos.</summary>
    public class EscenasBarrioT1624Pruebas
    {
        private static ReglaEscenasBarrio Crear(float azar = 0.5f) => new ReglaEscenasBarrio(() => azar, 10f, 30f, 30f, 100f, 100f);

        private static List<ReglaEscenasBarrio.Candidata> Lista(params string[] ids)
        {
            List<ReglaEscenasBarrio.Candidata> l = new List<ReglaEscenasBarrio.Candidata>();
            foreach (string id in ids)
            {
                l.Add(new ReglaEscenasBarrio.Candidata { Id = id, EsBullying = id.StartsWith("b"), Pandilla = 1, Peso = 1f });
            }
            return l;
        }

        [Test]
        public void PuedeIntentar_AntesDeLaPrimeraEspera_NoPuede()
        {
            ReglaEscenasBarrio r = Crear();
            Assert.IsFalse(r.PuedeIntentar(5f, false));
            Assert.IsTrue(r.PuedeIntentar(10f, false));
        }

        [Test]
        public void PuedeIntentar_ConMisionOCinematica_NoPuede()
        {
            Assert.IsFalse(Crear().PuedeIntentar(50f, true));
        }

        [Test]
        public void PuedeIntentar_ConEscenaEnCurso_NuncaDosALaVez()
        {
            ReglaEscenasBarrio r = Crear();
            r.Empezar("a");
            Assert.IsFalse(r.PuedeIntentar(500f, false));
        }

        [Test]
        public void Terminar_FijaEspaciadoMinimoEntreEscenas()
        {
            ReglaEscenasBarrio r = Crear();
            r.Empezar("a");
            r.Terminar(100f, false, 0);
            Assert.IsFalse(r.PuedeIntentar(129f, false));
            Assert.IsTrue(r.PuedeIntentar(130f, false));
        }

        [Test]
        public void Elegir_NoRepiteLaUltimaSiHayOtra()
        {
            ReglaEscenasBarrio r = Crear(0f);
            r.Empezar("a");
            r.Terminar(0f, false, 0);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual("b", r.Elegir(Lista("a", "b"), 1000f));
            }
        }

        [Test]
        public void Elegir_ConUnaSolaCandidata_LaRepiteEnVezDeNoDarNada()
        {
            ReglaEscenasBarrio r = Crear();
            r.Empezar("a");
            r.Terminar(0f, false, 0);
            Assert.AreEqual("a", r.Elegir(Lista("a"), 1000f));
        }

        [Test]
        public void Elegir_BullyingDeUnaPandillaEnEspera_NoSale()
        {
            ReglaEscenasBarrio r = Crear(0f);
            r.Empezar("b1");
            r.Terminar(100f, true, 1);
            Assert.AreEqual("n1", r.Elegir(Lista("b2", "n1"), 150f), "A los 50 s la pandilla sigue esperando");
            Assert.AreEqual("b2", r.Elegir(Lista("b2", "n1"), 250f), "Pasada la espera vuelve a poder salir");
        }

        [Test]
        public void Reintentar_NoGastaLaEsperaDeLaPandilla()
        {
            ReglaEscenasBarrio r = Crear();
            r.Empezar("b1");
            r.Reintentar(50f, 8f);
            Assert.AreEqual(0f, r.EsperaBullying(1));
            Assert.IsFalse(r.PuedeIntentar(57f, false));
            Assert.IsTrue(r.PuedeIntentar(58f, false));
        }

        [Test]
        public void DuracionLinea_SinTexto_EsCero_ConClipUsaElClip_ConTextoLargoTopa()
        {
            Assert.AreEqual(0f, ReglaEscenasBarrio.DuracionLinea(0f, 0f, string.Empty));
            Assert.AreEqual(3.3f, ReglaEscenasBarrio.DuracionLinea(0f, 3.3f, "hola"), 0.001f);
            Assert.AreEqual(2f, ReglaEscenasBarrio.DuracionLinea(2f, 3.3f, "hola"), 0.001f);
            Assert.AreEqual(7f, ReglaEscenasBarrio.DuracionLinea(0f, 0f, new string('a', 500)), 0.001f);
        }

        [Test]
        public void Subtitulos_Duracion_CoincideConLaDeLaRegla()
        {
            string texto = "Esta calle es nuestra, doña.";
            Assert.AreEqual(ReglaEscenasBarrio.DuracionLinea(0f, 0f, texto), ReglaSubtitulos.Duracion(0f, texto), 0.001f);
        }

        [Test]
        public void Subtitulos_DebeVerse_SoloCercaAlFrenteYEncendido()
        {
            Vector3 centro = new Vector3(0.5f, 0.5f, 5f);
            Assert.IsTrue(ReglaSubtitulos.DebeVerse(true, 10f, centro));
            Assert.IsFalse(ReglaSubtitulos.DebeVerse(false, 10f, centro), "Apagados");
            Assert.IsFalse(ReglaSubtitulos.DebeVerse(true, 16f, centro), "Lejos");
            Assert.IsFalse(ReglaSubtitulos.DebeVerse(true, 10f, new Vector3(0.5f, 0.5f, -1f)), "Detrás de la cámara");
            Assert.IsFalse(ReglaSubtitulos.DebeVerse(true, 10f, new Vector3(1.5f, 0.5f, 5f)), "Fuera de la pantalla");
        }
    }
}
