using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-707: el prefab de la patrulla es el carro de Vehiculos (cuerpo cinemático, colliders, asiento) con la IA de la persecución, y el barrio lo sigue usando.</summary>
    public class PatrullaFisicaT707Pruebas
    {
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";

        [Test]
        public void PrefabPatrulla_EsUnCarro_ConIAYFisica()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla);
            Assert.IsNotNull(prefab.GetComponent<PatrullaMundo>());
            Assert.IsNotNull(prefab.GetComponent<CarroViejo>(), "Se maneja con la física de carro de Vehiculos");
            Assert.IsNotNull(prefab.GetComponent<IMontable>(), "Se puede robar");
            Assert.IsNotNull(prefab.GetComponent<CarroEstacionado>(), "Robarla suma calor como cualquier carro");
            Rigidbody cuerpo = prefab.GetComponent<Rigidbody>();
            Assert.IsNotNull(cuerpo);
            // T-986: ya no es cinemática: la maneja un conductor IA con la misma física que el carro del jugador.
            Assert.IsFalse(cuerpo.isKinematic, "Cuerpo con física, como el carro del jugador");
            Assert.AreEqual(RigidbodyInterpolation.Interpolate, cuerpo.interpolation, "Con interpolación, como el carro del jugador");
            Assert.GreaterOrEqual(prefab.GetComponentsInChildren<Collider>(true).Count(c => !c.isTrigger), 1, "Es sólida");
            Assert.AreEqual(2, prefab.GetComponentsInChildren<Light>(true).Count(l => l.name.StartsWith("Luz")), "Sigue con sus luces de sirena");
        }

        [Test]
        public void Barrio_SigueApuntandoALaPatrulla()
        {
            GameObject barrio = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio);
            GestorPersecucion gestor = barrio.GetComponentInChildren<GestorPersecucion>(true);
            Assert.IsNotNull(gestor);
            SerializedObject so = new SerializedObject(gestor);
            PatrullaMundo referida = so.FindProperty("prefabPatrulla").objectReferenceValue as PatrullaMundo;
            Assert.IsNotNull(referida, "La referencia del barrio a la patrulla sigue viva");
            Assert.AreEqual(RutaPatrulla, AssetDatabase.GetAssetPath(referida));
        }
    }
}
