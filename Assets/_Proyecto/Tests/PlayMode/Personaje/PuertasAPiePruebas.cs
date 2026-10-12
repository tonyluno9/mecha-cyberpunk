using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Pruebas jugables de puertas a pie: jugador real, locales instanciados en la prueba, entrada simulada.</summary>
    public class PuertasAPiePruebas : BaseJugadorAPie
    {
        private const float DistanciaPuerta = 1.5f;
        private const float GradosMediaVuelta = 180f;
        private const float SensibilidadRaton = 0.1f;

        private readonly List<GameObject> locales = new List<GameObject>();
        private InteraccionAPie interaccion;
        private readonly List<ResultadoPuerta> resultados = new List<ResultadoPuerta>();
        private readonly List<IdLugar> entradas = new List<IdLugar>();
        private readonly List<IdLugar> salidas = new List<IdLugar>();

        public override void Setup()
        {
            LocalesDePrueba.LimpiarGlobales();
            resultados.Clear();
            entradas.Clear();
            salidas.Clear();
            base.Setup();
            interaccion = jugador.GetComponent<InteraccionAPie>();
            interaccion.PuertaIntentada += (puerta, resultado) => resultados.Add(resultado);
            EventosLugares.JugadorEntro += id => entradas.Add(id);
            EventosLugares.JugadorSalio += id => salidas.Add(id);
        }

        public override void TearDown()
        {
            foreach (GameObject local in locales)
            {
                if (local != null) Object.Destroy(local);
            }
            locales.Clear();
            base.TearDown();
            LocalesDePrueba.LimpiarGlobales();
        }

        private GameObject CrearLocal(IdLugar id, float zPuerta, bool cerradoEnDemo, bool abiertoPorHorario)
        {
            GameObject local = LocalesDePrueba.Crear(id, new Vector3(0f, 0f, zPuerta), new HorarioLugar(20f, 6f), cerradoEnDemo, abiertoPorHorario);
            locales.Add(local);
            return local;
        }

        private IEnumerator PresionarE()
        {
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            yield return null;
        }

        private IEnumerator Caminar(float segundos)
        {
            Press(teclado.wKey);
            yield return Esperar(segundos);
            Release(teclado.wKey);
            yield return null;
        }

        private IEnumerator MediaVuelta()
        {
            Set(raton.delta, new Vector2(GradosMediaVuelta / SensibilidadRaton, 0f));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bar_EDeFrenteAbre_WCruzaElUmbralYLuegoSale()
        {
            yield return Calentar();
            GameObject bar = CrearLocal(IdLugar.Bar, DistanciaPuerta, false, true);
            LugarDePrueba lugar = bar.GetComponent<LugarDePrueba>();
            PuertaDePrueba puerta = bar.GetComponentInChildren<PuertaDePrueba>();
            yield return null;

            Assert.AreSame(puerta, interaccion.PuertaCandidata, "Frente a la puerta cerrada es la candidata");
            Assert.AreSame(lugar, interaccion.LugarCandidato);

            yield return PresionarE();
            CollectionAssert.AreEqual(new[] { ResultadoPuerta.Abierta }, resultados);
            Assert.IsTrue(puerta.EstaAbierta);
            yield return Esperar(0.4f);
            Assert.IsNull(interaccion.PuertaCandidata, "Una puerta abierta ya no es candidata");

            yield return Caminar(3f);
            Assert.Greater(jugador.transform.position.z, DistanciaPuerta + 1f, "Cruzó el umbral");
            CollectionAssert.AreEqual(new[] { IdLugar.Bar }, entradas, "JugadorEntro llega una vez");
            Assert.IsTrue(lugar.JugadorDentro);
            Assert.AreEqual(0, salidas.Count);

            yield return MediaVuelta();
            yield return Caminar(3f);
            Assert.Less(jugador.transform.position.z, DistanciaPuerta - 1f, "Salió a la calle");
            CollectionAssert.AreEqual(new[] { IdLugar.Bar }, salidas, "JugadorSalio llega una vez");
            CollectionAssert.AreEqual(new[] { IdLugar.Bar }, entradas, "No hubo otra entrada");
            Assert.IsFalse(lugar.JugadorDentro);
        }

        [UnityTest]
        public IEnumerator Clandestino_FueraDeHorario_EDaCerradaPorHorarioYNoPasa()
        {
            yield return Calentar();
            CrearLocal(IdLugar.Clandestino, DistanciaPuerta, false, false);
            yield return null;

            yield return PresionarE();
            CollectionAssert.AreEqual(new[] { ResultadoPuerta.CerradaPorHorario }, resultados);

            yield return Caminar(3f);
            Assert.Less(jugador.transform.position.z, DistanciaPuerta - 0.3f, "La hoja cerrada lo frena antes del umbral");
            Assert.AreEqual(0, entradas.Count);
        }

        [UnityTest]
        public IEnumerator Casino_CerradoEnLaDemo_EDaBloqueada()
        {
            yield return Calentar();
            GameObject casino = CrearLocal(IdLugar.Casino, DistanciaPuerta, true, true);
            yield return null;

            yield return PresionarE();
            CollectionAssert.AreEqual(new[] { ResultadoPuerta.Bloqueada }, resultados);
            Assert.IsFalse(casino.GetComponentInChildren<PuertaDePrueba>().EstaAbierta);

            yield return Caminar(3f);
            Assert.Less(jugador.transform.position.z, DistanciaPuerta - 0.3f);
        }

        [UnityTest]
        public IEnumerator Clandestino_DentroConLaPuertaCerrada_EAbreYSale()
        {
            yield return Calentar();
            GameObject clandestino = CrearLocal(IdLugar.Clandestino, DistanciaPuerta, false, false);
            LugarDePrueba lugar = clandestino.GetComponent<LugarDePrueba>();
            PuertaDePrueba puerta = clandestino.GetComponentInChildren<PuertaDePrueba>();

            // Dentro, a 1.5 m de la puerta y mirándola (la calle queda hacia -Z).
            jugadorAPie.Colocar(new Pose(new Vector3(0f, 0.05f, DistanciaPuerta + DistanciaPuerta), Quaternion.Euler(0f, GradosMediaVuelta, 0f)));
            yield return Esperar(0.3f);
            Assert.IsTrue(lugar.JugadorDentro, "La zona lo detecta dentro");
            Assert.IsFalse(lugar.EstaAbierto, "El lugar está cerrado por horario");
            Assert.IsFalse(puerta.EstaAbierta);

            yield return PresionarE();
            CollectionAssert.AreEqual(new[] { ResultadoPuerta.Abierta }, resultados);

            yield return Caminar(3f);
            Assert.Less(jugador.transform.position.z, DistanciaPuerta - 1f, "Salió aunque el lugar esté cerrado");
            CollectionAssert.AreEqual(new[] { IdLugar.Clandestino }, salidas);
            Assert.IsFalse(lugar.JugadorDentro);
        }

        [UnityTest]
        public IEnumerator Puerta_DeEspaldasA1MetroYMedio_ENoLlamaAIntentarAbrir()
        {
            yield return Calentar();
            GameObject bar = CrearLocal(IdLugar.Bar, DistanciaPuerta, false, true);
            PuertaDePrueba puerta = bar.GetComponentInChildren<PuertaDePrueba>();
            jugadorAPie.Colocar(new Pose(new Vector3(0f, 0.05f, 0f), Quaternion.Euler(0f, GradosMediaVuelta, 0f)));
            yield return null;

            Assert.IsNull(interaccion.PuertaCandidata);
            yield return PresionarE();
            Assert.AreEqual(0, puerta.IntentosAbrir);
            Assert.AreEqual(0, resultados.Count);
            Assert.IsFalse(puerta.EstaAbierta);
        }

        [UnityTest]
        public IEnumerator Puerta_DeFrenteA4Metros_ENoLlamaAIntentarAbrir()
        {
            yield return Calentar();
            GameObject bar = CrearLocal(IdLugar.Bar, 4f, false, true);
            PuertaDePrueba puerta = bar.GetComponentInChildren<PuertaDePrueba>();
            yield return null;

            Assert.IsNull(interaccion.PuertaCandidata);
            yield return PresionarE();
            Assert.AreEqual(0, puerta.IntentosAbrir);
            Assert.AreEqual(0, resultados.Count);
        }

        [UnityTest]
        public IEnumerator Puerta_TodaviaNoEnAlcance_AlAcercarseSeVuelveCandidata()
        {
            yield return Calentar();
            GameObject bar = CrearLocal(IdLugar.Bar, 4f, false, true);
            PuertaDePrueba puerta = bar.GetComponentInChildren<PuertaDePrueba>();
            yield return null;
            Assert.IsNull(interaccion.PuertaCandidata);

            yield return Caminar(0.6f);
            Assert.AreSame(puerta, interaccion.PuertaCandidata);
        }

        [UnityTest]
        public IEnumerator Puerta_ConElJugadorEnKO_ENoHaceNada()
        {
            yield return Calentar();
            GameObject bar = CrearLocal(IdLugar.Bar, DistanciaPuerta, false, true);
            PuertaDePrueba puerta = bar.GetComponentInChildren<PuertaDePrueba>();
            controlador.ActivarControl(false);
            yield return null;

            Assert.IsNull(interaccion.PuertaCandidata);
            yield return PresionarE();
            Assert.AreEqual(0, puerta.IntentosAbrir);
        }

        [UnityTest]
        public IEnumerator Puerta_EDosVecesSeguidas_LaSegundaNoLlamaPorqueYaNoEsCandidata()
        {
            yield return Calentar();
            GameObject bar = CrearLocal(IdLugar.Bar, DistanciaPuerta, false, true);
            PuertaDePrueba puerta = bar.GetComponentInChildren<PuertaDePrueba>();
            yield return null;

            yield return PresionarE();
            yield return PresionarE();
            Assert.AreEqual(1, puerta.IntentosAbrir);
            CollectionAssert.AreEqual(new[] { ResultadoPuerta.Abierta }, resultados);
        }
    }
}
