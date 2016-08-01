using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Networking;
using Windows.Networking.Sockets;
using System.Threading.Tasks;
using System.Numerics;

namespace Signal_Program
{

    public class FFT
    {

        /* 
         * Computes the discrete Fourier transform (DFT) of the given complex vector, storing the result back into the vector.
         * The vector can have any length. This is a wrapper function.
         */
        private float[] cosTable;
        private float[] sinTable;

        public FFT(int size)
        {
            cosTable = new float[size];
            sinTable = new float[size];
            for (int i = 0; i < size; i++)
            {
                cosTable[i] = (float)Math.Cos(2 * Math.PI * i / size);
                sinTable[i] = (float)Math.Sin(2 * Math.PI * i / size);
            }
        }

        /* 
         * Computes the discrete Fourier transform (DFT) of the given complex vector, storing the result back into the vector.
         * The vector's length must be a power of 2. Uses the Cooley-Tukey decimation-in-time radix-2 algorithm.
         */
        public void TransformRadix2(float[] real, float[] imag)
        {
            // Initialization
            if (real.Length != imag.Length)
                throw new ArgumentException("Mismatched lengths");

            int n = real.Length;
            int levels = 31 - NumberOfLeadingZeros(n);  // Equal to floor(log2(n))
            if (1 << levels != n)
                throw new ArgumentException("Length is not a power of 2");

            // Bit-reversed addressing permutation
            for (int i = 0; i < n; i++)
            {
                int j = (int)((uint)ReverseBits(i) >> (32 - levels));
                if (j > i)
                {
                    float temp = real[i];
                    real[i] = real[j];
                    real[j] = temp;
                    temp = imag[i];
                    imag[i] = imag[j];
                    imag[j] = temp;
                }
            }

            // Cooley-Tukey decimation-in-time radix-2 FFT
            for (int size = 2; size <= n; size *= 2)
            {
                int halfsize = size / 2;
                int tablestep = n / size;
                for (int i = 0; i < n; i += size)
                {
                    for (int j = i, k = 0; j < i + halfsize; j++, k += tablestep)
                    {
                        float tpre = real[j + halfsize] * cosTable[k] + imag[j + halfsize] * sinTable[k];
                        float tpim = -real[j + halfsize] * sinTable[k] + imag[j + halfsize] * cosTable[k];
                        real[j + halfsize] = real[j] - tpre;
                        imag[j + halfsize] = imag[j] - tpim;
                        real[j] += tpre;
                        imag[j] += tpim;
                    }
                }
                if (size == n)  // Prevent overflow in 'size *= 2'
                    break;
            }
        }

        private int NumberOfLeadingZeros(int val)
        {
            if (val == 0)
                return 32;
            int result = 0;
            for (; val >= 0; val <<= 1)
                result++;
            return result;
        }

        private int HighestOneBit(int val)
        {
            for (int i = 1 << 31; i != 0; i = (int)((uint)i >> 1))
            {
                if ((val & i) != 0)
                    return i;
            }
            return 0;
        }

        private int ReverseBits(int val)
        {
            int result = 0;
            for (int i = 0; i < 32; i++, val >>= 1)
                result = (result << 1) | (val & 1);
            return result;
        }

    }
    public class CircleBuffer
    {
        float[] nodes;
        int current;
        int emptySpot;

        public CircleBuffer(int size)
        {
            nodes = new float[size];
            current = 0;
            emptySpot = 0;
        }

        public void AddValue(float value)
        {
            nodes[emptySpot] = value;
            emptySpot++;
            if (emptySpot >= nodes.Length)
            {
                emptySpot = 0;
            }
        }
        public float GetValue()
        {
            int ret = current;
            current++;
            if (current >= nodes.Length)
            {
                current = 0;
            }
            return nodes[ret];
        }
    }
    public sealed partial class MainPage : Page
    {
        FFT fft;
        FFT han;
        float frequency = 1021.58f;
        float amplitude = 10.0f;
        int samples = 4096;
        float phase = 1.7f;
        float[] signal;
        float[] hannWindow;
        float[] hannFFTreal;
        float[] hannFFTimag;

        float[] sh;
        float[] h1;
        float[] h2;

        public MainPage()
        {
            this.InitializeComponent();
            Init();
        }

        private void Init()
        {
            fft = new FFT(samples);
            han = new FFT(samples);
            hannFFTreal = new float[samples];
            hannFFTimag = new float[samples];
            hannWindow = new float[samples];
            h1 = new float[samples];
            h2 = new float[samples];
            sh = new float[samples];

            signal = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                signal[i] = (float)Math.Sin((2.0f * Math.PI * frequency * i / samples) + phase);
                signal[i] += (float)Math.Sin((2.0f * Math.PI * frequency * 1.3f * i / samples) + phase);
                signal[i] += (float)Math.Sin((2.0f * Math.PI * frequency * 0.4f * i / samples) + phase);
                signal[i] += (float)Math.Sin((2.0f * Math.PI * frequency * 0.9f * i / samples) + phase);
                hannWindow[i] = (float)(0.5f * (1.0f - Math.Cos((2.0f * Math.PI * (i + 1)) / (samples - 1))));
                sh[i] = signal[i] * hannWindow[i];
                hannFFTreal[i] = sh[i];
                hannFFTimag[i] = 0.0f;
                h1[i] = signal[i];
                h2[i] = 0.0f;
            }
            fft.TransformRadix2(h1, h2);
            fft.TransformRadix2(hannFFTreal, hannFFTimag);
        }

        private void CanvasAnimatedControl_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            

            float resolution = (float)(1280f / (float)samples);
            CanvasPathBuilder path = new CanvasPathBuilder(sender);
            path.BeginFigure(0, 600);
            for (int i = 0; i < samples; i++)
            {
                float value = amplitude * signal[i];
                path.AddLine(i * resolution, 600f - value);
            }
            path.EndFigure(CanvasFigureLoop.Open);
            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(path), Colors.Black);

            CanvasPathBuilder hannSignal = new CanvasPathBuilder(sender);
            hannSignal.BeginFigure(0, 500);
            for (int i = 0; i < samples; i++)
            {
                float value = amplitude * sh[i];
                hannSignal.AddLine(i * resolution, 500f - value);
            }
            hannSignal.EndFigure(CanvasFigureLoop.Open);
            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(hannSignal), Colors.Red);

            CanvasPathBuilder fftPath = new CanvasPathBuilder(sender);
            fftPath.BeginFigure(0, 360);
            for(int i = 0; i < samples/2; i++)
            {
                float value = (float)(Math.Sqrt((hannFFTreal[i] * hannFFTreal[i]) + (hannFFTimag[i] * hannFFTimag[i])));
                fftPath.AddLine(i * resolution * 2, 360f - value);
            }
            fftPath.EndFigure(CanvasFigureLoop.Open);
            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(fftPath), Colors.Red);

            CanvasPathBuilder hannPath = new CanvasPathBuilder(sender);
            hannPath.BeginFigure(0, 360f);
            for (int i = 0; i < samples / 2; i++)
            {
                float value = (float)(Math.Sqrt((h1[i] * h1[i]) + (h2[i] * h2[i])));
                hannPath.AddLine(i * resolution * 2, 360f - value);
            }
            hannPath.EndFigure(CanvasFigureLoop.Open);
            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(hannPath), Colors.Black);


        }

    }
}
