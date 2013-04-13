using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


// because unity doesn't let us have PointF's grumble grumble
class PointF
{
    public float X { get; set; }
    public float Y { get; set; }

    public PointF(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }
}

