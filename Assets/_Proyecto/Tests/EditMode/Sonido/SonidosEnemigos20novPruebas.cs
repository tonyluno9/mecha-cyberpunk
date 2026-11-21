using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Progresion.Sonido;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>
    /// T-1533: sonidos de Nova, robots, drones, NPCs y policía (20-nov). Lógica pura: qué ID pide cada tipo de enemigo y cada estado, el fade del jetpack,
    /// el género del NPC, la ráfaga del dron, y que todos los IDs que se piden tienen archivo y política de mezcla.
    /// </summary>
    public class SonidosEnemigos20novPruebas
    {
        private static readonly Regex Variante = new Regex(@"^(.+?)(_\d\d)?$");

        // ---- tipo de enemigo por nombre ----

        [TestCase("E15_CentinelaNova_a", TipoSonoroEnemigo.Centinela)]
        [TestCase("E15_CentinelaNova_b", TipoSonoroEnemigo.Centinela)]
        [TestCase("E16_RobotPesadoNova_a", TipoSonoroEnemigo.RobotPesado)]
        [TestCase("DatosPandilleroAPie_RobotPesadoE16", TipoSonoroEnemigo.RobotPesado)]
        [TestCase("E17_PerroRobotNova_esc", TipoSonoroEnemigo.Perro)]
        [TestCase("E18_EliteNova_a", TipoSonoroEnemigo.Elite)]
        [TestCase("E12_GuardiaPesadoNova", TipoSonoroEnemigo.GuardiaPesado)]
        [TestCase("E14_JefeFinalNova", TipoSonoroEnemigo.JefeFinal)]
        [TestCase("GuardiaNova", TipoSonoroEnemigo.GuardiaNova)]
        [TestCase("Nova_Guardia_1", TipoSonoroEnemigo.Ninguno)]
        [TestCase("E19_PandilleroSkulls_a_2", TipoSonoroEnemigo.Ninguno)]
        [TestCase("", TipoSonoroEnemigo.Ninguno)]
        public void TipoDeNombre_CadaModelo_SuTipo(string nombre, TipoSonoroEnemigo esperado)
        {
            Assert.AreEqual(esperado, ReglaSonidoEnemigo.TipoDeNombre(nombre));
        }

        [Test]
        public void Tipo_PerroRobotMarcado_GanaAunqueElNombreNoDiga()
        {
            Assert.AreEqual(TipoSonoroEnemigo.Perro, ReglaSonidoEnemigo.Tipo(new[] { "X" }, true));
        }

        [Test]
        public void Tipo_NombreGenericoDeGuardia_PierdeContraElDelModelo()
        {
            // el datos dice "GuardiaNova" pero el controlador dice E18: es la élite
            Assert.AreEqual(TipoSonoroEnemigo.Elite, ReglaSonidoEnemigo.Tipo(new[] { "DatosPandilleroAPie_GuardiaNova", "E18_EliteNova_a" }, false));
            Assert.AreEqual(TipoSonoroEnemigo.GuardiaNova, ReglaSonidoEnemigo.Tipo(new[] { "GuardiaNova_Armado" }, false));
            Assert.AreEqual(TipoSonoroEnemigo.Ninguno, ReglaSonidoEnemigo.Tipo(null, false));
        }

        // ---- pasos ----

        [TestCase(TipoSonoroEnemigo.GuardiaNova, false, "NOV_PASOS_CAMINAR")]
        [TestCase(TipoSonoroEnemigo.GuardiaNova, true, "NOV_PASOS_CORRER")]
        [TestCase(TipoSonoroEnemigo.Elite, false, "NOV_PASOS_CAMINAR")]
        [TestCase(TipoSonoroEnemigo.Elite, true, "NOV_PASOS_CORRER")]
        [TestCase(TipoSonoroEnemigo.Centinela, false, "PAS_METAL")]
        [TestCase(TipoSonoroEnemigo.Centinela, true, "PAS_METAL")]
        [TestCase(TipoSonoroEnemigo.Perro, true, "ROB_PERRO_MOVER")]
        [TestCase(TipoSonoroEnemigo.RobotPesado, false, "NOV_PASOS_CAMINAR")]
        [TestCase(TipoSonoroEnemigo.GuardiaPesado, false, "NOV_PASOS_CAMINAR")]
        [TestCase(TipoSonoroEnemigo.Ninguno, false, "")]
        public void IdPaso_CadaTipo_SuSonido(TipoSonoroEnemigo tipo, bool corre, string esperado)
        {
            Assert.AreEqual(esperado, ReglaSonidoEnemigo.IdPaso(tipo, corre));
        }

        [Test]
        public void MetrosPorPaso_TodosPositivos_YElPesadoDaPasosMasLargosQueElGuardia()
        {
            foreach (TipoSonoroEnemigo t in Enum.GetValues(typeof(TipoSonoroEnemigo)))
            {
                Assert.Greater(ReglaSonidoEnemigo.MetrosPorPaso(t, false), 0f, t.ToString());
                Assert.Greater(ReglaSonidoEnemigo.MetrosPorPaso(t, true), 0f, t.ToString());
            }
            Assert.Greater(ReglaSonidoEnemigo.MetrosPorPaso(TipoSonoroEnemigo.RobotPesado, false), ReglaSonidoEnemigo.MetrosPorPaso(TipoSonoroEnemigo.GuardiaNova, false));
            Assert.Greater(ReglaSonidoEnemigo.MetrosPorPaso(TipoSonoroEnemigo.GuardiaNova, true), ReglaSonidoEnemigo.MetrosPorPaso(TipoSonoroEnemigo.GuardiaNova, false));
        }

        [Test]
        public void Corre_PorLaVelocidad()
        {
            Assert.IsFalse(ReglaSonidoEnemigo.Corre(2.2f), "el pesado camina");
            Assert.IsTrue(ReglaSonidoEnemigo.Corre(4.5f), "el guardia corre");
            Assert.IsFalse(ReglaSonidoEnemigo.EnMovimiento(0.1f));
            Assert.IsTrue(ReglaSonidoEnemigo.EnMovimiento(1f));
        }

        [Test]
        public void AcumuladorPasos_DaUnPasoCadaTantosMetros()
        {
            AcumuladorPasos a = new AcumuladorPasos();
            int pasos = 0;
            for (int i = 0; i < 100; i++)
            {
                if (a.Avanzar(0.1f, 1f)) pasos++; // 10 m a un paso por metro
            }
            Assert.AreEqual(10, pasos);
            Assert.AreEqual(10, a.Pasos);
        }

        [Test]
        public void AcumuladorPasos_UnSaltoGrande_NoDaRafagaDePasos()
        {
            AcumuladorPasos a = new AcumuladorPasos();
            Assert.IsTrue(a.Avanzar(50f, 1f), "un paso");
            Assert.IsFalse(a.Avanzar(0.1f, 1f), "y no una ráfaga detrás");
        }

        [Test]
        public void AcumuladorPasos_Reiniciar_EmpiezaDeCero()
        {
            AcumuladorPasos a = new AcumuladorPasos();
            a.Avanzar(0.9f, 1f);
            a.Reiniciar();
            Assert.IsFalse(a.Avanzar(0.2f, 1f));
        }

        // ---- robots: muerte, apagado, alerta, daño ----

        [Test]
        public void Apagado_CadaRobot_ReciclaSuSonido()
        {
            Assert.AreEqual("ROB_PES_PLACA_CAE", ReglaSonidoEnemigo.IdApagado(TipoSonoroEnemigo.RobotPesado));
            Assert.AreEqual("ROB_PES_PLACA_CAE", ReglaSonidoEnemigo.IdApagado(TipoSonoroEnemigo.JefeFinal));
            Assert.AreEqual("ROB_SERVOS", ReglaSonidoEnemigo.IdApagado(TipoSonoroEnemigo.Centinela));
            Assert.AreEqual("ROB_SERVOS", ReglaSonidoEnemigo.IdApagado(TipoSonoroEnemigo.Perro));
            Assert.AreEqual(string.Empty, ReglaSonidoEnemigo.IdApagado(TipoSonoroEnemigo.GuardiaNova), "las personas no se apagan");
            Assert.Greater(ReglaSonidoEnemigo.SegundosHastaApagado(TipoSonoroEnemigo.RobotPesado), ReglaSonidoEnemigo.SegundosHastaApagado(TipoSonoroEnemigo.Centinela));
        }

        [Test]
        public void Alerta_SoloCentinelaPesadoYPerro_GritanConVozDeRobot()
        {
            Assert.AreEqual("ROB_VOZ_ALERTA", ReglaSonidoEnemigo.IdAlerta(TipoSonoroEnemigo.Centinela));
            Assert.AreEqual("ROB_VOZ_ALERTA", ReglaSonidoEnemigo.IdAlerta(TipoSonoroEnemigo.RobotPesado));
            Assert.AreEqual("ROB_VOZ_ALERTA", ReglaSonidoEnemigo.IdAlerta(TipoSonoroEnemigo.Perro));
            Assert.AreEqual(string.Empty, ReglaSonidoEnemigo.IdAlerta(TipoSonoroEnemigo.GuardiaNova));
            Assert.AreEqual(string.Empty, ReglaSonidoEnemigo.IdAlerta(TipoSonoroEnemigo.Elite));
        }

        [Test]
        public void Danio_LosRobotsSueltanChispas_LasPersonasNo()
        {
            Assert.AreEqual("ROB_CHISPAS_DANIO", ReglaSonidoEnemigo.IdDanio(TipoSonoroEnemigo.Centinela));
            Assert.AreEqual("ROB_CHISPAS_DANIO", ReglaSonidoEnemigo.IdDanio(TipoSonoroEnemigo.Perro));
            Assert.AreEqual(string.Empty, ReglaSonidoEnemigo.IdDanio(TipoSonoroEnemigo.GuardiaNova));
        }

        [Test]
        public void EsRobot_Clasificacion()
        {
            foreach (TipoSonoroEnemigo t in new[] { TipoSonoroEnemigo.Centinela, TipoSonoroEnemigo.RobotPesado, TipoSonoroEnemigo.Perro, TipoSonoroEnemigo.JefeFinal })
            {
                Assert.IsTrue(ReglaSonidoEnemigo.EsRobot(t), t.ToString());
            }
            foreach (TipoSonoroEnemigo t in new[] { TipoSonoroEnemigo.GuardiaNova, TipoSonoroEnemigo.Elite, TipoSonoroEnemigo.GuardiaPesado, TipoSonoroEnemigo.Ninguno })
            {
                Assert.IsFalse(ReglaSonidoEnemigo.EsRobot(t), t.ToString());
            }
            Assert.IsTrue(ReglaSonidoEnemigo.EsGigante(TipoSonoroEnemigo.RobotPesado));
            Assert.IsFalse(ReglaSonidoEnemigo.EsGigante(TipoSonoroEnemigo.Perro));
        }

        // ---- escudo de Nova ----

        [Test]
        public void Escudo_ActivarAlCubrirse_YAlLevantarloElPesado()
        {
            Assert.IsTrue(ReglaSonidoEnemigo.EscudoAlCubrirse(TipoSonoroEnemigo.GuardiaNova));
            Assert.IsTrue(ReglaSonidoEnemigo.EscudoAlCubrirse(TipoSonoroEnemigo.Elite));
            Assert.IsTrue(ReglaSonidoEnemigo.EscudoAlCubrirse(TipoSonoroEnemigo.Centinela));
            Assert.IsFalse(ReglaSonidoEnemigo.EscudoAlCubrirse(TipoSonoroEnemigo.Perro), "el perro no se cubre");
            Assert.IsTrue(ReglaSonidoEnemigo.EscudoAlPelear(TipoSonoroEnemigo.GuardiaPesado));
            Assert.IsFalse(ReglaSonidoEnemigo.EscudoAlPelear(TipoSonoroEnemigo.GuardiaNova));
            Assert.AreEqual("NOV_ESCUDO_ACTIVAR", ReglaSonidoEnemigo.IdEscudoActivar);
            Assert.AreEqual("NOV_ESCUDO_IMPACTO", ReglaSonidoEnemigo.IdEscudoImpacto);
        }

        // ---- jetpack ----

        [Test]
        public void Jetpack_QuienLoLleva()
        {
            Assert.IsTrue(ReglaSonidoEnemigo.TieneJetpack(TipoSonoroEnemigo.Elite));
            Assert.IsTrue(ReglaSonidoEnemigo.TieneJetpack(TipoSonoroEnemigo.Centinela));
            Assert.IsTrue(ReglaSonidoEnemigo.TieneJetpack(TipoSonoroEnemigo.RobotPesado));
            Assert.IsFalse(ReglaSonidoEnemigo.TieneJetpack(TipoSonoroEnemigo.Perro));
        }

        [Test]
        public void Jetpack_FadeDeEntrada_SubeDeCeroAUnoEnSuTiempo()
        {
            float nivel = 0f;
            float t = 0f;
            const float dt = 0.05f;
            while (nivel < 1f && t < 5f)
            {
                nivel = ReglaSonidoEnemigo.NivelJetpack(nivel, true, dt);
                t += dt;
            }
            Assert.AreEqual(1f, nivel);
            Assert.AreEqual(ReglaSonidoEnemigo.SegundosFadeJetpack, t, dt * 1.5f);
        }

        [Test]
        public void Jetpack_FadeDeSalida_BajaDeUnoACeroYNoPasa()
        {
            float nivel = 1f;
            for (int i = 0; i < 1000; i++)
            {
                float nuevo = ReglaSonidoEnemigo.NivelJetpack(nivel, false, 0.05f);
                Assert.LessOrEqual(nuevo, nivel, "nunca sube mientras se apaga");
                Assert.GreaterOrEqual(nuevo, 0f);
                nivel = nuevo;
            }
            Assert.AreEqual(0f, nivel);
        }

        [Test]
        public void Jetpack_FadeDeEntradaCortado_NoSaltaAUno()
        {
            float nivel = ReglaSonidoEnemigo.NivelJetpack(0f, true, 0.1f);
            Assert.Less(nivel, 1f);
            Assert.Greater(nivel, 0f);
            // si deja de volar a la mitad, baja desde donde iba
            float baja = ReglaSonidoEnemigo.NivelJetpack(nivel, false, 0.1f);
            Assert.AreEqual(0f, baja, 1e-5f);
        }

        [Test]
        public void Jetpack_Tono_VaDelMinimoAUno()
        {
            Assert.AreEqual(ReglaSonidoEnemigo.TonoMinimoJetpack, ReglaSonidoEnemigo.TonoJetpack(0f), 1e-6f);
            Assert.AreEqual(1f, ReglaSonidoEnemigo.TonoJetpack(1f), 1e-6f);
            Assert.AreEqual(1f, ReglaSonidoEnemigo.TonoJetpack(5f), 1e-6f);
            Assert.Less(ReglaSonidoEnemigo.TonoJetpack(0.3f), ReglaSonidoEnemigo.TonoJetpack(0.6f));
        }

        // ---- NPC: género ----

        [TestCase("Visual_woman", 1, "NPC_M_DOLOR")]
        [TestCase("Visual_woman2", 2, "NPC_M_DOLOR")]
        [TestCase("Visual_man", 0, "NPC_H_DOLOR")]
        [TestCase("Visual_NP12_Abuela_a", 3, "NPC_M_DOLOR")]
        [TestCase("Visual_NP07_Pescador_a", 0, "NPC_H_DOLOR")]
        [TestCase("Visual_NP10_Elotero_a", 0, "NPC_H_DOLOR")]
        [TestCase("Visual_NP11_Estibador_b", 0, "NPC_H_DOLOR")]
        [TestCase("Visual_NP13_Repartidor_a", 0, "NPC_H_DOLOR")]
        [TestCase("Visual_SkullHombre", 0, "NPC_H_DOLOR")]
        [TestCase("robot", 0, "ROB_CHISPAS_DANIO")]
        [TestCase("Visual_RobotHombre", 0, "ROB_CHISPAS_DANIO")]
        [TestCase("Visual_MeseraRobot", 0, "ROB_CHISPAS_DANIO")]
        public void IdDolor_PorElNombreDelVisual(string visual, int semilla, string esperado)
        {
            Assert.AreEqual(esperado, ReglaSonidoNpc.IdDolor(visual, semilla));
        }

        [TestCase("Visual_NP08_Turista_a")]
        [TestCase("Visual_NP09_Corredor_b")]
        [TestCase("Visual_NP14_Oficinista_a")]
        [TestCase("")]
        public void IdDolor_VisualSinGenero_SeRepartePorSemilla(string visual)
        {
            Assert.AreEqual("NPC_M_DOLOR", ReglaSonidoNpc.IdDolor(visual, 2));
            Assert.AreEqual("NPC_H_DOLOR", ReglaSonidoNpc.IdDolor(visual, 3));
            Assert.AreEqual("NPC_H_DOLOR", ReglaSonidoNpc.IdDolor(visual, -1), "semilla negativa");
            Assert.AreEqual(ReglaSonidoNpc.IdDolor(visual, 7), ReglaSonidoNpc.IdDolor(visual, 7), "estable");
        }

        // ---- dron: ráfaga ----

        [Test]
        public void Rafaga_UnDisparo_SuenaTresTirosEnTotal()
        {
            ReglaRafagaDron r = new ReglaRafagaDron();
            r.Iniciar(10f); // el primero suena al iniciar
            int extra = 0;
            for (float t = 10f; t < 12f; t += 0.01f)
            {
                if (r.Avanzar(t)) extra++;
            }
            Assert.AreEqual(ReglaRafagaDron.TirosPorRafaga - 1, extra);
            Assert.AreEqual(0, r.Restantes);
        }

        [Test]
        public void Rafaga_LosTirosSalenSeparadosYNoAntesDeTiempo()
        {
            ReglaRafagaDron r = new ReglaRafagaDron();
            r.Iniciar(0f);
            Assert.IsFalse(r.Avanzar(0.05f), "aún no toca");
            Assert.IsTrue(r.Avanzar(ReglaRafagaDron.SegundosEntreTiros + 0.001f));
            Assert.IsFalse(r.Avanzar(ReglaRafagaDron.SegundosEntreTiros + 0.002f), "el siguiente espera su turno");
        }

        [Test]
        public void Rafaga_Cancelar_CortaLosQueFaltan()
        {
            ReglaRafagaDron r = new ReglaRafagaDron();
            r.Iniciar(0f);
            r.Cancelar();
            Assert.IsFalse(r.Avanzar(5f));
        }

        [Test]
        public void Rafaga_SuIntervaloRespetaElMinimoDeLaMezcla()
        {
            Assert.GreaterOrEqual(ReglaRafagaDron.SegundosEntreTiros, ReglaMezclaSonidos.Para("DRN_PES_AMETRALLADORA").SegundosMinimos,
                "si no, la mezcla se comería los tiros de la ráfaga");
        }

        // ---- policía ----

        [Test]
        public void Muro_SoloLasPiezasDeTorreMU02()
        {
            Assert.IsTrue(SonidosPolicia.EsTorreDelMuro("MU02_Torre_a"));
            Assert.IsFalse(SonidosPolicia.EsTorreDelMuro("MU01_Muro_a"));
            Assert.IsFalse(SonidosPolicia.EsTorreDelMuro("Puerta_MU02"));
            Assert.IsFalse(SonidosPolicia.EsTorreDelMuro(null));
        }

        // ---- IDs: archivos y política ----

        private static SortedSet<string> IdsConArchivo()
        {
            SortedSet<string> ids = new SortedSet<string>();
            string carpeta = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Assets/Arte/Audio");
            foreach (string f in Directory.GetFiles(carpeta, "*.wav", SearchOption.AllDirectories))
            {
                if (f.Replace('\\', '/').Contains("/Provisional/")) continue;
                ids.Add(Variante.Match(Path.GetFileNameWithoutExtension(f)).Groups[1].Value);
            }
            return ids;
        }

        private static IEnumerable<string> IdsDeConstantes(Type tipo)
        {
            return tipo.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.StartsWith("Id", StringComparison.Ordinal))
                .Select(f => (string)f.GetRawConstantValue());
        }

        private static IEnumerable<string> TodosLosIds()
        {
            return IdsDeConstantes(typeof(ReglaSonidoEnemigo))
                .Concat(IdsDeConstantes(typeof(ReglaSonidoNpc)))
                .Concat(IdsDeConstantes(typeof(SonidosPolicia)))
                .Concat(IdsDeConstantes(typeof(NaveNova)))
                .Concat(new[] { "DRN_PES_AMETRALLADORA", "NAV_DANIADA", "POL_ESTRELLA_SUBE" })
                .Distinct();
        }

        [Test]
        public void TodosLosIdsQueSePiden_TienenArchivo()
        {
            SortedSet<string> conArchivo = IdsConArchivo();
            List<string> sin = TodosLosIds().Where(id => !conArchivo.Contains(id)).ToList();
            Assert.IsEmpty(sin, "IDs sin archivo en Assets/Arte/Audio: " + string.Join(", ", sin));
            Assert.Greater(TodosLosIds().Count(), 25, "la lista no debe quedar vacía por un error de reflexión");
        }

        [Test]
        public void TodosLosIdsNuevos_TienenPoliticaDeMezcla()
        {
            foreach (string id in TodosLosIds())
            {
                if (!MapaSonidos20nov.EsNuevo(id)) continue; // PAS_METAL y los demás de antes tienen su política vieja
                PoliticaSonido p = ReglaMezclaSonidos.Para(id);
                Assert.Greater(p.MaxVoces, 0, id + ": sin límite de voces");
                Assert.Greater(p.Volumen, 0f, id);
                Assert.IsNotNull(p.Grupo, id);
            }
        }

        [Test]
        public void ElJetpack_EsUnBucleDeUnaSolaVoz()
        {
            Assert.AreEqual(1, MapaSonidos20nov.Bucles("NOV_JETPACK_LOOP"));
            Assert.AreEqual(1, MapaSonidos20nov.Bucles("POL_SIRENA_MURO"));
            Assert.AreEqual(3, MapaSonidos20nov.Bucles("NAV_ROTORES_LOOP"));
        }
    }
}
