using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-632 y T-669: la ruta GPS del minimapa se la pide a quien trace rutas por las calles (IRutaCalles) y, sin nadie, va en recta.</summary>
    public class CalculoRutaGpsPruebas
    {
        private sealed class RutaEnLDoble : IRutaCalles
        {
            public int Llamadas;

            public List<Vector3> Calcular(Vector3 origen, Vector3 destino)
            {
                Llamadas++;
                return new List<Vector3> { origen, new Vector3(origen.x, 0f, destino.z), destino };
            }
        }

        private sealed class ProveedorVacio : IRutaCalles
        {
            public List<Vector3> Calcular(Vector3 origen, Vector3 destino) => new List<Vector3>();
        }

        [SetUp]
        public void Preparar() => Registros.LimpiarTodos();

        [TearDown]
        public void Limpiar() => Registros.LimpiarTodos();

        [Test]
        public void Ruta_ConUnProveedorRegistrado_EsLaQueElTraza()
        {
            RutaEnLDoble calles = new RutaEnLDoble();
            Registro<IRutaCalles>.Agregar(calles);
            Vector3 origen = new Vector3(-47f, 3f, -60f);
            Vector3 destino = new Vector3(47f, 2f, 60f);
            List<Vector3> ruta = CalculoRutaGps.Ruta(origen, destino);
            Assert.AreEqual(1, calles.Llamadas);
            Assert.AreEqual(3, ruta.Count);
            Assert.AreEqual(new Vector3(-47f, 0f, -60f), ruta[0], "Empieza en el origen, a la altura 0");
            Assert.AreEqual(new Vector3(-47f, 0f, 60f), ruta[1], "Pasa por la esquina que trazó el proveedor");
            Assert.AreEqual(new Vector3(47f, 0f, 60f), ruta[2], "Termina en el destino, a la altura 0");
        }

        [Test]
        public void Ruta_SinProveedor_EsUnaRecta()
        {
            List<Vector3> ruta = CalculoRutaGps.Ruta(new Vector3(0f, 0f, 0f), new Vector3(30f, 5f, 40f));
            Assert.AreEqual(2, ruta.Count);
            Assert.AreEqual(50f, CalculoRutaGps.Largo(ruta), 0.01f);
        }

        [Test]
        public void Ruta_SiElProveedorNoEncuentraCamino_CaeAUnaRecta()
        {
            Registro<IRutaCalles>.Agregar(new ProveedorVacio());
            List<Vector3> ruta = CalculoRutaGps.Ruta(Vector3.zero, new Vector3(0f, 0f, 10f));
            Assert.AreEqual(2, ruta.Count);
        }
    }
}
