using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Las balas de reserva del jugador por arma (T-720). Las crea <see cref="PistolaAPie"/> si faltan. Los datos de cada arma (tope y balas
    /// iniciales) salen de su <see cref="DatosMunicionArma"/> en el catálogo del inventario. Se guarda como <see cref="IGuardable"/> (clave "reserva_balas").
    /// </summary>
    public class ReservaBalas : MonoBehaviour, IGuardable
    {
        public const string Clave = "reserva_balas";
        private const int MaximoSinDatos = 100;

        [Serializable]
        private class Par
        {
            public int arma;
            public int balas;
        }

        [Serializable]
        private class DatosGuardados
        {
            public List<Par> reservas = new List<Par>();
        }

        private sealed class FuenteArma : IFuenteBalas
        {
            private readonly ReservaBalas reserva;
            private readonly ArmaAPie arma;

            public FuenteArma(ReservaBalas reserva, ArmaAPie arma)
            {
                this.reserva = reserva;
                this.arma = arma;
            }

            public int Disponibles => reserva.Cantidad(arma);
            public int Tomar(int cantidad) => reserva.Tomar(arma, cantidad);
        }

        private readonly ModeloReservaBalas modelo = new ModeloReservaBalas();
        private readonly Dictionary<ArmaAPie, FuenteArma> fuentes = new Dictionary<ArmaAPie, FuenteArma>();

        public string ClaveGuardado => Clave;

        public event Action Cambio
        {
            add => modelo.Cambio += value;
            remove => modelo.Cambio -= value;
        }

        private void OnEnable()
        {
            Registro<ReservaBalas>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ReservaBalas>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        /// <summary>La fuente de balas de un arma limitada; null si el arma tiene balas infinitas.</summary>
        public IFuenteBalas Fuente(ArmaAPie arma)
        {
            if (!ModeloReservaBalas.EsLimitada(arma))
            {
                return null;
            }
            if (!fuentes.TryGetValue(arma, out FuenteArma fuente))
            {
                fuente = new FuenteArma(this, arma);
                fuentes[arma] = fuente;
            }
            return fuente;
        }

        /// <summary>Los datos de las balas del arma, o null si el catálogo no los trae.</summary>
        public static DatosMunicionArma Datos(ArmaAPie arma)
        {
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            if (inventario == null || inventario.Catalogo == null)
            {
                return null;
            }
            foreach (DatosObjeto objeto in inventario.Catalogo.Objetos)
            {
                if (objeto is DatosMunicionArma municion && municion.Arma == arma)
                {
                    return municion;
                }
            }
            return null;
        }

        /// <summary>El tope de balas de reserva del arma.</summary>
        public static int Maximo(ArmaAPie arma)
        {
            DatosMunicionArma datos = Datos(arma);
            return datos != null ? datos.Maximo : MaximoSinDatos;
        }

        /// <summary>Balas de reserva del arma. La primera vez que se tiene el arma recibe sus balas iniciales.</summary>
        public int Cantidad(ArmaAPie arma)
        {
            if (!ModeloReservaBalas.EsLimitada(arma))
            {
                return 0;
            }
            if (!modelo.Existe(arma))
            {
                ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
                if (arsenal == null || !arsenal.Tiene(arma))
                {
                    return 0;
                }
                DatosMunicionArma datos = Datos(arma);
                modelo.Asegurar(arma, datos != null ? datos.BalasIniciales : 0, Maximo(arma));
            }
            return modelo.Cantidad(arma);
        }

        /// <summary>Cuántas balas caben todavía en la reserva.</summary>
        public int Espacio(ArmaAPie arma)
        {
            Cantidad(arma);
            return modelo.Espacio(arma, Maximo(arma));
        }

        /// <summary>Suma balas a la reserva sin pasar del tope. Devuelve cuántas entraron.</summary>
        public int Agregar(ArmaAPie arma, int cantidad)
        {
            Cantidad(arma); // da primero las iniciales
            return modelo.Agregar(arma, cantidad, Maximo(arma));
        }

        public int Tomar(ArmaAPie arma, int cantidad)
        {
            Cantidad(arma);
            return modelo.Tomar(arma, cantidad);
        }

        public string Guardar()
        {
            var datos = new DatosGuardados();
            foreach (KeyValuePair<ArmaAPie, int> par in modelo.Todas)
            {
                datos.reservas.Add(new Par { arma = (int)par.Key, balas = par.Value });
            }
            return JsonUtility.ToJson(datos);
        }

        public void Cargar(string texto)
        {
            DatosGuardados datos;
            try
            {
                datos = JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (ArgumentException)
            {
                datos = null;
            }
            if (datos == null || datos.reservas == null)
            {
                Debug.LogWarning("[Personaje] ReservaBalas: no se entiende el guardado.");
                return;
            }
            var lista = new List<KeyValuePair<ArmaAPie, int>>();
            foreach (Par par in datos.reservas)
            {
                lista.Add(new KeyValuePair<ArmaAPie, int>((ArmaAPie)par.arma, par.balas));
            }
            modelo.Restablecer(lista);
        }
    }
}
