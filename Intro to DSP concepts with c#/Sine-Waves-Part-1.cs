private void CanvasAnimatedControl_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
{
	//y(t) = A * sin(2 * pi * f * t + p) = Asin(wt + p)
	float amplitude = 50.0f;
	float frequency = 35.0f; //in Hz
	float phase = 0.0f; //radians 360 = 2pi
	float samples = 1280.0f;


	CanvasPathBuilder path = new CanvasPathBuilder(sender);
	path.BeginFigure(0, 360);
	for(int i  = 0; i < samples; i++)
	{
		float wt = (float)(amplitude * Math.Cos((2.0f * Math.PI * frequency * i / samples) + phase));
		path.AddLine(i, 360f - wt);
	}
	path.EndFigure(CanvasFigureLoop.Open);

	args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(path), Colors.Black);

}