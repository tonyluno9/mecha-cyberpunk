using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Tests
{
    public class ValoresMechaPruebas
    {
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private DatosMecha creado;

        [TearDown]
        public void TearDown()
        {
            if (creado != null) Object.DestroyImmediate(creado);
            creado = null;
        }

        private static ValoresMecha ValoresJugador()
        {
            DatosMecha asset = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaJugador);
            Assert.IsNotNull(asset);
            return asset.Valores;
        }

        [Test]
        public void CrearEnEjecucion_ValoresDelJugador_IdaYVuelta()
        {
            ValoresMecha e = ValoresJugador();
            creado = DatosMecha.CrearEnEjecucion(e);
            ValoresMecha r = creado.Valores;
            Assert.AreEqual(e.nombre, r.nombre);
            Assert.AreEqual(e.vidaMaxima, r.vidaMaxima);
            Assert.AreEqual(e.velocidadMaxima, r.velocidadMaxima);
            Assert.AreEqual(e.aceleracion, r.aceleracion);
            Assert.AreEqual(e.desaceleracion, r.desaceleracion);
            Assert.AreEqual(e.velocidadGiroMaxima, r.velocidadGiroMaxima);
            Assert.AreEqual(e.danioGolpe, r.danioGolpe);
            Assert.AreEqual(e.alcanceGolpe, r.alcanceGolpe);
            Assert.AreEqual(e.preparacionGolpe, r.preparacionGolpe);
            Assert.AreEqual(e.recargaGolpe, r.recargaGolpe);
            Assert.AreEqual(e.tiempoSubirGuardia, r.tiempoSubirGuardia);
            Assert.AreEqual(e.reduccionBloqueo, r.reduccionBloqueo);
            Assert.AreEqual(e.ventanaBloqueoPerfecto, r.ventanaBloqueoPerfecto);
            Assert.AreEqual(e.nombre + " (ejecución)", creado.name);
        }

        [Test]
        public void CrearEnEjecucion_VidaCero_QuedaEnUno()
        {
            ValoresMecha v = ValoresJugador();
            v.vidaMaxima = 0f;
            creado = DatosMecha.CrearEnEjecucion(v);
            Assert.AreEqual(1f, creado.VidaMaxima);
        }

        [Test]
        public void CrearEnEjecucion_ReduccionFueraDeRango_QuedaEnElLimite()
        {
            ValoresMecha v = ValoresJugador();
            v.reduccionBloqueo = 1.5f;
            creado = DatosMecha.CrearEnEjecucion(v);
            Assert.AreEqual(1f, creado.ReduccionBloqueo);
            v.reduccionBloqueo = -2f;
            DatosMecha otro = DatosMecha.CrearEnEjecucion(v);
            try { Assert.AreEqual(0f, otro.ReduccionBloqueo); }
            finally { Object.DestroyImmediate(otro); }
        }

        [Test]
        public void CrearEnEjecucion_NaN_UsaValorPorDefecto()
        {
            DatosMecha porDefecto = ScriptableObject.CreateInstance<DatosMecha>();
            try
            {
                ValoresMecha v = ValoresJugador();
                v.danioGolpe = float.NaN;
                v.velocidadMaxima = float.NaN;
                creado = DatosMecha.CrearEnEjecucion(v);
                Assert.AreEqual(porDefecto.DanioGolpe, creado.DanioGolpe);
                Assert.AreEqual(porDefecto.VelocidadMaxima, creado.VelocidadMaxima);
            }
            finally { Object.DestroyImmediate(porDefecto); }
        }

        [Test]
        public void CrearEnEjecucion_Resultado_NoSeGuardaYNoEsAsset()
        {
            creado = DatosMecha.CrearEnEjecucion(ValoresJugador());
            Assert.AreEqual(HideFlags.DontSave, creado.hideFlags & HideFlags.DontSave);
            Assert.IsTrue(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(creado)));
        }
    }
}
