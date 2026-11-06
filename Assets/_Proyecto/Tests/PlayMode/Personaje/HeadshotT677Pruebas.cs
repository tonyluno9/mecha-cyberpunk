using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-677: la cabeza hace x3 el daño del cuerpo con las balas del jugador. Se dispara con la mira fija y se mueve el blanco para dar en la cabeza o en el pecho.</summary>
    public class HeadshotT677Pruebas : BaseJugadorAPie
    {
        private readonly System.Collections.Generic.List<GameObject> munecos = new System.Collections.Generic.List<GameObject>();

        // Los muñecos propios se destruyen ya: si quedaran en la escena taparían los tiros de las pruebas que siguen.
        public override void TearDown()
        {
            foreach (GameObject m in munecos)
            {
                if (m != null)
                {
                    Object.DestroyImmediate(m);
                }
            }
            munecos.Clear();
            base.TearDown();
        }

        private IEnumerator PonerPistola()
        {
            Press(teclado.digit2Key);
            yield return null;
            Release(teclado.digit2Key);
            float limite = Time.time + 3f;
            while (jugador.GetComponent<ArmasAPie>().ArmaActiva != ArmaAPie.Pistola && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(ArmaAPie.Pistola, jugador.GetComponent<ArmasAPie>().ArmaActiva);
            yield return Esperar(0.3f);
        }

        // Muñeco de 1.8 m (cápsula) a 8 m, con sus pies a <paramref name="piesY"/>.
        private MunecoAPie CrearMuneco(float piesY)  // en la línea de la mira (la cámara va al hombro, con x distinta de cero)
        {
            GameObject m = new GameObject("MunecoCabeza");
            m.transform.position = new Vector3(controlador.Camara.position.x, piesY, 8f);
            CapsuleCollider c = m.AddComponent<CapsuleCollider>();
            c.height = 1.8f;
            c.radius = 0.3f;
            c.center = new Vector3(0f, 0.9f, 0f);
            munecos.Add(m);
            MunecoAPie muneco = m.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
            return muneco;
        }

        private IEnumerator Disparar()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.3f);
        }

        [UnityTest]
        public IEnumerator Cabeza_HaceTresVecesElDanioDelPecho()
        {
            yield return Calentar();
            yield return PonerPistola();
            float alturaMira = controlador.Camara.position.y;
            PistolaAPie pistola = jugador.GetComponent<PistolaAPie>();
            ImpactoBala ultimo = default;
            pistola.BalaImpacto += (arma, impacto) => ultimo = impacto;

            // Pecho: la mira cae a 60 % de la altura del blanco.
            MunecoAPie pecho = CrearMuneco(alturaMira - 1.8f * 0.6f);
            yield return Disparar();
            float danioPecho = pecho.Maxima - pecho.Actual;
            Assert.IsTrue(ultimo.EnPersona, "Pegó a la persona");
            Assert.IsFalse(ultimo.Cabeza, "En el pecho no es cabeza");
            Assert.Greater(danioPecho, 0f);
            Object.Destroy(pecho.gameObject);
            yield return null;

            // Cabeza: la mira cae a 95 % de la altura.
            MunecoAPie cabeza = CrearMuneco(alturaMira - 1.8f * 0.95f);
            ultimo = default;
            yield return Disparar();
            float danioCabeza = cabeza.Maxima - cabeza.Actual;
            Assert.IsTrue(ultimo.Cabeza, "En la cabeza se marca el headshot");
            Assert.AreEqual(danioPecho * ReglaZonaDanio.MultiplicadorCabeza, danioCabeza, 0.01f, "La cabeza hace x3 (" + danioCabeza + " contra " + danioPecho + ")");
        }
    }
}
