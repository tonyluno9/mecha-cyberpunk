using System;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Guardado
{
    /// <summary>
    /// Guarda y carga la partida (T-111, D-tony-50). Junta todo lo que está en <c>Registro&lt;IGuardable&gt;</c> sin conocer a
    /// nadie. No guarda ni carga solo: lo decide quien lo llame (menú y dormir, T-F6). Va en ServiciosProgresion.prefab.
    /// </summary>
    public class ServicioGuardado : MonoBehaviour
    {
        private const string NombreArchivo = "partida.json";
        private const string ExtensionTemporal = ".tmp";

        private string ruta;
        private readonly Dictionary<string, string> pendientes = new Dictionary<string, string>();

        /// <summary>Dónde está el archivo. Por defecto persistentDataPath/partida.json.</summary>
        public string RutaArchivo => ruta ?? (ruta = Path.Combine(Application.persistentDataPath, NombreArchivo));

        public bool HayPartida => File.Exists(RutaArchivo);

        public event Action Guardada;
        public event Action Cargada;

        /// <summary>Cambia la ruta del archivo (pruebas).</summary>
        public void FijarRuta(string nuevaRuta)
        {
            ruta = nuevaRuta;
        }

        private void OnEnable()
        {
            Registro<IGuardable>.Agregado += AlRegistrarse;
        }

        private void OnDisable()
        {
            Registro<IGuardable>.Agregado -= AlRegistrarse;
        }

        /// <summary>Escribe la partida de forma atómica. False (con error en consola) si no se pudo; el archivo anterior queda intacto.</summary>
        public bool Guardar()
        {
            var piezas = new List<PiezaGuardada>();
            var claves = new HashSet<string>();
            foreach (IGuardable guardable in Registro<IGuardable>.Todos)
            {
                string clave = guardable.ClaveGuardado;
                if (!claves.Add(clave))
                {
                    Debug.LogWarning("[Progresion] Clave de guardado repetida '" + clave + "'; solo se guarda la primera.");
                    continue;
                }
                piezas.Add(new PiezaGuardada { clave = clave, datos = guardable.Guardar() ?? string.Empty });
            }
            string json = ArchivoPartida.Escribir(piezas, DateTime.UtcNow.ToString("o"));
            string temporal = RutaArchivo + ExtensionTemporal;
            try
            {
                string carpeta = Path.GetDirectoryName(RutaArchivo);
                if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);
                File.WriteAllText(temporal, json);
                if (File.Exists(RutaArchivo)) File.Replace(temporal, RutaArchivo, null);
                else File.Move(temporal, RutaArchivo);
            }
            catch (Exception e)
            {
                Debug.LogError("[Progresion] No se pudo guardar la partida: " + e.Message);
                try
                {
                    if (File.Exists(temporal)) File.Delete(temporal);
                }
                catch (Exception)
                {
                    // Sin más: el error ya se avisó.
                }
                return false;
            }
            Guardada?.Invoke();
            return true;
        }

        /// <summary>Lee la partida y se la devuelve a cada registrado. False (con warning) si no existe o no se entiende; no cambia nada.</summary>
        public bool Cargar()
        {
            if (!File.Exists(RutaArchivo))
            {
                Debug.LogWarning("[Progresion] No hay partida guardada en " + RutaArchivo);
                return false;
            }
            string json;
            try
            {
                json = File.ReadAllText(RutaArchivo);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Progresion] No se pudo leer la partida: " + e.Message);
                return false;
            }
            if (!ArchivoPartida.TryLeer(json, out List<PiezaGuardada> piezas, out _))
            {
                Debug.LogWarning("[Progresion] La partida guardada no se entiende; no se cambia nada.");
                return false;
            }
            pendientes.Clear();
            foreach (PiezaGuardada pieza in piezas)
            {
                pendientes[pieza.clave] = pieza.datos;
            }
            // Copia: Cargar de un guardable podría registrar otros.
            foreach (IGuardable guardable in new List<IGuardable>(Registro<IGuardable>.Todos))
            {
                Entregar(guardable);
            }
            Cargada?.Invoke();
            return true;
        }

        /// <summary>Borra el archivo de la partida.</summary>
        public void Borrar()
        {
            try
            {
                if (File.Exists(RutaArchivo)) File.Delete(RutaArchivo);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Progresion] No se pudo borrar la partida: " + e.Message);
            }
            pendientes.Clear();
        }

        private void AlRegistrarse(IGuardable guardable)
        {
            if (guardable != null) Entregar(guardable);
        }

        private void Entregar(IGuardable guardable)
        {
            if (pendientes.TryGetValue(guardable.ClaveGuardado, out string datos))
            {
                pendientes.Remove(guardable.ClaveGuardado);
                guardable.Cargar(datos);
            }
        }
    }
}
