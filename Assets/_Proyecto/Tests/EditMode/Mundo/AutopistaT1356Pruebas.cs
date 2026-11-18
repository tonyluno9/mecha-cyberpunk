using System.Collections.Generic;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEngine;
using T = MechaCyberpunk.Arte.Editor.ColocarT1356Trazado;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1356: trazado puro de la autopista elevada (pendiente, radio, uniones U6 a U9 y columnas).</summary>
    public class AutopistaT1356Pruebas
    {
        private static IEnumerable<(string nombre, List<Vector3> linea)> Lineas()
        {
            yield return ("Principal", T.Principal());
            yield return ("RampaNorte", T.RampaNorte());
            yield return ("Tunel", T.Tunel());
            yield return ("CalleEste", T.CalleEste());
            yield return ("ConectorEste", T.ConectorEste());
        }

        [Test]
        public void Pendiente_TodasLasLineas_NoPasanDe9PorCiento()
        {
            foreach ((string nombre, List<Vector3> linea) in Lineas())
            {
                Assert.LessOrEqual(T.PendienteMax(linea), T.PendienteMaxima + 0.0001f, nombre);
            }
        }

        [Test]
        public void Pendiente_RampasSuben10MetrosSinPasarse()
        {
            List<Vector3> norte = T.RampaNorte();
            Assert.AreEqual(10f, norte[norte.Count - 1].y - norte[0].y, 0.001f);
            Assert.Greater(T.PendienteMax(norte), 0.03f, "la rampa norte debe subir de verdad");
            Assert.Greater(T.PendienteMax(T.Principal()), 0.05f, "la bajada este debe bajar de verdad");
        }

        [Test]
        public void Radio_CurvasDeLaCalle_NoBajanDe40()
        {
            Assert.GreaterOrEqual(T.RadioMin(T.Principal()), T.RadioMinimo - 0.5f);
            // El enlace de 19 m (U7 a su enlace) es un cambio de rumbo seco: el radio se mide desde el enlace.
            Assert.GreaterOrEqual(T.RadioMin(T.RampaNorte(), 1), T.RadioMinimo - 0.5f);
        }

        [Test]
        public void Radio_Tunel_NoBajaDe30()
        {
            Assert.GreaterOrEqual(T.RadioMin(T.Tunel()), T.RadioTunel - 0.5f);
        }

        [Test]
        public void Uniones_U6Tunel_BocaYSalidaExactas()
        {
            List<Vector3> tunel = T.Tunel();
            Assert.AreEqual(new Vector3(-330f, 10f, 250f), tunel[0]);
            Assert.AreEqual(new Vector3(-360f, 10f, 185f), tunel[tunel.Count - 1]);
            Assert.IsTrue(T.Pasa(T.Principal(), new Vector3(-330f, 10f, 250f)), "el tablero empieza en la boca");
            Assert.AreEqual(Vector3.back, (tunel[tunel.Count - 1] - tunel[tunel.Count - 2]).normalized, "sale mirando al sur");
        }

        [Test]
        public void Uniones_U7RampaNorte_PasaPorSusPuntos()
        {
            List<Vector3> r = T.RampaNorte();
            Assert.IsTrue(T.Pasa(r, new Vector3(-107f, 0f, 201f)), "sigue unida a la calle cruzada norte");
            Assert.IsTrue(T.Pasa(r, new Vector3(-120f, 0f, 215f)));
            Assert.IsTrue(T.Pasa(r, new Vector3(-270f, 10f, 250f)), "T-1362: se une al tablero junto al túnel");
            Assert.IsTrue(T.Pasa(T.Principal(), new Vector3(-270f, 10f, 250f)), "la autopista tiene el punto de unión");
        }

        [Test]
        public void CalleBaja_SaleLaRampaDeElla_YVaParalelaAlTablero()
        {
            List<Vector3> baja = T.CalleBaja();
            Assert.IsTrue(T.Pasa(baja, T.U7Enlace), "la calle baja termina en el enlace donde arranca la rampa");
            Assert.IsTrue(T.Pasa(T.RampaNorte(), T.U7Enlace));
            foreach (Vector3 v in baja)
            {
                Assert.AreEqual(0f, v.y, 0.0001f, "a ras del piso");
                Assert.AreEqual(T.ZCalleBaja, v.z, 0.0001f, "paralela al tablero (z constante) y al sur de él");
                Assert.Less(v.z, T.ZTablero - T.MedioTableroPlanta - 10f);
            }
            Assert.LessOrEqual(T.PendienteMax(baja), T.PendienteMaxima);
        }

        [Test]
        public void CalleOeste_UneLaCruzadaOesteConLaCalleBaja_SinCulDeSac()
        {
            List<Vector3> o = T.CalleOeste();
            Assert.AreEqual(T.CruzadaOesteNorte, o[0], "arranca en el extremo norte de la calle cruzada oeste de T530");
            Assert.AreEqual(T.CalleBaja()[0], o[o.Count - 1], "termina donde empieza la calle baja");
            Assert.GreaterOrEqual(T.RadioMin(o), 25f - 0.5f, "vuelta de radio >= 25 m");
            Assert.LessOrEqual(T.PendienteMax(o), T.PendienteMaxima);
            foreach (Vector3 v in o)
            {
                Assert.Greater(v.x, -295f + T.AnchoCalleOeste, "lejos del cerro (x < -295 es de la sesión B)");
                Assert.GreaterOrEqual(v.z, 60f, "no entra a la calle cruzada oeste ni al corredor oeste de T530 (z = 0)");
            }
        }

        [Test]
        public void Extremos_DeTodasLasCallesDeLaAutopista_NingunoQuedaSuelto()
        {
            var lineas = new List<(string nombre, List<Vector3> linea)>(Lineas())
            {
                ("CalleBaja", T.CalleBaja()),
                ("CalleOeste", T.CalleOeste()),
            };
            foreach ((string nombre, List<Vector3> linea) in lineas)
            {
                foreach (Vector3 extremo in new[] { linea[0], linea[linea.Count - 1] })
                {
                    bool unido = false;
                    foreach ((string otro, List<Vector3> otra) in lineas)
                    {
                        if (otro != nombre && T.Pasa(otra, extremo, 2f))
                        {
                            unido = true;
                        }
                    }
                    foreach (Vector3 red in T.PuntosDeLaRedExterna)
                    {
                        if (Vector3.Distance(red, extremo) < 2f)
                        {
                            unido = true;
                        }
                    }
                    Assert.IsTrue(unido, nombre + ": extremo suelto en " + extremo);
                }
            }
        }

        [Test]
        public void Rampa_SubeAlOeste_ConPendienteEntre6Y9PorCiento()
        {
            List<Vector3> r = T.RampaNorte();
            Assert.Less(r[r.Count - 1].x, r[1].x, "sube hacia el oeste");
            Assert.LessOrEqual(T.PendienteMax(r), 0.09f);
            Assert.GreaterOrEqual(T.PendienteMax(r), 0.06f);
        }

        [Test]
        public void Casas_NingunaAMenosDe8MetrosDelBordeDelTablero()
        {
            foreach (List<Vector3> linea in new[] { T.Principal(), T.RampaNorte(), T.Tunel() })
            {
                foreach (Vector3 v in T.Resamplear(linea, 2f))
                {
                    foreach (var huella in T.HuellasCasas)
                    {
                        float borde = T.DistanciaAHuella(v, huella) - T.MedioTableroPlanta;
                        Assert.GreaterOrEqual(borde, T.SeparacionCasasTablero, "casa a " + borde + " m del borde del tablero en " + v);
                    }
                }
            }
        }

        [Test]
        public void Casas_LasDelSurDeLaCalleBajaQuedanAlSurDeSuAsfalto()
        {
            var oeste = T.HuellasCasas[0];
            Assert.Less(oeste.z1, T.ZCalleBaja - 7f, "el asfalto empieza 7 m al sur de la línea de la calle baja");
        }

        [Test]
        public void Uniones_U8U9_PasanPorSusPuntos()
        {
            List<Vector3> p = T.Principal();
            Assert.IsTrue(T.Pasa(p, new Vector3(240f, 10f, 250f)));
            Assert.IsTrue(T.Pasa(p, new Vector3(300f, 10f, 190f)));
            Assert.IsTrue(T.Pasa(p, new Vector3(300f, 0f, 60f)));
            List<Vector3> e = T.CalleEste();
            Assert.IsTrue(T.Pasa(e, new Vector3(300f, 0f, 60f)));
            Assert.IsTrue(T.Pasa(e, new Vector3(300f, 0f, 0f)));
            Assert.IsTrue(T.Pasa(e, new Vector3(300f, 0f, -45f)));
            List<Vector3> c = T.ConectorEste();
            Assert.IsTrue(T.Pasa(c, new Vector3(300f, 0f, 0f)));
            Assert.IsTrue(T.Pasa(c, new Vector3(249f, 0f, 0f)));
        }

        [Test]
        public void Tablero_LaLineaSigueLaAlturaRealDelTablero()
        {
            foreach (Vector3 v in T.Principal().GetRange(0, 20))
            {
                Assert.AreEqual(10f, v.y, 0.0001f);
                Assert.AreEqual(250f, v.z, 0.0001f);
            }
            List<Vector3> p = T.Principal();
            for (int i = 1; i < p.Count; i++)
            {
                Assert.LessOrEqual(Mathf.Abs(p[i].y - p[i - 1].y), 10f / 13f + 0.0001f, "sin saltos de altura");
            }
        }

        [Test]
        public void Columnas_ElevadasSeparadasA26MOMenos()
        {
            foreach (List<Vector3> linea in new[] { T.Principal(), T.RampaNorte() })
            {
                List<float> estaciones = T.Estaciones(linea, T.SeparacionColumnas);
                Assert.Greater(estaciones.Count, 5);
                float anterior = 0f;
                foreach (float s in estaciones)
                {
                    Assert.LessOrEqual(s - anterior, T.MaxSeparacionColumnas);
                    anterior = s;
                }
                Assert.LessOrEqual(T.Longitud(linea) - anterior, T.MaxSeparacionColumnas);
                foreach (Vector3 c in T.Columnas(linea))
                {
                    Assert.GreaterOrEqual(c.y, T.AlturaMinimaColumna);
                }
            }
        }

        [Test]
        public void Luminarias_CadaTreintaMetros()
        {
            var l = T.Luminarias(T.Principal(), 30f);
            Assert.Greater(l.Count, 15);
            Assert.LessOrEqual(Vector3.Distance(l[0].punto, l[1].punto), 30.5f);
        }
    }
}
