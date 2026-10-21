using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    public class ReglaEstadisticasPiezasPruebas
    {
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private readonly List<Object> creados = new List<Object>();

        private ValoresMecha DeSerie() => AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaMecha).Valores;

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in creados) if (o != null) Object.DestroyImmediate(o);
            creados.Clear();
        }

        private DatosPieza Pieza(RanuraPieza ranura, float vida = 0f, float danio = 0f, float vel = 0f, float recarga = 0f, string id = null)
        {
            DatosPieza p = DatosPieza.Crear(id ?? "p_" + ranura + "_" + creados.Count, "P", ranura, RamaPieza.Militar, 1, 500, vida, danio, vel, recarga);
            creados.Add(p);
            return p;
        }

        [Test]
        public void Componer_SinPiezasONulas_DaLosValoresDeSerie()
        {
            ValoresMecha s = DeSerie();
            ValoresMecha v = ReglaEstadisticasPiezas.Componer(s, new List<DatosPieza>());
            Assert.AreEqual(s.vidaMaxima, v.vidaMaxima);
            Assert.AreEqual(s.velocidadMaxima, v.velocidadMaxima);
            Assert.AreEqual(s.danioGolpe, v.danioGolpe);
            Assert.AreEqual(s.recargaGolpe, v.recargaGolpe, 1e-5f);
            Assert.AreEqual(s.aceleracion, v.aceleracion, 1e-5f);
        }

        [Test]
        public void Componer_CincoDeSerieDelCatalogo_DanLosValoresDeDatosMecha()
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>("Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset");
            Assert.NotNull(catalogo);
            var puestas = new List<DatosPieza>();
            for (int r = 0; r < 5; r++) puestas.Add(catalogo.DeSerie((RanuraPieza)r));
            ValoresMecha s = DeSerie();
            ValoresMecha v = ReglaEstadisticasPiezas.Componer(s, puestas);
            Assert.AreEqual(100f, v.vidaMaxima);
            Assert.AreEqual(4f, v.velocidadMaxima);
            Assert.AreEqual(10f, v.danioGolpe);
            Assert.AreEqual(0.8f, v.recargaGolpe, 1e-5f);
            Assert.AreEqual(s.aceleracion, v.aceleracion, 1e-5f);
            Assert.AreEqual(s.desaceleracion, v.desaceleracion, 1e-5f);
            Assert.AreEqual(s.velocidadGiroMaxima, v.velocidadGiroMaxima, 1e-5f);
            Assert.AreEqual(s.alcanceGolpe, v.alcanceGolpe);
        }

        [Test]
        public void Componer_TorsoCuerpoACuerpoII_Suma60DeVida()
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>("Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset");
            DatosPieza torso = catalogo.Buscar("pieza_torso_cuerpoacuerpo_2");
            ValoresMecha v = ReglaEstadisticasPiezas.Componer(DeSerie(), new List<DatosPieza> { torso });
            Assert.AreEqual(160f, v.vidaMaxima, 1e-4f);
        }

        [Test]
        public void Componer_PiernasTechI_SubenVelocidadYAceleracionEnLaMismaRazon()
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>("Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset");
            ValoresMecha s = DeSerie();
            ValoresMecha v = ReglaEstadisticasPiezas.Componer(s, new List<DatosPieza> { catalogo.Buscar("pieza_piernas_tech_1") });
            Assert.AreEqual(4.15f, v.velocidadMaxima, 1e-4f);
            float razon = 4.15f / 4f;
            Assert.AreEqual(s.aceleracion * razon, v.aceleracion, 1e-4f);
            Assert.AreEqual(s.desaceleracion * razon, v.desaceleracion, 1e-4f);
            Assert.AreEqual(s.velocidadGiroMaxima * razon, v.velocidadGiroMaxima, 1e-3f);
        }

        [Test]
        public void Componer_BrazoIzqFuegoIIIYBrazoDerMilitarIII_MezclaRamas()
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>("Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset");
            var puestas = new List<DatosPieza>
            {
                catalogo.Buscar("pieza_brazoizq_fuego_3"),
                catalogo.Buscar("pieza_brazoder_militar_3"),
                catalogo.DeSerie(RanuraPieza.Torso),
                catalogo.DeSerie(RanuraPieza.Piernas),
                catalogo.DeSerie(RanuraPieza.Cabina)
            };
            ValoresMecha v = ReglaEstadisticasPiezas.Componer(DeSerie(), puestas);
            Assert.AreEqual(26.5f, v.danioGolpe, 1e-4f);
            Assert.AreEqual(0.71f, v.recargaGolpe, 1e-4f);
        }

        [Test]
        public void Componer_RecargaNegativaGrande_NoBajaDe03()
        {
            var puestas = new List<DatosPieza>
            {
                Pieza(RanuraPieza.BrazoIzquierdo, recarga: -0.5f),
                Pieza(RanuraPieza.BrazoDerecho, recarga: -0.5f)
            };
            ValoresMecha v = ReglaEstadisticasPiezas.Componer(DeSerie(), puestas);
            Assert.AreEqual(0.3f, v.recargaGolpe, 1e-5f);
        }

        [Test]
        public void Componer_PiezaRepetidaEnRanura_SeIgnoraConWarning()
        {
            var puestas = new List<DatosPieza> { Pieza(RanuraPieza.Torso, vida: 10f, id: "a"), Pieza(RanuraPieza.Torso, vida: 50f, id: "b") };
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("repetida"));
            ValoresMecha v = ReglaEstadisticasPiezas.Componer(DeSerie(), puestas);
            Assert.AreEqual(110f, v.vidaMaxima, 1e-4f);
        }

        [Test]
        public void Componer_PiezaNull_SeIgnoraConWarning()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("null"));
            ValoresMecha v = ReglaEstadisticasPiezas.Componer(DeSerie(), new List<DatosPieza> { null });
            Assert.AreEqual(100f, v.vidaMaxima);
        }

        [Test]
        public void Componer_PiezaEnRanuraEquivocada_SeIgnoraConWarning()
        {
            var porRanura = new Dictionary<RanuraPieza, DatosPieza> { { RanuraPieza.Cabina, Pieza(RanuraPieza.Torso, vida: 50f) } };
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no cabe"));
            ValoresMecha v = ReglaEstadisticasPiezas.Componer(DeSerie(), porRanura);
            Assert.AreEqual(100f, v.vidaMaxima);
        }
    }
}
