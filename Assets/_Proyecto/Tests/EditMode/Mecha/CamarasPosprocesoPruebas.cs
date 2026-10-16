using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>Las cámaras del jugador dibujan el posproceso de los Volumes de la escena (pedido de Alan, D-alan-19).</summary>
    public class CamarasPosprocesoPruebas
    {
        [TestCase("Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab")]
        [TestCase("Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab")]
        [TestCase("Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab")]
        public void Prefab_CamaraDelJugador_TienePosprocesoPrendido(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Existe " + ruta);
            Camera camara = prefab.GetComponentInChildren<Camera>(true);
            Assert.IsNotNull(camara, "Tiene cámara " + ruta);
            UniversalAdditionalCameraData datos = camara.GetComponent<UniversalAdditionalCameraData>();
            Assert.IsNotNull(datos, "La cámara tiene datos de URP");
            Assert.IsTrue(datos.renderPostProcessing, "renderPostProcessing prendido en " + ruta);
        }
    }
}
