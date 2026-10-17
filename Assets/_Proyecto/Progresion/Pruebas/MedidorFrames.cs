using System;
using System.Collections.Generic;
using System.Globalization;

namespace MechaCyberpunk.Progresion.Pruebas
{
    // Mide la duración de los frames de un recorrido de prueba (D-070, D-tony-60). Ignora el calentamiento y tolera
    // unos pocos tirones del editor: cumple si pocos frames pasan del tope blando y ninguno pasa del tope duro.
    public sealed class MedidorFrames
    {
        private readonly float segundosCalentamiento;
        private readonly float topeBlando;
        private readonly float topeDuro;
        private readonly int maxFramesLentos;
        private readonly List<float> duraciones = new List<float>();
        private float transcurrido;

        public MedidorFrames(float segundosCalentamiento = 2f, float topeBlando = 0.1f, float topeDuro = 0.5f, int maxFramesLentos = 3)
        {
            this.segundosCalentamiento = segundosCalentamiento;
            this.topeBlando = topeBlando;
            this.topeDuro = topeDuro;
            this.maxFramesLentos = maxFramesLentos;
        }

        public int FramesMedidos => duraciones.Count;

        public float Maximo
        {
            get
            {
                float max = 0f;
                for (int i = 0; i < duraciones.Count; i++)
                {
                    max = Math.Max(max, duraciones[i]);
                }
                return max;
            }
        }

        public float Percentil99
        {
            get
            {
                if (duraciones.Count == 0)
                {
                    return 0f;
                }
                List<float> orden = new List<float>(duraciones);
                orden.Sort();
                int indice = (int)Math.Ceiling(orden.Count * 0.99) - 1;
                return orden[Math.Max(0, Math.Min(orden.Count - 1, indice))];
            }
        }

        public int FramesLentos
        {
            get
            {
                int lentos = 0;
                for (int i = 0; i < duraciones.Count; i++)
                {
                    if (duraciones[i] > topeBlando)
                    {
                        lentos++;
                    }
                }
                return lentos;
            }
        }

        public void Reiniciar()
        {
            duraciones.Clear();
            transcurrido = 0f;
        }

        // Registra la duración de un frame. Los frames dentro del calentamiento (por tiempo) no se miden.
        public void Registrar(float duracion)
        {
            bool calentando = transcurrido < segundosCalentamiento;
            transcurrido += duracion;
            if (!calentando)
            {
                duraciones.Add(duracion);
            }
        }

        public bool Cumple
        {
            get
            {
                if (Maximo > topeDuro)
                {
                    return false;
                }
                int permitidos = Math.Max(maxFramesLentos, (int)Math.Floor(duraciones.Count * 0.01));
                return FramesLentos <= permitidos;
            }
        }

        public string Resumen()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "frames medidos " + duraciones.Count + ", máximo " + (Maximo * 1000f).ToString("F0", c)
                + " ms, percentil 99 " + (Percentil99 * 1000f).ToString("F0", c) + " ms, lentos (más de "
                + (topeBlando * 1000f).ToString("F0", c) + " ms) " + FramesLentos + ", tope duro " + (topeDuro * 1000f).ToString("F0", c) + " ms";
        }
    }
}
