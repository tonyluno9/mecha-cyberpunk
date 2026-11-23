using System;
using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1631 (T17): "NOVA INTERVIENE": con el máximo de estrellas no se genera policía, la que hay se retira y Nova llega por la jerarquía con su tope de vivos.</summary>
    public class ReglaNovaIntervieneT1631Pruebas
    {
        private static ReglaEscaladaRespuesta Crear(Action<ParametrosRespuesta> ajustar = null)
        {
            ParametrosRespuesta numeros = ParametrosRespuesta.PorOmision();
            numeros.PoliciasMuertosParaNova = 0; // T-1660: estas pruebas no son de la puerta de Nova (esa va en ReglaPuertaNovaT1660Pruebas)
            numeros.CamionetasParaNova = 0;
            if (ajustar != null)
            {
                ajustar(numeros);
            }
            return new ReglaEscaladaRespuesta(numeros, new Random(1));
        }

        private static void Avanzar(ReglaEscaladaRespuesta r, float t, int estrellas, int[] vivos = null)
        {
            r.Avanzar(t, estrellas, false, false, vivos ?? new int[9]);
        }

        private static int[] Vivos(TipoRespuesta a, int cuantosA, TipoRespuesta b = TipoRespuesta.PoliciaAPie, int cuantosB = 0)
        {
            int[] v = new int[9];
            v[(int)a] = cuantosA;
            v[(int)b] += cuantosB;
            return v;
        }

        [Test]
        public void ConCincoEstrellas_ModoNova_YNoSePideNingunaPolicia()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 5);
            Assert.IsTrue(r.ModoNova);
            foreach (TipoRespuesta t in new[] { TipoRespuesta.PoliciaAPie, TipoRespuesta.Patrulla, TipoRespuesta.Camioneta, TipoRespuesta.Dron })
            {
                Assert.IsFalse(r.PuedeGenerar(t, 1000f), t + " no se genera en modo Nova");
                Assert.IsFalse(r.PermiteMantener(t), t + " debe retirarse");
            }
        }

        [Test]
        public void ConCuatroEstrellas_SigueLaPolicia_SinModoNova()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 4);
            Assert.IsFalse(r.ModoNova);
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.Patrulla, 1000f));
            Assert.IsTrue(r.PermiteMantener(TipoRespuesta.Patrulla));
        }

        [Test]
        public void ModoNova_SaltaALosSoldadosHumanoides_ConElRetrasoDeNivelNuevo()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 5, Vivos(TipoRespuesta.Patrulla, 1));
            Assert.AreEqual(ReglaEscaladaRespuesta.PrimerNivelNova, r.NivelDesbloqueado);
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.NovaHumanoide, 5f), "dentro del retraso del nivel nuevo");
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.NovaHumanoide, 11f), "pasado el retraso llega el primero");
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.RobotMediano, 1000f), "el siguiente nivel aún no está desbloqueado");
        }

        [Test]
        public void ModoNova_SigueElOrdenDeLaJerarquia_ConVariasBajasPorNivel()
        {
            ReglaEscaladaRespuesta r = Crear(p => p.SegundosParaSubir = 100000f);
            Avanzar(r, 0f, 5);
            r.RegistrarGenerado(TipoRespuesta.NovaHumanoide, 11f);
            Avanzar(r, 12f, 5, Vivos(TipoRespuesta.NovaHumanoide, 1));
            r.RegistrarPerdida(TipoRespuesta.NovaHumanoide, 20f);
            Avanzar(r, 20f, 5);
            Assert.AreEqual(4, r.NivelDesbloqueado, "con una baja aún no sube: faltan 1 o 2 más");
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.NovaHumanoide, 25f), "el reemplazo espera su cooldown");
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.NovaHumanoide, 51f), "pasado su cooldown llega otro humanoide");
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.RobotMediano, 1000f), "el E18 no llega antes");
            r.RegistrarGenerado(TipoRespuesta.NovaHumanoide, 51f);
            Avanzar(r, 52f, 5, Vivos(TipoRespuesta.NovaHumanoide, 1));
            r.RegistrarPerdida(TipoRespuesta.NovaHumanoide, 60f);
            Avanzar(r, 60f, 5);
            Assert.AreEqual(5, r.NivelDesbloqueado, "vencidos dos humanoides: llega el E18");
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.Perro, 1000f), "los perros vienen después");
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.RobotMediano, 100f));
        }

        [Test]
        public void ModoNova_NuncaPasaDelTopeDeNovaVivos()
        {
            ReglaEscaladaRespuesta r = Crear(p => p.SegundosParaSubir = 1f);
            for (int i = 0; i <= 10; i++)
            {
                Avanzar(r, i * 2f, 5);
            }
            Assert.GreaterOrEqual(r.NivelDesbloqueado, 7);
            Avanzar(r, 100f, 5, Vivos(TipoRespuesta.NovaHumanoide, 1, TipoRespuesta.RobotMediano, 1));
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.Perro, 1000f), "con 2 vivos aún cabe uno");
            Avanzar(r, 101f, 5, Vivos(TipoRespuesta.NovaHumanoide, 2, TipoRespuesta.RobotMediano, 1));
            Assert.AreEqual(3, r.NovaVivos);
            foreach (TipoRespuesta t in new[] { TipoRespuesta.NovaHumanoide, TipoRespuesta.RobotMediano, TipoRespuesta.Perro, TipoRespuesta.RobotGrande })
            {
                Assert.IsFalse(r.PuedeGenerar(t, 1000f), t + " no cabe con 3 de Nova vivos");
            }
        }

        [Test]
        public void ModoNova_ElRobotGrandeSoloUnoALaVez()
        {
            ReglaEscaladaRespuesta r = Crear(p => p.SegundosParaSubir = 1f);
            for (int i = 0; i <= 10; i++)
            {
                Avanzar(r, i * 2f, 5);
            }
            Avanzar(r, 100f, 5, Vivos(TipoRespuesta.RobotGrande, 1));
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.RobotGrande, 1000f));
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.NovaHumanoide, 1000f), "otros de Nova sí caben");
        }

        [Test]
        public void ModoNova_LosDronesSoloEntranConSuNivelDeNova()
        {
            ReglaEscaladaRespuesta r = Crear(p => p.SegundosParaSubir = 100000f);
            Avanzar(r, 0f, 5);
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.Dron, 1000f));
            Assert.IsFalse(r.PermiteMantener(TipoRespuesta.Dron), "un dron de policía que había se retira");
            ReglaEscaladaRespuesta s = Crear(p => p.SegundosParaSubir = 1f);
            for (int i = 0; i <= 4; i++)
            {
                Avanzar(s, i * 2f, 5);
            }
            Assert.GreaterOrEqual(s.NivelDesbloqueado, 6);
            Assert.IsTrue(s.PermiteMantener(TipoRespuesta.Dron));
            Assert.IsTrue(s.PuedeGenerar(TipoRespuesta.Dron, 1000f));
            int[] vivos = Vivos(TipoRespuesta.Dron, 1, TipoRespuesta.NovaHumanoide, 1);
            vivos[(int)TipoRespuesta.RobotMediano] = 1;
            Avanzar(s, 50f, 5, vivos);
            Assert.AreEqual(3, s.NovaVivos, "el dron de Nova cuenta para el tope");
        }

        [Test]
        public void BajarDeCincoEstrellas_VuelveLaPolicia()
        {
            ReglaEscaladaRespuesta r = Crear();
            Avanzar(r, 0f, 5);
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.Patrulla, 1000f));
            Avanzar(r, 1f, 4);
            Assert.IsFalse(r.ModoNova);
            Assert.IsTrue(r.PuedeGenerar(TipoRespuesta.Patrulla, 1000f));
        }

        [Test]
        public void MisionProtegida_NoActivaElModoNova()
        {
            ReglaEscaladaRespuesta r = Crear();
            r.Avanzar(0f, 5, true, true, new int[9]);
            Assert.IsFalse(r.ModoNova);
        }

        [Test]
        public void SinNovaParaGenerar_NoSeActivaElModoNova()
        {
            ReglaEscaladaRespuesta r = Crear();
            foreach (TipoRespuesta t in new[] { TipoRespuesta.NovaHumanoide, TipoRespuesta.RobotMediano, TipoRespuesta.Perro, TipoRespuesta.RobotGrande, TipoRespuesta.Jefe })
            {
                r.Declarar(t, false);
            }
            Avanzar(r, 0f, 5);
            Assert.IsFalse(r.ModoNova, "sin Nova la policía no se retira: nadie la reemplazaría");
            Assert.IsTrue(r.PermiteMantener(TipoRespuesta.Patrulla));
        }

        [Test]
        public void Director_PublicaNovaIntervieneUnaVezPorEntrada()
        {
            ParametrosRespuesta numeros = ParametrosRespuesta.PorOmision();
            numeros.PoliciasMuertosParaNova = 0; // T-1660: estas pruebas no son de la puerta de Nova (esa va en ReglaPuertaNovaT1660Pruebas)
            numeros.CamionetasParaNova = 0;
            numeros.Registrar = false;
            DirectorRespuesta.Configurar(numeros);
            int avisos = 0;
            Action alAviso = () => avisos++;
            EventosRespuesta.NovaInterviene += alAviso;
            try
            {
                DirectorRespuesta.Avanzar(0f, 4);
                Assert.AreEqual(0, avisos);
                DirectorRespuesta.Avanzar(1f, 5);
                DirectorRespuesta.Avanzar(2f, 5);
                Assert.AreEqual(1, avisos);
                Assert.IsTrue(DirectorRespuesta.ModoNova);
                DirectorRespuesta.Avanzar(3f, 4);
                Assert.IsFalse(DirectorRespuesta.ModoNova);
                DirectorRespuesta.Avanzar(4f, 5);
                Assert.AreEqual(2, avisos, "una nueva entrada vuelve a avisar");
            }
            finally
            {
                EventosRespuesta.NovaInterviene -= alAviso;
                DirectorRespuesta.Configurar(null);
            }
        }

        [Test]
        public void Parametros_NuevosTienenValoresDeOmision()
        {
            ParametrosRespuesta p = ParametrosRespuesta.PorOmision();
            p.PoliciasMuertosParaNova = 0; // T-1660: estas pruebas no son de la puerta de Nova (esa va en ReglaPuertaNovaT1660Pruebas)
            p.CamionetasParaNova = 0;
            Assert.AreEqual(5, p.EstrellasModoNova);
            Assert.AreEqual(3, p.TopeNovaVivos);
            Assert.AreEqual(2, p.PerdidasNovaParaSubir);
            Assert.AreEqual(6, p.NivelDronNova);
            Assert.Greater(p.MetrosMinimosRetiroPolicia, 0f);
        }
    }
}
