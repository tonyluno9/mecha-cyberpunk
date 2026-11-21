using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1544: casas generadas como caja cambiadas por modelos reales. Las pruebas de cálculo son puras; las de escena piden haber corrido
    /// CasasT1544.Aplicar (que guarda Ciudad_Prologo).
    /// </summary>
    public class CasasT1544Pruebas
    {
        private const float ToleranciaHuella = 0.1f;
        private const float ToleranciaPiso = 0.05f;

        // ---------------- Cálculo puro ----------------

        [TestCase("S2c_Mercado/Casas/Casa_3/Cuerpo", "Tianguis")]
        [TestCase("S2c_CalleDorada/Casas/Casa_3/Cuerpo", "Tianguis")]
        [TestCase("S2c_Suburbios/CalleOriente/Casa_3/Cuerpo", "Suburbios")]
        [TestCase("T1102_CalleDoc/Casas/Fachada_O_01_2p_c3/Cuerpo", "Suburbios")]
        [TestCase("S2o_CalleK01/Fondo/Casa_2/Cuerpo", "Suburbios")]
        [TestCase("S2c_Nova/Frente_Nova/Edificio_0/Cuerpo", "NeonAlto")]
        [TestCase("Otra/Cosa", "")]
        public void BarrioDe_RutaDeCasa_DaLaPaletaDelBarrio(string ruta, string esperado)
        {
            Assert.AreEqual(esperado, CalculoCasasT1544.BarrioDe(ruta));
        }

        [Test]
        public void BarrioDe_ConCadaBarrioDeCasas_ExisteUnaPaletaED()
        {
            foreach (string barrio in new[] { "Tianguis", "Suburbios", "NeonAlto" })
            {
                Assert.Greater(CalculoEdificiosT1512.NombresPara(barrio).Length, 0, barrio);
            }
        }

        [Test]
        public void EsTorre_SoloLasTorresDelSkyline()
        {
            Assert.IsTrue(CalculoCasasT1544.EsTorre("T501_Suburbios/Horizonte/Torre_4/Cuerpo"));
            Assert.IsTrue(CalculoCasasT1544.EsTorre("T505_NeonAlto/Torres/Torre_0/Cuerpo"));
            Assert.IsFalse(CalculoCasasT1544.EsTorre("S2c_Mercado/Casas/Casa_3/Cuerpo"));
        }

        [Test]
        public void EsCuerpoDeCasa_CuerpoGeneradoDeCasa_EsCasa()
        {
            Assert.IsTrue(CalculoCasasT1544.EsCuerpoDeCasa("S2c_Mercado/Casas/Casa_3/Cuerpo", "Cuerpo", "S2c_Mercado_Cuerpo_12", "S2c_Cuerpo_Crema", new Vector3(5f, 6.3f, 8f)));
            Assert.IsTrue(CalculoCasasT1544.EsCuerpoDeCasa("S2c_Nova/Frente_Nova/Edificio_0/Cuerpo", "Cuerpo", "S2c_Nova_Cuerpo_3", "S2p_NovaCuerpo_Nitida", new Vector3(12f, 21f, 14f)));
        }

        [Test]
        public void EsCuerpoDeCasa_TorreAdornoOChico_NoEsCasa()
        {
            Assert.IsFalse(CalculoCasasT1544.EsCuerpoDeCasa("T501_Suburbios/Horizonte/Torre_4/Cuerpo", "Cuerpo", "x_Cuerpo_1", "S2c_Cuerpo_Crema", new Vector3(8f, 30f, 8f)));
            Assert.IsFalse(CalculoCasasT1544.EsCuerpoDeCasa("T930_Casino/Tragamonedas_Adorno/Cuerpo", "Cuerpo", "x_Cuerpo_1", "Metal", new Vector3(1f, 2f, 1f)));
            Assert.IsFalse(CalculoCasasT1544.EsCuerpoDeCasa("S2c_Mercado/Casas/Casa_3/Cuerpo", "Cuerpo", "S2c_Mercado_Cuerpo_12", "S2c_Cuerpo_Crema", new Vector3(5f, 1f, 8f)));
            Assert.IsFalse(CalculoCasasT1544.EsCuerpoDeCasa("S2c_Mercado/Calle/Banqueta", "Banqueta", "S2c_Mercado_Banqueta_2", "S2c_Banqueta", new Vector3(74f, 0.25f, 2.3f)));
        }

        [Test]
        public void EsTipoLugar_ComponentesDeLugar_SiYDecoracion_No()
        {
            Assert.IsTrue(CalculoCasasT1544.EsTipoLugar("NegocioPunto"));
            Assert.IsTrue(CalculoCasasT1544.EsTipoLugar("MarcaLugar"));
            Assert.IsTrue(CalculoCasasT1544.EsTipoLugar("PuertaAutomatica"));
            Assert.IsFalse(CalculoCasasT1544.EsTipoLugar("LuzNocturna"));
            Assert.IsFalse(CalculoCasasT1544.EsTipoLugar("PuntoTrepar"));
            Assert.IsFalse(CalculoCasasT1544.EsTipoLugar(null));
        }

        [Test]
        public void EsNombreLugar_MarcasDeLugar_SiYAccesosDeTecho_No()
        {
            Assert.IsTrue(CalculoCasasT1544.EsNombreLugar("Marca_Casa"));
            Assert.IsTrue(CalculoCasasT1544.EsNombreLugar("Negocio_tiendita"));
            Assert.IsTrue(CalculoCasasT1544.EsNombreLugar("Lugar_TallerMecanico"));
            Assert.IsFalse(CalculoCasasT1544.EsNombreLugar("Acceso_SNc_Suburbios_3"));
            Assert.IsFalse(CalculoCasasT1544.EsNombreLugar("PU01_Puerta"));
            Assert.IsFalse(CalculoCasasT1544.EsNombreLugar(""));
        }

        [Test]
        public void Cobertura_IntervalosSolapadosYFueraDelFrente_SeRecortanYSeUnen()
        {
            var intervalos = new List<Vector2> { new Vector2(-2.5f, 0f), new Vector2(-1f, 1f), new Vector2(4f, 9f) };
            // Frente de 5 m: [-2.5, 2.5]. Unión dentro: [-2.5, 1] = 3.5 m.
            Assert.AreEqual(0.7f, CalculoCasasT1544.Cobertura(intervalos, 5f), 0.001f);
            Assert.AreEqual(0f, CalculoCasasT1544.Cobertura(new List<Vector2>(), 5f));
            Assert.AreEqual(1f, CalculoCasasT1544.Cobertura(new List<Vector2> { new Vector2(-9f, 9f) }, 5f), 0.001f);
        }

        [Test]
        public void Receso_KitQueTapaElFrente_MeteElModeloDetras()
        {
            Assert.AreEqual(CalculoCasasT1544.RecesoConKit, CalculoCasasT1544.Receso(0.9f));
            Assert.AreEqual(CalculoCasasT1544.RecesoSinKit, CalculoCasasT1544.Receso(0.1f));
            Assert.Greater(CalculoCasasT1544.RecesoConKit, CalculoCasasT1544.RecesoSinKit);
        }

        [Test]
        public void Ajustar_ModeloParecidoALaCaja_UnaCopiaSinEstirar()
        {
            int tramos, pilas;
            Vector3 esc;
            float costo;
            Assert.IsTrue(CalculoCasasT1544.Ajustar(new Vector3(5f, 6f, 8f), 5f, 8f, 6.3f, CalculoCasasT1544.Estricto, out tramos, out pilas, out esc, out costo));
            Assert.AreEqual(1, tramos);
            Assert.AreEqual(1, pilas);
            Assert.AreEqual(1f, esc.x, 0.001f);
            Assert.AreEqual(1.05f, esc.y, 0.001f);
            Assert.AreEqual(1f, esc.z, 0.001f);
        }

        [Test]
        public void Ajustar_EdificioMuyAlto_ApilaCopias()
        {
            int tramos, pilas;
            Vector3 esc;
            float costo;
            Assert.IsTrue(CalculoCasasT1544.Ajustar(new Vector3(12f, 8f, 14f), 12f, 14f, 36f, CalculoCasasT1544.Estricto, out tramos, out pilas, out esc, out costo));
            Assert.GreaterOrEqual(pilas, 2);
            Assert.AreEqual(36f, esc.y * pilas * 8f, 0.01f);
        }

        [Test]
        public void Ajustar_ModeloQueNoCabe_Falla()
        {
            int tramos, pilas;
            Vector3 esc;
            float costo;
            Assert.IsFalse(CalculoCasasT1544.Ajustar(new Vector3(40f, 3f, 2f), 5f, 8f, 6.3f, CalculoCasasT1544.Estricto, out tramos, out pilas, out esc, out costo));
            Assert.IsFalse(CalculoCasasT1544.Ajustar(Vector3.zero, 5f, 8f, 6.3f, CalculoCasasT1544.Estricto, out tramos, out pilas, out esc, out costo));
        }

        [Test]
        public void AjustarPanel_AplastaElFondoAlGrosorYNoCuentaEnElEstiramiento()
        {
            int tramos, pilas;
            Vector3 esc;
            float costo;
            Assert.IsTrue(CalculoCasasT1544.AjustarPanel(new Vector3(6f, 6f, 9f), 6f, 6f, 0.3f, CalculoCasasT1544.Estricto, out tramos, out pilas, out esc, out costo));
            Assert.AreEqual(0.3f / 9f, esc.z, 0.0001f);
            Assert.AreEqual(1f, esc.x, 0.001f);
            Assert.AreEqual(1f, esc.y, 0.001f);
        }

        [Test]
        public void FraccionDentro_PiezaDentroFueraYAMedias()
        {
            Bounds modelo = new Bounds(Vector3.zero, new Vector3(10f, 10f, 10f));
            Assert.AreEqual(1f, CalculoCasasT1544.FraccionDentro(new Bounds(Vector3.zero, Vector3.one), modelo), 0.001f);
            Assert.AreEqual(0f, CalculoCasasT1544.FraccionDentro(new Bounds(new Vector3(20f, 0f, 0f), Vector3.one), modelo), 0.001f);
            Assert.AreEqual(0.5f, CalculoCasasT1544.FraccionDentro(new Bounds(new Vector3(5f, 0f, 0f), new Vector3(2f, 2f, 2f)), modelo), 0.001f);
        }

        [Test]
        public void DentroDeCaja_PuntoConMargen()
        {
            Vector3 tam = new Vector3(4f, 6f, 8f);
            Assert.IsTrue(CalculoCasasT1544.DentroDeCaja(new Vector3(1f, 1f, 1f), tam, 0f));
            Assert.IsFalse(CalculoCasasT1544.DentroDeCaja(new Vector3(3f, 0f, 0f), tam, 0f));
            Assert.IsTrue(CalculoCasasT1544.DentroDeCaja(new Vector3(3f, 0f, 0f), tam, 1.5f));
        }

        // ---------------- Escena (después de CasasT1544.Aplicar) ----------------

        private static Scene Abrir()
        {
            return EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Additive);
        }

        private static Transform CuerpoDe(Transform contenedor, string prefijo)
        {
            int idx;
            if (contenedor.parent == null || !int.TryParse(contenedor.name.Substring(prefijo.Length), out idx) || idx >= contenedor.parent.childCount)
            {
                return null;
            }
            return contenedor.parent.GetChild(idx);
        }

        private static List<Transform> Contenedores(Scene escena, string prefijo)
        {
            List<Transform> lista = new List<Transform>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith(prefijo))
                    {
                        lista.Add(t);
                    }
                }
            }
            return lista;
        }

        private static Bounds CajaDe(Transform raiz)
        {
            Renderer[] rs = raiz.GetComponentsInChildren<Renderer>(false);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        [Test]
        public void Escena_NingunaCasaNormalConCajaVisible()
        {
            Scene escena = Abrir();
            try
            {
                List<CasasT1544.Caja> visibles = CasasT1544.Escanear(escena)
                    .Where(c => c.Categoria == CasasT1544.Categoria.Casa && c.Lugar == null).ToList();
                Assert.AreEqual(0, visibles.Count, "Casas con caja generada visible (corre CasasT1544.Aplicar): " + string.Join(", ", visibles.Take(5).Select(c => c.Ruta)));
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_HayCasasReemplazadasYLugaresCubiertos()
        {
            Scene escena = Abrir();
            try
            {
                Assert.Greater(Contenedores(escena, CasasT1544.PrefijoModelo).Count, 100, "Pocas casas con modelo T-1544");
                Assert.Greater(Contenedores(escena, CasasT1544.PrefijoPanel).Count, 0, "Ningún lugar con paneles T-1544");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_LugaresIntactos_CajaPrendidaConColliderYSoloPaneles()
        {
            Scene escena = Abrir();
            try
            {
                List<CasasT1544.Caja> lugares = CasasT1544.Escanear(escena).Where(c => c.Categoria == CasasT1544.Categoria.Casa && c.Lugar != null).ToList();
                Assert.Greater(lugares.Count, 0, "No se encontró ningún lugar del juego entre las casas");
                foreach (CasasT1544.Caja c in lugares)
                {
                    Assert.IsTrue(c.Renderer.enabled, c.Ruta + ": la caja de un lugar no se toca");
                    Collider col = c.Cuerpo.GetComponent<Collider>();
                    Assert.IsTrue(col != null && col.enabled, c.Ruta + ": el collider de un lugar no se toca");
                    Transform padre = c.Cuerpo.parent;
                    foreach (Transform h in padre.Cast<Transform>())
                    {
                        Assert.IsFalse(h.name.StartsWith(CasasT1544.PrefijoModelo), c.Ruta + ": un lugar no lleva un modelo que reemplace el cuerpo");
                    }
                    Assert.IsNotNull(padre.Cast<Transform>().FirstOrDefault(h => h.name == CasasT1544.PrefijoPanel + c.Cuerpo.GetSiblingIndex()),
                        c.Ruta + ": el lugar no tiene sus paneles de lados y trasera");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_CasasConModelo_AsentadasEnSuHuellaYConColliderDeLaCaja()
        {
            Scene escena = Abrir();
            try
            {
                foreach (Transform contenedor in Contenedores(escena, CasasT1544.PrefijoModelo))
                {
                    Transform cuerpo = CuerpoDe(contenedor, CasasT1544.PrefijoModelo);
                    Assert.IsNotNull(cuerpo, contenedor.name + ": no se encontró su cuerpo");
                    Collider col = cuerpo.GetComponent<Collider>();
                    Assert.IsTrue(col != null && col.enabled, cuerpo.name + ": el collider de la caja debe quedarse (techo y escaleras)");
                    MeshRenderer mr = cuerpo.GetComponent<MeshRenderer>();
                    Assert.IsTrue(mr != null && !mr.enabled, cuerpo.name + ": la caja generada debe estar apagada");
                    Assert.Greater(contenedor.GetComponentsInChildren<Renderer>(false).Length, 0, contenedor.name + " sin modelo visible");

                    Bounds modelo = CajaDe(contenedor);
                    Bounds caja = col.bounds;
                    // Nada sobre la banqueta: el modelo no se sale de la huella de la caja.
                    Assert.LessOrEqual(modelo.max.x, caja.max.x + ToleranciaHuella, contenedor.name + " se sale de la huella (+x)");
                    Assert.GreaterOrEqual(modelo.min.x, caja.min.x - ToleranciaHuella, contenedor.name + " se sale de la huella (-x)");
                    Assert.LessOrEqual(modelo.max.z, caja.max.z + ToleranciaHuella, contenedor.name + " se sale de la huella (+z)");
                    Assert.GreaterOrEqual(modelo.min.z, caja.min.z - ToleranciaHuella, contenedor.name + " se sale de la huella (-z)");
                    // Asentado: la base llega al piso de la caja o más abajo (hasta el hueco máximo), nunca flota.
                    Assert.LessOrEqual(modelo.min.y, caja.min.y + ToleranciaPiso, contenedor.name + " flota sobre el piso");
                    Assert.GreaterOrEqual(modelo.min.y, caja.min.y - CalculoCasasT1544.HuecoMaximoPiso - ToleranciaPiso, contenedor.name + " baja más de lo permitido");
                    Assert.AreEqual(caja.max.y, modelo.max.y, 0.2f, contenedor.name + " no llega a la altura de la caja");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
