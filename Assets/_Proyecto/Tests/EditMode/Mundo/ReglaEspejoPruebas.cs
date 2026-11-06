using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-854: cuentas del espejo de verdad (ojo reflejado y campo que cubre el vidrio).</summary>
    public class ReglaEspejoPruebas
    {
        [Test]
        public void Reflejar_OjoFrenteAlEspejo_QuedaDetrasALaMismaDistancia()
        {
            Vector3 r = ReglaEspejo.Reflejar(new Vector3(1f, 1.6f, 3f), Vector3.zero, Vector3.forward);
            Assert.AreEqual(new Vector3(1f, 1.6f, -3f).ToString("F3"), r.ToString("F3"));
        }

        [Test]
        public void Reflejar_PuntoEnElPlano_NoSeMueve()
        {
            Vector3 p = new Vector3(0.3f, 1.2f, 0f);
            Assert.AreEqual(p.ToString("F3"), ReglaEspejo.Reflejar(p, Vector3.zero, new Vector3(0f, 0f, 5f)).ToString("F3"));
        }

        [Test]
        public void CampoParaCubrir_VidrioDe1mA1m_CubreLasEsquinasConMargen()
        {
            Vector3[] esquinas = { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            Vector3 ojo = new Vector3(0f, 0f, -1f);
            float campo = ReglaEspejo.CampoParaCubrir(ojo, Vector3.forward, esquinas, 4f, 10f, 150f);
            float esquina = Vector3.Angle(Vector3.forward, esquinas[3] - ojo);
            Assert.AreEqual(esquina * 2f + 4f, campo, 0.01f);
            Assert.AreEqual(150f, ReglaEspejo.CampoParaCubrir(new Vector3(0f, 0f, -0.01f), Vector3.forward, esquinas, 4f, 10f, 150f), 0.01f, "de muy cerca se topa");
        }
    }
}
