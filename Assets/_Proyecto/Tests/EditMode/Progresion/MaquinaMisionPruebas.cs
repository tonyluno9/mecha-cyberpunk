using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>MaquinaMision (T-183): pasos, límite de tiempo y abandono.</summary>
    public class MaquinaMisionPruebas
    {
        private DatosMision mision;
        private MaquinaMision maquina;
        private int terminadas;
        private bool ultimoExito;
        private MotivoFalloMision ultimoMotivo;
        private int pasosAvisados;

        [SetUp]
        public void SetUp()
        {
            maquina = new MaquinaMision();
            terminadas = 0;
            pasosAvisados = 0;
            maquina.Terminada += (exito, motivo) => { terminadas++; ultimoExito = exito; ultimoMotivo = motivo; };
            maquina.PasoCambio += () => pasosAvisados++;
        }

        [TearDown]
        public void TearDown()
        {
            if (mision != null) Object.DestroyImmediate(mision);
        }

        private DatosMision Crear(int pasos, float limite = 0f)
        {
            var lista = new PasoMision[pasos];
            for (int i = 0; i < pasos; i++) lista[i] = new PasoMision(TipoPaso.IrA, "Mision_Destino_P" + i, "Paso " + i);
            mision = DatosMision.Crear("m", "M", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche, lista, limiteSegundos: limite);
            return mision;
        }

        [Test]
        public void Cumplir_TresPasos_SeCumplenEnOrdenYAlUltimoTerminaConExito()
        {
            Assert.IsTrue(maquina.Empezar(Crear(3)));
            Assert.AreEqual(0, maquina.IndicePaso);
            Assert.AreEqual("Mision_Destino_P0", maquina.PasoActual.IdPunto);

            maquina.Cumplir();
            Assert.AreEqual(1, maquina.IndicePaso);
            Assert.AreEqual("Mision_Destino_P1", maquina.PasoActual.IdPunto);
            maquina.Cumplir();
            Assert.AreEqual(2, maquina.IndicePaso);
            Assert.AreEqual("Mision_Destino_P2", maquina.PasoActual.IdPunto);
            Assert.AreEqual(0, terminadas);
            Assert.AreEqual(2, pasosAvisados, "PasoCambio avisa en cada cambio, no al terminar");

            maquina.Cumplir();
            Assert.IsFalse(maquina.Activa);
            Assert.IsNull(maquina.PasoActual);
            Assert.AreEqual(1, terminadas);
            Assert.IsTrue(ultimoExito);
            Assert.AreEqual(MotivoFalloMision.Ninguno, ultimoMotivo);
        }

        [Test]
        public void Cumplir_DespuesDeTerminar_NoHaceNada()
        {
            maquina.Empezar(Crear(1));
            maquina.Cumplir();
            Assert.AreEqual(1, terminadas);
            maquina.Cumplir();
            maquina.Cumplir();
            Assert.AreEqual(1, terminadas);
            Assert.AreEqual(0, pasosAvisados);
            Assert.IsFalse(maquina.Activa);
        }

        [Test]
        public void Avanzar_LimiteDe150s_FallaEnLos150sYNoAntes()
        {
            maquina.Empezar(Crear(2, 150f));
            maquina.Avanzar(149f);
            maquina.Avanzar(0f);
            Assert.IsTrue(maquina.Activa);
            Assert.AreEqual(0, terminadas);
            Assert.AreEqual(1f, maquina.Restante, 0.001f);
            maquina.Avanzar(1f);
            Assert.IsFalse(maquina.Activa);
            Assert.AreEqual(1, terminadas);
            Assert.IsFalse(ultimoExito);
            Assert.AreEqual(MotivoFalloMision.Tiempo, ultimoMotivo);
            maquina.Avanzar(50f);
            Assert.AreEqual(1, terminadas, "no avisa otra vez");
        }

        [Test]
        public void Avanzar_SinLimite_NuncaFalla()
        {
            maquina.Empezar(Crear(1, 0f));
            maquina.Avanzar(100000f);
            Assert.IsTrue(maquina.Activa);
            Assert.AreEqual(0f, maquina.Restante);
        }

        [Test]
        public void Fallar_Abandono_AvisaUnaSolaVez()
        {
            maquina.Empezar(Crear(2));
            maquina.Fallar(MotivoFalloMision.Abandono);
            maquina.Fallar(MotivoFalloMision.Abandono);
            maquina.Fallar(MotivoFalloMision.KO);
            Assert.AreEqual(1, terminadas);
            Assert.IsFalse(ultimoExito);
            Assert.AreEqual(MotivoFalloMision.Abandono, ultimoMotivo);
        }

        [Test]
        public void Fallar_KO_AvisaKO()
        {
            maquina.Empezar(Crear(2));
            maquina.Fallar(MotivoFalloMision.KO);
            Assert.AreEqual(MotivoFalloMision.KO, ultimoMotivo);
        }

        [Test]
        public void Empezar_ConUnaActivaOSinPasos_DaFalse()
        {
            Assert.IsTrue(maquina.Empezar(Crear(2)));
            Assert.IsFalse(maquina.Empezar(Crear(1)), "ya hay una activa");
            maquina.Fallar(MotivoFalloMision.Abandono);
            var vacia = DatosMision.Crear("v", "V", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche, new PasoMision[0]);
            Assert.IsFalse(maquina.Empezar(vacia));
            Object.DestroyImmediate(vacia);
            Assert.IsTrue(maquina.Empezar(Crear(1)), "tras terminar, se puede empezar otra");
        }
    }
}
