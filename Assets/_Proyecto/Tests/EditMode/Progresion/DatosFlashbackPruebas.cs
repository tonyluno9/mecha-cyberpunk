using MechaCyberpunk.Progresion.Prologo;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>DatosFlashback_Base (T-131): seis planos completos con los ids que busca el director.</summary>
    public class DatosFlashbackPruebas
    {
        private const string Ruta = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosFlashback_Base.asset";
        private const int CantidadPlanos = 6;

        private DatosFlashback datos;

        [SetUp]
        public void Preparar()
        {
            datos = AssetDatabase.LoadAssetAtPath<DatosFlashback>(Ruta);
            Assert.IsNotNull(datos, "Existe DatosFlashback_Base");
        }

        [Test]
        public void Asset_TieneSeisPlanosConIdSegundosYSubtitulo()
        {
            Assert.AreEqual(CantidadPlanos, datos.Planos.Count);
            for (int i = 0; i < datos.Planos.Count; i++)
            {
                PlanoFlashback plano = datos.Planos[i];
                Assert.AreEqual("Flashback_Plano_" + (i + 1), plano.idPunto, "Id del plano " + (i + 1));
                Assert.Greater(plano.segundos, 0f, "Segundos del plano " + (i + 1));
                Assert.IsFalse(string.IsNullOrWhiteSpace(plano.subtitulo), "Subtítulo del plano " + (i + 1));
            }
        }

        [Test]
        public void Asset_TieneFundidoYTextoDeSalto()
        {
            Assert.Greater(datos.SegundosFundido, 0f);
            Assert.IsFalse(string.IsNullOrWhiteSpace(datos.TextoSalto));
            Assert.Greater(datos.SegundosParaSaltar, 0f);
        }

        [Test]
        public void SegundosTotales_SumaPlanosYDosFundidosPorPlano()
        {
            float esperado = 0f;
            foreach (PlanoFlashback plano in datos.Planos)
            {
                esperado += plano.segundos + 2f * datos.SegundosFundido;
            }
            Assert.AreEqual(esperado, datos.SegundosTotales, 0.0001f);
        }
    }
}
