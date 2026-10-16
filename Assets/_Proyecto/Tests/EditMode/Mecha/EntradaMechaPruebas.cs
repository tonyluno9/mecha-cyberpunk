using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>Las acciones de ControlesMecha conservan sus id: agregar acciones nuevas no rompe las referencias del prefab.</summary>
    public class EntradaMechaPruebas
    {
        private const string RutaEntrada = "Assets/_Proyecto/Mecha/Entrada/ControlesMecha.inputactions";
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";

        private static readonly string[][] IdsOriginales =
        {
            new[] { "Mover", "d0c1f605-710a-494d-ad92-d059afa8fb20" },
            new[] { "Mirar", "d999e6f0-c925-429b-993c-fcc705e4c773" },
            new[] { "Golpear", "4aa8c1a5-6c59-4e77-8d1f-562f110e0430" },
            new[] { "Bloquear", "9bb8b8ac-1a13-46d3-911c-dcb3690c60b8" },
            new[] { "LiberarCursor", "df2d40d4-97cb-4465-bf60-410bb5dbc86a" }
        };

        private static InputActionAsset Cargar()
        {
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaEntrada);
        }

        [Test]
        public void Entrada_ConservaLosIdDeLasAccionesOriginales()
        {
            InputActionMap mapa = Cargar().FindActionMap("Mecha", true);

            foreach (string[] original in IdsOriginales)
            {
                InputAction accion = mapa.FindAction(original[0]);
                Assert.IsNotNull(accion, original[0] + " sigue en el mapa");
                Assert.AreEqual(original[1], accion.id.ToString(), "id de " + original[0]);
            }
        }

        [Test]
        public void Entrada_TieneLasAccionesDelEncendidoConSusTeclas()
        {
            InputActionMap mapa = Cargar().FindActionMap("Mecha", true);

            Assert.AreEqual("<Keyboard>/f", mapa.FindAction("Confirmar", true).bindings[0].path);
            Assert.AreEqual("<Keyboard>/space", mapa.FindAction("Levantar", true).bindings[0].path);
            Assert.AreEqual("<Keyboard>/q", mapa.FindAction("Cristal", true).bindings[0].path);
        }

        [Test]
        public void Prefab_LasReferenciasDeEntradaApuntanAAccionesValidas()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            int referencias = 0;
            foreach (Component componente in prefab.GetComponentsInChildren<Component>(true))
            {
                if (componente == null)
                {
                    continue;
                }
                SerializedProperty propiedad = new SerializedObject(componente).GetIterator();
                while (propiedad.NextVisible(true))
                {
                    if (propiedad.propertyType == SerializedPropertyType.ObjectReference
                        && propiedad.objectReferenceValue is InputActionReference referencia)
                    {
                        referencias++;
                        Assert.IsNotNull(referencia.action, componente.GetType().Name + "." + propiedad.name + " apunta a una acción válida");
                    }
                }
            }
            Assert.GreaterOrEqual(referencias, 8, "El prefab tiene las referencias de entrada de siempre y las del encendido");

            SerializedObject controlador = new SerializedObject(prefab.GetComponent<ControladorMecha>());
            foreach (string[] original in new[] { IdsOriginales[0], IdsOriginales[1], IdsOriginales[2], IdsOriginales[4] })
            {
                string campo = char.ToLowerInvariant(original[0][0]) + original[0].Substring(1);
                InputActionReference referencia = (InputActionReference)controlador.FindProperty(campo).objectReferenceValue;
                Assert.IsNotNull(referencia, campo + " tiene referencia");
                Assert.AreEqual(original[1], referencia.action.id.ToString(), campo + " apunta a la acción de siempre");
            }

            SerializedObject encendido = new SerializedObject(prefab.GetComponent<EncendidoMecha>());
            foreach (string campo in new[] { "confirmar", "levantar", "cristal" })
            {
                InputActionReference referencia = (InputActionReference)encendido.FindProperty(campo).objectReferenceValue;
                Assert.IsNotNull(referencia, campo + " tiene referencia");
                Assert.IsNotNull(referencia.action, campo + " apunta a una acción válida");
            }
        }
    }
}
