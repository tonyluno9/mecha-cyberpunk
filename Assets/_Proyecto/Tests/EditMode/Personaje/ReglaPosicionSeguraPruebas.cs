using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Bug de CONTINUAR: cuándo vale una posición guardada y la red de seguridad.</summary>
    public class ReglaPosicionSeguraPruebas
    {
        [Test]
        public void Evaluar_CadaMotivo()
        {
            Assert.AreEqual(ReglaPosicionSegura.Motivo.Valida, ReglaPosicionSegura.Evaluar(true, 0f, -20f, true, true));
            Assert.AreEqual(ReglaPosicionSegura.Motivo.SinPiso, ReglaPosicionSegura.Evaluar(false, 0f, -20f, true, true), "en el aire");
            Assert.AreEqual(ReglaPosicionSegura.Motivo.PisoBajoElMapa, ReglaPosicionSegura.Evaluar(true, -50f, -20f, true, true));
            Assert.AreEqual(ReglaPosicionSegura.Motivo.FueraDelMapa, ReglaPosicionSegura.Evaluar(true, 0f, -20f, false, true), "fuera del muro");
            Assert.AreEqual(ReglaPosicionSegura.Motivo.Ocupada, ReglaPosicionSegura.Evaluar(true, 0f, -20f, true, false), "dentro de algo");
        }

        [Test]
        public void Candidatos_EmpiezaEnElCentro_YVanDeCercaALejos()
        {
            var lista = ReglaPosicionSegura.Candidatos(new Vector3(5f, 1f, 5f), 6f, 1.5f);
            Assert.AreEqual(new Vector3(5f, 1f, 5f), lista[0]);
            float anterior = 0f;
            foreach (Vector3 p in lista)
            {
                float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(5f, 5f));
                Assert.GreaterOrEqual(d + 1e-3f, anterior, "no se aleja y vuelve");
                Assert.LessOrEqual(d, 6f + 1e-3f);
                anterior = d;
            }
            Assert.Greater(lista.Count, 20);
        }

        [Test]
        public void DebeRescatar_BajoElMapa_OCayendoMuchoBajoElUltimoSeguro()
        {
            Assert.IsFalse(ReglaPosicionSegura.DebeRescatar(0f, 0f, 30f, -20f));
            Assert.IsFalse(ReglaPosicionSegura.DebeRescatar(-10f, 5f, 30f, -20f), "una caída normal no");
            Assert.IsTrue(ReglaPosicionSegura.DebeRescatar(-21f, 0f, 30f, -20f), "bajo el mapa");
            Assert.IsTrue(ReglaPosicionSegura.DebeRescatar(10f, 45f, 30f, -20f), "cayó 35 m desde un techo");
        }
    }
}
