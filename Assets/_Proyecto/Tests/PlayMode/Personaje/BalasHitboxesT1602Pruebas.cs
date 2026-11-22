using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1602: el disparo (misma regla y máscara que el juego) le pega al personaje junto a un poste, atraviesa lo hueco y se detiene en paredes.</summary>
    public class BalasHitboxesT1602Pruebas
    {
        private static readonly Vector3 Origen = new Vector3(5000f, 0f, 5000f);
        private GameObject raiz;
        private GameObject personaje;

        [SetUp]
        public void Preparar()
        {
            raiz = new GameObject("PruebaT1602");
            raiz.transform.position = Origen;
            // Poste: cápsula delgada a un lado del personaje (x = 0.5), a la misma distancia.
            GameObject poste = Crear("Poste", new Vector3(0.5f, 1.5f, 10f));
            CapsuleCollider cap = poste.AddComponent<CapsuleCollider>();
            cap.radius = 0.1f;
            cap.height = 3f;
            // Personaje con cápsula de cuerpo, pegado al poste.
            personaje = Crear("Personaje", new Vector3(0f, 0.9f, 10f));
            CapsuleCollider cuerpo = personaje.AddComponent<CapsuleCollider>();
            cuerpo.radius = 0.3f;
            cuerpo.height = 1.8f;
        }

        [TearDown]
        public void Limpiar()
        {
            Object.Destroy(raiz);
        }

        private GameObject Crear(string nombre, Vector3 local)
        {
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(raiz.transform, false);
            g.transform.localPosition = local;
            return g;
        }

        private bool Disparar(out RaycastHit impacto)
        {
            // Raíz propia = otra cosa: el tirador no está en la escena de la prueba.
            return ReglaImpactoBala.BuscarPrimero(Origen + Vector3.up, Vector3.forward, 50f, null, null, ReglaImpactoBala.MascaraBala, out impacto);
        }

        [UnityTest]
        public IEnumerator LaCapaNoDetieneBalasExiste()
        {
            yield return null;
            Assert.GreaterOrEqual(LayerMask.NameToLayer(ReglaImpactoBala.NombreCapaNoDetieneBalas), 0, "Falta la capa NoDetieneBalas en TagManager.");
        }

        [UnityTest]
        public IEnumerator ElDisparoAlPersonajePegadoAlPosteLePega()
        {
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(Disparar(out RaycastHit impacto));
            Assert.AreEqual(personaje.transform, impacto.collider.transform);
        }

        [UnityTest]
        public IEnumerator UnToldoEnNoDetieneBalasDelanteNoImpide()
        {
            GameObject toldo = Crear("Toldo", new Vector3(0f, 1.2f, 5f));
            BoxCollider caja = toldo.AddComponent<BoxCollider>();
            caja.size = new Vector3(3f, 3f, 0.05f);
            toldo.layer = LayerMask.NameToLayer(ReglaImpactoBala.NombreCapaNoDetieneBalas);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(Disparar(out RaycastHit impacto));
            Assert.AreEqual(personaje.transform, impacto.collider.transform, "El toldo hueco no debe detener la bala.");
        }

        [UnityTest]
        public IEnumerator UnaParedDelanteDetieneLaBala()
        {
            GameObject pared = Crear("Pared", new Vector3(0f, 1.5f, 5f));
            pared.AddComponent<BoxCollider>().size = new Vector3(6f, 3f, 0.3f);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(Disparar(out RaycastHit impacto));
            Assert.AreEqual(pared.transform, impacto.collider.transform, "La pared debe detener la bala.");
        }
    }
}
