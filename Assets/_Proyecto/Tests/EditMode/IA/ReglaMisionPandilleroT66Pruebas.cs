using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1701 (T66): la misión del Tianguis cuenta solo a los vivos y activos; el inalcanzable o atorado se reubica.</summary>
    public class ReglaMisionPandilleroT66Pruebas
    {
        [Test]
        public void NoBloquea_CaidoVencidoInexistenteONuncaAparecio()
        {
            Assert.IsTrue(ReglaMisionPandillero.NoBloquea(false, false, false, false, false), "destruido");
            Assert.IsTrue(ReglaMisionPandillero.NoBloquea(true, true, false, false, false), "vencido");
            Assert.IsTrue(ReglaMisionPandillero.NoBloquea(true, false, true, true, false), "KO");
            Assert.IsTrue(ReglaMisionPandillero.NoBloquea(true, false, false, false, true), "nunca apareció con el grupo ya en marcha");
        }

        [Test]
        public void Bloquea_VivoYActivo_OInactivoSiElGrupoAunNoArranca()
        {
            Assert.IsFalse(ReglaMisionPandillero.NoBloquea(true, false, false, true, true));
            Assert.IsFalse(ReglaMisionPandillero.NoBloquea(true, false, false, false, false));
        }

        [Test]
        public void FueraDeLugar_BajoElMapa_OLejosDeLaZona()
        {
            Vector3 centro = new Vector3(10f, 0f, 0f);
            Assert.IsFalse(ReglaMisionPandillero.FueraDeLugar(new Vector3(20f, 0f, 5f), centro, 35f));
            Assert.IsTrue(ReglaMisionPandillero.FueraDeLugar(new Vector3(10f, -8f, 0f), centro, 35f), "bajo el mapa");
            Assert.IsTrue(ReglaMisionPandillero.FueraDeLugar(new Vector3(200f, 0f, 0f), centro, 35f), "fuera de la zona");
        }

        [Test]
        public void Atorado_SinMoverseYLejosDelJugador()
        {
            Vector3 p = new Vector3(50f, 0f, 0f);
            Assert.IsTrue(ReglaMisionPandillero.Atorado(p, p + Vector3.right * 0.1f, Vector3.zero));
            Assert.IsFalse(ReglaMisionPandillero.Atorado(p, p + Vector3.right * 5f, Vector3.zero), "se movió");
            Assert.IsFalse(ReglaMisionPandillero.Atorado(p, p, p + Vector3.right * 3f), "está junto al jugador: pelea normal");
        }

        [Test]
        public void Candidato_EstaCercaDelJugador_YDelanteElPrimero()
        {
            Vector3 c = ReglaMisionPandillero.Candidato(new Vector3(5f, 1f, 5f), Vector3.forward, 0);
            Assert.AreEqual(ReglaMisionPandillero.DistanciaReubicacion, Vector3.Distance(new Vector3(5f, 1f, 5f), c), 0.01f);
            Assert.Greater(c.z, 5f);
        }
    }
}
