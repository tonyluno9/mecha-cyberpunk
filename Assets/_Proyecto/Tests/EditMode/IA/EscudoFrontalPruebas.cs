using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1109: el escudo frontal del guardia pesado: ángulos, reducción 85%, aguante 150, rotura y regreso a los 5 s.</summary>
    public class EscudoFrontalPruebas
    {
        // El guardia mira a +Z. Un golpe que llega de frente viaja hacia -Z.
        private static readonly Vector3 Frente = Vector3.forward;

        private static Vector3 GolpeDesde(float grados)
        {
            // Dirección de un golpe cuyo ángulo contra el frente (de -Direccion) es "grados".
            return -(Quaternion.Euler(0f, grados, 0f) * Frente);
        }

        [TestCase(0f, true)]
        [TestCase(60f, true)]
        [TestCase(69f, true)]
        [TestCase(80f, false)]
        [TestCase(180f, false)]
        public void EsDeFrente_SegunElAngulo(float grados, bool esperado)
        {
            ReglaEscudoFrontal r = new ReglaEscudoFrontal();
            Assert.AreEqual(esperado, r.EsDeFrente(GolpeDesde(grados), Frente));
            Assert.AreEqual(grados, ReglaEscudoFrontal.AnguloDeFrente(GolpeDesde(grados), Frente), 0.01f);
        }

        [Test]
        public void Filtrar_DeFrente_ReduceOchentaYCincoYEsNormal()
        {
            ReglaEscudoFrontal r = new ReglaEscudoFrontal();
            ReglaEscudoFrontal.Resultado res = r.Filtrar(20f, GolpeDesde(0f), Frente);
            Assert.AreEqual(3f, res.Entra, 0.001f);
            Assert.AreEqual(TipoBloqueo.Normal, res.Bloqueo);
            Assert.IsFalse(res.SeRompio);
            Assert.AreEqual(130f, r.Aguante, 0.001f, "gasta aguante con el daño bruto");
        }

        [TestCase(80f)]
        [TestCase(180f)]
        public void Filtrar_DeLadoODeEspalda_EntraCompletoYNoGastaAguante(float grados)
        {
            ReglaEscudoFrontal r = new ReglaEscudoFrontal();
            ReglaEscudoFrontal.Resultado res = r.Filtrar(20f, GolpeDesde(grados), Frente);
            Assert.AreEqual(20f, res.Entra, 0.001f);
            Assert.AreEqual(TipoBloqueo.Ninguno, res.Bloqueo);
            Assert.AreEqual(150f, r.Aguante, 0.001f);
        }

        [Test]
        public void Filtrar_AlAcabarseElAguante_SeRompeUnaVezYSinEscudoEntraTodo()
        {
            ReglaEscudoFrontal r = new ReglaEscudoFrontal();
            Assert.IsFalse(r.Filtrar(100f, GolpeDesde(0f), Frente).SeRompio);
            ReglaEscudoFrontal.Resultado roto = r.Filtrar(60f, GolpeDesde(0f), Frente);
            Assert.IsTrue(roto.SeRompio, "150 de daño bruto rompen el escudo");
            Assert.AreEqual(9f, roto.Entra, 0.001f, "el golpe que lo rompe todavía se reduce");
            Assert.IsFalse(r.Activo);
            ReglaEscudoFrontal.Resultado sin = r.Filtrar(10f, GolpeDesde(0f), Frente);
            Assert.AreEqual(10f, sin.Entra, 0.001f, "sin escudo entra todo");
            Assert.AreEqual(TipoBloqueo.Ninguno, sin.Bloqueo);
            Assert.IsFalse(sin.SeRompio);
        }

        [Test]
        public void Avanzar_ALosCincoSegundos_ElEscudoVuelveCompleto()
        {
            ReglaEscudoFrontal r = new ReglaEscudoFrontal();
            r.Filtrar(150f, GolpeDesde(0f), Frente);
            r.Avanzar(4.9f);
            Assert.IsFalse(r.Activo, "a los 4.9 s sigue caído");
            r.Avanzar(0.2f);
            Assert.IsTrue(r.Activo, "a los 5.1 s vuelve");
            Assert.AreEqual(150f, r.Aguante, 0.001f);
            Assert.AreEqual(TipoBloqueo.Normal, r.Filtrar(10f, GolpeDesde(0f), Frente).Bloqueo);
        }
    }
}
