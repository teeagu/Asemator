using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

// Aseprite format documentation: https://github.com/aseprite/aseprite/blob/master/docs/ase-file-specs.md

namespace Asemation
{
    public class AseInfo
    {
        private enum Chunk
        {
            Cel = 0x2005,
            Tags = 0x2018,
            Slice = 0x2022 // I'll use this as pivot later :D
        }

        //public readonly ColorDepth colorDepth;
        public readonly int width;
        public readonly int height;
        public readonly int frameCount;

        public List<Frame> frames = new List<Frame>();
        public List<Tag> tags = new List<Tag>();

        public AseInfo(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                AseReader reader = new AseReader(stream);

                reader.DWord(); // File size

                ushort magicNumber = reader.Word();
                if (magicNumber != 0xA5E0) Debug.LogError("File is not in .ase format");

                frameCount = reader.Word();
                width = reader.Word();
                height = reader.Word();
                
                reader.Word();    // Color depth (asemator only supports RGBA)
                reader.DWord();   // Flags
                reader.Word();    // Speed (deprecated)
                reader.DWord();   // Set be 0
                reader.DWord();   // Set be 0
                reader.Byte();    // Palette entry 
                reader.Seek(3);   // Ignore these bytes
                reader.Word();    // Number of colors (0 means 256 for old sprites)
                reader.Byte();    // Pixel width
                reader.Byte();    // Pixel height
                reader.Seek(92);  // For Future

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    Frame frame = new Frame();
                    frame.pixels = new Color[width * height];

                    long frameStart, frameEnd;
                    int chunkCount;

                    frameStart = reader.Position;
                    frameEnd = frameStart + reader.DWord();  // Bytes in this frame
                    reader.Word();                           // Magic number (always 0xF1FA)
                    chunkCount = reader.Word();              // Number of chunks in this frame
                    frame.duration = reader.Word();          // Frame duration in milliseconds
                    reader.Seek(6);                          // For future (set to zero)

                    for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
                    {
                        long chunkStart = reader.BaseStream.Position;
                        long chunkEnd = chunkStart + reader.DWord();
                        Chunk chunkType = (Chunk)reader.Word();
                        
                        switch (chunkType)
                        {
                            case Chunk.Cel: HandleCelChunk(reader, ref frame.pixels); break;
                            case Chunk.Tags: HandleTagChunk(reader); break;
                        }

                        reader.Position = chunkEnd;
                    }

                    frames.Add(frame);
                    reader.Position = frameEnd;
                }
            }
        }

        public class Frame
        {
            public float duration;
            public Color[] pixels;
        }

        public class Tag
        {
            public string name;
            public int from;
            public int to;
        }

        public class AseReader
        {
            BinaryReader _reader;

            public AseReader(FileStream stream) => _reader = new BinaryReader(stream);

            public long Position
            {
                get => _reader.BaseStream.Position;
                set => _reader.BaseStream.Position = value;
            }

            public Stream BaseStream => _reader.BaseStream;

            // Matching aseprite documentation references
            public byte Byte() => _reader.ReadByte();
            public ushort Word() => _reader.ReadUInt16();
            public short Short() => _reader.ReadInt16();
            public uint DWord() => _reader.ReadUInt32();
            public long Long() => _reader.ReadInt32();
            public string String() => Encoding.UTF8.GetString(Bytes(Word()));
            public byte[] Bytes(int number) => _reader.ReadBytes(number);
            public void Seek(int number) => _reader.BaseStream.Position += number;
        }

        void HandleCelChunk(AseReader reader, ref Color[] framePixels)
        {
            reader.Word();                   // Layer index
            int celX = reader.Short();       // X position
            int celY = reader.Short();       // Y position
            reader.Byte();                   // Opacity level
            reader.Word();                   // Cel type
            reader.Seek(7);                  // For future (set to zero)
            int celWidth = reader.Word();    // Width
            int celHeight = reader.Word();   // Height
            reader.Seek(2);

            int celByteCount = celWidth * celHeight * 4;  // Size of a cel times RGBA 
            byte[] celBytes = new byte[celByteCount];
            DeflateStream deflate = new DeflateStream(reader.BaseStream, CompressionMode.Decompress);
            deflate.Read(celBytes, 0, celByteCount);
            framePixels = CelToFrame(celBytes, celX, celY, celWidth, celHeight);
        }

        void HandleTagChunk(AseReader reader)
        {
            int tagCount = reader.Word();  // Number of tags
            reader.Seek(8);                // For future (set to zero) 

            for (int tagIndex = 0; tagIndex < tagCount; tagIndex++)
            {
                Tag tag = new Tag();
                tag.from = reader.Word();    // From frame
                tag.to = reader.Word();      // To frame 
                reader.Byte();               // Loop direction
                reader.Seek(8);              // For future (set to zero) 
                reader.Seek(3);              // Tag color
                reader.Seek(1);              // Extra byte (zero)
                tag.name = reader.String();  // Tag name
                tags.Add(tag);
            }
        }

        Color[] CelToFrame(byte[] bytes, int celX, int celY, int celWidth, int celHeight)
        {
            Color[] celPixels = new Color[bytes.Length / 4];
            Color[] framePixels = new Color[width * height];

            for (int p = 0, b = 0; p < celPixels.Length; p++, b += 4)
            {
                celPixels[p].r = bytes[b + 0] * bytes[b + 3] / 255;
                celPixels[p].g = bytes[b + 1] * bytes[b + 3] / 255;
                celPixels[p].b = bytes[b + 2] * bytes[b + 3] / 255;
                celPixels[p].a = bytes[b + 3];
            }

            for (int sx = 0; sx < celWidth; sx++)
            {
                int dx = celX + sx;
                int dy = celY * width;

                for (int i = 0, sy = 0; i < celHeight; i++, sy += celWidth, dy += width)
                {
                    framePixels[dx + dy] = celPixels[sx + sy];
                }
            }

            return framePixels;
        }
    }
}