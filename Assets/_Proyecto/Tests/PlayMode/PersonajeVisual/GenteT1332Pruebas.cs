using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-1332: los que platican se miran de frente y la gente de banqueta recibe daño y muere cayendo.</summary>
    public class GenteT1332Pruebas
    {
        private readonly System.Collections.Generic.List<GameObject> creados = new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject g in creados)
            {
                if (g != null) { Object.Destroy(g); }
            }
            creados.Clear();
        }

        private GenteDeBanqueta Persona(string nombre, Vector3 pos, Quaternion giro, ModoGente modo)
        {
            GameObject g = new GameObject(nombre);
            g.transform.SetPositionAndRotation(pos, giro);
            creados.Add(g);
            GenteDeBanqueta gente = g.AddComponent<GenteDeBanqueta>();
            gente.Configurar(modo, 0.3f, 1f, null, Vector3.zero, Vector3.zero, 1f, 0f, null);
            return gente;
        }

        [UnityTest]
        public IEnumerator Platicar_DosDeEspaldas_SeMiranDeFrente()
        {
            // A en el origen y B a 1.2 m en +X, los dos mirando hacia -Z (de espaldas a la pareja, de lado a lado).
            GenteDeBanqueta a = Persona("A", Vector3.zero, Quaternion.LookRotation(Vector3.back), ModoGente.Platicar);
            GenteDeBanqueta b = Persona("B", new Vector3(1.2f, 0f, 0f), Quaternion.LookRotation(Vector3.back), ModoGente.Platicar);
            yield return new WaitForSeconds(1.2f);
            Assert.AreSame(b, a.Companero, "A elige a B");
            Assert.AreSame(a, b.Companero, "B elige a A");
            Assert.Greater(Vector3.Dot(a.transform.forward, Vector3.right), 0.99f, "A mira hacia B");
            Assert.Greater(Vector3.Dot(b.transform.forward, Vector3.left), 0.99f, "B mira hacia A");
        }

        [UnityTest]
        public IEnumerator Platicar_Solo_NoGiraNiFalla()
        {
            Quaternion inicial = Quaternion.LookRotation(Vector3.back);
            GenteDeBanqueta a = Persona("Solo", Vector3.zero, inicial, ModoGente.Platicar);
            yield return null;
            yield return null;
            Assert.IsNull(a.Companero);
            Assert.Less(Quaternion.Angle(a.transform.rotation, inicial), 0.1f);
        }

        [UnityTest]
        public IEnumerator Danio_Disparo_BajaLaVidaYTieneCollider()
        {
            GenteDeBanqueta a = Persona("Herida", Vector3.zero, Quaternion.identity, ModoGente.Quieto);
            yield return null;
            VidaGente vida = a.GetComponent<VidaGente>();
            Assert.IsNotNull(vida, "Se agrega sola");
            Assert.IsNotNull(a.GetComponent<Collider>(), "Tiene collider para que las balas le peguen");
            IRecibeDanio recibe = a.GetComponent<IRecibeDanio>();
            float antes = vida.Vida;
            ResultadoDanio r = recibe.RecibirDanio(new InfoDanio(10f, TipoDanio.Disparo, Vector3.up, Vector3.forward, null));
            Assert.AreEqual(10f, r.DanioAplicado, 0.001f);
            Assert.AreEqual(antes - 10f, vida.Vida, 0.001f);
            Assert.IsFalse(vida.Muerta);
        }

        [UnityTest]
        public IEnumerator Danio_Letal_MuereCayendoYQuedaTirada()
        {
            GenteDeBanqueta a = Persona("Muerta", Vector3.zero, Quaternion.identity, ModoGente.Platicar);
            yield return null;
            VidaGente vida = a.GetComponent<VidaGente>();
            ResultadoDanio r = vida.RecibirDanio(new InfoDanio(999f, TipoDanio.Explosion, Vector3.up, Vector3.forward, null));
            Assert.IsTrue(r.CausoKO);
            Assert.IsTrue(vida.Muerta);
            Assert.IsFalse(a.enabled, "Deja de platicar");
            yield return new WaitForSeconds(0.8f);
            Assert.Less(a.transform.up.y, 0.1f, "Queda acostada");
            Assert.AreEqual(ResultadoDanio.Ignorado.DanioAplicado, vida.RecibirDanio(new InfoDanio(5f, TipoDanio.Golpe, Vector3.up, Vector3.forward, null)).DanioAplicado, "Ya muerta ignora el daño");
        }
    }
}
