using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1543: la ciudad dentro del muro, sin nada detrás y sin huecos. Las pruebas de cuentas son puras; las de escena piden haber corrido
    /// FueraDelMuroT1543.Aplicar (la escena guardada).
    /// </summary>
    public class FueraDelMuroT1543Pruebas
    {
        private static readonly Rect Linea = Rect.MinMaxRect(-135f, -88f, 135f, 88f);
        private const float Margen = 4f;

        [Test]
        public void CentroFuera_PuntoAfueraYAdentro_Distingue()
        {
            Assert.IsTrue(CalculoMuroT1543.CentroFuera(Linea, Margen, new Vector3(200f, 0f, 0f)));
            Assert.IsTrue(CalculoMuroT1543.CentroFuera(Linea, Margen, new Vector3(0f, 5f, 100f)));
            Assert.IsFalse(CalculoMuroT1543.CentroFuera(Linea, Margen, new Vector3(0f, 0f, 0f)));
            Assert.IsFalse(CalculoMuroT1543.CentroFuera(Linea, Margen, new Vector3(137f, 0f, 0f)), "dentro del margen del grosor");
        }

        [Test]
        public void CajaFuera_SoloSiNoTocaElRectangulo()
        {
            Assert.IsTrue(CalculoMuroT1543.CajaFuera(Linea, Margen, new Bounds(new Vector3(0f, 5f, 100f), new Vector3(20f, 10f, 10f))));
            Assert.IsFalse(CalculoMuroT1543.CajaFuera(Linea, Margen, new Bounds(new Vector3(0f, 5f, 90f), new Vector3(20f, 10f, 10f))), "toca el margen");
            Assert.IsFalse(CalculoMuroT1543.CajaFuera(Linea, Margen, new Bounds(new Vector3(300f, 0f, 0f), new Vector3(600f, 1f, 600f))), "un plano gigante que cubre todo no esta fuera");
        }

        [Test]
        public void Recortar_PlanoGigante_QuedaEnLineaMasHolgura()
        {
            Bounds piso = new Bounds(new Vector3(0f, -0.5f, 0f), new Vector3(1000f, 1f, 1000f));
            Assert.IsTrue(CalculoMuroT1543.Excede(Linea, CalculoMuroT1543.HolguraPiso, piso));
            Vector2 min, max;
            Assert.IsTrue(CalculoMuroT1543.Recortar(Linea, CalculoMuroT1543.HolguraPiso, piso, out min, out max));
            Assert.AreEqual(-138f, min.x, 0.001f);
            Assert.AreEqual(138f, max.x, 0.001f);
            Assert.AreEqual(-91f, min.y, 0.001f);
            Assert.AreEqual(91f, max.y, 0.001f);
        }

        [Test]
        public void Recortar_PlanoTotalmenteAfuera_NoQuedaNada()
        {
            Vector2 min, max;
            Assert.IsFalse(CalculoMuroT1543.Recortar(Linea, CalculoMuroT1543.HolguraPiso, new Bounds(new Vector3(400f, 0f, 0f), new Vector3(100f, 1f, 100f)), out min, out max));
        }

        [Test]
        public void Componentes_DosGruposSeparados_DaDos()
        {
            List<Vector2Int> celdas = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1),
                new Vector2Int(10, 10), new Vector2Int(10, 11)
            };
            List<List<Vector2Int>> comp = CalculoMuroT1543.Componentes(celdas);
            Assert.AreEqual(2, comp.Count);
            Assert.AreEqual(3, comp[0].Count, "de mayor a menor");
            Assert.AreEqual(2, comp[1].Count);
        }

        [Test]
        public void Rectangulos_FormaEnL_CubreTodasLasCeldasSinRepetir()
        {
            List<Vector2Int> celdas = new List<Vector2Int>();
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    celdas.Add(new Vector2Int(x, y));
                }
            }
            for (int y = 2; y < 5; y++)
            {
                celdas.Add(new Vector2Int(0, y));
            }
            List<RectInt> rects = CalculoMuroT1543.Rectangulos(celdas);
            HashSet<Vector2Int> cubiertas = new HashSet<Vector2Int>();
            int suma = 0;
            foreach (RectInt r in rects)
            {
                suma += r.width * r.height;
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    for (int y = r.yMin; y < r.yMax; y++)
                    {
                        Assert.IsTrue(celdas.Contains(new Vector2Int(x, y)), "el rectangulo se sale de las celdas");
                        cubiertas.Add(new Vector2Int(x, y));
                    }
                }
            }
            Assert.AreEqual(celdas.Count, suma, "sin traslapes");
            Assert.AreEqual(celdas.Count, cubiertas.Count, "sin celdas sin cubrir");
            Assert.AreEqual(12, rects.Max(r => r.width * r.height), "el primero es el mayor posible (6x2)");
        }

        [Test]
        public void Barrio_PorCuadrante_SigueElMapaFinal()
        {
            Assert.AreEqual("Suburbios", CalculoMuroT1543.Barrio(new Vector2(-50f, 40f)));
            Assert.AreEqual("NeonAlto", CalculoMuroT1543.Barrio(new Vector2(50f, 40f)));
            Assert.AreEqual("Suburbios", CalculoMuroT1543.Barrio(new Vector2(-50f, -40f)));
            Assert.AreEqual("Nova", CalculoMuroT1543.Barrio(new Vector2(50f, -40f)));
            Assert.AreEqual("Mercado", CalculoMuroT1543.Barrio(new Vector2(0f, 22f)));
            Assert.AreEqual("NeonAlto", CalculoMuroT1543.ClavePaleta("Nova"));
        }

        [Test]
        public void DireccionAfuera_EnLadosYEsquinas_ApuntaAlExterior()
        {
            Assert.AreEqual(Vector3.right, CalculoMuroT1543.DireccionAfuera(new Vector3(135f, 0f, 0f), Linea));
            Assert.AreEqual(Vector3.left, CalculoMuroT1543.DireccionAfuera(new Vector3(-135f, 0f, 20f), Linea));
            Assert.AreEqual(Vector3.forward, CalculoMuroT1543.DireccionAfuera(new Vector3(47f, 0f, 88f), Linea));
            Vector3 esquina = CalculoMuroT1543.DireccionAfuera(new Vector3(135f, 0f, -88f), Linea);
            Assert.Greater(esquina.x, 0.5f);
            Assert.Less(esquina.z, -0.5f);
        }

        [Test]
        public void RepartirFrente_LoteDe40x14_RellenaElFrenteConVariosEdificios()
        {
            List<CalculoMuroT1543.Modelo> paleta = new List<CalculoMuroT1543.Modelo>
            {
                new CalculoMuroT1543.Modelo { Tam = new Vector3(14f, 20f, 12f) },
                new CalculoMuroT1543.Modelo { Tam = new Vector3(18f, 25f, 16f) }
            };
            List<CalculoMuroT1543.Ajuste> r = CalculoMuroT1543.RepartirFrente(paleta, 40f, 14f, 3);
            Assert.GreaterOrEqual(r.Count, 2);
            Assert.IsTrue(r.All(a => a.Razon <= CalculoMuroT1543.EstiramientoMaximo + 0.001f));
        }

        [Test]
        public void RepartirFrente_LoteImposible_NoPonePiezasDistorsionadas()
        {
            List<CalculoMuroT1543.Modelo> paleta = new List<CalculoMuroT1543.Modelo> { new CalculoMuroT1543.Modelo { Tam = new Vector3(14f, 20f, 60f) } };
            Assert.AreEqual(0, CalculoMuroT1543.RepartirFrente(paleta, 14f, 4f, 0).Count);
        }

        // ---- Escena (despues de FueraDelMuroT1543.Aplicar) ----

        private static Scene Escena()
        {
            return EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Additive);
        }

        [Test]
        public void Escena_NadaActivoFueraDelMuro()
        {
            Scene escena = Escena();
            try
            {
                float margen = FueraDelMuroT1543.GrosorDelMuro() + 1f;
                List<string> fuera = FueraDelMuroT1543.ActivosAfuera(escena, FueraDelMuroT1543.LineaDelMuro(), margen);
                Assert.AreEqual(0, fuera.Count, "activos afuera (correr FueraDelMuroT1543.Aplicar): " + string.Join("; ", fuera.Take(15)));
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_NoQuedanHuecosGrandes()
        {
            Scene escena = Escena();
            try
            {
                List<FueraDelMuroT1543.ZonaHueco> zonas = FueraDelMuroT1543.MedirHuecos(escena);
                float mayor = zonas.Count == 0 ? 0f : zonas.Max(z => z.Area);
                Assert.LessOrEqual(mayor, FueraDelMuroT1543.HuecoMaximoM2, "hueco mas grande " + mayor + " m2 (" + zonas.Count + " zonas); correr FueraDelMuroT1543.Aplicar");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_MuroConTorresEnEsquinasYReflectores()
        {
            Scene escena = Escena();
            try
            {
                GameObject luces = escena.GetRootGameObjects().FirstOrDefault(g => g.name == FueraDelMuroT1543.RaizLuces);
                Assert.IsNotNull(luces, "falta " + FueraDelMuroT1543.RaizLuces + ": correr FueraDelMuroT1543.Aplicar");
                Light[] spots = luces.GetComponentsInChildren<Light>(true).Where(l => l.type == LightType.Spot).ToArray();
                Assert.GreaterOrEqual(spots.Length, 16, "un reflector por torre (4 esquinas + 12 a los lados de las 6 puertas)");
                GameObject muro = escena.GetRootGameObjects().FirstOrDefault(g => g.name == MuroCiudadT1513.NombreRaiz);
                Assert.IsNotNull(muro);
                Assert.GreaterOrEqual(muro.transform.Cast<Transform>().Count(h => h.name.StartsWith("TorreEsquina_")), 4, "torres en las 4 esquinas");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
