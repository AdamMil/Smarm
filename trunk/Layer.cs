using System;
using System.Collections;
using System.Drawing;
using GameLib.Video;

namespace Smarm
{

class Layer : IDisposable
{ public Layer() { Clear(); }
  public Layer(List list, string basePath) { Load(list, basePath); }

  ~Layer() { Dispose(); }
  public void Dispose()
  { Clear();
    GC.SuppressFinalize(this);
  }

  public int Width  { get { return width*PartWidth;   } }
  public int Height { get { return height*PartHeight; } }
  public IList Objects { get { return objects; } }

  public void Clear()
  { objects = new ArrayList();
    if(surfaces!=null)
      for(int x=0; x<width; x++)
        for(int y=0; y<height; y++)
          if(surfaces[y, x]!=null) { surfaces[y, x].Dispose(); surfaces[y, x]=null; }
    surfaces = new Surface[32, 32];
    width = height = 0;
   }

  public void InsertSurface(Surface s, int x, int y)
  { s = s.CloneDisplay();
    int bx=x/PartWidth, by=y/PartHeight, bw=(s.Width+PartWidth-1)/PartWidth, bh=(s.Height+PartHeight-1)/PartHeight;
    x %= PartWidth; y %= PartHeight;
    if(bx+bw>surfaces.GetLength(1) || by+bh>surfaces.GetLength(0))
    { Surface[,] narr = new Surface[Math.Max(bx+bw, surfaces.GetLength(1)*2), Math.Max(by+bh, surfaces.GetLength(0))];
      for(int yi=0; yi<height; yi++)
        Array.Copy(surfaces, yi*surfaces.GetLength(1), narr, yi*narr.GetLength(1), width);
      surfaces = narr;
    }      

    s.UsingAlpha = false; // copy alpha information. don't blend.
    for(int xi=0, sx=0; xi<bw; xi++)
    { for(int yi=0, sy=0; yi<bh; yi++)
      { if(surfaces[by+yi, bx+xi]==null)
        { surfaces[by+yi, bx+xi] = new Surface(PartWidth, PartHeight, 32, SurfaceFlag.SrcAlpha);
          width  = Math.Max(width,  bx+xi+1);
          height = Math.Max(height, by+yi+1);
        }
        Point dpt = new Point(xi==0 ? x : 0, yi==0 ? y : 0);
        Rectangle srect = new Rectangle(sx, sy, Math.Min(PartWidth-dpt.X, s.Width-xi*PartWidth),
                                        Math.Min(PartHeight-dpt.Y, s.Height-yi*PartHeight));
        s.Blit(surfaces[by+yi, bx+xi], srect, dpt);
        sy += srect.Height;
      }
      sx += xi==0 ? PartWidth-x : PartWidth;
    }
  }

  public void Render(Surface dest, int sx, int sy, Rectangle drect, ZoomMode zoom, bool renderObjects, Object hilite)
  { int osx=sx, osy=sy, bx=sx/PartWidth, by=sy/PartHeight;
    sx %= PartWidth; sy %= PartHeight;

    for(int xi=0, dx=drect.X; dx<drect.Right; xi++)
    { if(bx+xi>width) break;
      if(bx+xi>=0)
        for(int yi=0, dy=drect.Y; dy<drect.Bottom; yi++)
        { if(by+yi>height) break;
          if(by+yi>=0)
          { Point sloc = new Point(xi==0 ? sx : 0, yi==0 ? sy : 0);
            Rectangle srect = new Rectangle(sloc.X, sloc.Y, Math.Min(PartWidth-sloc.X, drect.Right-dx),
                                            Math.Min(PartHeight-sloc.Y, drect.Bottom-dy));
            if(surfaces[by+yi, bx+xi]!=null) surfaces[by+yi, bx+xi].Blit(dest, srect, dx, dy);
          }
          dy += yi==0 ? PartHeight-sy : PartHeight;
        }
      dx += xi==0 ? PartWidth-sx : PartWidth;
    }
    
    if(renderObjects)
      foreach(Object o in objects)
      { Rectangle bounds = o.Bounds;
        bounds.Offset(drect.X-osx, drect.Y-osy);
        if(bounds.IntersectsWith(drect)) o.Blit(dest, bounds.X, bounds.Y, o==hilite);
      }
  }

  public void Save(string path, System.IO.TextWriter writer, Color backColor, int layerNum, bool compile)
  { string header = string.Format("  (layer {0}", layerNum);
    int img=0;
    bool tiles=false;
    for(int x=0; x<width; x++)
      for(int y=0; y<height; y++)
        if(surfaces[y, x]!=null && NotEmpty(surfaces[y, x], backColor))
        { string fn = (compile ? "clayer" : "layer") + layerNum + '_' + img++ + ".png";
          if(!tiles)
          { writer.WriteLine(header);
            writer.WriteLine("    (tiles");
            tiles=true;
          }
          writer.WriteLine("      (tile \"{0}\" (pos {1} {2}))", fn, x*PartWidth, y*PartHeight);
          if(compile) throw new NotImplementedException("compile not implemented");
          else surfaces[y, x].Save(path+fn, ImageType.PNG);
        }
    if(tiles) writer.WriteLine("    )");
    else
    { if(objects.Count==0) return;
      writer.WriteLine("  (layer {0}", layerNum);
    }

    if(objects.Count>0)
    { writer.WriteLine("    (objects");
      foreach(Object o in objects) o.Save(writer);
      writer.WriteLine("    )");
    }
    writer.WriteLine("  )");
  }

  void Load(List list, string basePath)
  { Clear();

    List objects = list["objects"];
    if(objects!=null) foreach(List obj in objects) this.objects.Add(new Object(obj));

    List tiles = list["tiles"];
    if(tiles!=null)
      foreach(List image in tiles)
      { Surface surf = new Surface(basePath+image.GetString(0));
        List pos = image["pos"];
        InsertSurface(surf, pos.GetInt(0), pos.GetInt(1));
      }
  }
  
  bool NotEmpty(Surface s, Color back)
  { Color c;
    for(int x=0; x<s.Width; x++)
      for(int y=0; y<s.Height; y++)
      { c = s.GetPixel(x, y);
        if(c.A!=0 && c!=back) return true;
      }
    return false;
  }

  const int PartWidth=128, PartHeight=64;

  ArrayList objects;
  Surface[,] surfaces;
  int width, height;
}

} // namespace Smarm