using System.Collections.Generic;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1110: la pelea del jefe final de Nova (fases por vida, escudo de energía, ciclos de ataques, avisos y velocidad de la fase Furia).</summary>
    public class ReglaJefeFinalNovaPruebas
    {
        private const float Dt = 0.01f;

        private static ReglaJefeFinalNova Crear() => new ReglaJefeFinalNova(ReglaJefeFinalNova.Parametros.Diseno);

        private static ReglaJefeFinalNova.Entrada En(float distancia, float vida = 1f, bool enPelea = true)
        {
            return new ReglaJefeFinalNova.Entrada { Distancia = distancia, VidaFraccion = vida, EnPelea = enPelea };
        }

        private static void Pasar(ReglaJefeFinalNova r, float segundos, float distancia = 2f, float vida = 1f)
        {
            for (float t = 0f; t < segundos - 0.0001f; t += Dt)
            {
                r.Avanzar(Dt, En(distancia, vida));
            }
        }

        // Fuerza la fase Escudo (vida 60%) y espera a que el jefe esté caminando sin ataque en curso.
        private static ReglaJefeFinalNova EnEscudo()
        {
            ReglaJefeFinalNova r = Crear();
            r.Avanzar(Dt, En(20f, 0.6f));
            Assert.AreEqual(ReglaJefeFinalNova.FaseJefe.Escudo, r.Fase);
            return r;
        }

        [TestCase(1f, ReglaJefeFinalNova.FaseJefe.Asedio)]
        [TestCase(0.67f, ReglaJefeFinalNova.FaseJefe.Asedio)]
        [TestCase(0.66f, ReglaJefeFinalNova.FaseJefe.Escudo)]
        [TestCase(0.5f, ReglaJefeFinalNova.FaseJefe.Escudo)]
        [TestCase(0.34f, ReglaJefeFinalNova.FaseJefe.Escudo)]
        [TestCase(0.33f, ReglaJefeFinalNova.FaseJefe.Furia)]
        [TestCase(0.1f, ReglaJefeFinalNova.FaseJefe.Furia)]
        public void Fase_SegunLaVida(float vida, ReglaJefeFinalNova.FaseJefe esperada)
        {
            ReglaJefeFinalNova r = Crear();
            r.Avanzar(Dt, En(20f, vida));
            Assert.AreEqual(esperada, r.Fase);
        }

        [Test]
        public void CambioA66_PrendeElEscudoUnaSolaVezConCientoCuarentaPuntos()
        {
            ReglaJefeFinalNova r = Crear();
            Assert.IsFalse(r.EscudoActivo);
            ReglaJefeFinalNova.Salida s = r.Avanzar(Dt, En(20f, 0.66f));
            Assert.IsTrue(s.CambioFase);
            Assert.IsTrue(s.EscudoActivado, "el grito y la esfera salen al cambiar");
            Assert.IsTrue(r.EscudoActivo);
            Assert.AreEqual(140f, r.AguanteEscudo, 0.001f);
            Assert.IsFalse(r.Avanzar(Dt, En(20f, 0.6f)).EscudoActivado, "una sola vez");
        }

        [Test]
        public void CambioA33_QuitaElEscudoYNoVuelveAPrenderlo()
        {
            ReglaJefeFinalNova r = EnEscudo();
            ReglaJefeFinalNova.Salida s = r.Avanzar(Dt, En(20f, 0.32f));
            Assert.IsTrue(s.CambioFase);
            Assert.AreEqual(ReglaJefeFinalNova.FaseJefe.Furia, r.Fase);
            Assert.IsFalse(r.EscudoActivo, "sin escudo en la fase Furia");
            Assert.IsFalse(r.AbsorberDanio(10f).Absorbido);
        }

        [Test]
        public void FaseNuncaRetrocede_AunqueLaVidaSuba()
        {
            ReglaJefeFinalNova r = EnEscudo();
            r.Avanzar(Dt, En(20f, 0.9f));
            Assert.AreEqual(ReglaJefeFinalNova.FaseJefe.Escudo, r.Fase);
        }

        [Test]
        public void Escudo_AbsorbeTodoElDanioYSeRompeAlLlegarACero()
        {
            ReglaJefeFinalNova r = EnEscudo();
            ReglaJefeFinalNova.ResultadoEscudo uno = r.AbsorberDanio(100f);
            Assert.IsTrue(uno.Absorbido);
            Assert.IsFalse(uno.SeRompio);
            Assert.AreEqual(40f, r.AguanteEscudo, 0.001f);
            ReglaJefeFinalNova.ResultadoEscudo dos = r.AbsorberDanio(40f);
            Assert.IsTrue(dos.Absorbido, "el golpe que lo rompe también se absorbe");
            Assert.IsTrue(dos.SeRompio);
            Assert.IsFalse(r.EscudoActivo);
            Assert.AreEqual(ReglaJefeFinalNova.Accion.Aturdido, r.Actual);
            Assert.IsTrue(r.Castigable);
            Assert.IsFalse(r.AbsorberDanio(10f).Absorbido, "roto, el daño pasa");
        }

        [Test]
        public void Aturdido_DuraTresSegundos()
        {
            ReglaJefeFinalNova r = EnEscudo();
            r.AbsorberDanio(140f);
            Pasar(r, 2.9f, 20f, 0.6f);
            Assert.AreEqual(ReglaJefeFinalNova.Accion.Aturdido, r.Actual, "a 2.9 s sigue aturdido");
            Pasar(r, 0.2f, 20f, 0.6f);
            Assert.AreNotEqual(ReglaJefeFinalNova.Accion.Aturdido, r.Actual);
            Assert.IsFalse(r.Castigable);
        }

        [Test]
        public void Escudo_VuelveALosDiezSegundosSiLaVidaSigueSobre33()
        {
            ReglaJefeFinalNova r = EnEscudo();
            r.AbsorberDanio(140f);
            bool regreso = false;
            float t = 0f;
            while (t < 9.9f)
            {
                regreso |= r.Avanzar(Dt, En(20f, 0.5f)).EscudoRegreso;
                t += Dt;
            }
            Assert.IsFalse(regreso, "a 9.9 s todavía no");
            Assert.IsFalse(r.EscudoActivo);
            while (t < 10.2f)
            {
                regreso |= r.Avanzar(Dt, En(20f, 0.5f)).EscudoRegreso;
                t += Dt;
            }
            Assert.IsTrue(regreso);
            Assert.IsTrue(r.EscudoActivo);
            Assert.AreEqual(140f, r.AguanteEscudo, 0.001f);
        }

        [Test]
        public void Escudo_NoVuelveSiLaVidaBajoDe33()
        {
            ReglaJefeFinalNova r = EnEscudo();
            r.AbsorberDanio(140f);
            Pasar(r, 11f, 20f, 0.3f);
            Assert.AreEqual(ReglaJefeFinalNova.FaseJefe.Furia, r.Fase);
            Assert.IsFalse(r.EscudoActivo, "bajo 33% no regresa");
        }

        [Test]
        public void Asedio_CadaTresAcciones_UnCanonazo()
        {
            ReglaJefeFinalNova r = Crear();
            List<ReglaJefeFinalNova.Ataque> ataques = new List<ReglaJefeFinalNova.Ataque>();
            for (float t = 0f; t < 20f && ataques.Count < 6; t += Dt)
            {
                ReglaJefeFinalNova.Salida s = r.Avanzar(Dt, En(2f));
                if (s.EmpezoAviso)
                {
                    ataques.Add(s.Tipo);
                }
            }
            CollectionAssert.AreEqual(new[]
            {
                ReglaJefeFinalNova.Ataque.Punetazo, ReglaJefeFinalNova.Ataque.Punetazo, ReglaJefeFinalNova.Ataque.Canon,
                ReglaJefeFinalNova.Ataque.Punetazo, ReglaJefeFinalNova.Ataque.Punetazo, ReglaJefeFinalNova.Ataque.Canon
            }, ataques);
        }

        [Test]
        public void Punetazo_AvisoDeNueveDecimasYLejosNoAtaca()
        {
            ReglaJefeFinalNova r = Crear();
            for (int i = 0; i < 300; i++)
            {
                Assert.IsFalse(r.Avanzar(Dt, En(6f)).EmpezoAviso, "a 6 m no lanza el puñetazo (alcance 3 m)");
            }
            Assert.AreEqual(ReglaJefeFinalNova.Accion.Camina, r.Actual);
            float t = 0f;
            bool empezo = false;
            while (t < 2f)
            {
                ReglaJefeFinalNova.Salida s = r.Avanzar(Dt, En(2.5f));
                t += Dt;
                empezo |= s.EmpezoAviso;
                if (s.ImpactoPunetazo)
                {
                    break;
                }
            }
            Assert.IsTrue(empezo);
            Assert.AreEqual(0.9f, t, 0.03f);
        }

        [Test]
        public void Canonazo_UnArosEnElJugadorYExplotaAlos1Punto2Segundos()
        {
            ReglaJefeFinalNova r = Crear();
            r.ForzarIndice(2);
            ReglaJefeFinalNova.Salida primera = r.Avanzar(Dt, En(10f));
            Assert.IsNotNull(primera.NuevosAros);
            CollectionAssert.AreEqual(new[] { ReglaJefeFinalNova.LugarAro.Jugador }, primera.NuevosAros);
            float t = Dt;
            int explotan = 0;
            while (t < 2f && explotan == 0)
            {
                explotan += r.Avanzar(Dt, En(10f)).Explotan;
                t += Dt;
            }
            Assert.AreEqual(1, explotan);
            Assert.AreEqual(1.2f, t, 0.03f);
        }

        [Test]
        public void Escudo_AlternaPunetazoOndaYDobleCanonazo()
        {
            ReglaJefeFinalNova r = EnEscudo();
            List<ReglaJefeFinalNova.Ataque> ataques = new List<ReglaJefeFinalNova.Ataque>();
            for (float t = 0f; t < 30f && ataques.Count < 6; t += Dt)
            {
                ReglaJefeFinalNova.Salida s = r.Avanzar(Dt, En(2f, 0.6f));
                if (s.EmpezoAviso)
                {
                    ataques.Add(s.Tipo);
                }
            }
            CollectionAssert.AreEqual(new[]
            {
                ReglaJefeFinalNova.Ataque.Punetazo, ReglaJefeFinalNova.Ataque.Onda, ReglaJefeFinalNova.Ataque.DobleCanon,
                ReglaJefeFinalNova.Ataque.Punetazo, ReglaJefeFinalNova.Ataque.Onda, ReglaJefeFinalNova.Ataque.DobleCanon
            }, ataques);
        }

        [Test]
        public void DobleCanonazo_ElSegundoAroApareceCuandoExplotaElPrimero()
        {
            ReglaJefeFinalNova r = EnEscudo();
            r.ForzarIndice(2);
            List<float> aparecen = new List<float>();
            List<float> explotan = new List<float>();
            float t = 0f;
            while (t < 3f && explotan.Count < 2)
            {
                ReglaJefeFinalNova.Salida s = r.Avanzar(Dt, En(10f, 0.6f));
                t += Dt;
                if (s.NuevosAros != null) aparecen.Add(t);
                for (int i = 0; i < s.Explotan; i++) explotan.Add(t);
            }
            Assert.AreEqual(2, aparecen.Count);
            Assert.AreEqual(0f, aparecen[0], 0.03f);
            Assert.AreEqual(1.2f, aparecen[1], 0.03f);
            Assert.AreEqual(1.2f, explotan[0], 0.03f);
            Assert.AreEqual(2.4f, explotan[1], 0.03f);
        }

        [Test]
        public void Onda_AvisoDeUnPuntoDosYAroDeCeroANueveMetrosEnOchoDecimas()
        {
            ReglaJefeFinalNova r = EnEscudo();
            r.ForzarIndice(1);
            float t = 0f;
            while (!r.Avanzar(Dt, En(5f, 0.6f)).EmpezoOnda && t < 3f)
            {
                t += Dt;
            }
            Assert.AreEqual(1.2f, t, 0.04f, "aviso de la onda");
            Assert.AreEqual(ReglaJefeFinalNova.Accion.Onda, r.Actual);
            Assert.AreEqual(0f, r.RadioOnda, 0.2f);
            Pasar(r, 0.4f, 5f, 0.6f);
            Assert.AreEqual(4.5f, r.RadioOnda, 0.3f, "a media onda, 4.5 m");
            Pasar(r, 0.38f, 5f, 0.6f);
            Assert.AreEqual(9f, r.RadioOnda, 0.4f);
        }

        [Test]
        public void Furia_AvisosVeintePorCientoMasCortosYTreintaPorCientoMasRapido()
        {
            ReglaJefeFinalNova r = Crear();
            Assert.AreEqual(1f, r.FactorAviso, 0.001f);
            Assert.AreEqual(1f, r.FactorVelocidad, 0.001f);
            Assert.AreEqual(0.9f, r.DuracionAvisoPunetazo, 0.001f);
            r.Avanzar(Dt, En(20f, 0.3f));
            Assert.AreEqual(ReglaJefeFinalNova.FaseJefe.Furia, r.Fase);
            Assert.AreEqual(0.8f, r.FactorAviso, 0.001f);
            Assert.AreEqual(1.3f, r.FactorVelocidad, 0.001f);
            Assert.AreEqual(0.72f, r.DuracionAvisoPunetazo, 0.001f);
            Assert.AreEqual(0.96f, r.DuracionAvisoOnda, 0.001f);
            Assert.AreEqual(0.96f, r.DuracionAvisoCanon, 0.001f);
        }

        [Test]
        public void Furia_PunetazoSeguidoDeOndaYLuegoRafagaDeTresAros()
        {
            ReglaJefeFinalNova r = Crear();
            r.Avanzar(Dt, En(20f, 0.3f));
            List<ReglaJefeFinalNova.Ataque> ataques = new List<ReglaJefeFinalNova.Ataque>();
            ReglaJefeFinalNova.LugarAro[] aros = null;
            float tPunetazo = -1f;
            float tOnda = -1f;
            float t = 0f;
            while (t < 12f && ataques.Count < 3)
            {
                ReglaJefeFinalNova.Salida s = r.Avanzar(Dt, En(2f, 0.3f));
                t += Dt;
                if (s.EmpezoAviso)
                {
                    ataques.Add(s.Tipo);
                    if (s.Tipo == ReglaJefeFinalNova.Ataque.Onda) tOnda = t;
                    if (s.Tipo == ReglaJefeFinalNova.Ataque.Rafaga) aros = s.NuevosAros;
                }
                if (s.ImpactoPunetazo) tPunetazo = t;
            }
            CollectionAssert.AreEqual(new[] { ReglaJefeFinalNova.Ataque.Punetazo, ReglaJefeFinalNova.Ataque.Onda, ReglaJefeFinalNova.Ataque.Rafaga }, ataques);
            Assert.AreEqual(0.2f, tOnda - tPunetazo, 0.05f, "combo: la onda empieza 0.2 s después del puñetazo");
            Assert.IsNotNull(aros);
            CollectionAssert.AreEqual(new[]
            {
                ReglaJefeFinalNova.LugarAro.Jugador, ReglaJefeFinalNova.LugarAro.DelanteDelJugador, ReglaJefeFinalNova.LugarAro.DetrasDelJugador
            }, aros, "uno en el jugador y dos a +-3 m sobre su dirección de movimiento");
        }

        [Test]
        public void Rafaga_LosTresArosExplotanEscalonados()
        {
            ReglaJefeFinalNova r = Crear();
            r.Avanzar(Dt, En(20f, 0.3f));
            r.ForzarIndice(2);
            List<float> explotan = new List<float>();
            float t = 0f;
            while (t < 3f && explotan.Count < 3)
            {
                ReglaJefeFinalNova.Salida s = r.Avanzar(Dt, En(10f, 0.3f));
                t += Dt;
                for (int i = 0; i < s.Explotan; i++) explotan.Add(t);
            }
            Assert.AreEqual(3, explotan.Count);
            Assert.AreEqual(0.96f, explotan[0], 0.03f);
            Assert.Greater(explotan[1], explotan[0] + 0.2f);
            Assert.Greater(explotan[2], explotan[1] + 0.2f);
        }

        [Test]
        public void SinPelea_NoAtacaYVuelveACaminar()
        {
            ReglaJefeFinalNova r = Crear();
            Pasar(r, 0.3f, 2f);
            Assert.AreEqual(ReglaJefeFinalNova.Accion.AvisoPunetazo, r.Actual);
            ReglaJefeFinalNova.Salida s = r.Avanzar(Dt, En(2f, 1f, false));
            Assert.AreEqual(ReglaJefeFinalNova.Accion.Camina, r.Actual);
            Assert.IsFalse(s.ImpactoPunetazo);
        }

        [Test]
        public void Reiniciar_VuelveAAsedioSinEscudo()
        {
            ReglaJefeFinalNova r = EnEscudo();
            r.Reiniciar();
            Assert.AreEqual(ReglaJefeFinalNova.FaseJefe.Asedio, r.Fase);
            Assert.IsFalse(r.EscudoActivo);
            Assert.AreEqual(ReglaJefeFinalNova.Accion.Camina, r.Actual);
        }
    }
}
