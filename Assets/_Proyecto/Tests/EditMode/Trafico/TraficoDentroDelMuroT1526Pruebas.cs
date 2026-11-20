using System.Collections.Generic;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1526: el tráfico solo cicla dentro del muro (grafo recortado, área del muro) y los modelos de policía no salen como carros civiles.</summary>
    public class TraficoDentroDelMuroT1526Pruebas
    {
        private const float Lado = 80f;
        private const float Paso = 94f;

        private static GrafoCalles GrafoConAfueras()
        {
            List<Vector2> manzanas = new List<Vector2>
            {
                new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
                new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f),
            };
            GrafoCalles grafo = GrafoCalles.DeManzanas(manzanas, Lado, 0.2f);
            TraficoCalle.AlargarHaciaLasAfueras(grafo);
            return grafo;
        }

        [Test]
        public void Recortar_DesconectaLosNodosDeAfuera_YLaCalleDeSalidaTerminaAdentro()
        {
            GrafoCalles grafo = GrafoConAfueras();
            int total = grafo.Cantidad;
            // El área solo cubre las manzanas: las afueras (92 m más allá) quedan fuera.
            Rect area = Rect.MinMaxRect(-Paso - Lado / 2f - 1f, -Paso / 2f - Lado / 2f - 1f, Paso + Lado / 2f + 1f, Paso / 2f + Lado / 2f + 1f);
            int quitados = grafo.Recortar(area);
            Assert.Greater(quitados, 0, "las calles de las afueras caen fuera del muro");
            Assert.AreEqual(quitados, grafo.NodosFuera);
            int dentro = 0;
            for (int i = 0; i < total; i++)
            {
                if (grafo.Dentro(i))
                {
                    dentro++;
                    foreach (int v in grafo.Vecinos(i))
                    {
                        Assert.IsTrue(grafo.Dentro(v), "ningún nodo de adentro lleva a uno de afuera");
                    }
                }
                else
                {
                    Assert.AreEqual(0, grafo.Vecinos(i).Count, "un nodo de afuera no tiene tramos");
                }
            }
            Assert.AreEqual(total - quitados, dentro);
            Assert.IsTrue(grafo.Dentro(grafo.MasCercano(new Vector3(500f, 0f, 0f))), "el nodo más cercano a un punto lejano es de adentro");
            Assert.IsTrue(grafo.Dentro(grafo.MasLejano(new Vector3(0f, 0f, 0f))));
        }


        [Test]
        public void Recortar_ConElAreaRealDelMuro_ConservaLaCuadriculaYNingunRecorridoSale()
        {
            // El muro de la ciudad: área de 270 x 176 m (x de -135 a 135, z de -88 a 88); los extremos de la cuadrícula (+-134, +-87) quedan dentro, a 1 m del muro, y no se mueven.
            GrafoCalles grafo = GrafoConAfueras();
            Rect area = Rect.MinMaxRect(-135f, -88f, 135f, 88f);
            Vector3[] antes = new Vector3[grafo.Cantidad];
            for (int i = 0; i < antes.Length; i++)
            {
                antes[i] = grafo.Nodo(i);
            }
            int quitados = grafo.Recortar(area);
            Assert.Greater(quitados, 0, "las calles de las afueras se desconectan");
            Assert.AreEqual(0, grafo.NodosAjustados, "los nodos de la cuadrícula no se mueven");
            int dentro = 0;
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                if (!grafo.Dentro(i))
                {
                    continue;
                }
                dentro++;
                Assert.IsTrue(area.Contains(new Vector2(grafo.Nodo(i).x, grafo.Nodo(i).z)), "el nodo " + i + " está fuera del área: " + grafo.Nodo(i));
            }
            Assert.GreaterOrEqual(dentro, 8, "queda toda la cuadrícula");
            for (int i = 0; i < antes.Length; i++)
            {
                if (grafo.Dentro(i))
                {
                    Assert.AreEqual(antes[i], grafo.Nodo(i), "la cuadrícula queda donde estaba");
                }
            }
            // Un recorrido al azar (como el de los carros, que no regresan salvo en un callejón) nunca llega a un nodo de afuera.
            System.Random azar = new System.Random(1526);
            for (int inicio = 0; inicio < grafo.Cantidad; inicio++)
            {
                if (!grafo.Dentro(inicio))
                {
                    continue;
                }
                int actual = inicio;
                for (int paso = 0; paso < 2000; paso++)
                {
                    IReadOnlyList<int> v = grafo.Vecinos(actual);
                    Assert.Greater(v.Count, 0, "un nodo de adentro nunca queda suelto");
                    actual = v[azar.Next(v.Count)];
                    Assert.IsTrue(grafo.Dentro(actual));
                    Assert.IsTrue(area.Contains(new Vector2(grafo.Nodo(actual).x, grafo.Nodo(actual).z)));
                }
            }
        }

        [Test]
        public void Fuera_TieneUnaToleranciaParaElCarroQueDaVueltaPegadoAlMuro()
        {
            AreaMuro.Definir(new Rect(-135f, -88f, 270f, 176f));
            try
            {
                Assert.IsFalse(AreaMuro.Fuera(new Vector3(136f, 0f, 0f)), "1 m pasado el muro: todavía está dando la vuelta");
                Assert.IsTrue(AreaMuro.Fuera(new Vector3(140f, 0f, 0f)));
                Assert.IsTrue(AreaMuro.Fuera(new Vector3(0f, 0f, -95f)));
            }
            finally
            {
                AreaMuro.Definir(new Rect());
            }
        }

        [Test]
        public void Callejon_ParadoContraElMuroAlFinalDeLaCalle_DaVuelta()
        {
            Assert.IsTrue(ReglaCallejon.DebeDarVuelta("fisico", ReglaCallejon.SegundosParaVuelta, 1, 4f), "parado contra el muro en un nodo sin salida: vuelta en U");
            Assert.IsFalse(ReglaCallejon.DebeDarVuelta("fisico", 0.5f, 1, 4f), "todavía no esperó lo suficiente");
            Assert.IsFalse(ReglaCallejon.DebeDarVuelta("fisico", 5f, 3, 4f), "en un cruce no da vuelta: elige otra calle");
            Assert.IsFalse(ReglaCallejon.DebeDarVuelta("carro CarroTrafico_2", 5f, 1, 4f), "esperar a otro carro no es dar vuelta");
            Assert.IsFalse(ReglaCallejon.DebeDarVuelta("fisico", 5f, 1, 200f), "lejos del final no da vuelta");
        }

        [Test]
        public void Callejon_LosFinalesDeCalleSiguenSiendoNodosDeAdentroConSalidaDeRegreso()
        {
            GrafoCalles grafo = GrafoConAfueras();
            Rect area = Rect.MinMaxRect(-135f, -88f, 135f, 88f);
            grafo.Recortar(area);
            int finales = 0;
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                if (grafo.Dentro(i) && grafo.Vecinos(i).Count == 1)
                {
                    finales++;
                    Assert.LessOrEqual(Mathf.Abs(grafo.Nodo(i).x), 135f);
                    Assert.LessOrEqual(Mathf.Abs(grafo.Nodo(i).z), 88f);
                }
            }
            Assert.Greater(finales, 0, "hay finales de calle");
            // Un carro que llega a un final vuelve por donde vino (el único vecino) y nunca se queda sin siguiente nodo.
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                if (grafo.Dentro(i))
                {
                    Assert.GreaterOrEqual(grafo.Vecinos(i).Count, 1, "el nodo " + i + " quedó sin salida ni regreso");
                }
            }
        }
        [Test]
        public void Recortar_ConUnAreaQueDejaPocosNodos_NoHaceNada()
        {
            GrafoCalles grafo = GrafoConAfueras();
            Assert.AreEqual(0, grafo.Recortar(Rect.MinMaxRect(1000f, 1000f, 1100f, 1100f)));
            Assert.AreEqual(0, grafo.NodosFuera);
        }

        [Test]
        public void Recortar_TodoDentro_NoQuitaNada()
        {
            GrafoCalles grafo = GrafoConAfueras();
            Assert.AreEqual(0, grafo.Recortar(Rect.MinMaxRect(-10000f, -10000f, 10000f, 10000f)));
            Assert.IsTrue(grafo.Dentro(0));
        }

        [Test]
        public void Calcular_CajaDeLosPuntosMetidaPorElMargen()
        {
            List<Vector2> puntos = new List<Vector2> { new Vector2(-200f, -150f), new Vector2(200f, -150f), new Vector2(200f, 150f), new Vector2(-200f, 150f), new Vector2(0f, 150f) };
            Rect? area = AreaMuro.Calcular(puntos, 4f);
            Assert.IsTrue(area.HasValue);
            Assert.AreEqual(-196f, area.Value.xMin, 0.001f);
            Assert.AreEqual(196f, area.Value.xMax, 0.001f);
            Assert.AreEqual(-146f, area.Value.yMin, 0.001f);
            Assert.AreEqual(146f, area.Value.yMax, 0.001f);
        }

        [Test]
        public void Calcular_ConPocosPuntosOUnaCajaChica_NoHayArea()
        {
            Assert.IsFalse(AreaMuro.Calcular(null, 4f).HasValue);
            Assert.IsFalse(AreaMuro.Calcular(new List<Vector2> { Vector2.zero, Vector2.one }, 4f).HasValue);
            List<Vector2> chica = new List<Vector2> { Vector2.zero, new Vector2(10f, 0f), new Vector2(10f, 10f), new Vector2(0f, 10f) };
            Assert.IsFalse(AreaMuro.Calcular(chica, 1f).HasValue, "10 m de lado no es un muro que cierre la ciudad");
        }

        [Test]
        public void AreaMuro_DefinirYFuera()
        {
            AreaMuro.Definir(new Rect(-100f, -100f, 200f, 200f));
            try
            {
                Assert.IsFalse(AreaMuro.Fuera(new Vector3(0f, 0f, 0f)));
                Assert.IsTrue(AreaMuro.Fuera(new Vector3(150f, 0f, 0f)));
                Assert.IsTrue(AreaMuro.Fuera(new Vector3(0f, 5f, -101f)));
            }
            finally
            {
                AreaMuro.Definir(new Rect());
            }
        }

        [Test]
        public void EsPiezaDelMuro_SoloMU()
        {
            Assert.IsTrue(AreaMuro.EsPiezaDelMuro("MU01_MuroPerimetral_a"));
            Assert.IsTrue(AreaMuro.EsPiezaDelMuro("MU03_PuertaMuro_a(Clone)"));
            Assert.IsFalse(AreaMuro.EsPiezaDelMuro("Puerta_3"));
            Assert.IsFalse(AreaMuro.EsPiezaDelMuro(null));
        }

        [TestCase("CarroTrafico_V05", true)]
        [TestCase("CarroTrafico_V06", true)]
        [TestCase("CarroTrafico_V08", true)]
        [TestCase("Carro_V11", true)]
        [TestCase("Carro_PA01", true)]
        [TestCase("Carro_PA02", true)]
        [TestCase("CarroTrafico_V05_Rojo", true)]
        [TestCase("Patrulla", true)]
        [TestCase("PatrullaCamioneta", true)]
        [TestCase("CarroTrafico_V01a", false)]
        [TestCase("CarroTrafico_V02", false)]
        [TestCase("CarroTrafico_V10", false)]
        [TestCase("CarroTrafico_V22", false)]
        [TestCase("CarroTrafico_MT01", false)]
        [TestCase("CarroTrafico", false)]
        [TestCase("", false)]
        public void ModeloDePolicia(string nombre, bool esperado)
        {
            Assert.AreEqual(esperado, ReglaModeloDePolicia.EsDePolicia(nombre));
        }
    }
}
