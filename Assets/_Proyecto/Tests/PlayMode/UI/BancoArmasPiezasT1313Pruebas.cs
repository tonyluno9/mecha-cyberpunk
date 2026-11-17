using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Armas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1313: el banco de Tino desarma el subfusil, el revólver, el lanzagranadas y el rifle de plasma: cada pieza se separa.</summary>
    public class BancoArmasPiezasT1313Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";

        private static IReadOnlyDictionary<PiezaArma, EstadoPieza> Estados()
        {
            return new Dictionary<PiezaArma, EstadoPieza>
            {
                { PiezaArma.Cargador, EstadoPieza.Disponible },
                { PiezaArma.Canon, EstadoPieza.Disponible },
                { PiezaArma.Mira, EstadoPieza.Disponible }
            };
        }

        [UnityTest]
        public IEnumerator Banco_ArmaNueva_SeDesarmaYCadaPiezaSeSepara([Values(ArmaAPie.Subfusil, ArmaAPie.Revolver, ArmaAPie.Lanzagranadas, ArmaAPie.RiflePlasma)] ArmaAPie arma)
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            EntradaArmaMenu entrada = null;
            foreach (EntradaArmaMenu e in config.Armas)
            {
                if (e.Arma == arma)
                {
                    entrada = e;
                }
            }
            Assert.IsNotNull(entrada);
            GameObject go = new GameObject("VistaPrueba");
            VistaHologramaArma vista = go.AddComponent<VistaHologramaArma>();
            vista.Mostrar(entrada, config);
            yield return null;
            Assert.IsTrue(vista.ConModelo);
            PiezaArma[] piezas = entrada.TieneMira ? new[] { PiezaArma.Cargador, PiezaArma.Canon, PiezaArma.Mira } : new[] { PiezaArma.Cargador, PiezaArma.Canon };
            foreach (PiezaArma pieza in piezas)
            {
                Assert.IsTrue(vista.TienePieza(pieza), arma + " tiene " + pieza);
                vista.CentroPieza(pieza, out Vector3 antes);
                vista.ElegirPieza(pieza, Estados());
                yield return new WaitForSecondsRealtime(0.9f);
                Assert.Greater(vista.Separacion(pieza), 0.9f, arma + ": " + pieza + " se separa");
                vista.CentroPieza(pieza, out Vector3 despues);
                Assert.Greater((despues - antes).magnitude, 0.005f, arma + ": " + pieza + " se mueve");
            }
            Assert.AreEqual(entrada.TieneMira, vista.TienePieza(PiezaArma.Mira));
            vista.Ocultar();
            Object.Destroy(go);
        }
    }
}
