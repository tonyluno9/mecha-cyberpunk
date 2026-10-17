using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>Guardado, carga y Cambio de la apariencia en ServicioPerfil (sin pasar por Awake).</summary>
    public class ServicioPerfilAparienciaPruebas
    {
        private GameObject go;
        private DatosPerfil datos;
        private ServicioPerfil servicio;

        private ServicioPerfil NuevoServicio(GameObject objeto)
        {
            var nuevo = objeto.AddComponent<ServicioPerfil>();
            var so = new SerializedObject(nuevo);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.ApplyModifiedPropertiesWithoutUndo();
            return nuevo;
        }

        [SetUp]
        public void SetUp()
        {
            datos = ScriptableObject.CreateInstance<DatosPerfil>();
            go = new GameObject("PerfilDePrueba");
            servicio = NuevoServicio(go);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(datos);
        }

        [Test]
        public void Apariencia_SinNadaGuardado_EsPredeterminada()
        {
            Assert.AreEqual(AparienciaJugador.Predeterminada, servicio.Apariencia);
        }

        [Test]
        public void GuardarYCargar_ConAparienciaFijada_LaConserva()
        {
            servicio.Fijar("Juan", "LA CHISPA");
            servicio.FijarApariencia(new AparienciaJugador(2, 3, 1));
            string texto = servicio.Guardar();

            var otroGo = new GameObject("OtroPerfil");
            try
            {
                ServicioPerfil otro = NuevoServicio(otroGo);
                otro.Cargar(texto);
                Assert.AreEqual(new AparienciaJugador(2, 3, 1), otro.Apariencia);
                Assert.AreEqual("LA CHISPA", otro.Alias);
                Assert.AreEqual("Juan", otro.Nombre);
            }
            finally
            {
                Object.DestroyImmediate(otroGo);
            }
        }

        [Test]
        public void Cargar_ConJsonViejoSinApariencia_CargaNombreYAliasConPredeterminada()
        {
            servicio.FijarApariencia(new AparienciaJugador(1, 1, 1));
            servicio.Cargar("{\"nombre\":\"A\",\"alias\":\"B\"}");
            Assert.AreEqual("A", servicio.Nombre);
            Assert.AreEqual("B", servicio.Alias);
            Assert.AreEqual(AparienciaJugador.Predeterminada, servicio.Apariencia);
        }

        [Test]
        public void FijarApariencia_ConLaMisma_NoAvisaCambio()
        {
            servicio.FijarApariencia(new AparienciaJugador(1, 2, 3));
            int avisos = 0;
            servicio.Cambio += () => avisos++;
            servicio.FijarApariencia(new AparienciaJugador(1, 2, 3));
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void FijarApariencia_ConOtra_AvisaCambioUnaVez()
        {
            int avisos = 0;
            servicio.Cambio += () => avisos++;
            servicio.FijarApariencia(new AparienciaJugador(1, 2, 3));
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(new AparienciaJugador(1, 2, 3), servicio.Apariencia);
        }

        [Test]
        public void FijarApariencia_ConNegativos_NormalizaAlGuardar()
        {
            servicio.FijarApariencia(new AparienciaJugador(-3, 2, -1));
            Assert.AreEqual(new AparienciaJugador(0, 2, 0), servicio.Apariencia);
        }

        [Test]
        public void Cargar_ConLaMismaApariencia_NoAvisaCambio()
        {
            servicio.FijarApariencia(new AparienciaJugador(1, 2, 3));
            string texto = servicio.Guardar();
            int avisos = 0;
            servicio.Cambio += () => avisos++;
            servicio.Cargar(texto);
            Assert.AreEqual(0, avisos);
        }
    }
}
