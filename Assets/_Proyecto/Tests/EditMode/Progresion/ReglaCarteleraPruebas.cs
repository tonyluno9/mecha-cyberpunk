using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Progresion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>ReglaCartelera (T-159, D-tony-99): el orden y el contenido de la lista de peleas del mostrador.</summary>
    public class ReglaCarteleraPruebas
    {
        private const int Ligas = 4;
        private const int Rivales = 5;
        private const int PremioCasera = 600;
        private const float Fraccion = 0.5f;

        // Premio inventado y distinto por (liga, rival): liga * 1000 + rival * 100.
        private static int Premio(int liga, int indice) => liga * 1000 + indice * 100;

        private static List<OpcionCartelera> Armar(int liga, int siguiente, float fraccion = Fraccion) =>
            ReglaCartelera.Armar(Ligas, Rivales, liga, siguiente, fraccion, PremioCasera, Premio);

        private static string Tipos(List<OpcionCartelera> lista) =>
            string.Join(",", lista.Select(o => o.Tipo + " " + o.Liga + "-" + o.Indice));

        [Test]
        public void AlEmpezar_EsSiguiente_Casera_Bloqueada()
        {
            var lista = Armar(1, 1);
            Assert.AreEqual("Siguiente 1-1,Casera 0-0,Bloqueada 2-5", Tipos(lista));
            Assert.AreEqual(Premio(1, 1), lista[0].Premio);
            Assert.AreEqual(PremioCasera, lista[1].Premio);
            Assert.IsFalse(lista[0].EsJefe);
            Assert.IsTrue(lista[2].EsJefe, "El aviso nombra al jefe");
        }

        [Test]
        public void EnLiga1ConSiguiente3_HayDosRevanchasAMitadDePremio()
        {
            var lista = Armar(1, 3);
            Assert.AreEqual("Siguiente 1-3,Revancha 1-1,Revancha 1-2,Casera 0-0,Bloqueada 2-5", Tipos(lista));
            Assert.AreEqual(Premio(1, 3), lista[0].Premio);
            Assert.AreEqual(Premio(1, 1) / 2, lista[1].Premio);
            Assert.AreEqual(Premio(1, 2) / 2, lista[2].Premio);
        }

        [Test]
        public void EnLiga2_NoHayRevanchasDeLaLiga1()
        {
            var lista = Armar(2, 1);
            Assert.AreEqual("Siguiente 2-1,Casera 0-0,Bloqueada 3-5", Tipos(lista));
            Assert.IsFalse(lista.Any(o => o.Tipo == TipoOpcionCartelera.Revancha));
        }

        [Test]
        public void ConElJefeComoSiguiente_SeMarcaJefe_YHayCuatroRevanchas()
        {
            var lista = Armar(2, Rivales);
            Assert.IsTrue(lista[0].EsJefe);
            Assert.AreEqual(4, lista.Count(o => o.Tipo == TipoOpcionCartelera.Revancha));
            Assert.IsFalse(lista.Where(o => o.Tipo == TipoOpcionCartelera.Revancha).Any(o => o.EsJefe));
        }

        [Test]
        public void TrasElCampeonDeLaLiga4_NoHaySiguienteNiBloqueada_YHayCincoRevanchas()
        {
            var lista = Armar(Ligas, 0);
            Assert.AreEqual("Revancha 4-1,Revancha 4-2,Revancha 4-3,Revancha 4-4,Revancha 4-5,Casera 0-0", Tipos(lista));
            Assert.IsTrue(lista[4].EsJefe);
        }

        [Test]
        public void EnLaUltimaLigaSinVencerAlCampeon_NoHayBloqueada()
        {
            var lista = Armar(Ligas, 2);
            Assert.AreEqual("Siguiente 4-2,Revancha 4-1,Casera 0-0", Tipos(lista));
        }

        [Test]
        public void LaRevanchaRedondeaElPremio()
        {
            var lista = ReglaCartelera.Armar(Ligas, Rivales, 1, 2, 0.333f, PremioCasera, (l, i) => 250);
            Assert.AreEqual(83, lista[1].Premio, "250 x 0.333 = 83.25");
        }

        [Test]
        public void OpcionesIgualesSeComparanPorValor()
        {
            Assert.AreEqual(Armar(1, 3)[1], Armar(1, 3)[1]);
            Assert.AreNotEqual(Armar(1, 3)[1], Armar(1, 3)[2]);
            Assert.IsTrue(Armar(1, 3).Contains(Armar(1, 3)[0]));
        }
    }
}
