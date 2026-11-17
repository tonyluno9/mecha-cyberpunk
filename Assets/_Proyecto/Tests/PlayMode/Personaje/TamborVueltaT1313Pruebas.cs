using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1313: el tambor del revólver y del lanzagranadas da una vuelta completa al sacar el arma y vuelve a su reposo.</summary>
    public class TamborVueltaT1313Pruebas : BaseJugadorAPie
    {
        [UnityTest]
        public IEnumerator Tambor_AlSacarElArma_DaUnaVueltaCompletaYTerminaEnReposo([Values(ArmaAPie.Revolver, ArmaAPie.Lanzagranadas)] ArmaAPie tipo)
        {
            yield return Calentar();
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(tipo, 0), 1);
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            GameObject visual = armas.Visual(tipo);
            TamborArma t = visual.GetComponentsInChildren<TamborArma>(true).FirstOrDefault();
            Assert.IsNotNull(t, tipo + ": tiene TamborArma");
            Assert.IsTrue(armas.Equipar(tipo));
            if (tipo == ArmaAPie.Revolver)
            {
                Press(raton.rightButton); // la pose de pistola muestra el arma al apuntar
            }
            float maximo = 0f;
            float fin = Time.time + 2f;
            while (Time.time < fin)
            {
                maximo = Mathf.Max(maximo, t.Angulo);
                yield return null;
            }
            Release(raton.rightButton);
            Assert.Greater(maximo, 180f, tipo + ": el tambor giró más de media vuelta al sacar el arma");
            Assert.AreEqual(0f, t.Angulo, 0.5f, tipo + ": termina en su reposo tras la vuelta");
            Assert.AreEqual(0f, t.Objetivo, 0.5f, tipo + ": sin giro pendiente");
        }
    }
}
