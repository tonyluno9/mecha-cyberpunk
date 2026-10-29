using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-199: reglas de balance de D-tony-197 (a, b, d, e, f) contra los assets reales.</summary>
    public class BalanceAdaptacionPruebas
    {
        private const string RutaBase = "Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_Base.asset";
        private const string RutaAvanzado = "Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_Avanzado.asset";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private const string RutaSinapsis2 = "Assets/_Proyecto/Progresion/Datos/Implantes/DatosImplante_implante_sistemanervioso_2.asset";
        private const string RutaCasero = "Assets/_Proyecto/IA/Datos/DatosMecha_Casero.asset";
        private const string CarpetaLigas = "Assets/_Proyecto/IA/Datos/Ligas";
        private const float Cuadro = 1f / 30f;
        private const float Tolerancia = 0.0001f;

        private static DatosAdaptacionRival Cargar(string ruta)
        {
            DatosAdaptacionRival d = AssetDatabase.LoadAssetAtPath<DatosAdaptacionRival>(ruta);
            Assert.IsNotNull(d, "Falta " + ruta);
            return d;
        }

        private static IEnumerable<DatosAdaptacionRival> Los2()
        {
            yield return Cargar(RutaBase);
            yield return Cargar(RutaAvanzado);
        }

        [Test]
        public void Assets_NivelMaximoDeBaseEs1YDeAvanzadoEs2()
        {
            Assert.AreEqual(1, Cargar(RutaBase).NivelMaximo);
            Assert.AreEqual(2, Cargar(RutaAvanzado).NivelMaximo);
        }

        [Test]
        public void Assets_UmbralesYActivas_SonLosDeLaDecision()
        {
            foreach (DatosAdaptacionRival d in Los2())
            {
                Assert.AreEqual(2, d.UmbralesNivel.Count);
                Assert.AreEqual(1, d.UmbralesNivel[0], d.name);
                Assert.AreEqual(3, d.UmbralesNivel[1], d.name);
                Assert.LessOrEqual(d.MaximoActivas, 2, d.name);
                Assert.AreEqual(2, d.DerrotasSinAprender, d.name);
                Assert.AreEqual(1, d.OlvidoPorPelea, d.name);
            }
        }

        [Test]
        public void Assets_ProbabilidadesFactoresYExtra_RespetanLosTopes()
        {
            foreach (DatosAdaptacionRival d in Los2())
            {
                Assert.AreEqual(2, d.ProbabilidadGuardia.Count);
                Assert.AreEqual(2, d.ProbabilidadAmago.Count);
                Assert.AreEqual(2, d.ProbabilidadRetirada.Count);
                Assert.AreEqual(2, d.FactorVelocidadRetirada.Count);
                Assert.AreEqual(2, d.ExtraAvisoMaximo.Count);
                for (int n = 0; n < 2; n++)
                {
                    Assert.LessOrEqual(d.ProbabilidadGuardia[n], 0.65f, d.name);
                    Assert.LessOrEqual(d.ProbabilidadAmago[n], 0.35f, d.name);
                    Assert.LessOrEqual(d.ProbabilidadRetirada[n], 0.7f, d.name);
                    Assert.LessOrEqual(d.FactorVelocidadRetirada[n], 1f, d.name);
                    Assert.Greater(d.FactorVelocidadRetirada[n], 0f, d.name);
                    Assert.GreaterOrEqual(d.ExtraAvisoMaximo[n], 0f, "FINTAS nunca acortan el aviso: " + d.name);
                    Assert.LessOrEqual(d.ExtraAvisoMaximo[n], 0.4f, d.name);
                }
                // El nivel II nunca es más suave que el I.
                Assert.GreaterOrEqual(d.ProbabilidadGuardia[1], d.ProbabilidadGuardia[0]);
                Assert.GreaterOrEqual(d.ExtraAvisoMaximo[1], d.ExtraAvisoMaximo[0]);
            }
        }

        [Test]
        public void Assets_NoTienenCamposDeVidaDanioAlcanceRecargaNiVelocidadAbsoluta()
        {
            string[] prohibidos = { "vida", "danio", "alcance", "recarga", "velocidad" };
            SerializedProperty p = new SerializedObject(Cargar(RutaBase)).GetIterator();
            bool entrar = true;
            while (p.NextVisible(entrar))
            {
                entrar = false;
                string nombre = p.name.ToLowerInvariant();
                if (nombre == "m_script")
                {
                    continue;
                }
                foreach (string palabra in prohibidos)
                {
                    // "factorVelocidadRetirada" es un multiplicador (<= 1), permitido por la regla a.
                    if (nombre.Contains(palabra))
                    {
                        Assert.IsTrue(nombre.StartsWith("factor"), "Campo absoluto prohibido: " + p.name);
                    }
                }
            }
        }

        [Test]
        public void Guardia_VentanaDeReaccion_EntreSinapsis2YElGolpeDeSerieParaLos21Rivales()
        {
            DatosMecha jugador = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaJugador);
            DatosImplante sinapsis2 = AssetDatabase.LoadAssetAtPath<DatosImplante>(RutaSinapsis2);
            Assert.IsNotNull(jugador);
            Assert.IsNotNull(sinapsis2);

            float serie = jugador.PreparacionGolpe;
            float conSinapsis2 = ReglaReaccionImplantes.Aplicar(jugador.Valores, sinapsis2.Efectos.multiplicadorReaccionMecha).preparacionGolpe;
            float techo = serie - Cuadro;
            float piso = conSinapsis2 + Cuadro;

            List<DatosMecha> rivales = new List<DatosMecha>();
            foreach (string guid in AssetDatabase.FindAssets("t:DatosMecha", new[] { CarpetaLigas }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileName(ruta).StartsWith("DatosMecha_Liga"))
                {
                    rivales.Add(AssetDatabase.LoadAssetAtPath<DatosMecha>(ruta));
                }
            }
            rivales.Add(AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaCasero));
            Assert.AreEqual(21, rivales.Count, "20 rivales de liga + EL CHATARRAS");

            foreach (DatosAdaptacionRival d in Los2())
            {
                foreach (DatosMecha r in rivales)
                {
                    float arriba = d.ReaccionGuardia + r.TiempoSubirGuardia;
                    Debug.Log("[Balance] " + d.name + " " + r.name + ": " + d.ReaccionGuardia.ToString("0.###") + " + "
                        + r.TiempoSubirGuardia.ToString("0.###") + " = " + arriba.ToString("0.####") + " s; techo (serie "
                        + serie.ToString("0.###") + " - 1/30) = " + techo.ToString("0.####") + "; piso (SINAPSIS II "
                        + conSinapsis2.ToString("0.####") + " + 1/30) = " + piso.ToString("0.####"));
                    Assert.LessOrEqual(arriba, techo + Tolerancia, d.name + " " + r.name + " no alcanza al golpe de serie");
                    Assert.GreaterOrEqual(arriba, piso - Tolerancia, d.name + " " + r.name + " alcanza a SINAPSIS II");
                }
            }
        }
    }
}
