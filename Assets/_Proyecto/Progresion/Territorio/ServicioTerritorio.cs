using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Quién manda en cada zona y cuánto respeto tienes (T-404): implementa <see cref="ITerritorio"/> y se guarda con clave <c>territorio</c>.
    /// También escucha la vida del jugador: cuando cae en KO llama <see cref="RegistrarCaida"/> en su posición.
    /// </summary>
    public class ServicioTerritorio : MonoBehaviour, ITerritorio, IGuardable
    {
        public const string Clave = "territorio";
        private const float SegundosBusqueda = 0.5f;

        [SerializeField] private DatosZona[] zonas = Array.Empty<DatosZona>();

        [Serializable]
        private class DatosGuardados
        {
            public string[] ids = Array.Empty<string>();
            public int[] duenos = Array.Empty<int>();
            public int[] respeto = Array.Empty<int>();
        }

        private ReglaTerritorio regla;
        private List<string> ids;
        private IVida vidaSuscrita;
        private float proximaBusqueda;

        public string ClaveGuardado => Clave;
        public event Action<string> Cambio;
        public event Action<string, IdPandilla> Conquistada;

        public IReadOnlyList<string> Zonas
        {
            get
            {
                Asegurar();
                return ids;
            }
        }

        private void Asegurar()
        {
            if (regla != null)
            {
                return;
            }
            ids = new List<string>();
            int[] duenos = new int[zonas.Length];
            for (int i = 0; i < zonas.Length; i++)
            {
                ids.Add(zonas[i].Id);
                duenos[i] = (int)zonas[i].DuenoInicial;
            }
            regla = new ReglaTerritorio(duenos, (int)IdPandilla.Jugador);
            for (int i = 0; i < zonas.Length; i++)
            {
                regla.Poner(i, zonas[i].RespetoInicial, duenos[i]);
            }
        }

        private int Indice(string zona)
        {
            Asegurar();
            return zona == null ? -1 : ids.IndexOf(zona);
        }

        private void OnEnable()
        {
            Registro<ITerritorio>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
            Registro<ServicioTerritorio>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ITerritorio>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
            Registro<ServicioTerritorio>.Quitar(this);
            if (vidaSuscrita != null)
            {
                vidaSuscrita.KO -= AlCaerElJugador;
                vidaSuscrita = null;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < proximaBusqueda)
            {
                return;
            }
            proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
            IJugador jugador = Registro<IJugador>.Actual;
            IVida vida = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<IVida>() : null;
            if (ReferenceEquals(vida, vidaSuscrita))
            {
                return;
            }
            if (vidaSuscrita != null)
            {
                vidaSuscrita.KO -= AlCaerElJugador;
            }
            vidaSuscrita = vida;
            if (vidaSuscrita != null)
            {
                vidaSuscrita.KO += AlCaerElJugador;
            }
        }

        private void AlCaerElJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null)
            {
                RegistrarCaida(jugador.Raiz.position);
            }
        }

        public string ZonaEn(Vector3 posicion)
        {
            for (int i = 0; i < zonas.Length; i++)
            {
                if (zonas[i] != null && zonas[i].Rectangulo.Contains(new Vector2(posicion.x, posicion.z)))
                {
                    return zonas[i].Id;
                }
            }
            return string.Empty;
        }

        public string Nombre(string zona)
        {
            int i = Indice(zona);
            return i >= 0 ? zonas[i].Nombre : string.Empty;
        }

        public IdPandilla Dueno(string zona)
        {
            int i = Indice(zona);
            return i >= 0 ? (IdPandilla)regla.Dueno(i) : IdPandilla.Ninguna;
        }

        public int Respeto(string zona)
        {
            int i = Indice(zona);
            return i >= 0 ? regla.Respeto(i) : 0;
        }

        public bool JefeDisponible(string zona)
        {
            int i = Indice(zona);
            return i >= 0 && regla.JefeDisponible(i);
        }

        public void RegistrarVencido(Vector3 posicion, IdPandilla pandilla, bool jefe)
        {
            Asegurar();
            int zona = Indice(ZonaEn(posicion));
            int[] antes = Foto();
            IdPandilla anterior = zona >= 0 ? (IdPandilla)regla.Dueno(zona) : pandilla;
            bool conquistada = regla.RegistrarVencido(zona, jefe);
            Avisar(antes);
            if (conquistada)
            {
                Conquistada?.Invoke(ids[zona], anterior);
            }
        }

        public void RegistrarCaida(Vector3 posicion)
        {
            Asegurar();
            int[] antes = Foto();
            regla.RegistrarCaida(Indice(ZonaEn(posicion)));
            Avisar(antes);
        }

        // Respeto y dueño de todas las zonas, para avisar solo de las que cambiaron.
        private int[] Foto()
        {
            int[] foto = new int[regla.Cantidad * 2];
            for (int i = 0; i < regla.Cantidad; i++)
            {
                foto[i * 2] = regla.Respeto(i);
                foto[i * 2 + 1] = regla.Dueno(i);
            }
            return foto;
        }

        private void Avisar(int[] antes)
        {
            int[] ahora = Foto();
            for (int i = 0; i < regla.Cantidad; i++)
            {
                if (ahora[i * 2] != antes[i * 2] || ahora[i * 2 + 1] != antes[i * 2 + 1])
                {
                    Cambio?.Invoke(ids[i]);
                }
            }
        }

        public string Guardar()
        {
            Asegurar();
            DatosGuardados datos = new DatosGuardados
            {
                ids = ids.ToArray(),
                duenos = new int[regla.Cantidad],
                respeto = new int[regla.Cantidad]
            };
            for (int i = 0; i < regla.Cantidad; i++)
            {
                datos.duenos[i] = regla.Dueno(i);
                datos.respeto[i] = regla.Respeto(i);
            }
            return JsonUtility.ToJson(datos);
        }

        public void Cargar(string texto)
        {
            Asegurar();
            DatosGuardados datos;
            try
            {
                datos = JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (Exception)
            {
                datos = null;
            }
            if (datos == null || datos.ids == null || datos.duenos == null || datos.respeto == null
                || datos.ids.Length != datos.duenos.Length || datos.ids.Length != datos.respeto.Length)
            {
                Debug.LogWarning("[Progresion] ServicioTerritorio: el guardado no se entiende; no se cambia nada.");
                return;
            }
            for (int k = 0; k < datos.ids.Length; k++)
            {
                int i = Indice(datos.ids[k]);
                if (i >= 0)
                {
                    regla.Poner(i, datos.respeto[k], datos.duenos[k]);
                    Cambio?.Invoke(ids[i]);
                }
            }
        }
    }
}
