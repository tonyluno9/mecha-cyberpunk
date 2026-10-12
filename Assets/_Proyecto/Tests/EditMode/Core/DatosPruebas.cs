using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    public class DatosPruebas
    {
        [Test]
        public void DatosMecha_RecienCreado_TodosLosValoresSonMayoresQueCero()
        {
            var datos = ScriptableObject.CreateInstance<DatosMecha>();
            try
            {
                Assert.Greater(datos.VidaMaxima, 0f);
                Assert.Greater(datos.VelocidadMaxima, 0f);
                Assert.Greater(datos.Aceleracion, 0f);
                Assert.Greater(datos.Desaceleracion, 0f);
                Assert.Greater(datos.VelocidadGiroMaxima, 0f);
                Assert.Greater(datos.DanioGolpe, 0f);
                Assert.Greater(datos.AlcanceGolpe, 0f);
                Assert.Greater(datos.PreparacionGolpe, 0f);
                Assert.Greater(datos.RecargaGolpe, 0f);
                Assert.Greater(datos.TiempoSubirGuardia, 0f);
                Assert.Greater(datos.ReduccionBloqueo, 0f);
                Assert.Greater(datos.VentanaBloqueoPerfecto, 0f);
                Assert.LessOrEqual(datos.ReduccionBloqueo, 1f);
                Assert.IsNotEmpty(datos.Nombre);
            }
            finally
            {
                Object.DestroyImmediate(datos);
            }
        }

        [Test]
        public void DatosRival_RecienCreado_TieneDistanciasYEsperasCoherentes()
        {
            var datos = ScriptableObject.CreateInstance<DatosRival>();
            try
            {
                Assert.Less(datos.DistanciaMinima, datos.DistanciaAtaque);
                Assert.LessOrEqual(datos.EsperaEntreAtaquesMin, datos.EsperaEntreAtaquesMax);
                Assert.Greater(datos.EsperaEntreAtaquesMin, 0f);
                Assert.IsNotEmpty(datos.Alias);
                Assert.IsNull(datos.Mecha);
            }
            finally
            {
                Object.DestroyImmediate(datos);
            }
        }
    }
}
