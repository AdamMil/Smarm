/*
Smarm is an editor for the game Swarm, which was written by Jim Crawford. 
http://www.adammil.net
Copyright (C) 2003-2004 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.IO;
using System.Drawing;
using GameLib.Video;

namespace Smarm
{

#region Sprite
class Sprite
{ public Sprite(List obj)
  { List list = obj["image"];
    if(list!=null)
    { string name = list.GetString(0);
      surface = LoadImage(App.SmarmPath+name, false);
      if(surface==null) surface=LoadImage(App.SpritePath+name);
      list  = list["elementwidth"];
      width = list==null ? surface.Width : list.GetInt(0);
    }
    else
    { string dir=App.SmarmPath, fn = obj.Name+".sps";
      if(!File.Exists(dir+fn)) dir = App.SpritePath;
      if(File.Exists(dir+fn))
      { FileStream file = File.Open(dir+fn, FileMode.Open, FileAccess.Read);
        list    = new List(file);
        surface = LoadImage(dir+list["file"].GetString(0));
        width   = list["elementwidth"].GetInt(0);
        file.Close();
      }
      else if(File.Exists(App.SmarmPath+obj.Name+".png"))
      { surface = LoadImage(App.SmarmPath+obj.Name+".png");
        width   = surface.Width;
      }
      else
      { surface = LoadImage(App.SpritePath+obj.Name+".png");
        width   = surface.Width;
      }
    }
    
    if(obj.Contains("colorkey")) surface.ColorKey = obj["colorkey"].ToColor();
    else if(!surface.UsingAlpha) surface.ColorKey = Color.Magenta;

    if(surface.UsingAlpha)
    { hlSurface = surface.Clone();
      Primitives.FilledBox(hlSurface, hlSurface.Bounds, Color.FromArgb(128, 255, 255, 255));
    }
    else
    { Surface temp = surface.Clone();
      hlSurface = new Surface(surface.Width, surface.Height, 32, SurfaceFlag.SrcAlpha);
      hlSurface.Fill(0);
      temp.Blit(hlSurface);
      temp.Dispose();
      Primitives.FilledBox(hlSurface, hlSurface.Bounds, Color.FromArgb(128, 255, 255, 255));
    }

    list = obj["imageindex"];
    index = list==null ? 0 : list[0];
  }

  public int Sprites { get { return surface.Width/width; } }
  public int Height { get { return surface.Height; } }
  public object Index { get { return index; } }
  public int Width { get { return width; } }
  public Size Size { get { return new Size(width, surface.Height); } }

  public void Blit(Surface dest, int sprite, int dx, int dy, bool hilite)
  { (hilite ? hlSurface : surface).Blit(dest, new Rectangle(sprite*width, 0, width, surface.Height), dx, dy);
  }

  Surface surface, hlSurface;
  object index;
  int width;
  
  public static Surface LoadImage(string path) { return LoadImage(path, true); }
  public static Surface LoadImage(string path, bool throwOnFailure)
  { if(images.Contains(path)) return (Surface)images[path];
    Surface s;
    if(throwOnFailure) s=new Surface(path);
    else try { s=new Surface(path); } catch { return null; }
    images[path] = s;
    return s;
  }

  static System.Collections.Hashtable images = new System.Collections.Hashtable();
}
#endregion

#region Property
struct Property
{ public Property(List definition)
  { data=definition;
    if(Array.IndexOf(validTypes, Type)==-1) throw new ArgumentException("Invalid property type '"+Type+'\'');
    if(Limiter!=null && Array.IndexOf(validLimiters, LimitType)==-1)
      throw new ArgumentException("Invalid limiter '"+LimitType+'\'');
  }

  public object Default
  { get
    { if(data.Contains("default")) return data["default"][0];
      List limit = Limiter;
      if(limit!=null)
      { if(LimitType=="range" || LimitType=="enum") return limit[1];
        throw new InvalidOperationException(string.Format("Unhandled limiter '{0}' for property '{1}'",
                                                          LimitType, Name));
      }
      if(Type=="int") return 0;
      else if(Type=="float") return 0.0;
      else if(Type=="color") return Color.Black;
      return null;
    }
  }

  public string Name { get { return data.GetString(0); } }
  public string Type { get { return data.GetString(1); } }
  public List   Limiter { get { return data["limit"]; } }
  public string LimitType { get { List limit=Limiter; return limit==null ? null : limit.GetString(0); } }

  public int GetTieIndex(object value)
  { List limit = Limiter;
    if(limit==null) return Convert.ToInt32(value);
    string type = LimitType;
    if(type=="range") return Convert.ToInt32(value)-limit.GetInt(1);
    if(type=="enum")
    { for(int i=1; i<limit.Length; i++) if(value.Equals(Convert.ChangeType(limit[i], value.GetType()))) return i-1;
      return 0; // shouldn't get here
    }
    throw new InvalidOperationException(string.Format("Unhandled limiter '{0}' for property '{1}'",
                                                      limit.Name, Name));
  }

  public string Validate(object value)
  { switch(Type)
    { case "int": case "float":
        try { return ValidateNumber(Convert.ToDouble(value)); }
        catch { return "Invalid number."; }
      case "string":
        string str = (string)value;
        if(str.IndexOf('\"')!=-1 && str.IndexOf('\'')!=-1)
          return "String cannot contain both single and double quotes.";
        if(LimitType=="enum")
        { List limit=Limiter;
          for(int i=1; i<limit.Length; i++) if(limit.GetString(i)==str) return null;
          return EnumError(limit, value);
        }
        return null;
      case "color":
        Color c;
        try { c = ToColor(value); } catch { return "Not a valid color"; }
        if(LimitType=="enum")
        { List limit=Limiter;
          for(int i=1; i<limit.Length; i++) if(ToColor(limit[i])==c) return null;
          return EnumError(limit, c);
        }
        return null;
      case "bool": return null;
    }
    return null; // can't get here
  }
  
  public string ValidateNumber(double value)
  { List limit = Limiter;
    if(limit==null) return null;
    if(LimitType=="range")
      return value>=limit.GetFloat(1) && value<=limit.GetFloat(2) ? null :
        string.Format("Value must be from {0} to {1}", limit.GetFloat(1), limit.GetFloat(2));
    if(LimitType=="enum")
    { for(int i=1; i<limit.Length; i++) if(limit.GetFloat(i)==value) return null;
      return EnumError(limit, value);
    }
    return null; // can't get here
  }
  
  public static Color ToColor(object value)
  { if(value is Color) return (Color)value;
    else if(value is int) return Color.FromArgb((int)value);
    else if(value is string)
    { string cs = (string)value;
      if(cs[0]=='#')
        return Color.FromArgb(HexToInt(cs.Substring(1,2)), HexToInt(cs.Substring(3,2)), HexToInt(cs.Substring(5,2)));
      else
      { string[] vals = cs.Split(',');
        if(vals.Length==3) return Color.FromArgb(int.Parse(vals[0]), int.Parse(vals[1]), int.Parse(vals[2]));
        else return Color.FromName(cs);
      }
    }
    else throw new ApplicationException("Not a valid color!");
  }
  
  static string EnumError(List limit, object value)
  { string s = value.ToString()+" is not a valid value for enum";
    for(int i=1; i<limit.Length; i++) s += (i==1 ? " (" : " ") + limit[i];
    if(limit.Length>1) s += ')';
    return s;
  }

  static int HexToInt(string hex)
  { string digits = "0123456789abcdef";
    int res = 0;
    foreach(char c in hex)
    { int pos = digits.IndexOf(char.ToLower(c));
      if(pos==-1) throw new ApplicationException("Not a valid hex string");
      res = (res<<4) | pos;
    }
    return res;
  }

  List data;
  
  static string[] validTypes = new string[] { "int", "float", "string", "bool", "color" };
  static string[] validLimiters = new string[] { "range", "enum" };
}
#endregion

#region ObjectDef
class ObjectDef
{ public ObjectDef(List definition) : this(definition, new Sprite(definition)) { }
  public ObjectDef(List definition, Sprite sprite)
  { data = definition;
    this.sprite = sprite;

    System.Collections.ArrayList array = new System.Collections.ArrayList();
    foreach(List list in data) if(list.Name=="prop") array.Add(new Property(list));
    props = (Property[])array.ToArray(typeof(Property));
  }

  public Property this[string property]
  { get
    { foreach(Property prop in props) { if(prop.Name==property) return prop; }
      throw new ArgumentException(string.Format("Object '{0}' has no property '{1}'", Name, property), "Property");
    }
  }

  public bool Colorized { get { return data["colorize"]!=null; } }
  public string Name { get { return data.Name; } }
  public Property[] Properties { get { return props; } }
  public Sprite Sprite { get { return sprite; } }

  public Color GetColor(int value)
  { List color = data["colorize"];
    if(color==null) throw new InvalidOperationException(string.Format("Object '{0}' is not colorized!", Name));
    return color.GetList(this[color.GetString(0)].GetTieIndex(value)).ToColor();
  }

  Property[] props;
  Sprite sprite;
  List   data;
  
  public static ObjectDef[] Objects { get { return defs; } }
  public static void LoadDefs(List objs)
  { defs = new ObjectDef[objs.Length];
    for(int i=0; i<objs.Length; i++) defs[i] = new ObjectDef(objs.GetList(i));
  }
  static ObjectDef[] defs;
}
#endregion

#region PolygonType
class PolygonType
{ public PolygonType(List definition)
  { Type  = definition.Name;
    Color = definition["color"].ToColor();
  }

  public static PolygonType[] Types { get { return defs; } }
  public static void LoadDefs(List objs)
  { defs = new PolygonType[objs.Length];
    for(int i=0; i<objs.Length; i++) defs[i] = new PolygonType(objs.GetList(i));
  }

  public string Type;
  public Color  Color;

  static PolygonType[] defs;
}
#endregion

#region Polygon
class Polygon
{ public Polygon(string type) { points = new Point[0]; Type=type; }
  public Polygon(List list) { Load(list); }
  
  public Rectangle Bounds
  { get
    { if(points.Length==0) return new Rectangle();
      int top=int.MaxValue, left=int.MaxValue, right=int.MinValue, bottom=int.MinValue;
      foreach(Point pt in points)
      { if(pt.X<left)   left   = pt.X;
        if(pt.X>right)  right  = pt.X;
        if(pt.Y<top)    top    = pt.Y;
        if(pt.Y>bottom) bottom = pt.Y;
      }
      return new Rectangle(left, top, right-left+1, bottom-top+1);
    }
  }

  public Color Color { get { return type.Color; } }
  public Point[] Points { get { return points; } }
  public string  Type
  { get { return type.Type; }
    set
    { if(type==null || value!=type.Type)
      { foreach(PolygonType pt in PolygonType.Types) if(value==pt.Type) { type=pt; return; }
        throw new ArgumentException(string.Format("Invalid polygon type '{0}'", value));
      }
    }
  }

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

  public void Save(TextWriter writer)
  { writer.Write("  (polygon (type \"{0}\") (points", Type);
    foreach(Point pt in points) writer.Write(" ({0} {1})", pt.X, pt.Y);
    writer.WriteLine("))");
  }

  /*
  void Split()
  { if(this.Points.Length<3) return null;
    Polygon[] test, done;
    test.Add(this);
    while(test.Count)
    { restart:
      foreach(Polygon p in test)
      { foreach(Corner c in p)
        { if(c makes concave) // c.DotSign != lastC.dotSign
          { (ie,pt,dist) = NearestIntersecting(p, c.Edges[0]);
            if(ie==null)
            { (ie, pt, ep, dist) = NearestIntersecting(p, c.Edges[1]);
              if(ie==null) give up, throw exception;
            }
            if(ie!=null)
            { np = Add point between ie.Points[0] and ie.Points[1];
              reverse directions starting from from 'ep' and add all points up to and including 'np'
                into a new polygon, and removing those points from 'p' (except 'np', which is not removed);
              add that polygon to 'test';
              goto restart;
            }
          }
        }
        done.Add(p); // convex, so we're done with it
      }
    }
  }
  
  Edge,Point,Point,int NearestIntersecting(Polygon p, Edge e)
  { Edge ie=null; // intersecting edge
    Point ip, ep;
    int dist=int.Max;
    foreach(Edge oe in p)
      if(!oe.Adjacent(e)) // adjacent edge shares a vertex
      { Point pt = e.Intersection(oe); // line intersection
        if(Min(pt.DistanceTo(e.Points[0]), pt.DistanceTo(e.Points[1])) < dist)
        { dist = the distance;
          ep = the edge that was extended (the one that had the smaller distance)
          ip = pt;
          ie = oe;
        }
      }
    return ie, pt, ep, dist;
  }
  */
  public Polygon[] Split()
  { if(points.Length<3) return null;
    return null;
  }

  void Load(List list)
  { Type = list["type"].GetString(0);

    List pts = list["points"];
    points = new Point[pts.Length];
    for(int i=0; i<pts.Length; i++)
    { List point = pts.GetList(i);
      points[i] = new Point(point.GetInt(0), point.GetInt(1));
    }
  }

  Point[] points;
  PolygonType type;
}
#endregion

#region Object
class Object
{ public Object(string type)
  { SetType(type);
    data = new List(type);
  }
  public Object(string type, Point location) : this(type) { Location=location; }
  public Object(List data)
  { SetType(data.Name);
    this.data = data;
    if(data.Contains("pos")) Location = data["pos"].ToPoint();
  }
  public Object(ObjectDef def) { type=def; data=new List(def.Name); }
  public Object(ObjectDef def, List data)
  { type=def;
    this.data = data;
    if(data.Contains("pos")) Location = data["pos"].ToPoint();
  }

  public object this[string property]
  { get
    { List list = data[property];
      if(list!=null)
      { object value = list[0];
        switch(type[property].Type)
        { case "int": value=Convert.ToInt32(value); break;
          case "float": value=Convert.ToDouble(value); break;
          case "string": value=value.ToString(); break;
          case "bool": value=Convert.ToBoolean(value); break;
          case "color": value=Property.ToColor(value); break;
        }
        return value;
      }
      return type[property].Default;
    }
    set
    { Property prop = type[property];
      string error = prop.Validate(value);
      if(error!=null) throw new ArgumentException(error);
      List list = data[property];
      if(list==null) data.Add(new List(property, value));
      else list[0] = value;
    }
  }

  public Rectangle Bounds { get { return new Rectangle(Location, type.Sprite.Size); } }
  public int Width  { get { return type.Sprite.Width; } }
  public int Height { get { return type.Sprite.Height; } }

  public Point Location
  { get { return location; }
    set
    { location=value;
      List pos = data["pos"];
      if(pos==null) { pos = new List("pos", location.X, location.Y); data.Add(pos); }
      else { pos[0] = location.X; pos[1] = location.Y; }
    }
  }

  public string Name { get { return type.Name; } }
  public ObjectDef Type { get { return type; } }

  public void Blit(Surface dest, int x, int y, bool hilite)
  { object index = type.Sprite.Index;
    if(index is int) type.Sprite.Blit(dest, (int)index, x, y, hilite);
    else
    { Property prop = type[(string)index];
      object value = this[prop.Name];
      type.Sprite.Blit(dest, value==null ? 0 : prop.GetTieIndex(value), x, y, hilite);
    }
    // TODO: implement colorize
  }

  public void Save(TextWriter writer) { Save(writer, -1); }
  public void Save(TextWriter writer, int layer)
  { writer.Write("({0} (pos {1} {2})", Name, Location.X, Location.Y);
    foreach(Property prop in type.Properties)
    { object value = this[prop.Name];
      if(value!=null)
      { if(prop.Type=="color")
        { Color c = (Color)value;
          value = string.Format("\"{0},{1},{2}\"", c.R, c.G, c.B);
        }
        if(prop.Type=="string")
        { string s = (string)value;
          if(s.IndexOf('\"')!=-1) value = "'" + s + '\'';
          else value = "\"" + s + '\"';
        }
        else if(prop.Type=="bool") value = (bool)value ? 1 : 0;
        writer.Write(" ({0} {1})", prop.Name, value);
      }
    }
    if(layer!=-1) writer.WriteLine(" (layer {0})", layer);
    writer.WriteLine(')');
  }

  void SetType(string type)
  { foreach(ObjectDef def in ObjectDef.Objects) if(def.Name==type) { this.type=def; return; }
    throw new ArgumentException(string.Format("No such object type '{0}'", type));
  }

  ObjectDef type;
  List data;
  Point location;
}
#endregion

} // namespace Smarm