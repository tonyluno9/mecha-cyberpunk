using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Habilidades;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T98: cada habilidad de prótesis dura el triple que antes del 24-nov y su recarga no cambia (datos de Resources/ConfigHabilidadesProtesis).</summary>
    public class DuracionesProtesisT98Pruebas
    {
        private const float Factor = 3f;

        // Habilidad, duración nivel I y II antes de T98 (s), recarga (s, igual que antes).
        private static readonly (HabilidadProtesis h, float nivel1, float nivel2, float recarga)[] Antes =
        {
            (HabilidadProtesis.Piernas, 6f, 10f, 20f),
            (HabilidadProtesis.Brazos, 6f, 10f, 25f),
            (HabilidadProtesis.Cabeza, 8f, 14f, 30f),
            (HabilidadProtesis.Laser, 4f, 4f, 16f),
            (HabilidadProtesis.Misiles, 1f, 1f, 22f),
        };

        [Test]
        public void Config_CadaHabilidad_DuraElTriple_YLaRecargaNoCambia()
        {
            ConfigHabilidadesProtesis config = Resources.Load<ConfigHabilidadesProtesis>(ConfigHabilidadesProtesis.NombreRecurso);
            Assert.IsNotNull(config, "Resources/" + ConfigHabilidadesProtesis.NombreRecurso);
            foreach ((HabilidadProtesis h, float nivel1, float nivel2, float recarga) in Antes)
            {
                TiemposHabilidad t = config.Tiempos(h);
                Assert.AreEqual(nivel1 * Factor, t.duracionNivel1, 1e-4f, h + " nivel I");
                Assert.AreEqual(nivel2 * Factor, t.duracionNivel2, 1e-4f, h + " nivel II");
                Assert.AreEqual(recarga, t.recarga, 1e-4f, h + " recarga");
            }
        }

        [Test]
        public void ValoresPorOmision_DelCodigo_TambienSonElTriple()
        {
            ConfigHabilidadesProtesis nuevo = ScriptableObject.CreateInstance<ConfigHabilidadesProtesis>();
            try
            {
                foreach ((HabilidadProtesis h, float nivel1, float nivel2, float recarga) in Antes)
                {
                    TiemposHabilidad t = nuevo.Tiempos(h);
                    Assert.AreEqual(nivel1 * Factor, t.duracionNivel1, 1e-4f, h + " nivel I");
                    Assert.AreEqual(nivel2 * Factor, t.duracionNivel2, 1e-4f, h + " nivel II");
                    Assert.AreEqual(recarga, t.recarga, 1e-4f, h + " recarga");
                }
            }
            finally
            {
                Object.DestroyImmediate(nuevo);
            }
        }

        [Test]
        public void Regla_ConLosDatos_PiernasNivelI_SigueActivaHastaElTriple()
        {
            ConfigHabilidadesProtesis config = Resources.Load<ConfigHabilidadesProtesis>(ConfigHabilidadesProtesis.NombreRecurso);
            Assert.IsNotNull(config);
            TiemposHabilidad t = config.Tiempos(HabilidadProtesis.Piernas);
            ReglaHabilidadProtesis r = new ReglaHabilidadProtesis(t.duracionNivel1, t.duracionNivel2, t.recarga);
            Assert.IsTrue(r.Activar(1));
            r.Avanzar(6f * Factor - 0.1f);
            Assert.AreEqual(EstadoHabilidad.Activa, r.Estado, "a los 17.9 s sigue activa");
            r.Avanzar(0.2f);
            Assert.AreEqual(EstadoHabilidad.Recargando, r.Estado, "a los 18.1 s recarga");
        }
    }
}
