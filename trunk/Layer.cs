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
using System.Collections;
using System.Drawing;
using GameLib.Video;
using ICSharpCode.SharpZipLib.Zip;

namespace Smarm
{

class Layer : IDisposable
{ public Layer() { Clear(); }
  public Layer(List list, ZipFile zip) { Load(list, zip); }

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
    foreach(Surface[,] surfaces in new Surface[][,] { full, fourth, sixteenth })
      if(surfaces!=null)
        for(int y=0; y<surfaces.GetLength(0); y++)
          for(int x=0; x<surfaces.GetLength(1); x++)
            if(surfaces[y, x]!=null) { surfaces[y, x].Dispose(); surfaces[y, x]=null; }
    full = new Surface[32, 32];
    fourth = new Surface[8, 8];
    sixteenth = new Surface[2, 2];
    width = height = 0;
   }

  public void InsertSurface(Surface s, int x, int y, ZoomMode zoom, bool checkEmpty)
  { s = s.CloneDisplay();
    int ox=x, oy=y;
    int bx=x/PartWidth, by=y/PartHeight, bw=(s.Width+PartWidth-1)/PartWidth, bh=(s.Height+PartHeight-1)/PartHeight;
    x %= PartWidth; y %= PartHeight;
    Surface[,] surfaces = (zoom==ZoomMode.Full ? full : zoom==ZoomMode.Normal ? fourth : sixteenth);
    if(bx+bw>surfaces.GetLength(1) || by+bh>surfaces.GetLength(0))
    { Surface[,] narr = new Surface[Math.Max(bx+bw, surfaces.GetLength(1)*2), Math.Max(by+bh, surfaces.GetLength(0))];
      for(int yi=0; yi<height; yi++)
        Array.Copy(surfaces, yi*surfaces.GetLength(1), narr, yi*narr.GetLength(1), width);
      if(zoom==ZoomMode.Full) full=narr;
      else if(zoom==ZoomMode.Normal) fourth=narr;
      else sixteenth=narr;
      surfaces=narr;
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

        if(checkEmpty && IsEmpty(surfaces[by+yi, bx+xi]))
        { surfaces[by+yi, bx+xi].Dispose();
          surfaces[by+yi, bx+xi]=null;
        }
        sy += srect.Height;
      }
      sx += xi==0 ? PartWidth-x : PartWidth;
    }
    
    if(s.Width>1 && s.Height>1)
      if(zoom==ZoomMode.Full) InsertSurface(ScaleDown(s), ox/4, oy/4, ZoomMode.Normal, checkEmpty);
      else if(zoom==ZoomMode.Normal) InsertSurface(ScaleDown(s), ox/4, oy/4, ZoomMode.Tiny, checkEmpty);
  }

  public void Render(Surface dest, int sx, int sy, Rectangle drect, ZoomMode zoom, bool renderObjects, Object hilite)
  { sx /= (int)zoom; sy /= (int)zoom;
    int osx=sx, osy=sy, bx=sx/PartWidth, by=sy/PartHeight, width=this.width/(int)zoom, height=this.height/(int)zoom;
    sx %= PartWidth; sy %= PartHeight;

    Surface[,] surfaces = (zoom==ZoomMode.Full ? full : zoom==ZoomMode.Normal ? fourth : sixteenth);
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
    
    if(zoom==ZoomMode.Normal && renderObjects)
      foreach(Object o in objects)
      { Rectangle bounds = o.Bounds;
        bounds.Offset(drect.X-osx, drect.Y-osy);
        if(bounds.IntersectsWith(drect)) o.Blit(dest, bounds.X, bounds.Y, o==hilite);
      }
  }

  public void Save(string path, System.IO.TextWriter writer, ZipOutputStream zip, int layerNum, bool compile)
  { string header = string.Format("  (layer {0}", layerNum);
    int img=0;
    bool tiles=compile;

    Surface[,] surfaces = compile ? fourth : full;

    for(int x=0; x<surfaces.GetLength(1); x++)
      for(int y=0; y<surfaces.GetLength(0); y++)
        if(surfaces[y, x]!=null && !IsEmpty(surfaces[y, x]))
        { string fn = string.Format("layer{0}_{1}.png", layerNum, img++);
          if(!tiles)
          { writer.WriteLine(header);
            writer.WriteLine("    (tiles");
            tiles=true;
          }
          if(compile) writer.WriteLine("  (stamp (file \"{0}\") (pos {1} {2}) (layer {3}))",
                                       fn, x*PartWidth, y*PartHeight, layerNum);
          else writer.WriteLine("      (tile \"{0}\" (pos {1} {2}))", fn, x*PartWidth, y*PartHeight);
          if(zip==null) surfaces[y, x].Save(path+fn, ImageType.PNG);
          else
          { System.IO.MemoryStream ms = new System.IO.MemoryStream(2048);
            surfaces[y, x].Save(ms, ImageType.PNG);
            zip.PutNextEntry(new ZipEntry(fn));
            GameLib.IO.IOH.CopyStream(ms, zip, true);
          }
        }

    if(tiles) { if(!compile) writer.WriteLine("    )"); }
    else
    { if(objects.Count==0) return;
      writer.WriteLine("  (layer {0}", layerNum);
    }

    if(objects.Count>0)
    { if(!compile) writer.WriteLine("    (objects");
      foreach(Object o in objects) o.Save(writer, compile ? layerNum : -1);
      if(!compile) writer.WriteLine("    )");
    }
    if(!compile) writer.WriteLine("  )");
  }

  void Load(List list, ZipFile zip)
  { Clear();

    List objects = list["objects"];
    if(objects!=null) foreach(List obj in objects) this.objects.Add(new Object(obj));

    List tiles = list["tiles"];
    if(tiles!=null)
      foreach(List image in tiles)
      { ZipEntry entry = zip.GetEntry(image.GetString(0));
        Surface surf = new Surface(new System.IO.MemoryStream(GameLib.IO.IOH.Read(zip.GetInputStream(entry), (int)entry.Size)), ImageType.PNG, true);
        List pos = image["pos"];
        InsertSurface(surf, pos.GetInt(0), pos.GetInt(1), ZoomMode.Full, false);
      }
  }
  
  bool IsEmpty(Surface s)
  { // first check diagonally
    int len = Math.Min(s.Width, s.Height);
    s.Lock();
    try
    { for(int i=0; i<len; i++) if(s.GetPixel(i, i).A!=0) return false;
      for(int i=0; i<len; i++) if(s.GetPixel(s.Width-i-1, i).A!=0) return false;
      for(int i=0; i<len; i++) if(s.GetPixel(i, s.Height-i-1).A!=0) return false;
      for(int i=1; i<=len; i++) if(s.GetPixel(s.Width-i, s.Height-i).A!=0) return false;
      for(int x=0; x<s.Width; x++) // then check the entire surface
        for(int y=0; y<s.Height; y++) if(s.GetPixel(x, y).A!=0) return false;
    }
    finally { s.Unlock(); }
    return true;
  }
  
  Surface ScaleDown(Surface s)
  { Surface ret = new Surface((s.Width+2)/4, (s.Height+2)/4, s.Format);
    s.Lock(); ret.Lock();
    for(int y=0; y<ret.Height; y++)
      for(int x=0; x<ret.Width; x++)
      { int a=0, r=0, g=0, b=0, n=0, nh;
        for(int osx=x*4,sxe=Math.Min(osx+4, s.Width),sy=y*4,sye=Math.Min(sy+4, s.Height); sy<sye; sy++)
          for(int sx=osx; sx<sxe; n++,sx++)
          { Color c = s.GetPixel(sx, sy);
            a+=c.A; r+=c.R; g+=c.G; b+=c.B;
          }
        nh=n/2;
        a=Math.Min(255, (a+nh)/n); r=Math.Min(255, (r+nh)/n); g=Math.Min(255, (g+nh)/n); b=Math.Min(255, (b+nh)/n);
        ret.PutPixel(x, y, Color.FromArgb(a, r, g, b));
      }
    ret.Unlock(); s.Unlock();
    return ret;
  }

  const int PartWidth=128, PartHeight=64;

  ArrayList objects;
  Surface[,] full, fourth, sixteenth;
  int width, height;
}

} // namespace Smarm