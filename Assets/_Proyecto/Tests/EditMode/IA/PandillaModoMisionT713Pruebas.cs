using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-713: el modo misión de un grupo de pandilleros (salen todos a la vez y no vuelven) empieza apagado y no cuenta vencidos sin pandilleros.</summary>
    public class PandillaModoMisionT713Pruebas
    {
        [Test]
        public void ModoMision_EmpiezaApagado_YSePuedeEncender()
        {
            GameObject go = new GameObject("GrupoPrueba");
            try
            {
                PandillaAPie grupo = go.AddComponent<PandillaAPie>();
                Assert.IsFalse(grupo.ModoMision, "Los grupos normales siguen con su ciclo de día y noche");
                grupo.ModoMision = true;
                Assert.IsTrue(grupo.ModoMision);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SinPandilleros_NadaEstaVencido_NiTodosVencidos()
        {
            GameObject go = new GameObject("GrupoPrueba");
            try
            {
                PandillaAPie grupo = go.AddComponent<PandillaAPie>();
                Assert.AreEqual(0, grupo.Vencidos);
                Assert.IsFalse(grupo.TodosVencidos);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
