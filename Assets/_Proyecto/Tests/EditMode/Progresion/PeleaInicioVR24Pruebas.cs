using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>VR24: el nuevo arranque (C02 → C03 → pelea con 3 SKULLS en el inicio → Dinero) sin la ranura, y la regla de la pelea.</summary>
    public class PeleaInicioVR24Pruebas
    {
        private static List<PasoRecorrido> Recorrer(PasoRecorrido desde, PasoRecorrido hasta)
        {
            var pasos = new List<PasoRecorrido> { desde };
            PasoRecorrido p = desde;
            for (int i = 0; i < 30 && p != hasta; i++)
            {
                p = GestorCandados.Siguiente(p);
                p = GestorCandados.Reemplazo(p); // lo que hace el director al entrar a un paso que ya no se juega
                pasos.Add(p);
            }
            return pasos;
        }

        [Test]
        public void Flujo_DelMaletinAlDoc_SinRanuraConPelea()
        {
            List<PasoRecorrido> pasos = Recorrer(PasoRecorrido.Maletin, PasoRecorrido.Dinero);
            CollectionAssert.AreEqual(new[] { PasoRecorrido.Maletin, PasoRecorrido.Decision, PasoRecorrido.PeleaSkulls, PasoRecorrido.Dinero }, pasos);
        }

        [Test]
        public void Flujo_DesdeDespierta_NoJuegaMolestanNiRanura()
        {
            List<PasoRecorrido> pasos = Recorrer(PasoRecorrido.Despierta, PasoRecorrido.Dinero);
            Assert.IsFalse(pasos.Contains(PasoRecorrido.Molestan), string.Join(",", pasos));
            Assert.IsFalse(pasos.Contains(PasoRecorrido.Ranura), string.Join(",", pasos));
            Assert.IsTrue(pasos.Contains(PasoRecorrido.PeleaSkulls));
        }

        [Test]
        public void GuardadoEnRanura_SigueEnLaDecision()
        {
            Assert.IsTrue(GestorCandados.SeSalta(PasoRecorrido.Ranura));
            Assert.AreEqual(PasoRecorrido.Decision, GestorCandados.Reemplazo(PasoRecorrido.Ranura));
            Assert.AreEqual(PasoRecorrido.PeleaSkulls, GestorCandados.Reemplazo(PasoRecorrido.PeleaSkulls));
        }

        [Test]
        public void ObjetivoDeLaRanura_YaNoEstaEnLaLista()
        {
            Assert.IsFalse(TablaRecorrido.Objetivos().Contains(TablaRecorrido.ObjetivoRanura));
            Assert.IsTrue(TablaRecorrido.Objetivos().Contains(TablaRecorrido.ObjetivoPeleaSkulls));
            Assert.AreEqual("TIENES Ƀ20,000 EN EL BANCO", string.Format(TablaRecorrido.AvisoBanco, "Ƀ20,000"));
        }

        [Test]
        public void PeleaSkulls_DeNocheYProtegidaComoElInicio()
        {
            Assert.IsTrue(ReglaNocheFija.Aplica(PasoRecorrido.PeleaSkulls, true));
            GestorCandados.Candados c = GestorCandados.CandadosPara(PasoRecorrido.PeleaSkulls);
            Assert.IsTrue(c.Policia && c.Nova && c.Pandillas, "sin policía ni NOVA en la pelea");
        }

        [Test]
        public void Terminada_SoloConLosTresVencidos()
        {
            Assert.IsFalse(ReglaPeleaInicio.Terminada(2, 3));
            Assert.IsTrue(ReglaPeleaInicio.Terminada(3, 3));
            Assert.IsFalse(ReglaPeleaInicio.Terminada(0, 0));
        }

        [Test]
        public void Danio_NuncaBajaDeLaVidaMinima()
        {
            Assert.AreEqual(ReglaPeleaInicio.DanioGolpe, ReglaPeleaInicio.Danio(100f, 100f), 0.001f);
            Assert.AreEqual(2f, ReglaPeleaInicio.Danio(32f, 100f), 0.001f);
            Assert.AreEqual(0f, ReglaPeleaInicio.Danio(20f, 100f), 0.001f);
        }

        [Test]
        public void SiguienteTurno_SaltaALosCaidos()
        {
            Assert.AreEqual(2, ReglaPeleaInicio.SiguienteTurno(0, new[] { true, false, true }));
            Assert.AreEqual(0, ReglaPeleaInicio.SiguienteTurno(2, new[] { true, false, true }));
            Assert.AreEqual(-1, ReglaPeleaInicio.SiguienteTurno(0, new[] { false, false, false }));
        }

        [Test]
        public void Aparicion_TresSeparadosDelanteDelInicio()
        {
            Vector3 a = ReglaPeleaInicio.Aparicion(Vector3.zero, Vector3.forward, 0, 3);
            Vector3 b = ReglaPeleaInicio.Aparicion(Vector3.zero, Vector3.forward, 1, 3);
            Vector3 c = ReglaPeleaInicio.Aparicion(Vector3.zero, Vector3.forward, 2, 3);
            Assert.AreEqual(ReglaPeleaInicio.MetrosAparicion, b.z, 0.001f);
            Assert.AreEqual(0f, b.x, 0.001f);
            Assert.AreEqual(ReglaPeleaInicio.SeparacionAparicion, Mathf.Abs(c.x - b.x), 0.001f);
            Assert.AreEqual(-a.x, c.x, 0.001f);
        }
    }
}
