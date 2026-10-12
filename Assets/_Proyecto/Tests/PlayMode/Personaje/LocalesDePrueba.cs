using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Pruebas;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Instancia el prefab LocalDePrueba ya configurado como un lugar. Lo usan las pruebas de puertas.</summary>
    public static class LocalesDePrueba
    {
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Pruebas/LocalDePrueba.prefab";

        public static GameObject Crear(IdLugar id, Vector3 posicionPuerta, HorarioLugar horario, bool cerradoEnDemo, bool abiertoPorHorario)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            GameObject local = Object.Instantiate(prefab, posicionPuerta, Quaternion.identity);
            local.name = id.ToString();
            LugarDePrueba lugar = local.GetComponent<LugarDePrueba>();
            lugar.Configurar(id, id.ToString(), horario, cerradoEnDemo);
            lugar.PonerAbiertoPorHorario(abiertoPorHorario);
            return local;
        }

        public static void LimpiarGlobales()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }
    }
}
