using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1503: la regla de ondas guarda las últimas pisadas y deja vivas solo las recientes.</summary>
    public class OndasCharcoT1503Pruebas
    {
        [Test]
        public void Agregar_MasDelMaximo_GuardaSoloLasUltimas()
        {
            ReglaOndasCharco regla = new ReglaOndasCharco();
            int total = ReglaOndasCharco.Maximo + 3;
            for (int i = 0; i < total; i++)
            {
                regla.Agregar(new Vector3(i, 0f, 0f), i * 0.1f);
            }
            Assert.AreEqual(ReglaOndasCharco.Maximo, regla.Guardadas);
            Assert.AreEqual(3f, regla.Posicion(0).x, "la más vieja que queda es la 4a");
            Assert.AreEqual(total - 1, regla.Posicion(ReglaOndasCharco.Maximo - 1).x, "la última es la más nueva");
        }

        [Test]
        public void Vigentes_PasadaLaVida_YaNoCuentan()
        {
            ReglaOndasCharco regla = new ReglaOndasCharco();
            regla.Agregar(Vector3.zero, 0f);
            regla.Agregar(Vector3.one, 5f);
            Assert.AreEqual(1, regla.Vigentes(5.5f));
            Assert.AreEqual(2, regla.Vigentes(1f));
            Assert.AreEqual(0, regla.Vigentes(5f + ReglaOndasCharco.Vida + 0.1f));
        }

        [Test]
        public void Llenar_PoneLasVivasAlPrincipio_YLimpiaElResto()
        {
            ReglaOndasCharco regla = new ReglaOndasCharco();
            regla.Agregar(new Vector3(1f, 0f, 2f), 0f);
            regla.Agregar(new Vector3(3f, 0f, 4f), 10f);
            Vector4[] destino = new Vector4[ReglaOndasCharco.Maximo];
            int n = regla.Llenar(destino, 10.5f);
            Assert.AreEqual(1, n);
            Assert.AreEqual(new Vector4(3f, 0f, 4f, 10f), destino[0]);
            Assert.AreEqual(Vector4.zero, destino[1]);
        }
    }
}
