using System.Collections;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR42: con una pantalla abierta, lo que queda entre la cabeza y el panel (el NPC que la abrió, un mueble) no se dibuja; al cerrar vuelve.
    /// Lo de los lados y lo grande (paredes, piso) no se toca.
    /// </summary>
    public class MenusVR42Pruebas : BaseJugadorVR
    {
        [Test]
        public void CruzaCaja_DentroSiFueraNo()
        {
            var medio = new Vector3(0.8f, 0.45f, 0.6f);
            Assert.IsTrue(OcultarTapanVR.CruzaCaja(new Bounds(new Vector3(0f, 0f, 0.3f), Vector3.one * 0.3f), Vector3.zero, Quaternion.identity, medio));
            Assert.IsFalse(OcultarTapanVR.CruzaCaja(new Bounds(new Vector3(3f, 0f, 0f), Vector3.one * 0.3f), Vector3.zero, Quaternion.identity, medio));
            Assert.IsTrue(OcultarTapanVR.Grande(new Bounds(Vector3.zero, new Vector3(10f, 3f, 0.2f))), "Una pared no se oculta.");
            Assert.IsFalse(OcultarTapanVR.Grande(new Bounds(Vector3.zero, new Vector3(0.6f, 1.8f, 0.4f))), "Una persona sí.");
        }

        [UnityTest]
        public IEnumerator PantallaAbierta_OcultaLoQueTapaElPanelYAlCerrarVuelve()
        {
            yield return ArmarVR();
            yield return Segundos(0.5f);
            Transform cabeza = adaptador.Camara.transform;
            Vector3 frente = ReglaLienzosVR.FrentePlano(cabeza.forward, cabeza.up);
            // Abajo de la línea de los ojos (el rayo que acerca el panel a las paredes pasa por encima): queda entre la cabeza y el panel.
            GameObject tapa = Caja(cabeza.position + frente * 0.9f + Vector3.down * 0.3f, new Vector3(0.3f, 0.3f, 0.3f));
            tapa.name = "TapaPruebaVR42";
            GameObject lado = Caja(cabeza.position + frente * 0.9f + Vector3.Cross(Vector3.up, frente) * 2.5f, new Vector3(0.3f, 0.3f, 0.3f));
            lado.name = "LadoPruebaVR42";
            Renderer rTapa = tapa.GetComponent<Renderer>();
            Renderer rLado = lado.GetComponent<Renderer>();
            Renderer rPiso = piso.GetComponent<Renderer>();
            var pantalla = new GameObject("PantallaPruebaVR42");
            extras.Add(pantalla);
            PilaPantallas.Abrir(pantalla);
            yield return Cuadros(4);
            Assert.IsTrue(MenusVR.MenuAbierto);
            Assert.IsTrue(rTapa.forceRenderingOff, "Lo que está entre la cabeza y el panel no se dibuja.");
            Assert.IsTrue(MenusVR.Actual.Tapan.Oculta(rTapa));
            Assert.IsFalse(rLado.forceRenderingOff, "Lo de los lados se sigue viendo.");
            Assert.IsFalse(rPiso.forceRenderingOff, "El piso (grande) no se oculta.");
            Assert.IsTrue(tapa.GetComponent<Collider>().enabled, "Solo se apaga el dibujo: la colisión sigue.");

            PilaPantallas.Cerrar(pantalla);
            yield return Cuadros(4);
            Assert.IsFalse(MenusVR.MenuAbierto);
            Assert.IsFalse(rTapa.forceRenderingOff, "Al cerrar la pantalla vuelve a verse.");
            Assert.AreEqual(0, MenusVR.Actual.Tapan.Ocultos);
        }
    }
}
