using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-668: marcar un lugar, desmarcarlo con otro clic, cambiar de lugar y llegar (la marca se quita sola).</summary>
    public class PuntoMarcadoMapaPruebas
    {
        private static readonly Vector3 Tino = new Vector3(-45f, 0f, -77f);
        private static readonly Vector3 Casino = new Vector3(55f, 0f, 47f);

        [SetUp]
        public void Preparar() => PuntoMarcadoMapa.Quitar();

        [TearDown]
        public void Limpiar() => PuntoMarcadoMapa.Quitar();

        [Test]
        public void MarcarDestino_GuardaElLugarYSuPosicion()
        {
            PuntoMarcadoMapa.MarcarDestino(IdDestinoMapa.Tino, Tino + Vector3.up * 3f);
            Assert.IsTrue(PuntoMarcadoMapa.Hay);
            Assert.AreEqual(IdDestinoMapa.Tino, PuntoMarcadoMapa.Destino);
            Assert.AreEqual(Tino, PuntoMarcadoMapa.Posicion, "A la altura 0");
        }

        [Test]
        public void AlternarDestino_ElMismoLoQuita_OtroLoReemplaza()
        {
            PuntoMarcadoMapa.AlternarDestino(IdDestinoMapa.Tino, Tino);
            Assert.AreEqual(IdDestinoMapa.Tino, PuntoMarcadoMapa.Destino);
            PuntoMarcadoMapa.AlternarDestino(IdDestinoMapa.Casino, Casino);
            Assert.AreEqual(IdDestinoMapa.Casino, PuntoMarcadoMapa.Destino, "Otro lugar reemplaza al anterior");
            Assert.AreEqual(Casino, PuntoMarcadoMapa.Posicion);
            PuntoMarcadoMapa.AlternarDestino(IdDestinoMapa.Casino, Casino);
            Assert.IsFalse(PuntoMarcadoMapa.Hay, "El mismo lugar lo quita");
            Assert.IsNull(PuntoMarcadoMapa.Destino);
        }

        [Test]
        public void MarcarUnPuntoLibre_QuitaElLugar()
        {
            PuntoMarcadoMapa.MarcarDestino(IdDestinoMapa.Tino, Tino);
            PuntoMarcadoMapa.Marcar(new Vector3(1f, 0f, 2f));
            Assert.IsTrue(PuntoMarcadoMapa.Hay);
            Assert.IsNull(PuntoMarcadoMapa.Destino);
        }

        [Test]
        public void Llego_SoloDentroDeLosMetrosDeLlegada_YSoloSiHayMarca()
        {
            Assert.IsFalse(PuntoMarcadoMapa.Llego(Tino), "Sin marca no se llega a nada");
            PuntoMarcadoMapa.MarcarDestino(IdDestinoMapa.Tino, Tino);
            Assert.IsFalse(PuntoMarcadoMapa.Llego(Tino + new Vector3(PuntoMarcadoMapa.MetrosParaLlegar + 1f, 0f, 0f)));
            Assert.IsTrue(PuntoMarcadoMapa.Llego(Tino + new Vector3(PuntoMarcadoMapa.MetrosParaLlegar - 1f, 5f, 0f)), "Solo cuenta el plano");
        }

        [Test]
        public void Catalogo_TieneLosOchoLugaresDelDiseno_ConNombreYSinRepetirse()
        {
            string[] esperados = { "CASA", "EL DOC", "TINO", "MECÁNICO", "TIANGUIS", "CASINO", "MERCADO", "MISIONES" };
            Assert.AreEqual(esperados.Length, CatalogoDestinosMapa.Todos.Count);
            for (int i = 0; i < esperados.Length; i++)
            {
                Assert.AreEqual(esperados[i], CatalogoDestinosMapa.Todos[i].Nombre);
                Assert.AreSame(CatalogoDestinosMapa.Todos[i], CatalogoDestinosMapa.De(CatalogoDestinosMapa.Todos[i].Id));
            }
        }
    }
}
