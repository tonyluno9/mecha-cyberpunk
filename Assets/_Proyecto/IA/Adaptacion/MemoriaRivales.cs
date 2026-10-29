using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>
    /// Lo que los rivales recuerdan del jugador, una <see cref="MemoriaRival"/> por rival (T-200, D-tony-194). Se guarda en la partida con la
    /// clave <c>adaptacion</c>. Va en ServiciosProgresion.prefab; sin ella cada rival usa una memoria propia que se pierde al destruirse.
    /// </summary>
    public class MemoriaRivales : MonoBehaviour, IGuardable
    {
        public const string Clave = "adaptacion";

        private readonly List<MemoriaRival> memorias = new List<MemoriaRival>();

        /// <summary>La memoria de rivales activa de la escena, o null.</summary>
        public static MemoriaRivales Actual => Registro<MemoriaRivales>.Actual;

        public string ClaveGuardado => Clave;

        /// <summary>Cuántos rivales tienen memoria.</summary>
        public int Cantidad => memorias.Count;

        /// <summary>Se avisa cuando la memoria cambia: al aprender, al olvidar y al cargar.</summary>
        public event Action Cambio;

        private void OnEnable()
        {
            Registro<MemoriaRivales>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IGuardable>.Quitar(this);
            Registro<MemoriaRivales>.Quitar(this);
        }

        /// <summary>La memoria de un rival; si no hay, crea una vacía.</summary>
        public MemoriaRival Obtener(string idRival)
        {
            foreach (MemoriaRival memoria in memorias)
            {
                if (memoria.idRival == idRival)
                {
                    return memoria;
                }
            }
            MemoriaRival nueva = new MemoriaRival { idRival = idRival };
            memorias.Add(nueva);
            return nueva;
        }

        /// <summary>La memoria de un rival o null si no hay; no crea nada (para leer sin ensuciar el guardado).</summary>
        public MemoriaRival Buscar(string idRival)
        {
            foreach (MemoriaRival memoria in memorias)
            {
                if (memoria.idRival == idRival)
                {
                    return memoria;
                }
            }
            return null;
        }

        /// <summary>Avisa que una memoria cambió (lo llama quien la modifica, por ejemplo al aprender).</summary>
        public void AvisarCambio()
        {
            Cambio?.Invoke();
        }

        /// <summary>Borra lo que recuerda un rival.</summary>
        public void Olvidar(string idRival)
        {
            if (memorias.RemoveAll(m => m.idRival == idRival) > 0)
            {
                Cambio?.Invoke();
            }
        }

        /// <summary>Borra la memoria de todos los rivales.</summary>
        public void OlvidarTodo()
        {
            if (memorias.Count == 0)
            {
                return;
            }
            memorias.Clear();
            Cambio?.Invoke();
        }

        public string Guardar()
        {
            ListaMemoriasRival lista = new ListaMemoriasRival();
            lista.memorias.AddRange(memorias);
            return JsonUtility.ToJson(lista);
        }

        public void Cargar(string datos)
        {
            ListaMemoriasRival lista = null;
            if (!string.IsNullOrEmpty(datos))
            {
                try
                {
                    lista = JsonUtility.FromJson<ListaMemoriasRival>(datos);
                }
                catch (Exception)
                {
                    lista = null;
                }
            }
            if (lista == null || lista.memorias == null)
            {
                Debug.LogWarning("[IA] La memoria de los rivales guardada no se entiende; no se cambia nada.");
                return;
            }
            memorias.Clear();
            foreach (MemoriaRival memoria in lista.memorias)
            {
                if (memoria == null || string.IsNullOrEmpty(memoria.idRival))
                {
                    continue;
                }
                if (memoria.evidencias == null || memoria.evidencias.Length != 3)
                {
                    int[] ajustadas = new int[3];
                    if (memoria.evidencias != null)
                    {
                        Array.Copy(memoria.evidencias, ajustadas, Math.Min(memoria.evidencias.Length, 3));
                    }
                    memoria.evidencias = ajustadas;
                }
                memorias.Add(memoria);
            }
            Cambio?.Invoke();
        }
    }
}
