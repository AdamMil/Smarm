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
using System.IO;
using GameLib.Collections;
using GameLib.IO;
using GameLib.Video;
using ICSharpCode.SharpZipLib.Zip;

namespace Smarm
{

class Layer : IDisposable
{ public Layer(World world) { this.world=world; Clear(); }
  public Layer(World world, List list) { this.world=world; Load(list); }

  ~Layer() { Dispose(); }
  public void Dispose()
  { Clear();
    GC.SuppressFinalize(this);
  }

  public const int PartWidth=128, PartHeight=64;

  public int Width  { get { return width*PartWidth;   } }
  public int Height { get { return height*PartHeight; } }
  public IList Objects { get { return objects; } }

  public void Clear()
  { objects = new ArrayList();
    if(mru!=null) foreach(CachedSurface cs in mru) cs.Surface.Dispose();
    surfaces = new Hashtable();
    mru = new LinkedList();
    full = new string[64, 32];
    fourth = new string[16, 8];
    sixteenth = new string[4, 2];
    width = height = 0;
  }

  public void Render(Surface dest, int sx, int sy, Rectangle drect, ZoomMode zoom, bool renderObjects, Object hilite)
  { sx /= (int)zoom; sy /= (int)zoom;
    int osx=sx, osy=sy, bx=sx/PartWidth, by=sy/PartHeight, width=this.width/(int)zoom, height=this.height/(int)zoom;
    sx %= PartWidth; sy %= PartHeight;

    string[,] surfaces = (zoom==ZoomMode.Full ? full : zoom==ZoomMode.Normal ? fourth : sixteenth);

    for(int xi=0, dx=drect.X; dx<drect.Right; xi++)
    { if(bx+xi>width) break;
      if(bx+xi>=0)
        for(int yi=0, dy=drect.Y; dy<drect.Bottom; yi++)
        { if(by+yi>height) break;
          if(by+yi>=0)
          { CachedSurface cs = GetSurface(surfaces, bx+xi, by+yi);
            if(cs==null && zoom!=ZoomMode.Full)
            { ScaleDown(bx+xi, by+yi, zoom);
              cs = GetSurface(surfaces, bx+xi, by+yi);
            }
            if(cs!=null)
            { Point sloc = new Point(xi==0 ? sx : 0, yi==0 ? sy : 0);
              Rectangle srect = new Rectangle(sloc.X, sloc.Y, Math.Min(PartWidth-sloc.X, drect.Right-dx),
                                              Math.Min(PartHeight-sloc.Y, drect.Bottom-dy));
              cs.Surface.Blit(dest, srect, dx, dy);
            }
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
    bool tiles=compile;

    MemoryStream ms = new MemoryStream(4096);
    string[,] surfaces = compile ? fourth : full;

    for(int x=0,img=0; x<surfaces.GetLength(1); x++)
      for(int y=0; y<surfaces.GetLength(0); y++)
      { CachedSurface cs = GetSurface(surfaces, x, y);
        if(cs==null && compile)
        { ScaleDown(x, y, ZoomMode.Normal);
          cs = GetSurface(surfaces, x, y);
        }
        if(cs!=null && !IsEmpty(cs.Surface))
        { string fn = string.Format("layer{0}_{1}.png", layerNum, img++);
          if(!tiles)
          { writer.WriteLine(header);
            writer.WriteLine("    (tiles");
            tiles=true;
          }
          if(compile) writer.WriteLine("  (stamp (file \"{0}\") (pos {1} {2}) (layer {3}))",
                                       fn, x*PartWidth, y*PartHeight, layerNum);
          else writer.WriteLine("      (tile \"{0}\" (pos {1} {2}))", fn, x*PartWidth, y*PartHeight);
          if(zip==null) cs.Surface.Save(path+fn, ImageType.PNG);
          else
          { ms.Position = 0;
            ms.SetLength(0);
            cs.Surface.Save(ms, ImageType.PNG);
            zip.PutNextEntry(new ZipEntry(fn));
            IOH.CopyStream(ms, zip, true);
          }
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

  CachedSurface GetSurface(string[,] array, int x, int y)
  { string name = array[y, x];
    if(name==null) return null;

    CachedSurface cs;
    LinkedList.Node node = (LinkedList.Node)surfaces[name];
    if(node!=null)
    { mru.Remove(node);
      mru.Prepend(node);
      cs = (CachedSurface)node.Data;
    }
    else
    { cs = new CachedSurface(name);
      cs.Surface = LoadSurface(name);
      surfaces[name] = mru.Prepend(cs);
    }
    UnloadOldTiles();
    return cs;
  }
  
  void InsertSurface(string name, Point pos)
  { int x=pos.X/PartWidth, y=pos.Y/PartHeight;
    full = ResizeTo(full, x, y);
    full[y, x] = name;
    if(x>width)  width=x;
    if(y>height) height=y;
  }

  void InsertSurface(Surface s, int x, int y, ZoomMode zoom, bool checkEmpty)
  { s = s.CloneDisplay();
    int ox=x, oy=y;
    int bx=x/PartWidth, by=y/PartHeight, bw=(s.Width+PartWidth-1)/PartWidth, bh=(s.Height+PartHeight-1)/PartHeight;
    x %= PartWidth; y %= PartHeight;

    string[,] surfaces = (zoom==ZoomMode.Full ? full : zoom==ZoomMode.Normal ? fourth : sixteenth), narr;
    narr = ResizeTo(surfaces, bx+bw, by+bh);
    if(narr != surfaces)
    { if(zoom==ZoomMode.Full) full=narr;
      else if(zoom==ZoomMode.Normal) fourth=narr;
      else sixteenth=narr;
      surfaces=narr;
    }

    s.UsingAlpha = false; // copy alpha information. don't blend.
    for(int xi=0, sx=0; xi<bw; xi++)
    { for(int yi=0, sy=0; yi<bh; yi++)
      { CachedSurface cs = GetSurface(surfaces, bx+xi, by+yi);
        if(cs==null)
        { cs = SetSurface(surfaces, bx+xi, by+yi);
          width  = Math.Max(width,  bx+xi+1);
          height = Math.Max(height, by+yi+1);
        }
        Point dpt = new Point(xi==0 ? x : 0, yi==0 ? y : 0);
        Rectangle srect = new Rectangle(sx, sy, Math.Min(PartWidth-dpt.X, s.Width-xi*PartWidth),
                                        Math.Min(PartHeight-dpt.Y, s.Height-yi*PartHeight));
        s.Blit(cs.Surface, srect, dpt);

        if(checkEmpty && IsEmpty(cs.Surface)) RemoveSurface(surfaces, bx+xi, by+yi);
        else cs.Changed = true;

        sy += srect.Height;
      }
      sx += xi==0 ? PartWidth-x : PartWidth;
    }
    
    if(s.Width>1 && s.Height>1)
      if(zoom==ZoomMode.Full) InsertSurface(ScaleDown(s), ox/4, oy/4, ZoomMode.Normal, checkEmpty);
      else if(zoom==ZoomMode.Normal) InsertSurface(ScaleDown(s), ox/4, oy/4, ZoomMode.Tiny, checkEmpty);
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
  
  void Load(List list)
  { Clear();

    List objects = list["objects"];
    if(objects!=null) foreach(List obj in objects) this.objects.Add(new Object(obj));

    List tiles = list["tiles"];
    if(tiles!=null)
    { foreach(List image in tiles) InsertSurface(image.GetString(0), image["pos"].ToPoint());
      fourth = ResizeTo(fourth, (width+3)/4, (height+3)/4);
      sixteenth = ResizeTo(sixteenth, (width+15)/16, (height+15)/16);
    }
  }

  Surface LoadSurface(string name)
  { if(File.Exists(world.basePath+name)) return new Surface(world.basePath+name, ImageType.PNG);
    else if(world.zip!=null)
    { ZipEntry entry = world.zip.GetEntry(name);
      return new Surface(new MemoryStream(IOH.Read(world.zip.GetInputStream(entry), (int)entry.Size)),
                         ImageType.PNG, true);
    }
    else throw new ArgumentException("Unable to load surface");
  }

  string[,] ResizeTo(string[,] array, int width, int height)
  { int owidth = array.GetLength(1), oheight = array.GetLength(0);
    if(width>owidth || height>oheight)
    { string[,] narr = new string[Math.Max(width, owidth*2), Math.Max(height, oheight*2)];
      for(int y=0, nwidth=narr.GetLength(1); y<oheight; y++) Array.Copy(full, y*owidth, narr, y*nwidth, owidth);
      return narr;
    }
    return array;
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

  void ScaleDown(int x, int y, ZoomMode zoom)
  { int pw=width/((int)zoom/4), ph=height/((int)zoom/4);
    string[,] surfaces, pars;
    if(zoom==ZoomMode.Normal) { surfaces=fourth; pars=full; }
    else { surfaces=sixteenth; pars=fourth; }

    int bx=x*4, by=y*4, xlen=Math.Min(4, pw-bx), ylen=Math.Min(4, ph-by);
    int xoff=x*PartWidth, yoff=y*PartHeight;
    for(int yi=0; yi<ylen; yi++)
      for(int xi=0,ryoff=yoff+yi*(PartHeight/4); xi<xlen; xi++)
      { CachedSurface par = GetSurface(surfaces, bx+xi, by+yi);
        if(par!=null) InsertSurface(ScaleDown(par.Surface), xoff+xi*(PartWidth/4), ryoff, zoom, false);
      }
  }
    
  CachedSurface SetSurface(string[,] array, int x, int y)
  { string name = "layer" + world.NextTile + ".png";
    array[y, x] = name;

    CachedSurface cs = new CachedSurface(name);
    cs.Surface = new Surface(PartWidth, PartHeight, 32, SurfaceFlag.SrcAlpha);
    surfaces[name] = mru.Prepend(cs);

    UnloadOldTiles();
    return cs;
  }

  void RemoveSurface(string[,] array, int x, int y)
  { string name = array[y, x];
    LinkedList.Node node = (LinkedList.Node)surfaces[name];
    CachedSurface cs = node==null ? null : (CachedSurface)node.Data;
    if(cs!=null)
    { mru.Remove(node);
      surfaces.Remove(name);
    }
    if(File.Exists(world.basePath+name)) File.Delete(world.basePath+name);
    array[y, x] = null;
  }

  void UnloadOldTiles()
  { if(mru.Count>App.MaxTiles)
    { CachedSurface old = (CachedSurface)mru.Tail.Data;
      surfaces.Remove(old.Name);
      mru.Remove(mru.Tail);

      if(old.Changed)
      { old.Surface.Save(world.basePath+old.Name, ImageType.PNG);
        old.Surface.Dispose();
      }
    }
  }

  class CachedSurface
  { public CachedSurface(string name) { Name=name; }  
    public Surface Surface;
    public string  Name;
    public bool Changed;
  }

  ArrayList  objects;
  Hashtable  surfaces;
  LinkedList mru;
  string[,]  full, fourth, sixteenth;
  World      world;
  int width, height;
}

} // namespace Smarm