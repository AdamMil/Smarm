/*
PSDtoPNG is a tool for the game Swarm, which was written by Jim Crawford.
It converts layers in a PSD file into tiles in a PNG file.
http://www.adammil.net
Copyright (C) 2004 Adam Milazzo

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
using System.Drawing;
using System.IO;
using GameLib.Video;

namespace PSDtoPNG
{

class App
{
	static void Main(string[] args)
	{ if(args.Length != 2)
	  { Console.WriteLine("USAGE: PSDtoPNG input.psd output.png");
	    return;
	  }

	  PSDImage psd = PSDCodec.ReadPSD(args[0]);
	  if(psd.Layers==null)
	  { ScaleDown(psd.Flattened).Save(args[1], ImageType.PNG);
	  }
	  else
	  { Surface output = new Surface(psd.Width/4 * psd.Layers.Length, psd.Height/4, 32, SurfaceFlag.SrcAlpha);
	    Surface   temp = new Surface(psd.Width, psd.Height, 32, SurfaceFlag.SrcAlpha);
	    for(int i=psd.Layers.Length-1,x=0; i>=0; i--)
	    { PSDLayer layer = psd.Layers[i];
	      temp.Fill(0);
	      layer.Surface.UsingAlpha = false;
	      layer.Surface.Blit(temp, layer.Location);
	      ScaleDown(temp).Blit(output, x, 0);
	      x += psd.Width/4;
	    }
	    output.Save(args[1], ImageType.PNG);
	  }
	  Console.WriteLine("(spriteset (file {0}) (elementwidth {1}))", Path.GetFileName(args[1]), psd.Width/4);
	}

  static Surface ScaleDown(Surface s)
  { Surface ret = new Surface(s.Width/4, s.Height/4, s.Format);
    ret.UsingAlpha = false;
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
}

} // namespace PSDtoPNG
