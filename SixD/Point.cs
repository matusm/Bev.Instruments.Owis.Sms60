namespace SixD
{
    public class Point
    {
        public double X;
        public double Y;

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point(Point p)
        {
            X = p.X;
            Y = p.Y;
        }

        public override string ToString() => $"[{X} , {Y}]";
    }
}
