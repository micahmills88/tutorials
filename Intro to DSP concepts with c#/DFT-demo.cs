using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;


namespace Signal_Program
{
    public sealed partial class MainPage : Page
    {
        float frequency = 1021.58f;
        float amplitude = 10.0f;
        int samples = 4096;
        float phase = (float)Math.PI / 4f;
        float[] signal;
        float[] hannWindow;
        float[] dftRealSamples;
        float[] dftImagSamples;

        public MainPage()
        {
            this.InitializeComponent();
            Init();
        }

        private void Init()
        {
            hannWindow = new float[samples];
            dftRealSamples = new float[samples];
            dftImagSamples = new float[samples];

            signal = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                signal[i] = (float)Math.Sin((2.0f * Math.PI * frequency * i / samples) + phase);
                signal[i] += (float)Math.Sin((2.0f * Math.PI * frequency * 1.3f * i / samples) + phase);
                signal[i] += (float)Math.Sin((2.0f * Math.PI * frequency * 0.4f * i / samples) + phase);
                signal[i] += (float)Math.Sin((2.0f * Math.PI * frequency * 0.9f * i / samples) + phase);
                hannWindow[i] = (float)(0.5f * (1.0f - Math.Cos((2.0f * Math.PI * (i + 1)) / (samples - 1))));
                dftRealSamples[i] = 0.0f;
            }

            for (int freq = 0; freq < samples; freq++)
            {
                float realAverage = 0.0f;
                float imagAverage = 0.0f;
                for (int bin = 0; bin < samples; bin++)
                {
                    realAverage += (float)Math.Sin((2.0f * Math.PI * freq * bin / samples)) * signal[bin];
                    imagAverage += (float)Math.Cos((2.0f * Math.PI * freq * bin / samples)) * signal[bin];
                }
                dftRealSamples[freq] = Math.Abs(realAverage);
                dftImagSamples[freq] = Math.Abs(imagAverage);
            }
        }

        private void CanvasAnimatedControl_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            float resolution = 1280f / samples;
            CanvasPathBuilder path = new CanvasPathBuilder(sender);
            path.BeginFigure(0, 360);
            for (int i = 0; i < samples; i++)
            {
                float power = (float)(Math.Sqrt((dftRealSamples[i] * dftRealSamples[i]) + (dftImagSamples[i] * dftImagSamples[i])));
                path.AddLine(i * resolution, 360f - power);
            }
            path.EndFigure(CanvasFigureLoop.Open);
            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(path), Colors.Black);


            path = new CanvasPathBuilder(sender);
            path.BeginFigure(0, 500);
            for (int i = 0; i < samples; i++)
            {
                path.AddLine(i * resolution, 500f - signal[i] * 10f);
            }
            path.EndFigure(CanvasFigureLoop.Open);
            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(path), Colors.Red);

        }

    }
}
