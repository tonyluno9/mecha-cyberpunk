using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>
    /// T-1535: ambiente por barrio con cruce de 2 a 4 s, clima, interiores y objetos con tope de fuentes, y los eventos nuevos (misión fallida, temporizador, implantes).
    /// Todo con reglas puras: no hace falta escena ni gestor.
    /// </summary>
    public class SonidosAmbiente20novPruebas
    {
        [TestCase(IdLugar.Calle, "fosa", null, true, false, "AMB_SUBURBIOS_NOCHE")]
        [TestCase(IdLugar.Calle, "fosa", null, false, false, "AMB_CALLE_DIA")]
        [TestCase(IdLugar.Calle, "refineria", null, true, false, "AMB_NOVA")]
        [TestCase(IdLugar.Calle, "refineria", null, false, false, "AMB_NOVA")]
        [TestCase(IdLugar.Calle, "tianguis", null, true, false, "AMB_TIANGUIS")]
        [TestCase(IdLugar.Calle, "neon", null, true, false, "AMB_NEON_ALTO")]
        [TestCase(IdLugar.Calle, "tianguis", "LasLomas", false, false, "AMB_LOMAS")]
        [TestCase(IdLugar.Calle, "", "LasLomas", true, false, "AMB_LOMAS")]
        [TestCase(IdLugar.Calle, "tianguis", "Deshuesadero", true, false, "AMB_TIANGUIS")]
        [TestCase(IdLugar.Calle, "", "Avenida", true, false, "AMB_CALLE_NOCHE")]
        [TestCase(IdLugar.Calle, "", "Avenida", false, false, "AMB_CALLE_DIA")]
        [TestCase(IdLugar.Calle, "neon", "LasLomas", true, true, "AMB_AZOTEA_CIUDAD")]
        [TestCase(IdLugar.Calle, "fosa", null, false, true, "AMB_AZOTEA_CIUDAD")]
        [TestCase(IdLugar.Casa, "fosa", "LasLomas", true, true, "AMB_INTERIOR_CASA")]
        [TestCase(IdLugar.Casino, "refineria", null, true, false, "AMB_CASINO")]
        public void Ambiente_PorLugarZonaBarrioHoraYAltura_EsElQueToca(IdLugar lugar, string zona, string barrio, bool noche, bool alto, string esperado)
        {
            Assert.AreEqual(esperado, ReglaAmbienteZona.Elegir(lugar, zona, barrio, noche, alto));
            Assert.AreEqual(esperado, GestorSonidos.AmbientePara(lugar, zona, barrio, noche, alto));
        }

        [Test]
        public void Ambiente_FirmaVieja_SigueIgual()
        {
            Assert.AreEqual("AMB_NEON_ALTO", ReglaAmbienteZona.Elegir(IdLugar.Calle, "neon", true));
            Assert.AreEqual("AMB_CALLE_DIA", ReglaAmbienteZona.Elegir(IdLugar.Ninguno, "", false));
            Assert.AreEqual("AMB_CALLE_NOCHE", ReglaAmbienteZona.Elegir(IdLugar.Calle, "", true));
        }

        [Test]
        public void Azotea_TieneHisteresis_EntraA20YSaleBajoLos17()
        {
            Assert.IsFalse(ReglaAmbienteZona.EnAzotea(19.9f, false));
            Assert.IsTrue(ReglaAmbienteZona.EnAzotea(26f, false));
            Assert.IsTrue(ReglaAmbienteZona.EnAzotea(18f, true));
            Assert.IsFalse(ReglaAmbienteZona.EnAzotea(16.9f, true));
            Assert.IsFalse(ReglaAmbienteZona.EnAzotea(18f, false));
        }

        [Test]
        public void Cruce_EntreDosAmbientesDeCalle_EsDeTresSegundos()
        {
            Assert.AreEqual(3f, ReglaAmbienteZona.SegundosCruce("AMB_CALLE_NOCHE", "AMB_LOMAS", 1f, 3f), 0.0001f);
            Assert.AreEqual(3f, ReglaAmbienteZona.SegundosCruce("AMB_NOVA", "AMB_AZOTEA_CIUDAD", 1f, 3f), 0.0001f);
            Assert.AreEqual(3f, ReglaAmbienteZona.SegundosCruce("AMB_SUBURBIOS_NOCHE", "AMB_CALLE_NOCHE", 1f, 3f), 0.0001f);
        }

        [Test]
        public void Cruce_ConUnLugar_SigueSiendoElCorto()
        {
            Assert.AreEqual(1f, ReglaAmbienteZona.SegundosCruce("AMB_CALLE_NOCHE", "AMB_INTERIOR_CASA", 1f, 3f), 0.0001f);
            Assert.AreEqual(1f, ReglaAmbienteZona.SegundosCruce("AMB_CASINO", "AMB_NOVA", 1f, 3f), 0.0001f);
            Assert.AreEqual(1f, ReglaAmbienteZona.SegundosCruce(null, "AMB_LOMAS", 1f, 3f), 0.0001f);
        }

        [TestCase(0.5f, 2f)]
        [TestCase(2f, 2f)]
        [TestCase(3.5f, 3.5f)]
        [TestCase(9f, 4f)]
        public void Cruce_DeBarrio_SeAcotaEntre2Y4Segundos(float pedido, float esperado)
        {
            Assert.AreEqual(esperado, ReglaAmbienteZona.SegundosCruce("AMB_LOMAS", "AMB_NOVA", 1f, pedido), 0.0001f);
        }

        [Test]
        public void Cruce_ElNivelLlegaAUnoEnElTiempoPedido_YElOtroBajaACero()
        {
            float duracion = ReglaAmbienteZona.SegundosCruce("AMB_CALLE_NOCHE", "AMB_LOMAS", 1f, ReglaAmbienteZona.SegundosCruceBarrio);
            float sube = 0f;
            float baja = 1f;
            float t = 0f;
            const float dt = 0.1f;
            float tSube = -1f;
            float tBaja = -1f;
            while (t < 6f)
            {
                t += dt;
                sube = Mathf.MoveTowards(sube, 1f, dt / duracion);
                baja = Mathf.MoveTowards(baja, 0f, dt / duracion);
                if (tSube < 0f && sube >= 1f) tSube = t;
                if (tBaja < 0f && baja <= 0f) tBaja = t;
                Assert.LessOrEqual(sube + baja, 1.0001f);
            }
            Assert.AreEqual(3f, tSube, 0.15f);
            Assert.AreEqual(3f, tBaja, 0.15f);
        }

        private static ReglaFuentesMundo.Candidato C(int grupo, int id, float distancia, float radio = 12f) =>
            new ReglaFuentesMundo.Candidato { Grupo = grupo, Id = id, Distancia = distancia, Radio = radio };

        [Test]
        public void Fuentes_SoloSuenanLasMasCercanas_HastaElTopeDelGrupo()
        {
            List<ReglaFuentesMundo.Candidato> c = new List<ReglaFuentesMundo.Candidato>();
            for (int i = 0; i < 50; i++) c.Add(C((int)GrupoEmisorMundo.Objeto, i % 4, 1f + i * 0.2f));
            bool[] activos = new bool[c.Count];
            int n = ReglaFuentesMundo.Elegir(c, TablaEmisoresMundo.TopePorGrupo, TablaEmisoresMundo.TopePorId, activos);
            Assert.AreEqual(TablaEmisoresMundo.TopePorGrupo[(int)GrupoEmisorMundo.Objeto], n);
            Assert.IsTrue(activos[0] && activos[1]);
            for (int i = 2; i < activos.Length; i++) Assert.IsFalse(activos[i], "el " + i + " queda lejos");
        }

        [Test]
        public void Fuentes_FueraDelRadio_NoSuenan()
        {
            List<ReglaFuentesMundo.Candidato> c = new List<ReglaFuentesMundo.Candidato> { C(0, 0, 14.9f, 15f), C(0, 1, 15.1f, 15f) };
            bool[] activos = new bool[2];
            ReglaFuentesMundo.Elegir(c, TablaEmisoresMundo.TopePorGrupo, TablaEmisoresMundo.TopePorId, activos);
            Assert.IsTrue(activos[0]);
            Assert.IsFalse(activos[1]);
        }

        [Test]
        public void Fuentes_ElMismoId_NoPasaDelTopePorId()
        {
            List<ReglaFuentesMundo.Candidato> c = new List<ReglaFuentesMundo.Candidato>
            {
                C((int)GrupoEmisorMundo.Objeto, 7, 1f), C((int)GrupoEmisorMundo.Objeto, 7, 2f), C((int)GrupoEmisorMundo.Objeto, 7, 3f), C((int)GrupoEmisorMundo.Objeto, 8, 4f),
            };
            bool[] activos = new bool[4];
            ReglaFuentesMundo.Elegir(c, new[] { 0, 5, 0, 0, 0 }, 2, activos);
            CollectionAssert.AreEqual(new[] { true, true, false, true }, activos);
        }

        [Test]
        public void Fuentes_TinoRadioYVentilador_SuenanJuntos_ConElTopeDeInteriores()
        {
            List<ReglaFuentesMundo.Candidato> c = new List<ReglaFuentesMundo.Candidato>
            {
                C((int)GrupoEmisorMundo.Interior, 0, 5f, 15f), C((int)GrupoEmisorMundo.Interior, 1, 5f, 15f), C((int)GrupoEmisorMundo.Interior, 2, 9f, 15f),
            };
            bool[] activos = new bool[3];
            ReglaFuentesMundo.Elegir(c, TablaEmisoresMundo.TopePorGrupo, TablaEmisoresMundo.TopePorId, activos);
            CollectionAssert.AreEqual(new[] { true, true, false }, activos);
        }

        [Test]
        public void Fuentes_LosTopes_DejanSitioParaLasDoceVocesDelGestor()
        {
            int bucles = 0;
            for (int g = 0; g < TablaEmisoresMundo.TopePorGrupo.Length; g++)
            {
                if (g != (int)GrupoEmisorMundo.Puntual) bucles += TablaEmisoresMundo.TopePorGrupo[g];
            }
            Assert.LessOrEqual(bucles + 2, 8, "bucles de mundo + clima dejan al menos 4 de las 12 voces libres");
        }

        [Test]
        public void SegundosHastaOtro_VaDelMinimoAlMaximo()
        {
            Assert.AreEqual(18f, ReglaFuentesMundo.SegundosHastaOtro(18f, 40f, 0f), 0.0001f);
            Assert.AreEqual(29f, ReglaFuentesMundo.SegundosHastaOtro(18f, 40f, 0.5f), 0.0001f);
            Assert.AreEqual(18f, ReglaFuentesMundo.SegundosHastaOtro(18f, 10f, 0.9f), 0.0001f);
        }

        [Test]
        public void TablaEmisores_TodosLosIdsSonDelCatalogoNuevo_ConSuPolitica()
        {
            foreach (TablaEmisoresMundo.Definicion d in TablaEmisoresMundo.Definiciones)
            {
                Assert.Greater(d.Ids.Length, 0, d.Nombre);
                foreach (string id in d.Ids)
                {
                    Assert.IsTrue(MapaSonidos20nov.TryFila(id, out TablaMezcla20nov.Fila fila), id + " tiene fila en la tabla de mezcla");
                    if (d.EsPuntual) Assert.AreEqual(0, fila.Bucles, id + " no es bucle");
                    else Assert.Greater(fila.Bucles, 0, id + " es bucle");
                }
            }
        }

        [TestCase("Lugar_TallerMecanico", "INT_TALLER_CANEPA")]
        [TestCase("Lugar_CIC", "INT_TIENDA_TINO_RADIO")]
        [TestCase("Lugar_CIC", "INT_TIENDA_TINO_VENTILADOR")]
        [TestCase("T931_Armero", "INT_ARMERIA_VENTILADOR")]
        [TestCase("Bodega_Norte", "INT_BODEGA_GOTERAS")]
        [TestCase("C08_Transformador", "OBJ_TRANSFORMADOR")]
        [TestCase("C10_AireAcondicionado (3)", "OBJ_AIRE_ACONDICIONADO")]
        [TestCase("CY08_CamaraCorporativa", "OBJ_CAMARA_SEGURIDAD")]
        [TestCase("CY06_Expendedora", "OBJ_MAQUINA_EXPENDEDORA")]
        [TestCase("Expendedora_2", "OBJ_MAQUINA_EXPENDEDORA")]
        [TestCase("Tiendita", "OBJ_REFRIGERADOR")]
        [TestCase("Clinica_Camilla", "OBJ_REFRIGERADOR")]
        [TestCase("Set_Hacker", "OBJ_TELE_REINICIO")]
        [TestCase("Lamina", "CLI_LLUVIA_SOBRE_LAMINA")]
        [TestCase("Bocina_Fiesta_1", "RAD_EST1_CUMBIA_PERCUSION")]
        public void TablaEmisores_ElNombreDeLaEscena_BuscaSuId(string nombre, string id)
        {
            int d = TablaEmisoresMundo.Buscar(nombre);
            Assert.GreaterOrEqual(d, 0, nombre);
            CollectionAssert.Contains(TablaEmisoresMundo.Definiciones[d].Ids, id);
        }

        [Test]
        public void TablaEmisores_LoQueNoEsAncla_NoBusca()
        {
            Assert.AreEqual(-1, TablaEmisoresMundo.Buscar("Poste"));
            Assert.AreEqual(-1, TablaEmisoresMundo.Buscar("Toldo_Lamina"));
            Assert.AreEqual(-1, TablaEmisoresMundo.Buscar("Lugar_Casa"));
            Assert.AreEqual(-1, TablaEmisoresMundo.Buscar(null));
        }

        [Test]
        public void Interiores_ElRadioEs15m_YElTransformadorVaBajito()
        {
            foreach (string id in new[] { "INT_TALLER_CANEPA", "INT_TIENDA_TINO_RADIO", "INT_TIENDA_TINO_VENTILADOR", "INT_ARMERIA_VENTILADOR", "INT_BODEGA_GOTERAS" })
            {
                MapaSonidos20nov.TryFila(id, out TablaMezcla20nov.Fila f);
                Assert.AreEqual(15f, f.Distancia, 0.0001f, id);
            }
            MapaSonidos20nov.TryFila("OBJ_TRANSFORMADOR", out TablaMezcla20nov.Fila t);
            Assert.LessOrEqual(t.Distancia, 12f);
            Assert.LessOrEqual(t.Volumen, 0.05f);
        }

        [Test]
        public void Clima_SinLluviaNiViento_NoSuenaNada()
        {
            Assert.AreEqual(0, ReglaClimaSonido.Bucles(0f, false, 0f, false, false).Count);
        }

        [Test]
        public void Clima_LluviaEnLaCalle_EsLaFuerte_ConTormentaSonConTruenos()
        {
            CollectionAssert.AreEqual(new[] { "CLI_LLUVIA_FUERTE" }, ReglaClimaSonido.Bucles(0.6f, false, 0f, false, false));
            CollectionAssert.AreEqual(new[] { "CLI_TORMENTA_TRUENOS" }, ReglaClimaSonido.Bucles(1f, true, 0f, false, false));
        }

        [Test]
        public void Clima_ConViento_SumaElViento()
        {
            CollectionAssert.AreEqual(new[] { "CLI_LLUVIA_FUERTE", "CLI_VIENTO" }, ReglaClimaSonido.Bucles(0.6f, false, 0.8f, false, false));
            CollectionAssert.AreEqual(new[] { "CLI_VIENTO" }, ReglaClimaSonido.Bucles(0f, false, 0.5f, false, false));
            Assert.AreEqual(0, ReglaClimaSonido.Bucles(0f, false, 0.1f, false, false).Count);
        }

        [Test]
        public void Clima_EnCarro_LaLluviaDeAfueraSigueYBaja_LaDeDentroEsDeVehiculos()
        {
            CollectionAssert.AreEqual(new[] { "CLI_LLUVIA_FUERTE" }, ReglaClimaSonido.Bucles(0.6f, false, 0f, true, false));
            Assert.Less(ReglaClimaSonido.Factor(true), 1f);
            Assert.AreEqual(1f, ReglaClimaSonido.Factor(false));
            CollectionAssert.DoesNotContain(ReglaClimaSonido.Bucles(1f, true, 1f, true, false), "CLI_LLUVIA_DENTRO_CARRO");
        }

        [Test]
        public void Clima_DentroDeUnLugarCerrado_NoSuenaLluviaDeCalle()
        {
            Assert.AreEqual(0, ReglaClimaSonido.Bucles(1f, true, 1f, false, true).Count);
        }

        [Test]
        public void Lamina_SoloSuenaConLluviaAIntemperie()
        {
            Assert.IsTrue(ReglaClimaSonido.SuenaLamina(0.5f, false, false));
            Assert.IsFalse(ReglaClimaSonido.SuenaLamina(0f, false, false));
            Assert.IsFalse(ReglaClimaSonido.SuenaLamina(0.5f, true, false));
            Assert.IsFalse(ReglaClimaSonido.SuenaLamina(0.5f, false, true));
        }

        [Test]
        public void ClimaSonoro_Fijar_AcotaYLimpia()
        {
            try
            {
                ClimaSonoro.Fijar(3f, true, -1f);
                Assert.AreEqual(1f, ClimaSonoro.Lluvia);
                Assert.IsTrue(ClimaSonoro.Tormenta);
                Assert.AreEqual(0f, ClimaSonoro.Viento);
                ClimaSonoro.Fijar(0f, true, 0.5f);
                Assert.IsFalse(ClimaSonoro.Tormenta);
                Assert.IsFalse(ClimaSonoro.Llueve);
            }
            finally
            {
                ClimaSonoro.Limpiar();
            }
        }

        [Test]
        public void Temporizador_SoloEnLosUltimosDiezSegundos_CadaDos()
        {
            Assert.IsFalse(ReglaSonidoEventos.TocaTemporizador(0f, 100f, 0f));
            Assert.IsFalse(ReglaSonidoEventos.TocaTemporizador(30f, 100f, 0f));
            Assert.IsTrue(ReglaSonidoEventos.TocaTemporizador(10f, 100f, 0f));
            Assert.IsFalse(ReglaSonidoEventos.TocaTemporizador(9f, 101f, 100f));
            Assert.IsTrue(ReglaSonidoEventos.TocaTemporizador(8f, 102f, 100f));
        }

        [Test]
        public void InstalarImplante_EmpiezaConElTaladro_YTerminaConElClic()
        {
            IReadOnlyList<ReglaSonidoEventos.Paso> pasos = ReglaSonidoEventos.InstalarImplante;
            Assert.AreEqual("IMP_INSTALAR_TALADRO", pasos[0].Id);
            Assert.AreEqual(0f, pasos[0].Segundos);
            Assert.AreEqual("TIE_EQUIPAR", pasos[pasos.Count - 1].Id);
            int sinapsis = 0;
            float anterior = -1f;
            foreach (ReglaSonidoEventos.Paso p in pasos)
            {
                Assert.GreaterOrEqual(p.Segundos, anterior);
                anterior = p.Segundos;
                if (p.Id == "IMP_SINAPSIS") sinapsis++;
            }
            Assert.AreEqual(2, sinapsis);
        }

        [Test]
        public void IdsDeEventos_ExistenEnLaTablaNueva_YLaMisionFallidaEsUiError()
        {
            foreach (string id in new[] { ReglaSonidoEventos.Temporizador, ReglaSonidoEventos.Taladro, ReglaSonidoEventos.Sinapsis, ReglaSonidoEventos.EscudoGolpe,
                ReglaSonidoEventos.VisorActivar, ReglaSonidoEventos.Muerte, ReglaSonidoEventos.Esposas })
            {
                Assert.IsTrue(MapaSonidos20nov.EsNuevo(id), id);
            }
            Assert.AreEqual("UI_ERROR", MapaSonidos20nov.Nuevo(ReglaSonidoEventos.MisionFallida));
        }
    }
}
