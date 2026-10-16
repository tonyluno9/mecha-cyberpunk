using System.Collections;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ServicioCalor dentro de ServiciosProgresion.prefab: registro, sumar, enfriar tras el retraso y borrar.</summary>
    public class ServicioCalorPruebas : InputTestFixture
    {
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const float Margen = 0.6f;

        private GameObject raiz;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
        }

        public override void TearDown()
        {
            if (raiz != null) Object.Destroy(raiz);
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin) yield return null;
        }

        private IEnumerator Crear(string ruta)
        {
            raiz = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Prefab_Instanciado_HayUnICalorRegistradoEnCero()
        {
            yield return Crear(RutaServicios);
            ICalor calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(calor);
            Assert.AreEqual(0f, calor.Valor);
            Assert.AreEqual(100f, calor.Maximo);
            Assert.AreEqual(0, calor.Nivel);
        }

        [UnityTest]
        public IEnumerator JugadorAPie_SoloEnEscena_TieneExactamenteUnICalor()
        {
            yield return Crear(RutaJugador);
            Assert.IsNotNull(Registro<ICalor>.Actual);
            Assert.AreEqual(1, raiz.GetComponentsInChildren<MechaCyberpunk.Progresion.ServicioCalor>(true).Length);
            Assert.AreSame(raiz.GetComponentInChildren<MechaCyberpunk.Progresion.ServicioCalor>(true), Registro<ICalor>.Actual);
        }

        [UnityTest]
        public IEnumerator Sumar_CambiaValorYNivelYAvisa()
        {
            yield return Crear(RutaServicios);
            ICalor calor = Registro<ICalor>.Actual;
            int nivelAvisado = -1;
            float valorAvisado = -1f;
            calor.NivelCambio += n => nivelAvisado = n;
            calor.ValorCambio += v => valorAvisado = v;

            calor.Sumar(30f, MotivoCalor.Otro);

            Assert.AreEqual(30f, calor.Valor, 0.01f);
            Assert.AreEqual(1, calor.Nivel);
            Assert.AreEqual(1, nivelAvisado);
            Assert.AreEqual(30f, valorAvisado, 0.01f);
        }

        [UnityTest]
        public IEnumerator Enfriar_TrasElRetraso_BajaCincoPorSegundo()
        {
            yield return Crear(RutaServicios);
            ICalor calor = Registro<ICalor>.Actual;
            calor.Sumar(50f, MotivoCalor.Otro);

            yield return Esperar(9f);
            Assert.AreEqual(50f, calor.Valor, 0.01f, "Dentro del retraso no baja");

            yield return Esperar(3f + 2f);
            // 14 s desde que se sumó: 10 s de retraso y unos 4 s enfriando a 5/s.
            Assert.AreEqual(30f, calor.Valor, 1.5f);
            Assert.Less(calor.Valor, 50f);
        }

        [UnityTest]
        public IEnumerator Borrar_DejaElCalorEnCero()
        {
            yield return Crear(RutaServicios);
            ICalor calor = Registro<ICalor>.Actual;
            calor.Sumar(80f, MotivoCalor.Otro);
            Assert.AreEqual(3, calor.Nivel);

            calor.Borrar();

            Assert.AreEqual(0f, calor.Valor);
            Assert.AreEqual(0, calor.Nivel);
        }

        [UnityTest]
        public IEnumerator Destruir_QuitaDelRegistro()
        {
            yield return Crear(RutaServicios);
            Assert.IsNotNull(Registro<ICalor>.Actual);
            Object.Destroy(raiz);
            raiz = null;
            yield return null;
            Assert.IsNull(Registro<ICalor>.Actual);
        }
    }
}
