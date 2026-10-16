using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Pruebas jugables de interactuables a pie: jugador real, doble de prueba, entrada simulada.</summary>
    public class InteractuablesAPiePruebas : BaseJugadorAPie
    {
        private const float GradosMediaVuelta = 180f;
        private const float SensibilidadRaton = 0.1f;
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";

        private InteraccionAPie interaccion;
        private InteractuableDePrueba objeto;
        private readonly List<IInteractuable> usados = new List<IInteractuable>();
        private readonly List<IInteractuable> negados = new List<IInteractuable>();
        private readonly List<GameObject> creados = new List<GameObject>();

        public override void Setup()
        {
            LocalesDePrueba.LimpiarGlobales();
            usados.Clear();
            negados.Clear();
            base.Setup();
            interaccion = jugador.GetComponent<InteraccionAPie>();
            interaccion.InteractuableUsado += i => usados.Add(i);
            interaccion.InteractuableNegado += i => negados.Add(i);
        }

        public override void TearDown()
        {
            foreach (GameObject creado in creados)
            {
                if (creado != null) Object.Destroy(creado);
            }
            creados.Clear();
            base.TearDown();
            LocalesDePrueba.LimpiarGlobales();
        }

        private InteractuableDePrueba Crear(Vector3 posicion)
        {
            GameObject go = new GameObject("Interactuable de prueba");
            go.transform.position = posicion;
            creados.Add(go);
            return go.AddComponent<InteractuableDePrueba>();
        }

        private IEnumerator PresionarE()
        {
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CaminandoConW_ADosMetros_EsCandidatoYELoUsaUnaVez()
        {
            yield return Calentar();
            objeto = Crear(new Vector3(0f, 0f, 6f));
            yield return null;
            Assert.IsNull(interaccion.InteractuableCandidato, "A 6 m no es candidato");

            Press(teclado.wKey);
            float limite = Time.time + 10f;
            while (jugador.transform.position.z < 4f && Time.time < limite)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;

            Assert.AreSame(objeto, interaccion.InteractuableCandidato, "pos=" + jugador.transform.position);
            Assert.AreEqual(TipoCandidatoAPie.Interactuable, interaccion.TipoCandidato);

            yield return PresionarE();
            Assert.AreEqual(1, objeto.Usos);
            Assert.AreEqual(1, usados.Count);
            Assert.AreSame(objeto, usados[0]);
            Assert.AreEqual(0, negados.Count);
        }

        [UnityTest]
        public IEnumerator EMantenida_UsaSoloUnaVezPorPulsacion()
        {
            yield return Calentar();
            objeto = Crear(new Vector3(0f, 0f, 2f));
            yield return null;

            Press(teclado.eKey);
            yield return Esperar(0.3f);
            Release(teclado.eKey);
            yield return null;
            Assert.AreEqual(1, objeto.Usos);

            yield return PresionarE();
            Assert.AreEqual(2, objeto.Usos, "Otra pulsación, otro uso");
        }

        [UnityTest]
        public IEnumerator SinPuedeUsar_ENoLoUsaYAvisaNegado()
        {
            yield return Calentar();
            objeto = Crear(new Vector3(0f, 0f, 2f));
            objeto.PuedeUsar = false;
            yield return null;
            Assert.AreSame(objeto, interaccion.InteractuableCandidato, "Sigue siendo candidato para que el HUD diga por qué");

            yield return PresionarE();
            Assert.AreEqual(0, objeto.Usos);
            Assert.AreEqual(0, usados.Count);
            Assert.AreEqual(1, negados.Count);
            Assert.AreSame(objeto, negados[0]);
        }

        [UnityTest]
        public IEnumerator DeEspaldas_NoEsCandidatoYENoHaceNada()
        {
            yield return Calentar();
            objeto = Crear(new Vector3(0f, 0f, 2f));
            Set(raton.delta, new Vector2(GradosMediaVuelta / SensibilidadRaton, 0f));
            yield return null;
            yield return null;

            Assert.IsNull(interaccion.InteractuableCandidato);
            yield return PresionarE();
            Assert.AreEqual(0, objeto.Usos);
            Assert.AreEqual(0, negados.Count);
        }

        [UnityTest]
        public IEnumerator ATresMetros_NoEsCandidato()
        {
            yield return Calentar();
            objeto = Crear(new Vector3(0f, 0f, 3f));
            yield return null;

            Assert.IsNull(interaccion.InteractuableCandidato);
            yield return PresionarE();
            Assert.AreEqual(0, objeto.Usos);
        }

        [UnityTest]
        public IEnumerator ConPuertaCerradaMasCerca_GanaLaPuerta()
        {
            yield return Calentar();
            GameObject local = LocalesDePrueba.Crear(IdLugar.Bar, new Vector3(0f, 0f, 1.5f), new HorarioLugar(20f, 6f), false, true);
            creados.Add(local);
            objeto = Crear(new Vector3(0f, 0f, 2.3f));
            yield return null;

            Assert.AreEqual(TipoCandidatoAPie.Puerta, interaccion.TipoCandidato);
            Assert.IsNull(interaccion.InteractuableCandidato);
            yield return PresionarE();
            Assert.AreEqual(0, objeto.Usos);
        }

        [UnityTest]
        public IEnumerator SubidoAlCarro_NoSeOfreceNiSeUsa()
        {
            yield return Calentar();
            GameObject carro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(2f, 0f, 1.1f), Quaternion.identity);
            creados.Add(carro);
            objeto = Crear(new Vector3(0f, 0f, 3f));
            yield return null;
            Assert.AreEqual(TipoCandidatoAPie.Montable, interaccion.TipoCandidato, "El carro está más cerca");

            yield return PresionarE();
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "Subió al carro");

            objeto.transform.position = jugador.transform.position + jugador.transform.forward;
            yield return null;
            Assert.IsNull(interaccion.InteractuableCandidato, "En carro no se ofrece");
            yield return PresionarE();
            Assert.AreEqual(0, objeto.Usos);
            Assert.AreEqual(0, usados.Count);
        }
    }
}
