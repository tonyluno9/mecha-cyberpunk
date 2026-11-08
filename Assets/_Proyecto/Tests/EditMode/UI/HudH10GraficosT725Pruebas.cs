using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-725: la configuración del HUD trae todos los gráficos del chat de arte (H20 a H26), ya como sprites.</summary>
    public class HudH10GraficosT725Pruebas
    {
        private static readonly string[] IdsObjeto = { "bebida_energetica", "papitas", "venda", "cafe", "bateria_escudo" };

        private static bool EsDelChatDeArte(Sprite sprite) => sprite != null && AssetDatabase.GetAssetPath(sprite).Contains("UI/Iconos/H10/");

        [Test]
        public void Config_TraeLosGraficosDeLasBarrasElMarcoLaCasillaYLasEstrellas()
        {
            ConfigHudH10 cfg = ConfigHudH10.Actual;
            Assert.IsNotNull(cfg);
            Assert.IsTrue(EsDelChatDeArte(cfg.MarcoVida), "H21 marco de vida");
            Assert.IsTrue(EsDelChatDeArte(cfg.MarcoEscudo), "H21 marco de escudo");
            Assert.IsTrue(EsDelChatDeArte(cfg.RellenoVida), "H20 relleno de vida");
            Assert.IsTrue(EsDelChatDeArte(cfg.RellenoEscudo), "H20 relleno de escudo");
            Assert.IsTrue(EsDelChatDeArte(cfg.EmblemaVida), "Emblema");
            Assert.IsTrue(EsDelChatDeArte(cfg.MarcoMinimapa), "H22");
            Assert.IsTrue(EsDelChatDeArte(cfg.CasillaObjeto), "H26");
            Assert.IsTrue(EsDelChatDeArte(cfg.EstrellaLlena) && EsDelChatDeArte(cfg.EstrellaContorno), "H25");
        }

        [Test]
        public void Config_ElMarcoDelMinimapaTieneBordeParaElNueveCortes()
        {
            Sprite marco = ConfigHudH10.Actual.MarcoMinimapa;
            Assert.Greater(marco.border.x, 8f);
            Assert.AreEqual(marco.border.x, marco.border.z, 0.01f);
            Assert.AreEqual(marco.border.y, marco.border.w, 0.01f);
            Assert.Less(marco.border.x * 2f, marco.rect.width, "Queda centro por estirar");
        }

        [Test]
        public void Config_LosCincoObjetosYElMunequitoSonDelChatDeArte()
        {
            ConfigHudH10 cfg = ConfigHudH10.Actual;
            foreach (string id in IdsObjeto)
            {
                Assert.IsTrue(EsDelChatDeArte(cfg.Icono(id)), "Ícono " + id);
            }
            for (int z = 0; z < ConfigHudH10.Zonas; z++)
            {
                Assert.IsTrue(EsDelChatDeArte(cfg.CuerpoLinea((ZonaCuerpoH10)z)), "Línea " + (ZonaCuerpoH10)z);
                Assert.IsTrue(EsDelChatDeArte(cfg.CuerpoRelleno((ZonaCuerpoH10)z)), "Relleno " + (ZonaCuerpoH10)z);
            }
        }

        [Test]
        public void Config_LasPartesDelMunequitoTienenElMismoTamanoParaApilarse()
        {
            ConfigHudH10 cfg = ConfigHudH10.Actual;
            Rect referencia = cfg.CuerpoLinea(ZonaCuerpoH10.Cabeza).rect;
            for (int z = 1; z < ConfigHudH10.Zonas; z++)
            {
                Assert.AreEqual(referencia.size, cfg.CuerpoLinea((ZonaCuerpoH10)z).rect.size, "Parte " + (ZonaCuerpoH10)z);
            }
        }

        [Test]
        public void Config_LaPerdidaEsperaUnMomentoYBajaEnMedioSegundoAproximadamente()
        {
            ConfigHudH10 cfg = ConfigHudH10.Actual;
            Assert.Greater(cfg.SegundosEsperaPerdida, 0.1f);
            Assert.Less(cfg.SegundosEsperaPerdida, 1.5f);
            Assert.Greater(cfg.SegundosBajaPerdida, 0.2f);
            Assert.Less(cfg.SegundosBajaPerdida, 1.5f);
        }
    }
}
