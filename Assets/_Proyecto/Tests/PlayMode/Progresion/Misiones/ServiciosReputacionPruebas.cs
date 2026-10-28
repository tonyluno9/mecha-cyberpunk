using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion.Misiones
{
    /// <summary>ServicioReputacion y ServicioFavores (T-182) desde ServiciosProgresion.prefab, con ruta de guardado temporal.</summary>
    public class ServiciosReputacionPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";

        private string carpeta;
        private string rutaAnterior;
        private GameObject raiz;
        private ServicioReputacion reputacion;
        private ServicioFavores favores;
        private ServicioGuardado guardado;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            rutaAnterior = ServicioGuardado.RutaForzada;
            carpeta = Path.Combine(Application.temporaryCachePath, "reputacion_pruebas_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "existe ServiciosProgresion.prefab");
            raiz = Object.Instantiate(prefab);
            reputacion = raiz.GetComponent<ServicioReputacion>();
            favores = raiz.GetComponent<ServicioFavores>();
            guardado = raiz.GetComponent<ServicioGuardado>();
            Assert.IsNotNull(reputacion, "el prefab trae ServicioReputacion");
            Assert.IsNotNull(favores, "el prefab trae ServicioFavores");
            Assert.IsNotNull(guardado, "el prefab trae ServicioGuardado");
        }

        public override void TearDown()
        {
            if (raiz != null) Object.Destroy(raiz);
            ServicioGuardado.RutaForzada = rutaAnterior;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Prefab_RegistraReputacionYFavoresComoGuardables()
        {
            yield return null;
            Assert.AreSame(reputacion, Registro<IReputacion>.Actual);
            Assert.AreSame(favores, Registro<ServicioFavores>.Actual);
            bool hayReputacion = false, hayFavores = false;
            foreach (IGuardable g in Registro<IGuardable>.Todos)
            {
                if (g.ClaveGuardado == "reputacion") hayReputacion = true;
                if (g.ClaveGuardado == "favores") hayFavores = true;
            }
            Assert.IsTrue(hayReputacion);
            Assert.IsTrue(hayFavores);
            Assert.AreEqual(-100, Registro<IReputacion>.Actual.Minimo);
            Assert.AreEqual(100, Registro<IReputacion>.Actual.Maximo);
            Assert.AreEqual("DESCONOCIDO", Registro<IReputacion>.Actual.NombreNivel(TipoReputacion.Barrio));
            Assert.AreEqual(0.3f, favores.Datos(TipoFavor.Descuento).Valor, 0.0001f);
        }

        [UnityTest]
        public IEnumerator GuardarYCargar_ReputacionYFavores_SeConservan()
        {
            yield return null;
            reputacion.Sumar(TipoReputacion.Barrio, 55);
            reputacion.Sumar(TipoReputacion.Corporaciones, -30);
            favores.Dar(TipoFavor.Descuento);
            favores.Dar(TipoFavor.Escondite);
            favores.Dar(TipoFavor.Escondite);
            Assert.IsTrue(guardado.Guardar());

            reputacion.Sumar(TipoReputacion.Barrio, -100);
            reputacion.Sumar(TipoReputacion.Corporaciones, 100);
            favores.Gastar(TipoFavor.Descuento);
            favores.Gastar(TipoFavor.Escondite);
            int niveles = 0;
            reputacion.NivelCambio += (_, __) => niveles++;

            Assert.IsTrue(guardado.Cargar());

            Assert.AreEqual(55, reputacion.Valor(TipoReputacion.Barrio));
            Assert.AreEqual(-30, reputacion.Valor(TipoReputacion.Corporaciones));
            Assert.AreEqual(2, reputacion.Nivel(TipoReputacion.Barrio));
            Assert.AreEqual(-1, reputacion.Nivel(TipoReputacion.Corporaciones));
            Assert.AreEqual(0, niveles, "cargar no avisa NivelCambio");
            Assert.AreEqual(1, favores.Cantidad(TipoFavor.Descuento));
            Assert.AreEqual(2, favores.Cantidad(TipoFavor.Escondite));
            Assert.AreEqual(0, favores.Cantidad(TipoFavor.Informacion));
        }

        [UnityTest]
        public IEnumerator Calor_ConCorporacionesEnMenos50_SubeTrece()
        {
            yield return null;
            reputacion.Sumar(TipoReputacion.Corporaciones, -50);
            ICalor calor = Registro<ICalor>.Actual;
            calor.Sumar(10f, MotivoCalor.Otro);
            Assert.AreEqual(13f, calor.Valor, 0.01f);
        }

        [UnityTest]
        public IEnumerator Calor_ConCorporacionesEnMenos15_SubeOnceYMedio()
        {
            yield return null;
            reputacion.Sumar(TipoReputacion.Corporaciones, -15);
            ICalor calor = Registro<ICalor>.Actual;
            calor.Sumar(10f, MotivoCalor.Otro);
            Assert.AreEqual(11.5f, calor.Valor, 0.01f);
        }

        [UnityTest]
        public IEnumerator Calor_ConCorporacionesEnCeroOPositivas_SubeDiez()
        {
            yield return null;
            ICalor calor = Registro<ICalor>.Actual;
            calor.Sumar(10f, MotivoCalor.Otro);
            Assert.AreEqual(10f, calor.Valor, 0.01f);
            calor.Borrar();
            reputacion.Sumar(TipoReputacion.Corporaciones, 80);
            calor.Sumar(10f, MotivoCalor.Otro);
            Assert.AreEqual(10f, calor.Valor, 0.01f);
        }

        [UnityTest]
        public IEnumerator Calor_SinReputacionRegistrada_SubeComoAntes()
        {
            yield return null;
            reputacion.Sumar(TipoReputacion.Corporaciones, -80);
            reputacion.enabled = false;
            Assert.IsNull(Registro<IReputacion>.Actual);
            ICalor calor = Registro<ICalor>.Actual;
            calor.Sumar(10f, MotivoCalor.Otro);
            Assert.AreEqual(10f, calor.Valor, 0.01f);
        }

        [UnityTest]
        public IEnumerator Reaccion_AlSubirElBarrioA15_CambiaSuTexto()
        {
            var cuerpo = new GameObject("Reaccion");
            var reaccion = cuerpo.AddComponent<ReaccionReputacionDePrueba>();
            yield return null;
            Assert.AreEqual("", reaccion.Texto);

            reputacion.Sumar(TipoReputacion.Barrio, 15);
            StringAssert.StartsWith("GRAFITI: ", reaccion.Texto);

            reputacion.Sumar(TipoReputacion.Barrio, -15);
            Assert.AreEqual("", reaccion.Texto);
            reputacion.Sumar(TipoReputacion.Corporaciones, -15);
            StringAssert.StartsWith("ANUNCIO: ", reaccion.Texto);
            StringAssert.EndsWith(" ES UN PELIGRO", reaccion.Texto);
            Object.Destroy(cuerpo);
        }
    }
}
