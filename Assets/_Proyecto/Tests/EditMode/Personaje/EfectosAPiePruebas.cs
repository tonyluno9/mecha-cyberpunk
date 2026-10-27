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
    }
}
