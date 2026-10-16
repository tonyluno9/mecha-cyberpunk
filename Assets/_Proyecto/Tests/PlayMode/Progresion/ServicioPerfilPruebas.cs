using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ServicioPerfil dentro de ServiciosProgresion.prefab: registro, respaldo, Fijar y Cambio.</summary>
    public class ServicioPerfilPruebas : InputTestFixture
    {
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

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

        private IEnumerator Crear(string ruta)
        {
            raiz = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Prefab_Instanciado_RegistraUnPerfilConElRespaldo()
        {
            yield return Crear(RutaServicios);
            Assert.AreEqual(1, Registro<IPerfilJugador>.Cantidad);
            Assert.AreEqual("NOVATO", Registro<IPerfilJugador>.Actual.Alias);
            Assert.IsFalse(string.IsNullOrEmpty(Registro<IPerfilJugador>.Actual.Nombre));
        }

        [UnityTest]
        public IEnumerator JugadorAPie_SoloEnEscena_TieneExactamenteUnPerfilNovato()
        {
            yield return Crear(RutaJugador);
            Assert.AreEqual(1, Registro<IPerfilJugador>.Cantidad);
            Assert.AreEqual(1, raiz.GetComponentsInChildren<ServicioPerfil>(true).Length);
            Assert.AreEqual("NOVATO", Registro<IPerfilJugador>.Actual.Alias);
        }

        [UnityTest]
        public IEnumerator Fijar_ConAliasConEspacios_GuardaElAliasLimpioYAvisaCambio()
        {
            yield return Crear(RutaServicios);
            IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
            int avisos = 0;
            perfil.Cambio += () => avisos++;

            raiz.GetComponent<ServicioPerfil>().Fijar("Juan", "  LA CHISPA  ");

            Assert.AreEqual("LA CHISPA", perfil.Alias);
            Assert.AreEqual("Juan", perfil.Nombre);
            Assert.AreEqual(1, avisos);
        }

        [UnityTest]
        public IEnumerator Fijar_ConAliasVacioONull_VuelveAlRespaldo()
        {
            yield return Crear(RutaServicios);
            ServicioPerfil servicio = raiz.GetComponent<ServicioPerfil>();
            servicio.Fijar("Juan", "LA CHISPA");
            servicio.Fijar("Juan", "   ");
            Assert.AreEqual("NOVATO", servicio.Alias);
            servicio.Fijar("Juan", "EL RAYO");
            servicio.Fijar(null, null);
            Assert.AreEqual("NOVATO", servicio.Alias);
            Assert.IsFalse(string.IsNullOrEmpty(servicio.Nombre));
        }

        [UnityTest]
        public IEnumerator Fijar_ConLosMismosValores_NoAvisaCambio()
        {
            yield return Crear(RutaServicios);
            ServicioPerfil servicio = raiz.GetComponent<ServicioPerfil>();
            servicio.Fijar("Juan", "LA CHISPA");
            int avisos = 0;
            servicio.Cambio += () => avisos++;
            servicio.Fijar("Juan", " LA CHISPA ");
            Assert.AreEqual(0, avisos);
        }

        [UnityTest]
        public IEnumerator Destruir_QuitaElPerfilDelRegistro()
        {
            yield return Crear(RutaServicios);
            Object.Destroy(raiz);
            raiz = null;
            yield return null;
            Assert.AreEqual(0, Registro<IPerfilJugador>.Cantidad);
        }
    }
}
