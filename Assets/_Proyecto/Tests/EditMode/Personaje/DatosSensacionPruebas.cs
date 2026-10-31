using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    public class DatosSensacionPruebas
    {
        [Test]
        public void Ajuste_EscopetaYSniper_PateanMasQueLaPistola()
        {
            var datos = ScriptableObject.CreateInstance<DatosSensacion>();
            float pistola = datos.Ajuste(ArmaAPie.Pistola).patada;
            Assert.Greater(datos.Ajuste(ArmaAPie.Escopeta).patada, pistola);
            Assert.Greater(datos.Ajuste(ArmaAPie.Sniper).patada, datos.Ajuste(ArmaAPie.Escopeta).patada);
            Assert.Greater(datos.Ajuste(ArmaAPie.Punos).patada, 0f, "Los puños sacuden al conectar");
            Assert.AreEqual(0f, datos.Ajuste(ArmaAPie.Punos).destello, "Los puños no tienen destello");
            Object.DestroyImmediate(datos);
        }
    }
}
