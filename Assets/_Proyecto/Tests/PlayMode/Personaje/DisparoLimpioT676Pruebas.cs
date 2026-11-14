using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-676: destello chico y brevísimo, sonido de disparo siempre igual (nada al impactar), y sin línea de vista no hay disparo.</summary>
    public class DisparoLimpioT676Pruebas : BaseJugadorAPie
    {
        private sealed class SonidosFalsos : ISonidos
        {
            public readonly List<string> Ids = new List<string>();
            public void Reproducir(string id) { Ids.Add(id); }
            public void ReproducirEn(string id, Vector3 posicion) { Ids.Add(id); }
            public int ReproducirBucle(string id, Transform sigue) { return Sonidos.SinBucle; }
            public void DetenerBucle(int bucle) { }
            public void FijarTono(int bucle, float tono) { }
            public float Volumen(SeccionSonido seccion) { return 1f; }
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        private SonidosFalsos sonidos;

        public override void Setup()
        {
            base.Setup();
            sonidos = new SonidosFalsos();
            Registro<ISonidos>.Agregar(sonidos);
        }

        public override void TearDown()
        {
            Registro<ISonidos>.Quitar(sonidos);
            base.TearDown();
        }

        private IEnumerator EquiparPistola()
        {
            yield return ElegirEnRueda(1);
            float limite = Time.time + 3f;
            while (jugador.GetComponent<ArmasAPie>().ArmaActiva != ArmaAPie.Pistola && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(ArmaAPie.Pistola, jugador.GetComponent<ArmasAPie>().ArmaActiva, "La pistola quedó en la mano");
            yield return Esperar(0.3f);
        }

        private IEnumerator Disparar()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
        }

        private GameObject Muneco(Vector3 centro)
        {
            GameObject m = Caja(centro, new Vector3(1.5f, 2f, 0.5f));
            m.AddComponent<MunecoAPie>();
            return m;
        }

        [UnityTest]
        public IEnumerator Disparo_SuenaSiempreElMismoDisparo_NadaEnPersona_YUnImpactoEnPared()
        {
            yield return Calentar();
            GameObject muneco = Muneco(new Vector3(0f, 1f, 8f));
            Physics.SyncTransforms();
            yield return EquiparPistola();

            sonidos.Ids.Clear();
            yield return Disparar(); // al muñeco
            yield return Esperar(0.4f);
            foreach (string id in sonidos.Ids)
            {
                Assert.IsFalse(id.Contains("IMPACTO"), "Pegarle a una persona no suena: " + id);
            }
            Object.Destroy(muneco);
            Caja(new Vector3(0f, 1f, 8f), new Vector3(6f, 3f, 0.5f)); // ahora una pared en la mira
            yield return null;
            Physics.SyncTransforms();
            sonidos.Ids.Clear();
            yield return Disparar(); // a la pared
            yield return Esperar(0.6f);
            // T-977: el tiro que falla suena una vez en la pared (ARM_BALA_IMPACTO_CONCRETO); lo demás es el disparo de siempre.
            foreach (string id in sonidos.Ids)
            {
                Assert.IsTrue(id == "ARM_PIS_DISPARO" || id == "ARM_CASQUILLO" || id == "ARM_PIS_RECARGA" || id == PistolaAPie.SonidoImpactoSuperficie,
                    "Solo el disparo del arma, el casquillo y el impacto en la pared: " + id);
            }
            Assert.Contains("ARM_PIS_DISPARO", sonidos.Ids);
            Assert.AreEqual(1, sonidos.Ids.FindAll(i => i == PistolaAPie.SonidoImpactoSuperficie).Count, "Un solo impacto en la pared por tiro");
        }

        [UnityTest]
        public IEnumerator Destello_EsChicoYBrevisimo()
        {
            yield return Calentar();
            Muneco(new Vector3(0f, 1f, 8f));
            Physics.SyncTransforms();
            yield return EquiparPistola();
            jugador.GetComponentInChildren<MechaCyberpunk.Personaje.Visual.VisualPersonaje>(true).Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            SensacionDisparo sensacion = jugador.GetComponent<SensacionDisparo>();
            Assert.IsFalse(sensacion.DestelloVisible);
            Press(raton.rightButton);
            yield return Esperar(0.9f);
            yield return Disparar();
            Assert.IsTrue(sensacion.DestelloVisible, "Destella");
            Transform destello = jugador.GetComponent<SensacionDisparo>().transform; // el quad cuelga de la escena
            GameObject quad = GameObject.Find("DestelloCanon");
            Assert.IsNotNull(quad);
            CapturaS1.Foto("s1n-T-676-destello-pistola", jugador.transform, new Vector3(1.6f, 1.5f, 0.3f), new Vector3(-0.1f, 1.4f, 0.6f), 40f, 700, 700);
            Assert.LessOrEqual(quad.transform.localScale.x, 0.35f, "Destello chico (" + quad.transform.localScale.x + " m)");
            // La duración es de tiempo de juego (2 o 3 cuadros a 60 fps); con capturas el render alarga un cuadro, así que ahí no se mide.
            float inicio = Time.time;
            int cuadros = 0;
            while (sensacion.DestelloVisible && cuadros < 600)
            {
                cuadros++;
                yield return null;
            }
            if (!CapturaS1.Activa)
            {
                Assert.LessOrEqual(Time.time - inicio, sensacion.Datos.DuracionDestello + 0.1f, "Dura solo unos cuadros");
            }
            Assert.IsNotNull(destello);
            Release(raton.rightButton);
            Assert.LessOrEqual(sensacion.Datos.DuracionDestello, 0.06f, "La duración configurada es de 2 o 3 cuadros");
        }

        [UnityTest]
        public IEnumerator Barricada_EntreElJugadorYElBlanco_NoDejaPasarLaBala()
        {
            yield return Calentar();
            Caja(new Vector3(0f, 1f, 3f), new Vector3(6f, 2f, 0.3f)); // barricada
            GameObject muneco = Muneco(new Vector3(0f, 1f, 8f));
            MunecoAPie vida = muneco.GetComponent<MunecoAPie>();
            Physics.SyncTransforms();
            yield return EquiparPistola();
            for (int i = 0; i < 3; i++)
            {
                yield return Disparar();
                yield return Esperar(0.5f);
            }
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.001f, "La barricada frena las balas");
        }

        [UnityTest]
        public IEnumerator ArmaPegadaAUnMuro_NoDisparaAlOtroLado()
        {
            yield return Calentar();
            // Muro delgado casi sobre el jugador: la boca del arma queda del otro lado del muro.
            Caja(new Vector3(0f, 1f, 0.35f), new Vector3(6f, 2.5f, 0.1f));
            GameObject muneco = Muneco(new Vector3(0f, 1f, 6f));
            MunecoAPie vida = muneco.GetComponent<MunecoAPie>();
            Physics.SyncTransforms();
            yield return EquiparPistola();
            int impactos = 0;
            Vector3 punto = Vector3.zero;
            jugador.GetComponent<PistolaAPie>().BalaImpacto += (arma, impacto) => { impactos++; punto = impacto.Punto; };
            for (int i = 0; i < 3; i++)
            {
                yield return Disparar();
                yield return Esperar(0.5f);
            }
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.001f, "Con la boca tras el muro la bala no llega al otro lado");
            Assert.Greater(impactos, 0, "La bala pega en el muro");
            Assert.Less(punto.z, 1.0f, "El impacto es en el muro, no más allá");
        }

        [UnityTest]
        public IEnumerator SinObstaculos_LaBalaSiPega()
        {
            yield return Calentar();
            GameObject muneco = Muneco(new Vector3(0f, 1f, 6f));
            MunecoAPie vida = muneco.GetComponent<MunecoAPie>();
            Physics.SyncTransforms();
            yield return EquiparPistola();
            Vector3 punto = Vector3.zero;
            string quien = "nada";
            jugador.GetComponent<PistolaAPie>().BalaImpacto += (arma, impacto) => { punto = impacto.Punto; quien = "persona=" + impacto.EnPersona; };
            yield return Disparar();
            yield return Esperar(0.3f);
            Assert.Less(vida.Actual, vida.Maxima, "Sin nada en medio el disparo hace daño (impacto: " + quien + " en " + punto + ")");
        }
    }
}
