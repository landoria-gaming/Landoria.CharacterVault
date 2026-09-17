using System;
using System.IO;
using Splatform;

namespace Landoria.CharacterVault
{
    internal static class ProfileFile
    {
        internal static byte[] Read(PlayerProfile profile)
        {
            FileReader reader = new FileReader(profile.GetPath(), profile.m_fileSource);
            try
            {
                Stream stream = reader.m_binary.BaseStream;
                stream.Position = 0;
                byte[] data = new byte[stream.Length];
                int offset = 0;
                while (offset < data.Length)
                {
                    int read = stream.Read(data, offset, data.Length - offset);
                    if (read == 0)
                    {
                        throw new EndOfStreamException("The character profile ended unexpectedly.");
                    }

                    offset += read;
                }

                return data;
            }
            finally
            {
                reader.Dispose();
            }
        }

        internal static PlayerProfile ReplaceSelected(byte[] data)
        {
            PlayerProfile selected = Game.instance.GetPlayerProfile();
            string path = selected.GetPath();
            string next = path + ".vault-new";
            Write(next, selected.m_fileSource, data);
            SaveApiCompatibility.ReplaceOldFile(path, next, selected.m_fileSource);
            SaveApiCompatibility.InvalidateCharacterCache();
            PlayerProfile loaded = new PlayerProfile(selected.GetFilename(), selected.m_fileSource);
            if (!loaded.Load())
            {
                throw new InvalidDataException("Valheim rejected the authoritative server profile.");
            }

            return loaded;
        }

        private static void Write(string path, FileHelpers.FileSource source, byte[] data)
        {
            FileWriter writer = SaveApiCompatibility.CreateWriter(path, source);
            writer.m_binary.Write(data);
            writer.Finish();
            if (writer.Status != FileWriter.WriterStatus.CloseSucceeded)
            {
                throw new IOException("The authoritative character profile could not be written.");
            }
        }
    }
}
