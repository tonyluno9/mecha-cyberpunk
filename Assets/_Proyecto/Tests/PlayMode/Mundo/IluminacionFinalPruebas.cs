using System.Collections;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// Iluminación final (T-099): las cuatro vistas de referencia, a las 22:00 y a las 12:00, no salen lavadas ni aplastadas. Guarda las ocho capturas
    /// y escribe sus mediciones en la consola para compararlas con la imagen de referencia a ojo.
    /// </summary>
    public class IluminacionFinalPruebas
    {
        private const int Vistas = 4;
        private const float MediaMinima = 0.05f;
        private const float MediaMaxima = 0.75f;
        private const float SaturadosMaximos = 0.25f;
        private const float NegrosMaximos = 0.6f;
        private const float SegundosFisica = 0.3f;

        private GameObject camaraObjeto;

        [TearDown]
        public void TearDown()
        {
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
        }

        [UnityTest]
        public IEnumerator Vistas_ADiaYNoche_NoSalenLavadasNiAplastadas()
        {
            yield return EscenaLugares.CargarCiudad();
            EscenaLugares.ColocarFrenteA(IdLugar.Casa, out camaraObjeto);
            CamaraLibre camara = camaraObjeto.GetComponent<CamaraLibre>();
            StringBuilder informe = new StringBuilder("[Mundo] T-099 mediciones:");
            for (int vista = 1; vista <= Vistas; vista++)
            {
                foreach (float hora in new[] { EscenaLugares.HoraNoche, EscenaLugares.HoraMediodia })
                {
                    Assert.IsTrue(VistasReferencia.Ir(vista, hora), "Existe la vista " + vista);
                    yield return EscenaLugares.Calentar();
                    yield return EscenaLugares.Esperar(SegundosFisica);
                    string nombre = "V" + vista + (hora >= 19f ? "_22" : "_12");
                    CapturaPruebas.Medicion m = CapturaPruebas.GuardarYMedir(camara.Camara, "docs/capturas/T-099-" + nombre + ".png");
                    informe.Append("\n  ").Append(nombre).Append(": ").Append(m);
                    Assert.Greater(m.Media, MediaMinima, nombre + " muy oscura (" + m + ")");
                    Assert.Less(m.Media, MediaMaxima, nombre + " muy clara (" + m + ")");
                    Assert.Less(m.Saturados, SaturadosMaximos, nombre + " lavada (" + m + ")");
                    Assert.Less(m.Negros, NegrosMaximos, nombre + " aplastada (" + m + ")");
                }
            }
            Debug.Log(informe.ToString());
        }
    }
}
