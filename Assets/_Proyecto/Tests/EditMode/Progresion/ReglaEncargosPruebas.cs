using MechaCyberpunk.Progresion.Prologo.Encargos;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    public class ReglaEncargosPruebas
    {
        [Test]
        public void ProximoRotaEntreLosTiposDisponibles()
        {
            ReglaEncargos regla = new ReglaEncargos();
            Assert.IsTrue(regla.TryProximo(out TipoEncargo primero));
            Assert.AreEqual(TipoEncargo.Cuota, primero);
            regla.Terminar(TipoEncargo.Cuota, true);
            Assert.IsTrue(regla.TryProximo(out TipoEncargo segundo));
            Assert.AreEqual(TipoEncargo.Defender, segundo);
            regla.Terminar(TipoEncargo.Defender, false);
            Assert.IsTrue(regla.TryProximo(out TipoEncargo tercero));
            Assert.AreEqual(TipoEncargo.Robar, tercero);
        }

        [Test]
        public void EnfriamientoBajaConElTiempoYNoDejaRepetir()
        {
            ReglaEncargos regla = new ReglaEncargos();
            regla.Terminar(TipoEncargo.Cuota, true);
            Assert.IsFalse(regla.Disponible(TipoEncargo.Cuota));
            Assert.AreEqual(ReglaEncargos.EnfriamientoExito, regla.Espera(TipoEncargo.Cuota), 0.001f);
            regla.Avanzar(ReglaEncargos.EnfriamientoExito - 1f);
            Assert.IsFalse(regla.Disponible(TipoEncargo.Cuota));
            regla.Avanzar(1f);
            Assert.IsTrue(regla.Disponible(TipoEncargo.Cuota));
            Assert.AreEqual(1, regla.Hechos(TipoEncargo.Cuota));
        }

        [Test]
        public void FalloEnfriaMenosYNoCuentaComoHecho()
        {
            ReglaEncargos regla = new ReglaEncargos();
            regla.Terminar(TipoEncargo.Robar, false);
            Assert.AreEqual(ReglaEncargos.EnfriamientoFallo, regla.Espera(TipoEncargo.Robar), 0.001f);
            Assert.AreEqual(0, regla.Hechos(TipoEncargo.Robar));
        }

        [Test]
        public void SinEncargosCuandoTodosEsperan()
        {
            ReglaEncargos regla = new ReglaEncargos();
            regla.Terminar(TipoEncargo.Cuota, true);
            regla.Terminar(TipoEncargo.Defender, true);
            regla.Terminar(TipoEncargo.Robar, false);
            Assert.IsFalse(regla.TryProximo(out _));
            Assert.AreEqual(ReglaEncargos.EnfriamientoFallo, regla.EsperaMinima(), 0.001f);
        }

        [Test]
        public void SoloSeOfreceConLaHistoriaLibreYSinOtroEncargo()
        {
            Assert.IsTrue(ReglaEncargos.Permitido(true, false));
            Assert.IsFalse(ReglaEncargos.Permitido(false, false));
            Assert.IsFalse(ReglaEncargos.Permitido(true, true));
        }

        [Test]
        public void SeResisteAVecesYSiempreIgual()
        {
            int resisten = 0;
            for (int hechas = 0; hechas < 3; hechas++)
            {
                for (int local = 0; local < 3; local++)
                {
                    bool a = ReglaEncargos.SeResiste(hechas, local);
                    Assert.AreEqual(a, ReglaEncargos.SeResiste(hechas, local));
                    resisten += a ? 1 : 0;
                }
            }
            Assert.AreEqual(3, resisten, "Una de cada tres veces");
        }

        [Test]
        public void GuardarYCargarConservaEsperaHechosYRotacion()
        {
            ReglaEncargos regla = new ReglaEncargos();
            regla.Terminar(TipoEncargo.Cuota, true);
            regla.Avanzar(100f);
            ReglaEncargos otra = new ReglaEncargos();
            Assert.IsTrue(otra.Cargar(regla.Guardar()));
            Assert.AreEqual(regla.Espera(TipoEncargo.Cuota), otra.Espera(TipoEncargo.Cuota), 0.001f);
            Assert.AreEqual(1, otra.Hechos(TipoEncargo.Cuota));
            otra.TryProximo(out TipoEncargo t);
            Assert.AreEqual(TipoEncargo.Defender, t);
        }

        [Test]
        public void CargarBasuraNoCambiaNada()
        {
            ReglaEncargos regla = new ReglaEncargos();
            regla.Terminar(TipoEncargo.Cuota, true);
            Assert.IsFalse(regla.Cargar("esto no es json"));
            Assert.IsFalse(regla.Cargar(string.Empty));
            Assert.IsFalse(regla.Disponible(TipoEncargo.Cuota));
        }

        [Test]
        public void PremiosPositivosYObjetivosCaben()
        {
            foreach (TipoEncargo tipo in System.Enum.GetValues(typeof(TipoEncargo)))
            {
                Assert.Greater(ReglaEncargos.Premio(tipo), 0);
                Assert.Greater(ReglaEncargos.RespetoUnidades(tipo), 0);
            }
            foreach (string texto in DirectorEncargos.TextosObjetivo)
            {
                Assert.LessOrEqual(texto.Length, DirectorEncargos.LargoMaximoObjetivo, texto);
            }
        }
    }
}
