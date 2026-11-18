using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1344: la matemática de las bisagras (signos) y que el componente sin bisagras no falla.</summary>
    public class PuertasCarroT1344Pruebas
    {
        [Test]
        public void PuertaIzquierda_Abierta_DejaSuBordeTraseroMasANegativo()
        {
            // El borde trasero de la puerta está 1 m detrás de la bisagra (0, 0, -1).
            Vector3 borde = PuertasCarro.RotacionAbierta(Quaternion.identity, 70f, Vector3.up) * new Vector3(0f, 0f, -1f);
            Assert.Less(borde.x, -0.9f, "Abre hacia fuera del lado izquierdo (x negativa)");
        }

        [Test]
        public void PuertaDerecha_Abierta_DejaSuBordeTraseroEnXPositiva()
        {
            Vector3 borde = PuertasCarro.RotacionAbierta(Quaternion.identity, -70f, Vector3.up) * new Vector3(0f, 0f, -1f);
            Assert.Greater(borde.x, 0.9f);
        }

        [Test]
        public void Cofre_Abierto_LevantaElBordeDelantero()
        {
            Vector3 borde = PuertasCarro.RotacionAbierta(Quaternion.identity, -55f, Vector3.right) * new Vector3(0f, 0f, 1f);
            Assert.Greater(borde.y, 0.7f, "El borde delantero sube");
        }

        [Test]
        public void RotacionAbierta_ParteDeLaRotacionCerrada()
        {
            Quaternion cerrada = Quaternion.Euler(0f, 30f, 0f);
            Quaternion abierta = PuertasCarro.RotacionAbierta(cerrada, 40f, Vector3.up);
            Assert.AreEqual(40f, Quaternion.Angle(cerrada, abierta), 0.01f);
        }

        [Test]
        public void SinBisagras_NoFalla()
        {
            GameObject carro = new GameObject("CarroSinPuertas");
            PuertasCarro puertas = carro.AddComponent<PuertasCarro>();
            Assert.DoesNotThrow(() =>
            {
                puertas.AbrirPuerta(true);
                puertas.AbrirPuerta(false);
                puertas.AbrirCofre();
                puertas.CerrarPuerta(true);
                puertas.CerrarCofre();
            });
            Assert.IsTrue(puertas.LadoIzquierdoMasCercano(carro.transform.TransformPoint(new Vector3(-1f, 0f, 0f))));
            Assert.IsFalse(puertas.LadoIzquierdoMasCercano(carro.transform.TransformPoint(new Vector3(1f, 0f, 0f))));
            Object.DestroyImmediate(carro);
        }
    }
}
