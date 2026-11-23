using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T37 (T-1680): Ciudad_Prologo sin primitivas visibles (fuera de la lista blanca justificada de LimpiezaT1680.MotivoPermitida), sin nada
    /// afuera del muro, sin el bar de cubos ni edificios de relleno a media calle o dentro del casino, y el casino sin pasillo.
    /// Hay que correr LimpiezaT1680.Aplicar antes.
    /// </summary>
    public class LimpiezaT1680Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void AbrirEscena()
        {
            escena = EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        [Test]
        public void Ciudad_PrimitivasVisibles_NingunaFueraDeLaListaBlanca()
        {
            List<Renderer> lista = LimpiezaT1680.PrimitivasVisibles(escena);
            Assert.AreEqual(0, lista.Count, "Primitivas visibles: " + string.Join(" | ", lista.Take(10).Select(r => LimpiezaT1680.LimpiezaRuta(r.transform))));
        }

        [Test]
        public void Ciudad_AfueraDelMuro_NingunRenderer()
        {
            List<Renderer> lista = LimpiezaT1680.RenderersAfuera(escena);
            Assert.AreEqual(0, lista.Count, "Afuera del muro: " + string.Join(" | ", lista.Take(10).Select(r => LimpiezaT1680.LimpiezaRuta(r.transform) + " " + r.bounds.center)));
        }

        [Test]
        public void Ciudad_BarDeCubosRellenosYFlotantes_NoQueda()
        {
            List<LimpiezaT1680.Hallazgo> lista = LimpiezaT1680.Buscar(escena)
                .Where(h => h.Cat == LimpiezaT1680.Cat.Bar || h.Cat == LimpiezaT1680.Cat.RellenoMalPuesto || h.Cat == LimpiezaT1680.Cat.Flotante).ToList();
            Assert.AreEqual(0, lista.Count, string.Join(" | ", lista.Take(10).Select(h => h.Cat + " " + h.Ruta)));
        }

        [Test]
        public void Casino_SinPasilloNiEdificiosAdentro_ConFachadaDeModelos()
        {
            GameObject lugar = GameObject.Find(LimpiezaT1680.RutaLugarCasino);
            Assert.IsNotNull(lugar);
            foreach (string n in new[] { "CuerpoLadoIzquierdo", "CuerpoLadoDerecho", "CuerpoTecho" })
            {
                Transform t = lugar.transform.Find(n);
                Assert.IsTrue(t == null || !t.gameObject.activeInHierarchy, n + " (pasillo) sigue activo");
            }
            foreach (string n in new[] { "Interior/Revestimiento/MuroFrenteIzq", "Interior/Revestimiento/MuroFrenteDer" })
            {
                Renderer r = lugar.transform.Find(n).GetComponent<Renderer>();
                Assert.AreEqual(LimpiezaT1680.XFrenteInterior, r.bounds.center.x, 0.1f, n + ": el frente de la sala va junto a la puerta");
            }
            Renderer piso = lugar.transform.Find("Interior/Revestimiento/Piso").GetComponent<Renderer>();
            Assert.LessOrEqual(piso.bounds.min.x, LimpiezaT1680.XFrenteInterior + 0.05f, "El piso de la sala llega a la puerta");

            Transform fachada = lugar.transform.Find("FachadaIzquierda");
            Assert.IsTrue(fachada.GetComponent<Collider>().enabled && fachada.gameObject.activeInHierarchy, "El cascaron sigue cerrando el casino");
            Assert.IsFalse(fachada.GetComponent<Renderer>().enabled, "El cubo de la fachada ya no se ve");

            GameObject modulos = GameObject.Find(LimpiezaT1680.RaizFachada);
            Assert.IsNotNull(modulos, "Falta la fachada de modelos NV01");
            Assert.Greater(modulos.transform.childCount, 30);

            Rect huella = LimpiezaT1680.HuellaCasino;
            GameObject relleno = GameObject.Find("T1543_Relleno");
            if (relleno != null)
            {
                foreach (Renderer r in relleno.GetComponentsInChildren<Renderer>(false))
                {
                    Assert.IsFalse(r.enabled && huella.Contains(new Vector2(r.bounds.center.x, r.bounds.center.z)), "Edificio dentro del casino: " + r.name);
                }
            }
        }
    }
}
