using System.Collections.Generic;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Peatones;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1350: las calles de las zonas nuevas entran al grafo de calles (unión a la cuadrícula, alturas, autopista) y tienen rutas de banqueta.</summary>
    public class CallesDeZonaT1350Pruebas
    {
        private const float Lado = 80f;
        private const float Paso = 94f;
        private const float Altura = 0.2f;
        private const float Union = 12f;

        private static readonly List<Vector2> Manzanas = new List<Vector2>
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        private static GrafoCalles Base() => GrafoCalles.DeManzanas(Manzanas, Lado, Altura);

        private static CallesDeZona.Definicion Calle(bool autopista, params Vector3[] puntos) => new CallesDeZona.Definicion(puntos, autopista);

        private static int Agregar(GrafoCalles grafo, params CallesDeZona.Definicion[] calles) => CallesDeZona.Agregar(grafo, calles, Union);

        [Test]
        public void Agregar_SinCalles_NoCambiaElGrafo()
        {
            GrafoCalles grafo = Base();
            int antes = grafo.Cantidad;
            Assert.AreEqual(0, Agregar(grafo));
            Assert.AreEqual(antes, grafo.Cantidad);
            Assert.AreEqual(0, CallesDeZona.Agregar(grafo), "sin CalleZona en la escena no agrega nada");
            Assert.AreEqual(antes, grafo.Cantidad);
            Assert.IsFalse(grafo.TieneAlturas);
        }

        [Test]
        public void Agregar_CalleEnL_UnidaALaCuadricula_TieneCaminoDeUnCruceAlFinal()
        {
            GrafoCalles grafo = Base();
            int antes = grafo.Cantidad;
            Vector3 fin = new Vector3(200f, Altura, -60f);
            int unidos = Agregar(grafo, Calle(false, new Vector3(140f, Altura, 0f), new Vector3(200f, Altura, 0f), fin));
            Assert.AreEqual(antes + 3, grafo.Cantidad, "un nodo por punto");
            Assert.AreEqual(1, unidos, "solo el extremo cercano se une");
            int desde = grafo.MasCercano(new Vector3(-47f, Altura, 0f));
            int hasta = grafo.MasCercano(fin);
            List<int> camino = grafo.Camino(desde, hasta);
            Assert.GreaterOrEqual(camino.Count, 4, "existe un camino del cruce del centro al final de la calle de zona");
            Assert.AreEqual(hasta, camino[camino.Count - 1]);
        }

        [Test]
        public void Agregar_ExtremoLejano_NoSeUne()
        {
            GrafoCalles grafo = Base();
            int unidos = Agregar(grafo, Calle(false, new Vector3(160f, Altura, 40f), new Vector3(220f, Altura, 40f)));
            Assert.AreEqual(0, unidos);
            Assert.AreEqual(0, grafo.Camino(grafo.MasCercano(new Vector3(-47f, Altura, 0f)), grafo.MasCercano(new Vector3(220f, Altura, 40f))).Count,
                "una calle a más de 12 m y fuera del rumbo de cualquier calle sin salida queda aparte");
        }

        [Test]
        public void Agregar_ExtremoJustoAntesDelLimite_SeUne()
        {
            GrafoCalles grafo = Base();
            int unidos = Agregar(grafo, Calle(false, new Vector3(135f + Union - 0.5f, Altura, 0f), new Vector3(220f, Altura, 0f)));
            Assert.AreEqual(1, unidos);
        }

        [Test]
        public void Agregar_CalleSinSalidaQueApuntaAlExtremo_SeUneAunqueQuedeLejos()
        {
            // El corredor de las afueras llega más lejos que la cuadrícula: el extremo a 25 m sobre el mismo rumbo se une.
            GrafoCalles grafo = Base();
            int unidos = Agregar(grafo, Calle(false, new Vector3(160f, Altura, 0f), new Vector3(220f, Altura, 0f)));
            Assert.AreEqual(1, unidos);
            Assert.Greater(grafo.Camino(grafo.MasCercano(new Vector3(-47f, Altura, 0f)), grafo.MasCercano(new Vector3(220f, Altura, 0f))).Count, 0);
        }

        [Test]
        public void Agregar_NodosEnAlto_ConservanSuY()
        {
            GrafoCalles grafo = Base();
            Agregar(grafo, Calle(false, new Vector3(140f, Altura, 0f), new Vector3(200f, 10f, 0f), new Vector3(260f, 10f, 0f)));
            Assert.IsTrue(grafo.TieneAlturas);
            int alto = grafo.MasCercano(new Vector3(260f, 10f, 0f));
            Assert.AreEqual(10f, grafo.Nodo(alto).y, 0.0001f);
            int medio = grafo.MasCercano(new Vector3(200f, 10f, 0f));
            Assert.AreEqual(10f, grafo.Nodo(medio).y, 0.0001f);
        }

        [Test]
        public void Agregar_AutopistaEncimaDeLaCalle_NoSeUneALasDeAbajo()
        {
            GrafoCalles grafo = Base();
            int unidos = Agregar(grafo, Calle(true, new Vector3(140f, 10f, 0f), new Vector3(300f, 10f, 0f)));
            Assert.AreEqual(0, unidos, "a 5 m en planta pero 10 m más arriba: no es la misma calle");
        }

        [Test]
        public void MasCercano_ConAlturas_UsaLaDistanciaEn3D()
        {
            GrafoCalles grafo = Base();
            Agregar(grafo,
                Calle(true, new Vector3(250f, 10f, 0f), new Vector3(260f, 10f, 0f)),
                Calle(false, new Vector3(260f, Altura, -30f), new Vector3(260f, Altura, 0f), new Vector3(260f, Altura, 30f)));
            Assert.AreEqual(10f, grafo.Nodo(grafo.MasCercano(new Vector3(260f, 9f, 0f))).y, 0.0001f, "arriba, en la autopista");
            Assert.AreEqual(Altura, grafo.Nodo(grafo.MasCercano(new Vector3(260f, 1f, 0f))).y, 0.0001f, "abajo, en la calle");
        }

        [Test]
        public void Agregar_Autopista_MarcaLosTramosRapidos()
        {
            GrafoCalles grafo = Base();
            Agregar(grafo, Calle(true, new Vector3(300f, 10f, 0f), new Vector3(360f, 10f, 0f)), Calle(false, new Vector3(300f, 10f, 40f), new Vector3(360f, 10f, 40f)));
            int a = grafo.MasCercano(new Vector3(300f, 10f, 0f));
            int b = grafo.MasCercano(new Vector3(360f, 10f, 0f));
            int c = grafo.MasCercano(new Vector3(300f, 10f, 40f));
            int d = grafo.MasCercano(new Vector3(360f, 10f, 40f));
            Assert.IsTrue(grafo.EsRapido(a, b));
            Assert.IsFalse(grafo.EsRapido(c, d));
        }

        [Test]
        public void Agregar_DosCallesQueSeTocan_SeUnenYCompartenElPunto()
        {
            GrafoCalles grafo = Base();
            int antes = grafo.Cantidad;
            Agregar(grafo,
                Calle(false, new Vector3(300f, Altura, 0f), new Vector3(300f, Altura, 50f)),
                Calle(false, new Vector3(300f, Altura, 50f), new Vector3(340f, Altura, 50f)));
            Assert.AreEqual(antes + 3, grafo.Cantidad, "el punto común no se duplica");
            Assert.Greater(grafo.Camino(grafo.MasCercano(new Vector3(300f, Altura, 0f)), grafo.MasCercano(new Vector3(340f, Altura, 50f))).Count, 0);
        }

        [Test]
        public void AlargarHaciaLasAfueras_NoAlargaLasPuntasDeZonaNiLaUnidaALaZona()
        {
            GrafoCalles grafo = Base();
            Agregar(grafo, Calle(false, new Vector3(140f, Altura, 0f), new Vector3(200f, Altura, 0f), new Vector3(200f, Altura, -60f)));
            int antes = grafo.Cantidad;
            TraficoCalle.AlargarHaciaLasAfueras(grafo);
            Assert.AreEqual(antes + 5, grafo.Cantidad, "solo las 5 calles sin salida de la cuadrícula se alargan: la de (135, 0) ya sigue por la zona y la punta de zona se queda");
            Assert.AreEqual(1, grafo.Vecinos(grafo.MasCercano(new Vector3(200f, Altura, -60f))).Count);
        }

        [Test]
        public void DeCalleZona_DaDosBanquetasAAnchoMedioMasUnMetroYMedio()
        {
            List<Vector3[]> rutas = RutasBanqueta.DeCalleZona(new[] { new Vector3(0f, 0f, 0f), new Vector3(50f, 0f, 0f), new Vector3(100f, 0f, 0f) }, 14f, 1.5f, 0.2f);
            Assert.AreEqual(2, rutas.Count);
            foreach (Vector3[] ruta in rutas)
            {
                Assert.AreEqual(3, ruta.Length);
                foreach (Vector3 p in ruta)
                {
                    Assert.AreEqual(8.5f, Mathf.Abs(p.z), 0.001f);
                    Assert.AreEqual(0.2f, p.y, 0.001f);
                }
            }
            Assert.AreNotEqual(Mathf.Sign(rutas[0][0].z), Mathf.Sign(rutas[1][0].z), "una a cada lado");
        }

        [Test]
        public void DeCalleZona_UnSoloPunto_NoDaRutas()
        {
            Assert.AreEqual(0, RutasBanqueta.DeCalleZona(new[] { Vector3.zero, Vector3.zero }, 14f, 1.5f, 0.2f).Count);
        }
    }
}
