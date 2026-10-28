using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Misiones;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Pelea
{
    /// <summary>T-185: la tabla de peleas de misión cubre exactamente las misiones con paso Pelear y no fija objetivos.</summary>
    public class ConfigPeleasMisionPruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Pelea/Datos/ConfigPeleasMision_Base.asset";
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/Misiones/CatalogoMisiones_Base.asset";
        private const string RutaMechaPandilla = "Assets/_Proyecto/IA/Datos/DatosMecha_Pandilla.asset";
        private const string IdCamion = "robo_camion";
        private const float FactorEscolta = 1.2f;
        private const float Tolerancia = 0.01f;

        private ConfigPeleasMision config;
        private CatalogoMisiones catalogo;

        [SetUp]
        public void Preparar()
        {
            config = AssetDatabase.LoadAssetAtPath<ConfigPeleasMision>(RutaConfig);
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoMisiones>(RutaCatalogo);
            Assert.IsNotNull(config, "existe ConfigPeleasMision_Base");
            Assert.IsNotNull(catalogo, "existe el catálogo de misiones");
        }

        private HashSet<string> MisionesConPelea()
        {
            var ids = new HashSet<string>();
            foreach (DatosMision m in catalogo.Misiones)
            {
                foreach (PasoMision paso in m.Pasos)
                {
                    if (paso.Tipo == TipoPaso.Pelear) ids.Add(m.Id);
                }
            }
            return ids;
        }

        [Test]
        public void Tabla_CadaMisionConPasoPelear_TieneSusDatos()
        {
            HashSet<string> ids = MisionesConPelea();
            Assert.IsNotEmpty(ids, "el catálogo trae misiones con paso Pelear");
            foreach (string id in ids)
            {
                Assert.IsNotNull(config.Buscar(id), "la misión " + id + " tiene su DatosEmboscada");
            }
        }

        [Test]
        public void Tabla_NingunaFilaSobra_NiSeRepiteUnaMision()
        {
            HashSet<string> ids = MisionesConPelea();
            var vistas = new HashSet<string>();
            foreach (ConfigPeleasMision.Entrada fila in config.Entradas)
            {
                Assert.IsTrue(ids.Contains(fila.idMision), "la fila " + fila.idMision + " es de una misión con paso Pelear");
                Assert.IsTrue(vistas.Add(fila.idMision), "la misión " + fila.idMision + " no se repite");
                Assert.IsNotNull(fila.datos, "la fila " + fila.idMision + " tiene datos");
            }
            Assert.AreEqual(ids.Count, config.Entradas.Count, "ni más ni menos filas que misiones con Pelear");
        }

        [Test]
        public void Datos_LosDeMision_NoFijanObjetivosYTraenRivales()
        {
            foreach (ConfigPeleasMision.Entrada fila in config.Entradas)
            {
                Assert.IsFalse(fila.datos.FijaObjetivos, fila.idMision + ": la misión pone sus objetivos");
                Assert.IsNotNull(fila.datos.PrefabRival, fila.idMision + ": trae el prefab del rival");
                Assert.IsNotNull(fila.datos.PrefabRival.GetComponent<IPeleador>(), fila.idMision + ": el rival es un IPeleador");
                Assert.Greater(fila.datos.Poses.Length, 0, fila.idMision + ": trae poses");
            }
        }

        [Test]
        public void Datos_CallejonDos_BaldioTres_CamionDosEscoltasConVidaMasVeinte()
        {
            Assert.AreEqual(2, config.Buscar("barrio_callejon").Poses.Length, "el callejón trae 2 pandilleros");
            Assert.AreEqual(3, config.Buscar("corp_limpieza").Poses.Length, "el baldío trae 3");
            Assert.AreSame(config.Buscar("corp_limpieza"), config.Buscar("ilegal_pelea"), "la pelea sin jaula usa el baldío");
            DatosEmboscada camion = config.Buscar(IdCamion);
            Assert.AreEqual(2, camion.Poses.Length, "el camión trae 2 escoltas");

            DatosMecha pandilla = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaMechaPandilla);
            float vidaEscolta = VidaDelRival(camion.PrefabRival);
            float vidaPandillero = VidaDelRival(config.Buscar("barrio_callejon").PrefabRival);
            Assert.AreEqual(pandilla.VidaMaxima, vidaPandillero, Tolerancia, "el pandillero conserva su vida");
            Assert.AreEqual(pandilla.VidaMaxima * FactorEscolta, vidaEscolta, Tolerancia, "el escolta tiene +20 % de vida");
        }

        // La vida máxima que lee el rival: el DatosMecha del componente que tiene un campo "datos" de ese tipo.
        private static float VidaDelRival(GameObject prefab)
        {
            foreach (MonoBehaviour componente in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                SerializedProperty propiedad = new SerializedObject(componente).FindProperty("datos");
                if (propiedad != null && propiedad.propertyType == SerializedPropertyType.ObjectReference
                    && propiedad.objectReferenceValue is DatosMecha mecha)
                {
                    return mecha.VidaMaxima;
                }
            }
            Assert.Fail("el rival no trae un DatosMecha");
            return 0f;
        }
    }
}
