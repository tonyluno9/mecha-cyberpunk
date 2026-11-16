using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-1007: la llanta de cada rueda de V01a, V01b, V05 y la patrulla mide casi lo que su hueco (el paso de rueda medido en la malla de la carrocería, sin llantas),
    /// y toca el piso. En las motos V07 y V08 el hueco es el de la llanta horneada que se borró (C19 viene sin llanta horneada: sin hueco que medir).
    /// </summary>
    public class RuedasAlHuecoT1007Pruebas
    {
        private const float PisoTolerancia = 0.02f;
        private const float MinimoRelleno = 0.8f;
        private const float MaximoRelleno = 1.02f;
        private const float CentradoTolerancia = 0.09f;

        private static IEnumerable<string> RutasCarros() => MedirRuedasT1007.RutasCarros();

        [Test]
        public void CadaLlanta_LlenaSuHueco_TocaElPiso_YQuedaCentrada([ValueSource(nameof(RutasCarros))] string ruta)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                var triangulos = MedirRuedasT1007.TriangulosEn(raiz.transform, MedirRuedasT1007.MallasCarroceria(raiz));
                Assert.IsNotEmpty(triangulos, "Carrocería sin ruedas en " + ruta);
                foreach (string nombre in MedirRuedasT1007.NombresRuedas)
                {
                    Transform modelo = raiz.transform.Find(nombre + "/Direccion/Giro/Modelo");
                    Bounds b = MedirRuedasT1007.LimitesEn(modelo.gameObject, raiz.transform);
                    var hueco = MedirRuedasT1007.MedirHueco(triangulos, b.center, Mathf.Sign(b.center.x));
                    float radio = b.extents.y;
                    Assert.That(radio / hueco.Radio, Is.InRange(MinimoRelleno, MaximoRelleno),
                        ruta + " " + nombre + ": llanta " + radio.ToString("F3") + " m, hueco " + hueco.Radio.ToString("F3") + " m");
                    Assert.AreEqual(0f, b.min.y, PisoTolerancia, ruta + " " + nombre + ": la llanta toca el piso (y = 0)");
                    Assert.LessOrEqual(Mathf.Abs(hueco.DesplazamientoZ), CentradoTolerancia, ruta + " " + nombre + ": llanta centrada en el hueco");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        [TestCase("V07")]
        [TestCase("V08")]
        public void MotoConLlantaHorneada_LaLlantaNuevaLlenaElHuecoQueSeBorro(string codigo)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(CreadorMotosPiezasT743.RutaPrefab(codigo));
            try
            {
                var m = CreadorMotosPiezasT743.Tabla.First(t => t.Codigo == codigo);
// La llanta mide la altura de su centro (apoyada en el piso): el radio de RuedasVisualesCarro. Se compara con el radio del cilindro que se borró, en la misma escala del modelo.                float radio = raiz.GetComponent<MechaCyberpunk.Vehiculos.Carro.RuedasVisualesCarro>().Radio;                float centros = (m.CentroDelantera.y + m.CentroTrasera.y) * 0.5f;                float huecos = (m.RadioCorteDelantera + m.RadioCorteTrasera) * 0.5f;                Assert.Greater(radio, 0f, codigo);                Assert.That(centros / huecos, Is.InRange(0.8f, 1.05f), codigo + ": llanta " + centros.ToString("F3") + ", hueco " + huecos.ToString("F3") + " (unidades del modelo)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
