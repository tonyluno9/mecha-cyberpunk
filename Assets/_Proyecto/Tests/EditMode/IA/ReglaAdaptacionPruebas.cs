using System.Collections.Generic;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.IA.Adaptacion;
using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-199: nivel y memoria de la adaptación (D-tony-194), con Base y Avanzado creados desde los valores de serie.</summary>
    public class ReglaAdaptacionPruebas
    {
        private DatosAdaptacionRival baseDatos;
        private DatosAdaptacionRival avanzado;

        [SetUp]
        public void SetUp()
        {
            baseDatos = ScriptableObject.CreateInstance<DatosAdaptacionRival>();
            avanzado = ScriptableObject.CreateInstance<DatosAdaptacionRival>();
            SerializedObject so = new SerializedObject(avanzado);
            so.FindProperty("nivelMaximo").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(baseDatos);
            Object.DestroyImmediate(avanzado);
        }

        private static bool[] Vistos(bool guardia, bool fintas, bool retirada)
        {
            return new[] { guardia, fintas, retirada };
        }

        private static int Nivel(MemoriaRival m, TipoAdaptacion t, DatosAdaptacionRival d) => ReglaAdaptacion.Nivel(m, t, d);

        [Test]
        public void Nivel_MemoriaNull_EsCero()
        {
            Assert.AreEqual(0, ReglaAdaptacion.Nivel(null, TipoAdaptacion.Guardia, avanzado));
        }

        [Test]
        public void Nivel_MemoriaNueva_EsCeroEnLosTres()
        {
            MemoriaRival m = new MemoriaRival();
            foreach (TipoAdaptacion t in System.Enum.GetValues(typeof(TipoAdaptacion)))
            {
                Assert.AreEqual(0, Nivel(m, t, avanzado));
            }
        }

        [Test]
        public void Nivel_EvidenciaAlta_NoPasaDeNivelMaximo()
        {
            MemoriaRival m = new MemoriaRival { evidencias = new[] { 9, 0, 0 } };
            Assert.AreEqual(1, Nivel(m, TipoAdaptacion.Guardia, baseDatos));
            Assert.AreEqual(2, Nivel(m, TipoAdaptacion.Guardia, avanzado));
        }

        [Test]
        public void Nivel_EnLosUmbrales_SubeJustoAhi()
        {
            MemoriaRival m = new MemoriaRival { evidencias = new[] { 0, 1, 2 } };
            Assert.AreEqual(0, Nivel(m, TipoAdaptacion.Guardia, avanzado));
            Assert.AreEqual(1, Nivel(m, TipoAdaptacion.Fintas, avanzado));
            Assert.AreEqual(1, Nivel(m, TipoAdaptacion.Retirada, avanzado));
            m.evidencias[2] = 3;
            Assert.AreEqual(2, Nivel(m, TipoAdaptacion.Retirada, avanzado));
        }

        [Test]
        public void Aprender_PrimeraPelea_AntesDeAprenderSiempreEsCero()
        {
            MemoriaRival m = new MemoriaRival();
            Assert.AreEqual(0, Nivel(m, TipoAdaptacion.Guardia, avanzado));
            ReglaAdaptacion.Aprender(m, Vistos(true, false, false), true, avanzado);
            Assert.AreEqual(1, m.peleas);
        }

        [Test]
        public void Aprender_UnaPeleaConPunos_DaGuardiaIEnBaseYEnAvanzado()
        {
            foreach (DatosAdaptacionRival d in new[] { baseDatos, avanzado })
            {
                MemoriaRival m = new MemoriaRival();
                List<CambioAdaptacion> cambios = ReglaAdaptacion.Aprender(m, Vistos(true, false, false), true, d);
                Assert.AreEqual(1, Nivel(m, TipoAdaptacion.Guardia, d));
                Assert.AreEqual(1, cambios.Count);
                Assert.AreEqual(TipoAdaptacion.Guardia, cambios[0].Tipo);
                Assert.AreEqual(0, cambios[0].NivelAntes);
                Assert.AreEqual(1, cambios[0].NivelDespues);
                Assert.IsTrue(cambios[0].Subio);
                Assert.IsFalse(cambios[0].Bajo);
            }
        }

        [Test]
        public void Aprender_TresPeleasSeguidas_DaNivelIISoloEnAvanzado()
        {
            MemoriaRival mb = new MemoriaRival();
            MemoriaRival ma = new MemoriaRival();
            List<CambioAdaptacion> ultimos = null;
            for (int i = 0; i < 3; i++)
            {
                ReglaAdaptacion.Aprender(mb, Vistos(true, false, false), true, baseDatos);
                ultimos = ReglaAdaptacion.Aprender(ma, Vistos(true, false, false), true, avanzado);
            }
            Assert.AreEqual(1, Nivel(mb, TipoAdaptacion.Guardia, baseDatos));
            Assert.AreEqual(2, Nivel(ma, TipoAdaptacion.Guardia, avanzado));
            Assert.AreEqual(1, ultimos.Count, "solo la tercera pelea sube de I a II");
            Assert.AreEqual(1, ultimos[0].NivelAntes);
            Assert.AreEqual(2, ultimos[0].NivelDespues);
        }

        [Test]
        public void Aprender_PeleaSinElHabito_RestaUnaEvidencia()
        {
            MemoriaRival m = new MemoriaRival { evidencias = new[] { 3, 0, 0 } };
            List<CambioAdaptacion> cambios = ReglaAdaptacion.Aprender(m, Vistos(false, false, false), true, avanzado);
            Assert.AreEqual(2, m.evidencias[0]);
            Assert.AreEqual(1, cambios.Count);
            Assert.AreEqual(2, cambios[0].NivelAntes);
            Assert.AreEqual(1, cambios[0].NivelDespues);
            Assert.IsTrue(cambios[0].Bajo);
        }

        [Test]
        public void Aprender_Tope_DiezPeleasConPunosYTresSinElloDejanGuardiaEnCero()
        {
            MemoriaRival m = new MemoriaRival();
            for (int i = 0; i < 10; i++)
            {
                ReglaAdaptacion.Aprender(m, Vistos(true, false, false), true, avanzado);
            }
            Assert.AreEqual(3, m.evidencias[0], "la evidencia no pasa del umbral del nivel II");
            for (int i = 0; i < 3; i++)
            {
                ReglaAdaptacion.Aprender(m, Vistos(false, false, false), true, avanzado);
            }
            Assert.AreEqual(0, Nivel(m, TipoAdaptacion.Guardia, avanzado));
            Assert.AreEqual(0, m.evidencias[0]);
        }

        [Test]
        public void Aprender_NoBajaDeCero()
        {
            MemoriaRival m = new MemoriaRival();
            List<CambioAdaptacion> cambios = ReglaAdaptacion.Aprender(m, Vistos(false, false, false), true, avanzado);
            Assert.AreEqual(0, m.evidencias[0]);
            Assert.AreEqual(0, cambios.Count);
        }

        [Test]
        public void Aprender_TresHabitos_QuedanDosActivosYSeApagaElDeMenosEvidencia()
        {
            // Guardia con 3 de evidencia (II), Fintas y Retirada con 0: ven los tres a la vez.
            MemoriaRival m = new MemoriaRival { evidencias = new[] { 3, 0, 0 } };
            List<CambioAdaptacion> cambios = ReglaAdaptacion.Aprender(m, Vistos(true, true, true), true, avanzado);
            // Fintas y Retirada quedan con 1: empatan en evidencia y ambos se vieron; baja el que va después (Retirada).
            Assert.AreEqual(2, Nivel(m, TipoAdaptacion.Guardia, avanzado));
            Assert.AreEqual(1, Nivel(m, TipoAdaptacion.Fintas, avanzado));
            Assert.AreEqual(0, Nivel(m, TipoAdaptacion.Retirada, avanzado));
            Assert.AreEqual(1, cambios.Count, "Retirada subió y bajó en la misma pelea: sin cambio neto");
            Assert.AreEqual(TipoAdaptacion.Fintas, cambios[0].Tipo);
        }

        [Test]
        public void Aprender_Desempate_PrimeroElQueNoSeVioEnEstaPelea()
        {
            // Fintas (index 1) y Retirada (2) con 1 de evidencia; Guardia con 3. Esta pelea se ve Guardia y Retirada.
            // Fintas no se vio: pierde 1 -> 0, ya no está activa; no hace falta desempatar. Armamos el empate real con maximoActivas 1.
            SerializedObject so = new SerializedObject(avanzado);
            so.FindProperty("maximoActivas").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            MemoriaRival m = new MemoriaRival { evidencias = new[] { 2, 2, 0 } };
            // Guardia no se ve (queda en 1), Fintas se ve (queda en 3): Guardia tiene menos evidencia, se apaga.
            ReglaAdaptacion.Aprender(m, Vistos(false, true, false), true, avanzado);
            Assert.AreEqual(0, Nivel(m, TipoAdaptacion.Guardia, avanzado));
            Assert.AreEqual(2, Nivel(m, TipoAdaptacion.Fintas, avanzado));

            // Empate de evidencia en 1 con maximoActivas 1: Guardia se vio, Fintas no se vio pero queda en 1 (venía de 2).
            m = new MemoriaRival { evidencias = new[] { 0, 2, 0 } };
            ReglaAdaptacion.Aprender(m, Vistos(true, false, false), true, avanzado);
            Assert.AreEqual(1, Nivel(m, TipoAdaptacion.Guardia, avanzado), "el que se vio se queda");
            Assert.AreEqual(0, Nivel(m, TipoAdaptacion.Fintas, avanzado), "el que no se vio se apaga");
        }

        [Test]
        public void Aprender_PrimeraDerrotaEnsena_SegundaSeguidaNoSubeNadaPeroOlvida_SiguienteVictoriaVuelveAEnsenar()
        {
            MemoriaRival m = new MemoriaRival();
            ReglaAdaptacion.Aprender(m, Vistos(true, false, false), false, avanzado);
            Assert.AreEqual(1, m.derrotasSeguidas);
            Assert.AreEqual(1, Nivel(m, TipoAdaptacion.Guardia, avanzado), "la primera derrota enseña");

            // Segunda derrota seguida: lo visto no sube; lo no visto (Guardia aquí) olvida.
            List<CambioAdaptacion> cambios = ReglaAdaptacion.Aprender(m, Vistos(false, true, false), false, avanzado);
            Assert.AreEqual(2, m.derrotasSeguidas);
            Assert.AreEqual(0, Nivel(m, TipoAdaptacion.Guardia, avanzado), "olvida");
            Assert.AreEqual(0, Nivel(m, TipoAdaptacion.Fintas, avanzado), "no sube");
            Assert.AreEqual(1, cambios.Count);
            Assert.IsTrue(cambios[0].Bajo);

            // Victoria: reinicia derrotas y vuelve a enseñar.
            ReglaAdaptacion.Aprender(m, Vistos(false, true, false), true, avanzado);
            Assert.AreEqual(0, m.derrotasSeguidas);
            Assert.AreEqual(1, Nivel(m, TipoAdaptacion.Fintas, avanzado));
        }

        [Test]
        public void Aprender_Cambios_SonExactosYEnOrdenDeTipo()
        {
            MemoriaRival m = new MemoriaRival { evidencias = new[] { 0, 1, 0 } };
            // Guardia sube a I, Fintas se ve y llega a 2 (sigue en I: sin cambio), Retirada no se ve (sigue en 0).
            List<CambioAdaptacion> cambios = ReglaAdaptacion.Aprender(m, Vistos(true, true, false), true, avanzado);
            Assert.AreEqual(1, cambios.Count);
            Assert.AreEqual(TipoAdaptacion.Guardia, cambios[0].Tipo);
            Assert.AreEqual(0, cambios[0].NivelAntes);
            Assert.AreEqual(1, cambios[0].NivelDespues);

            // Fintas llega a 3 (II) y Guardia a 2 (I): un solo cambio.
            cambios = ReglaAdaptacion.Aprender(m, Vistos(true, true, false), true, avanzado);
            Assert.AreEqual(1, cambios.Count);
            Assert.AreEqual(TipoAdaptacion.Fintas, cambios[0].Tipo);
            Assert.AreEqual(1, cambios[0].NivelAntes);
            Assert.AreEqual(2, cambios[0].NivelDespues);
        }

        [Test]
        public void Aprender_MemoriaNull_DevuelveListaVacia()
        {
            Assert.AreEqual(0, ReglaAdaptacion.Aprender(null, Vistos(true, true, true), true, avanzado).Count);
        }

        [Test]
        public void ListaMemoriasRival_IdaYVueltaPorJsonUtility_EsIgual()
        {
            ListaMemoriasRival lista = new ListaMemoriasRival();
            lista.memorias.Add(new MemoriaRival { idRival = "DatosRival_Liga1_1", evidencias = new[] { 3, 0, 1 }, peleas = 5, derrotasSeguidas = 1 });
            lista.memorias.Add(new MemoriaRival { idRival = "DatosRival_Casero", evidencias = new[] { 0, 2, 0 }, peleas = 2, derrotasSeguidas = 0 });

            ListaMemoriasRival vuelta = JsonUtility.FromJson<ListaMemoriasRival>(JsonUtility.ToJson(lista));

            Assert.AreEqual(2, vuelta.memorias.Count);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(lista.memorias[i].idRival, vuelta.memorias[i].idRival);
                CollectionAssert.AreEqual(lista.memorias[i].evidencias, vuelta.memorias[i].evidencias);
                Assert.AreEqual(lista.memorias[i].peleas, vuelta.memorias[i].peleas);
                Assert.AreEqual(lista.memorias[i].derrotasSeguidas, vuelta.memorias[i].derrotasSeguidas);
            }
        }
    }
}
