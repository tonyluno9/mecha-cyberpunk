using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Ambiente sonoro (T-097): al entrar al bar sube la música y baja la lluvia; al salir, al revés; de día no hay lluvia ni neón.</summary>
    public class AmbienteSonoroPruebas
    {
        private const float SegundosFisica = 0.3f;
        private const float SegundosMezcla = 2.5f;        // más que SegundosCambio de la config (1 s)
        private const float AlturaApoyo = 0.05f;
        private const float Margen = 0.02f;

        private GameObject camaraObjeto;
        private CamaraLibre camara;
        private AmbienteSonoro ambiente;

        [TearDown]
        public void TearDown()
        {
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
        }

        private IEnumerator Preparar(float hora)
        {
            yield return EscenaLugares.CargarCiudad();
            EscenaLugares.RelojPausado().AvanzarHasta(hora);
            ambiente = Object.FindFirstObjectByType<AmbienteSonoro>();
            Assert.IsNotNull(ambiente, "El barrio trae el AmbienteSonoro");
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Bar, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            yield return EscenaLugares.Esperar(SegundosMezcla);
        }

        [UnityTest]
        public IEnumerator Ambiente_AlEntrarAlBar_SubeLaMusicaYBajaLaLluvia_YAlSalirAlReves()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            float lluviaAfuera = ambiente.VolumenLluvia;
            float musicaAfuera = ambiente.VolumenMusica;
            Assert.Greater(lluviaAfuera, Margen, "De noche afuera se oye la lluvia");
            Assert.Greater(ambiente.VolumenNeon, Margen, "Y el zumbido del neón");
            Assert.AreEqual(0f, musicaAfuera, Margen, "Sin música afuera");

            ZonaInterior zona = null;
            foreach (ZonaInterior candidata in Object.FindObjectsByType<ZonaInterior>(FindObjectsSortMode.None))
            {
                if (candidata.GetComponentInParent<LugarMundo>().Id == IdLugar.Bar)
                {
                    zona = candidata;
                }
            }
            Bounds limites = zona.GetComponent<BoxCollider>().bounds;
            Vector3 afuera = camara.transform.position;
            camara.Colocar(new Pose(new Vector3(limites.center.x, limites.min.y + AlturaApoyo, limites.center.z), Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosMezcla);
            Assert.IsTrue(zona.JugadorDentro);
            Assert.Greater(ambiente.VolumenMusica, musicaAfuera + Margen, "Dentro sube la música");
            Assert.Less(ambiente.VolumenLluvia, lluviaAfuera - Margen, "Dentro baja la lluvia");
            Assert.AreEqual(0f, ambiente.VolumenNeon, Margen, "Dentro no se oye el neón");

            camara.Colocar(new Pose(afuera, Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosMezcla);
            Assert.AreEqual(musicaAfuera, ambiente.VolumenMusica, Margen, "Al salir se va la música");
            Assert.AreEqual(lluviaAfuera, ambiente.VolumenLluvia, Margen, "Y vuelve la lluvia");
        }

        [UnityTest]
        public IEnumerator Ambiente_AMediodia_NoHayLluviaNiNeon_SoloTrafico()
        {
            yield return Preparar(EscenaLugares.HoraMediodia);
            Assert.AreEqual(0f, ambiente.VolumenLluvia, Margen);
            Assert.AreEqual(0f, ambiente.VolumenNeon, Margen);
            Assert.AreEqual(0f, ambiente.VolumenMusica, Margen);
            Assert.Greater(ambiente.VolumenTrafico, Margen);
        }

        [UnityTest]
        public IEnumerator Ambiente_ConLaLluviaApagada_NoSuenaLluvia()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            LluviaSigueJugador.Activa = false;
            yield return EscenaLugares.Esperar(SegundosMezcla);
            Assert.AreEqual(0f, ambiente.VolumenLluvia, Margen, "Con Activa = false tampoco suena");
            LluviaSigueJugador.Activa = true;
        }
    }
}
