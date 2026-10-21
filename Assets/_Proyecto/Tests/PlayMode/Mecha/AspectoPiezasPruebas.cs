using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Montaje;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>
    /// T-170: lo que compraste se ve. Prefab real MechaEstacionado.prefab (arranque Directo, sin encendido) con el EquipoMecha real:
    /// los brazos de la cabina toman el material de su rama y encienden tantas franjas como su nivel, el marco toma el de la cabina
    /// y el cuerpo exterior el del torso. Solo se compara sharedMaterial: ninguna prueba modifica un material.
    /// </summary>
    public class AspectoPiezasPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaCatalogo = "Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset";
        private const string RutaConfig = "Assets/_Proyecto/Mecha/Datos/ConfigAspectoPiezas_Base.asset";
        private const string RutaCabina = "Sacudida/Cabina/";
        private const int NumeroFranjas = 4;

        private GameObject mecha;
        private EquipoMecha equipo;
        private CatalogoPiezas catalogo;
        private ConfigAspectoPiezas config;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            Registros.LimpiarTodos();
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>(RutaCatalogo);
            config = AssetDatabase.LoadAssetAtPath<ConfigAspectoPiezas>(RutaConfig);
            Assert.IsNotNull(catalogo, "Existe CatalogoPiezas_Base");
            Assert.IsNotNull(config, "Existe ConfigAspectoPiezas_Base");
            Assert.IsTrue(config.EstaCompleta, "La config trae todos los materiales");
        }

        public override void TearDown()
        {
            if (mecha != null)
            {
                Object.DestroyImmediate(mecha);
                mecha = null;
            }
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private IEnumerator CrearMecha()
        {
            var padre = new GameObject("PadreApagado");
            padre.SetActive(false);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            mecha = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity, padre.transform);
            var so = new SerializedObject(mecha.GetComponent<MontajeMecha>());
            so.FindProperty("arranque").enumValueIndex = (int)ArranqueMontaje.Directo;
            so.ApplyModifiedPropertiesWithoutUndo();
            mecha.transform.SetParent(null);
            Object.Destroy(padre);
            equipo = mecha.GetComponent<EquipoMecha>();
            Assert.IsNotNull(equipo, "MechaEstacionado lleva EquipoMecha");
            Assert.IsNotNull(mecha.GetComponent<AspectoPiezas>(), "MechaEstacionado lleva AspectoPiezas");
            yield return null;
        }

        private DatosPieza Pieza(string id)
        {
            DatosPieza p = catalogo.Buscar(id);
            Assert.IsNotNull(p, "Existe la pieza " + id);
            return p;
        }

        private Renderer Parte(string ruta)
        {
            Transform t = mecha.transform.Find(ruta);
            Assert.IsNotNull(t, "Existe " + ruta);
            return t.GetComponent<Renderer>();
        }

        private int FranjasEncendidas(string brazo)
        {
            int n = 0;
            for (int i = 1; i <= NumeroFranjas; i++)
            {
                if (Parte(RutaCabina + brazo + "/Antebrazo/Franja" + i).enabled)
                {
                    n++;
                }
            }
            return n;
        }

        private void AfirmarBrazo(string brazo, Material esperado, int franjas)
        {
            foreach (string parte in new[] { "Brazo", "Antebrazo", "Puno" })
            {
                Assert.AreSame(esperado, Parte(RutaCabina + brazo + "/" + parte).sharedMaterial, brazo + "/" + parte);
            }
            Assert.AreEqual(franjas, FranjasEncendidas(brazo), "Franjas encendidas de " + brazo);
        }

        [UnityTest]
        public IEnumerator Equipar_BrazosDeDosRamas_CadaUnoConSuMaterialYSusFranjas()
        {
            yield return CrearMecha();
            equipo.Equipar(Pieza("pieza_brazoizq_fuego_2"));
            equipo.Equipar(Pieza("pieza_brazoder_militar_4"));
            yield return null;

            AfirmarBrazo("BrazoIzquierdo", config.DeRama(RamaPieza.Fuego), 2);
            AfirmarBrazo("BrazoDerecho", config.DeRama(RamaPieza.Militar), 4);
            Assert.AreNotSame(config.DeRama(RamaPieza.Fuego), config.DeRama(RamaPieza.Militar));
            Assert.AreSame(config.Franja, Parte(RutaCabina + "BrazoIzquierdo/Antebrazo/Franja1").sharedMaterial, "Las franjas usan el material de franja");
        }

        [UnityTest]
        public IEnumerator Equipar_TorsoTech_ElCuerpoExteriorTomaElMaterialTech()
        {
            yield return CrearMecha();
            equipo.Equipar(Pieza("pieza_torso_tech_1"));
            yield return null;

            Assert.AreSame(config.DeRama(RamaPieza.Tech), Parte("Cuerpo").sharedMaterial);
        }

        [UnityTest]
        public IEnumerator Equipar_Cabina_ElMarcoTomaElColorDeLaPieza()
        {
            yield return CrearMecha();
            equipo.Equipar(Pieza("pieza_cabina_militar_1"));
            yield return null;

            foreach (string parte in new[] { "PosteIzquierdo", "PosteDerecho", "VigaSuperior" })
            {
                Assert.AreSame(config.DeRama(RamaPieza.Militar), Parte(RutaCabina + parte).sharedMaterial, parte);
            }
        }

        [UnityTest]
        public IEnumerator PonerDeSerie_DespuesDeComprar_TodoVuelveAlGrisSinFranjas()
        {
            yield return CrearMecha();
            equipo.Equipar(Pieza("pieza_brazoizq_fuego_2"));
            equipo.Equipar(Pieza("pieza_brazoder_militar_4"));
            equipo.Equipar(Pieza("pieza_torso_tech_1"));
            equipo.Equipar(Pieza("pieza_cabina_militar_1"));
            yield return null;

            equipo.PonerDeSerie();
            yield return null;

            AfirmarBrazo("BrazoIzquierdo", config.DeRama(RamaPieza.DeSerie), 0);
            AfirmarBrazo("BrazoDerecho", config.DeRama(RamaPieza.DeSerie), 0);
            Assert.AreSame(config.DeRama(RamaPieza.DeSerie), Parte("Cuerpo").sharedMaterial);
            Assert.AreSame(config.MarcoDeRama(RamaPieza.DeSerie), Parte(RutaCabina + "PosteIzquierdo").sharedMaterial, "El marco vuelve al oscuro de siempre");
        }

        [UnityTest]
        public IEnumerator Aplicar_AlEmpezarDeSerie_BrazosGrisesYSinFranjas()
        {
            yield return CrearMecha();
            AfirmarBrazo("BrazoIzquierdo", config.DeRama(RamaPieza.DeSerie), 0);
            AfirmarBrazo("BrazoDerecho", config.DeRama(RamaPieza.DeSerie), 0);
        }

        [UnityTest]
        public IEnumerator Equipar_NoModificaNingunMaterialDeLosAssets()
        {
            yield return CrearMecha();
            Color antes = config.DeRama(RamaPieza.Fuego).GetColor("_BaseColor");

            equipo.Equipar(Pieza("pieza_brazoizq_fuego_2"));
            yield return null;
            equipo.PonerDeSerie();
            yield return null;

            Assert.AreEqual(antes, config.DeRama(RamaPieza.Fuego).GetColor("_BaseColor"));
            Assert.IsFalse(EditorUtility.IsDirty(config.DeRama(RamaPieza.Fuego)), "El material de fuego no quedó sucio");
        }
    }
}
