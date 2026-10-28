using System;
using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Core
{
    public class ContratosImplantesPruebas
    {
        private class ImplantesFalso : IImplantes
        {
            private readonly int[] niveles = new int[4];
            private readonly string[] nombres = { "", "", "", "" };
            public event Action Cambio;
            public event Action<EspacioImplante, int> Instalado;

            public int Nivel(EspacioImplante espacio) => niveles[(int)espacio];
            public string Nombre(EspacioImplante espacio) => nombres[(int)espacio] ?? "";

            public int Cantidad
            {
                get
                {
                    int n = 0;
                    foreach (int nivel in niveles) if (nivel > 0) n++;
                    return n;
                }
            }

            public EfectosImplantes Efectos
            {
                get
                {
                    var e = EfectosImplantes.Ninguno;
                    if (niveles[(int)EspacioImplante.Brazos] > 0) e.multiplicadorGolpeAPie = 1.5f;
                    if (niveles[(int)EspacioImplante.Ojos] > 0) { e.verVidaRival = true; e.anticipacionAtaque = 0.4f; }
                    return e;
                }
            }

            public void Instalar(EspacioImplante espacio, int nivel, string nombre)
            {
                niveles[(int)espacio] = nivel;
                nombres[(int)espacio] = nombre;
                Instalado?.Invoke(espacio, nivel);
                Cambio?.Invoke();
            }

            public void Cargar(EspacioImplante espacio, int nivel, string nombre)
            {
                niveles[(int)espacio] = nivel;
                nombres[(int)espacio] = nombre;
                Cambio?.Invoke();
            }
        }

        [Test]
        public void Ninguno_EsNeutro()
        {
            var e = EfectosImplantes.Ninguno;
            Assert.AreEqual(1f, e.multiplicadorGolpeAPie);
            Assert.AreEqual(1f, e.multiplicadorCorrer);
            Assert.AreEqual(1f, e.multiplicadorSalto);
            Assert.AreEqual(1f, e.multiplicadorReaccionMecha);
            Assert.IsFalse(e.verVidaRival);
            Assert.IsFalse(e.vidaRivalEnNumeros);
            Assert.AreEqual(0f, e.anticipacionAtaque);
        }

        [Test]
        public void Combinar_MultiplicaHaceOrYTomaMaximo()
        {
            var a = EfectosImplantes.Ninguno;
            a.multiplicadorGolpeAPie = 1.5f; a.multiplicadorCorrer = 1.2f; a.verVidaRival = true; a.anticipacionAtaque = 0.4f;
            var b = EfectosImplantes.Ninguno;
            b.multiplicadorGolpeAPie = 2f; b.multiplicadorSalto = 1.3f; b.multiplicadorReaccionMecha = 0.85f;
            b.vidaRivalEnNumeros = true; b.anticipacionAtaque = 0.8f;
            var c = EfectosImplantes.Combinar(a, b);
            Assert.AreEqual(3f, c.multiplicadorGolpeAPie, 1e-5f);
            Assert.AreEqual(1.2f, c.multiplicadorCorrer, 1e-5f);
            Assert.AreEqual(1.3f, c.multiplicadorSalto, 1e-5f);
            Assert.AreEqual(0.85f, c.multiplicadorReaccionMecha, 1e-5f);
            Assert.IsTrue(c.verVidaRival);
            Assert.IsTrue(c.vidaRivalEnNumeros);
            Assert.AreEqual(0.8f, c.anticipacionAtaque, 1e-5f);
        }

        [Test]
        public void Combinar_ConNinguno_NoCambiaNada()
        {
            var a = EfectosImplantes.Ninguno;
            a.multiplicadorGolpeAPie = 1.5f; a.multiplicadorReaccionMecha = 0.7f; a.verVidaRival = true; a.anticipacionAtaque = 0.4f;
            Assert.AreEqual(a, EfectosImplantes.Combinar(a, EfectosImplantes.Ninguno));
            Assert.AreEqual(a, EfectosImplantes.Combinar(EfectosImplantes.Ninguno, a));
        }

        [Test]
        public void Nombre_NuncaEsNull_YCantidadCuenta()
        {
            var imp = new ImplantesFalso();
            foreach (EspacioImplante e in Enum.GetValues(typeof(EspacioImplante)))
            {
                Assert.IsNotNull(imp.Nombre(e));
                Assert.AreEqual(0, imp.Nivel(e));
            }
            Assert.AreEqual(0, imp.Cantidad);
            imp.Instalar(EspacioImplante.Ojos, 1, "OJOS DE LECHUZA");
            Assert.AreEqual("OJOS DE LECHUZA", imp.Nombre(EspacioImplante.Ojos));
            Assert.AreEqual(1, imp.Cantidad);
        }

        [Test]
        public void Instalado_NoSeAvisaAlCargar_PeroCambioSi()
        {
            var imp = new ImplantesFalso();
            int instalados = 0, cambios = 0;
            imp.Instalado += (e, n) => instalados++;
            imp.Cambio += () => cambios++;
            imp.Cargar(EspacioImplante.Brazos, 2, "BRAZOS DE PRENSA");
            Assert.AreEqual(0, instalados);
            Assert.AreEqual(1, cambios);
            imp.Instalar(EspacioImplante.Piernas, 1, "PIERNAS DE RESORTE");
            Assert.AreEqual(1, instalados);
            Assert.AreEqual(2, cambios);
        }

        [Test]
        public void SinNadaRegistrado_QuienLeeObtieneNinguno()
        {
            IImplantes imp = Registro<IImplantes>.Actual;
            var efectos = imp != null ? imp.Efectos : EfectosImplantes.Ninguno;
            int cantidad = imp != null ? imp.Cantidad : 0;
            Assert.AreEqual(EfectosImplantes.Ninguno, efectos);
            Assert.AreEqual(0, cantidad);
        }
    }
}
