using System;
using System.Collections;
using System.Drawing;
using GameLib.Video;

namespace Smarm
{

class Object
{ public Object(List list) { this.list=list; }

  public string Type { get { return list.Name; } }

  List list;
}

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

  public void Render(Surface dest, int sx, int sy, Rectangle drect)
  { int bx=sx/PartWidth, by=sy/PartHeight;
    sx %= PartWidth; sy %= PartHeight;
    // FIXME: handle clipping

    for(int xi=0, dx=drect.X; dx<drect.Right; xi++)
    { for(int yi=0, dy=drect.Y; dy<drect.Bottom; yi++)
      { Point sloc = new Point(xi==0 ? sx : 0, yi==0 ? sy : 0);
        Rectangle srect = new Rectangle(sloc.X, sloc.Y, Math.Min(PartWidth-sloc.X, drect.Right-dx),
                                        Math.Min(PartHeight-sloc.Y, drect.Bottom-dy));
        if(surfaces[by+yi, bx+xi]!=null) surfaces[by+yi, bx+xi].Blit(dest, srect, dx, dy);
        dy += yi==0 ? PartHeight-sy : PartHeight;
      }
      dx += xi==0 ? PartWidth-sx : PartWidth;
    }
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

  const int PartWidth=128, PartHeight=64;

  ArrayList objects;
  Surface[,] surfaces;
  int width, height;
}

} // namespace Smarm