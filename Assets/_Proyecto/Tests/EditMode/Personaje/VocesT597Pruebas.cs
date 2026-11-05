using System;
using MechaCyberpunk.Combate.Voces;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-597: la voz depende del género (jugador por su cara, pandilleros y aliados por semilla) y el gestor entiende la etiqueta.</summary>
    public class VocesT597Pruebas
    {
        private sealed class PerfilFalso : IPerfilJugador
        {
            public PerfilFalso(int cara) { Apariencia = new AparienciaJugador(cara, 0, 0); }
            public string Nombre => "N";
            public string Alias => "A";
            public AparienciaJugador Apariencia { get; }
            public string NombrePandilla => "P";
            public int ColorPandilla => 0;
            public event Action Cambio { add { } remove { } }
        }

        [TearDown]
        public void Limpiar() => Registros.LimpiarTodos();

        [Test]
        public void Etiqueta_ElSeparadorCoincideConElDelGestor()
        {
            Assert.AreEqual(ReglaVozGenero.Separador, EtiquetaVoz.Separador);
            Assert.AreEqual("JUG_DANIO@F", EtiquetaVoz.De("JUG_DANIO", GeneroVoz.Femenina));
            Assert.AreEqual("JUG_DANIO@M", EtiquetaVoz.De("JUG_DANIO", GeneroVoz.Masculina));
            Assert.AreEqual("JUG_DANIO", ReglaVozGenero.Separar("JUG_DANIO@F", out string e));
            Assert.AreEqual("F", e);
        }

        [Test]
        public void Jugador_ElGeneroSaleDeLaCaraDelPerfil()
        {
            Registro<IPerfilJugador>.Agregar(new PerfilFalso(0));
            Assert.AreEqual(GeneroVoz.Femenina, VozJugador.Genero());
            Assert.AreEqual("JUG_CAE_DERROTADO@F", VozJugador.Id("JUG_CAE_DERROTADO"));
            Registros.LimpiarTodos();
            Registro<IPerfilJugador>.Agregar(new PerfilFalso(1));
            Assert.AreEqual(GeneroVoz.Masculina, VozJugador.Genero());
            Assert.AreEqual("JUG_DANIO@M", VozJugador.Id("JUG_DANIO"));
        }

        [Test]
        public void Jugador_SinPerfil_EsMasculina()
        {
            Assert.AreEqual(GeneroVoz.Masculina, VozJugador.Genero());
        }

        [Test]
        public void Semilla_UnaDeCadaCuatroPandillerosEsFemenina_YEsEstable()
        {
            int femeninas = 0;
            for (int i = -400; i < 400; i++)
            {
                GeneroVoz g = EtiquetaVoz.PorSemilla(i, 4);
                Assert.AreEqual(g, EtiquetaVoz.PorSemilla(i, 4));
                if (g == GeneroVoz.Femenina) femeninas++;
            }
            Assert.AreEqual(200, femeninas);
        }
    }
}
