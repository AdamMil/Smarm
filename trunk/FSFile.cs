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
  internal int EndOffset { get { return DataOffset+length; } }
  internal string name; // okay, so this provides zero effective encapsulation...
  internal int offset, length;
}

class FSFile : IDisposable
{ public FSFile(string filename) : this(filename, FileAccess.ReadWrite) { }
  public FSFile(string filename, FileAccess access) { Open(filename, access); }
  ~FSFile() { Dispose(true); }
  public void Dispose() { Dispose(false); GC.SuppressFinalize(this); }

  public void Abandon()
  { if(file!=null)
    { file.Close();
      file=null;
      free=null;
      names=null;
      chunks=null;
    }
  }

  public Stream AddFile(string name, int length)
  { AssertOpen();
    if(name.Length>255) throw new ArgumentException("Name cannot be longer than 255 characters");
    if(length<0) throw new ArgumentOutOfRangeException("length", length, "must not be negative");
    FSEntry entry;
    { LinkedList.Node node = (LinkedList.Node)names[name];
      entry = node==null ? null : (FSEntry)node.Data;
    }
    if(entry==null || entry.Length!=length)
    { if(entry!=null) DeleteFile(entry);
if(names[name]!=null) throw new Exception("stest4");
      int tmhead=length+name.Length, total=tmhead+HeaderSize;
      foreach(LinkedList.Node node in free)
      { entry = (FSEntry)node.Data;
        if(entry.Name==null && (entry.Length>=total || entry.Length==tmhead))
        { int extra = entry.Length-total;
if(!free.Contains(node)) throw new Exception("wtf");
if(!chunks.Contains(node)) throw new Exception("wtf2");
          if(extra==-HeaderSize)
          { entry.name = name;
            entry.length = length;
            free.Remove(node);
            names[name] = node;
          }
          else
          { int offset = entry.offset;
            entry.length -= total;
            entry.offset += total;
            names[name] = chunks.InsertBefore(node, new FSEntry(name, offset, length));
          }
          return GetStream(name);
        }
      }
      FSEntry tail = chunks.Tail==null ? null : (FSEntry)chunks.Tail.Data;
      entry = new FSEntry(name, tail==null ? 0 : tail.EndOffset, length);
      names[name] = chunks.Append(entry);
    }
    return GetStream(name);
  }

  public void Close()
  { if(file!=null)
    { Save();
      Abandon();
    }
  }
  
  public bool Contains(string name) { AssertOpen(); return names.Contains(name); }

  public void DeleteFile(FSEntry entry) { DeleteFile(entry.Name); }
  public void DeleteFile(string name)
  { AssertOpen();
    LinkedList.Node node = (LinkedList.Node)names[name];
    if(node==null) throw new ArgumentException("That file doesn't exist", "name");
    names.Remove(name);

    LinkedList.Node prev=node.PrevNode, next=node.NextNode;
    FSEntry entry = (FSEntry)node.Data;
if(entry.Name==null) throw new Exception("stest1");
    entry.name = null;
    entry.length += name.Length;
    if(next!=null)
    { FSEntry nextEntry = (FSEntry)next.Data;
      if(nextEntry.Name==null)
      { entry.length += nextEntry.Length+HeaderSize;
        chunks.Remove(next);
        free.Remove(next);
if(chunks.Contains(next)) throw new Exception("stest7");
      }
    }
    if(prev!=null)
    { FSEntry prevEntry = (FSEntry)prev.Data;
      if(prevEntry.Name==null)
      { prevEntry.length += entry.Length+HeaderSize;
        chunks.Remove(node);
if(!chunks.Contains(prev)) throw new Exception("stest5");
if(!free.Contains(prev)) throw new Exception("stest8");
        return;
      }
    }
if(!chunks.Contains(node)) throw new Exception("stest6");
    free.Add(node);
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

  public void Save()
  { AssertOpen();
    int length=0, alloc=0, free=0, nchunks=0, namelen=0;
    foreach(FSEntry entry in chunks)
    { file.Position = entry.offset;
nchunks++;
      if(entry.Name==null) file.WriteByte(0);
      else
      { file.WriteByte((byte)entry.Name.Length);
        IOH.WriteString(file, entry.Name);
namelen += entry.Name.Length;
      }
      IOH.WriteBE4(file, entry.Length);
if(entry.Name==null) free += entry.Length;
else alloc += entry.Length;
      IOH.WriteBE4(file, 0); // reserved
      if(entry.EndOffset>length) length=entry.EndOffset;
    }
    file.SetLength(length);
Console.WriteLine("Wrote: alloc={0}, free={1}, namelen={2}, length={3}, chunks={4}", alloc, free, namelen, length, nchunks);
    file.Flush();
  }

  void AssertOpen()
  { if(file==null) throw new InvalidOperationException("File has already been closed!");
  }

  void Dispose(bool destructing) { Abandon(); }

  void Open(string filename, FileAccess access)
  { FileMode mode = FileMode.OpenOrCreate;
    switch(access)
    { case FileAccess.Read: mode=FileMode.Open; break;
      case FileAccess.Write: mode=FileMode.Create; break;
    }

    file = File.Open(filename, mode, access);
    free = new ArrayList();
    names = new Hashtable();
    chunks = new LinkedList();

int last=-1;
    while(file.Position<file.Length)
    { int offset   = (int)file.Position;
if(offset<=last) throw new Exception("stest2");
last=offset;
      byte nameLen = IOH.Read1(file);
      string name  = nameLen==0 ? null : IOH.ReadString(file, nameLen);
      int length   = IOH.ReadBE4(file);
if(length<=0) throw new Exception("stest3");
      IOH.Skip(file, 4); // reserved field
      LinkedList.Node node = chunks.Append(new FSEntry(name, offset, length));
      if(name==null) free.Add(node);
      else names[name] = node;
      IOH.Skip(file, length);
    }
  }

  internal const int HeaderSize=9; // plus name.Length

  FileStream file;
  Hashtable  names;
  LinkedList chunks;
  ArrayList  free;
}

} // namespace Smarm