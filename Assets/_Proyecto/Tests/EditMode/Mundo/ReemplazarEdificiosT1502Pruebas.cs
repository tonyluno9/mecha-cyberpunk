using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tipo = MechaCyberpunk.Mundo.Editor.CalculoEdificiosT1502.Tipo;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1502: edificios de primitivas cambiados por modelos. Las cuentas son puras; la última prueba mira la escena ya procesada.</summary>
    public class ReemplazarEdificiosT1502Pruebas
    {
        [Test]
        public void EsMallaPrimitiva_MallaDeUnityOSinArchivo_EsPrimitiva()
        {
            Assert.IsTrue(CalculoEdificiosT1502.EsMallaPrimitiva(""));
            Assert.IsTrue(CalculoEdificiosT1502.EsMallaPrimitiva("Library/unity default resources"));
            Assert.IsFalse(CalculoEdificiosT1502.EsMallaPrimitiva("Assets/Arte/Zonas/CC/CC01_CasaCerro/CC01_CasaCerro.fbx"));
        }

        [Test]
        public void Clasificar_CuboEdificioDeLasAfueras_EsReemplazable()
        {
            Assert.AreEqual(Tipo.EdificioAfueras, CalculoEdificiosT1502.Clasificar("T530_Afueras/Salida_1/Edificio", new Vector3(8f, 26f, 44f)));
        }

        [Test]
        public void Clasificar_EdificioDeBarrio_SeAnotaPorElKit()
        {
            Assert.AreEqual(Tipo.EdificioConKit, CalculoEdificiosT1502.Clasificar("S2n_Calles/Edificios/Edificio", new Vector3(22f, 12f, 23f)));
        }

        [Test]
        public void Clasificar_TorresMurosYLocales_TienenSuTipo()
        {
            Assert.AreEqual(Tipo.Torre, CalculoEdificiosT1502.Clasificar("T_NeonAlto/Torres/Torre_1/Cuerpo", new Vector3(30f, 128f, 27f)));
            Assert.AreEqual(Tipo.Muro, CalculoEdificiosT1502.Clasificar("T_Afueras/Orillas/Muro", new Vector3(2f, 6f, 78f)));
            Assert.AreEqual(Tipo.LocalConInterior, CalculoEdificiosT1502.Clasificar("T_Locales/CIC/MuroFondo", new Vector3(8.1f, 3.6f, 0.1f * 80f)));
        }

        [Test]
        public void Clasificar_CosasDelgadasONiebla_NoSonEdificios()
        {
            Assert.AreEqual(Tipo.Ninguno, CalculoEdificiosT1502.Clasificar("T_Afueras/Salida_1/Ventanas", new Vector3(29f, 20f, 0.1f)));
            Assert.AreEqual(Tipo.Ninguno, CalculoEdificiosT1502.Clasificar("T_NeonAlto/Niebla/Niebla_1", new Vector3(20f, 14f, 1f)));
            Assert.AreEqual(Tipo.Ninguno, CalculoEdificiosT1502.Clasificar("AMB_Noche/Nubes", new Vector3(1400f, 1400f, 1400f)));
        }

        [Test]
        public void Cardinal_DireccionDiagonal_ElijeElEjeMayor()
        {
            Assert.AreEqual(new Vector3(-1f, 0f, 0f), CalculoEdificiosT1502.Cardinal(new Vector3(-5f, 0f, 2f)));
            Assert.AreEqual(new Vector3(0f, 0f, 1f), CalculoEdificiosT1502.Cardinal(new Vector3(1f, 0f, 4f)));
        }

        [Test]
        public void Ajustar_ModeloDelTamanoDelHueco_UnTramoSinEscalar()
        {
            int tramos;
            Vector3 escala;
            float costo;
            Assert.IsTrue(CalculoEdificiosT1502.Ajustar(new Vector3(10f, 12f, 8f), 10f, 8f, 12f, out tramos, out escala, out costo));
            Assert.AreEqual(1, tramos);
            Assert.AreEqual(1f, escala.x, 0.001f);
            Assert.AreEqual(1f, escala.y, 0.001f);
            Assert.AreEqual(1f, escala.z, 0.001f);
        }

        [Test]
        public void Ajustar_FrenteLargo_PartePorTramos()
        {
            int tramos;
            Vector3 escala;
            float costo;
            Assert.IsTrue(CalculoEdificiosT1502.Ajustar(new Vector3(10f, 12f, 8f), 40f, 8f, 12f, out tramos, out escala, out costo));
            Assert.AreEqual(4, tramos);
            Assert.AreEqual(1f, escala.x, 0.001f);
        }

        [Test]
        public void Ajustar_HuecoMuyEstirado_Falla()
        {
            int tramos;
            Vector3 escala;
            float costo;
            Assert.IsFalse(CalculoEdificiosT1502.Ajustar(new Vector3(10f, 4f, 10f), 10f, 10f, 200f, out tramos, out escala, out costo));
            Assert.AreEqual(0, tramos);
        }

        // Pasa solo después de correr ReemplazarEdificiosT1502.Reemplazar (que guarda la escena): ningún cubo de edificio de las afueras
        // queda visible sin modelo ni marca T1502_Pendiente. Las demás primitivas están anotadas por tipo en Logs/t1502-pendientes.txt.
        [Test]
        public void Escena_EdificiosDeLasAfueras_TodosConModeloOAnotados()
        {
            Scene escena = EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Additive);
            try
            {
                var sinCambiar = ReemplazarEdificiosT1502.Escanear(escena)
                    .Where(p => p.Tipo == Tipo.EdificioAfueras && !p.TienePendiente).Select(p => p.Ruta).ToList();
                Assert.AreEqual(0, sinCambiar.Count, "Cubos de las afueras sin modelo ni anotar: " + string.Join(", ", sinCambiar.Take(5)));
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
