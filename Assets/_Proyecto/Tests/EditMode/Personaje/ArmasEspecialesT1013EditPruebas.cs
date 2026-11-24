using System;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1013: el tiro en arco, la explosión con caída, el rifle de plasma (cargar 3 s, rayo 5 s) y los datos de las cinco armas nuevas.</summary>
    public class ArmasEspecialesT1013EditPruebas
    {
        private const float G = -9.81f;

        [Test]
        public void DireccionHacia_ConLaDireccionCalculada_ElArcoPasaPorElBlanco()
        {
            Vector3 origen = new Vector3(0f, 1.5f, 0f);
            Vector3 blanco = new Vector3(3f, 0f, 20f);
            Assert.IsTrue(ReglaBalistica.DireccionHacia(origen, blanco, 28f, G, out Vector3 dir));
            Vector3 v = dir * 28f;
            float x = new Vector2(blanco.x, blanco.z).magnitude;
            float t = x / new Vector2(v.x, v.z).magnitude;
            Vector3 llega = ReglaBalistica.Posicion(origen, v, new Vector3(0f, G, 0f), t);
            Assert.AreEqual(blanco.x, llega.x, 0.05f);
            Assert.AreEqual(blanco.y, llega.y, 0.05f);
            Assert.AreEqual(blanco.z, llega.z, 0.05f);
            Assert.Less(Vector3.Angle(dir, Vector3.ProjectOnPlane(dir, Vector3.up)), 30f, "Toma el tiro tenso (ángulo bajo)");
        }

        [Test]
        public void DireccionHacia_FueraDeAlcance_Devuelve45Grados()
        {
            Assert.IsFalse(ReglaBalistica.DireccionHacia(Vector3.zero, new Vector3(0f, 0f, 500f), 20f, G, out Vector3 dir));
            Assert.AreEqual(45f, Vector3.Angle(dir, Vector3.forward), 0.5f);
        }

        [Test]
        public void Elevar_SubeLaMiraSinPasarDeLaVertical()
        {
            Assert.AreEqual(14f, Vector3.Angle(ReglaBalistica.Elevar(Vector3.forward, 14f), Vector3.forward), 0.01f);
            Assert.Greater(ReglaBalistica.Elevar(Vector3.forward, 14f).y, 0f, "Hacia arriba");
            Assert.AreEqual(Vector3.up, ReglaBalistica.Elevar(Vector3.up, 30f));
        }

        [Test]
        public void Arco_EmpiezaEnElOrigenYCae()
        {
            Vector3[] puntos = new Vector3[10];
            ReglaBalistica.Arco(Vector3.up, new Vector3(0f, 5f, 10f), new Vector3(0f, G, 0f), 2f, puntos);
            Assert.AreEqual(Vector3.up, puntos[0]);
            Assert.Less(puntos[9].y, puntos[4].y, "Al final cae");
            Assert.AreEqual(20f, puntos[9].z, 0.001f);
        }

        [Test]
        public void DanioExplosion_CompletoEnElCentro_ParteEnElBorde_NadaFuera()
        {
            Assert.AreEqual(100f, ReglaBalistica.DanioExplosion(100f, 5f, 0f, 0.3f), 0.001f);
            Assert.AreEqual(30f, ReglaBalistica.DanioExplosion(100f, 5f, 5f, 0.3f), 0.001f);
            Assert.AreEqual(65f, ReglaBalistica.DanioExplosion(100f, 5f, 2.5f, 0.3f), 0.001f);
            Assert.AreEqual(0f, ReglaBalistica.DanioExplosion(100f, 5f, 5.1f, 0.3f));
        }

        [Test]
        public void Plasma_CargaTresSegundos_RayoCincoAunqueSueltes_YLuegoHayQueSoltar()
        {
            ReglaRayoPlasma p = new ReglaRayoPlasma(3f, 5f);
            int disparos = 0;
            Func<bool> puede = () => { disparos++; return true; };
            bool empezo = false;
            for (int i = 0; i < 29; i++)
            {
                empezo |= p.Avanzar(0.1f, true, puede);
            }
            Assert.AreEqual(EstadoPlasma.Cargando, p.Estado, "A los 2.9 s sigue cargando");
            Assert.AreEqual(2.9f / 3f, p.Carga, 0.01f);
            Assert.IsFalse(empezo);
            Assert.IsTrue(p.Avanzar(0.15f, true, puede), "A los 3 s sale el rayo");
            Assert.AreEqual(EstadoPlasma.Rayo, p.Estado);
            Assert.AreEqual(1, disparos, "Gasta una celda");
            for (int i = 0; i < 49; i++)
            {
                p.Avanzar(0.1f, false, puede);
            }
            Assert.AreEqual(EstadoPlasma.Rayo, p.Estado, "Soltar no corta el rayo");
            p.Avanzar(0.2f, true, puede);
            Assert.AreEqual(EstadoPlasma.Reposo, p.Estado, "A los 5 s se acaba");
            p.Avanzar(0.5f, true, puede);
            Assert.AreEqual(EstadoPlasma.Reposo, p.Estado, "Con el botón aún abajo no vuelve a cargar");
            p.Avanzar(0.1f, false, puede);
            p.Avanzar(0.1f, true, puede);
            Assert.AreEqual(EstadoPlasma.Cargando, p.Estado, "Al soltar y volver a pulsar carga otra vez");
        }

        [Test]
        public void Plasma_SoltarAntesDeCargar_Cancela_YSinCeldasNoDispara()
        {
            ReglaRayoPlasma p = new ReglaRayoPlasma(3f, 5f);
            p.Avanzar(1f, true, () => true);
            p.Avanzar(0.1f, false, () => true);
            Assert.AreEqual(EstadoPlasma.Reposo, p.Estado);
            Assert.AreEqual(0f, p.Carga);
            for (int i = 0; i < 40; i++)
            {
                Assert.IsFalse(p.Avanzar(0.1f, true, () => false));
            }
            Assert.AreEqual(EstadoPlasma.Reposo, p.Estado, "Sin celdas no sale el rayo");
        }

        [Test]
        public void Datos_LasCincoArmasNuevasConSusCuatroNivelesYSuTipoDeDisparo()
        {
            CatalogoArmas catalogo = AssetDatabase.LoadAssetAtPath<CatalogoArmas>("Assets/_Proyecto/Personaje/Datos/CatalogoArmas_Base.asset");
            (ArmaAPie arma, TipoDisparoArma tipo)[] nuevas =
            {
                (ArmaAPie.Subfusil, TipoDisparoArma.Bala), (ArmaAPie.Revolver, TipoDisparoArma.Bala), (ArmaAPie.Lanzagranadas, TipoDisparoArma.Proyectil),
                (ArmaAPie.Granada, TipoDisparoArma.Lanzada), (ArmaAPie.RiflePlasma, TipoDisparoArma.Rayo),
            };
            foreach (var n in nuevas)
            {
                for (int nivel = 0; nivel < 4; nivel++)
                {
                    DatosArma d = catalogo.Buscar(n.arma, nivel);
                    Assert.IsNotNull(d, n.arma + " nivel " + nivel);
                    Assert.AreEqual(n.tipo, d.TipoDisparo, n.arma + " dispara como " + n.tipo);
                    Assert.Greater(d.Precio, 0, n.arma + " se compra");
                }
            }
            Assert.IsTrue(catalogo.Buscar(ArmaAPie.Subfusil, 0).Automatica, "El subfusil es automático");
            Assert.AreEqual(12, catalogo.Buscar(ArmaAPie.Revolver, 0).Cargador, "El revólver lleva 12 balas (T92; el tambor sigue con 6 recámaras)");
            Assert.Greater(catalogo.Buscar(ArmaAPie.Lanzagranadas, 0).RadioExplosion, 3f);
            Assert.Greater(catalogo.Buscar(ArmaAPie.Granada, 0).RadioExplosion, 3f);
            DatosArma plasma = catalogo.Buscar(ArmaAPie.RiflePlasma, 0);
            Assert.AreEqual(3f, plasma.SegundosCarga, 0.001f, "Carga 3 s");
            Assert.AreEqual(5f, plasma.SegundosRayo, 0.001f, "Rayo de 5 s");
            ConfigArmasEspeciales cfg = AssetDatabase.LoadAssetAtPath<ConfigArmasEspeciales>("Assets/_Proyecto/Personaje/Datos/Resources/ConfigArmasEspeciales.asset");
            Assert.IsNotNull(cfg.PrefabExplosion, "La explosión tiene efecto");
            Assert.IsNotNull(cfg.MaterialRayo, "El rayo tiene material");
        }
    }
}
