using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1686: lo que pidió la sesión B (ronda D) en Ciudad_Prologo: el marcador Historia_EsperaSkulls a media calle frente a la casa, una máquina
    /// CY06 entre el CIC y el Tianguis, y ningún PuntoTrepar activo que suba a una azotea. Hay que correr ColocarSesionBT1686.Aplicar antes.
    /// </summary>
    public class ColocarSesionBT1686Pruebas
    {
        [OneTimeSetUp]
        public void AbrirEscena()
        {
            EditorSceneManager.OpenScene(ColocarSesionBT1686.RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        [Test]
        public void EsperaSkulls_AMediaCalleFrenteALaCasa_SobreAsfalto()
        {
            Transform espera = ColocarSesionBT1686.Buscar(ColocarSesionBT1686.NombreEspera);
            Transform inicio = ColocarSesionBT1686.Buscar(ColocarSesionBT1686.NombreInicio);
            Assert.IsNotNull(espera, "Falta Historia_EsperaSkulls");
            Assert.IsNotNull(inicio, "Falta Inicio_Calle");
            Assert.AreEqual(1, ColocarSesionBT1686.TodosConNombre(ColocarSesionBT1686.NombreEspera).Count(), "Debe haber un solo marcador");
            Vector3 d = espera.position - inicio.position;
            d.y = 0f;
            Assert.That(d.magnitude, Is.InRange(ColocarSesionBT1686.MinEspera, ColocarSesionBT1686.MaxEspera), "Distancia al Inicio_Calle");
            Assert.Greater(Vector3.Dot(d.normalized, inicio.forward), 0.8f, "Va hacia la calle, delante del inicio");
            Assert.IsTrue(Physics.Raycast(espera.position + Vector3.up * 3f, Vector3.down, out RaycastHit h, 4f, ~0, QueryTriggerInteraction.Ignore));
            StringAssert.StartsWith("Asfalto", h.collider.name);
            Assert.AreEqual(h.point.y, espera.position.y, 0.05f, "Sobre el piso");
        }

        [Test]
        public void Expendedora_EntreCicYTianguis_PegadaAPared()
        {
            Transform m = ColocarSesionBT1686.Buscar(ColocarSesionBT1686.NombreMaquina);
            Assert.IsNotNull(m, "Falta la expendedora");
            Assert.IsTrue(m.gameObject.activeInHierarchy);
            Assert.That(m.position.x, Is.InRange(ColocarSesionBT1686.XCic, ColocarSesionBT1686.XTianguis));
            // El frente del CY06 mira a -Z local: detrás (+Z local) hay pared.
            Assert.IsTrue(Physics.Raycast(m.position + Vector3.up * 1.2f + m.forward * 0.2f, m.forward, 0.6f, ~0, QueryTriggerInteraction.Ignore), "Sin pared detrás");
        }

        [Test]
        public void PuntoTrepar_NingunoActivoAAzotea()
        {
            var lista = ColocarSesionBT1686.ActivosAAzotea();
            Assert.AreEqual(0, lista.Count, string.Join(" | ", lista.Take(10).Select(p => ColocarSesionBT1686.Ruta(p.transform))));
        }
    }
}
