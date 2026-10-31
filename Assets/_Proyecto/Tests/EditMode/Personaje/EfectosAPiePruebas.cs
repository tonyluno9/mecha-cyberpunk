using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Cálculo de correr, salto y golpe a pie con implantes (T-192), con los valores de los assets.</summary>
    public class EfectosAPiePruebas
    {
        private const string RutaDatosPersonaje = "Assets/_Proyecto/Personaje/Datos/DatosPersonaje_Base.asset";
        private const float Tolerancia = 0.0001f;

        private DatosPersonaje datos;

        [SetUp]
        public void SetUp() => datos = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaDatosPersonaje);

        private static EfectosImplantes De(EspacioImplante espacio, int nivel)
        {
            var d = AssetDatabase.LoadAssetAtPath<DatosImplante>("Assets/_Proyecto/Progresion/Datos/Implantes/DatosImplante_implante_"
                + espacio.ToString().ToLowerInvariant() + "_" + nivel + ".asset");
            Assert.IsNotNull(d, espacio + " " + nivel);
            return d.Efectos;
        }

        [Test]
        public void Ninguno_NoCambiaNada()
        {
            EfectosImplantes e = EfectosImplantes.Ninguno;
            Assert.AreEqual(datos.VelocidadCorrer, EfectosAPie.VelocidadCorrer(datos, e), Tolerancia);
            Assert.AreEqual(datos.AlturaSalto, EfectosAPie.AlturaSalto(datos, e), Tolerancia);
            Assert.AreEqual(datos.DanioGolpe, EfectosAPie.DanioGolpe(datos, e), Tolerancia);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Piernas_CorrerYSaltoSubenYElGolpeNoCambia(int nivel)
        {
            EfectosImplantes e = De(EspacioImplante.Piernas, nivel);
            Assert.Greater(e.multiplicadorCorrer, 1f);
            Assert.Greater(e.multiplicadorSalto, 1f);
            Assert.AreEqual(datos.VelocidadCorrer * e.multiplicadorCorrer, EfectosAPie.VelocidadCorrer(datos, e), Tolerancia);
            Assert.AreEqual(datos.AlturaSalto * e.multiplicadorSalto, EfectosAPie.AlturaSalto(datos, e), Tolerancia);
            Assert.AreEqual(datos.DanioGolpe, EfectosAPie.DanioGolpe(datos, e), Tolerancia);
            Debug.Log("[Prueba] T-192 piernas " + nivel + ": correr " + EfectosAPie.VelocidadCorrer(datos, e) + " m/s (base " + datos.VelocidadCorrer
                + "), salto " + EfectosAPie.AlturaSalto(datos, e) + " m (base " + datos.AlturaSalto + ")");
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Brazos_ElGolpeSubeYCorrerYSaltoNoCambian(int nivel)
        {
            EfectosImplantes e = De(EspacioImplante.Brazos, nivel);
            Assert.Greater(e.multiplicadorGolpeAPie, 1f);
            Assert.AreEqual(datos.DanioGolpe * e.multiplicadorGolpeAPie, EfectosAPie.DanioGolpe(datos, e), Tolerancia);
            Assert.AreEqual(datos.VelocidadCorrer, EfectosAPie.VelocidadCorrer(datos, e), Tolerancia);
            Assert.AreEqual(datos.AlturaSalto, EfectosAPie.AlturaSalto(datos, e), Tolerancia);
            Debug.Log("[Prueba] T-192 brazos " + nivel + ": golpe " + EfectosAPie.DanioGolpe(datos, e) + " (base " + datos.DanioGolpe + ")");
        }

        [Test]
        public void PiernasYBrazosJuntos_SeCombinan()
        {
            EfectosImplantes e = EfectosImplantes.Combinar(De(EspacioImplante.Piernas, 2), De(EspacioImplante.Brazos, 2));
            Assert.AreEqual(datos.DanioGolpe * De(EspacioImplante.Brazos, 2).multiplicadorGolpeAPie, EfectosAPie.DanioGolpe(datos, e), Tolerancia);
            Assert.AreEqual(datos.AlturaSalto * De(EspacioImplante.Piernas, 2).multiplicadorSalto, EfectosAPie.AlturaSalto(datos, e), Tolerancia);
        }

        [Test]
        public void SeguidorSinServicio_EfectosNingunoYNivelesCero()
        {
            Registros.LimpiarTodos();
            var seguidor = new SeguidorImplantes();
            seguidor.Activar();
            Assert.AreEqual(1f, seguidor.Efectos.multiplicadorCorrer);
            Assert.AreEqual(0, seguidor.Nivel(EspacioImplante.Brazos));
            seguidor.Desactivar();
        }

        [Test]
        public void Ninguno_T430_NadaCambiaEnApuntadoRetrocesoRodadaYRecarga()
        {
            EfectosImplantes e = EfectosImplantes.Ninguno;
            Assert.AreEqual(1f, EfectosAPie.FactorAlcanceApuntado(e), Tolerancia);
            Assert.AreEqual(1f, EfectosAPie.FactorRetroceso(e), Tolerancia);
            Assert.AreEqual(1f, EfectosAPie.FactorRecarga(e), Tolerancia);
            Assert.AreEqual(2f, EfectosAPie.EnfriamientoRodada(2f, e), Tolerancia);
        }

        [TestCase(EspacioImplante.Ojos)]
        [TestCase(EspacioImplante.Brazos)]
        [TestCase(EspacioImplante.Piernas)]
        [TestCase(EspacioImplante.SistemaNervioso)]
        public void T430_CadaImplante_TocaSoloSuEfectoAPie(EspacioImplante espacio)
        {
            for (int nivel = 1; nivel <= 2; nivel++)
            {
                EfectosImplantes e = De(espacio, nivel);
                Assert.AreEqual(espacio == EspacioImplante.Ojos, EfectosAPie.FactorAlcanceApuntado(e) > 1f, espacio + " alcance");
                Assert.AreEqual(espacio == EspacioImplante.Brazos, EfectosAPie.FactorRetroceso(e) < 1f, espacio + " retroceso");
                Assert.AreEqual(espacio == EspacioImplante.Piernas, EfectosAPie.EnfriamientoRodada(2f, e) < 2f, espacio + " rodada");
                Assert.AreEqual(espacio == EspacioImplante.SistemaNervioso, EfectosAPie.FactorRecarga(e) < 1f, espacio + " recarga");
            }
        }

        [TestCase(EspacioImplante.Ojos)]
        [TestCase(EspacioImplante.Brazos)]
        [TestCase(EspacioImplante.Piernas)]
        [TestCase(EspacioImplante.SistemaNervioso)]
        public void T430_ElNivel2_EsMejorQueElNivel1(EspacioImplante espacio)
        {
            EfectosImplantes a = De(espacio, 1);
            EfectosImplantes b = De(espacio, 2);
            Assert.GreaterOrEqual(EfectosAPie.FactorAlcanceApuntado(b), EfectosAPie.FactorAlcanceApuntado(a));
            Assert.LessOrEqual(EfectosAPie.FactorRetroceso(b), EfectosAPie.FactorRetroceso(a));
            Assert.LessOrEqual(EfectosAPie.EnfriamientoRodada(2f, b), EfectosAPie.EnfriamientoRodada(2f, a));
            Assert.LessOrEqual(EfectosAPie.FactorRecarga(b), EfectosAPie.FactorRecarga(a));
        }

        [Test]
        public void T430_LosCuatroJuntos_SeCombinanYNuncaPasanDeLos90PorCiento()
        {
            EfectosImplantes todos = EfectosImplantes.Ninguno;
            foreach (EspacioImplante espacio in new[] { EspacioImplante.Ojos, EspacioImplante.Brazos, EspacioImplante.Piernas, EspacioImplante.SistemaNervioso })
            {
                todos = EfectosImplantes.Combinar(todos, De(espacio, 2));
            }
            Assert.Greater(EfectosAPie.FactorAlcanceApuntado(todos), 1f);
            Assert.Less(EfectosAPie.FactorRetroceso(todos), 1f);
            Assert.Less(EfectosAPie.FactorRecarga(todos), 1f);
            var enorme = new EfectosImplantes { reduccionRetroceso = 5f, reduccionRecarga = 5f, reduccionEnfriamientoRodada = 5f };
            Assert.AreEqual(0.1f, EfectosAPie.FactorRetroceso(enorme), Tolerancia);
            Assert.AreEqual(0.1f, EfectosAPie.FactorRecarga(enorme), Tolerancia);
            Assert.AreEqual(0.2f, EfectosAPie.EnfriamientoRodada(2f, enorme), Tolerancia);
        }

        [Test]
        public void T430_Sinapsis_LaRecargaDelCargadorDuraMenos()
        {
            var cargador = new MechaCyberpunk.Personaje.Combate.CargadorPistola(8, 0.1f, 2f);
            cargador.IntentarDisparar();
            cargador.FactorRecarga = EfectosAPie.FactorRecarga(De(EspacioImplante.SistemaNervioso, 2));
            cargador.EmpezarRecarga();
            cargador.Avanzar(2f * cargador.FactorRecarga + 0.01f);
            Assert.IsFalse(cargador.Recargando, "Con SINAPSIS la recarga termina antes de los 2 s base");
        }
    }
}
