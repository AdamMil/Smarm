using System;
using System.Collections;
using System.Drawing;
using System.IO;

namespace Smarm
{

class Polygon
{ public Polygon() { points = new Point[0]; }
  public Polygon(List list) { Load(list); }
  
  public Point[] Points { get { return points; } }
  public string  Type   { get { return type; } }

  public void AddPoint(Point pt)
  { Point[] narr = new Point[points.Length+1];
    Array.Copy(points, narr, points.Length);
    narr[points.Length] = pt;
    points = narr;
  }

  /* algorithm: distance from a point to the nearest point on a line
     example line: -5,-2 to 3,-3
     convert line into parametric form (vector from origin + normal) by:
       transform the coordinate space so one point is at the origin (end - start)
       line is now 0,0 to 8,-1
       find a perpendicular vector by rotating the other point 90 degrees.
       this is the 2d analog of the 3d cross product operation, which returns a vector perpendicular
       to two other vectors.
       rotation is: newX = x*COS(angle)-y*SIN(angle), newY = x*SIN(angle)+Y*COS(angle)
       however, sin(90)==1 and cos(90)==0, so this can be simplified to: newX = -y, newY = x
       so the perpendicular vector is 1,8
       
       to find the distance from a point to the nearest point on the line, we do the following:
       example point: -9,3
       convert the point to the coordinate space of the parametric line: it becomes -4,5
       take the normal (normalize the perpendicular vector): approx. 0.124, 0.992
       do the 2d dot product of the normal with the point (norm.X*point.X + norm.Y*point.Y)
       it comes out to: approx. -0.496 + 4.961 == 4.465
       the dot product is the cosine of the angle between the vectors, times the magnitude of each vector.
       the normal's magnitude is 1.0 by definition, so the signed distance to the line is about 4.465.
       if the signed distance is positive, the point is on the same side of the line as the way the normal points.
       otherwise, it's on the opposite side.

       we use this to our advantage. for the polygon, we calculate the normal in such a way that it's consistent
       for each of the line segments. they either all point inward or all point outward, depending on whether the
       polygon was defined in a clockwise or counterclockwise fashion. thus, if the point is inside, the signed
       distances to each of the lines will all have the same sign.
       
       as an optimization, the vector is not normalized, as we only need the sign (this also allows integer math
       to be used).
  */
  // FIXME: only works for convex polygons
  public bool Contains(Point pt) { return Contains(pt.X, pt.Y); }
  public bool Contains(int x, int y)
  { if(points.Length<3) return false;
    Point s = points[0];
    int   sgn;
    bool  pos=true, neg=true;
    for(int i=1; i<points.Length; i++)
    { // combine "2d cross product" and dot product into one big expression
      sgn = Math.Sign((s.Y-points[i].Y)*(x-s.X)+(points[i].X-s.X)*(y-s.Y));
      if(sgn==-1) { pos=false; if(neg) continue; }
      else if(sgn==1) { neg=false; if(pos) continue; }
      return false;
    }
    sgn = Math.Sign((s.Y-points[0].Y)*(x-s.X)+(points[0].X-s.X)*(y-s.Y));
    if(neg) return sgn==-1;
    else if(pos) return sgn==1;
    return false;
  }

  public void RemoveLastPoint()
  { Point[] narr = new Point[points.Length-1];
    Array.Copy(points, narr, narr.Length);
    points = narr;
  }

  void Load(List list)
  { type = list["type"].GetString(0);

    List pts = list["points"];
    points = new Point[pts.Length];
    for(int i=0; i<pts.Length; i++)
    { List point = pts.GetList(i);
      points[i] = new Point(point.GetInt(0), point.GetInt(1));
    }
  }

  Point[] points;
  string  type;
}

class World : IDisposable
{ ~World() { Dispose(); }

  public bool ChangedSinceSave { get { return changed; } }

  public int Height
  { get
    { int height=0;
      foreach(Layer layer in layers) if(layer!=null && layer.Height>height) height=layer.Height;
      return height;
    }
  }

  public int Width
  { get
    { int width=0;
      foreach(Layer layer in layers) if(layer!=null && layer.Width>width) width=layer.Width;
      return width;
    }
  }

  public IList Polygons { get { return polygons; } }

  public void Clear()
  { foreach(Layer layer in layers) if(layer!=null) layer.Dispose();
    layers=new Layer[0];
    polygons.Clear();
    changed=false;
  }

  public void Dispose()
  { Clear();
    GC.SuppressFinalize(this);
  }

  public void Load(string directory)
  { if(!Directory.Exists(directory))
      throw new DirectoryNotFoundException(string.Format("Directory '{0}' not found", directory));

    string path = directory;
    path.Replace('\\', '/');
    if(path[path.Length-1] != '/') path += '/';
    FileStream fs = File.Open(path+"definition", FileMode.Open);

    try
    { Clear();
      foreach(List list in new List(fs))
      { if(list.Name=="layer")
        { int z = list.GetInt(0);
          if(z+1>layers.Length)
          { Layer[] narr = new Layer[z+1];
            Array.Copy(layers, narr, layers.Length);
            layers = narr;
          }
          layers[z] = new Layer(list, path);
        }
        else if(list.Name=="polygon") polygons.Add(new Polygon(list));
      }
    }
    finally { fs.Close(); }
  }

  public void Render(GameLib.Video.Surface dest, int sx, int sy, Rectangle drect)
  { foreach(Layer layer in layers) if(layer!=null) layer.Render(dest, sx, sy, drect);
  }

  ArrayList polygons = new ArrayList();
  Layer[] layers = new Layer[0];
  bool changed;
}

} // namespace Smarm