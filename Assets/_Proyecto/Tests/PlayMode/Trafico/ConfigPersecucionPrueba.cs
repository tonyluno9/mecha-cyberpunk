using System.Reflection;
using MechaCyberpunk.Trafico.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>Una copia del ConfigPersecucion real para pruebas de persecución que no deben terminar en arresto (T-665): con el jugador quieto, una patrulla que llega lo arrestaría.</summary>
    public static class ConfigPersecucionPrueba
    {
        private const string Ruta = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";

        public static ConfigPersecucion SinArresto()
        {
            ConfigPersecucion copia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(Ruta));
            FieldInfo campo = typeof(ConfigPersecucion).GetField("segundosArresto", BindingFlags.NonPublic | BindingFlags.Instance);
            campo.SetValue(copia, 9999f);
            return copia;
        }
    }
}
