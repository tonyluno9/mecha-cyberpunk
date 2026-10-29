using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// Clínica del mercado negro (T-321), lo que se comprueba sin regenerar el barrio: el asset de datos del lugar (IdLugar.Clinica, abre de día)
    /// y la geometría del cuarto contra los puestos y las pilas de cajas del interior de T-320 (medidas de CreadorMercadoNegroInterior).
    /// </summary>
    public class ClinicaPruebas
    {
        private const float MitadAncho = 5f;
        private const float Fondo = 10f;
        private const float XPuestoMostrador = 3.7f;
        private const float MitadAnchoMostradorPuesto = 0.55f;
        private const float MitadLargoMostradorPuesto = 1.2f;
        private const float ZCentroPuestoCercano = 3f;     // Puesto de la izquierda más cercano al fondo (5 - 2)
        private const float XPasilloIzquierdo = -1.2f;
        private const float AnchoMinimoVano = 0.9f;
        private const float HoraDia = 12f;
        private const float HoraNoche = 22f;
        private const float LibreAlrededorDelPunto = 0.5f;

        [Test]
        public void Datos_SonDeLaClinica_YAbrenDeDia()
        {
            DatosLugar datos = AssetDatabase.LoadAssetAtPath<DatosLugar>(GeometriaClinica.RutaDatos);
            Assert.IsNotNull(datos, "Falta DatosLugar_Clinica.asset");
            Assert.AreEqual(IdLugar.Clinica, datos.Id);
            Assert.IsFalse(datos.CerradoEnDemo);
            Assert.IsTrue(datos.Horario.EstaAbierto(HoraDia));
            Assert.IsFalse(datos.Horario.EstaAbierto(HoraNoche));
        }

        [Test]
        public void Cuarto_QuedaDentroDelInterior_YNoChocaConLosPuestos()
        {
            Assert.GreaterOrEqual(GeometriaClinica.XMin, -MitadAncho);
            Assert.Less(GeometriaClinica.XMax, XPasilloIzquierdo, "El cuarto no debe invadir el pasillo central");
            Assert.Less(GeometriaClinica.Profundidad + GeometriaClinica.GrosorTabique / 2f, Fondo);
            float inicioPuesto = ZCentroPuestoCercano - MitadLargoMostradorPuesto;
            Assert.Less(GeometriaClinica.Profundidad + GeometriaClinica.GrosorTabique / 2f, inicioPuesto, "El tabique no debe tocar el mostrador del puesto");
            // El tabique solo estorbaría al puesto si compartiera x; el puesto izquierdo ocupa -XPuesto ± mitad.
            Assert.Greater(-XPuestoMostrador + MitadAnchoMostradorPuesto, GeometriaClinica.XMin);
        }

        [Test]
        public void Vano_EsAnchoYEstaDentroDelTabique_Y_LaEntradaQuedaAfuera()
        {
            Assert.GreaterOrEqual(GeometriaClinica.VanoXMax - GeometriaClinica.VanoXMin, AnchoMinimoVano);
            Assert.GreaterOrEqual(GeometriaClinica.VanoXMin, GeometriaClinica.XMin);
            Assert.LessOrEqual(GeometriaClinica.VanoXMax, GeometriaClinica.XMax);
            Assert.GreaterOrEqual(GeometriaClinica.EntradaX, GeometriaClinica.VanoXMin);
            Assert.LessOrEqual(GeometriaClinica.EntradaX, GeometriaClinica.VanoXMax);
            Assert.Greater(GeometriaClinica.EntradaZ, GeometriaClinica.Profundidad, "La entrada va del lado del pasillo");
        }

        [Test]
        public void PuntoDelMostrador_EstaAdentro_Libre_YLejosDeLaCamilla()
        {
            Assert.Greater(GeometriaClinica.MostradorX, GeometriaClinica.XMin);
            Assert.Less(GeometriaClinica.MostradorX, GeometriaClinica.XMax);
            Assert.Greater(GeometriaClinica.MostradorZ, 0f);
            Assert.Less(GeometriaClinica.MostradorZ, GeometriaClinica.Profundidad - GeometriaClinica.GrosorTabique / 2f);
            float dx = Mathf.Max(GeometriaClinica.CamillaXMin - GeometriaClinica.MostradorX, 0f, GeometriaClinica.MostradorX - GeometriaClinica.CamillaXMax);
            float dz = Mathf.Max(GeometriaClinica.CamillaZMin - GeometriaClinica.MostradorZ, 0f, GeometriaClinica.MostradorZ - GeometriaClinica.CamillaZMax);
            Assert.GreaterOrEqual(Mathf.Sqrt(dx * dx + dz * dz), LibreAlrededorDelPunto, "El punto del mostrador debe quedar libre de la camilla");
        }

        [Test]
        public void Camilla_QuedaContraElFondo_DentroDelCuarto()
        {
            Assert.GreaterOrEqual(GeometriaClinica.CamillaXMin, GeometriaClinica.XMin);
            Assert.LessOrEqual(GeometriaClinica.CamillaXMax, GeometriaClinica.XMax);
            Assert.GreaterOrEqual(GeometriaClinica.CamillaZMin, 0f);
            Assert.LessOrEqual(GeometriaClinica.CamillaZMax, GeometriaClinica.Profundidad);
        }
    }
}
