using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ServicioImplantes (T-191): instalar, cobrar y guardar, con el catálogo real.</summary>
    public class ServicioImplantesGuardadoPruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/Implantes/CatalogoImplantes_Base.asset";
        private GameObject objeto;
        private ServicioImplantes servicio;
        private CatalogoImplantes catalogo;
        private ModeloCartera cartera;

        [SetUp]
        public void SetUp()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoImplantes>(RutaCatalogo);
            Assert.IsNotNull(catalogo, RutaCatalogo);
            objeto = new GameObject("ServicioImplantesDePrueba");
            objeto.SetActive(false);
            servicio = objeto.AddComponent<ServicioImplantes>();
            var so = new SerializedObject(servicio);
            so.FindProperty("catalogo").objectReferenceValue = catalogo;
            so.ApplyModifiedPropertiesWithoutUndo();
            cartera = new ModeloCartera(10000);
            Registro<ICartera>.Agregar(cartera);
        }

        [TearDown]
        public void TearDown()
        {
            Registro<ICartera>.Quitar(cartera);
            Object.DestroyImmediate(objeto);
        }

        private DatosImplante Dato(string id) => catalogo.Buscar(id);

        [Test]
        public void Instalar_ConDinero_CobraYAvisaInstaladoYCambio()
        {
            int instalados = 0, cambios = 0;
            servicio.Instalado += (e, n) => instalados++;
            servicio.Cambio += () => cambios++;

            ResultadoImplante r = servicio.Instalar(Dato("implante_brazos_1"));

            Assert.AreEqual(ResultadoImplante.Instalado, r);
            Assert.AreEqual(10000 - 2000, cartera.Dinero);
            Assert.AreEqual(1, servicio.Nivel(EspacioImplante.Brazos));
            Assert.AreEqual("BRAZOS DE PISTÓN", servicio.Nombre(EspacioImplante.Brazos));
            Assert.AreEqual(1, servicio.Cantidad);
            Assert.AreEqual(1, instalados);
            Assert.AreEqual(1, cambios);
        }

        [Test]
        public void Instalar_Nivel2ConNivel1_CobraLaDiferencia()
        {
            var ligas = new LigasFalsas { LigaActual = 3 };
            Registro<IProgresoLigas>.Agregar(ligas);
            try
            {
                servicio.Instalar(Dato("implante_brazos_1"));
                int antes = cartera.Dinero;
                Assert.AreEqual(ResultadoImplante.Instalado, servicio.Instalar(Dato("implante_brazos_2")));
                Assert.AreEqual(3000, antes - cartera.Dinero);
                Assert.AreEqual(2, servicio.Nivel(EspacioImplante.Brazos));
                Assert.AreEqual(ResultadoImplante.YaInstalado, servicio.Instalar(Dato("implante_brazos_2")));
                Assert.AreEqual(ResultadoImplante.NoMejora, servicio.Instalar(Dato("implante_brazos_1")));
            }
            finally
            {
                Registro<IProgresoLigas>.Quitar(ligas);
            }
        }

        private class LigasFalsas : IProgresoLigas
        {
            public int LigaActual { get; set; }
            public int CantidadLigas => 4;
            public int CampeonesVencidos => 0;
            public string NombreLiga(int liga) => "L" + liga;
            public event System.Action Cambio { add { } remove { } }
            public event System.Action<int> CampeonVencido { add { } remove { } }
        }

        [Test]
        public void Instalar_SinDineroOConDeuda_NoInstalaNiCobra()
        {
            var pobre = new ModeloCartera(100);
            Registro<ICartera>.Quitar(cartera);
            Registro<ICartera>.Agregar(pobre);
            try
            {
                Assert.AreEqual(ResultadoImplante.SinDinero, servicio.Instalar(Dato("implante_brazos_1")));
                Assert.AreEqual(100, pobre.Dinero);
                pobre.CargarDeuda(500, MotivoDinero.Otro);
                Assert.AreEqual(ResultadoImplante.SinDinero, servicio.Instalar(Dato("implante_brazos_1")));
                Assert.AreEqual(0, servicio.Cantidad);
            }
            finally
            {
                Registro<ICartera>.Quitar(pobre);
                Registro<ICartera>.Agregar(cartera);
            }
        }

        [Test]
        public void Instalar_SinLigaSuficiente_EsLigaBaja()
        {
            // Sin servicio de ligas la liga es 1 y el nivel 2 abre en la 3.
            Assert.AreEqual(ResultadoImplante.LigaBaja, servicio.Instalar(Dato("implante_brazos_2")));
            Assert.AreEqual(10000, cartera.Dinero);
        }

        [Test]
        public void Instalar_SinCartera_AvisaUnaVezYNoInstala()
        {
            Registro<ICartera>.Quitar(cartera);
            try
            {
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no hay ICartera"));
                Assert.AreEqual(ResultadoImplante.SinDinero, servicio.Instalar(Dato("implante_brazos_1")));
                Assert.AreEqual(ResultadoImplante.SinDinero, servicio.Instalar(Dato("implante_brazos_1")));
                Assert.AreEqual(0, servicio.Cantidad);
            }
            finally
            {
                Registro<ICartera>.Agregar(cartera);
            }
        }

        [Test]
        public void GuardarYCargar_ConservaLoInstalado_ConCambioPeroSinInstalado()
        {
            servicio.Instalar(Dato("implante_ojos_1"));
            servicio.Instalar(Dato("implante_brazos_1"));
            string texto = servicio.Guardar();

            var otroObjeto = new GameObject("Otro");
            otroObjeto.SetActive(false);
            try
            {
                ServicioImplantes otro = otroObjeto.AddComponent<ServicioImplantes>();
                var so = new SerializedObject(otro);
                so.FindProperty("catalogo").objectReferenceValue = catalogo;
                so.ApplyModifiedPropertiesWithoutUndo();
                int instalados = 0, cambios = 0;
                otro.Instalado += (e, n) => instalados++;
                otro.Cambio += () => cambios++;

                otro.Cargar(texto);

                Assert.AreEqual(1, otro.Nivel(EspacioImplante.Ojos));
                Assert.AreEqual(1, otro.Nivel(EspacioImplante.Brazos));
                Assert.AreEqual(0, otro.Nivel(EspacioImplante.Piernas));
                Assert.AreEqual(2, otro.Cantidad);
                Assert.AreEqual(0, instalados);
                Assert.AreEqual(1, cambios);
                Assert.AreEqual(servicio.Efectos.multiplicadorGolpeAPie, otro.Efectos.multiplicadorGolpeAPie);
            }
            finally
            {
                Object.DestroyImmediate(otroObjeto);
            }
        }

        [Test]
        public void Cargar_TextoRoto_NoCambiaNadaYAvisa()
        {
            servicio.Instalar(Dato("implante_ojos_1"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no entendieron"));
            servicio.Cargar("{esto no es json");
            Assert.AreEqual(1, servicio.Nivel(EspacioImplante.Ojos));
        }

        [Test]
        public void Cargar_SinLaClave_QuedaSinImplantes()
        {
            servicio.Instalar(Dato("implante_ojos_1"));
            servicio.Cargar("{}");
            Assert.AreEqual(0, servicio.Cantidad);
        }

        [Test]
        public void Cargar_IdDesconocido_SeIgnoraConWarning()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("desconocido"));
            servicio.Cargar("{\"ids\":[\"implante_ojos_1\",\"implante_alas_9\"]}");
            Assert.AreEqual(1, servicio.Cantidad);
            Assert.AreEqual(1, servicio.Nivel(EspacioImplante.Ojos));
        }

        [Test]
        public void Opcion_ReflejaLaCarteraYElCatalogo()
        {
            OfertaImplante o = servicio.Opcion(EspacioImplante.Ojos);
            Assert.AreEqual(EstadoOfertaImplante.Instalar, o.Estado);
            Assert.AreEqual(2500, o.Costo);
            servicio.QuitarTodos();
            Assert.AreEqual(0, servicio.Cantidad);
        }
    }
}
