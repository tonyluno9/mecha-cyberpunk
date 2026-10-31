using System;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-446: la patrulla elige puntos cerca del hogar, espera al llegar, cambia de punto si se traba y con radio 0 no patrulla.</summary>
    public class CalculoPatrullaPandilleroPruebas
    {
        private const float Radio = 6f;
        private const float Paso = 0.1f;
        private const float Velocidad = 2f;
        private static readonly Vector3 Hogar = new Vector3(10f, 0f, -4f);

        private static CalculoPatrullaPandillero Crear(int semilla = 3, float radio = Radio, float espera = 2f)
        {
            System.Random azar = new System.Random(semilla);
            return new CalculoPatrullaPandillero(radio, espera, espera, 0.3f, 2f, () => (float)azar.NextDouble());
        }

        [Test]
        public void Avanzar_ConRadioCero_NoPatrulla()
        {
            CalculoPatrullaPandillero patrulla = Crear(radio: 0f);
            Assert.IsFalse(patrulla.Activa);
            Assert.AreEqual(Vector3.zero, patrulla.Avanzar(Paso, Hogar, Hogar));
        }

        [Test]
        public void Avanzar_ElPrimerDestino_QuedaDentroDelRadioDelHogar()
        {
            for (int semilla = 0; semilla < 25; semilla++)
            {
                CalculoPatrullaPandillero patrulla = Crear(semilla);
                Vector3 direccion = patrulla.Avanzar(Paso, Hogar, Hogar);
                Assert.IsTrue(patrulla.HayDestino);
                Vector3 desde = patrulla.Destino - Hogar;
                desde.y = 0f;
                Assert.LessOrEqual(desde.magnitude, Radio + 0.001f, "Semilla " + semilla);
                Assert.Greater(desde.magnitude, 0.3f * Radio - 0.001f, "No se queda pegado al hogar");
                Assert.AreEqual(1f, direccion.magnitude, 0.001f, "La dirección es unitaria");
            }
        }

        [Test]
        public void Avanzar_AlLlegar_EsperaYLuegoElige_OtroPunto()
        {
            CalculoPatrullaPandillero patrulla = Crear(espera: 2f);
            patrulla.Avanzar(Paso, Hogar, Hogar);
            Vector3 primero = patrulla.Destino;
            Vector3 posicion = primero;
            Assert.AreEqual(Vector3.zero, patrulla.Avanzar(Paso, posicion, Hogar), "Llegó: se queda quieto");
            Assert.IsTrue(patrulla.Esperando);
            for (int i = 0; i < 15; i++)
            {
                Assert.AreEqual(Vector3.zero, patrulla.Avanzar(Paso, posicion, Hogar), "Sigue esperando");
            }
            for (int i = 0; i < 10; i++)
            {
                patrulla.Avanzar(Paso, posicion, Hogar);
            }
            Assert.IsFalse(patrulla.Esperando, "Pasados los 2 s deja de esperar");
            Assert.IsTrue(patrulla.HayDestino);
            Assert.AreNotEqual(primero, patrulla.Destino, "Elige otro punto");
        }

        [Test]
        public void Avanzar_CaminandoSinParar_LlegaAlDestino()
        {
            CalculoPatrullaPandillero patrulla = Crear();
            Vector3 posicion = Hogar;
            int pasos = 0;
            while (pasos < 1000)
            {
                Vector3 dir = patrulla.Avanzar(Paso, posicion, Hogar);
                if (dir == Vector3.zero)
                {
                    break;
                }
                posicion += dir * Velocidad * Paso;
                pasos++;
            }
            Assert.Less(pasos, 1000, "Llega en un tiempo razonable");
            Assert.IsTrue(patrulla.Esperando, "Al llegar espera");
        }

        [Test]
        public void Avanzar_SiNoAvanza_CambiaDePuntoDespuesDelAtasco()
        {
            CalculoPatrullaPandillero patrulla = Crear();
            Vector3 posicion = Hogar;
            patrulla.Avanzar(Paso, posicion, Hogar);
            Vector3 primero = patrulla.Destino;
            for (int i = 0; i < 50; i++)
            {
                patrulla.Avanzar(Paso, posicion, Hogar);
            }
            Assert.AreNotEqual(primero, patrulla.Destino, "Trabado en una pared, elige otro punto");
        }

        [Test]
        public void Reiniciar_OlvidaElPuntoYLaEspera()
        {
            CalculoPatrullaPandillero patrulla = Crear();
            patrulla.Avanzar(Paso, Hogar, Hogar);
            patrulla.Reiniciar();
            Assert.IsFalse(patrulla.HayDestino);
            Assert.IsFalse(patrulla.Esperando);
        }

        [Test]
        public void Constructor_SinAzar_Lanza()
        {
            Assert.Throws<ArgumentNullException>(() => new CalculoPatrullaPandillero(Radio, 1f, 2f, 0.3f, 2f, null));
        }
    }
}
