using MechaCyberpunk.Casino.Meseras;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Casino
{
    public class CalculoRondaMeseraPruebas
    {
        private static readonly Vector3[] Puntos = { Vector3.zero, new Vector3(2f, 0f, 0f), new Vector3(2f, 0f, 2f) };
        private const float Velocidad = 1f;
        private const float Pausa = 1f;
        private const float Paso = 0.1f;

        private static void Correr(CalculoRondaMesera r, float segundos)
        {
            for (float t = 0f; t < segundos - Paso * 0.5f; t += Paso)
            {
                r.Avanzar(Paso);
            }
        }

        [Test]
        public void Avanzar_ConEsperaInicial_NoSeMueveHastaQuePase()
        {
            CalculoRondaMesera r = new CalculoRondaMesera(Puntos, Velocidad, Pausa, 1f);
            Correr(r, 0.5f);
            Assert.IsFalse(r.EstaCaminando);
            Assert.AreEqual(Vector3.zero, r.Posicion);
        }

        [Test]
        public void Avanzar_AlLlegarAUnPunto_SeDetieneLaPausa()
        {
            CalculoRondaMesera r = new CalculoRondaMesera(Puntos, Velocidad, Pausa, 0f);
            Correr(r, 2.2f);
            Assert.AreEqual(Puntos[1].x, r.Posicion.x, 0.001f);
            Assert.IsFalse(r.EstaCaminando);
            Correr(r, 1.1f);
            Assert.IsTrue(r.EstaCaminando);
        }

        [Test]
        public void Avanzar_AlLlegarAlUltimoPunto_RegresaPorElMismoCamino()
        {
            CalculoRondaMesera r = new CalculoRondaMesera(Puntos, Velocidad, 0f, 0f);
            Correr(r, 4.5f);
            Assert.AreEqual(Vector3.forward * -1f, r.Direccion);
            Assert.Less(r.Posicion.z, Puntos[2].z);
        }

        [Test]
        public void Direccion_SiempreSinComponenteVertical()
        {
            Vector3[] conDesnivel = { Vector3.zero, new Vector3(1f, 1f, 0f) };
            CalculoRondaMesera r = new CalculoRondaMesera(conDesnivel, Velocidad, Pausa, 0f);
            Assert.AreEqual(0f, r.Direccion.y);
        }
    }
}
