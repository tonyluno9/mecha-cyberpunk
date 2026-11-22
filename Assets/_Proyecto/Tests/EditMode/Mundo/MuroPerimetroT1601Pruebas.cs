using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>T9 (T-1601): el muro perimetral cerrado y nada activo afuera. Piden haber corrido MuroPerimetroT1601.Aplicar (la escena guardada).</summary>
    public class MuroPerimetroT1601Pruebas
    {
        [Test]
        public void Escena_NadaActivoFueraDelMuro()
        {
            Scene escena = EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Additive);
            try
            {
                float margen = FueraDelMuroT1543.GrosorDelMuro() + 1f;
                List<string> fuera = FueraDelMuroT1543.ActivosAfuera(escena, FueraDelMuroT1543.LineaDelMuro(), margen);
                Assert.AreEqual(0, fuera.Count, "activos afuera: " + string.Join("; ", fuera.Take(15)));
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_RaycastsCadaMetroDesdeAdentroChocanConElMuro()
        {
            EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Single);
            MuroPerimetroT1601.Resultado r = MuroPerimetroT1601.Analizar();
            Assert.Greater(r.Muestras, 1000, "el barrido no corrio");
            Assert.AreEqual(0, r.RayosSinChoque, "rayos que salen sin chocar: " + string.Join("; ", r.SinChoque.Take(15)));
        }

        [Test]
        public void Escena_UnionesDelMuroSinHuecosNiDesniveles()
        {
            EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Single);
            MuroPerimetroT1601.Resultado r = MuroPerimetroT1601.Analizar();
            Assert.AreEqual(0, r.Problemas.Count, string.Join("; ", r.Problemas.Take(10).Select(p => p.Texto)));
        }
    }
}
