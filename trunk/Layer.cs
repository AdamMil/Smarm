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

/*** THIS FILE -REALLY- NEEDS REWRITING FROM SCRATCH ***/

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

  public int Width  { get { return width*PartWidth;   } } // width in full-pixels
  public int Height { get { return height*PartHeight; } } // height in full-pixels
  public IList Objects { get { return objects; } }

  public void Clear()
  { objects = new ArrayList();
    if(mru!=null)
    { Clear(full);
      Clear(fourth);
      Clear(sixteenth);
    }
    surfaces = new Hashtable();
    mru = new LinkedList();
    full = new string[32, 32];
    fourth = new string[8, 8];
    sixteenth = new string[2, 2];
    width = height = 0;
  }

  public void ClearTiles(Rectangle rect)
  { Clear(full, rect.X*4, rect.Y*4, rect.Width*4, rect.Height*4);
    Clear(fourth, rect.X, rect.Y, rect.Width, rect.Height);
    Clear(sixteenth, rect.X/4, rect.Y/4, rect.Width/4, rect.Height/4);
  }

  public void InsertSurface(Surface s, int fx, int fy)
  { InsertSurface(s, fx, fy, ZoomMode.Full, true);
    SyncScaledSizes(); // if the insertion enlarged the world size, enlarge the scaled arrays to match
  }

  public void MoveRect(Rectangle rect, int xo, int yo)
  { foreach(Object obj in objects)
    { Point pt = obj.Location;
      pt.Offset(obj.Width/2, obj.Height/2);
      if(rect.Contains(pt))
      { pt = obj.Location;
        pt.Offset(xo, yo);
        obj.Location = pt;
      }
    }
    
    // TODO: move the tiles
  }

  /* render the world starting at full-coord fx,fy into dest's drect using the zoom level specified */
  public void Render(Surface dest, int fx, int fy, Rectangle drect, ZoomMode zoom,
                     bool renderObjects, Object[] hilite, bool blend)
  { fx /= (int)zoom; fy /= (int)zoom; // convert to zoomed coordinates
    int ozx=fx, ozy=fy, bx=fx/PartWidth, by=fy/PartHeight, bw=(width+(int)zoom-1)/(int)zoom, bh=(height+(int)zoom-1)/(int)zoom;
    fx %= PartWidth; fy %= PartHeight; // fx,fy become the offset into the first block
    // ozx,ozy hold the old zoom coordinates. bx,by are the index of the starting block.
    // bw,bh are the width/height of the array we'll be using

    string[,] surfaces = (zoom==ZoomMode.Full ? full : zoom==ZoomMode.Normal ? fourth : sixteenth);

    for(int xi=0, dx=drect.X; dx<=drect.Right; xi++)
    { if(bx+xi>=bw) break;
      if(bx+xi>=0)
        for(int yi=0, dy=drect.Y; dy<=drect.Bottom; yi++)
        { if(by+yi>=bh) break;
          if(by+yi>=0)
          { CachedSurface cs = GetSurface(surfaces, bx+xi, by+yi);
            // if the surface is null and we're not at full zoom level, try to get it by scaling another zoom level
            // down to our zoom level
            if(cs==null && zoom!=ZoomMode.Full)
            { ScaleDown(bx+xi, by+yi, zoom);
              cs = GetSurface(surfaces, bx+xi, by+yi);
            }
            if(cs!=null)
            { Point sloc = new Point(xi==0 ? fx : 0, yi==0 ? fy : 0);
              Rectangle srect = new Rectangle(sloc.X, sloc.Y, Math.Min(PartWidth-sloc.X, drect.Right-dx),
                                              Math.Min(PartHeight-sloc.Y, drect.Bottom-dy));
              cs.Surface.UsingAlpha = blend; // if 'blend', do alpha blending. otherwise, copy the alpha information
              cs.Surface.Blit(dest, srect, dx, dy);
              cs.Surface.UsingAlpha = true;
            }
          }
          dy += yi==0 ? PartHeight-fy : PartHeight;
        }
      dx += xi==0 ? PartWidth-fx : PartWidth;
    }
    
    if(renderObjects) RenderObjects(dest, ozx, ozy, drect, zoom, hilite);
  }

  public void RenderObjects(Surface dest, int zx, int zy, Rectangle drect, ZoomMode zoom, Object[] hilite)
  { RenderObjects(dest, zx, zy, drect, zoom, hilite, App.Desktop.Font);
  }

  public void RenderObjects(Surface dest, int zx, int zy, Rectangle drect, ZoomMode zoom,
                            Object[] hilite, GameLib.Fonts.Font font)
  { if(zoom==ZoomMode.Normal)
      foreach(Object o in objects)
      { Rectangle bounds = o.Bounds;
        bounds.Offset(drect.X-zx, drect.Y-zy);
        if(bounds.IntersectsWith(drect))
          o.Blit(dest, bounds.X, bounds.Y, hilite!=null && Array.IndexOf(hilite, o)!=-1);
      }
    else
      foreach(Object obj in objects)
        { Rectangle r = obj.Bounds;
          if(zoom==ZoomMode.Full) { r.X *= 4; r.Y *= 4; r.Width *= 4; r.Height *= 4; }
          else { r.X /= 4; r.Y /= 4; r.Width /= 4; r.Height /= 4; }
          r.Offset(drect.X-zx, drect.Y-zy);
          if(r.IntersectsWith(drect))
          { Primitives.Box(dest, r, Color.White);
            if(zoom==ZoomMode.Full && font!=null) font.Render(dest, obj.Name, r, ContentAlignment.MiddleCenter);
          }
        }
  }

  public void Save(string path, System.IO.TextWriter writer, ZipOutputStream zip, int layerNum, bool compile)
  { if(compile) Save(path, writer, zip, true, layerNum, 0, ZoomMode.Normal);
    else
    { int imgNum=0;
      writer.WriteLine("  (layer {0}", layerNum);
      writer.WriteLine("    (tiles");
      imgNum = Save(path, writer, zip, false, layerNum, imgNum, ZoomMode.Full);
      imgNum = Save(path, writer, zip, false, layerNum, imgNum, ZoomMode.Normal);
      imgNum = Save(path, writer, zip, false, layerNum, imgNum, ZoomMode.Tiny);
      writer.WriteLine("    )");
    }

    if(objects.Count>0)
    { if(!compile) writer.WriteLine("    (objects");
      foreach(Object o in objects) o.Save(writer, compile ? layerNum : -1);
      if(!compile) writer.WriteLine("    )");
    }
    if(!compile) writer.WriteLine("  )");
  }

  // offsets are in world pixels and guaranteed to be a multiple of the partsize/4 (so it's a multiple of the size
  // of a full block in world pixels)
  public void Shift(int wxo, int wyo)
  { foreach(Object obj in objects)
    { Point p = obj.Location;
      p.Offset(wxo, wyo);
      obj.Location = p;
    }

    if(width==0) return;
    full = Shift(full, wxo*4/PartWidth, wyo*4/PartHeight, width, height);

    if(wxo%PartWidth==0 && wyo%PartHeight==0) // if it's a multiple of the world-blocks, shift those too
    { fourth = Shift(fourth, wxo/PartWidth, wyo/PartHeight, width/4, height/4);
      if(wxo%(PartWidth*4)==0 && wyo%(PartHeight*4)==0) // if it's a multiple of the tiny-blocks, shift them
        sixteenth = Shift(sixteenth, wxo/4/PartWidth, wyo/4/PartHeight, width/16, height/16);
      else Clear(sixteenth); // otherwise drop the tiny-tiles
    }
    else { Clear(fourth); Clear(sixteenth);  } // otherwise just drop all the zoomed tiles
    
    width  += wxo*4/PartWidth;  // update the width
    height += wyo*4/PartHeight;
    SyncScaledSizes();          // and the scaled widths, too
  }

  void Clear(string[,] array) // remove all surfaces in an array
  { int w=array.GetLength(1), h=array.GetLength(0);
    for(int y=0; y<h; y++) for(int x=0; x<w; x++) RemoveSurface(array, x, y);
  }

  // remove a rectangle of surfaces in an array
  void Clear(string[,] array, int zx, int zy, int zw, int zh)
  { int bx=zx/PartWidth, by=zy/PartHeight, bw=(zw+PartWidth-1)/PartWidth, bh=(zh+PartHeight-1)/PartHeight;
    bw = Math.Min(bw, array.GetLength(1)-bx); bh = Math.Min(bh, array.GetLength(0)-by);
    for(int yi=0; yi<bh; yi++) for(int xi=0; xi<bw; xi++) RemoveSurface(array, bx+xi, by+yi);
  }

  // return a cachedsurface for the given array element, loading it first if necessary
  CachedSurface GetSurface(string[,] array, int x, int y)
  { string name = array[y, x];
    if(name==null) return null;

    CachedSurface cs;
    LinkedList.Node node = (LinkedList.Node)surfaces[name];
    if(node!=null) // if found, move it to the front of the MRU list
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
  
  // insert a surface name into an array (used during level loading)
  void InsertSurface(string name, Point pos, ZoomMode zoom)
  { string[,] surfaces = (zoom==ZoomMode.Full ? full : zoom==ZoomMode.Normal ? fourth : sixteenth);
    int x=pos.X/PartWidth, y=pos.Y/PartHeight;
    string[,] narr = ResizeTo(surfaces, x+1, y+1);
    if(narr!=surfaces)
    { if(zoom==ZoomMode.Full) full=narr;
      else if(zoom==ZoomMode.Normal) fourth=narr;
      else sixteenth=narr;
      surfaces=narr;
    }
    surfaces[y, x] = name;
    if(x+1>width)  width=x+1;
    if(y+1>height) height=y+1;
  }

  // insert a surface into a zoom level, chopping it up as appropriate
  void InsertSurface(Surface s, int zx, int zy, ZoomMode zoom, bool checkEmpty)
  { s = s.CloneDisplay();
    int ozx=zx, ozy=zy, bx=zx/PartWidth, by=zy/PartHeight;
    zx %= PartWidth; zy %= PartHeight;
    int bw=(zx+s.Width+PartWidth-1)/PartWidth, bh=(zy+s.Height+PartHeight-1)/PartHeight;
    // ozx,ozy = original zoom coords. zx,zy = offset into first block. bw,bh = number of affected blocks

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
        Point dpt = new Point(xi==0 ? zx : 0, yi==0 ? zy : 0);
        Rectangle srect = new Rectangle(sx, sy, Math.Min(PartWidth-dpt.X, s.Width-xi*PartWidth),
                                        Math.Min(PartHeight-dpt.Y, s.Height-yi*PartHeight));
        s.Blit(cs.Surface, srect, dpt);

        if(checkEmpty && IsEmpty(cs.Surface)) RemoveSurface(surfaces, bx+xi, by+yi);
        else cs.Changed = true;

        sy += srect.Height;
      }
      sx += xi==0 ? PartWidth-zx : PartWidth;
    }
    
    // FIXME: if the surface has area (shouldn't it always?!), clear the affected scaled blocks
    if(s.Width>1 && s.Height>1 && zoom==ZoomMode.Full)
    { Clear(fourth, ozx/4, ozy/4, s.Width, s.Height);
      Clear(sixteenth, ozx/16, ozy/16, s.Width, s.Height);
    }
  }

  bool IsEmpty(Surface s) // return true if a surface contains only transparent pixels
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
    { foreach(List image in tiles)
      { ZoomMode zoom = ZoomMode.Full;
        if(image["zoom"]!=null) zoom = (ZoomMode)image["zoom"].GetInt(0);
        InsertSurface(image.GetString(0), image["pos"].ToPoint(), zoom);
      }
      SyncScaledSizes();
    }
  }

  // given a filename, load a surface, looking first in the directory and then in the zip file
  Surface LoadSurface(string name)
  { if(File.Exists(world.basePath+name)) return new Surface(world.basePath+name, ImageType.PNG);
    else if(world.zip!=null)
    { ZipEntry entry = world.zip.GetEntry(name);
      return new Surface(new MemoryStream(IOH.Read(world.zip.GetInputStream(entry), (int)entry.Size)),
                         ImageType.PNG, true);
    }
    else throw new ArgumentException("Unable to load surface");
  }

  // remove a surface and delete it from the disk if it's been swapped out
  void RemoveSurface(string[,] array, int x, int y)
  { string name = array[y, x];
    if(name==null) return;
    LinkedList.Node node = (LinkedList.Node)surfaces[name];
    CachedSurface cs = node==null ? null : (CachedSurface)node.Data;
    if(cs!=null)
    { cs.Surface.Dispose();
      mru.Remove(node);
      surfaces.Remove(name);
    }
    if(File.Exists(world.basePath+name)) File.Delete(world.basePath+name);
    array[y, x] = null;
  }

  // resize the array to at least widthxheight and return the new array.
  // if no resizing is needed, the old array is returned.
  string[,] ResizeTo(string[,] array, int width, int height)
  { int owidth = array.GetLength(1), oheight = array.GetLength(0);
    if(width>owidth || height>oheight)
    { int nw=width>owidth ? Math.Max(width, owidth*2) : owidth;
      int nh=height>oheight ? Math.Max(height, oheight*2) : oheight;
      string[,] narr = new string[nh, nw];
      for(int y=0; y<oheight; y++) Array.Copy(array, y*owidth, narr, y*nw, owidth);
      return narr;
    }
    return array;
  }
  
  int Save(string path, TextWriter writer, ZipOutputStream zip, bool compile, int layerNum, int imgNum, ZoomMode zoom)
  { string[,] surfaces = (zoom==ZoomMode.Full ? full : zoom==ZoomMode.Normal ? fourth : sixteenth);
    MemoryStream ms = new MemoryStream(4096);
    for(int x=0; x<surfaces.GetLength(1); x++)
      for(int y=0; y<surfaces.GetLength(0); y++)
      { CachedSurface cs = GetSurface(surfaces, x, y);
        if(cs==null && compile)
        { ScaleDown(x, y, zoom);
          cs = GetSurface(surfaces, x, y);
        }
        if(cs!=null)
        { if(!IsEmpty(cs.Surface))
          { string fn = string.Format("layer{0}_{1}.png", layerNum, imgNum++);
            if(compile) writer.WriteLine("  (stamp (file \"{0}\") (pos {1} {2}) (layer {3}))",
                                        fn, x*PartWidth, y*PartHeight, layerNum);
            else writer.WriteLine("      (tile \"{0}\" (pos {1} {2}) (zoom {3}))",
                                  fn, x*PartWidth, y*PartHeight, (int)zoom);
            if(zip==null) cs.Surface.Save(path+fn, ImageType.PNG);
            else
            { ms.Position = 0;
              ms.SetLength(0);
              cs.Surface.Save(ms, ImageType.PNG);
              zip.PutNextEntry(new ZipEntry(fn));
              IOH.CopyStream(ms, zip, true);
            }
            
            if(File.Exists(world.basePath+cs.Name)) File.Delete(world.basePath+cs.Name);
            // update the filename. note that this leaves the array in an invalid state, but we'll fix it below.
            // we can't fix it now because the new name might conflict with another tile having that name
            cs.Name=fn;
          }
          else RemoveSurface(surfaces, x, y);
        }
      }

    // fix up the filenames
    for(int x=0; x<surfaces.GetLength(1); x++)
      for(int y=0; y<surfaces.GetLength(0); y++)
      { CachedSurface cs = GetSurface(surfaces, x, y);
        if(cs!=null && cs.Name!=surfaces[y,x])
        { LinkedList.Node node = (LinkedList.Node)this.surfaces[surfaces[y,x]];
          this.surfaces.Remove(surfaces[y,x]);
          this.surfaces[surfaces[y,x]=cs.Name] = node;
        }
      }

    return imgNum;
  }
  
  // take an image and return a copy in the same format, but scaled down to 1/16th the size (1/4th on each axis)
  Surface ScaleDown(Surface s)
  { Surface ret = new Surface((s.Width+2)/4, (s.Height+2)/4, s.Format);
    s.Lock(); ret.Lock();
    for(int y=0; y<ret.Height; y++)
      for(int x=0; x<ret.Width; x++)
      { int a=0, r=0, g=0, b=0, n=0, ah;
        for(int osx=x*4,sxe=Math.Min(osx+4, s.Width),sy=y*4,sye=Math.Min(sy+4, s.Height); sy<sye; sy++)
          for(int sx=osx; sx<sxe; n++,sx++)
          { Color c = s.GetPixel(sx, sy);
            a+=c.A; r+=c.R*c.A; g+=c.G*c.A; b+=c.B*c.A;
          }
        if(a!=0)
        { ah=a/2;
          r=Math.Min(255, (r+ah)/a); g=Math.Min(255, (g+ah)/a); b=Math.Min(255, (b+ah)/a); 
        }
        a=Math.Min(255, (a+n/2)/n);
        ret.PutPixel(x, y, Color.FromArgb(a, r, g, b));
      }
    ret.Unlock(); s.Unlock();
    return ret;
  }

  // given a non-full zoom mode and the index of an empty tile x,y, it attempts to scale down larger tiles
  // to fill the x,y tile.
  void ScaleDown(int x, int y, ZoomMode zoom)
  { int zs=(int)zoom/4;
    int pw=(width+zs-1)/zs, ph=(height+zs-1)/zs;
    string[,] surfaces, pars;
    if(zoom==ZoomMode.Normal) { surfaces=fourth; pars=full; }
    else { surfaces=sixteenth; pars=fourth; }

    int bx=x*4, by=y*4, xlen=Math.Min(4, pw-bx), ylen=Math.Min(4, ph-by);
    int xoff=x*PartWidth, yoff=y*PartHeight;
    for(int yi=0; yi<ylen; yi++)
      for(int xi=0; xi<xlen; xi++)
      { CachedSurface par = GetSurface(pars, bx+xi, by+yi);
        if(par==null && zoom==ZoomMode.Tiny)
        { ScaleDown(bx+xi, by+yi, (ZoomMode)zs);
          par = GetSurface(pars, bx+xi, by+yi);
        }
        if(par!=null)
          InsertSurface(ScaleDown(par.Surface), xoff+xi*(PartWidth/4), yoff+yi*(PartHeight/4), zoom, false);
      }
  }
    
  // given the index of an empty tile, it creates an empty surface with a new name and fills that space
  // the surface should then be filled with valid data
  CachedSurface SetSurface(string[,] array, int x, int y)
  { if(array[y, x]!=null) throw new InvalidOperationException("Tile is not null!");
    string name = "layer" + world.NextTile + ".png";
    array[y, x] = name;

    CachedSurface cs = new CachedSurface(name);
    cs.Surface = new Surface(PartWidth, PartHeight, 32, SurfaceFlag.SrcAlpha);
    surfaces[name] = mru.Prepend(cs);

    UnloadOldTiles();
    return cs;
  }

  // shift an array over by x,y tiles. width,height are the width,heigth of the array's existing data
  string[,] Shift(string[,] array, int xo, int yo, int width, int height)
  { string[,] narr;
    int nw=width+xo, nh=height+yo;
    if(nw>array.GetLength(1) || nh>array.GetLength(0))
      narr = new string[Math.Max(nh, array.GetLength(0)*2), Math.Max(nw, array.GetLength(1)*2)];
    else narr = array;

    for(int y=nh-1; y>=yo; y--)
      for(int x=nw-1; x>=xo; x--)
      { narr[y,x] = array[y-yo,x-xo];
        narr[y-yo,x-xo] = null;
      }
    return narr;
  }

  // make sure the scaled-down arrays are big enough to hold the full image
  void SyncScaledSizes()
  { fourth = ResizeTo(fourth, (width+3)/4, (height+3)/4);
    sixteenth = ResizeTo(sixteenth, (width+15)/16, (height+15)/16);
  }

  // unload least recently used tiles from memory, saving them to disk if they've been changed
  void UnloadOldTiles()
  { while(mru.Count>App.MaxTiles)
    { CachedSurface old = (CachedSurface)mru.Tail.Data;
      surfaces.Remove(old.Name);
      mru.Remove(mru.Tail);

      if(old.Changed)
      { old.Surface.Save(world.basePath+old.Name, ImageType.PNG);
        old.Surface.Dispose();
        old.Changed = false; // the on-disk version is now up to date
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