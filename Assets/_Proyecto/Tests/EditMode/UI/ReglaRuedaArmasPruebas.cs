using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Rueda;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-991 y T-1010 (M30): la lógica pura de la rueda de armas y objetos (disposición de lo que llevas, ángulo a sector, zona muerta y resultado al soltar).</summary>
    public class ReglaRuedaArmasPruebas
    {
        private static Vector2 Cursor(float grados, float radio)
        {
            float a = grados * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radio;
        }

        private static readonly int Pistola = (int)ArmaAPie.Pistola;
        private static readonly int Escopeta = (int)ArmaAPie.Escopeta;
        private static readonly int Fusil = (int)ArmaAPie.Fusil;
        private static readonly int Sniper = (int)ArmaAPie.Sniper;
        private static readonly int Punos = (int)ArmaAPie.Punos;

        [Test]
        public void Disposicion_ComoElMockupM30_ArmasLuegoPunosLuegoObjetos()
        {
            // Las cuatro armas, bebida (0), papitas (1) y venda (2): ocho sectores, pistola arriba y en el sentido del reloj.
            HashSet<int> llevas = new HashSet<int> { Pistola, Escopeta, Fusil, Sniper, ReglaRuedaArmas.CasillaObjeto(0), ReglaRuedaArmas.CasillaObjeto(1), ReglaRuedaArmas.CasillaObjeto(2) };
            List<int> d = ReglaRuedaArmas.Disposicion(llevas.Contains);
            CollectionAssert.AreEqual(new[] { Pistola, Escopeta, Fusil, Sniper, Punos, ReglaRuedaArmas.CasillaObjeto(0), ReglaRuedaArmas.CasillaObjeto(1), ReglaRuedaArmas.CasillaObjeto(2) }, d);
            Assert.AreEqual(0f, ReglaRuedaArmas.AnguloDeSector(0, d.Count), 0.001f, "La pistola arriba");
            Assert.AreEqual(90f, ReglaRuedaArmas.AnguloDeSector(2, d.Count), 0.001f, "El fusil a la derecha");
            Assert.AreEqual(180f, ReglaRuedaArmas.AnguloDeSector(4, d.Count), 0.001f, "Los puños abajo");
        }

        [Test]
        public void Disposicion_ConPocoSeRellenaConVaciosHastaOcho_YLosPunosSiempreEstan()
        {
            List<int> d = ReglaRuedaArmas.Disposicion(c => false);
            Assert.AreEqual(ReglaRuedaArmas.MinimoSectores, d.Count);
            Assert.AreEqual(Punos, d[0], "Sin nada, solo los puños");
            for (int i = 1; i < d.Count; i++)
            {
                Assert.AreEqual(ReglaRuedaArmas.Vacia, d[i]);
            }
        }

        [Test]
        public void Disposicion_ConTodoCabenTodasLasArmasYLosObjetos()
        {
            List<int> d = ReglaRuedaArmas.Disposicion(c => true);
            Assert.AreEqual(ReglaRuedaArmas.CasillasArma + ReglaRuedaArmas.ObjetosMaximos, d.Count);
            CollectionAssert.AllItemsAreUnique(d);
        }

        [Test]
        public void SectorEnAngulo_CadaCentroDaSuSector_YLosBordesSeRepartenALaMitad()
        {
            for (int n = 8; n <= 15; n++)
            {
                for (int i = 0; i < n; i++)
                {
                    Assert.AreEqual(i, ReglaRuedaArmas.SectorEnAngulo(ReglaRuedaArmas.AnguloDeSector(i, n), n), "n " + n + " sector " + i);
                }
            }
            Assert.AreEqual(0, ReglaRuedaArmas.SectorEnAngulo(22f, 8));
            Assert.AreEqual(1, ReglaRuedaArmas.SectorEnAngulo(23f, 8));
            Assert.AreEqual(0, ReglaRuedaArmas.SectorEnAngulo(340f, 8), "Cerca de 360° vuelve al primero");
        }

        [Test]
        public void CasillaResaltada_ZonaMuertaVacioYNoDisponibleDanMenosUno()
        {
            List<int> d = ReglaRuedaArmas.Disposicion(c => c == Pistola);
            Assert.AreEqual(-1, ReglaRuedaArmas.CasillaResaltada(Cursor(0f, ReglaRuedaArmas.ZonaMuerta * 0.5f), d, null), "Zona muerta");
            Assert.AreEqual(Pistola, ReglaRuedaArmas.CasillaResaltada(Cursor(0f, 120f), d, null), "Arriba, la pistola");
            Assert.AreEqual(Punos, ReglaRuedaArmas.CasillaResaltada(Cursor(45f, 120f), d, null), "Luego los puños");
            Assert.AreEqual(-1, ReglaRuedaArmas.CasillaResaltada(Cursor(180f, 120f), d, null), "Un sector vacío");
            Assert.AreEqual(-1, ReglaRuedaArmas.CasillaResaltada(Cursor(0f, 120f), d, c => false), "No disponible");
        }

        [Test]
        public void MoverCursor_SeQuedaDentroDelRadioMaximo()
        {
            Vector2 c = ReglaRuedaArmas.MoverCursor(Vector2.zero, new Vector2(1000f, 0f));
            Assert.AreEqual(ReglaRuedaArmas.RadioMaximo, c.magnitude, 0.01f);
        }

        [Test]
        public void Resultado_ArmaObjetoONada()
        {
            SeleccionRueda arma = ReglaRuedaArmas.Resultado(Fusil);
            Assert.AreEqual(TipoSeleccionRueda.Arma, arma.Tipo);
            Assert.AreEqual(Fusil, arma.Indice);
            SeleccionRueda objeto = ReglaRuedaArmas.Resultado(ReglaRuedaArmas.CasillaObjeto(2));
            Assert.AreEqual(TipoSeleccionRueda.Objeto, objeto.Tipo);
            Assert.AreEqual(2, objeto.Indice);
            Assert.AreEqual(TipoSeleccionRueda.Nada, ReglaRuedaArmas.Resultado(-1).Tipo);
            Assert.AreEqual(TipoSeleccionRueda.Nada, ReglaRuedaArmas.Resultado(ReglaRuedaArmas.Casillas).Tipo);
        }
    }
}
