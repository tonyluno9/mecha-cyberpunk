using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1324: reglas de apuntar con balas, del daño del rayo de plasma y de la búsqueda del primer impacto (el bug de los policías).</summary>
    public class ArmasT1324EditPruebas
    {
        [Test]
        public void Bloquea_SinBalas_NoDejaApuntar()
        {
            Assert.IsTrue(ReglaApuntarConBalas.Bloquea(0, false));
        }

        [Test]
        public void Bloquea_ConBalas_DejaApuntar()
        {
            Assert.IsFalse(ReglaApuntarConBalas.Bloquea(1, false));
        }

        [Test]
        public void Bloquea_RayoDePlasmaEnMarcha_NoSeCorta()
        {
            Assert.IsFalse(ReglaApuntarConBalas.Bloquea(0, true));
        }

        [Test]
        public void Calcular_Persona_UnToqueLaMata()
        {
            float danio = ReglaDanioRayo.Calcular(40f, false, 60f, 3f, 20f);
            Assert.GreaterOrEqual(danio, 60f);
        }

        [Test]
        public void Calcular_Objeto_PegaMasFuerte()
        {
            Assert.AreEqual(120f, ReglaDanioRayo.Calcular(40f, true, 0f, 3f, 20f), 0.001f);
        }

        [Test]
        public void AtraviesaOtra_HastaElMaximo()
        {
            Assert.IsTrue(ReglaDanioRayo.AtraviesaOtra(2, 3));
            Assert.IsFalse(ReglaDanioRayo.AtraviesaOtra(3, 3));
        }

        [Test]
        public void BuscarPrimero_ConMasDeDieciseisColliders_DaElMasCercano()
        {
            var objetos = new System.Collections.Generic.List<GameObject>();
            try
            {
                // 30 delgados lejos y un blanco cercano: el buffer de 16 de antes podía no traer el blanco.
                for (int i = 0; i < 30; i++)
                {
                    objetos.Add(Caja("Lejos" + i, new Vector3(0f, 0f, 40f + i * 2f)));
                }
                GameObject blanco = Caja("Blanco", new Vector3(0f, 0f, 20f));
                objetos.Add(blanco);
                Physics.SyncTransforms();
                bool hay = ReglaImpactoBala.BuscarPrimero(Lejos, Vector3.forward, 150f, null, null, out RaycastHit mejor);
                Assert.IsTrue(hay);
                Assert.AreEqual("Blanco", mejor.collider.name);
            }
            finally
            {
                foreach (GameObject o in objetos)
                {
                    Object.DestroyImmediate(o);
                }
            }
        }

        [Test]
        public void BuscarPrimero_SaltaElColliderPropio()
        {
            GameObject propio = Caja("Propio", new Vector3(0f, 0f, 2f));
            GameObject blanco = Caja("Blanco", new Vector3(0f, 0f, 10f));
            try
            {
                Physics.SyncTransforms();
                bool hay = ReglaImpactoBala.BuscarPrimero(Lejos, Vector3.forward, 50f, propio.transform.root, null, out RaycastHit mejor);
                Assert.IsTrue(hay);
                Assert.AreEqual("Blanco", mejor.collider.name);
                Assert.AreEqual(9.5f, mejor.distance, 0.05f);
            }
            finally
            {
                Object.DestroyImmediate(propio);
                Object.DestroyImmediate(blanco);
            }
        }

        /// <summary>Lejos de cualquier escena abierta por otras pruebas (la ciudad tiene banquetas en el origen).</summary>
        private static readonly Vector3 Lejos = new Vector3(0f, 500f, 0f);

        private static GameObject Caja(string nombre, Vector3 posicion)
        {
            var o = new GameObject(nombre);
            o.transform.position = Lejos + posicion;
            o.AddComponent<BoxCollider>().size = new Vector3(2f, 2f, 1f);
            return o;
        }
    }
}
