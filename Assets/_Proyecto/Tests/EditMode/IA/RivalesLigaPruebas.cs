using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Rival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>Pruebas de T-157 sobre los assets reales de los 20 rivales de liga (reglas de D-tony-97).</summary>
    public class RivalesLigaPruebas
    {
        private const int Ligas = 4;
        private const int RivalesPorLiga = 5;
        private const string RutaDatosJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";

        private static GameObject Cargar(int liga, int n)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga" + liga + "_" + n + ".prefab");
            Assert.IsNotNull(prefab, "Falta el prefab de la liga " + liga + " rival " + n);
            return prefab;
        }

        private static DatosRival Datos(int liga, int n)
        {
            RivalBasico rival = Cargar(liga, n).GetComponent<RivalBasico>();
            Assert.IsNotNull(rival, "Sin RivalBasico en liga " + liga + " rival " + n);
            DatosRival datos = new SerializedObject(rival).FindProperty("datos").objectReferenceValue as DatosRival;
            Assert.IsNotNull(datos, "Sin DatosRival en liga " + liga + " rival " + n);
            return datos;
        }

        [Test]
        public void Prefabs_Son20ConAliasUnicoYSuVidaConElMismoDatosMecha()
        {
            HashSet<string> alias = new HashSet<string>();
            for (int l = 1; l <= Ligas; l++)
            {
                for (int n = 1; n <= RivalesPorLiga; n++)
                {
                    DatosRival datos = Datos(l, n);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(datos.Alias));
                    Assert.IsTrue(alias.Add(datos.Alias), "Alias repetido: " + datos.Alias);
                    Assert.IsNotNull(datos.Mecha);
                    VidaMecha vida = Cargar(l, n).GetComponent<VidaMecha>();
                    Assert.IsNotNull(vida);
                    Assert.AreSame(datos.Mecha, new SerializedObject(vida).FindProperty("datos").objectReferenceValue);
                }
            }
            Assert.AreEqual(20, alias.Count);
        }

        [Test]
        public void Prefabs_NingunoTienePiedadYSoloLosJefesTienenFuriaConConfig()
        {
            for (int l = 1; l <= Ligas; l++)
            {
                for (int n = 1; n <= RivalesPorLiga; n++)
                {
                    GameObject prefab = Cargar(l, n);
                    Assert.IsNull(prefab.GetComponent<PiedadRival>(), "PiedadRival en " + prefab.name);
                    FuriaJefe furia = prefab.GetComponent<FuriaJefe>();
                    if (n == RivalesPorLiga)
                    {
                        Assert.IsNotNull(furia, "Falta FuriaJefe en " + prefab.name);
                        Assert.IsNotNull(new SerializedObject(furia).FindProperty("config").objectReferenceValue);
                    }
                    else
                    {
                        Assert.IsNull(furia, "FuriaJefe de más en " + prefab.name);
                    }
                }
            }
        }

        [Test]
        public void Liga1Rival1_NoSuperaAlJugadorEnVidaNiEnDanio()
        {
            DatosMecha jugador = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaDatosJugador);
            Assert.IsNotNull(jugador);
            DatosMecha primero = Datos(1, 1).Mecha;
            Assert.LessOrEqual(primero.VidaMaxima, jugador.VidaMaxima);
            Assert.LessOrEqual(primero.DanioGolpe, jugador.DanioGolpe);
        }

        [Test]
        public void Ligas_PromedioDeVidaYDanioSubeEnCadaLiga()
        {
            float vidaAnterior = 0f;
            float danioAnterior = 0f;
            for (int l = 1; l <= Ligas; l++)
            {
                float vida = 0f;
                float danio = 0f;
                for (int n = 1; n <= RivalesPorLiga; n++)
                {
                    vida += Datos(l, n).Mecha.VidaMaxima;
                    danio += Datos(l, n).Mecha.DanioGolpe;
                }
                vida /= RivalesPorLiga;
                danio /= RivalesPorLiga;
                Assert.Greater(vida, vidaAnterior, "Vida promedio de la liga " + l);
                Assert.Greater(danio, danioAnterior, "Daño promedio de la liga " + l);
                vidaAnterior = vida;
                danioAnterior = danio;
            }
        }

        [Test]
        public void Ligas_ElJefeTieneLaVidaMasAltaYDanioMayorOIgualQueSusCompaneros()
        {
            for (int l = 1; l <= Ligas; l++)
            {
                DatosMecha jefe = Datos(l, RivalesPorLiga).Mecha;
                for (int n = 1; n < RivalesPorLiga; n++)
                {
                    DatosMecha otro = Datos(l, n).Mecha;
                    Assert.Greater(jefe.VidaMaxima, otro.VidaMaxima, "Vida del jefe de la liga " + l + " vs " + n);
                    Assert.GreaterOrEqual(jefe.DanioGolpe, otro.DanioGolpe, "Daño del jefe de la liga " + l + " vs " + n);
                }
            }
        }

        [Test]
        public void Rivales_DistanciaAtaqueNoPasaDelAlcanceDelGolpe()
        {
            for (int l = 1; l <= Ligas; l++)
            {
                for (int n = 1; n <= RivalesPorLiga; n++)
                {
                    DatosRival datos = Datos(l, n);
                    Assert.LessOrEqual(datos.DistanciaAtaque, datos.Mecha.AlcanceGolpe, datos.Alias);
                }
            }
        }
    }
}
