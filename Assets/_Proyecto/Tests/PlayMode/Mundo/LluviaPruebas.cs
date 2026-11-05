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
    /// <summary>Lluvia y niebla baja (T-096): caen de noche afuera, se apagan dentro, de día y con el interruptor, y no cuestan fotogramas.</summary>
    public class LluviaPruebas
    {
        private const float SegundosFisica = 0.3f;
        private const float SegundosCalentar = 3f;
        private const float SegundosMedicion = 4f;
        private const float SegundosMaximosPorFrame = 0.1f;
        private const float AlturaApoyo = 0.05f;
        private const float AlturaOjos = 1.7f;

        private GameObject camaraObjeto;
        private CamaraLibre camara;
        private LluviaSigueJugador lluvia;

        [TearDown]
        public void TearDown()
        {
            LluviaSigueJugador.Activa = true;
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
            lluvia = Object.FindFirstObjectByType<LluviaSigueJugador>();
            Assert.IsNotNull(lluvia, "El barrio trae el objeto Lluvia");
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Bar, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            yield return EscenaLugares.Esperar(SegundosFisica);
        }

        [UnityTest]
        public IEnumerator Lluvia_DeNocheAfuera_Cae()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            Assert.IsTrue(lluvia.EstaLloviendo);
            Assert.IsTrue(lluvia.Gotas.isEmitting);
            Assert.IsTrue(lluvia.Niebla.isEmitting);
        }

        [UnityTest]
        public IEnumerator Lluvia_AMediodia_CaeLigera()
        {
            // T-512: también llueve de día, pero solo una fracción de las gotas de la noche.
            yield return Preparar(EscenaLugares.HoraMediodia);
            Assert.IsTrue(lluvia.EstaLloviendo);
            Assert.IsTrue(lluvia.Gotas.isEmitting);
            Assert.Less(lluvia.Intensidad, 0.5f, "Lluvia ligera");
            Assert.Greater(lluvia.Intensidad, 0f);
            float deNoche = UnityEditor.AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Mundo.Datos.ConfigLluvia>("Assets/_Proyecto/Mundo/Datos/ConfigLluvia_Mundo.asset").GotasPorSegundo;
            Assert.Less(lluvia.Gotas.emission.rateOverTime.constant, 0.5f * deNoche, "Menos gotas que de noche");
        }

        [UnityTest]
        public IEnumerator Lluvia_ConElInterruptorApagado_NoCae()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            LluviaSigueJugador.Activa = false;
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.IsFalse(lluvia.EstaLloviendo, "Con Activa = false la lluvia se apaga");
            LluviaSigueJugador.Activa = true;
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.IsTrue(lluvia.EstaLloviendo, "Y vuelve al prenderlo");
        }

        [UnityTest]
        public IEnumerator Lluvia_DentroDelBar_EstaApagadaYAfueraEncendida()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            ZonaInterior zona = null;
            foreach (ZonaInterior candidata in Object.FindObjectsByType<ZonaInterior>(FindObjectsSortMode.None))
            {
                if (candidata.GetComponentInParent<LugarMundo>().Id == IdLugar.Bar)
                {
                    zona = candidata;
                }
            }
            Assert.IsNotNull(zona);
            Bounds limites = zona.GetComponent<BoxCollider>().bounds;
            Vector3 afuera = camara.transform.position;
            camara.Colocar(new Pose(new Vector3(limites.center.x, limites.min.y + AlturaApoyo, limites.center.z), Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.IsTrue(zona.JugadorDentro);
            Assert.IsFalse(lluvia.EstaLloviendo, "Dentro del bar no llueve");
            camara.Colocar(new Pose(afuera, Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            Assert.IsFalse(zona.JugadorDentro);
            Assert.IsTrue(lluvia.EstaLloviendo, "Afuera vuelve a llover");
        }

        // T-634: la lluvia llega al suelo y ahí salpica (colisión con el plano del piso + sub-emisor de salpicadura).
        [UnityTest]
        public IEnumerator Lluvia_LlegaAlSueloYSalpica()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            yield return EscenaLugares.Esperar(SegundosCalentar);
            Assert.IsTrue(lluvia.Gotas.collision.enabled, "Las gotas chocan con el piso");
            Assert.AreEqual(ParticleSystemCollisionType.Planes, lluvia.Gotas.collision.type);
            Assert.IsTrue(lluvia.Gotas.subEmitters.enabled);
            Assert.AreEqual(lluvia.Salpicaduras, lluvia.Gotas.subEmitters.GetSubEmitterSystem(0), "El sub-emisor de colisión es la salpicadura");
            Assert.AreEqual(ParticleSystemSubEmitterType.Collision, lluvia.Gotas.subEmitters.GetSubEmitterType(0));

            ParticleSystem.Particle[] gotas = new ParticleSystem.Particle[lluvia.Gotas.main.maxParticles];
            float pisoY = lluvia.Suelo.position.y;
            float masBajo = float.MaxValue;
            float masAlto = float.MinValue;
            int salpicadurasMaximas = 0;
            float inicio = Time.time;
            while (Time.time - inicio < SegundosMedicion)
            {
                yield return null;
                int n = lluvia.Gotas.GetParticles(gotas);
                for (int i = 0; i < n; i++)
                {
                    masBajo = Mathf.Min(masBajo, gotas[i].position.y);
                    masAlto = Mathf.Max(masAlto, gotas[i].position.y);
                }
                salpicadurasMaximas = Mathf.Max(salpicadurasMaximas, lluvia.Salpicaduras.particleCount);
            }
            Assert.Greater(masAlto - pisoY, 2f, "Caen desde arriba");
            Assert.GreaterOrEqual(masBajo, pisoY - 0.35f, "Ninguna gota atraviesa el piso (la más baja a " + (masBajo - pisoY) + " m del piso)");
            Assert.Less(masBajo - pisoY, 0.6f, "Las gotas bajan hasta el piso");
            Assert.Greater(salpicadurasMaximas, 0, "Al tocar el piso salpican");
        }

        [UnityTest]
        public IEnumerator Lluvia_NoCuestaFotogramas_NingunFrameDeMasDe100Ms()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            yield return EscenaLugares.Esperar(SegundosCalentar);
            float peor = 0f;
            float inicio = Time.time;
            while (Time.time - inicio < SegundosMedicion)
            {
                yield return null;
                peor = Mathf.Max(peor, Time.unscaledDeltaTime);
            }
            Assert.Less(peor, SegundosMaximosPorFrame, "Peor frame con lluvia: " + peor + " s");
            Assert.Greater(lluvia.Gotas.particleCount, 100, "Hay gotas vivas");
        }

        [UnityTest]
        public IEnumerator Lluvia_Capturas_CalleDeNoche()
        {
            yield return Preparar(EscenaLugares.HoraNoche);
            Vector3 haciaPuerta = EscenaLugares.HaciaLaPuerta(IdLugar.Bar);
            Vector3 desde = EscenaLugares.Lugar(IdLugar.Bar).PosicionEntrada - haciaPuerta * 6f + Vector3.up * AlturaOjos;
            camara.Colocar(new Pose(desde, Quaternion.LookRotation(haciaPuerta + Vector3.up * 0.1f, Vector3.up)));
            yield return EscenaLugares.Esperar(SegundosCalentar);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-096-1.png");
            Vector3 lado = Vector3.Cross(Vector3.up, haciaPuerta);
            camara.Colocar(new Pose(desde, Quaternion.LookRotation(lado + Vector3.up * 0.05f, Vector3.up)));
            yield return EscenaLugares.Esperar(SegundosFisica);
            CapturaPruebas.Guardar(camara.Camara, "docs/capturas/T-096-2.png");
        }
    }
}
