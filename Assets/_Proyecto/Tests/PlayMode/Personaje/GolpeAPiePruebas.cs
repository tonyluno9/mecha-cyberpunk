using System.Collections;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Pruebas jugables del golpe a pie: prefab real, muñeco creado en la prueba, clic simulado.</summary>
    public class GolpeAPiePruebas : BaseJugadorAPie
    {
        private GolpeAPie golpe;
        private GameObject muneco;
        private MunecoAPie vidaMuneco;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            golpe = jugador.GetComponent<GolpeAPie>();
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            if (muneco != null)
            {
                Object.Destroy(muneco);
            }
            base.TearDown();
        }

        // T-403: los puños pegan lo que dice su nivel en el arsenal (20 en el nivel 0), no DatosPersonaje.DanioGolpe.
        private float DanioPunos() => jugador.GetComponent<ArsenalJugador>().Datos(ArmaAPie.Punos).Danio;

        private void CrearMuneco(float distancia)
        {
            muneco = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            muneco.transform.position = new Vector3(0f, 0.9f, distancia);
            muneco.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            vidaMuneco = muneco.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
        }

        [UnityTest]
        public IEnumerator Golpe_MunecoA12Metros_QuitaElDanioDespuesDeLaPreparacion()
        {
            yield return Calentar();
            CrearMuneco(1.2f);
            yield return null;
            float maxima = vidaMuneco.Maxima;

            yield return Clic();
            Assert.AreEqual(maxima, vidaMuneco.Actual, 0.0001f, "El daño no llega en el frame del clic");

            yield return Esperar(0.3f);
            Assert.AreEqual(maxima - DanioPunos(), vidaMuneco.Actual, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Golpe_SegundoClicEnLaRecarga_NoPegaYDespuesSi()
        {
            yield return Calentar();
            CrearMuneco(1.2f);
            yield return null;
            float danio = DanioPunos();
            float maxima = vidaMuneco.Maxima;

            yield return Clic();
            yield return Esperar(0.3f);
            Assert.AreEqual(maxima - danio, vidaMuneco.Actual, 0.0001f);

            yield return Clic(); // Todavía en la recarga (0.45 s después del impacto).
            yield return Esperar(0.8f);
            Assert.AreEqual(maxima - danio, vidaMuneco.Actual, 0.0001f, "Un clic durante la recarga no debe pegar");

            yield return Clic();
            yield return Esperar(0.3f);
            Assert.AreEqual(maxima - 2f * danio, vidaMuneco.Actual, 0.0001f, "Después de la recarga sí pega");
        }

        [UnityTest]
        public IEnumerator Golpe_MunecoA3Metros_NoHaceDanio()
        {
            yield return Calentar();
            CrearMuneco(3f);
            yield return null;

            yield return Clic();
            yield return Esperar(0.4f);
            Assert.AreEqual(vidaMuneco.Maxima, vidaMuneco.Actual, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Golpe_PegadoAUnMuro_NoSeHaceDanioASiMismo()
        {
            yield return Calentar();
            Caja(new Vector3(0f, 1f, 0.7f), new Vector3(4f, 2f, 0.4f));
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            int impactos = 0;
            EventosPelea.ImpactoRecibido += (objetivo, info, resultado) => impactos++;
            yield return null;

            yield return Clic();
            yield return Esperar(0.4f);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.0001f);
            Assert.AreEqual(0, impactos);
        }

        [UnityTest]
        public IEnumerator Golpe_ConPistolaEnLaMano_NoPega()
        {
            yield return Calentar();
            CrearMuneco(1.2f);
            golpe.ArmaActiva = ArmaAPie.Pistola;
            yield return null;

            yield return Clic();
            yield return Esperar(0.4f);
            Assert.AreEqual(vidaMuneco.Maxima, vidaMuneco.Actual, 0.0001f);
        }

        // T-400: en tercera persona no hay puño visible (Puno null); se comprueba el ciclo del golpe.
        [UnityTest]
        public IEnumerator Golpe_Clic_PasaPorPreparandoRecuperandoYVuelveAListo()
        {
            yield return Calentar();
            CrearMuneco(1.2f);
            yield return null;
            Assert.AreEqual(FaseGolpe.Listo, golpe.Fase);

            yield return Clic();
            bool preparo = golpe.Fase == FaseGolpe.Preparando;
            bool recupero = false;
            float fin = Time.time + 1.2f;
            while (Time.time < fin)
            {
                recupero |= golpe.Fase == FaseGolpe.Recuperando;
                yield return null;
            }
            Assert.IsTrue(preparo, "Tras el clic prepara el golpe");
            Assert.IsTrue(recupero, "Después del impacto recupera");
            Assert.AreEqual(FaseGolpe.Listo, golpe.Fase, "Y al terminar la recarga queda listo");
        }
    }
}
