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
using System.IO;
using GameLib.Collections;
using GameLib.IO;

namespace Smarm
{

class FSEntry
{ public FSEntry(string name) { this.name=name; }
  internal FSEntry(string name, int offset, int length) { this.name=name; this.offset=offset; this.length=length; }

  public string Name { get { return name; } }
  public int Length { get { return length; } }

  internal int DataOffset { get { return offset+FSFile.HeaderSize+(name==null ? 0 : name.Length); } }
  internal string name; // okay, so this provides zero effective encapsulation...
  internal int offset, length;
}

class FSFile : IDisposable
{ public FSFile(string filename) : this(filename, FileAccess.ReadWrite) { }
  public FSFile(string filename, FileAccess access) { Open(filename, access); }
  ~FSFile() { Dispose(true); }
  public void Dispose() { Dispose(false); GC.SuppressFinalize(this); }

  public void Close()
  { if(file!=null)
    { Flush();
      file.Close();
      file=null;
      names=null;
      chunks=null;
    }
  }

  public Stream AddFile(string name, int length)
  { AssertOpen();
    if(name.Length>255) throw new ArgumentException("Name cannot be longer than 255 characters");
    if(length<0) throw new ArgumentOutOfRangeException("length", length, "must not be negative");
    if(names.Contains(name)) DeleteFile((FSEntry)((LinkedList.Node)names[name]).Data);
    FSEntry entry;
    foreach(LinkedList.Node node in chunks)
    { entry = (FSEntry)node.Data;
      if(entry.Name==null && entry.Length>=length)
      { int extra = entry.Length-length;
        entry.name = name;
        entry.length = length;
        if(extra>0) chunks.InsertAfter(node, new FSEntry(null, entry.offset+HeaderSize+name.Length, extra));
        return GetStream(name);
      }
    }
    FSEntry tail = (FSEntry)chunks.Tail.Data;
    entry = new FSEntry(name, tail.DataOffset+tail.Length, length);
    chunks.Append(entry);
    return GetStream(name);
  }

  public void DeleteFile(FSEntry entry) { DeleteFile(entry.Name); }
  public void DeleteFile(string name)
  { AssertOpen();
    LinkedList.Node node = (LinkedList.Node)names[name], prev=node.PrevNode, next=node.NextNode;
    FSEntry entry = (FSEntry)node.Data;
    entry.name = null;
    if(next!=null)
    { FSEntry nextEntry = (FSEntry)next.Data;
      if(nextEntry.Name==null)
      { entry.length += nextEntry.Length+HeaderSize;
        chunks.Remove(next);
      }
    }
    if(prev!=null)
    { FSEntry prevEntry = (FSEntry)prev.Data;
      if(prevEntry.Name==null)
      { prevEntry.length += entry.Length+HeaderSize;
        chunks.Remove(node);
      }
    }
  }

  public void Flush()
  { AssertOpen();
    foreach(LinkedList.Node node in chunks)
    { FSEntry entry = (FSEntry)node.Data;
      file.Position = entry.offset;
      file.WriteByte((byte)entry.Name.Length);
      IOH.WriteString(file, entry.Name);
      IOH.WriteBE4(file, entry.Length);
    }
    file.Flush();
  }

  public FSEntry GetEntry(string name)
  { AssertOpen();
    LinkedList.Node node = (LinkedList.Node)names[name];
    if(node==null) throw new ArgumentException("File '"+name+"' does not exist in the collection");
    return (FSEntry)node.Data;
  }

  public Stream GetStream(string name) { return GetStream(GetEntry(name)); }
  public Stream GetStream(FSEntry entry)
  { AssertOpen();
    return new StreamStream(file, entry.DataOffset, entry.Length, true);
  }

  void AssertOpen()
  { if(file==null) throw new InvalidOperationException("File has already been closed!");
  }

  void Dispose(bool destructing) { Close(); }

  void Open(string filename, FileAccess access)
  { FileMode mode = FileMode.OpenOrCreate;
    switch(access)
    { case FileAccess.Read: mode=FileMode.Open; break;
      case FileAccess.Write: mode=FileMode.Create; break;
    }

    file = File.Open(filename, mode, access);
    names = new Hashtable();
    chunks = new LinkedList();

    while(file.Position<file.Length)
    { int offset   = (int)file.Position;
      byte nameLen = IOH.Read1(file);
      string name  = nameLen==0 ? null : IOH.ReadString(file, nameLen);
      int length   = IOH.ReadBE4(file);
      IOH.Skip(file, 4); // reserved field
      LinkedList.Node node = chunks.Append(new FSEntry(name, offset, length));
      if(name!=null) names[name] = node;
      IOH.Skip(file, length);
    }
  }

  internal const int HeaderSize=9; // plus name.Length

  FileStream file;
  Hashtable  names;
  LinkedList chunks;
}

} // namespace Smarm