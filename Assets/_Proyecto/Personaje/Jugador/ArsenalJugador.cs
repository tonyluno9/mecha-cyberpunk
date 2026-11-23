using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Qué armas tiene el jugador a pie y en qué nivel (T-403). El nivel de un arma es el más alto de sus <see cref="DatosArma"/> en el
    /// <see cref="ServicioInventario"/>. Los puños son nivel 0 desde el inicio; la pistola también si <c>pistolaAlInicio</c> (en T-406 se
    /// consigue en la historia). Comprar el nivel n pide tener el n-1. Se registra como <c>Registro&lt;ArsenalJugador&gt;</c>.
    /// </summary>
    public class ArsenalJugador : MonoBehaviour
    {
        [SerializeField] private CatalogoArmas catalogo;
        [Tooltip("True: la pistola se tiene desde el inicio. T-406 la pone en false y la consigue en la historia.")]
        [SerializeField] private bool pistolaAlInicio = true;

        public CatalogoArmas Catalogo => catalogo;
        public bool PistolaAlInicio => pistolaAlInicio;

        // T-1527: una sola arma por tipo (ReglaTipoArma). Se vigila el inventario; la que llega reemplaza a la de su tipo.
        private readonly List<ArmaAPie> llegada = new List<ArmaAPie>();
        private readonly HashSet<ArmaAPie> desplazadas = new HashSet<ArmaAPie>();
        private ServicioInventario inventarioVigilado;
        private bool revisar = true;
        private bool revisando;

        /// <summary>T-1527: una arma (primero) quedó reemplazada por otra de su mismo tipo (segundo): se quitó del inventario.</summary>
        public event Action<ArmaAPie, ArmaAPie> Reemplazada;

        private void OnEnable() => Registro<ArsenalJugador>.Agregar(this);

        private void OnDisable()
        {
            Registro<ArsenalJugador>.Quitar(this);
            Desvigilar();
            DevolverPrestadas();
        }

        private void Desvigilar()
        {
            if (inventarioVigilado != null)
            {
                inventarioVigilado.Cambio -= AlCambiarInventario;
            }
            inventarioVigilado = null;
        }

        private void AlCambiarInventario() => revisar = true;

        private void Update()
        {
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            if (inventario != inventarioVigilado)
            {
                Desvigilar();
                inventarioVigilado = inventario;
                if (inventario != null)
                {
                    inventario.Cambio += AlCambiarInventario;
                }
                revisar = true;
            }
            if (revisar && inventario != null)
            {
                revisar = false;
                AplicarUnaPorTipo(inventario);
            }
        }

        /// <summary>T-1527: revisa el inventario y deja una sola arma por tipo (la última en llegar). Se llama sola cuando cambia el inventario.</summary>
        public void AplicarUnaPorTipo(ServicioInventario inventario)
        {
            if (revisando || inventario == null)
            {
                return;
            }
            revisando = true;
            try
            {
                List<ArmaAPie> actuales = new List<ArmaAPie>();
                if (pistolaAlInicio && !desplazadas.Contains(ArmaAPie.Pistola))
                {
                    actuales.Add(ArmaAPie.Pistola);
                }
                foreach (DatosObjeto objeto in inventario.Todos)
                {
                    if (objeto is DatosArma datos && datos.Arma != ArmaAPie.Punos && !actuales.Contains(datos.Arma))
                    {
                        actuales.Add(datos.Arma);
                        desplazadas.Remove(datos.Arma);
                    }
                }
                llegada.RemoveAll(a => !actuales.Contains(a));
                foreach (ArmaAPie a in actuales)
                {
                    if (!llegada.Contains(a))
                    {
                        llegada.Add(a);
                    }
                }
                List<ArmaAPie> viejas = ReglaTipoArma.Reemplazadas(llegada);
                foreach (ArmaAPie vieja in viejas)
                {
                    ArmaAPie nueva = vieja;
                    for (int i = llegada.Count - 1; i >= 0; i--)
                    {
                        if (llegada[i] != vieja && ReglaTipoArma.MismoTipo(llegada[i], vieja))
                        {
                            nueva = llegada[i];
                            break;
                        }
                    }
                    QuitarArma(inventario, vieja);
                    llegada.Remove(vieja);
                    Debug.Log("[Personaje] T-1527: " + nueva + " reemplaza a " + vieja + " (una arma por tipo).");
                    Reemplazada?.Invoke(vieja, nueva);
                }
            }
            finally
            {
                revisando = false;
            }
        }

        private void QuitarArma(ServicioInventario inventario, ArmaAPie arma)
        {
            desplazadas.Add(arma);
            List<DatosArma> aQuitar = new List<DatosArma>();
            foreach (DatosObjeto objeto in inventario.Todos)
            {
                if (objeto is DatosArma datos && datos.Arma == arma)
                {
                    aQuitar.Add(datos);
                }
            }
            foreach (DatosArma datos in aQuitar)
            {
                inventario.Quitar(datos, Mathf.Max(1, inventario.Cantidad(datos)));
            }
        }

        // T-1722 (T79): armas "prestadas" mientras se está en el campo de tiro: todas las que no se tienen, en su nivel más bajo del catálogo.
        // No tocan el inventario ni lo que se guarda: al devolverlas el arsenal queda como estaba.
        private readonly Dictionary<ArmaAPie, int> prestadas = new Dictionary<ArmaAPie, int>();

        /// <summary>T-1722: true mientras hay armas prestadas (campo de tiro).</summary>
        public bool Prestado => prestadas.Count > 0;

        /// <summary>T-1722: presta TODAS las armas que no se tienen y pone la munición infinita (<see cref="CargadorPistola.MunicionInfinita"/>).</summary>
        public void PrestarTodas()
        {
            prestadas.Clear();
            foreach (ArmaAPie arma in (ArmaAPie[])Enum.GetValues(typeof(ArmaAPie)))
            {
                if (NivelReal(arma) >= 0)
                {
                    continue;
                }
                int nivel = 0;
                if (catalogo != null)
                {
                    for (int n = 0; n <= 3; n++)
                    {
                        if (catalogo.Buscar(arma, n) != null)
                        {
                            nivel = n;
                            break;
                        }
                    }
                }
                prestadas[arma] = nivel;
            }
            CargadorPistola.MunicionInfinita = true;
        }

        /// <summary>T-1722: quita las armas prestadas y la munición infinita. El arsenal vuelve a lo que era.</summary>
        public void DevolverPrestadas()
        {
            prestadas.Clear();
            CargadorPistola.MunicionInfinita = false;
        }

        /// <summary>Nivel del arma (0 a 3) o -1 si no la tiene. Cuenta las armas prestadas del campo de tiro (T-1722).</summary>
        public int Nivel(ArmaAPie arma)
        {
            int real = NivelReal(arma);
            return real < 0 && prestadas.TryGetValue(arma, out int prestado) ? prestado : real;
        }

        private int NivelReal(ArmaAPie arma)
        {
            if (desplazadas.Contains(arma))
            {
                return -1;
            }
            int nivel = arma == ArmaAPie.Punos || (arma == ArmaAPie.Pistola && pistolaAlInicio) ? 0 : -1;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            if (inventario == null)
            {
                return nivel;
            }
            foreach (DatosObjeto objeto in inventario.Todos)
            {
                if (objeto is DatosArma datos && datos.Arma == arma && datos.Nivel > nivel)
                {
                    nivel = datos.Nivel;
                }
            }
            return nivel;
        }

        public bool Tiene(ArmaAPie arma) => Nivel(arma) >= 0;

        /// <summary>Los datos del arma en su nivel actual, o null si no la tiene.</summary>
        public DatosArma Datos(ArmaAPie arma)
        {
            int nivel = Nivel(arma);
            return nivel >= 0 && catalogo != null ? catalogo.Buscar(arma, nivel) : null;
        }

        /// <summary>True si este nivel es el que sigue (el n-1 ya se tiene): la tienda solo muestra esa mejora de cada arma.</summary>
        public bool EsSiguiente(DatosArma oferta) => oferta != null && oferta.Nivel == Nivel(oferta.Arma) + 1;

        /// <summary>
        /// Regala el nivel que sigue del arma, al menos el I (T-418: EL INVENTOR mejora la pistola). Lo mete al inventario sin cobrar.
        /// Devuelve sus datos, o null si ya está al máximo o falta el catálogo o el inventario.
        /// </summary>
        public DatosArma DarMejora(ArmaAPie arma)
        {
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosArma siguiente = catalogo != null ? catalogo.Buscar(arma, Mathf.Max(1, Nivel(arma) + 1)) : null;
            if (siguiente == null || inventario == null || inventario.Agregar(siguiente, 1) <= 0)
            {
                return null;
            }
            return siguiente;
        }
    }
}
