using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>
    /// El inventario del jugador en la escena (T-142): envuelve <see cref="ModeloInventario"/>, se registra como
    /// <c>Registro&lt;ServicioInventario&gt;</c> (no es contrato de Core, D-tony-79) y se guarda como <see cref="IGuardable"/>
    /// (clave "inventario"). Va en ServiciosEconomia.prefab.
    /// </summary>
    public class ServicioInventario : MonoBehaviour, IGuardable
    {
        public const string Clave = "inventario";

        [Serializable]
        private class Par
        {
            public string id;
            public int cantidad;
        }

        [Serializable]
        private class DatosGuardados
        {
            public List<Par> objetos = new List<Par>();
        }

        [SerializeField] private CatalogoObjetos catalogo;

        private readonly ModeloInventario modelo = new ModeloInventario();

        public CatalogoObjetos Catalogo => catalogo;
        public IReadOnlyList<DatosObjeto> Todos => modelo.Todos;
        public string ClaveGuardado => Clave;

        public event Action Cambio
        {
            add => modelo.Cambio += value;
            remove => modelo.Cambio -= value;
        }

        private void OnEnable()
        {
            Registro<ServicioInventario>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ServicioInventario>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        public int Cantidad(DatosObjeto objeto) => modelo.Cantidad(objeto);

        public bool PuedeAgregar(DatosObjeto objeto, int n) => modelo.PuedeAgregar(objeto, n);

        public int Agregar(DatosObjeto objeto, int n) => modelo.Agregar(objeto, n);

        public bool Quitar(DatosObjeto objeto, int n) => modelo.Quitar(objeto, n);

        public string Guardar()
        {
            var datos = new DatosGuardados();
            foreach (DatosObjeto o in modelo.Todos)
            {
                datos.objetos.Add(new Par { id = o.Id, cantidad = modelo.Cantidad(o) });
            }
            return JsonUtility.ToJson(datos);
        }

        public void Cargar(string texto)
        {
            DatosGuardados leidos = null;
            try
            {
                leidos = string.IsNullOrWhiteSpace(texto) ? null : JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (ArgumentException)
            {
                leidos = null;
            }
            if (leidos == null || leidos.objetos == null)
            {
                Debug.LogWarning("[Economia] El inventario no entendió los datos guardados; no se cambia nada.");
                return;
            }
            var nuevos = new List<KeyValuePair<DatosObjeto, int>>();
            foreach (Par par in leidos.objetos)
            {
                DatosObjeto objeto = catalogo != null && par != null ? catalogo.Buscar(par.id) : null;
                if (objeto == null)
                {
                    Debug.LogWarning("[Economia] El inventario guardado trae un objeto que ya no existe: " + (par?.id ?? "(vacío)") + ". Se ignora.");
                    continue;
                }
                nuevos.Add(new KeyValuePair<DatosObjeto, int>(objeto, par.cantidad));
            }
            modelo.Restablecer(nuevos);
        }
    }
}
