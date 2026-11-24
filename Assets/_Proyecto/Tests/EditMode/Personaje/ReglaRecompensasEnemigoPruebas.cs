using System.Collections.Generic;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-drops-24nov: qué suelta cada bando, a qué arma van las balas y cuándo se recogen.</summary>
    public class ReglaRecompensasEnemigoPruebas
    {
        private static System.Func<float> Secuencia(params float[] valores)
        {
            var cola = new Queue<float>(valores);
            return () => cola.Dequeue();
        }

        [Test]
        public void Tirar_AzarBajoLaProbabilidad_SueltaAmbos()
        {
            ReglaRecompensasEnemigo.Tirada t = ReglaRecompensasEnemigo.Tirar(0.9f, 0.15f, Secuencia(0.5f, 0.1f));
            Assert.IsTrue(t.Municion);
            Assert.IsTrue(t.Botiquin);
        }

        [Test]
        public void Tirar_AzarSobreLaProbabilidad_NoSueltaNada()
        {
            ReglaRecompensasEnemigo.Tirada t = ReglaRecompensasEnemigo.Tirar(0.35f, 0.15f, Secuencia(0.5f, 0.2f));
            Assert.IsFalse(t.Municion);
            Assert.IsFalse(t.Botiquin);
        }

        [Test]
        public void Config_PoliciaYNovaCasiSiempre_SkullsAVeces()
        {
            var config = ScriptableObject.CreateInstance<ConfigRecompensasEnemigo>();
            try
            {
                Assert.GreaterOrEqual(config.ProbabilidadMunicion(FaccionRecompensa.Policia), 0.8f);
                Assert.GreaterOrEqual(config.ProbabilidadMunicion(FaccionRecompensa.Nova), 0.8f);
                float skulls = config.ProbabilidadMunicion(FaccionRecompensa.Skulls);
                Assert.Greater(skulls, 0f);
                Assert.Less(skulls, 0.6f);
                Assert.Less(config.ProbabilidadBotiquin, skulls, "El botiquín sale de vez en cuando");
                Assert.AreEqual(30f, config.SegundosEnPiso, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Config_PoliciaConAzarAlto_SueltaBalas_SkullsNo()
        {
            var config = ScriptableObject.CreateInstance<ConfigRecompensasEnemigo>();
            try
            {
                const float azar = 0.7f;
                Assert.IsTrue(ReglaRecompensasEnemigo.Tirar(config.ProbabilidadMunicion(FaccionRecompensa.Policia), 0f, Secuencia(azar, 1f)).Municion);
                Assert.IsFalse(ReglaRecompensasEnemigo.Tirar(config.ProbabilidadMunicion(FaccionRecompensa.Skulls), 0f, Secuencia(azar, 1f)).Municion);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ElegirArma_TieneLaDelEnemigo_VaALaDelEnemigo()
        {
            ArmaAPie? arma = ReglaRecompensasEnemigo.ElegirArma(ArmaAPie.Subfusil, ArmaAPie.Escopeta, a => a == ArmaAPie.Subfusil || a == ArmaAPie.Escopeta);
            Assert.AreEqual(ArmaAPie.Subfusil, arma);
        }

        [Test]
        public void ElegirArma_NoTieneLaDelEnemigo_VaALaDeLaMano()
        {
            ArmaAPie? arma = ReglaRecompensasEnemigo.ElegirArma(ArmaAPie.Subfusil, ArmaAPie.Fusil, a => a == ArmaAPie.Fusil || a == ArmaAPie.Escopeta);
            Assert.AreEqual(ArmaAPie.Fusil, arma);
        }

        [Test]
        public void ElegirArma_EnemigoConPistolaYManoConPunos_VaALaPrimeraQueGastaBalas()
        {
            ArmaAPie? arma = ReglaRecompensasEnemigo.ElegirArma(ArmaAPie.Pistola, ArmaAPie.Punos, a => a == ArmaAPie.Pistola || a == ArmaAPie.Escopeta);
            Assert.AreEqual(ArmaAPie.Escopeta, arma);
        }

        [Test]
        public void ElegirArma_SoloPistolaYGranada_Null()
        {
            Assert.IsNull(ReglaRecompensasEnemigo.ElegirArma(ArmaAPie.Pistola, ArmaAPie.Granada, a => a == ArmaAPie.Pistola || a == ArmaAPie.Granada));
        }

        [Test]
        public void Cerca_CuerpoDentroDelRadioEnElPlano_True()
        {
            Assert.IsTrue(ReglaRecompensasEnemigo.Cerca(Vector3.zero, new Vector3(1f, 1.7f, 0f), 1.3f, null, 0, 0.35f));
            Assert.IsFalse(ReglaRecompensasEnemigo.Cerca(Vector3.zero, new Vector3(2f, 0f, 0f), 1.3f, null, 0, 0.35f));
        }

        [Test]
        public void Cerca_ManoVRJuntoAlObjeto_True_LejosFalse()
        {
            Vector3[] manos = { new Vector3(5f, 0.3f, 5.2f), new Vector3(9f, 1f, 9f) };
            Vector3 objeto = new Vector3(5f, 0.3f, 5f);
            Assert.IsTrue(ReglaRecompensasEnemigo.Cerca(objeto, null, 1.3f, manos, 2, 0.35f));
            Assert.IsFalse(ReglaRecompensasEnemigo.Cerca(objeto, null, 1.3f, manos, 0, 0.35f), "Sin manos registradas no cuenta");
            Assert.IsFalse(ReglaRecompensasEnemigo.Cerca(objeto, new Vector3(9f, 0f, 9f), 1.3f, new[] { new Vector3(5f, 1.5f, 5f) }, 1, 0.35f),
                "La mano cuenta en 3D: muy arriba no recoge");
        }
    }
}
