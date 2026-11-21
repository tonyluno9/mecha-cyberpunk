using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1472: qué ID se pide por arma y evento, la regla de material de los impactos, la cola de la ráfaga y el tope de explosiones en cadena.</summary>
    public class SonidosArmasT1472Pruebas
    {
        private sealed class SonidosFalsos : ISonidos
        {
            public readonly List<string> Ids = new List<string>();
            public void Reproducir(string id) { Ids.Add(id); }
            public void ReproducirEn(string id, Vector3 posicion) { Ids.Add(id); }
            public int ReproducirBucle(string id, Transform sigue) { Ids.Add("bucle:" + id); return 1; }
            public void DetenerBucle(int bucle) { }
            public void FijarTono(int bucle, float tono) { }
            public float Volumen(SeccionSonido seccion) { return 1f; }
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        [TestCase(ArmaAPie.Pistola, "ARM_W01_DISPARO")]
        [TestCase(ArmaAPie.Escopeta, "ARM_W02_DISPARO")]
        [TestCase(ArmaAPie.Fusil, "ARM_W03_DISPARO")]
        [TestCase(ArmaAPie.Sniper, "ARM_W04_DISPARO")]
        [TestCase(ArmaAPie.Subfusil, "ARM_SUB_DISPARO")]
        [TestCase(ArmaAPie.Revolver, "ARM_W06_DISPARO")]
        [TestCase(ArmaAPie.Lanzagranadas, "ARM_W08_DISPARO")]
        [TestCase(ArmaAPie.RiflePlasma, "ARM_W07_DISPARO")]
        public void IdDisparo_CadaArma_PideSuId(ArmaAPie arma, string esperado)
        {
            Assert.AreEqual(esperado, SonidosArmasT1472.IdDisparo(arma));
            Assert.AreEqual(esperado, PistolaAPie.IdDisparo(arma));
        }

        [TestCase(ArmaAPie.Fusil, "ARM_W03_RECARGA_CARGADOR")]
        [TestCase(ArmaAPie.Subfusil, "ARM_W03_RECARGA_CARGADOR")]
        [TestCase(ArmaAPie.Escopeta, "ARM_ESC_RECARGA_CARTUCHO")]
        [TestCase(ArmaAPie.Sniper, "ARM_SNI_RECARGA")]
        [TestCase(ArmaAPie.Pistola, "ARM_PIS_RECARGA")]
        [TestCase(ArmaAPie.Revolver, "ARM_W06_MECANICA")]
        public void IdRecarga_CadaArma_PideSuId(ArmaAPie arma, string esperado)
        {
            Assert.AreEqual(esperado, SonidosArmasT1472.IdRecarga(arma));
        }

        // T-1532 (LEEME 20-nov): sacar y guardar usa la recarga de cada arma; solo el revolver y la granada usan ARM_W06_SACAR.
        [TestCase(ArmaAPie.Pistola, "ARM_PIS_RECARGA")]
        [TestCase(ArmaAPie.Escopeta, "ARM_ESC_RECARGA_CARTUCHO")]
        [TestCase(ArmaAPie.Fusil, "ARM_W03_RECARGA_CARGADOR")]
        [TestCase(ArmaAPie.Sniper, "ARM_SNI_RECARGA")]
        [TestCase(ArmaAPie.Subfusil, "ARM_W03_RECARGA_CARGADOR")]
        [TestCase(ArmaAPie.RiflePlasma, "ARM_W03_RECARGA_CARGADOR")]
        [TestCase(ArmaAPie.Revolver, "ARM_W06_SACAR")]
        [TestCase(ArmaAPie.Granada, "ARM_W06_SACAR")]
        public void IdSacar_ArmasDeMano_UsanSuRecargaOElSacarDelRevolver(ArmaAPie arma, string esperado)
        {
            Assert.AreEqual(esperado, SonidosArmasT1472.IdSacar(arma));
        }

        [Test]
        public void IdSacar_Punos_NoPideNada()
        {
            Assert.IsNull(SonidosArmasT1472.IdSacar(ArmaAPie.Punos));
        }

        [Test]
        public void IdTrasDisparo_EscopetaFrancotiradorYRevolver()
        {
            Assert.AreEqual("ARM_W02_BOMBA", SonidosArmasT1472.IdTrasDisparo(ArmaAPie.Escopeta));
            Assert.AreEqual("ARM_W04_CERROJO", SonidosArmasT1472.IdTrasDisparo(ArmaAPie.Sniper));
            Assert.AreEqual("ARM_W06_MECANICA", SonidosArmasT1472.IdTrasDisparo(ArmaAPie.Revolver));
            Assert.IsNull(SonidosArmasT1472.IdTrasDisparo(ArmaAPie.Pistola));
            Assert.IsNull(SonidosArmasT1472.IdTrasDisparo(ArmaAPie.Fusil));
        }

        [Test]
        public void IdDisparoEnemigo_UnPerdigonPistola_VariosEscopeta()
        {
            Assert.AreEqual("ARM_W01_DISPARO", SonidosArmasT1472.IdDisparoEnemigo(1));
            Assert.AreEqual("ARM_W02_DISPARO", SonidosArmasT1472.IdDisparoEnemigo(6));
        }

        [Test]
        public void Ids_PedidosAlFalso_LlegaElIdDeCadaArma()
        {
            SonidosFalsos falso = new SonidosFalsos();
            Registro<ISonidos>.Agregar(falso);
            try
            {
                Sonidos.Reproducir(SonidosArmasT1472.IdDisparo(ArmaAPie.Revolver));
                Sonidos.ReproducirEn(SonidosArmasT1472.IdTrasDisparo(ArmaAPie.Escopeta), Vector3.zero);
                Assert.AreEqual(new[] { "ARM_W06_DISPARO", "ARM_W02_BOMBA" }, falso.Ids.ToArray());
            }
            finally
            {
                Registro<ISonidos>.Quitar(falso);
            }
        }

        [TestCase(true, true, false, "", "IMP_BALA_CABEZA")]
        [TestCase(true, false, false, "Pandillero", "IMP_BALA_CARNE")]
        [TestCase(true, false, false, "GuardiaNova", "IMP_BALA_ROBOT")]
        [TestCase(false, false, false, "DronPatrulla", "IMP_BALA_ROBOT")]
        [TestCase(false, false, false, "MechaRival", "IMP_BALA_ROBOT")]
        [TestCase(false, false, true, "CarroViejo", "IMP_BALA_METAL")]
        [TestCase(false, false, false, "Contenedor_03", "IMP_BALA_METAL")]
        [TestCase(false, false, false, "Lamina_ondulada", "IMP_BALA_METAL")]
        [TestCase(false, false, false, "Ventana_Edificio", "IMP_BALA_CRISTAL")]
        [TestCase(false, false, false, "mat_Vidrio_Azul", "IMP_BALA_CRISTAL")]
        [TestCase(false, false, true, "Parabrisas", "IMP_BALA_CRISTAL")]
        [TestCase(false, false, false, "Pared", "IMP_BALA_CONCRETO")]
        [TestCase(false, false, false, null, "IMP_BALA_CONCRETO")]
        public void MaterialImpacto_PorComponentesYNombres(bool persona, bool cabeza, bool vehiculo, string nombres, string esperado)
        {
            Assert.AreEqual(esperado, ReglaMaterialImpacto.Id(persona, cabeza, vehiculo, nombres));
        }

        [Test]
        public void Zumba_SoloSiPasaACercaDe2Metros()
        {
            Assert.IsTrue(ReglaMaterialImpacto.Zumba(0.5f));
            Assert.IsTrue(ReglaMaterialImpacto.Zumba(2f));
            Assert.IsFalse(ReglaMaterialImpacto.Zumba(2.5f));
        }

        [Test]
        public void Rafaga_RepiteTiroYLaColaSuenaUnaVezAlSoltar()
        {
            ReglaColaRafaga regla = new ReglaColaRafaga();
            SonidosFalsos falso = new SonidosFalsos();
            Registro<ISonidos>.Agregar(falso);
            try
            {
                for (int bala = 0; bala < 5; bala++)
                {
                    Sonidos.Reproducir(SonidosArmasT1472.IdDisparo(ArmaAPie.Fusil)); // cada bala repite el tiro, no hay clip de ráfaga
                    Assert.IsFalse(regla.Avanzar(true, true));
                    Assert.IsFalse(regla.Avanzar(false, true), "Con el gatillo pulsado entre bala y bala no suena la cola");
                }
                if (regla.Avanzar(false, false))
                {
                    Sonidos.Reproducir(SonidosArmasT1472.ColaCalle);
                }
                Assert.IsFalse(regla.Avanzar(false, false), "La cola suena una sola vez");
                Assert.AreEqual(5, falso.Ids.FindAll(i => i == "ARM_W03_DISPARO").Count);
                Assert.AreEqual(1, falso.Ids.FindAll(i => i == "ARM_COLA_CALLE").Count);
            }
            finally
            {
                Registro<ISonidos>.Quitar(falso);
            }
        }

        [Test]
        public void Rafaga_SinDisparar_NoHayCola()
        {
            ReglaColaRafaga regla = new ReglaColaRafaga();
            Assert.IsFalse(regla.Avanzar(false, false));
            Assert.IsFalse(regla.Avanzar(false, true));
        }

        [Test]
        public void Rafaga_AcabarseElCargadorTambienCierraConCola()
        {
            ReglaColaRafaga regla = new ReglaColaRafaga();
            regla.Avanzar(true, true);
            Assert.IsTrue(regla.Avanzar(false, false));
        }

        [Test]
        public void CadenaExplosiones_DiezEnElMismoFrame_SuenaUnaSola()
        {
            LimiteExplosionesSonido limite = new LimiteExplosionesSonido();
            int sonaron = 0;
            for (int i = 0; i < 10; i++)
            {
                if (limite.Permite(1f))
                {
                    sonaron++;
                }
            }
            Assert.AreEqual(1, sonaron);
        }

        [Test]
        public void CadenaExplosiones_EscalonadasNoPasanDeTresEnLaVentana()
        {
            LimiteExplosionesSonido limite = new LimiteExplosionesSonido();
            int sonaron = 0;
            for (int i = 0; i < 5; i++)
            {
                if (limite.Permite(i * 0.1f)) // una cada 0.1 s dentro de la ventana de 0.6 s
                {
                    sonaron++;
                }
            }
            Assert.AreEqual(3, sonaron);
        }

        [Test]
        public void CadenaExplosiones_DespuesDeLaVentanaVuelveASonar()
        {
            LimiteExplosionesSonido limite = new LimiteExplosionesSonido();
            Assert.IsTrue(limite.Permite(0f));
            Assert.IsTrue(limite.Permite(0.2f));
            Assert.IsTrue(limite.Permite(0.4f));
            Assert.IsFalse(limite.Permite(0.5f), "Ya van tres en la ventana");
            Assert.IsTrue(limite.Permite(1.2f));
        }
    }
}
