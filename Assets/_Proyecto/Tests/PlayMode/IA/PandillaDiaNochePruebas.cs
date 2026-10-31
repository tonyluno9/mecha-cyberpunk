using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-447: una pandilla en modo siempre deja de día solo una fracción de sus pandilleros (al menos uno) y de noche salen todos; al
    /// amanecer los que sobran se retiran. Una pandilla de solo noche no sale de día. Reloj doble que sí avisa los cambios de franja.
    /// </summary>
    public class PandillaDiaNochePruebas
    {
        private const string RutaPandilla = "Assets/_Proyecto/IA/Prefabs/PandillaAPie.prefab";
        private const float FraccionDePrueba = 0.34f;

        private sealed class RelojDoble : IRelojMundo
        {
            private bool noche;
            public float Hora => noche ? 22f : 12f;
            public int Dia => 1;
            public FranjaDia Franja => noche ? FranjaDia.Noche : FranjaDia.Dia;
            public bool EsDeNoche => noche;
            public float HoraAmanecer => 6f;
            public float HoraAnochecer => 20f;
            public bool Pausado => true;
            public event Action<int> HoraEnteraCambio { add { } remove { } }
            public event Action<FranjaDia> FranjaCambio;
            public event Action<int> DiaCambio { add { } remove { } }
            public void Pausar(bool pausado) { }
            public void AvanzarHasta(float hora) { }

            public void Poner(bool esNoche)
            {
                noche = esNoche;
                FranjaCambio?.Invoke(Franja);
            }
        }

        private GameObject raizInactiva;
        private GameObject pandilla;
        private DatosPandilleroAPie datos;
        private RelojDoble reloj;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            reloj = new RelojDoble();
            Registro<IRelojMundo>.Agregar(reloj);
        }

        [TearDown]
        public void Limpiar()
        {
            if (pandilla != null)
            {
                UnityEngine.Object.DestroyImmediate(pandilla);
            }
            if (raizInactiva != null)
            {
                UnityEngine.Object.DestroyImmediate(raizInactiva);
            }
            if (datos != null)
            {
                UnityEngine.Object.DestroyImmediate(datos);
            }
            Registros.LimpiarTodos();
        }

        // Crea la pandilla con datos propios (siempre o solo de noche) y la activa con el reloj ya en la franja pedida.
        private PandillaAPie Crear(bool siempre, bool esNoche)
        {
            reloj.Poner(esNoche);
            raizInactiva = new GameObject("Inactiva");
            raizInactiva.SetActive(false);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandilla);
            Assert.IsNotNull(prefab, "Falta " + RutaPandilla);
            pandilla = UnityEngine.Object.Instantiate(prefab, raizInactiva.transform);
            PandillaAPie grupo = pandilla.GetComponent<PandillaAPie>();
            datos = UnityEngine.Object.Instantiate(grupo.Datos);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("siempre").boolValue = siempre;
            so.FindProperty("fraccionDeDia").floatValue = FraccionDePrueba;
            so.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soGrupo = new SerializedObject(grupo);
            soGrupo.FindProperty("datos").objectReferenceValue = datos;
            soGrupo.ApplyModifiedPropertiesWithoutUndo();
            pandilla.transform.SetParent(null, true);
            return grupo;
        }

        private static int Visibles(PandillaAPie grupo)
        {
            int cuenta = 0;
            foreach (PandilleroAPie p in grupo.Pandilleros)
            {
                cuenta += p.gameObject.activeSelf ? 1 : 0;
            }
            return cuenta;
        }

        [UnityTest]
        public IEnumerator Siempre_DeDiaQuedaLaFraccion_DeNocheTodos_YAlAmanecerSeRetiranLosSobrantes()
        {
            PandillaAPie grupo = Crear(siempre: true, esNoche: false);
            yield return null;
            int total = grupo.Pandilleros.Length;
            Assert.GreaterOrEqual(total, 2, "El prefab trae un grupo");
            int deDia = Mathf.Clamp(Mathf.RoundToInt(total * FraccionDePrueba), 1, total);
            Assert.Less(deDia, total, "De día quedan menos que el total");
            Assert.AreEqual(deDia, Visibles(grupo), "De día se ve solo la fracción");

            reloj.Poner(true);
            yield return null;
            Assert.AreEqual(total, Visibles(grupo), "Al anochecer salen todos");

            reloj.Poner(false);
            yield return null;
            yield return null;
            Assert.AreEqual(deDia, Visibles(grupo), "Al amanecer se retiran los sobrantes");
        }

        [UnityTest]
        public IEnumerator Siempre_SinReloj_SeVenTodos()
        {
            Registros.LimpiarTodos();
            reloj.Poner(false);
            raizInactiva = new GameObject("Inactiva");
            raizInactiva.SetActive(false);
            pandilla = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandilla), raizInactiva.transform);
            PandillaAPie grupo = pandilla.GetComponent<PandillaAPie>();
            datos = UnityEngine.Object.Instantiate(grupo.Datos);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("siempre").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject soGrupo = new SerializedObject(grupo);
            soGrupo.FindProperty("datos").objectReferenceValue = datos;
            soGrupo.ApplyModifiedPropertiesWithoutUndo();
            pandilla.transform.SetParent(null, true);
            yield return null;
            Assert.AreEqual(grupo.Pandilleros.Length, Visibles(grupo), "Sin reloj no se sabe si es de día: salen todos");
        }

        [UnityTest]
        public IEnumerator SoloDeNoche_DeDiaNoSale_YDeNocheSalenTodos()
        {
            PandillaAPie grupo = Crear(siempre: false, esNoche: false);
            yield return null;
            Assert.AreEqual(0, Visibles(grupo), "Una pandilla de solo noche no sale de día");
            reloj.Poner(true);
            yield return null;
            Assert.AreEqual(grupo.Pandilleros.Length, Visibles(grupo), "De noche salen todos");
        }
    }
}
