using System.Text.RegularExpressions;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ModeloFavores y ServicioFavores (T-182): máximo, gastar, guardar y cargar.</summary>
    public class FavoresPruebas
    {
        private GameObject objeto;
        private ServicioFavores servicio;

        [SetUp]
        public void SetUp()
        {
            objeto = new GameObject("Favores");
            servicio = objeto.AddComponent<ServicioFavores>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(objeto);

        [Test]
        public void Dar_HastaElMaximo_EntraTresVecesYLaCuartaNo()
        {
            Assert.IsTrue(servicio.Dar(TipoFavor.Descuento));
            Assert.IsTrue(servicio.Dar(TipoFavor.Descuento));
            Assert.IsTrue(servicio.Dar(TipoFavor.Descuento));
            Assert.IsFalse(servicio.Dar(TipoFavor.Descuento));
            Assert.AreEqual(3, servicio.Cantidad(TipoFavor.Descuento));
            Assert.AreEqual(0, servicio.Cantidad(TipoFavor.Escondite), "cada tipo cuenta aparte");
        }

        [Test]
        public void Gastar_SinFavores_DaFalseYNoAvisa()
        {
            int cambios = 0;
            servicio.Cambio += () => cambios++;
            Assert.IsFalse(servicio.Gastar(TipoFavor.Informacion));
            Assert.AreEqual(0, cambios);
            servicio.Dar(TipoFavor.Informacion);
            Assert.IsTrue(servicio.Gastar(TipoFavor.Informacion));
            Assert.AreEqual(0, servicio.Cantidad(TipoFavor.Informacion));
            Assert.AreEqual(2, cambios);
        }

        [Test]
        public void GuardarYCargar_ConservaLosTresConteos()
        {
            servicio.Dar(TipoFavor.Descuento);
            servicio.Dar(TipoFavor.Informacion);
            servicio.Dar(TipoFavor.Informacion);
            for (int i = 0; i < 3; i++) servicio.Dar(TipoFavor.Escondite);
            string texto = servicio.Guardar();

            var otro = new GameObject("Otro").AddComponent<ServicioFavores>();
            otro.Cargar(texto);

            Assert.AreEqual(1, otro.Cantidad(TipoFavor.Descuento));
            Assert.AreEqual(2, otro.Cantidad(TipoFavor.Informacion));
            Assert.AreEqual(3, otro.Cantidad(TipoFavor.Escondite));
            Object.DestroyImmediate(otro.gameObject);
        }

        [Test]
        public void Cargar_TextoRoto_NoCambiaNadaYAvisaConWarning()
        {
            servicio.Dar(TipoFavor.Descuento);
            servicio.Dar(TipoFavor.Descuento);
            foreach (string roto in new[] { "no es json", "" })
            {
                LogAssert.Expect(LogType.Warning, new Regex(@"\[Progresion\] Los favores no entendieron"));
                servicio.Cargar(roto);
            }
            Assert.AreEqual(2, servicio.Cantidad(TipoFavor.Descuento));
        }

        [Test]
        public void Cargar_TipoDesconocido_AvisaWarningEIgnoraEsaEntrada()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Progresion\] Los favores no conocen el tipo 'Raro'"));
            servicio.Cargar("{\"favores\":[{\"tipo\":\"Raro\",\"cantidad\":2},{\"tipo\":\"Escondite\",\"cantidad\":1}]}");
            Assert.AreEqual(1, servicio.Cantidad(TipoFavor.Escondite));
            Assert.AreEqual(0, servicio.Cantidad(TipoFavor.Descuento));
        }
    }
}
