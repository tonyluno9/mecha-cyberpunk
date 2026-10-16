using System;
using System.Collections.Generic;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-082: el parpadeo de neón viejo (CalculoParpadeo) es determinista, irregular y apaga entre 5 % y 30 % del tiempo.</summary>
    public class CalculoParpadeoPruebas
    {
        private const float Paso = 1f / 60f;
        private const float Minuto = 60f;
        private const float DiezSegundos = 10f;
        private const float FraccionMinima = 0.05f;
        private const float FraccionMaxima = 0.30f;
        private const int Semillas = 40;
        private const float HolguraPaso = 0.02f;

        private static CalculoParpadeo Nuevo(int semilla)
        {
            Random azar = new Random(semilla);
            return new CalculoParpadeo(() => (float)azar.NextDouble(), ParametrosParpadeo.PorDefecto);
        }

        private static List<bool> Secuencia(int semilla, float segundos)
        {
            CalculoParpadeo calculo = Nuevo(semilla);
            List<bool> estados = new List<bool>();
            for (float t = 0f; t < segundos; t += Paso)
            {
                estados.Add(calculo.Avanzar(Paso));
            }
            return estados;
        }

        [Test]
        public void Avanzar_MismaSemilla_DaLaMismaSecuencia()
        {
            Assert.AreEqual(Secuencia(7, Minuto), Secuencia(7, Minuto));
        }

        [Test]
        public void Avanzar_SemillasDistintas_DanSecuenciasDistintas()
        {
            CollectionAssert.AreNotEqual(Secuencia(1, Minuto), Secuencia(2, Minuto));
        }

        [Test]
        public void Avanzar_En60Segundos_ApagaEntre5Y30PorCientoDelTiempo()
        {
            for (int semilla = 1; semilla <= Semillas; semilla++)
            {
                List<bool> estados = Secuencia(semilla, Minuto);
                int apagados = estados.FindAll(prendido => !prendido).Count;
                float fraccion = apagados / (float)estados.Count;
                Assert.GreaterOrEqual(fraccion, FraccionMinima, "semilla " + semilla + ": se apaga muy poco");
                Assert.LessOrEqual(fraccion, FraccionMaxima, "semilla " + semilla + ": se apaga demasiado");
            }
        }

        [Test]
        public void Avanzar_En10Segundos_SiempreCambiaAlMenosUnaVez()
        {
            for (int semilla = 1; semilla <= Semillas; semilla++)
            {
                List<bool> estados = Secuencia(semilla, DiezSegundos);
                Assert.IsTrue(estados.Contains(false), "semilla " + semilla + ": ningún apagón en 10 s");
            }
        }

        [Test]
        public void Avanzar_CadaApagon_DuraLoQueDiceLaConfig()
        {
            ParametrosParpadeo p = ParametrosParpadeo.PorDefecto;
            for (int semilla = 1; semilla <= Semillas; semilla++)
            {
                int corrida = 0;
                foreach (bool prendido in Secuencia(semilla, Minuto))
                {
                    if (!prendido)
                    {
                        corrida++;
                        continue;
                    }
                    if (corrida > 0)
                    {
                        float duracion = corrida * Paso;
                        Assert.GreaterOrEqual(duracion, p.ApagadoMinimo - HolguraPaso, "apagón muy corto");
                        Assert.LessOrEqual(duracion, p.ApagadoMaximo + HolguraPaso, "apagón muy largo");
                        corrida = 0;
                    }
                }
            }
        }

        [Test]
        public void Avanzar_ConDtGrande_CruzaVariasFasesSinFallar()
        {
            CalculoParpadeo calculo = Nuevo(3);
            Assert.DoesNotThrow(() => calculo.Avanzar(Minuto));
            Assert.DoesNotThrow(() => calculo.Avanzar(0f));
        }

        [Test]
        public void Avanzar_DtNegativo_Lanza()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Nuevo(1).Avanzar(-0.1f));
        }

        [Test]
        public void Constructor_SinAzar_Lanza()
        {
            Assert.Throws<ArgumentNullException>(() => new CalculoParpadeo(null, ParametrosParpadeo.PorDefecto));
        }

        [Test]
        public void EstaEncendido_AlEmpezar_EsVerdadero()
        {
            Assert.IsTrue(Nuevo(5).EstaEncendido);
        }
    }
}
