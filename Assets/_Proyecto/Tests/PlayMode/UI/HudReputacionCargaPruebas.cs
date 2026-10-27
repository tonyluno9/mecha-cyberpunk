using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>El aviso de reputación del HUD no salta al cargar una partida, pero sí con un cambio real (T-189).</summary>
    public class HudReputacionCargaPruebas : BaseHudObjetivo
    {
        private string carpeta;
        private string rutaAnterior;

        public override void Setup()
        {
            base.Setup();
            rutaAnterior = ServicioGuardado.RutaForzada;
            carpeta = Path.Combine(Application.temporaryCachePath, "hud_rep_carga_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
        }

        public override void TearDown()
        {
            base.TearDown();
            ServicioGuardado.RutaForzada = rutaAnterior;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        }

        [UnityTest]
        public IEnumerator Cargar_ConBarrioGuardado_NoMuestraAviso_PeroUnCambioRealSi()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            ServicioReputacion reputacion = (ServicioReputacion)Registro<IReputacion>.Actual;
            ServicioGuardado guardado = ServicioGuardado.Actual;
            Assert.IsNotNull(reputacion, "hay servicio de reputación");
            Assert.IsNotNull(guardado, "hay servicio de guardado");

            reputacion.Sumar(TipoReputacion.Barrio, 40);
            yield return null;
            Assert.IsTrue(guardado.Guardar());
            // Deja pasar el aviso del cambio real y devuelve el barrio a 0.
            reputacion.Sumar(TipoReputacion.Barrio, -40);
            yield return Esperar(config.SegundosAvisoReputacion + 0.3f);
            Assert.IsFalse(hud.GrupoAvisoReputacion.activeSelf, "el aviso anterior ya se apagó");

            Assert.IsTrue(guardado.Cargar());
            yield return null;
            yield return null;

            Assert.AreEqual(40, reputacion.Valor(TipoReputacion.Barrio), "la partida cargó el barrio");
            Assert.IsFalse(hud.GrupoAvisoReputacion.activeSelf, "cargar no es jugar: sin aviso");

            reputacion.Sumar(TipoReputacion.Barrio, 5);
            yield return null;
            Assert.IsTrue(hud.GrupoAvisoReputacion.activeSelf, "un cambio real sí avisa");
            StringAssert.Contains("+5", hud.TextoAvisoReputacion.text);
            StringAssert.DoesNotContain("45", hud.TextoAvisoReputacion.text);
        }
    }
}
